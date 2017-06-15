namespace Iris.Service

// * Imports

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open Disruptor
open Disruptor.Dsl
open ZeroMQ
open Iris.Core
open Iris.Raft
open Iris.Zmq
open SharpYaml.Serialization

// * Pipeline

module Pipeline =

  // ** bufferSize

  [<Literal>]
  let private BufferSize = 1024

  // ** scheduler

  let private scheduler = TaskScheduler.Default

  // ** tag

  let private tag (str: string) = String.format "Pipeline.{0}" str

  // ** createDisruptor

  let private createDisruptor () =
    Dsl.Disruptor<PipelineEvent<IrisEvent>>(PipelineEvent<IrisEvent>, BufferSize, scheduler)

  // ** handleEventsWith

  let private handleEventsWith (handlers: IHandler<IrisEvent> [])
                               (disruptor: Disruptor<PipelineEvent<IrisEvent>>) =
    disruptor.HandleEventsWith handlers

  // ** thenDo

  let private thenDo (handlers: IHandler<IrisEvent>[]) (group: IHandlerGroup<IrisEvent>) =
    group.Then handlers

  // ** insertInto

  let private insertInto (ringBuffer: RingBuffer<PipelineEvent<IrisEvent>>) (cmd: IrisEvent) =
    let seqno = ringBuffer.Next()
    let entry = ringBuffer.[seqno]
    entry.Event <- Some cmd
    ringBuffer.Publish(seqno)

  // ** clearEvent

  let private clearEvent =
    [|  { new IHandler<IrisEvent> with
           member handler.OnEvent(ev: PipelineEvent<IrisEvent>, _, _) =
             ev.Clear() } |]

  // ** createHandler

  let createHandler (f: EventProcessor<IrisEvent>) : IHandler<IrisEvent> =
    { new IHandler<IrisEvent> with
        member handler.OnEvent(ev: PipelineEvent<IrisEvent>, seqno, eob) =
          Option.iter (f seqno eob) ev.Event }

  // ** create

  let create (processors: IHandler<IrisEvent>[]) (publish: IHandler<IrisEvent>[]) =
    let disruptor = createDisruptor()

    disruptor
    |> handleEventsWith processors
    |> thenDo publish
    |> thenDo clearEvent
    |> ignore

    let ringBuffer = disruptor.Start()

    { new IPipeline<IrisEvent> with
        member pipeline.Push(cmd: IrisEvent) =
          insertInto ringBuffer cmd

        member pipeline.Dispose() =
          disruptor.Shutdown() }

// * Dispatcher

module Dispatcher =

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
          |> RawClientRequest.create
          |> self.Socket.Request
          |> Either.mapError (string >> Logger.err (tag "Forward"))
          |> ignore
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
      Resolver      : IResolver
      Subscriptions : Subscriptions
      Disposables   : IDisposable array
      Context       : ZContext }

    // *** Dispose

    interface IDisposable with
      member self.Dispose() =
        self.Subscriptions.Clear()
        Array.iter dispose self.Disposables
        Option.iter dispose self.Leader
        dispose self.Resolver
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

  let private statePersistor (store: IAgentStore<IrisState>) _ _ (cmd: IrisEvent) =
    match cmd with
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
            |> Logger.debug (tag "persistLog")
          | Left error ->
            error |> String.format "Error persisting command to disk: {0}"
            |> Logger.err (tag "persistLog")
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
            |> Logger.debug (tag "persistLog")
          | Left error ->
            error
            |> String.format "Error persisting command to disk: {0}"
            |> Logger.err (tag "persistLog")
          //   ____                          _ _
          //  / ___|___  _ __ ___  _ __ ___ (_) |_
          // | |   / _ \| '_ ` _ \| '_ ` _ \| | __|
          // | |__| (_) | | | | | | | | | | | | |_
          //  \____\___/|_| |_| |_|_| |_| |_|_|\__|
          match Persistence.commitChanges state.Store.State with
          | Right commit ->
            commit.Sha
            |> String.format "Successfully committed changes in: {0}"
            |> Logger.debug (tag "persistLog")
          | Left error ->
            error
            |> String.format "Error committing changes to disk: {0}"
            |> Logger.err (tag "persistLog")
        | PersistenceStrategy.Ignore -> ignore cmd
    | _ -> ignore cmd

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

  let private createPublisher (sink: ISink<IrisEvent>) =
    fun (seqno: int64) (eob: bool) (cmd: IrisEvent) ->
      sink.Publish cmd

  // ** commandResolver

  let private commandResolver (sink: ISink<IrisEvent>) =
    fun (seqno: int64) (eob: bool) (cmd: IrisEvent) ->
      printfn "resolving commands"
      sink.Publish cmd

  // ** processors

  let private processors (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (stateMutator   store)
       Pipeline.createHandler (statePersistor store)
       Pipeline.createHandler (logPersistor   store) |]

  // ** publishers

  let private publishers (store: IAgentStore<IrisState>) =
    [| Pipeline.createHandler (createPublisher store.State.ApiServer)
       Pipeline.createHandler (createPublisher store.State.SocketServer)
       Pipeline.createHandler (commandResolver store.State.ApiServer) |]

  // ** dispatchEvent

  let private dispatchEvent (store: IAgentStore<IrisState>)
                            (pipeline: IPipeline<IrisEvent>)
                            (cmd:IrisEvent) =
    match cmd.DispatchStrategy with
    | Publish   -> pipeline.Push cmd
    | Process   -> pipeline.Push cmd
    | Replicate -> store.State.RaftServer.Publish cmd
    | Ignore    -> ()

    Observable.onNext store.State.Subscriptions cmd

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
            pipeline <- Pipeline.create (processors store) (publishers store)
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

  let private makeStore  (context: ZContext) (iris: IrisServiceOptions) =
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

        let clockService = Clock.create ()
        do clockService.Stop()

        let! raftServer =
          store
          |> makeRaftCallbacks
          |> RaftServer.create context state.Project.Config

        let! socketServer = WebSocketServer.create mem
        let! apiServer =
          store
          |> makeApiCallbacks
          |> ApiServer.create context mem state.Project.Id

        // IMPORTANT: use the projects path here, not the path to project.yml
        let gitServer = GitServer.create mem state.Project

        let cueResolver = Resolver.create ()

        let dispatcher = createDispatcher store

        let logForwarder =
          let mkev log =
            IrisEvent.Append(Origin.Service, LogMsg log)
          Logger.subscribe (forwardEvent mkev dispatcher)

        // wiring up the sources
        let disposables = [|
          gitServer.Subscribe(forwardEvent IrisEvent.Git dispatcher)
          apiServer.Subscribe(forwardEvent id dispatcher)
          socketServer.Subscribe(forwardEvent id dispatcher)
          raftServer.Subscribe(forwardEvent id dispatcher)
          cueResolver.Subscribe(forwardEvent id dispatcher)
          clockService.Subscribe(forwardEvent id dispatcher)
          logForwarder
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
          Context        = context
          ApiServer      = apiServer
          GitServer      = gitServer
          RaftServer     = raftServer
          SocketServer   = socketServer
          ClockService   = clockService
          Resolver       = cueResolver
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
          do! store.State.RaftServer.Start()
          do! store.State.ApiServer.Start()
          do! store.State.SocketServer.Start()
          do! store.State.GitServer.Start()
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

  let create ctx (iris: IrisServiceOptions) =
    either {
      let! store = makeStore ctx iris
      return makeService store
    }
