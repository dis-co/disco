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
    [| { new IHandler<StateMachine> with
           member handler.OnEvent(ev: PipelineEvent<StateMachine>, _, _) =
             ev.Clear() } |]

  // ** createHandler

  let createHandler (f: EventHandlerFunc<'t>) =
    { new IHandler<'t> with
        member handler.OnEvent(ev: PipelineEvent<'t>, seqno, eob) =
          Option.iter (f seqno eob) ev.Event }

  // ** create

  let create (processors: IHandler<StateMachine>[]) (publish: IHandler<StateMachine>[]) =
    let disruptor = createDisruptor()

    disruptor
    |> handleEventsWith processors
    |> thenDo publish
    |> thenDo clearEvent
    |> ignore

    let ringBuffer = disruptor.Start()

    { new IPipeline<StateMachine> with
        member pipeline.Push(cmd: StateMachine) =
          insertInto ringBuffer cmd

        member pipeline.Dispose() =
          disruptor.Shutdown() }


// * Dispatcher

module Dispatcher =

  // ** stateMutator

  let private stateMutator (store: Store) (seqno: int64) (eob: bool) (cmd: StateMachine) =
    store.Dispatch cmd

  // ** statePersistor

  let private statePersistor (seqno: int64) (eob: bool) (cmd: StateMachine) =
    printfn "persisting state now"

  // ** logPersistor

  let private logPersistor (seqno: int64) (eob: bool) (cmd: StateMachine) =
    printfn "writing log to disk"

  // ** createPublisher

  let private createPublisher (sink: ISink<StateMachine>) =
    fun (seqno: int64) (eob: bool) (cmd: StateMachine) ->
      sink.Publish cmd

  // ** commandResolver

  let private commandResolver (sink: ISink<StateMachine>) =
    fun (seqno: int64) (eob: bool) (cmd: StateMachine) ->
      printfn "resolving commands"
      sink.Publish cmd

  // ** processors

  let private processors (store: Store) =
    [| Pipeline.createHandler (stateMutator store)
       Pipeline.createHandler statePersistor
       Pipeline.createHandler logPersistor |]

  // ** publishers

  let private publishers (sinks: IIrisSinks) =
    [| Pipeline.createHandler (createPublisher sinks.Api)
       Pipeline.createHandler (createPublisher sinks.WebSocket)
       Pipeline.createHandler (commandResolver sinks.Api) |]

  // ** dispatchEvent

  let private dispatchEvent (sinks: IIrisSinks) (pipeline: IPipeline<_>) (cmd:StateMachine) =
    match cmd.DispatchStrategy with
    | Publish | Resolve -> pipeline.Push cmd
    | Replicate -> sinks.Raft.Publish cmd

  // ** create

  let create (store: Store) (sinks: IIrisSinks) =
    let pipeline = Pipeline.create (processors store) (publishers sinks)

    { new IDispatcher with
        member dispatcher.Dispatch(cmd: StateMachine) =
          dispatchEvent sinks pipeline cmd

        member dispatcher.Dispose() =
          dispose pipeline }

// * IrisNG

module IrisNG =

  // ** create

  let create(project: IrisProject) = either {
    let store = Store(State.Empty)

    let! raft = RaftServer.create ()
    let sinks = failwith "make sinks"
    let dispatcher = Dispatcher.create store sinks

    return
      { new IIris with
          member iris.Config
            with get () = store.State.Project.Config

          member iris.Publish (cmd: StateMachine) =
            dispatcher.Dispatch cmd }
    }
