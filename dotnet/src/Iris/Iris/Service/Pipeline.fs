namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Threading.Tasks
open Disruptor
open Disruptor.Dsl
open Interfaces
open Iris.Core

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

  let createHandler<'t> (f: EventHandlerFunc<'t>) : IHandler<'t> =
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

  let private stateMutator (store: Store) (seqno: int64) (eob: bool) (cmd: IrisEvent) =
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
      sink.Publish cmd

  // ** commandResolver

  let private commandResolver (sink: ISink<IrisEvent>) =
    fun (seqno: int64) (eob: bool) (cmd: IrisEvent) ->
      printfn "resolving commands"
      sink.Publish cmd

  // ** processors

  let private processors (store: Store) : IHandler<IrisEvent>[] =
    [| Pipeline.createHandler<IrisEvent> (stateMutator store)
       Pipeline.createHandler<IrisEvent> statePersistor
       Pipeline.createHandler<IrisEvent> logPersistor |]

  // ** publishers

  let private publishers (sinks: IIrisSinks<IrisEvent>) =
    [| Pipeline.createHandler<IrisEvent> (createPublisher sinks.Api)
       Pipeline.createHandler<IrisEvent> (createPublisher sinks.WebSocket)
       Pipeline.createHandler<IrisEvent> (commandResolver sinks.Api) |]

  // ** dispatchEvent

  let private dispatchEvent (sinks: IIrisSinks<IrisEvent>) (pipeline: IPipeline<IrisEvent>) (cmd:IrisEvent) =
    match cmd.DispatchStrategy with
    |  Publish | Resolve -> pipeline.Push cmd
    |  Replicate -> sinks.Raft.Publish cmd

  // ** create

  let create (store: Store) (sinks: IIrisSinks<IrisEvent>) =
    let pipeline = Pipeline.create (processors store) (publishers sinks)

    { new IDispatcher<IrisEvent> with
        member dispatcher.Dispatch(cmd: IrisEvent) =
          dispatchEvent sinks pipeline cmd

        member dispatcher.Dispose() =
          dispose pipeline }

// * IrisNG

module IrisNG =

  // ** Listener

  type private Listener = IObservable<IrisEvent>

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<IrisEvent>>

  // ** createListener

  let private createListener (subscriptions: Subscriptions) =
    { new Listener with
        member self.Subscribe(obs) =
          lock subscriptions <| fun _ ->
            subscriptions.Add obs

          { new IDisposable with
              member self.Dispose() =
                lock subscriptions <| fun _ ->
                  subscriptions.Remove obs
                  |> ignore } }

  // ** create

  let create(store: Store) = either {
    let project = store.State.Project

    let subscribers = Subscriptions()
    let listener = createListener subscribers

    let! mem = Project.selfMember project

    let! raft = RaftServer.create ()
    let! api = ApiServer.create mem project.Id
    let! websockets = WebSockets.SocketServer.create mem
    let! git = Git.GitServer.create mem project.Path
    let discovery = DiscoveryService.create store.State.Project.Config.Machine

    do! raft.Start()
    do! api.Start()
    do! discovery.Start()
    do! websockets.Start()
    do! git.Start()

    // setting up the sinks
    let sinks =
      { new IIrisSinks<IrisEvent> with
          member sinks.Raft = unbox raft
          member sinks.Api = unbox api
          member sinks.WebSocket = unbox websockets }

    // creating the pipeline
    let dispatcher = Dispatcher.create store sinks

    // wiring up the sources
    let wiring = [|
      git.Subscribe(IrisEvent.Git >> dispatcher.Dispatch)
      api.Subscribe(IrisEvent.Api >> dispatcher.Dispatch)
      websockets.Subscribe(IrisEvent.Socket >> dispatcher.Dispatch)
      raft.Subscribe(IrisEvent.Raft >> dispatcher.Dispatch)
      discovery.Subscribe(IrisEvent.Discovery >> dispatcher.Dispatch)
    |]

    let! idle = discovery.Register {
      Id = mem.Id
      WebPort = port project.Config.Machine.WebPort
      Status = Busy(project.Id, project.Name)
      Services =
        [| { ServiceType = ServiceType.Api;       Port = port mem.ApiPort }
           { ServiceType = ServiceType.Git;       Port = port mem.GitPort }
           { ServiceType = ServiceType.Raft;      Port = port mem.Port    }
           { ServiceType = ServiceType.WebSocket; Port = port mem.WsPort  } |]
      ExtraMetadata = Array.empty
    }

    // done
    return
      { new IIris<IrisEvent> with
          member iris.Config
            with get () = store.State.Project.Config

          member iris.Subscribe(f: IrisEvent -> unit) =
            { new IObserver<IrisEvent> with
                member self.OnCompleted() = ()
                member self.OnError(error) = ()
                member self.OnNext(value) = f value }
            |> listener.Subscribe

          member iris.Publish (cmd: IrisEvent) =
            dispatcher.Dispatch cmd

          member iris.Dispose() =
            subscribers.Clear()
            dispose idle
            [| raft       :> IDisposable
               api        :> IDisposable
               websockets :> IDisposable
               git        :> IDisposable
               dispatcher :> IDisposable |]
            |> Array.append wiring
            |> Array.Parallel.iter dispose }
    }

  // ** load

  let load (name: string) (machine: IrisMachine) =
    either {
      let! path = Project.checkPath machine name
      let! (state: State) = Asset.loadWithMachine path machine
      let store = Store state
      return! create store
    }
