namespace Iris.Service

// * Imports

#if !IRIS_NODES

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
open SharpYaml.Serialization
open Hopac
open Hopac.Infixes
open ZeroMQ

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

module Iris =
  open Discovery

  // ** tag

  let private tag (str: string) =
    String.Format("IrisService.{0}", str)

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

  [<Literal>]
  let private CLOCK_SERVICE = "clock"

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
  let inline private disposeAll (disposables: Map<_,IDisposable>) =
    Map.iter (konst dispose) disposables

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: IClient }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Socket

  // ** IrisState

  /// ## IrisState
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
  type private IrisState =
    { Member               : RaftMember
      Machine              : IrisMachine
      Status               : ServiceStatus
      Store                : Store
      Leader               : Leader option
      ApiServer            : IApiServer
      GitServer            : IGitServer
      RaftServer           : IRaftServer
      SocketServer         : IWebSocketServer
      DiscoveryService     : IDiscoveryService
      ClockService         : IClock
      Subscriptions        : Subscriptions
      MakePeerSocket       : ClientConfig -> IClient
      Disposables          : Map<string,IDisposable>
      DiscoverableService  : IDisposable option }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        Option.iter dispose self.DiscoverableService
        disposeAll self.Disposables
        dispose self.ApiServer
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.ClockService
        dispose self.SocketServer

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Git               of GitEvent
    | Socket            of WebSocketEvent
    | Raft              of RaftEvent
    | Api               of ApiEvent
    | Log               of LogEvent
    | Clock             of ClockEvent
    | Discovery         of DiscoveryEvent
    | SetConfig         of IrisConfig
    | AddMember         of RaftMember
    | RemoveMember      of Id
    | RawClientResponse of RawClientResponse
    | ForceElection
    | Periodic
    // | Join              of IpAddress  * uint16
    // | Leave

  // ** IrisAgent

  /// ## IrisAgent
  ///
  /// Type alias for internal state mutation actor.
  ///
  type private IrisAgent = MailboxProcessor<Msg>

  // ** registerService

  let private registerService (service: IDiscoveryService)
                              (config: IrisMachine)
                              (status: MachineStatus)
                              (services: ExposedService[])
                              (metadata: Property[])=

    let discoverable: DiscoverableService =
      { Id = config.MachineId
        WebPort = port config.WebPort
        Status = status
        Services = services
        ExtraMetadata = metadata }

    match service.Register discoverable with
    | Right registration -> Some registration
    | Left error ->
      error
      |> sprintf "Could not register service %O"
      |> Logger.err (tag "registerIdleServices")
      None

  // ** registerIdleServices

  let private registerIdleServices (config: IrisMachine) (service: IDiscoveryService) =
    let services =
      [| { ServiceType = ServiceType.Http
           Port = port config.WebPort } |]
    registerService service config MachineStatus.Idle services [| |]

  // ** registerLoadedServices

  let private registerLoadedServices (mem: RaftMember) (project: IrisProject) service =
    let status = MachineStatus.Busy (project.Id, project.Name)
    let services =
      [| { ServiceType = ServiceType.Api;       Port = port mem.ApiPort }
         { ServiceType = ServiceType.Git;       Port = port mem.GitPort }
         { ServiceType = ServiceType.Raft;      Port = port mem.Port    }
         { ServiceType = ServiceType.Http;      Port = port project.Config.Machine.WebPort }
         { ServiceType = ServiceType.WebSocket; Port = port mem.WsPort  } |]
    registerService service project.Config.Machine status services [| |]

  // ** notify

  let private notify (subscriptions: Subscriptions) (ev: IrisEvent) =
    let subs = subscriptions.ToArray()
    for KeyValue(_,subscription) in subs do
      try subscription.OnNext ev
      with
        | exn ->
          exn.Message
          |> sprintf "error notifying listeners: %s"
          |> Logger.err (tag "notify")

  // ** broadcastMsg

  let private broadcastMsg (state: IrisState) (cmd: StateMachine) =
    cmd
    |> state.SocketServer.Broadcast
    |> ignore

  // ** sendMsg

  let private sendMsg (state: IrisState) (id: Id) (cmd: StateMachine) =
    Tracing.trace (tag "sendMsg") <| fun () ->
      cmd
      |> state.SocketServer.Send id
      |> ignore

  // ** appendCmd

  let private appendCmd (state: IrisState) (cmd: StateMachine) =
    Tracing.trace (tag "appendCmd") <| fun () ->
      if state.RaftServer.IsLeader then
        cmd |> state.RaftServer.Append
      else
        "ignoring append request, not leader"
        |> Logger.debug (tag "appendCmd")

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
    sendMsg state session (DataSnapshot state.Store.State)

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
    match Map.tryFind id state.Store.State.Sessions with
    | Some session -> session |> RemoveSession |> appendCmd state
    | _ -> ()

  // ** onError

  /// ## OnError
  ///
  /// Register a callback to be run if the client connection unexpectectly fails. In that case the
  /// Session is retrieved and removed from global state.
  let private onError (state: IrisState) (sessionid: Id) (err: Exception) =
    match Map.tryFind sessionid state.Store.State.Sessions with
    | Some session -> session |> RemoveSession |> appendCmd state
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
    match cmd with
    // If its something that appeared via the fast-lane, dispatch it on the Store and via the
    // WebSockets right away. Evertything else needs to be logged via raft.
    | UpdateSlices _ | CallCue _ ->
      state.ApiServer.Update cmd
      state.Store.Dispatch cmd
      broadcastMsg state cmd
    | AddSession session ->
      session
      |> state.SocketServer.BuildSession id
      |> Either.map AddSession
      |> Either.iter (appendCmd state)
    | AddMember mem -> state.RaftServer.AddMember mem
    | RemoveMember mem -> state.RaftServer.RemoveMember mem.Id
    | cmd -> appendCmd state cmd

  // ** handleSocketEvent

  let private handleSocketEvent (state: IrisState) ev =
    match ev with
    | SessionAdded id   -> onOpen    state id
    | SessionRemoved id -> onClose   state id
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
    either {
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
    let cmd = AddMember mem
    state.Store.Dispatch cmd
    broadcastMsg state cmd
    state

  // ** onMemberUpdated

  /// ## OnMemberUpdated
  ///
  /// Register a callback to be called when a cluster mem's properties such as e.g. its mem
  /// state.

  let private onMemberUpdated (state: IrisState) (mem: RaftMember) =
    let cmd = UpdateMember mem
    state.Store.Dispatch cmd
    broadcastMsg state cmd
    state

  // ** onMemberRemoved

  /// ## OnMemberRemoved
  ///
  /// Register a callback to be run when a mem was removed from the cluster, resulting into
  /// the cluster entering into joint-consensus mode until the mem was successfully removed.

  let private onMemberRemoved (state: IrisState) (mem: RaftMember) =
    let cmd = RemoveMember mem
    state.Store.Dispatch cmd
    broadcastMsg state cmd
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
    state.ApiServer.Update sm

    if state.RaftServer.IsLeader then
      match persistEntry state.Store.State sm with
      | Right commit ->
        sprintf "Persisted command %s in commit: %s" (string sm) commit.Sha
        |> Logger.debug (tag "onApplyLog")
        state
      | Left error ->
        sprintf "Error persisting command: %A" error
        |> Logger.err (tag "onApplyLog")
        state
    else
      let raft = state.RaftServer.Raft
      let mem =
        raft
        |> Raft.currentLeader
        |> Option.bind (flip Raft.getMember raft)

      match mem with
      | Some leader ->
        match updateRepo state.Store.State.Project leader with
        | Right () -> ()
        | Left error ->
          error
          |> string
          |> Logger.err (tag "onApplyLog")
      | None -> ()
      state

  // ** makeLeader

  let private makeLeader (leader: RaftMember)
                         (construct: ClientConfig -> IClient)
                         (agent: IrisAgent) =
    let socket = construct {
        PeerId = leader.Id
        Frontend = Uri.raftUri leader
        Timeout = int Constants.REQ_TIMEOUT * 1<ms>
      }
    socket.Subscribe (Msg.RawClientResponse >> agent.Post) |> ignore
    { Member = leader; Socket = socket }

  // ** onStateChanged

  let private onStateChanged (state: IrisState)
                             (oldstate: RaftState)
                             (newstate: RaftState)
                             (agent: IrisAgent) =
    sprintf "Raft state changed from %A to %A" oldstate newstate
    |> Logger.debug (tag "onStateChanged")
    // create redirect socket
    match oldstate, newstate with
    | _, Follower ->
      Option.iter dispose state.Leader
      match state.RaftServer.Leader with
      | Some leader ->
        { state with
            Leader = Some (makeLeader leader state.MakePeerSocket agent) }
      | None ->
        "Could not start re-direct socket: no leader"
        |> Logger.debug (tag "onStateChanged")
        state
    | _, Leader ->
      Option.iter dispose state.Leader
      { state with Leader = None }
    | _ -> state

  // ** onCreateSnapshot

  let private onCreateSnapshot (state: IrisState) (ch: Ch<State option>) =
    job {
      do! Ch.send ch (Some state.Store.State)
    } |> Hopac.queue
    state

  // ** onRetrieveSnapshot

  let private onRetrieveSnapshot (state: IrisState) (ch: Ch<RaftLogEntry option>) =
    job {
      let path = Constants.RAFT_DIRECTORY <.>
                  Constants.SNAPSHOT_FILENAME +
                  Constants.ASSET_EXTENSION
      match Asset.read path with
      | Right str ->
        try
          let serializer = new Serializer()

          let yml = serializer.Deserialize<SnapshotYaml>(str)

          let members =
            match Config.getActiveSite state.Store.State.Project.Config with
            | Some site -> site.Members |> Map.toArray |> Array.map snd
            | _ -> [| |]

          let snapshot =
            Snapshot(Id yml.Id
                    ,yml.Index
                    ,yml.Term
                    ,yml.LastIndex
                    ,yml.LastTerm
                    ,members
                    ,DataSnapshot state.Store.State)

          do! Ch.send ch (Some snapshot)
        with
          | exn ->
            exn.Message
            |> Logger.err (tag "onRetrieveSnapshot")
            do! Ch.send ch None

      | Left error ->
        error
        |> string
        |> Logger.err (tag "onRetrieveSnapshot")
        do! Ch.send ch None
    } |> Hopac.queue
    state

  // ** onPersistSnapshot

  let private onPersistSnapshot (state: IrisState) (log: RaftLogEntry) =
    match persistSnapshot state.Store.State log with
    | Left error -> Logger.err (tag "onPersistSnapshot") (string error)
    | _ -> ()
    state

  // ** requestAppend

  let private requestAppend (self: MemberId) (leader: Leader) (sm: StateMachine) =
    AppendEntry sm
    |> Binary.encode
    |> fun body -> { Body = body }
    |> leader.Socket.Request
    |> Either.mapError (sprintf "request error: %O" >> Logger.err (tag "requestAppend"))
    |> ignore

    //   match result with
    //   | Right (AppendEntryResponse _) ->
    //     Either.succeed leader
    //   | Right (Redirect mem) ->
    //     if count < max then
    //       dispose leader
    //       let newleader = mkLeader self mem
    //       impl newleader (count + 1)
    //     else
    //       max
    //       |> sprintf "Maximum re-direct count reached (%d). Appending failed"
    //       |> Error.asRaftError (tag "requestAppend")
    //       |> Either.fail
    //   | Right other ->
    //     other
    //     |> sprintf "Received unexpected response from server: %A"
    //     |> Error.asRaftError (tag "requestAppend")
    //     |> Either.fail
    //   | Left error ->
    //     Either.fail error

    // Tracing.trace (tag "requestAppend") <| fun () ->
    //   impl leader 0

  // ** forwardCommand

  let private forwardCommand (state: IrisState) (sm: StateMachine) (agent: IrisAgent) =
    Tracing.trace (tag "forwardCommand") <| fun () ->
      match state.Leader with
      | Some leader ->
        requestAppend state.Member.Id leader sm
        state
        // | Right newleader ->
        //   { state with Leader = Some newleader }
        // | Left error ->
        //   dispose leader
        //   { state with Leader = None }
      | None ->
        match state.RaftServer.Leader with
        | Some mem ->
          let leader = makeLeader mem state.MakePeerSocket agent
          requestAppend state.Member.Id leader sm
          state
          // | Right newleader ->
          //   { state with Leader = Some newleader }
          // | Left error ->
          //   dispose leader
          //   { state with Leader = None }
        | None ->
          "Could not start re-direct socket: No Known Leader"
          |> Logger.debug (tag "forwardCommand")
          state

  // ** handleRaftEvent

  let private handleRaftEvent (state: IrisState) agent (ev: RaftEvent) =
    ev |> IrisEvent.Raft |> notify state.Subscriptions
    Tracing.trace (tag "handleRaftEvent") <| fun () ->
      match ev with
      | RaftEvent.ApplyLog sm             -> onApplyLog         state sm
      | RaftEvent.MemberAdded mem         -> onMemberAdded      state mem
      | RaftEvent.MemberRemoved mem       -> onMemberRemoved    state mem
      | RaftEvent.MemberUpdated mem       -> onMemberUpdated    state mem
      | RaftEvent.Configured mems         -> onConfigured       state mems
      | RaftEvent.CreateSnapshot ch       -> onCreateSnapshot   state ch
      | RaftEvent.RetrieveSnapshot ch     -> onRetrieveSnapshot state ch
      | RaftEvent.PersistSnapshot log     -> onPersistSnapshot  state log
      | RaftEvent.StateChanged (ost, nst) -> onStateChanged     state ost nst agent
      | _ -> state

  // ** handleApiEvent

  let private handleApiEvent (state: IrisState) agent (ev: ApiEvent) =
    Tracing.trace (tag "handleApiEvent") <| fun () ->
      match ev with
      | ApiEvent.Update sm ->
        // ApiEvents:
        // If its something that appeared via the fast-lane, dispatch it on the Store and via the
        // WebSockets right away. Evertything else needs to be logged via raft.
        match sm with
        | UpdateSlices _ | CallCue _ ->
          state.Store.Dispatch sm
          state.SocketServer.Broadcast sm
          |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
          |> ignore
          state
        | other ->
          if state.RaftServer.IsLeader then
            state.RaftServer.Append other
            state
          else
            forwardCommand state other agent
      | ApiEvent.Register client ->
        state.RaftServer.Append (AddClient client)
        state
      | ApiEvent.UnRegister client ->
        state.RaftServer.Append (RemoveClient client)
        state
      | _ -> // Status events
        notify state.Subscriptions (IrisEvent.Api ev)
        state

  // ** handleDiscoveryEvent

  let private handleDiscoveryEvent (state: IrisState) (ev: DiscoveryEvent) =
    Tracing.trace (tag "handleDiscoveryEvent") <| fun () ->
      match ev with
      | Appeared service -> AddDiscoveredService service    |> appendCmd state
      | Updated  service -> UpdateDiscoveredService service |> appendCmd state
      | Vanished service -> RemoveDiscoveredService service |> appendCmd state
      | _ -> ()
      state

  // ** forwardEvent

  let inline private forwardEvent (constr: ^a -> Msg) (agent: IrisAgent) =
    constr >> agent.Post
  // ** restartGitServer

  let private restartGitServer (state: IrisState) (agent: IrisAgent) =
    Tracing.trace (tag "restartGitServer") <| fun () ->
      state.Disposables
      |> Map.tryFind GIT_SERVER
      |> Option.map dispose
      |> ignore

      dispose state.GitServer

      let result =
        either {
          let mem = state.RaftServer.Member
          let! gitserver = GitServer.create mem state.Store.State.Project.Path
          let disposable =
            agent
            |> forwardEvent Msg.Git
            |> gitserver.Subscribe
          match gitserver.Start() with
          | Right () ->
            return { state with
                      GitServer = gitserver
                      Disposables = Map.add GIT_SERVER disposable state.Disposables }
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
        state

  // ** handleGitEvent

  let private handleGitEvent (state: IrisState) (agent: IrisAgent) (ev: GitEvent) =
    Tracing.trace (tag "handleGitEvent") <| fun () ->
      ev |> IrisEvent.Git |> notify state.Subscriptions
      match ev with
      | Started pid ->
        sprintf "Git daemon started with PID: %d" pid
        |> Logger.debug (tag "handleGitEvent")
        state

      | Exited _ ->
        "Git daemon exited. Attempting to restart."
        |> Logger.debug (tag "handleGitEvent")
        let newData = restartGitServer state agent
        newData

      | Pull (_, addr, port) ->
        sprintf "Client %s:%d pulled updates from me" addr port
        |> Logger.debug (tag "handleGitEvent")
        state

  // ** handleLoad

  let private handleLoad (state: IrisState)
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
        |> notify subscriptions
        finalstate

      | Left error ->
        // notify
        ServiceStatus.Failed error
        |> Status
        |> notify subscriptions
        state
    | Left error ->
      // notify
      ServiceStatus.Failed error
      |> Status
      |> notify subscriptions
      state

  //  _
  // | |    ___   __ _
  // | |   / _ \ / _` |
  // | |__| (_) | (_| |
  // |_____\___/ \__, |
  //             |___/

  // ** handleLogEvent

  let private handleLogEvent (state: IrisState) (log: LogEvent) =
    log
    |> LogMsg
    |> broadcastMsg state
    state

  // ** handleSetConfig

  let private handleSetConfig (state: IrisState) (config: IrisConfig) =
    Tracing.trace (tag "handleSetConfig") <| fun () ->
      Project.updateConfig config state.Store.State.Project
      |> UpdateProject
      |> state.Store.Dispatch
      state

  // ** handleForceElection

  let private handleForceElection (state: IrisState) =
    Tracing.trace (tag "handleForceElection") <| fun () ->
      state.RaftServer.ForceElection()
      state

  // ** handlePeriodic

  let private handlePeriodic (state: IrisState) =
    Tracing.trace (tag "handlePeriodic") <| fun () ->
      state.RaftServer.Periodic()
      state

  // ** handleJoin

  // let private handleJoin (state: IrisState) (chan: ReplyChan) (ip: IpAddress) (port: uint16) =
  //   Tracing.trace (tag "handleJoin") <| fun () ->
  //     match state.RaftServer.JoinCluster ip port with
  //     | Right () ->
  //       Reply.Ok
  //       |> Either.succeed
  //       |> chan.Reply
  //     | Left error ->
  //       error
  //       |> Either.fail
  //       |> chan.Reply
  //     state

  // ** handleLeave

  // let private handleLeave (state: IrisState) (chan: ReplyChan) =
  //   withDefaultReply state chan <| fun data ->
  //     asynchronously <| fun _ ->
  //       Tracing.trace (tag "handleLeave") <| fun () ->
  //         match data.RaftServer.LeaveCluster () with
  //         | Right () ->
  //           Reply.Ok
  //           |> Either.succeed
  //           |> chan.Reply
  //         | Left error ->
  //           error
  //           |> Either.fail
  //           |> chan.Reply
  //     state

  // ** handleAddMember

  let private handleAddMember (state: IrisState) (mem: RaftMember) =
    Tracing.trace (tag "handleAddMember") <| fun () ->
      state.RaftServer.AddMember mem
      state

  // ** handleRemoveMember

  let private handleRemoveMember (state: IrisState) (id: Id) =
    Tracing.trace (tag "handleRemoveMember") <| fun () ->
      state.RaftServer.RemoveMember id
      state

  // ** handleClock

  let private handleClock (state: IrisState) (clock: ClockEvent) =
    Tracing.trace (tag "handleClock") <| fun () ->
      let sm = clock.Frame |> uint32 |> UpdateClock
      broadcastMsg state sm
      state.ApiServer.Update sm
      state

  // ** handleClientResponse

  let private handleClientResponse (state: IrisState) (response: RawClientResponse) =
    state

  // ** loop

  let private loop (store: IAgentStore<IrisState>) (post: CommandAgent) (inbox: IrisAgent) =
    let rec act () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        let newstate =
          match msg with
          | Msg.SetConfig       cnf  -> handleSetConfig      state       cnf
          | Msg.Git    ev            -> handleGitEvent       state inbox ev
          | Msg.Socket ev            -> handleSocketEvent    state       ev
          | Msg.Raft   ev            -> handleRaftEvent      state inbox ev
          | Msg.Api    ev            -> handleApiEvent       state inbox ev
          | Msg.Discovery ev         -> handleDiscoveryEvent state       ev
          | Msg.Log   log            -> handleLogEvent       state       log
          | Msg.ForceElection        -> handleForceElection  state
          | Msg.Periodic             -> handlePeriodic       state
          | Msg.AddMember       mem  -> handleAddMember      state       mem
          | Msg.RemoveMember    id   -> handleRemoveMember   state       id
          | Msg.Clock clock          -> handleClock          state       clock
          | Msg.RawClientResponse r  -> handleClientResponse state       r
        store.Update newstate
        return! act ()
      }

    act ()

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    // *** makeIris

    let private makeIris (store: IAgentStore<IrisState>) (agent: IrisAgent) =
      let listener =
        { new IObservable<IrisEvent> with
            member self.Subscribe(obs) =
              let guid = Guid.NewGuid()
              do store.State.Subscriptions.TryAdd(guid, obs) |> ignore
              { new IDisposable with
                  member self.Dispose () =
                    do store.State.Subscriptions.TryRemove(guid) |> ignore } }

      { new IIrisServer with
          member self.Project
            with get () = store.State.Store.State.Project // :D

          member self.Config
            with get () = store.State.Store.State.Project.Config // :D
            and set config = agent.Post (Msg.SetConfig config)

          member self.Status
            with get () = store.State.Status

          member self.ForceElection () = agent.Post(Msg.ForceElection)

          member self.Periodic () = agent.Post(Msg.Periodic)

          member self.AddMember mem =
            mem
            |> Msg.AddMember
            |> agent.Post

          member self.RemoveMember id =
            id
            |> Msg.RemoveMember
            |> agent.Post

          member self.GitServer
            with get () = store.State.GitServer

          member self.RaftServer
            with get () = store.State.RaftServer

          member self.SocketServer
            with get () = store.State.SocketServer

          member self.Subscribe(callback: IrisEvent -> unit) =
            { new IObserver<IrisEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = callback value }
            |> listener.Subscribe

          member self.Dispose() =
            Tracing.trace (tag "Dispose") <| fun () ->
              ServiceStatus.Stopping
              |> Status
              |> notify store.State.Subscriptions
              dispose agent

          // member self.LeaveCluster () =
          //   Tracing.trace (tag "LeaveCluster") <| fun () ->
          //     match postCommand agent "LeaveCluster"  Msg.Leave with
          //     | Right Reply.Ok -> Right ()
          //     | Left error -> Left error
          //     | Right other ->
          //       sprintf "Unexpected response from IrisAgent: %A" other
          //       |> Error.asOther (tag "LeaveCluster")
          //       |> Either.fail

          // member self.JoinCluster ip port =
          //   Tracing.trace (tag "JoinCluster") <| fun () ->
          //     match postCommand agent "JoinCluster" (fun chan -> Msg.Join(chan,ip, port)) with
          //     | Right Reply.Ok -> Right ()
          //     | Left error  -> Left error
          //     | Right other ->
          //       sprintf "Unexpected response from IrisAgent: %A" other
          //       |> Error.asOther (tag "JoinCluster")
          //       |> Either.fail

        }

    // *** loadProject

    //  _                    _
    // | |    ___   __ _  __| |
    // | |   / _ \ / _` |/ _` |
    // | |__| (_) | (_| | (_| |
    // |_____\___/ \__,_|\__,_|

    let private loadProject (oldState: IrisState)
                            (machine: IrisMachine)
                            (projectName: string, userName: string, password: Password, site: string option)
                            (subscriptions: Subscriptions) =
      let isValidPassword (user: User) (password: Password) =
        let password = Crypto.hashPassword password user.Salt
        password = user.Password

      either {
        let! path = Project.checkPath machine projectName
        let! (state: State) = Asset.loadWithMachine path machine

        let user =
          state.Users
          |> Map.tryPick (fun _ u -> if u.UserName = name userName then Some u else None)

        match user with
        | Some user when isValidPassword user password ->
          let machine, discoveryService =
            dispose oldState
            oldState.Machine, oldState.DiscoveryService

          let state =
            match site with
            | Some site ->
              let site =
                state.Project.Config.Sites
                |> Array.tryFind (fun s -> s.Name = name site)
                |> function Some s -> s | None -> { ClusterConfig.Default with Name = name site }

              // Add current machine if necessary
              // taking the default ports from MachineConfig
              let site =
                let machineId = machine.MachineId
                if Map.containsKey machineId site.Members
                then site
                else
                  let selfMember =
                    { Member.create(machineId) with
                        IpAddr  = IpAddress.Parse machine.WebIP
                        GitPort = machine.GitPort
                        WsPort  = machine.WsPort
                        ApiPort = machine.ApiPort
                        Port    = machine.RaftPort }
                  { site with Members = Map.add machineId selfMember site.Members }

              let cfg = state.Project.Config |> Config.addSiteAndSetActive site
              { state with Project = { state.Project with Config = cfg }}
            | None -> state

          // This will fail if there's no ActiveSite set up in state.Project.Config
          // The frontend needs to handle that case
          let! mem = Config.selfMember state.Project.Config
          let ctx = new ZContext()
          let! raftserver = RaftServer.create ctx state.Project.Config
          let! wsserver   = WebSocketServer.create mem
          let! apiserver  = ApiServer.create ctx mem state.Project.Id
          let! gitserver  = GitServer.create mem state.Project.Path // IMPORTANT: use the projects
                                                                    // path here, not the path to
                                                                    // project.yml

          let clock = Clock.create ctx mem.IpAddr

          // Try to put discovered services into the state
          let state =
            match discoveryService.Services with
            | Right (_, resolvedServices) ->
              { state with DiscoveredServices = resolvedServices }
            | Left err ->
              string err |> Logger.err (tag "loadProject.getDiscoveredServices")
              state

          return { Member              = mem
                   Machine             = machine
                   Leader              = None
                   Status              = ServiceStatus.Starting
                   Store               = new Store(state)
                   ApiServer           = apiserver
                   GitServer           = gitserver
                   RaftServer          = raftserver
                   SocketServer        = wsserver
                   ClockService        = clock
                   Subscriptions       = subscriptions
                   DiscoveryService    = discoveryService
                   DiscoverableService = None
                   MakePeerSocket      = Client.create ctx
                   Disposables         = Map.empty }
        | _ ->
          return!
            "Login rejected"
            |> Error.asProjectError (tag "loadProject")
            |> Either.fail
      }

    // *** start

    let private start (state: IrisState) (agent: IrisAgent) =
      Tracing.trace (tag "start") <| fun () ->
        let disposables =
          [ (LOG_HANDLER,   agent |> forwardEvent Msg.Log    |> Logger.subscribe)
            (RAFT_SERVER,   agent |> forwardEvent Msg.Raft   |> state.RaftServer.Subscribe)
            (WS_SERVER,     agent |> forwardEvent Msg.Socket |> state.SocketServer.Subscribe)
            (API_SERVER,    agent |> forwardEvent Msg.Api    |> state.ApiServer.Subscribe)
            (GIT_SERVER,    agent |> forwardEvent Msg.Git    |> state.GitServer.Subscribe)
            (CLOCK_SERVICE, agent |> forwardEvent Msg.Clock  |> state.ClockService.Subscribe) ]
          |> Map.ofList

        let service =
          registerLoadedServices
            state.Member
            state.Store.State.Project
            state.DiscoveryService

        let result =
          either {
            do! state.RaftServer.Start()
            do! state.ApiServer.Start()
            do! state.SocketServer.Start()
            do! state.GitServer.Start()
          }

        match result with
        | Right _ ->
          { state with
              Status = ServiceStatus.Running
              DiscoverableService = service
              Disposables = disposables }
          |> Either.succeed
        | Left error ->
          disposeAll disposables
          dispose state.SocketServer
          dispose state.ApiServer
          dispose state.RaftServer
          dispose state.GitServer
          Either.fail error

    // *** create

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
          return makeIris subscriptions agent
        }
      with
      | ex -> IrisError.Other(tag "create", ex.Message) |> Either.fail

#endif
