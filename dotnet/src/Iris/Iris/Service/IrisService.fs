namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open System.IO
open System.Collections.Concurrent
open Iris.Raft
open Iris.Zmq
open Iris.Core
open Iris.Core.Utils
open Iris.Core.Commands
open Iris.Service.Interfaces
open Iris.Service.Persistence
open Iris.Service.Git
open Iris.Service.WebSockets
open Iris.Service.Raft
open Iris.Service.Http
open Microsoft.FSharp.Control
open FSharpx.Functional
open LibGit2Sharp
open Hopac
open Hopac.Infixes

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

module Iris =

  // ** tag

  let private tag (str: string) = sprintf "IrisServer.%s" str

  // ** keys

  [<Literal>]
  let private API_SERVER = "api"

  [<Literal>]
  let private GIT_SERVER = "git"

  [<Literal>]
  let private LOG_HANDLER = "log"

  [<Literal>]
  let private RAFT_SERVER = "raft"

  [<Literal>]
  let private WS_SERVER = "ws"

  let private signature =
    new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.UtcNow))

  // ** Subscriptions

  /// ## Subscriptions
  ///
  /// Type alias for IObserver subscriptions.
  ///
  type Subscriptions = ConcurrentDictionary<Guid,IObserver<IrisEvent>>

  // ** disposeAll

  /// ## disposeAll
  ///
  /// Dispose all resource in the passed `seq`.
  ///
  /// ### Signature:
  /// - disposables: IDisposable seq
  ///
  /// Returns: unit
  let private disposeAll (disposables: Map<string,IDisposable>) =
    Map.iter (konst dispose) disposables

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: IClient }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Socket

  // ** IrisStateData

  // ** IrisIdleStateData

  /// ## IrisIdleStateData
  ///
  /// Services that keep running also in idle state.
  ///
  [<NoComparison;NoEquality>]
  type private IrisIdleStateData =
    { DiscoveryService : IDiscoveryService }

    interface IDisposable with
      member self.Dispose() =
        ()

  // ** IrisLoadedStateData

  /// ## IrisLoadedStateData
  ///
  /// Encapsulate all service-internal state to hydrate an `IrisAgent` with a project loaded.
  /// As the actor receives messages, it uses (and updates) this record and passes it on.
  /// For ease of use it implements the IDisposable interface.
  ///
  /// ### Fields:
  /// - MemberId: ServiceStatus of currently loaded project
  /// - Status: ServiceStatus of currently loaded project
  /// - Store: Store containing all state. This is sent to user via WebSockets on connection.
  /// - Project: IrisProject currently loaded
  /// - GitServer: IGitServer for current project
  /// - RaftServer: IRaftServer for current project
  /// - SocketServer: IWebSocketServer for current project
  /// - Disposables: IDisposable list for Observables and the like
  ///
  [<NoComparison;NoEquality>]
  type private IrisLoadedStateData =
    { MemberId      : Id
      Status        : ServiceStatus
      Store         : Store
      Leader        : Leader option
      ApiServer     : IApiServer
      GitServer     : IGitServer
      RaftServer    : IRaftServer
      SocketServer  : IWebSocketServer
      Subscriptions : Subscriptions
      Disposables   : Map<string,IDisposable> }

    interface IDisposable with
      member self.Dispose() =
        disposeAll self.Disposables
        dispose self.ApiServer
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.SocketServer

  // ** IrisState

  /// ## IrisState
  ///
  /// Encodes the presence or absence of a loaded project. Implements IDisposable for
  /// convenience. This is the type our inner loop function is fed with.
  ///
  /// ### Constructors:
  /// - Idle: no IrisProject is currently loaded (implies ServiceStatus.Stopped)
  /// - Loaded: IrisStateData for loaded IrisProject
  ///
  [<NoComparison;NoEquality>]
  type private IrisState =
    | Idle of IrisIdleStateData
    | Loaded of IrisIdleStateData * IrisLoadedStateData

    interface IDisposable with
      member self.Dispose() =
        match self with
        | Idle data -> dispose data
        | Loaded (data1,data2) -> dispose data1; dispose data2

  // ** Reply

  /// ## Reply
  ///
  /// Type to model synchronous replies from the internal actor.
  ///
  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | State  of IrisLoadedStateData
    | Entry  of EntryResponse
    | Config of IrisConfig

  // ** ReplyChan

  /// ## ReplyChan
  ///
  /// Type alias over reply channel for computations on the internal actor that can fail.
  ///
  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  /// ## Msg
  ///
  /// Model the actor-internal state machine. Some constructors include a `ReplyChan` for
  /// synchronous request/response style computations.
  ///
  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Git         of GitEvent
    | Socket      of SocketEvent
    | Raft        of RaftEvent
    | Api         of ApiEvent
    | Log         of LogEvent
    | Discovery   of Discovery.DiscoveryEvent
    | Load        of ReplyChan * projectName:string * userName:string * password:string * site:string option
    | SetConfig   of ReplyChan * IrisConfig
    | AddMember   of ReplyChan * RaftMember
    | RmMember    of ReplyChan * Id
    | Join        of ReplyChan * IpAddress  * uint16
    | Leave       of ReplyChan
    | Config      of ReplyChan
    | Unload      of ReplyChan
    | State       of ReplyChan
    | ForceElection
    | Periodic

  // ** IrisAgent

  /// ## IrisAgent
  ///
  /// Type alias for internal state mutation actor.
  ///
  type private IrisAgent = MailboxProcessor<Msg>

  // ** postCommand

  let inline private postCommand (agent: IrisAgent) (loc: string) (cb: ReplyChan -> Msg) =
    async {
      let! result = agent.PostAndTryAsyncReply(cb, Constants.COMMAND_TIMEOUT)
      match result with
      | Some response -> return response
      | None ->
        return
          "Command Timeout"
          |> Error.asOther (loc |> sprintf "postCommand.%s" |> tag)
          |> Either.fail
    }
    |> Async.RunSynchronously

  /// ## withLoaded
  ///
  /// Reach into passed IrisState value and apply either one of the passed functions to the inner
  /// value.
  ///
  /// ### Signature:
  /// - state: IrisState value to reach into
  /// - idle: (unit -> IrisState) function evaluated when the Idle case was hit
  /// - arg: (IrisStateData -> IrisState) function evaluated when Loaded constructor was encoutered.
  ///
  /// Returns: IrisState
  let inline private withLoaded (state: IrisState)
                                (idle: unit -> 'a)
                                (loaded: IrisLoadedStateData -> 'a) =
    match state with
    | Idle _ -> idle ()
    | Loaded (_,data2) -> loaded data2

  /// ## withState
  ///
  /// If the passed `IrisState` is a loaded project, execute the supplied function against it.
  ///
  /// ### Signature:
  /// - state: IrisState value to check
  /// - cb: IrisStateData -> unit workload
  ///
  /// Returns: unit
  let private withState (state: IrisState) (loaded: IrisLoadedStateData -> unit) =
    withLoaded state (konst state) (loaded >> konst state)
    |> ignore

  /// ## notLoaded
  ///
  /// Reply with the most common error.
  ///
  /// ### Signature:
  /// - chan: ReplyChan to reply with
  ///
  /// Returns: unit
  let private notLoaded (chan: ReplyChan) () =
    asynchronously <| fun _ ->
      "No project loaded"
      |> Error.asProjectError (tag "notLoaded")
      |> Either.fail
      |> chan.Reply

  // ** withDefaultReply

  let private withDefaultReply (state: IrisState)
                               (chan: ReplyChan)
                               (loaded: IrisLoadedStateData -> IrisState) =
    withLoaded state (notLoaded chan >> konst state) loaded

  // ** withoutReply

  let private withoutReply (state: IrisState)
                           (loaded: IrisLoadedStateData -> IrisState) =
    withLoaded state (konst state) loaded

  // ** resetLoaded

  /// ## resetLoaded
  ///
  /// Dispose the passed `IrisLoadedState` and return `Idle`
  ///
  /// ### Signature:
  /// - state: IrisState to dispose
  ///
  /// Returns: IrisState
  let private resetLoaded (state: IrisState) =
    match state with
    | Idle _ -> state
    | Loaded (data1,data2) -> dispose data2; Idle data1

  let private updateLoaded (state: IrisState) (data: IrisLoadedStateData) =
    match state with
    | Idle _ -> state
    | Loaded (idleData,_) -> Loaded (idleData, data)


  // ** IIrisServer

  // ** triggerOnNext

  let private triggerOnNext (subscriptions: Subscriptions) (ev: IrisEvent) =
    for subscription in subscriptions.Values do
      subscription.OnNext ev

  // ** triggerWithLoaded

  let private triggerWithLoaded (state: IrisState) (ev: IrisEvent) =
    match state with
    | Loaded(_,data) -> triggerOnNext data.Subscriptions ev
    | _ -> ()

  // ** broadcastMsg

  let private broadcastMsg (state: IrisLoadedStateData) (cmd: StateMachine) =
    Tracing.trace "IrisService.broadcastMsg" <| fun () ->
      cmd
      |> state.SocketServer.Broadcast
      |> ignore

  // ** sendMsg

  let private sendMsg (state: IrisLoadedStateData) (id: Id) (cmd: StateMachine) =
    Tracing.trace "IrisService.sendMsg" <| fun () ->
      cmd
      |> state.SocketServer.Send id
      |> ignore

  // ** appendCmd

  let private appendCmd (state: IrisLoadedStateData) (cmd: StateMachine) =
    Tracing.trace "IrisService.appendCmd" <| fun () ->
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

  let private onOpen (state: IrisState) (session: Id) =
    withState state <| fun data ->
      sendMsg data session (DataSnapshot data.Store.State)

    // FIXME: need to check this bit for proper session handling
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
  let private onClose (state: IrisState) (id: Id) =
    withState state <| fun data ->
      match Map.tryFind id data.Store.State.Sessions with
      | Some session ->
        match appendCmd data (RemoveSession session) with
        | Right _ -> ()
        | Left error  ->
          error
          |> string
          |> Logger.err (tag "onClose")
      | _ -> ()

  // ** onError

  /// ## OnError
  ///
  /// Register a callback to be run if the client connection unexpectectly fails. In that case the
  /// Session is retrieved and removed from global state.
  let private onError (state: IrisState) (sessionid: Id) (err: Exception) =
    withState state <| fun data ->
      match Map.tryFind sessionid data.Store.State.Sessions with
      | Some session ->
        match appendCmd data (RemoveSession session) with
        | Right _ ->
          err.Message
          |> Logger.debug (tag "onError")
        | Left error ->
          error
          |> string
          |> Logger.err (tag "onError")
      | _ -> ()


  // ** onMessage

  /// ## OnMessage
  ///
  /// Register a handler to process messages coming from the browser client. The current handling
  /// mechanism is that incoming message get appended to the `Raft` log immediately, and a log
  /// message is sent back to the client. Once the new command has been replicated throughout the
  /// system, it will be applied to the server-side global state, then pushed over the socket to
  /// be applied to all client-side global state atoms.
  let private onMessage (state: IrisState) (id: Id) (cmd: StateMachine) =
    withState state <| fun data ->
      match cmd with
      // If its something that appeared via the fast-lane, dispatch it on the Store and via the
      // WebSockets right away. Evertything else needs to be logged via raft.
      | UpdateSlices _ | CallCue _ ->
        data.ApiServer.Update cmd
        data.Store.Dispatch cmd
        broadcastMsg data cmd
      | cmd ->
        match cmd with
        | AddSession session ->
          data.SocketServer.BuildSession id session
          |> Either.map AddSession
        | cmd -> Either.succeed cmd
        |> Either.bind (appendCmd data)
        |> function
          | Right _ -> ()
          | Left error ->
            error
            |> string
            |> Logger.err (tag "onMessage")

  // ** handleSocketEvent

  let private handleSocketEvent (state: IrisState) (ev: SocketEvent) =
    match ev with
    | OnOpen id         -> onOpen    state id
    | OnClose id        -> onClose   state id
    | OnMessage (id,sm) -> onMessage state id sm
    | OnError (id,err)  -> onError   state id err
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
  let private onConfigured (state: IrisState) (mems: RaftMember array) =
    withoutReply state <| fun data ->
      either {
        let! memid = data.RaftServer.MemberId
        mems
        |> Array.map (Member.getId >> string)
        |> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
        |> Logger.debug (tag "onConfigured")
      }
      |> konst state

  // ** onMemberAdded

  /// ## OnMemberAdded
  ///
  /// Register a callback to be run when the user has added a new mem to the `Raft` cluster. This
  /// commences the joint-consensus mode until the new mem has been caught up and is ready be a
  /// full member of the cluster.

  let private onMemberAdded (state: IrisState) (mem: RaftMember) =
    withoutReply state <| fun data ->
      let cmd = AddMember mem
      data.Store.Dispatch cmd
      broadcastMsg data cmd
      state

  // ** onMemberUpdated

  /// ## OnMemberUpdated
  ///
  /// Register a callback to be called when a cluster mem's properties such as e.g. its mem
  /// state.

  let private onMemberUpdated (state: IrisState) (mem: RaftMember) =
    withoutReply state <| fun data ->
      let cmd = UpdateMember mem
      data.Store.Dispatch cmd
      broadcastMsg data cmd
      state

  // ** onMemberRemoved

  /// ## OnMemberRemoved
  ///
  /// Register a callback to be run when a mem was removed from the cluster, resulting into
  /// the cluster entering into joint-consensus mode until the mem was successfully removed.

  let private onMemberRemoved (state: IrisState) (mem: RaftMember) =
    withoutReply state <| fun data ->
      let cmd = RemoveMember mem
      data.Store.Dispatch cmd
      broadcastMsg data cmd
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
    withoutReply state <| fun data ->
      data.Store.Dispatch sm
      broadcastMsg data sm
      data.ApiServer.Update sm

      if data.RaftServer.IsLeader then
        match persistEntry data.Store.State sm with
        | Right commit ->
          sprintf "Persisted command in commit: %s" commit.Sha
          |> Logger.debug (tag "onApplyLog")
          state
        | Left error ->
          sprintf "Error persisting command: %A" error
          |> Logger.err (tag "onApplyLog")
          state
      else
        match data.RaftServer.State with
        | Right state ->
          let mem =
            state.Raft
            |> Raft.currentLeader
            |> Option.bind (flip Raft.getMember state.Raft)

          match mem with
          | Some leader ->
            match updateRepo data.Store.State.Project leader with
            | Right () -> ()
            | Left error ->
              error
              |> string
              |> Logger.err (tag "onApplyLog")
          | None -> ()
        | Left error ->
          error
          |> string
          |> Logger.err (tag "onApplyLog")
        state

  // ** mkLeader

  let private mkLeader (self: Id) (leader: RaftMember) =
    let addr = memUri leader
    let socket = Client.create self addr // Constants.REQ_TIMEOUT)
    { Member = leader; Socket = socket }

  // ** onStateChanged

  let private onStateChanged (state: IrisState)
                             (oldstate: RaftState)
                             (newstate: RaftState) =
    withoutReply state <| fun data ->
      sprintf "Raft state changed from %A to %A" oldstate newstate
      |> Logger.debug (tag "onStateChanged")
      // create redirect socket
      match oldstate, newstate with
      | _, Follower ->
        Option.iter dispose data.Leader
        match data.RaftServer.Leader with
        | Right (Some leader) ->
          { data with Leader = Some (mkLeader data.MemberId leader) }
          |> updateLoaded state
        | Right None ->
          "Could not start re-direct socket: no leader"
          |> Logger.debug (tag "onStateChanged")
          state
        | Left error ->
          string error
          |> Logger.err (tag "onStateChanged")
          state
      | _, Leader ->
        Option.iter dispose data.Leader
        { data with Leader = None }
        |> updateLoaded state
      | _ -> state

  // ** onCreateSnapshot

  let private onCreateSnapshot (state: IrisState) (ch: Ch<State option>) =
    let response =
      match state with
      | Idle _ -> None
      | Loaded (_, data) -> Some data.Store.State

    job {
      do! Ch.send ch response
    } |> Hopac.queue

    state

  // ** onRetrieveSnapshot

  let private onRetrieveSnapshot (state: IrisState) (ch: Ch<RaftLogEntry option>) =
    job {
      do! Ch.send ch None
    } |> Hopac.queue

    state

  // ** onPersistSnapshot

  let private onPersistSnapshot (state: IrisState) (log: RaftLogEntry) =
    "OOOOO WEEEEE"
    |> Logger.err (tag "onPersistSnapshot")
    state

  // ** requestAppend

  let private requestAppend (self: MemberId) (leader: Leader) (sm: StateMachine) =
    let max = 5

    let rec impl (current: Leader) (count: int) =
      let result : Either<IrisError,RaftResponse> =
        AppendEntry sm
        |> Binary.encode
        |> current.Socket.Request
        |> Either.bind Binary.decode

      match result with
      | Right (AppendEntryResponse _) ->
        Either.succeed leader
      | Right (Redirect mem) ->
        if count < max then
          dispose leader
          let newleader = mkLeader self mem
          impl newleader (count + 1)
        else
          max
          |> sprintf "Maximum re-direct count reached (%d). Appending failed"
          |> Error.asRaftError (tag "requestAppend")
          |> Either.fail
      | Right other ->
        other
        |> sprintf "Received unexpected response from server: %A"
        |> Error.asRaftError (tag "requestAppend")
        |> Either.fail
      | Left error ->
        Either.fail error

    Tracing.trace "IrisService.requestAppend" <| fun () ->
      impl leader 0

  // ** forwardCommand

  let private forwardCommand (data: IrisLoadedStateData) (sm: StateMachine) =
    Tracing.trace "IrisService.forwardCommand" <| fun () ->
      match data.Leader with
      | Some leader ->
        match requestAppend data.MemberId leader sm with
        | Right newleader ->
          { data with Leader = Some newleader }
        | Left error ->
          dispose leader
          { data with Leader = None }
      | None ->
        match data.RaftServer.Leader with
        | Right (Some mem) ->
          let leader = mkLeader data.MemberId mem
          match requestAppend data.MemberId leader sm with
          | Right newleader ->
            { data with Leader = Some newleader }
          | Left error ->
            dispose leader
            { data with Leader = None }
        | Right None ->
          "Could not start re-direct socket: No Known Leader"
          |> Logger.debug (tag "onStateChanged")
          data
        | Left error ->
          string error
          |> Logger.err (tag "onStateChanged")
          data

  // ** handleRaftEvent

  let private handleRaftEvent (state: IrisState) (ev: RaftEvent) =
    Tracing.trace "IrisService.handleRaftEvent" <| fun () ->
      match ev with
      | ApplyLog sm             -> onApplyLog         state sm
      | MemberAdded mem         -> onMemberAdded      state mem
      | MemberRemoved mem       -> onMemberRemoved    state mem
      | MemberUpdated mem       -> onMemberUpdated    state mem
      | Configured mems         -> onConfigured       state mems
      | CreateSnapshot ch       -> onCreateSnapshot   state ch
      | RetrieveSnapshot ch     -> onRetrieveSnapshot state ch
      | PersistSnapshot log     -> onPersistSnapshot  state log
      | StateChanged (ost, nst) -> onStateChanged     state ost nst

  // ** handleApiEvent

  let private handleApiEvent (state: IrisState) (ev: ApiEvent) =
    withoutReply state <| fun data ->
      Tracing.trace "IrisService.handleApiEvent" <| fun () ->
        match ev with
        | ApiEvent.Update sm ->
          // ApiEvents:
          // If its something that appeared via the fast-lane, dispatch it on the Store and via the
          // WebSockets right away. Evertything else needs to be logged via raft.
          match sm with
          | UpdateSlices _ | CallCue _ ->
            data.Store.Dispatch sm
            data.SocketServer.Broadcast sm
            |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
            |> ignore
            state
          | other ->
            if data.RaftServer.IsLeader then
              data.RaftServer.Append other
              |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
              |> ignore
              state
            else
              forwardCommand data other
              |> updateLoaded state
        | ApiEvent.Register client ->
          data.RaftServer.Append (AddClient client)
          |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
          |> ignore
          state
        | ApiEvent.UnRegister client ->
          data.RaftServer.Append (RemoveClient client)
          |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
          |> ignore
          state
        | _ -> // Status events
          triggerOnNext data.Subscriptions (IrisEvent.Api ev)
          state

  let private handleDiscoveryEvent (state: IrisState) (ev: Discovery.DiscoveryEvent) =
    let appendCommand data cmd =
      match appendCmd data cmd with
      | Right _ -> ()
      | Left error  ->
        error
        |> string
        |> Logger.err (tag "handleDiscoveryEvent")

    withoutReply state <| fun data ->
      Tracing.trace "IrisService.handleDiscoveryEvent" <| fun () ->
        match ev with
        | Discovery.Appeared service ->
          AddResolvedService service |> appendCommand data
        | Discovery.Updated  service ->
          UpdateResolvedService service |> appendCommand data
        | Discovery.Vanished service ->
          RemoveResolvedService service |> appendCommand data
        | _ -> ()
        state

  // ** forwardLogEvents

  let private forwardLogEvents (agent: IrisAgent) (log: LogEvent) =
    log |> Msg.Log |> agent.Post

  // ** forwardRaftEvents

  let private forwardRaftEvents (agent: IrisAgent) (ev: RaftEvent) =
    ev |> Msg.Raft |> agent.Post

  // ** forwardGitEvents

  let private forwardGitEvents (agent: IrisAgent) (ev: GitEvent) =
    ev |> Msg.Git |> agent.Post

  // ** forwardSocketEvents

  let private forwardSocketEvents (agent: IrisAgent) (ev: SocketEvent) =
    ev |> Msg.Socket |> agent.Post

  // ** forwardApiEvents

  let private forwardApiEvents (agent: IrisAgent) (ev: ApiEvent) =
    ev |> Msg.Api |> agent.Post

  //   ____ _ _
  //  / ___(_) |_
  // | |  _| | __|
  // | |_| | | |_
  //  \____|_|\__|

  // ** restartGitServer

  let private restartGitServer (data: IrisLoadedStateData) (agent: IrisAgent) =
    Tracing.trace "IrisService.restartGitServer" <| fun () ->
      data.Disposables
      |> Map.tryFind GIT_SERVER
      |> Option.map dispose
      |> ignore

      dispose data.GitServer

      let result =
        either {
          let! mem = data.RaftServer.Member
          let! gitserver = GitServer.create mem data.Store.State.Project.Path
          let disposable =
            forwardGitEvents agent
            |> gitserver.Subscribe
          match gitserver.Start() with
          | Right () ->
            return { data with
                      GitServer = gitserver
                      Disposables = Map.add GIT_SERVER disposable data.Disposables }
          | Left error ->
            dispose disposable
            dispose gitserver
            return! Either.fail error
        }

      match result with
      | Right newdata -> newdata
      | Left error ->
        error
        |> string
        |> Logger.err (tag "restartGitServer")
        data

  // ** handleGitEvent

  let private handleGitEvent (state: IrisState) (agent: IrisAgent) (ev: GitEvent) =
    Tracing.trace "IrisService.handleGitEvent" <| fun () ->
      match state with
      | Idle _ -> state
      | Loaded (idleData, data) ->
        triggerOnNext data.Subscriptions (IrisEvent.Git ev)
        match ev with
        | Started pid ->
          sprintf "Git daemon started with PID: %d" pid
          |> Logger.debug (tag "handleGitEvent")
          state

        | Exited _ ->
          "Git daemon exited. Attempting to restart."
          |> Logger.debug (tag "handleGitEvent")
          let newData = restartGitServer data agent
          Loaded (idleData, newData)

        | Pull (_, addr, port) ->
          sprintf "Client %s:%d pulled updates from me" addr port
          |> Logger.debug (tag "handleGitEvent")
          state

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  // ** loadProject

  let private loadProject (oldState: IrisState)
                          (machine: IrisMachine)
                          (projectName: string, userName: string, password: string, site: string option)
                          (subscriptions: Subscriptions) =
    let isValidPassword (user: User) (password: string) =
      let password = Crypto.hashPassword password user.Salt
      password = user.Password

    either {
      let! path = Project.checkPath machine projectName
      let! (state: State) = Asset.loadWithMachine path machine

      let user =
        state.Users
        |> Map.tryPick (fun _ u -> if u.UserName = userName then Some u else None)

      match user with
      | Some user when isValidPassword user password ->
        let idleData =
          match oldState with
          | Idle idleData -> idleData
          | Loaded (idleData, loadedData) -> dispose loadedData; idleData

        let! state =
          match site with
          | Some site ->
            let site =
              state.Project.Config.Sites
              |> Array.tryFind (fun s -> s.Name = site)
              |> function Some s -> s | None -> { ClusterConfig.Default with Name = site }

            // Add current machine if necessary
            let site =
              let mid = machine.MachineId
              if Map.containsKey mid site.Members
              then Right site
              else
                // Another cluster must cointain the self member, search for it
                state.Project.Config.Sites
                |> Seq.collect (fun site -> site.Members)
                |> Seq.tryFind (fun kvp -> kvp.Key = mid)
                |> function
                  | Some kvp -> Right { site with Members = Map.add mid kvp.Value site.Members }
                  | None -> Error.asProjectError "" "" |> Left

            match site with
            | Right site ->
              let cfg = state.Project.Config |> Config.addSiteAndSetActive site
              Right { state with Project = { state.Project with Config = cfg }}
            | Left err -> Left err
          | None -> Right state

        // FIXME: load the actual state from disk
        let! mem = Config.selfMember state.Project.Config

        let! raftserver = RaftServer.create ()
        let! wsserver   = SocketServer.create mem
        let! apiserver  = ApiServer.create mem state.Project.Id
        let! gitserver  = GitServer.create mem path

        // Try to put discovered services into the state
        let state =
          match idleData.DiscoveryService.Services with
          | Right (_, resolvedServices) -> { state with DiscoveredServices = resolvedServices }
          | Left err ->
            string err |> Logger.err (tag "loadProject.getDiscoveredServices")
            state

        let loadedData =
          { MemberId      = mem.Id
          ; Leader        = None
          ; Status        = ServiceStatus.Starting
          ; Store         = new Store(state)
          ; ApiServer     = apiserver
          ; GitServer     = gitserver
          ; RaftServer    = raftserver
          ; SocketServer  = wsserver
          ; Subscriptions = subscriptions
          ; Disposables   = Map.empty }

        return Loaded(idleData, loadedData)
      | _ ->
        return!
          "Login rejected"
          |> Error.asProjectError (tag "loadProject")
          |> Either.fail
    }

  // ** start

  let private start (state: IrisState) (agent: IrisAgent) =
    Tracing.trace "IrisService.start" <| fun () ->
      match state with
      | Idle _ -> Right state
      | Loaded(idleData, data) ->
        let disposables =
          [ (LOG_HANDLER, forwardLogEvents    agent |> Logger.subscribe)
            (RAFT_SERVER, forwardRaftEvents   agent |> data.RaftServer.Subscribe)
            (WS_SERVER,   forwardSocketEvents agent |> data.SocketServer.Subscribe)
            (API_SERVER,  forwardApiEvents    agent |> data.ApiServer.Subscribe)
            (GIT_SERVER,  forwardGitEvents    agent |> data.GitServer.Subscribe) ]
          |> Map.ofList

        let result =
          either {
            do! data.RaftServer.Load(data.Store.State.Project.Config)
            do! data.ApiServer.Start()
            do! data.SocketServer.Start()
            do! data.GitServer.Start()
          }

        match result with
        | Right _ ->
          let loadedData =
            { data with
                    Status = ServiceStatus.Running
                    Disposables = disposables }
          Loaded(idleData, loadedData)
          |> Either.succeed
        | Left error ->
          disposeAll disposables
          dispose data.SocketServer
          dispose data.ApiServer
          dispose data.RaftServer
          dispose data.GitServer
          Either.fail error


  // ** handleLoad

  let private handleLoad (state: IrisState)
                         (chan: ReplyChan)
                         (projectName, userName, password, site)
                         (config: IrisMachine)
                         (post: CommandAgent)
                         (subscriptions: Subscriptions)
                         (inbox: IrisAgent) =
    match loadProject state config (projectName, userName, password, site) subscriptions with
    | Right nextstate ->
      match start nextstate inbox with
      | Right finalstate ->
        // notify
        ServiceStatus.Running
        |> Status
        |> triggerOnNext subscriptions

        // reply
        Reply.Ok
        |> Either.succeed
        |> chan.Reply

        finalstate

      | Left error ->
        // notify
        ServiceStatus.Failed error
        |> Status
        |> triggerOnNext subscriptions

        // reply
        error
        |> Either.fail
        |> chan.Reply

        state
        |> resetLoaded
    | Left error ->
      // notify
      ServiceStatus.Failed error
      |> Status
      |> triggerOnNext subscriptions

      error
      |> Either.fail
      |> chan.Reply

      state
      |> resetLoaded

  //  _
  // | |    ___   __ _
  // | |   / _ \ / _` |
  // | |__| (_) | (_| |
  // |_____\___/ \__, |
  //             |___/

  // ** handleLogEvent

  let private handleLogEvent (state: IrisState) (log: LogEvent) =
    Tracing.trace "IrisService.handleLogEvent" <| fun () ->
      withState state <| fun data ->
        log
        |> LogMsg
        |> broadcastMsg data
      state

  // ** handleUnload

  let private handleUnload (state: IrisState) (chan: ReplyChan) =
    Tracing.trace "IrisService.handleUnload" <| fun () ->
      triggerWithLoaded state (Status ServiceStatus.Stopped)
      let idleData =
        match state with
        | Idle idleData -> idleData
        | Loaded (idleData, loadedData) ->
          dispose loadedData
          idleData

      Reply.Ok
      |> Either.succeed
      |> chan.Reply

      Idle idleData

  // ** handleConfig

  let private handleConfig (state: IrisState) (chan: ReplyChan) =
    Tracing.trace "IrisService.handleConfig" <| fun () ->
      withDefaultReply state chan <| fun data ->
        data.Store.State.Project.Config
        |> Reply.Config
        |> Either.succeed
        |> chan.Reply
        state

  // ** handleSetConfig

  let private handleSetConfig (state: IrisState) (chan: ReplyChan) (config: IrisConfig) =
    Tracing.trace "IrisService.handleSetConfig" <| fun () ->
      withDefaultReply state chan <| fun data ->
        Project.updateConfig config data.Store.State.Project
        |> UpdateProject
        |> data.Store.Dispatch
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
        state

  // ** handleForceElection

  let private handleForceElection (state: IrisState) =
    Tracing.trace "IrisService.handleForceElection" <| fun () ->
      withoutReply state <| fun data ->
        match data.RaftServer.ForceElection () with
        | Left error ->
          error
          |> string
          |> Logger.err (tag "handleForceElection")
        | other -> ignore other
        state

  // ** handlePeriodic

  let private handlePeriodic (state: IrisState) =
    Tracing.trace "IrisService.handlePeriodic" <| fun () ->
      withoutReply state <| fun data ->
        match data.RaftServer.Periodic() with
        | Left error ->
          error
          |> string
          |> Logger.err (tag "handlePeriodic")
        | other -> ignore other
        state

  // ** handleJoin

  let private handleJoin (state: IrisState) (chan: ReplyChan) (ip: IpAddress) (port: uint16) =
    withDefaultReply state chan <| fun data ->
      asynchronously <| fun _ ->
        Tracing.trace "IrisService.handleJoin" <| fun () ->
          match data.RaftServer.JoinCluster ip port with
          | Right () ->
            Reply.Ok
            |> Either.succeed
            |> chan.Reply
          | Left error ->
            error
            |> Either.fail
            |> chan.Reply
      state

  // ** handleLeave

  let private handleLeave (state: IrisState) (chan: ReplyChan) =
    withDefaultReply state chan <| fun data ->
      asynchronously <| fun _ ->
        Tracing.trace "IrisService.handleLeave" <| fun () ->
          match data.RaftServer.LeaveCluster () with
          | Right () ->
            Reply.Ok
            |> Either.succeed
            |> chan.Reply
          | Left error ->
            error
            |> Either.fail
            |> chan.Reply
      state

  // ** handleAddMember

  let private handleAddMember (state: IrisState) (chan: ReplyChan) (mem: RaftMember) =
    withDefaultReply state chan <| fun data ->
      asynchronously <| fun _ ->
        Tracing.trace "IrisService.handleAddMember" <| fun () ->
          match data.RaftServer.AddMember mem with
          | Right entry ->
            Reply.Entry entry
            |> Either.succeed
            |> chan.Reply
          | Left error ->
            error
            |> Either.fail
            |> chan.Reply
      state

  // ** handleRmMember

  let private handleRmMember (state: IrisState) (chan: ReplyChan) (id: Id) =
    withDefaultReply state chan <| fun data ->
      asynchronously <| fun _ ->
        Tracing.trace "IrisService.handleRmMember" <| fun () ->
          match data.RaftServer.RmMember id  with
          | Right entry ->
            Reply.Entry entry
            |> Either.succeed
            |> chan.Reply
          | Left error ->
            error
            |> Either.fail
            |> chan.Reply
      state

  // ** handleState

  let private handleState (state: IrisState) (chan: ReplyChan) =
    Tracing.trace "IrisService.handleState" <| fun () ->
      withDefaultReply state chan <| fun data ->
        Reply.State data
        |> Either.succeed
        |> chan.Reply
        state

  // ** loop

  let private loop (initial: IrisState)
                   (config: IrisMachine)
                   (post: CommandAgent)
                   (subs: Subscriptions)
                   (inbox: IrisAgent) =
    let rec act (state: IrisState) =
      async {
        let! msg = inbox.Receive()
        let newstate =
          match msg with
          | Msg.Load (chan,pname,uname,pass,site) ->
            handleLoad state chan (pname,uname,pass,site) config post subs inbox
          | Msg.Unload chan          -> handleUnload        state chan
          | Msg.Config chan          -> handleConfig        state chan
          | Msg.SetConfig (chan,cnf) -> handleSetConfig     state chan  cnf
          | Msg.Git    ev            -> handleGitEvent      state inbox ev
          | Msg.Socket ev            -> handleSocketEvent   state       ev
          | Msg.Raft   ev            -> handleRaftEvent     state       ev
          | Msg.Api    ev            -> handleApiEvent      state       ev
          | Msg.Discovery ev         -> handleDiscoveryEvent state      ev
          | Msg.Log   log            -> handleLogEvent      state       log
          | Msg.ForceElection        -> handleForceElection state
          | Msg.Periodic             -> handlePeriodic      state
          | Msg.Join (chan,ip,port)  -> handleJoin          state chan  ip port
          | Msg.Leave  chan          -> handleLeave         state chan
          | Msg.AddMember (chan,mem) -> handleAddMember     state chan  mem
          | Msg.RmMember (chan,id)   -> handleRmMember      state chan  id
          | Msg.State chan           -> handleState         state chan
        return! act newstate
      }

    act initial

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    let private mkIris (subscriptions: Subscriptions) (agent: IrisAgent) =
      let listener =
        { new IObservable<IrisEvent> with
            member self.Subscribe(obs) =
              let guid = Guid.NewGuid()
              do subscriptions.TryAdd(guid, obs) |> ignore
              { new IDisposable with
                  member self.Dispose () =
                    do subscriptions.TryRemove(guid) |> ignore } }

      { new IIrisServer with
          member self.Config
            with get () =
              Tracing.trace "IrisService.Config" <| fun () ->
                match postCommand agent "Config" (fun chan -> Msg.Config chan) with
                | Right (Reply.Config config) -> Right config
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "Config")
                  |> Either.fail

          member self.SetConfig (config: IrisConfig) =
            Tracing.trace "IrisService.SetConfig()" <| fun () ->
              match postCommand agent "SetConfig" (fun chan -> Msg.SetConfig(chan,config)) with
              | Right Reply.Ok -> Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "SetConfig")
                |> Either.fail

          member self.Status
            with get () =
              Tracing.trace "IrisService.Status" <| fun () ->
                match postCommand agent "Status" (fun chan -> Msg.State chan) with
                | Right (Reply.State state) -> Right state.Status
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "Status")
                  |> Either.fail

          member self.LoadProject(name, username, password, site) =
            match postCommand agent "Load" (fun chan -> Msg.Load(chan, name, username, password, site)) with
            | Right Reply.Ok -> Right ()
            | Left error -> Left error
            | Right other ->
              sprintf "Unexpected response from IrisAgent: %A" other
              |> Error.asOther (tag "Load")
              |> Either.fail

          member self.UnloadProject() =
            Tracing.trace "IrisService.UnloadProject" <| fun () ->
              match postCommand agent "Unload" (fun chan -> Msg.Unload chan) with
              | Right Reply.Ok ->
                // Notify subscriptor of the change of state
                triggerOnNext subscriptions (Status ServiceStatus.Running)
                Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "Unload")
                |> Either.fail

          member self.ForceElection () =
            Tracing.trace "IrisService.ForceElection" <| fun () ->
              agent.Post(Msg.ForceElection)
              |> Either.succeed

          member self.Periodic () =
            Tracing.trace "IrisService.Periodic" <| fun () ->
              agent.Post(Msg.Periodic)
              |> Either.succeed

          member self.LeaveCluster () =
            Tracing.trace "IrisService.LeaveCluster" <| fun () ->
              match postCommand agent "LeaveCluster" (fun chan -> Msg.Leave chan) with
              | Right Reply.Ok -> Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "LeaveCluster")
                |> Either.fail

          member self.JoinCluster ip port =
            Tracing.trace "IrisService.JoinCluster" <| fun () ->
              match postCommand agent "JoinCluster" (fun chan -> Msg.Join(chan,ip, port)) with
              | Right Reply.Ok -> Right ()
              | Left error  -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "JoinCluster")
                |> Either.fail

          member self.AddMember mem =
            Tracing.trace "IrisService.AddMember" <| fun () ->
              match postCommand agent "AddMember" (fun chan -> Msg.AddMember(chan,mem)) with
              | Right (Reply.Entry entry) -> Right entry
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "AddMember")
                |> Either.fail

          member self.RmMember id =
            Tracing.trace "IrisService.RmMember" <| fun () ->
              match postCommand agent "RmMember" (fun chan -> Msg.RmMember(chan,id)) with
              | Right (Reply.Entry entry) -> Right entry
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "RmMember")
                |> Either.fail

          member self.GitServer
            with get () =
              Tracing.trace "IrisService.GitServer" <| fun () ->
                match postCommand agent "GitServer" (fun chan -> Msg.State chan) with
                | Right (Reply.State state) -> Right state.GitServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "GitServer")
                  |> Either.fail

          member self.RaftServer
            with get () =
              Tracing.trace "IrisService.RaftServer" <| fun () ->
                match postCommand agent "RaftServer" (fun chan -> Msg.State chan) with
                | Right (Reply.State state) -> Right state.RaftServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "RaftServer")
                  |> Either.fail

          member self.SocketServer
            with get () =
              Tracing.trace "IrisService.SocketServer" <| fun () ->
                match postCommand agent "SocketServer" (fun chan -> Msg.State chan) with
                | Right (Reply.State state) -> Right state.SocketServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "SocketServer")
                  |> Either.fail

          member self.Subscribe(callback: IrisEvent -> unit) =
            { new IObserver<IrisEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Dispose() =
            Tracing.trace "IrisService.Dispose" <| fun () ->
              triggerOnNext subscriptions (Status ServiceStatus.Stopping)
              postCommand agent "Dispose" (fun chan -> Msg.Unload chan)
              |> ignore
              dispose agent
        }


    let private initIdleState (agent: (IrisAgent option) ref) (config: IrisMachine) =
      let discovery = DiscoveryService.create config
      match discovery.Start() with
      | Right _ ->
        discovery.Subscribe(fun ev ->
          match !agent with
          | Some agent -> Msg.Discovery ev |> agent.Post
          | None -> ())
        |> ignore
      | Left error ->
        error
        |> string
        |> Logger.err (tag "startDiscoveryService")
      { DiscoveryService = discovery }
      |> Idle

    let create (config: IrisMachine) (post: CommandAgent) =
      try
        either {
          let subscriptions = new Subscriptions()
          let agent =
            let agentRef = ref None
            let initState = initIdleState agentRef config
            let agent = new IrisAgent(loop initState config post subscriptions)
            agentRef := Some agent
            agent
          agent.Start()
          return mkIris subscriptions agent
        }
      with
      | ex -> IrisError.Other(tag "create", ex.Message) |> Either.fail

#endif
