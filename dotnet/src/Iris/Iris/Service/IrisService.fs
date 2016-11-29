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
open FSharpx.Functional
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
    | Git    of GitEvent
    | Socket of SocketEvent
    | Raft   of RaftEvent
    | Log    of LogEvent
    | Load   of FilePath
    | Status
    | Unload

  // ** Reply

  [<RequireQualifiedAccess>]
  type private Reply =
    | Ok
    | Entry  of EntryResponse
    | Status of ServiceStatus

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
  type private IrisStateData =
    { Status       : ServiceStatus
      Store        : Store
      Project      : IrisProject
      GitServer    : IGitServer
      RaftServer   : IRaftServer
      HttpServer   : AssetServer
      SocketServer : IWebSocketServer
      Disposables  : IDisposable list }

    interface IDisposable with
      member self.Dispose() =
        disposeAll self.Disposables
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.SocketServer

  [<NoComparison;NoEquality>]
  type private IrisState =
    | Idle
    | Loaded of IrisStateData

    interface IDisposable with
      member self.Dispose() =
        match self with
        | Idle -> ()
        | Loaded data -> dispose data

  let private withState (state: IrisState) (cb: IrisStateData -> unit) =
    match state with
    | Idle -> ()
    | Loaded data -> cb data

  // ** resetState

  let private resetState (state: IrisState) =
    match state with
    | Idle -> Idle
    | Loaded data ->
      dispose data
      Idle

  // ** IIrisServer

  type IIrisServer =
    inherit IDisposable
    abstract Config : Either<IrisError,Config>
    abstract Status : Either<IrisError,ServiceStatus>
    abstract Start : unit -> Either<IrisError,unit>
    abstract Load : FilePath -> Either<IrisError,unit>

  // ** broadcastMsg

  let private broadcastMsg (state: IrisStateData) (cmd: StateMachine) =
    state.SocketServer.Broadcast cmd
    |> ignore

  // ** sendMsg

  let private sendMsg (state: IrisStateData) (id: Id) (cmd: StateMachine) =
    state.SocketServer.Send id cmd
    |> ignore

  // ** appendCmd

  let private appendCmd (state: IrisStateData) (cmd: StateMachine) =
    state.RaftServer.Append(cmd)

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

  let private onOpen (state: IrisState) (session: Id) (chan: ReplyChan) =
    withState state <| fun data ->
      sendMsg data session (DataSnapshot data.Store.State)

    // FIXME: need to check this bit for proper session handling
    Reply.Ok
    |> Either.succeed
    |> chan.Reply

    // match appendCmd state (AddSession session) with
    // | Right entry ->
    //   entry
    //   |> Reply.Entry
    //   |> Either.succeed
    //   |> chan.Reply
    // | Left error ->
    //   error
    //   |> Either.fail
    //   |> chan.Reply

  // ** onClose

  /// ## OnClose
  ///
  /// Register a callback to be run when a browser as exited a session in an orderly fashion. The
  /// session is removed from the global state by appending a `RemoveSession`
  let private onClose (state: IrisState) (id: Id) (chan: ReplyChan) =
    withState state <| fun data ->
      match Map.tryFind id data.Store.State.Sessions with
      | Some session ->
        match appendCmd data (RemoveSession session) with
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
  let private onError (state: IrisState) (sessionid: Id) (err: Exception) (chan: ReplyChan) =
    withState state <| fun data ->
      match Map.tryFind sessionid data.Store.State.Sessions with
      | Some session ->
        match appendCmd data (RemoveSession session) with
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
    withState state <| fun data ->
      match appendCmd data cmd with
      | Right entry ->
        entry
        |> Reply.Entry
        |> Either.succeed
        |> chan.Reply
      | Left error ->
        error
        |> Either.fail
        |> chan.Reply

  // ** handleSocketEvent

  let private handleSocketEvent (state: IrisState) (ev: SocketEvent) (chan: ReplyChan) =
    match ev with
    | OnOpen id         -> onOpen    state id     chan
    | OnClose id        -> onClose   state id     chan
    | OnMessage (id,sm) -> onMessage state id sm  chan
    | OnError (id,err)  -> onError   state id err chan
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
    withState state <| fun data ->
      nodes
      |> Array.map (Node.getId >> string)
      |> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      |> Logger.debug data.RaftServer.NodeId tag

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
    withState state <| fun data ->
      let cmd = AddNode node
      data.Store.Dispatch cmd
      broadcastMsg data cmd

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
    withState state <| fun data ->
      let cmd = UpdateNode node
      data.Store.Dispatch cmd
      broadcastMsg data cmd

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
    withState state <| fun data ->
      let cmd = RemoveNode node
      data.Store.Dispatch cmd
      broadcastMsg data cmd

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

  let private onApplyLog (state: IrisState) (sm: StateMachine) (chan: ReplyChan) =
    match state with
    | Idle ->
      Other "No project loaded"
      |> Either.fail
      |> chan.Reply
      state
    | Loaded data ->
      data.Store.Dispatch sm
      broadcastMsg data sm

      if RaftServer.isLeader data.RaftServer then
        match persistEntry data.Project sm with
        | Right (info, commit, updated) ->
          Reply.Ok
          |> Either.succeed
          |> chan.Reply
          Loaded { data with Project = updated }

        | Left error ->
          error
          |> Either.fail
          |> chan.Reply
          Loaded data
      else
        match data.RaftServer.State with
        | Right state ->
          let node =
            state.Raft
            |> Raft.currentLeader
            |> Option.bind (flip Raft.getNode state.Raft)

          match node with
          | Some leader ->
            match updateRepo data.Project leader with
            | Right () ->
              Reply.Ok
              |> Either.succeed
              |> chan.Reply
            | Left error ->
              error
              |> Either.fail
              |> chan.Reply
          | None ->
            Reply.Ok
            |> Either.succeed
            |> chan.Reply
        | Left error ->
          error
          |> Either.fail
          |> chan.Reply
        Loaded data

  // ** onStateChanged

  let private onStateChanged (state: IrisState)
                             (oldstate: RaftState)
                             (newstate: RaftState)
                             (chan: ReplyChan) =
    match state with
    | Loaded data ->
      sprintf "Raft state changed from %A to %A" oldstate newstate
      |> Logger.debug data.RaftServer.NodeId tag
    | _ -> ()

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  // ** onCreateSnapshot

  let private onCreateSnapshot (state: IrisState) (chan: ReplyChan) =
    match state with
    | Loaded data ->
      "CreateSnapshot requested"
      |> Logger.debug data.RaftServer.NodeId tag
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

  // ** handleGitEvent

  let private handleGitEvent (state: IrisState) (ev: GitEvent) (chan: ReplyChan) =
    match state with
    | Loaded data ->
      match ev with
      | Started pid ->
        "Git daemon started"
        |> Logger.debug data.RaftServer.NodeId tag

      | Exited pid ->
        "Git daemon exited"
        |> Logger.debug data.RaftServer.NodeId tag

      | Pull (_, addr, port) ->
        sprintf "Client %s:%d pulled updates from me" addr port
        |> Logger.debug data.RaftServer.NodeId tag
    | _ -> ()

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  // ** loadProject

  let private loadProject (state: IrisState) (path: FilePath) =
    either {
      dispose state

      let! project = Project.load path

      match project.Path with
      | None ->
        return!
          ProjectPathError
          |> Either.fail

      | Some path ->
        // FIXME: load the actual state from disk

        let  httpserver = new AssetServer(project.Config)
        let! raftserver = RaftServer.create project.Config
        let! wsserver   = SocketServer.create raftserver.Node
        let! gitserver  = GitServer.create raftserver.Node path

        return
          Loaded { Status       = ServiceStatus.Starting
                   Store        = new Store(State.Empty)
                   Project      = project
                   GitServer    = gitserver
                   RaftServer   = raftserver
                   HttpServer   = httpserver
                   SocketServer = wsserver
                   Disposables  = [] }
    }

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
    match state with
    | Idle -> Either.succeed Idle
    | Loaded data ->
      let disposables =
        [ forwardLogEvents    agent |> Logger.subscribe
          forwardRaftEvents   agent |> data.RaftServer.Subscribe
          forwardSocketEvents agent |> data.SocketServer.Subscribe
          forwardGitEvents    agent |> data.GitServer.Subscribe ]

      let result1 = data.SocketServer.Start()
      let result2 = data.GitServer.Start()
      let result3 = data.RaftServer.Start()
      let result4 = data.HttpServer.Start()

      match result1, result2, result3, result4 with
      | Right _, Right _, Right _, Right _ ->
        Loaded { data with
                   Status = ServiceStatus.Running
                   Disposables = disposables }
        |> Either.succeed
      | other ->
        disposeAll disposables
        dispose data.SocketServer
        dispose data.RaftServer
        dispose data.GitServer
        Other "Could not start servers."
        |> Either.fail

  // ** handleLoad

  let private handleLoad (state: IrisState)
                         (path: FilePath)
                         (chan: ReplyChan)
                         (inbox: IrisAgent) =
    match loadProject state path with
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
        Idle

    | Left error ->
      error
      |> Either.fail
      |> chan.Reply
      Idle

  //  _
  // | |    ___   __ _
  // | |   / _ \ / _` |
  // | |__| (_) | (_| |
  // |_____\___/ \__, |
  //             |___/

  // ** handleLogEvent

  let private handleLogEvent (state: IrisState) (log: LogEvent) (chan: ReplyChan) =
    withState state <| fun data ->
      broadcastMsg data (LogMsg log)
      Logger.stdout log

    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    state

  //  ____  _        _
  // / ___|| |_ __ _| |_ _   _ ___
  // \___ \| __/ _` | __| | | / __|
  //  ___) | || (_| | |_| |_| \__ \
  // |____/ \__\__,_|\__|\__,_|___/

  // ** handleStatus

  let private handleStatus (state: IrisState) (chan: ReplyChan) =
    match state with
    | Idle -> Idle
    | Loaded data ->
      data.Status
      |> Reply.Status
      |> Either.succeed
      |> chan.Reply
      state

  // ** handleUnload

  let private handleUnload (state: IrisState) (chan: ReplyChan) =
    dispose state
    Reply.Ok
    |> Either.succeed
    |> chan.Reply
    Idle

  // ** loop

  let private loop (initial: IrisState) (inbox: IrisAgent) =
    let rec act (state: IrisState) =
      async {
        let! (msg, chan) = inbox.Receive()
        let newstate =
          match msg with
          | Msg.Load path -> handleLoad        state path chan inbox
          | Msg.Status    -> handleStatus      state      chan
          | Msg.Git    ev -> handleGitEvent    state ev   chan
          | Msg.Socket ev -> handleSocketEvent state ev   chan
          | Msg.Raft   ev -> handleRaftEvent   state ev   chan
          | Msg.Log   log -> handleLogEvent    state log  chan
          | Msg.Unload    -> handleUnload      state      chan
        return! act newstate
      }

    act initial

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    let create () =
      let agent = new IrisAgent(loop Idle)

      Either.succeed
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

            member self.Status
              with get () =
                match agent.PostAndReply(fun chan -> Msg.Status,chan) with
                | Right (Reply.Status status) -> Right status
                | Right other -> Left (Other "Unexpected response")
                | Left error -> Left error

            member self.Load(path: FilePath) =
              match agent.PostAndReply(fun chan -> Msg.Load path,chan) with
              | Right Reply.Ok -> Right ()
              | Right other -> Left (Other "Unexpectted response")
              | Left error -> Left error

            member self.Dispose() =
              agent.PostAndReply(fun chan -> Msg.Unload, chan)
              |> ignore
              dispose agent
          }
