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
    [| { new IHandler<'t> with
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
    | Publish | Resolve -> pipeline.Push cmd
    | Replicate -> sinks.Raft.Publish cmd

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

  // ** create

  let create(project: IrisProject) = either {
    let store = Store(State.Empty)

    let! raft = RaftServer.create ()
    let! api = ApiServer.create (failwith "mem") (failwith "other")
    let! websockets = WebSockets.SocketServer.create (failwith "mem")
    let discovery = DiscoveryService.create project.Config.Machine

    let sinks =
      { new IIrisSinks<IrisEvent> with
          member sinks.Raft = unbox raft
          member sinks.Api = unbox api
          member sinks.WebSocket = unbox websockets }

    let dispatcher = Dispatcher.create store sinks

    let api = api.Subscribe(IrisEvent.Api >> dispatcher.Dispatch)
    let wobs = websockets.Subscribe(IrisEvent.Socket >> dispatcher.Dispatch)
    let robs = raft.Subscribe(IrisEvent.Raft >> dispatcher.Dispatch)
    let dobs = discovery.Subscribe(IrisEvent.Discovery >> dispatcher.Dispatch)

    return
      { new IIris<IrisEvent> with
          member iris.Config
            with get () = store.State.Project.Config

          member iris.Subscribe(f: IrisEvent -> unit) =
            failwith "subscribe"

          member iris.Publish (cmd: IrisEvent) =
            dispatcher.Dispatch cmd }
    }
