namespace Iris.Service

// * Imports

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Persistence
open Iris.Service.Git
open Iris.Service.WebSockets
open Iris.Service.Raft
open Microsoft.FSharp.Control
open LibGit2Sharp

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

module Iris =

  // ** tag

  [<Literal>]
  let private tag = "IrisServer"

  let private signature =
    new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Status of ServiceStatus
    | Git    of GitEvent
    | Socket of SocketEvent
    | Raft   of RaftEvent
    | Log    of LogEvent

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Ok
    | Entry of EntryResponse

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Message

  type private Message = Msg * ReplyChan

  // ** IrisAgent

  type private IrisAgent = MailboxProcessor<Message>

  // ** disposeAll

  let private disposeAll (disposables: IDisposable seq) =
    Seq.iter dispose disposables

  // ** IrisState

  [<NoComparison;NoEquality>]
  type private IrisState =
    { Status       : ServiceStatus
      Store        : Store
      Project      : IrisProject option
      GitServer    : IGitServer option
      RaftServer   : IRaftServer option
      SocketServer : IWebSocketServer option
      Disposables  : IDisposable list }

    interface IDisposable with
      member self.Dispose() =
        disposeAll self.Disposables
        Option.map dispose self.GitServer    |> ignore
        Option.map dispose self.RaftServer   |> ignore
        Option.map dispose self.SocketServer |> ignore

  // ** resetState

  let private resetState (state: IrisState) =
    dispose state
    { state with
        Store        = new Store(State.Empty)
        Project      = None
        GitServer    = None
        RaftServer   = None
        SocketServer = None
        Disposables  = [] }

  // ** IIrisServer

  type IIrisServer =
    inherit IDisposable

    abstract Status : Either<IrisError,ServiceStatus>
    abstract Start : unit -> Either<IrisError,unit>
    abstract Load : FilePath -> Either<IrisError,unit>

  // ** broadcastMsg

  let private broadcastMsg (state: IrisState) (cmd: StateMachine) =
    match state.SocketServer with
    | Some server ->
      match server.Broadcast with
      | Right () -> ()
      | Left errors ->
        List.iter (string >> Logger.err state.RaftServer.NodeId tag) errors
    | None ->
      "Could not send logs to clients: No socket server available"
      |> Logger.err state.RaftServer.NodeId tag

  // ** sendMsg

  let private sendMsg (state: IrisState) (id: Id) (cmd: StateMachine) =
    match state.SocketServer with
    | Some server ->
      match server.Send id cmd with
      | Right () -> ()
      | Left error ->
        error
        |> string
        |> Logger.err state.RaftServer.NodeId tag
    | None ->
      "Could not send logs to clients: No socket server available"
      |> Logger.err state.RaftServer.NodeId tag

  // ** appendCmd

  let private appendCmd (state: IrisState) (cmd: StateMachine) =
    match state.RaftServer with
    | Some server -> server.Append(cmd)
    | None ->
      "Could not append command to raft: No server available"
      |> Other
      |> Either.fail

  // ** onOpen

  // __        __   _    ____             _        _
  // \ \      / /__| |__/ ___|  ___   ___| | _____| |_ ___
  //  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __/ __|
  //   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_\__ \
  //    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|___/

  /// ## OnOpen
  ///
  /// Register a callback with the WebSocket server that is run when new browser session has
  /// contacted this IrisSerivce. First, we send a `DataSnapshot` to the client to initialize it
  /// with the current state. Then, we append the newly created Session value to the Raft log to
  /// replicate it throughout the cluster.

  let private onOpen (state: IrisState) (id: Id) (chan: ReplyChan) =
    sendMsg id (DataSnapshot state.Store.State)
    match appendCmd (AddSession session) with
    | Right entry ->
      entry
      |> Reply.Entry
      |> Either.succeed
      |> chan.Reply
    | Left error ->
      error
      |> Either.fail
      |> chan.Reply

  // ** onClose

  /// ## OnClose
  ///
  /// Register a callback to be run when a browser as exited a session in an orderly fashion. The
  /// session is removed from the global state by appending a `RemoveSession`
  let private onClose (state: IrisState) (id: Id) (chan: ReplyChan) =
    match Map.tryFind id state.Store.State.Sessions with
    | Some session ->
      match appendCmd (RemoveSession session) with
      | Right entry ->
        entry
        |> Reply.Entry
        |> Either.succeed
        |> chan.Reply
      | Left error ->
        error
        |> Either.fail
        |> chan.Reply
    | _ ->
      Other "Session not found. Something spooky is going on"
      |> Either.fail
      |> chan.Reply

  // ** onError

  /// ## OnError
  ///
  /// Register a callback to be run if the client connection unexpectectly fails. In that case the
  /// Session is retrieved and removed from global state.
  let private onError (state: IrisState) (sessionid: Id) (chan: ReplyChan) =
    match Map.tryFind sessionid state.Store.State.Sessions with
      match appendCmd (RemoveSession session) with
      | Right entry ->
        entry
        |> Reply.Entry
        |> Either.succeed
        |> chan.Reply
      | Left error ->
        error
        |> Either.fail
        |> chan.Reply
    | _ ->
      Other "Session not found. Something spooky is going on"
      |> Either.fail
      |> chan.Reply


  // ** onMessage

  /// ## OnMessage
  ///
  /// Register a handler to process messages coming from the browser client. The current handling
  /// mechanism is that incoming message get appended to the `Raft` log immediately, and a log
  /// message is sent back to the client. Once the new command has been replicated throughout the
  /// system, it will be applied to the server-side global state, then pushed over the socket to
  /// be applied to all client-side global state atoms.
  let private onMessage (state: IrisState) (id: Id) (cmd: StateMachine) (chan: ReplyChan) =
    match appendCmd cmd with
    | Right entry ->
      entry
      |> Reply.Entry
      |> Either.succeed
      |> chan.Reply
    | Left error ->
      error
      |> Either.fail
      |> chan.Reply

  // ** handleSocket

  let private handleSocket (state: IrisState) (chan: ReplyChan) (ev: SocketEvent) =
    match ev with
    | OnOpen id       -> onOpen    state id    chan
    | OnClose id      -> onClose   state id    chan
    | OnMessage id,sm -> onMessage state id sm chan
    | OnError id,err  -> onError   state id sm chan
    state

  // ** onConfigured

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// ## OnConfigured
  ///
  /// Register a callback to run when a new cluster configuration has been committed, and the
  /// joint-consensus mode has been concluded.
  let private onConfigured (state: IrisState) (nodes: RaftNode array) (chan: ReplyChan) =
    nodes
    |> Array.map (Node.getId >> string)
    |> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
    |> Logger.debug nodeid tag

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onNodeAdded

  /// ## OnNodeAdded
  ///
  /// Register a callback to be run when the user has added a new node to the `Raft` cluster. This
  /// commences the joint-consensus mode until the new node has been caught up and is ready be a
  /// full member of the cluster.

  let private onNodeAdded (state: IrisState) (node: RaftNode) (chan: ReplyChan) =
    let cmd = AddNode node
    state.Store.Dispatch cmd
    broadcastMsg state cmd
    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onNodeUpdated

  /// ## OnNodeUpdated
  ///
  /// Register a callback to be called when a cluster node's properties such as e.g. its node
  /// state.

  let private onNodeUpdated (state: IrisState) (node: RaftNode) (chan: ReplyChan) =
    let cmd = UpdateNode node
    state.Store.Dispatch cmd
    broadcastMsg state cmd
    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onNodeRemoved

  /// ## OnNodeRemoved
  ///
  /// Register a callback to be run when a node was removed from the cluster, resulting into
  /// the cluster entering into joint-consensus mode until the node was successfully removed.

  let private onNodeRemoved (state: IrisState) (node: RaftNode) (chan: ReplyChan) =
    let cmd = RemoveNode node
    state.Store.Dispatch cmd
    broadcastMsg state cmd
    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onApplyLog

  /// ## onApplyLog
  ///
  /// Register a callback to be run when an appended entry is considered safely appended to a
  /// majority of servers logs. The entry then is regarded as applied.
  ///
  /// In this callback implementation we essentially do 3 things:
  ///
  ///   - the state machine command is applied to the store, potentially altering its state
  ///   - the state machine command is broadcast to all clients
  ///   - the state machine command is persisted to disk (potentially recorded in a git commit)

  let private onApplyLog (state: IrisState) (sm: StateMachine) =
    state.Store.Dispatch sm
    broadcastMsg state sm

    if state.RaftServer.IsLeader then
      match state.Project with
      | Some project ->
        match persistEntry project sm with
        | Right (info, commit, updated) ->
          { state with Project = Some updated }

          Reply.Ok
          |> Either.succeed
          |> chan.Reply

        | Left error ->
          error
          |> Either.fail
          |> chan.Reply

      | None ->
        "Unable to persist project (not in IrisState)"
        |> Other
        |> Either.fail
        |> chan.Reply
    else
      match updateRepo project with
      | Right () ->
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
      | Left error ->
        error
        |> Either.fail
        |> chan.Reply

  // ** onStateChanged

  let private onStateChanged (state: IrisState)
                             (oldstate: RaftState)
                             (newstate: RaftSTate)
                             (chan: ReplyChan) =
    match state.RaftServer with
    | Some server ->
      sprintf "Raft state changed from %A to %A" oldstate newstate
      |> Logger.debug server.NodeId tag
    | _ -> ()

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onCreateSnapshot

  let private onCreateSnapshot (state: IrisState) (chan: ReplyChan) =
    match state.RaftServer with
    | Some server ->
      "CreateSnapshot requested"
      |> Logger.debug server.NodeId tag
    | _ -> ()

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** handleRaftEvent

  let private handleRaftEvent (state: IrisState) (ev: RaftEvent) (chan: ReplyChan) =
    match ev with
    | ApplyLog sm        -> onApplyLog       state sm    chan
    | NodeAdded node     -> onNodeAdded      state node  chan
    | NodeRemoved node   -> onNodeRemoved    state node  chan
    | NodeUpdated node   -> onNodeUpdated    state node  chan
    | Configured nodes   -> onConfigured     state nodes chan
    | CreateSnapshot str -> onCreateSnapshot state       chan
    | StateChanged (oldstate, newstate) ->
      onStateChanged state oldstate newstate chan

  //   ____ _ _
  //  / ___(_) |_
  // | |  _| | __|
  // | |_| | | |_
  //  \____|_|\__|

  // ** loadProject

  let private loadProject (state: IrisState) (path: FilePath) =
    either {
      let! project = Project.load path

      match project.Path with
      | None ->
        return!
          ProjectPathError
          |> Either.fail

      | Some path ->
        // FIXME: load the actual state from disk

        let! raftserver = RaftServer.create project.Config
        let! wsserver   = SocketServer.create raftserver.Node
        let! gitserver  = GitServer.create raftserver.Node path

        return
          { state with
              Store        = Some store
              Project      = Some project
              GitServer    = Some gitserver
              RaftServer   = Some raftserver
              SocketServer = Some wsserver }
    }

  // ** startServer

  let inline private startServer< ^a when ^a : (member Start : unit -> Either<IrisError,unit>)>
                                (server: ^a option) :
                                Either<IrisError, unit> =
    match server with
    | Some srv -> (^a: (member Start: unit -> Either<IrisError,unit>) server)
    | None ->
      "Could not start server. No instance provided."
      |> Other
      |> Either.fail

  // ** handleLogEvent

  let private handleLogEvent (state: IrisState) (chan: ReplyChan) (log: LogEvent) =
    broadcastMsg (LogMsg log)
    Logger.stdout log

    Reply.Ok
    |> Either.succeed
    |> chan.Reply

  // ** forwardLogEvents

  let private forwardLogEvents (agent: IrisAgent) (log: LogEvent) =
    agent.PostAndReply(fun chan -> Msg.Log log, chan)
    |> ignore

  // ** forwardRaftEvents

  let private forwardRaftEvents (agent: IrisAgent) (ev: RaftEvent) =
    agent.PostAndReply(fun chan -> Msg.Raft ev, chan)
    |> ignore

  // ** forwardGitEvents

  let private forwardGitEvents (agent: IrisAgent) (ev: GitEvent) =
    agent.PostAndReply(fun chan -> Msg.Git ev, chan)
    |> ignore

  // ** forwardSocketEvents

  let private forwardSocketEvents (agent: IrisAgent) (ev: SocketEvent) =
    agent.PostAndReply(fun chan -> Msg.Socket ev, chan)
    |> ignore

  // ** start

  let private start (state: IrisState) (agent: IrisAgent) =
    let disposables =
      [ forwardLogEvents    agent |> Logger.subscribe
        forwardRaftEvents   agent |> raftserver.Subscribe
        forwardSocketEvents agent |> wsserver.Subscribe
        forwardGitEvents    agent |> gitserver.Subscribe ]

    match startServer state.SocketServer with
    | Right () ->
      match startServer state.GitServer with
      | Right () ->
        match startServer state.RaftServer with
        | Right () ->
          { state with
              Status = ServiceStatus.Running
              Disposable = disposables }
          |> Either.succeed
        | Left error ->
          disposeAll disposables
          Either.fail error
      | Left error ->
        disposeAll disposables
        Either.fail error
    | Left error ->
      disposeAll disposables
      Either.fail error

  // ** handleLoad

  let private handleLoad (state: IrisState)
                         (path: FilePath)
                         (chan: ReplyChannel)
                         (inbox: IrisAgent) =

    match loadProject path with
    | Right nextstate ->

      match start nextstate inbox with
      | Right finalstate ->
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
        finalstate

      | Left error ->
        error
        |> Either.fail
        |> chan.Reply
        { resetState state with
            Status = ServiceStatus.Failed error }

    | Left error ->
      error
      |> Either.fail
      |> chan.Reply
      { resetState state with
          Status = ServiceStatus.Failed error }

  // ** handleStatus

  let private handleStatus (state: IrisState) (chan: ReplyChan) =
    state.Status
    |> Reply.Status
    |> Either.succeed
    |> chan.Reply
    state

  // ** handleGit

  let private handleGit (state: IrisState) (chan: ReplyChan) =
    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** loop

  let private loop (initial: IrisState) (inbox: IrisAgent) =
    let act (state: IrisState) =
      async {
        let! (msg, chan) = inbox.Receive()
        match msg with
        | Msg.Load path -> handleLoad        state path chan inbox
        | Msg.Status    -> handleStatus      state chan
        | Msg.Git    ev -> handleGitEvent    state chan ev
        | Msg.Socket ev -> handleSocketEvent state chan ev
        | Msg.Raft   ev -> handleRaftEvent   state chan ev
        | Msg.Log   log -> handleLogEvent    state chan log
      }

    act initial

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    let create () =
      either {
          let httpserver  = new AssetServer(project.Config)

          let initial =
            { Status       = ServiceStatus.Starting
              Store        = new Store(State.Empty)
              Project      = None
              GitServer    = None
              RaftServer   = None
              SocketServer = None
              Dispoables   = [] }

          let agent = new IrisAgent(loop initial)

          return
            { new IIrisServer with
                member self.Start() =
                  try
                    agent.Start()
                    |> Either.succeed
                  with
                    | exn ->
                      exn.Message
                      |> Other
                      |> Either.fail

                member self.Dispose() =
                  dispose httpserver
              }
      }
