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
      BufferedCues  : ConcurrentDictionary<(Frame * CueId),Cue>
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

  let private isLeader (store: IAgentStore<IrisState>) =
    store.State.RaftServer.IsLeader

  // ** persistWithLogging

  /// Persiste a state machine command to disk and log results.
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

  // ** stateMutator

  /// Dispatch the current event on the store, thereby globally mutating its state.
  let private stateMutator (store: IAgentStore<IrisState>) =
    fun _ _ -> function
    | IrisEvent.Append(_, Command AppCommand.Undo) -> store.State.Store.Undo()
    | IrisEvent.Append(_, Command AppCommand.Redo) -> store.State.Store.Redo()
    | IrisEvent.Append(_, cmd) -> store.State.Store.Dispatch cmd
    | _ -> ()

  // ** statePersistor

  /// Persists events marked as non-volatile to disk, possibly committing changes to git.
  let private statePersistor (store: IAgentStore<IrisState>) _ _ = function
      | IrisEvent.Append(_, sm) when sm.PersistenceStrategy = PersistenceStrategy.Save ->
        if isLeader store then do persistWithLogging store sm

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

  // ** mappingResolver

  let private mappingResolver (store: IAgentStore<IrisState>) =
    /// Group all PinMappings by the Source Id so that we may later just create new Slices
    /// values for those target sink pins and add them to a new SliceMap. This value is a local,
    /// voltale cache to make the operation faster by not recomputing the grouped value every
    /// time there is an UpdateSlice command.
    let mutable grouped =
      Map.fold
        (fun (out: Map<PinMappingId,PinMapping list>) _ (mapping: PinMapping) ->
          match Map.tryFind mapping.Source out with
          | Some lst -> Map.add mapping.Source (mapping :: lst) out
          | None -> Map.add mapping.Source [mapping] out)
        Map.empty
        store.State.Store.State.PinMappings

    fun _ _ -> function
    /// Add a new mapping to the Source-Id-grouped mappings. Remove any existing mapping with
    /// the same Id first.
    | IrisEvent.Append(_, AddPinMapping mapping)
    | IrisEvent.Append(_, UpdatePinMapping mapping) ->
      match Map.tryFind mapping.Source grouped with
      | Some mappings ->
        grouped <-
          mappings
          |> List.filter (fun (existing: PinMapping) -> existing.Id <> mapping.Id)
          |> fun mappings -> Map.add mapping.Source (mapping :: mappings) grouped
      | None ->
        grouped <- Map.add mapping.Source [mapping] grouped

    /// Remove a mapping from the grouped cache
    | IrisEvent.Append(_, RemovePinMapping mapping) ->
      match Map.tryFind mapping.Source grouped with
      | Some mappings ->
        let updated = List.filter (fun (existing: PinMapping) -> existing.Id <> mapping.Id) mappings
        if updated.Length > 0 then
          grouped <- Map.add mapping.Source updated grouped
        else
          grouped <- Map.remove mapping.Source grouped
      | None -> ()

    | IrisEvent.Append(_, UpdateSlices map) ->
      /// only engage in processing of mappings if there are any to be processed
      if not grouped.IsEmpty then
        /// produce a new SlicesMap by creating a Slices value for each of the Sinks in each mapping,
        /// *if* a corresponding mapping (current Slices's Id = mapping.Source) was found.
        let slicesMap =
          Map.fold
            (fun output pinid (slices: Slices) ->
              match Map.tryFind pinid grouped with
              | Some mappings ->
                List.fold
                  (fun (map: SlicesMap) (mapping: PinMapping) ->
                    mapping.Sinks
                    |> Set.map (flip Slices.setId slices)
                    |> Set.fold SlicesMap.add map)
                  output
                  mappings
              | None -> output)
            SlicesMap.empty
            map.Slices

        /// unless the resulting map is empty, re-queue the result as a new UpdateSlices command
        if not (SlicesMap.isEmpty slicesMap) then
          slicesMap
          |> UpdateSlices
          |> IrisEvent.appendService
          |> store.State.Dispatcher.Dispatch
    | _ -> ()

  // ** logPersistor

  /// Write all logged messages to a local log file.
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
    cue.Slices
    |> UpdateSlices.ofArray
    |> IrisEvent.appendService
    |> state.Dispatcher.Dispatch

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
    [| Pipeline.createHandler (stateMutator store) |]

  // ** processors

  let private processors (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (statePersistor  store)
       Pipeline.createHandler (mappingResolver store)
       Pipeline.createHandler (logPersistor    store) |]

  // ** publishers

  let private publishers (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (createPublisher store.State.ApiServer)
       Pipeline.createHandler (createPublisher store.State.SocketServer)
       Pipeline.createHandler (commandResolver store) |]

  // ** postActions

  let private postActions (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (subscriptionNotifier store) |]

  // ** sendLocalData

  /// ## Send local data to leader upon connection.
  ///
  /// Some pieces of data are intrinsically local to the service instance, such as connected
  /// browser sessions or locally connected client instances. These pieces of data need to be
  /// replicated to the leader once connected. IF those clients/sessions already exist, they
  /// will simply be ignored.
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

  /// Handle events happening on the socket connection to the current leader. When connected, send a
  /// command to append the locally connected clients and browser sessions to the leader.
  let private handleLeaderEvents socket store = function
    | TcpClientEvent.Connected _ ->
      do sendLocalData socket store
    // | TcpClientEvent. -> ()
    | _ -> ()

  // ** makeLeader

  /// Create a communication socket with the current Raft leader. Its important to note that
  /// the current members Id *must* be used to set up the client socket.
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

  /// ## Process IrisEvents that have special semantics.
  ///
  /// Events that need to be treated differently than normal state machine comand events come from
  /// RaftServer and are used to e.g. wire up communication with the leader for forwarding state
  /// machine commands to the leader.
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

  let private publishEvent (store: IAgentStore<IrisState>) pipeline = function
    /// globally set the loglevel to the desired value
    | IrisEvent.Append(Origin.Raft, SetLogLevel level) as cmd ->
      do Logger.setLevel level
      do Pipeline.push pipeline cmd
    /// when a cue player is created, also add a special pingroup for that player
    | IrisEvent.Append(Origin.Raft, AddCuePlayer player) as cmd ->
      player
      |> PinGroup.ofPlayer
      |> AddPinGroup
      |> IrisEvent.appendRaft
      |> store.State.Dispatcher.Dispatch
      do Pipeline.push pipeline cmd
    /// when a cue player is deleted, also delete the special pingroup for that player
    | IrisEvent.Append(Origin.Raft, RemoveCuePlayer player) as cmd ->
      player
      |> PinGroup.ofPlayer
      |> RemovePinGroup
      |> IrisEvent.appendRaft
      |> store.State.Dispatcher.Dispatch
      do Pipeline.push pipeline cmd
    /// when a pin widget is created, also add a special pingroup for that widget
    | IrisEvent.Append(Origin.Raft, AddPinWidget widget) as cmd->
      widget
      |> PinGroup.ofWidget
      |> AddPinGroup
      |> IrisEvent.appendRaft
      |> store.State.Dispatcher.Dispatch
      do Pipeline.push pipeline cmd
    /// when a pin widget is deleted, also delete the special pingroup for that widget
    | IrisEvent.Append(Origin.Raft, RemovePinWidget widget) as cmd ->
      widget
      |> PinGroup.ofWidget
      |> RemovePinGroup
      |> IrisEvent.appendRaft
      |> store.State.Dispatcher.Dispatch
      do Pipeline.push pipeline cmd
    | cmd -> do Pipeline.push pipeline cmd

  // ** dispatchEvent

  let private dispatchEvent store pipeline cmd =
    cmd |> dispatchStrategy |> function
    | Process   -> processEvent store cmd
    | Replicate -> replicateEvent store cmd
    | Ignore    -> Observable.onNext store.State.Subscriptions cmd
    | Publish   -> publishEvent store pipeline cmd

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
    match IrisData.read path with
    | Right str ->
      try
        let yml = Yaml.deserialize<SnapshotYaml> str
        let id = IrisId.Parse yml.Id
        let snapshot = DataSnapshot state.Store.State
        let members =
          match Config.getActiveSite state.Store.State.Project.Config with
          | Some site -> site.Members |> Map.toArray |> Array.map snd
          | _ -> [| |]
        (id,yml.Index ,yml.Term ,yml.LastIndex ,yml.LastTerm ,members , snapshot)
        |> Snapshot
        |> Some
      with exn ->
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
    | Some (name, site) ->
      let site =
        state.Project.Config.Sites
        |> Array.tryFind (fun s -> s.Id = site)
        |> function
        | Some s -> s
        | None -> { ClusterConfig.Default with Name = name }

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
      do! Config.validateSettings mem serviceOptions.Machine

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
        Logger.subscribe (forwardEvent (LogMsg >> IrisEvent.appendService) dispatcher)

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
        serviceOptions.Machine
        |> Asset.loadWithMachine path
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
    AddMember mem
    |> IrisEvent.appendService
    |> store.State.Dispatcher.Dispatch

  // ** removeMember

  let private removeMember (store: IAgentStore<IrisState>) (id: MemberId) =
    store.State.RaftServer.Raft.Peers
    |> Map.tryFind id
    |> Option.iter (RemoveMember >> IrisEvent.appendService >> store.State.Dispatcher.Dispatch)

  // ** append

  let private append (store: IAgentStore<IrisState>) (cmd: StateMachine) =
    cmd
    |> IrisEvent.appendService
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
