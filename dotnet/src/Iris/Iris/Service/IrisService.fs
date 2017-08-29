namespace Iris.Service

// * Imports

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open Disruptor
open Disruptor.Dsl
open Iris.Core
open Iris.Raft
open Iris.Net
open SharpYaml.Serialization

// * IrisService

module IrisService =

  // ** tag

  let private tag (str: string) = String.format "IrisService.{0}" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: ITcpClient }

    // *** ISink

    interface ISink<IrisEvent> with
      member self.Publish (update: IrisEvent) =
        match update with
        | IrisEvent.Append(_, sm) ->
          sm
          |> Binary.encode
          |> Request.create (Guid.ofId self.Socket.ClientId)
          |> self.Socket.Request
        | _ -> ()

    // *** IDisposable

    interface IDisposable with
      member self.Dispose() =
        dispose self.Socket

  // ** IrisState

  [<NoComparison;NoEquality>]
  type private IrisState =
    { Member        : RaftMember
      Machine       : IrisMachine
      Status        : ServiceStatus
      Store         : Store
      Leader        : Leader option
      Dispatcher    : IDispatcher<IrisEvent>
      LogForwarder  : IDisposable
      LogFile       : LogFile
      ApiServer     : IApiServer
      GitServer     : IGitServer
      RaftServer    : IRaftServer
      SocketServer  : IWebSocketServer
      ClockService  : IClock
      FsWatcher     : IFsWatcher
      Subscriptions : Subscriptions
      BufferedCues  : ConcurrentDictionary<(Frame * Id),Cue>
      Disposables   : IDisposable array }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        self.Subscriptions.Clear()
        Array.iter dispose self.Disposables
        Option.iter dispose self.Leader
        dispose self.LogForwarder
        dispose self.ApiServer
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.ClockService
        dispose self.SocketServer
        dispose self.Dispatcher
        dispose self.LogFile

  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  // ** isLeader

  /// <summary>
  ///   isLeader
  /// </summary>
  /// <param name="name">type</param>
  /// <param name="name">type</param>
  /// <returns>returns</returns>
  let private isLeader (store: IAgentStore<IrisState>) =
    store.State.RaftServer.IsLeader

  // ** persistWithLogging

  /// <summary>
  ///   Persiste a state machine command to disk and log results.
  /// </summary>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <param name="sm">StateMachine</param>
  /// <returns>unit</returns>
  let private persistWithLogging (store: IAgentStore<IrisState>) sm =
    match Persistence.persistEntry store.State.Store.State sm with
    | Right () ->
      string sm
      |> String.format "Successfully persisted command {0} to disk"
      |> Logger.debug (tag "statePersistor")
    | Left error ->
      error
      |> String.format "Error persisting command to disk: {0}"
      |> Logger.err (tag "statePersistor")

  //  ____  _            _ _
  // |  _ \(_)_ __   ___| (_)_ __   ___
  // | |_) | | '_ \ / _ \ | | '_ \ / _ \
  // |  __/| | |_) |  __/ | | | | |  __/
  // |_|   |_| .__/ \___|_|_|_| |_|\___|
  //         |_|
  //  _____                 _   _
  // |  ___|   _ _ __   ___| |_(_) ___  _ __  ___
  // | |_ | | | | '_ \ / __| __| |/ _ \| '_ \/ __|
  // |  _|| |_| | | | | (__| |_| | (_) | | | \__ \
  // |_|   \__,_|_| |_|\___|\__|_|\___/|_| |_|___/

  // ** stateMutator

  /// <summary>
  ///   Dispatch the current event on the store, thereby globally mutating its state.
  /// </summary>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <param name=""></param>
  /// <param name=""></param>
  /// <param name="cmd">IrisEvent</param>
  /// <returns>unit</returns>
  let private stateMutator (store: IAgentStore<IrisState>) _ _ = function
    | IrisEvent.Append(_, cmd) -> store.State.Store.Dispatch cmd
    | _ -> ()

  // ** statePersistor

  /// <summary>
  ///   Persists events marked as non-volatile to disk, possibly committing changes to git.
  /// </summary>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <param name=""></param>
  /// <param name=""></param>
  /// <param name="cmd">IrisEvent</param>
  /// <returns>unit</returns>
  let private statePersistor (store: IAgentStore<IrisState>) _ _ = function
      | IrisEvent.Append(_, sm) when sm.PersistenceStrategy = PersistenceStrategy.Save ->
        if isLeader store then
          do persistWithLogging store sm

      | IrisEvent.Append(_, sm) when sm.PersistenceStrategy = PersistenceStrategy.Commit ->
        if isLeader store then
          do persistWithLogging store sm
          let state= store.State
          //   ____                          _ _
          //  / ___|___  _ __ ___  _ __ ___ (_) |_
          // | |   / _ \| '_ ` _ \| '_ ` _ \| | __|
          // | |__| (_) | | | | | | | | | | | | |_
          //  \____\___/|_| |_| |_|_| |_| |_|_|\__|
          match Persistence.commitChanges state.Store.State with
          | Right (repo, commit) ->
            commit.Sha
            |> String.format "Successfully committed changes in: {0}"
            |> Logger.debug (tag "statePersistor")
            repo
            |> Persistence.ensureRemotes
                state.RaftServer.MemberId
                state.Store.State.Project
                state.RaftServer.Raft.Peers
            |> Persistence.pushChanges
            |> Map.iter
              (fun name err ->
                sprintf "could not push to %s: %O" name err
                |> Logger.err (tag "statePersistor"))
            dispose repo
          | Left error ->
            error
            |> String.format "Error committing changes to disk: {0}"
            |> Logger.err (tag "statePersistor")
      | _ -> ()

  // ** logPersistor

  /// <summary>
  ///   Write all logged messages to a machine-local log file.
  /// </summary>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <param name=""></param>
  /// <param name=""></param>
  /// <param name="cmd">IrisEvent</param>
  /// <returns>unit</returns>
  let private logPersistor (store: IAgentStore<IrisState>) _ _ (cmd: IrisEvent) =
    match cmd with
    | IrisEvent.Append(_, LogMsg log) ->
      match LogFile.write store.State.LogFile log with
      | Right _ -> ()
      | Left error ->
        error
        |> string
        |> Logger.err (tag "logPersistor")
    | _ -> ()

  // ** createPublisher

  let private createPublisher (sink: ISink<IrisEvent>) _ _ = sink.Publish

  // ** dispatchUpdates

  let private dispatchUpdates (state: IrisState) (cue: Cue) =
    Array.iter
      (fun slices ->
        (Origin.Service, UpdateSlices slices)
        |> IrisEvent.Append
        |> state.Dispatcher.Dispatch)
      cue.Slices

  // ** maybeDispatchUpdate

  let private maybeDispatchUpdate (current: Frame) (state: IrisState) =
    for KeyValue((desired, id), cue) in state.BufferedCues.ToArray() do
      if desired <= current then
        dispatchUpdates state cue
        state.BufferedCues.TryRemove((desired,id)) |> ignore

  // ** commandResolver

  let private commandResolver (store: IAgentStore<IrisState>) =
    let mutable current = 0<frame>
    fun _ _ -> function
      | IrisEvent.Append(_, UpdateClock tick) ->
        current <- int tick * 1<frame>
        maybeDispatchUpdate current store.State
      | IrisEvent.Append(_, CallCue cue) ->
        let key = (current, cue.Id)
        if not (store.State.BufferedCues.ContainsKey key) then
          store.State.BufferedCues.TryAdd(key, cue) |> ignore
        maybeDispatchUpdate current store.State
      | _ -> ()

  // ** subscriptionNotifier

  let private subscriptionNotifier (store: IAgentStore<IrisState>) =
    fun _ _ -> Observable.onNext store.State.Subscriptions

  // ** preActions

  let private preActions (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (stateMutator   store) |]

  // ** processors

  let private processors (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (statePersistor store)
       Pipeline.createHandler (logPersistor   store) |]

  // ** publishers

  let private publishers (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (createPublisher store.State.ApiServer)
       Pipeline.createHandler (createPublisher store.State.SocketServer)
       Pipeline.createHandler (commandResolver store) |]

  // ** postActions

  let private postActions (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (subscriptionNotifier store) |]

  // ** sendLocalData

  /// <summary>
  ///   Send local data to leader upon connection.
  /// </summary>
  /// <param name="socket">ITcpClient</param>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <returns>unit</returns>
  /// <remarks>
  ///   <para>
  ///     Some pieces of data are intrinsically local to the service instance, such as connected
  ///     browser sessions or locally connected client instances. These pieces of data need to be
  ///     replicated to the leader once connected. IF those clients/sessions already exist, they
  ///     will simply be ignored.
  ///   </para>
  /// </remarks>
  let private sendLocalData (socket: ITcpClient) (store: IAgentStore<IrisState>) =
    if (store.State.SocketServer.Sessions.Count + store.State.ApiServer.Clients.Count) > 0 then
      let sessions =
        store.State.SocketServer.Sessions
        |> Map.toList
        |> List.map (snd >> AddSession)
      let clients =
        store.State.ApiServer.Clients
        |> Map.toList
        |> List.map (snd >> AddClient)
      let batch =
        List.append sessions clients
        |> StateMachineBatch
        |> CommandBatch

      (clients.Length,sessions.Length)
      |> String.format "sending batch command with {0} (clients,session) "
      |> Logger.debug (tag "sendLocalData")

      batch
      |> RaftRequest.AppendEntry
      |> Binary.encode
      |> Request.create (Guid.ofId socket.ClientId)
      |> socket.Request
    else
      store.State.RaftServer.RaftState
      |> String.format "Nothing to send ({0})"
      |> Logger.debug (tag "sendLocalData")

  // ** handleLeaderEvents

  /// <summary>
  ///   Handle events happening on the socket connection to the current leader.
  /// </summary>
  /// <param name="socket">ITcpClient</param>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <returns>unit</returns>
  let private handleLeaderEvents socket store = function
    | TcpClientEvent.Connected _ ->
      do sendLocalData socket store
    // | TcpClientEvent. -> ()
    | _ -> ()

  // ** makeLeader

  /// <summary>
  ///   Create a communication socket with the current Raft leader. Its important to note that
  ///   the current members Id *must* be used to set up the client socket.
  /// </summary>
  /// <param name="leader">RaftMember</param>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <returns>Leader option</returns>
  let private makeLeader (leader: RaftMember) (store: IAgentStore<IrisState>) =
    let socket = TcpClient.create {
      ClientId = store.State.Member.Id  // IMPORTANT: this must be the current member's Id
      PeerAddress = leader.IpAddr
      PeerPort = leader.Port
      Timeout = int Constants.REQ_TIMEOUT * 1<ms>
    }
    handleLeaderEvents socket store
    |> socket.Subscribe
    |> ignore
    socket.Connect()
    Some { Member = leader; Socket = socket }

  // ** processEvent

  /// <summary>Process events marked DispatchStrategy.Process</summary>
  /// <param name="store">IAgentStore<IrisState></param>
  /// <param name="ev">IrisEvent</param>
  /// <returns>unit</returns>
  /// <remarks>
  ///   <para>
  ///     Process IrisEvents that require special treatment. Events that need to be treated
  ///     differently than normal state machine comand events come from RaftServer are used
  ///     to e.g. wire up communication with the leader for forwarding state machine commands to
  ///     the leader.
  ///   </para>
  /// </remarks>
  let private processEvent (store: IAgentStore<IrisState>) ev =
    Observable.onNext store.State.Subscriptions ev
    match ev with
    | IrisEvent.EnterJointConsensus changes ->
      changes
      |> Array.map
        (function
          | ConfigChange.MemberAdded mem ->
            mem
            |> Member.getId
            |> String.format "added {0}"
          | ConfigChange.MemberRemoved mem ->
            mem
            |> Member.getId
            |> String.format "removed {0}")
      |> Array.fold (fun s id -> s + " " + id) "Joint consensus with: "
      |> Logger.debug (tag "processEvent")

    | IrisEvent.ConfigurationDone mems ->
      mems
      |> Array.map (Member.getId >> string)
      |> Array.fold (fun s id -> s + " " + id) "New Configuration with: "
      |> Logger.debug (tag "processEvent")

    | IrisEvent.StateChanged (oldstate, newstate) ->
      newstate
      |> sprintf "Raft state changed from %A to %A" oldstate
      |> Logger.debug (tag "processEvent")

    | IrisEvent.LeaderChanged leader ->

      leader
      |> String.format "Leader changed to {0}"
      |> Logger.debug (tag "leaderChanged")

      Option.iter dispose store.State.Leader

      let newLeader =
        // create redirect socket if we have new leader other than this current node
        if Option.isSome leader && leader <> (Some store.State.Member.Id) then
          match store.State.RaftServer.Leader with
          | Some leader ->
            makeLeader leader store
          | None ->
            "Could not start re-direct socket: no leader"
            |> Logger.debug (tag "leaderChanged")
            None
        else None

      store.Update { store.State with Leader = newLeader }

    | IrisEvent.PersistSnapshot log ->
      match Persistence.persistSnapshot store.State.Store.State log with
      | Left error -> Logger.err (tag "persistSnapshot") (string error)
      | _ -> ()

    | IrisEvent.RaftError _ | _ -> ()

  // ** forwardCommand

  let private forwardCommand (store: IAgentStore<IrisState>) cmd =
    match store.State.Leader with
    | Some leader ->
      cmd
      |> RaftRequest.AppendEntry
      |> Binary.encode
      |> Request.create (Guid.ofId store.State.Member.Id)
      |> leader.Socket.Request
    | None ->
      "Could not forward command; No Leader"
      |> Logger.err (tag "forwardCommand")

  // ** handleAppend

  let private handleAppend (store: IAgentStore<IrisState>) cmd =
    if isLeader store
    then do store.State.RaftServer.Append cmd
    else do forwardCommand store cmd

  // ** replicateEvent

  let private replicateEvent (store: IAgentStore<IrisState>) = function
    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __ ___
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
    // | |  | |  __/ | | | | | |_) |  __/ |  \__ \
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |___/

    | Append (_, AddMember mem) ->
      if isLeader store then store.State.RaftServer.AddMember mem

    | Append (_, RemoveMember mem) ->
      if isLeader store then store.State.RaftServer.RemoveMember mem.Id

    //  ____             _        _
    // / ___|  ___   ___| | _____| |_
    // \___ \ / _ \ / __| |/ / _ \ __|
    //  ___) | (_) | (__|   <  __/ |_
    // |____/ \___/ \___|_|\_\___|\__|

    // first, send a snapshot to the new browser session to bootstrap it
    | SessionOpened id ->
      store.State.Store.State
      |> DataSnapshot
      |> store.State.SocketServer.Send id
      |> ignore

    // next, replicate AddSession to other Raft nodes
    | Append (Origin.Web id, AddSession session) ->
      session
      |> store.State.SocketServer.BuildSession id
      |> Either.iter (AddSession >> handleAppend store)

    // replicate a RemoveSession command if the session exists
    | SessionClosed id ->
      store.State.Store.State.Sessions
      |> Map.tryFind id
      |> Option.iter (RemoveSession >> handleAppend store)

    //     _                               _
    //    / \   _ __  _ __   ___ _ __   __| |
    //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |
    //  / ___ \| |_) | |_) |  __/ | | | (_| |
    // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|
    //  _the_  |_|   |_| base case...

    | Append (_, other) -> handleAppend store other

    //   ___  _   _
    //  / _ \| |_| |__   ___ _ __
    // | | | | __| '_ \ / _ \ '__|
    // | |_| | |_| | | |  __/ |
    //  \___/ \__|_| |_|\___|_|

    | other -> ignore other

  // ** publishEvent

  let private publishEvent (pipeline: IPipeline<IrisEvent>) cmd =
    match cmd with
    | IrisEvent.Append(Origin.Raft, SetLogLevel level) ->
      do Logger.setLevel level
    | _ -> ()
    pipeline.Push cmd

  // ** dispatchEvent

  let private dispatchEvent store pipeline cmd =
    cmd |> dispatchStrategy |> function
    | Process   -> processEvent store cmd
    | Replicate -> replicateEvent store cmd
    | Ignore    -> Observable.onNext store.State.Subscriptions cmd
    | Publish   -> publishEvent pipeline cmd

  // ** createDispatcher

  let private createDispatcher (store: IAgentStore<IrisState>) =
    let mutable pipeline = Unchecked.defaultof<IPipeline<IrisEvent>>
    let mutable status = ServiceStatus.Stopped

    { new IDispatcher<IrisEvent> with
        member dispatcher.Dispatch(cmd: IrisEvent) =
          if Service.isRunning status then
            dispatchEvent store pipeline cmd

        member dispatcher.Start() =
          if Service.isStopped status then
            pipeline <- Pipeline.create {
              PreActions  = preActions store
              Processors  = processors store
              Publishers  = publishers store
              PostActions = postActions store
            }
            status <- ServiceStatus.Running

        member dispatcher.Status
          with get () = status

        member dispatcher.Dispose() =
          if Service.isRunning status then
            dispose pipeline }

  // ** retrieveSnapshot

  let private retrieveSnapshot (state: IrisState) =
    let path = Constants.RAFT_DIRECTORY <.>
               Constants.SNAPSHOT_FILENAME +
               Constants.ASSET_EXTENSION
    match Asset.read path with
    | Right str ->
      try
        let serializer = Serializer()
        let yml = serializer.Deserialize<SnapshotYaml>(str)

        let members =
          match Config.getActiveSite state.Store.State.Project.Config with
          | Some site -> site.Members |> Map.toArray |> Array.map snd
          | _ -> [| |]

        Snapshot ( Id yml.Id
                 , yml.Index
                 , yml.Term
                 , yml.LastIndex
                 , yml.LastTerm
                 , members
                 , DataSnapshot state.Store.State
                 )
        |> Some
      with
        | exn ->
          exn.Message
          |> Logger.err (tag "retrieveSnapshot")
          None

    | Left error ->
      error
      |> string
      |> Logger.err (tag "retrieveSnapshot")
      None

  // ** persistSnapshot

  let private persistSnapshot (state: IrisState) (log: RaftLogEntry) =
    match Persistence.persistSnapshot state.Store.State log with
    | Left error -> Logger.err (tag "persistSnapshot") (string error)
    | _ -> ()
    state

  // ** makeRaftCallbacks

  let private makeRaftCallbacks (store: IAgentStore<IrisState>) =
    { new IRaftSnapshotCallbacks with
        member self.PrepareSnapshot () = Some store.State.Store.State
        member self.RetrieveSnapshot () = retrieveSnapshot store.State }

  // ** makeApiCallbacks

  let private makeApiCallbacks (store: IAgentStore<IrisState>) =
    { new IApiServerCallbacks with
        member self.PrepareSnapshot () = store.State.Store.State }

  // ** isValidPassword

  let private isValidPassword (user: User) (password: Password) =
    let password = Crypto.hashPassword password user.Salt
    password = user.Password

  // ** forwardEvent

  let inline private forwardEvent (constr: ^a -> IrisEvent) (dispatcher: IDispatcher<IrisEvent>) =
    constr >> dispatcher.Dispatch

  // ** withValidCredentials

  let private withValidCredentials pw f = function
    | Some user when isValidPassword user pw -> f user
    | _ ->
      "Login rejected"
      |> Error.asProjectError (tag "loadProject")
      |> Either.fail

  // ** updateSite

  let private updateSite state (serviceOptions: IrisServiceOptions) =
    match serviceOptions.SiteId with
    | Some site ->
      let site =
        state.Project.Config.Sites
        |> Array.tryFind (fun s -> s.Id = site)
        |> function Some s -> s | None -> ClusterConfig.Default

      // Add current machine if necessary
      // taking the default ports from MachineConfig
      let site =
        let machineId = serviceOptions.Machine.MachineId
        if Map.containsKey machineId site.Members
        then site
        else
          let selfMember =
            { Member.create(machineId) with
                IpAddr  = serviceOptions.Machine.BindAddress
                GitPort = serviceOptions.Machine.GitPort
                WsPort  = serviceOptions.Machine.WsPort
                ApiPort = serviceOptions.Machine.ApiPort
                Port    = serviceOptions.Machine.RaftPort }
          { site with Members = Map.add machineId selfMember site.Members }

      let cfg = state.Project.Config |> Config.addSiteAndSetActive site
      { state with Project = { state.Project with Config = cfg }}
    | None -> state

  // ** makeState

  let private makeState store state serviceOptions (user: User) =
    either {
      let subscriptions = Subscriptions()
      let state = updateSite state serviceOptions

      // This will fail if there's no ActiveSite set up in state.Project.Config
      // The frontend needs to handle that case
      let! mem = Config.selfMember state.Project.Config

      // ensure that we have all other nodes set-up correctly
      do! Project.updateRemotes state.Project

      let clockService = Clock.create ()
      do clockService.Stop()

      let! raftServer =
        store
        |> makeRaftCallbacks
        |> RaftServer.create state.Project.Config

      let! socketServer = WebSocketServer.create mem
      let! apiServer =
        store
        |> makeApiCallbacks
        |> ApiServer.create mem state.Project.Id

      let fsWatcher = FsWatcher.create state.Project

      // IMPORTANT: use the projects path here, not the path to project.yml
      let gitServer = GitServer.create mem state.Project

      let dispatcher = createDispatcher store

      let logForwarder =
        let mkev log =
          IrisEvent.Append(Origin.Service, LogMsg log)
        Logger.subscribe (forwardEvent mkev dispatcher)

      // wiring up the sources
      let disposables = [|
        fsWatcher.Subscribe    (forwardEvent id dispatcher)
        gitServer.Subscribe    (forwardEvent id dispatcher)
        apiServer.Subscribe    (forwardEvent id dispatcher)
        socketServer.Subscribe (forwardEvent id dispatcher)
        raftServer.Subscribe   (forwardEvent id dispatcher)
        clockService.Subscribe (forwardEvent id dispatcher)
      |]

      let! logFile =
        LogFile.create
          serviceOptions.Machine.MachineId
          serviceOptions.Machine.LogDirectory

      return
        { Member         = mem
          Machine        = serviceOptions.Machine
          Leader         = None
          Dispatcher     = dispatcher
          LogFile        = logFile
          LogForwarder   = logForwarder
          Status         = ServiceStatus.Starting
          Store          = Store(state)
          ApiServer      = apiServer
          GitServer      = gitServer
          RaftServer     = raftServer
          SocketServer   = socketServer
          ClockService   = clockService
          FsWatcher      = fsWatcher
          BufferedCues   = ConcurrentDictionary()
          Subscriptions  = subscriptions
          Disposables    = disposables }
    }

  // ** makeStore

  let private makeStore (serviceOptions: IrisServiceOptions) =
    either {
      let store = AgentStore.create()

      let logDir =
        if isNull (string serviceOptions.Machine.LogDirectory)
        then serviceOptions.Machine.LogDirectory
        else IrisMachine.Default.LogDirectory

      let! _ = Directory.createDirectory logDir

      let! path = Project.checkPath serviceOptions.Machine serviceOptions.ProjectName

      let! (state: State) =
        Asset.loadWithMachine path serviceOptions.Machine
        |> Either.map State.initialize

      let! updated =
        state.Users
        |> Map.tryPick (fun _ u -> if u.UserName = serviceOptions.UserName then Some u else None)
        |> withValidCredentials
            serviceOptions.Password
            (makeState store state serviceOptions)

      store.Update updated          // and feed it to the store, before we start the services
      return store
    }

  // ** start

  let private start (store: IAgentStore<IrisState>) =
    either {
      store.State.Dispatcher.Start()

      // start all services
      let result =
        either {
          do! store.State.ApiServer.Start()
          do! store.State.SocketServer.Start()
          do! store.State.GitServer.Start()
          do! store.State.RaftServer.Start()
        }

      match result with
      | Right _ ->
        { store.State with Status = ServiceStatus.Running }
        |> store.Update

        store.State.Status
        |> IrisEvent.Status
        |> Observable.onNext store.State.Subscriptions
        return ()
      | Left error ->
        { store.State with Status = ServiceStatus.Failed error }
        |> store.Update

        store.State.Status
        |> IrisEvent.Status
        |> Observable.onNext store.State.Subscriptions
        dispose store.State
        return! Either.fail error
    }

  // ** disposeService

  let private disposeService (store: IAgentStore<IrisState>) =
    dispose store.State         // dispose the state
    store.Update { store.State with Status = ServiceStatus.Disposed }

  // ** addMember

  let private addMember (store: IAgentStore<IrisState>) (mem: RaftMember) =
    (Origin.Service, AddMember mem)
    |> IrisEvent.Append
    |> store.State.Dispatcher.Dispatch

  // ** removeMember

  let private removeMember (store: IAgentStore<IrisState>) (id: Id) =
    store.State.RaftServer.Raft.Peers
    |> Map.tryFind id
    |> Option.iter
      (fun mem ->
        (Origin.Service, RemoveMember mem)
        |> IrisEvent.Append
        |> store.State.Dispatcher.Dispatch)

  // ** append

  let private append (store: IAgentStore<IrisState>) (cmd: StateMachine) =
    (Origin.Service, cmd)
    |> IrisEvent.Append
    |> store.State.Dispatcher.Dispatch

  // ** makeService

  let private makeService (store: IAgentStore<IrisState>) =
    { new IIrisService with
        member self.Start() = start store

        member self.State
          with get () = store.State.Store.State

        member self.Project
          with get () = store.State.Store.State.Project // :D

        member self.Config
          with get () = store.State.Store.State.Project.Config // :D
          and set config = failwith "set() Config"

        member self.Status
          with get () = store.State.Status

        member self.ForceElection () = store.State.RaftServer.ForceElection()

        member self.Periodic () = store.State.RaftServer.Periodic()

        member self.AddMember mem = addMember store mem

        member self.RemoveMember id = removeMember store id

        member self.Append cmd = append store cmd

        member self.GitServer
          with get () = store.State.GitServer

        member self.RaftServer
          with get () = store.State.RaftServer

        member self.SocketServer
          with get () = store.State.SocketServer

        member self.Subscribe(callback: IrisEvent -> unit) =
          Observable.subscribe callback store.State.Subscriptions

        member self.Machine
          with get () = store.State.Machine

        member self.Dispose() = disposeService store

        // member self.LeaveCluster () =
        //   Tracing.trace (tag "LeaveCluster") <| fun () ->
        //     match postCommand agent "LeaveCluster"  Msg.Leave with
        //     | Right Reply.Ok -> Right ()
        //     | Left error -> Left error
        //     | Right other ->
        //       String.format "Unexpected response from IrisAgent: {0}" other
        //       |> Error.asOther (tag "LeaveCluster")
        //       |> Either.fail

        // member self.JoinCluster ip port =
        //   Tracing.trace (tag "JoinCluster") <| fun () ->
        //     match postCommand agent "JoinCluster" (fun chan -> Msg.Join(chan,ip, port)) with
        //     | Right Reply.Ok -> Right ()
        //     | Left error  -> Left error
        //     | Right other ->
        //       String.format "Unexpected response from IrisAgent: {0}" other
        //       |> Error.asOther (tag "JoinCluster")
        //       |> Either.fail
      }

  // ** create

  let create (iris: IrisServiceOptions) =
    either {
      let! store = makeStore iris
      return makeService store
    }
