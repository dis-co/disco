namespace Iris.Service

// * Imports

open System
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

  let private bufferSize = 1024

  // ** scheduler

  let private scheduler = TaskScheduler.Default

  // ** createDirectory

  let private logtag (str: string) =
    String.Format("Pipeline.{0}", str)

  // ** createDisruptor

  let private createDisruptor () =
    Dsl.Disruptor<PipelineEvent<'t>>(PipelineEvent<'t>, bufferSize, scheduler)

  // ** handleEventsWith

  let private handleEventsWith (handlers: IHandler<'t> [])
                               (disruptor: Disruptor<PipelineEvent<'t>>) =
    disruptor.HandleEventsWith handlers

  // ** thenDo

  let private thenDo (handlers: IHandler<'t>[]) (group: IHandlerGroup<'t>) =
    group.Then handlers

  // ** insertInto

  let private insertInto (ringBuffer: RingBuffer<PipelineEvent<'t>>) (cmd: 't) =
    let seqno = ringBuffer.Next()
    let entry = ringBuffer.[seqno]
    entry.Event <- Some cmd
    ringBuffer.Publish(seqno)

  // ** clearEvent

  let private clearEvent =
    [|  { new IHandler<'t> with
           member handler.OnEvent(ev: PipelineEvent<'t>, _, _) =
             ev.Clear() } |]

  // ** createHandler

  let createHandler<'t> (f: EventProcessor<'t>) : IHandler<'t> =
    { new IHandler<'t> with
        member handler.OnEvent(ev: PipelineEvent<'t>, seqno, eob) =
          Option.iter (f seqno eob) ev.Event }

  // ** create

  let create (processors: IHandler<'t>[]) (publish: IHandler<'t>[]) =
    let disruptor = createDisruptor()

    disruptor
    |> handleEventsWith processors
    |> thenDo publish
    |> thenDo clearEvent
    |> ignore

    let ringBuffer = disruptor.Start()

    { new IPipeline<'t> with
        member pipeline.Push(cmd: 't) =
          insertInto ringBuffer cmd

        member pipeline.Dispose() =
          disruptor.Shutdown() }


// * Dispatcher

module Dispatcher =

  // ** stateMutator

  let private stateMutator (store: IAgentStore<_>) (seqno: int64) (eob: bool) (cmd: IrisEvent) =
    printfn "dispatch cmd on Store"

  // ** statePersistor

  let private statePersistor (seqno: int64) (eob: bool) (cmd: IrisEvent) =
    printfn "persisting state now"

  // ** logPersistor

  let private logPersistor (seqno: int64) (eob: bool) (cmd: IrisEvent) =
    printfn "writing log to disk"

  // ** createPublisher

  let private createPublisher (sink: ISink<IrisEvent>) =
    fun (seqno: int64) (eob: bool) (cmd: IrisEvent) ->
      sink.Publish Origin.Raft cmd

  // ** commandResolver

  let private commandResolver (sink: ISink<IrisEvent>) =
    fun (seqno: int64) (eob: bool) (cmd: IrisEvent) ->
      printfn "resolving commands"
      sink.Publish Origin.Raft cmd

  // ** processors

  let private processors (store: IAgentStore<_>) : IHandler<IrisEvent>[] =
    [| Pipeline.createHandler<IrisEvent> (stateMutator store)
       Pipeline.createHandler<IrisEvent> statePersistor
       Pipeline.createHandler<IrisEvent> logPersistor |]

  // ** publishers

  let private publishers (sinks: IIrisSinks<IrisEvent>) =
    [| Pipeline.createHandler<IrisEvent> (createPublisher sinks.Api)
       Pipeline.createHandler<IrisEvent> (createPublisher sinks.WebSocket)
       Pipeline.createHandler<IrisEvent> (commandResolver sinks.Api) |]

  // ** dispatchEvent

  let private dispatchEvent (sinks: IIrisSinks<IrisEvent>)
                            (pipeline: IPipeline<IrisEvent>)
                            (cmd:IrisEvent) =
    match cmd.DispatchStrategy with
    | Process   -> pipeline.Push cmd
    | Replicate -> sinks.Raft.Publish Origin.Service cmd
    | Ignore    -> ()

  // ** create

  let create (store: IAgentStore<_>) (sinks: IIrisSinks<IrisEvent>) =
    let pipeline = Pipeline.create (processors store) (publishers sinks)

    { new IDispatcher<IrisEvent> with
        member dispatcher.Dispatch(cmd: IrisEvent) =
          dispatchEvent sinks pipeline cmd

        member dispatcher.Dispose() =
          dispose pipeline }

// * IrisNG

module IrisNG =

  // ** tag

  let private tag (str: string) = String.format "IrisServiceNG.{0}" str

  // ** disposeAll

  let inline private disposeAll (disposables: IDisposable array) =
    Array.iter dispose disposables

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** Leader

  [<NoComparison;NoEquality>]
  type private Leader =
    { Member: RaftMember
      Socket: IClient }

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
        disposeAll self.Disposables
        Option.iter dispose self.Leader
        dispose self.Resolver
        dispose self.LogForwarder
        dispose self.ApiServer
        dispose self.GitServer
        dispose self.RaftServer
        dispose self.ClockService
        dispose self.SocketServer
        dispose self.Dispatcher

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
        clockService.Stop()

        let! raftServer = RaftServer.create context state.Project.Config {
            new IRaftSnapshotCallbacks with
              member self.PrepareSnapshot () = Some store.State.Store.State
              member self.RetrieveSnapshot () = retrieveSnapshot store.State
          }

        let! socketServer = WebSocketServer.create mem
        let! apiServer = ApiServer.create context mem state.Project.Id {
            new IApiServerCallbacks with
              member self.PrepareSnapshot () = store.State.Store.State
          }

        let logForwarder =
          let lobs =
            Logger.subscribe
              (fun log ->
                // Explanation:
                //
                // To prevent logs from other hosts being looped around endlessly, we only
                // publish messages on the on api that emenate either from this service or
                // any connected sessions.
                if not (log.Tier = Tier.Service && log.Id <> iris.Machine.MachineId) then
                  apiServer.Update Origin.Service (LogMsg log)
                socketServer.Broadcast (LogMsg log) |> ignore)
          { new IDisposable with member self.Dispose () = dispose lobs }

        // IMPORTANT: use the projects path here, not the path to project.yml
        let gitServer = GitServer.create mem state.Project

        let cueResolver = Resolver.create ()

        // setting up the sinks
        let sinks =
          { new IIrisSinks<IrisEvent> with
              member sinks.Raft = unbox raft
              member sinks.Api = unbox apiServer
              member sinks.WebSocket = unbox socketServer }

        // creating the pipeline
        let dispatcher = Dispatcher.create store sinks

        // wiring up the sources
        let disposables = [|
          gitServer.Subscribe(forwardEvent IrisEvent.Git dispatcher)
          apiServer.Subscribe(forwardEvent id dispatcher)
          socketServer.Subscribe(forwardEvent id dispatcher)
          raftServer.Subscribe(forwardEvent id dispatcher)
          cueResolver.Subscribe(forwardEvent id dispatcher)
          clockService.Subscribe(forwardEvent id dispatcher)
        |]

        // set up the agent state
        { Member         = mem
          Machine        = iris.Machine
          Leader         = None
          Dispatcher     = dispatcher
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
        |> store.Update          // and feed it to the store, before we start the services

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

        member self.ForceElection () = failwith "forceelection"

        member self.Periodic () = failwith "periodic"

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
