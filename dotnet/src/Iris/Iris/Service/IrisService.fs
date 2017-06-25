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

  let private tag (str: string) = String.format "IrisServiceNG.{0}" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: IClient }

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

  // ** stateMutator

  let private stateMutator (store: IAgentStore<IrisState>) _ _ (cmd: IrisEvent) =
    match cmd with
    | IrisEvent.Append(_, cmd) ->  store.State.Store.Dispatch cmd
    | _ -> ()

  // ** statePersistor

  let private statePersistor (store: IAgentStore<IrisState>) =
    fun _ _ -> function
      | IrisEvent.Append(_, sm) ->
        let state = store.State
        if state.RaftServer.IsLeader then
          match sm.PersistenceStrategy with
          | PersistenceStrategy.Save ->
            //  ____
            // / ___|  __ ___   _____
            // \___ \ / _` \ \ / / _ \
            //  ___) | (_| |\ V /  __/
            // |____/ \__,_| \_/ \___|
            match Persistence.persistEntry state.Store.State sm with
            | Right () ->
              string sm
              |> String.format "Successfully persisted command {0} to disk"
              |> Logger.debug (tag "statePersistor")
            | Left error ->
              error |> String.format "Error persisting command to disk: {0}"
              |> Logger.err (tag "statePersistor")
          | PersistenceStrategy.Commit ->
            //  ____
            // / ___|  __ ___   _____
            // \___ \ / _` \ \ / / _ \
            //  ___) | (_| |\ V /  __/
            // |____/ \__,_| \_/ \___| *and*
            match Persistence.persistEntry state.Store.State sm with
            | Right () ->
              string sm
              |> String.format "Successfully persisted command {0} to disk"
              |> Logger.debug (tag "statePersistor")
            | Left error ->
              error
              |> String.format "Error persisting command to disk: {0}"
              |> Logger.err (tag "statePersistor")
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
              Persistence.pushChanges repo
              |> Map.iter
                (fun name err ->
                  sprintf "could not push to %s: %O" name err
                  |> Logger.err (tag "statePersistor"))
              dispose repo
            | Left error ->
              error
              |> String.format "Error committing changes to disk: {0}"
              |> Logger.err (tag "statePersistor")
          | PersistenceStrategy.Ignore -> ()
      | _ -> ()

  // ** logPersistor

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
        while not (state.BufferedCues.TryRemove((desired,id)) |> fst) do
          ignore ()

  // ** commandResolver

  let private commandResolver (store: IAgentStore<IrisState>) =
    let mutable current = 0<frame>
    fun _ _ (cmd: IrisEvent) ->
      match cmd with
      | IrisEvent.Append(_, UpdateClock tick) ->
        current <- int tick * 1<frame>
        maybeDispatchUpdate current store.State
      | IrisEvent.Append(_, CallCue cue) ->
        let key = (current, cue.Id)
        if not (store.State.BufferedCues.ContainsKey key) then
          while not (store.State.BufferedCues.TryAdd(key, cue)) do
            ignore ()
        maybeDispatchUpdate current store.State
      | _ -> ()

  // ** subscriptionNotifier

  let private subscriptionNotifier (store: IAgentStore<IrisState>) =
    fun _ _ -> Observable.onNext store.State.Subscriptions

  // ** processors

  let private processors (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (stateMutator   store)
       Pipeline.createHandler (statePersistor store)
       Pipeline.createHandler (logPersistor   store) |]

  // ** publishers

  let private publishers (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (createPublisher store.State.ApiServer)
       Pipeline.createHandler (createPublisher store.State.SocketServer)
       Pipeline.createHandler (commandResolver store) |]

  // ** postActions

  let private postActions (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (subscriptionNotifier store) |]

  // ** makeLeader

  let private makeLeader (leader: RaftMember) =
    let socket = TcpClient.create {
      ClientId = leader.Id
      PeerAddress = leader.IpAddr
      PeerPort = leader.Port
      Timeout = int Constants.REQ_TIMEOUT * 1<ms>
    }
    match socket.Start() with
    | Right () ->
      (fun _ -> "TODO: setup leader socket response handler"
              |> Logger.warn (tag "makeLeader"))
      |> socket.Subscribe
      |> ignore
      Some { Member = leader; Socket = socket }
    | Left error ->
      error
      |> String.format "error creating connection for leader: {0}"
      |> Logger.err (tag "makeLeader")
      None

  // ** processEvent

  let private processEvent (store: IAgentStore<IrisState>) ev =
    Observable.onNext store.State.Subscriptions ev
    match ev with
    | IrisEvent.Configured mems ->
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
            makeLeader leader
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

  // ** replicateEvent

  let private replicateEvent (store: IAgentStore<IrisState>) = function
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
      |> Either.iter (AddSession >> store.State.RaftServer.Append)

    // replicate a RemoveSession command if the session exists
    | SessionClosed id ->
      store.State.Store.State.Sessions
      |> Map.tryFind id
      |> Option.iter (RemoveSession >> store.State.RaftServer.Append)

    //     _                               _
    //    / \   _ __  _ __   ___ _ __   __| |
    //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |
    //  / ___ \| |_) | |_) |  __/ | | | (_| |
    // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|
    //  _the_  |_|   |_| base case...

    | Append (_, AddMember mem) -> store.State.RaftServer.AddMember mem
    | Append (_, RemoveMember mem) -> store.State.RaftServer.RemoveMember mem.Id
    | Append (_, other) -> store.State.RaftServer.Append other

    //   ___  _   _
    //  / _ \| |_| |__   ___ _ __
    // | | | | __| '_ \ / _ \ '__|
    // | |_| | |_| | | |  __/ |
    //  \___/ \__|_| |_|\___|_|

    | other -> ignore other

  // ** dispatchEvent

  let private dispatchEvent (store: IAgentStore<IrisState>)
                            (pipeline: IPipeline<IrisEvent>)
                            (cmd:IrisEvent) =
    match cmd.DispatchStrategy with
    | Publish   -> pipeline.Push cmd
    | Process   -> processEvent store cmd
    | Replicate -> replicateEvent store cmd
    | Ignore    -> ()

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
            pipeline <- Pipeline.create (processors store) (publishers store) (postActions store)
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
        let serializer = new Serializer()
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

  // ** makeStore

  let private makeStore (iris: IrisServiceOptions) =
    either {
      let subscriptions = Subscriptions()
      let store = AgentStore.create()

      do Directory.createDirectory iris.Machine.LogDirectory |> ignore

      let! path = Project.checkPath iris.Machine iris.ProjectName
      let! (state: State) = Asset.loadWithMachine path iris.Machine

      let user =
        state.Users
        |> Map.tryPick (fun _ u -> if u.UserName = iris.UserName then Some u else None)

      match user with
      | Some user when isValidPassword user iris.Password ->
        let state =
          match iris.SiteId with
          | Some site ->
            let site =
              state.Project.Config.Sites
              |> Array.tryFind (fun s -> s.Id = site)
              |> function Some s -> s | None -> ClusterConfig.Default

            // Add current machine if necessary
            // taking the default ports from MachineConfig
            let site =
              let machineId = iris.Machine.MachineId
              if Map.containsKey machineId site.Members
              then site
              else
                let selfMember =
                 { Member.create(machineId) with
                      IpAddr  = iris.Machine.BindAddress
                      GitPort = iris.Machine.GitPort
                      WsPort  = iris.Machine.WsPort
                      ApiPort = iris.Machine.ApiPort
                      Port    = iris.Machine.RaftPort }
                { site with Members = Map.add machineId selfMember site.Members }

            let cfg = state.Project.Config |> Config.addSiteAndSetActive site
            { state with Project = { state.Project with Config = cfg }}
          | None -> state

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

        // IMPORTANT: use the projects path here, not the path to project.yml
        let gitServer = GitServer.create mem state.Project

        let dispatcher = createDispatcher store

        let logForwarder =
          let mkev log =
            IrisEvent.Append(Origin.Service, LogMsg log)
          Logger.subscribe (forwardEvent mkev dispatcher)

        // wiring up the sources
        let disposables = [|
          gitServer.Subscribe(forwardEvent id dispatcher)
          apiServer.Subscribe(forwardEvent id dispatcher)
          socketServer.Subscribe(forwardEvent id dispatcher)
          raftServer.Subscribe(forwardEvent id dispatcher)
          clockService.Subscribe(forwardEvent id dispatcher)
        |]

        let! logFile = LogFile.create iris.Machine.MachineId iris.Machine.LogDirectory

        // set up the agent state
        { Member         = mem
          Machine        = iris.Machine
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
          BufferedCues   = ConcurrentDictionary()
          Subscriptions  = subscriptions
          Disposables    = disposables }
        |> store.Update                  // and feed it to the store, before we start the services

        return store
      | _ ->
        return!
          "Login rejected"
          |> Error.asProjectError (tag "loadProject")
          |> Either.fail
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
