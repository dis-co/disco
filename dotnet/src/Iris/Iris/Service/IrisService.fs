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
  type private IrisLoadedStateData =
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
    | Git           of GitEvent
    | Socket        of WebSocketEvent
    | Raft          of RaftEvent
    | Api           of ApiEvent
    | Log           of LogEvent
    | Clock         of ClockEvent
    | Discovery     of DiscoveryEvent
    | SetConfig     of IrisConfig
    | AddMember     of RaftMember
    | RemoveMember  of Id
    | Join          of IpAddress  * uint16
    | Leave
    | Config
    | State
    | MachineStatus
    | ForceElection
    | Periodic

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
  let subs = suscriptions.ToArray()
    for subscription in subs do
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
        cmd
        |> state.RaftServer.Append
        |> Either.map ignore
      else
        "ignoring append request, not leader"
        |> Logger.debug (tag "appendCmd")
        |> Either.succeed

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
    sendMsg state session (DataSnapshot data.Store.State)

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
    | Some session ->
      match appendCmd state (RemoveSession session) with
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
    match Map.tryFind sessionid state.Store.State.Sessions with
    | Some session ->
      match appendCmd state (RemoveSession session) with
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
      |> Either.bind (appendCmd state)
      |> Either.mapError (string >> Logger.err (tag "onMessage"))
      |> ignore
    | AddMember mem ->
      state.RaftServer.AddMember mem
      |> Either.map (sprintf "added new member in: %O" >> Logger.debug (tag "onMessage"))
      |> Either.mapError (string >> Logger.err (tag "onMessage"))
      |> ignore
    | RemoveMember mem ->
      state.RaftServer.RmMember mem.Id
      |> Either.map (sprintf "removed member in: %O" >> Logger.debug (tag "onMessage"))
      |> Either.mapError (string >> Logger.err (tag "onMessage"))
      |> ignore
    | cmd ->
      appendCmd state cmd
      |> Either.mapError (string >> Logger.err (tag "onMessage"))
      |> ignore

  // ** handleSocketEvent

  let private handleSocketEvent (state: IrisState) = function
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
      match state.RaftServer.State with
      | Right raft ->
        let mem =
          raft.Raft
          |> Raft.currentLeader
          |> Option.bind (flip Raft.getMember raft.Raft)

        match mem with
        | Some leader ->
          match updateRepo state.Store.State.Project leader with
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
    let addr = Uri.raftUri leader
    let socket = Client.create self addr Constants.REQ_TIMEOUT
    { Member = leader; Socket = socket }

  // ** onStateChanged

  let private onStateChanged (state: IrisState)
                             (oldstate: RaftState)
                             (newstate: RaftState) =
    sprintf "Raft state changed from %A to %A" oldstate newstate
    |> Logger.debug (tag "onStateChanged")
    // create redirect socket
    match oldstate, newstate with
    | _, Follower ->
      Option.iter dispose state.Leader
      match state.RaftServer.Leader with
      | Right (Some leader) ->
        Loaded { state with Leader = Some (mkLeader data.Member.Id leader) }
      | Right None ->
        "Could not start re-direct socket: no leader"
        |> Logger.debug (tag "onStateChanged")
        state
      | Left error ->
        string error
        |> Logger.err (tag "onStateChanged")
        state
    | _, Leader ->
      Option.iter dispose state.Leader
      Loaded { state with Leader = None }
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

    Tracing.trace (tag "requestAppend") <| fun () ->
      impl leader 0

  // ** forwardCommand

  let private forwardCommand (state: IrisState) (sm: StateMachine) =
    Tracing.trace (tag "forwardCommand") <| fun () ->
      match state.Leader with
      | Some leader ->
        match requestAppend state.Member.Id leader sm with
        | Right newleader ->
          { state with Leader = Some newleader }
        | Left error ->
          dispose leader
          { state with Leader = None }
      | None ->
        match state.RaftServer.Leader with
        | Right (Some mem) ->
          let leader = mkLeader state.Member.Id mem
          match requestAppend state.Member.Id leader sm with
          | Right newleader ->
            { state with Leader = Some newleader }
          | Left error ->
            dispose leader
            { state with Leader = None }
        | Right None ->
          "Could not start re-direct socket: No Known Leader"
          |> Logger.debug (tag "forwardCommand")
          state
        | Left error ->
          string error
          |> Logger.err (tag "forwardCommand")
          state

  // ** handleRaftEvent

  let private handleRaftEvent (state: IrisState) (ev: RaftEvent) =
    ev |> IrisEvent.Raft |> notifyWithLoaded state
    Tracing.trace (tag "handleRaftEvent") <| fun () ->
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
            |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
            |> ignore
            state
          else
            forwardCommand state other
            |> Loaded
      | ApiEvent.Register client ->
        state.RaftServer.Append (AddClient client)
        |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
        |> ignore
        state
      | ApiEvent.UnRegister client ->
        state.RaftServer.Append (RemoveClient client)
        |> Either.mapError (string >> Logger.err (tag "handleApiEvent"))
        |> ignore
        state
      | _ -> // Status events
        notify state.Subscriptions (IrisEvent.Api ev)
        state

  // ** handleDiscoveryEvent

  let private handleDiscoveryEvent (state: IrisState) (ev: DiscoveryEvent) =
    let appendCommand data cmd =
      match appendCmd data cmd with
      | Right _ -> ()
      | Left error  ->
        error
        |> string
        |> Logger.err (tag "handleDiscoveryEvent")

    Tracing.trace (tag "handleDiscoveryEvent") <| fun () ->
      match ev with
      | Appeared service -> AddDiscoveredService service    |> appendCommand state
      | Updated  service -> UpdateDiscoveredService service |> appendCommand state
      | Vanished service -> RemoveDiscoveredService service |> appendCommand state
      | _ -> ()
      state


  // ** forwardEvent

  let inline private forwardEvent (constr: ^a -> Msg) (agent: IrisAgent) =
    constr >> agent.Post
  // ** restartGitServer

  let private restartGitServer (data: IrisLoadedStateData) (agent: IrisAgent) =
    Tracing.trace (tag "restartGitServer") <| fun () ->
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
            agent
            |> forwardEvent Msg.Git
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
    Tracing.trace (tag "handleGitEvent") <| fun () ->
      match state with
      | Idle   _    -> state
      | Loaded data ->
        notify data.Subscriptions (IrisEvent.Git ev)
        match ev with
        | Started pid ->
          sprintf "Git daemon started with PID: %d" pid
          |> Logger.debug (tag "handleGitEvent")
          state

        | Exited _ ->
          "Git daemon exited. Attempting to restart."
          |> Logger.debug (tag "handleGitEvent")
          let newData = restartGitServer data agent
          Loaded newData

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
          match oldState with
          | Idle idle ->
            dispose idle
            idle.Machine, idle.DiscoveryService
          | Loaded loaded ->
            dispose loaded
            loaded.Machine, loaded.DiscoveryService

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

        let! raftserver = RaftServer.create ()
        let! wsserver   = WebSocketServer.create mem
        let! apiserver  = ApiServer.create mem state.Project.Id
        let! gitserver  = GitServer.create mem state.Project.Path // IMPORTANT: use the projects
                                                                  // path here, not the path to
                                                                  // project.yml

        let clock = Clock.create mem.IpAddr

        // Try to put discovered services into the state
        let state =
          match discoveryService.Services with
          | Right (_, resolvedServices) ->
            { state with DiscoveredServices = resolvedServices }
          | Left err ->
            string err |> Logger.err (tag "loadProject.getDiscoveredServices")
            state

        return Loaded { Member              = mem
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
                        Disposables         = Map.empty }
      | _ ->
        return!
          "Login rejected"
          |> Error.asProjectError (tag "loadProject")
          |> Either.fail
    }

  // ** start

  let private start (state: IrisState) (agent: IrisAgent) =
    Tracing.trace (tag "start") <| fun () ->
      match state with
      | Idle   _    -> Right state
      | Loaded data ->
        let disposables =
          [ (LOG_HANDLER,   agent |> forwardEvent Msg.Log    |> Logger.subscribe)
            (RAFT_SERVER,   agent |> forwardEvent Msg.Raft   |> data.RaftServer.Subscribe)
            (WS_SERVER,     agent |> forwardEvent Msg.Socket |> data.SocketServer.Subscribe)
            (API_SERVER,    agent |> forwardEvent Msg.Api    |> data.ApiServer.Subscribe)
            (GIT_SERVER,    agent |> forwardEvent Msg.Git    |> data.GitServer.Subscribe)
            (CLOCK_SERVICE, agent |> forwardEvent Msg.Clock  |> data.ClockService.Subscribe) ]
          |> Map.ofList

        let service =
          registerLoadedServices
            data.Member
            data.Store.State.Project
            data.DiscoveryService

        let result =
          either {
            do! data.RaftServer.Load(data.Store.State.Project.Config)
            do! data.ApiServer.Start()
            do! data.SocketServer.Start()
            do! data.GitServer.Start()
          }

        match result with
        | Right _ ->
          { data with
              Status = ServiceStatus.Running
              DiscoverableService = service
              Disposables = disposables }
          |> Loaded
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
        |> notify subscriptions

        // reply
        Reply.Ok
        |> Either.succeed
        |> chan.Reply

        finalstate

      | Left error ->
        // notify
        ServiceStatus.Failed error
        |> Status
        |> notify subscriptions

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
      |> notify subscriptions

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
    withState state <| fun data ->
      log
      |> LogMsg
      |> broadcastMsg data
    state

  // ** handleUnload

  let private handleUnload (state: IrisState) (chan: ReplyChan) =
    Tracing.trace (tag "handleUnload") <| fun () ->
      notifyWithLoaded state (Status ServiceStatus.Stopped)
      let idleData =
        match state with
        | Idle idleData -> idleData
        | Loaded loaded ->
          // TODO: Send it to the store as well? (So it can notify listeners)
          broadcastMsg loaded StateMachine.UnloadProject
          let service = loaded.DiscoveryService
          let registration = registerIdleServices loaded.Machine service
          dispose loaded
          { Machine = loaded.Machine
            DiscoveryService = service
            DiscoverableService = registration }

      Reply.Ok
      |> Either.succeed
      |> chan.Reply

      Idle idleData

  // ** handleConfig

  let private handleConfig (state: IrisState) (chan: ReplyChan) =
    Tracing.trace (tag "handleConfig") <| fun () ->
      withDefaultReply state chan <| fun data ->
        data.Store.State.Project.Config
        |> Reply.Config
        |> Either.succeed
        |> chan.Reply
        state

  // ** handleSetConfig

  let private handleSetConfig (state: IrisState) (chan: ReplyChan) (config: IrisConfig) =
    Tracing.trace (tag "handleSetConfig") <| fun () ->
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
    Tracing.trace (tag "handleForceElection") <| fun () ->
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
    Tracing.trace (tag "handlePeriodic") <| fun () ->
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
        Tracing.trace (tag "handleJoin") <| fun () ->
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
        Tracing.trace (tag "handleLeave") <| fun () ->
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
        Tracing.trace (tag "handleAddMember") <| fun () ->
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
        Tracing.trace (tag "handleRmMember") <| fun () ->
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
    Tracing.trace (tag "handleState") <| fun () ->
      withDefaultReply state chan <| fun data ->
        Reply.State data
        |> Either.succeed
        |> chan.Reply
        state

  // ** handleClock

  let private handleClock (state: IrisState) (clock: ClockEvent) =
    Tracing.trace (tag "handleClock") <| fun () ->
      withState state <| fun data ->
        let sm = clock.Frame |> uint32 |> UpdateClock
        broadcastMsg data sm
        data.ApiServer.Update sm
      state

  // ** handleMachineStatus

  let private handleMachineStatus (state: IrisState) (chan: ReplyChan) =
    match state with
    | Idle _ ->
      MachineStatus.Idle
      |> Reply.MachineStatus
      |> Either.succeed
      |> chan.Reply
    | Loaded data ->
      let project = data.Store.State.Project
      MachineStatus.Busy(project.Id, project.Name)
      |> Reply.MachineStatus
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
          | Msg.Unload chan          -> handleUnload         state chan
          | Msg.Config chan          -> handleConfig         state chan
          | Msg.SetConfig (chan,cnf) -> handleSetConfig      state chan  cnf
          | Msg.Git    ev            -> handleGitEvent       state inbox ev
          | Msg.Socket ev            -> handleSocketEvent    state       ev
          | Msg.Raft   ev            -> handleRaftEvent      state       ev
          | Msg.Api    ev            -> handleApiEvent       state       ev
          | Msg.Discovery ev         -> handleDiscoveryEvent state       ev
          | Msg.Log   log            -> handleLogEvent       state       log
          | Msg.ForceElection        -> handleForceElection  state
          | Msg.Periodic             -> handlePeriodic       state
          | Msg.Join (chan,ip,port)  -> handleJoin           state chan  ip port
          | Msg.Leave  chan          -> handleLeave          state chan
          | Msg.AddMember (chan,mem) -> handleAddMember      state chan  mem
          | Msg.RmMember (chan,id)   -> handleRmMember       state chan  id
          | Msg.State chan           -> handleState          state chan
          | Msg.Clock clock          -> handleClock          state       clock
          | Msg.MachineStatus chan   -> handleMachineStatus  state chan
        return! act newstate
      }

    act initial

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    // *** mkIris

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
              Tracing.trace (tag "Config") <| fun () ->
                match postCommand agent "Config" Msg.Config with
                | Right (Reply.Config config) -> Right config
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "Config")
                  |> Either.fail

          member self.SetConfig (config: IrisConfig) =
            Tracing.trace (tag "SetConfig()") <| fun () ->
              match postCommand agent "SetConfig" (fun chan -> Msg.SetConfig(chan,config)) with
              | Right Reply.Ok -> Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "SetConfig")
                |> Either.fail

          member self.Status
            with get () =
              Tracing.trace (tag "Status") <| fun () ->
                match postCommand agent "Status" Msg.State with
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
            Tracing.trace (tag "UnloadProject") <| fun () ->
              match postCommand agent "Unload" Msg.Unload with
              | Right Reply.Ok ->
                // Notify subscriptor of the change of state
                notify subscriptions (Status ServiceStatus.Running)
                Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "Unload")
                |> Either.fail

          member self.ForceElection () =
            Tracing.trace (tag "ForceElection") <| fun () ->
              agent.Post(Msg.ForceElection)
              |> Either.succeed

          member self.Periodic () =
            Tracing.trace (tag "Periodic") <| fun () ->
              agent.Post(Msg.Periodic)
              |> Either.succeed

          member self.LeaveCluster () =
            Tracing.trace (tag "LeaveCluster") <| fun () ->
              match postCommand agent "LeaveCluster"  Msg.Leave with
              | Right Reply.Ok -> Right ()
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "LeaveCluster")
                |> Either.fail

          member self.JoinCluster ip port =
            Tracing.trace (tag "JoinCluster") <| fun () ->
              match postCommand agent "JoinCluster" (fun chan -> Msg.Join(chan,ip, port)) with
              | Right Reply.Ok -> Right ()
              | Left error  -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "JoinCluster")
                |> Either.fail

          member self.AddMember mem =
            Tracing.trace (tag "AddMember") <| fun () ->
              match postCommand agent "AddMember" (fun chan -> Msg.AddMember(chan,mem)) with
              | Right (Reply.Entry entry) -> Right entry
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "AddMember")
                |> Either.fail

          member self.RmMember id =
            Tracing.trace (tag "RmMember") <| fun () ->
              match postCommand agent "RmMember" (fun chan -> Msg.RmMember(chan,id)) with
              | Right (Reply.Entry entry) -> Right entry
              | Left error -> Left error
              | Right other ->
                sprintf "Unexpected response from IrisAgent: %A" other
                |> Error.asOther (tag "RmMember")
                |> Either.fail

          member self.GitServer
            with get () =
              Tracing.trace (tag "GitServer") <| fun () ->
                match postCommand agent "GitServer" Msg.State with
                | Right (Reply.State state) -> Right state.GitServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "GitServer")
                  |> Either.fail

          member self.RaftServer
            with get () =
              Tracing.trace (tag "RaftServer") <| fun () ->
                match postCommand agent "RaftServer" Msg.State with
                | Right (Reply.State state) -> Right state.RaftServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "RaftServer")
                  |> Either.fail

          member self.SocketServer
            with get () =
              Tracing.trace (tag "SocketServer") <| fun () ->
                match postCommand agent "SocketServer" Msg.State with
                | Right (Reply.State state) -> Right state.SocketServer
                | Left error -> Left error
                | Right other ->
                  sprintf "Unexpected response from IrisAgent: %A" other
                  |> Error.asOther (tag "SocketServer")
                  |> Either.fail

          member self.MachineStatus
            with get () =
              Tracing.trace (tag "MachineStatus") <| fun () ->
                match postCommand agent "MachineStatus" Msg.MachineStatus with
                | Right (Reply.MachineStatus status) -> Right status
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
            Tracing.trace (tag "Dispose") <| fun () ->
              notify subscriptions (Status ServiceStatus.Stopping)
              postCommand agent "Dispose" Msg.Unload
              |> ignore
              dispose agent
        }

    // *** initIdleState

    let private initIdleState (agent: (IrisAgent option) ref) (config: IrisMachine) =
      let discovery = DiscoveryService.create config
      match discovery.Start() with
      | Right _ ->
        discovery.Subscribe(fun ev ->
          match !agent with
          | Some agent -> Msg.Discovery ev |> agent.Post
          | None -> ())
        |> ignore

        // Register the services' http ip and port
        let service = registerIdleServices config discovery

        Idle { Machine = config
               DiscoveryService = discovery
               DiscoverableService = service }
      | Left error ->
        error
        |> string
        |> Logger.err (tag "startDiscoveryService")

        Idle { Machine = config
               DiscoveryService = discovery
               DiscoverableService = None }

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
          return mkIris subscriptions agent
        }
      with
      | ex -> IrisError.Other(tag "create", ex.Message) |> Either.fail

#endif
