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

  // ** publish

  let private publish (ringBuffer: RingBuffer<PipelineEvent<'t>>) (cmd: 't) =
    let seqno = ringBuffer.Next()
    let entry = ringBuffer.[seqno]
    entry.Event <- Some cmd
    ringBuffer.Publish(seqno)

  // ** clearEvent

  let private clearEvent =
    { new IHandler<'t> with
        member handler.OnEvent(ev: PipelineEvent<'t>, _, _) =
          ev.Clear() }

  // ** createHandler

  let createHandler (f: EventHandlerFunc<'t>) =
    { new IHandler<'t> with
        member handler.OnEvent(ev: PipelineEvent<'t>, seqno, eob) =
          Option.iter (f seqno eob) ev.Event }

  // ** create

  let create (handlers: IHandler<'t>[]) =
    let disruptor = createDisruptor()

    disruptor
    |> handleEventsWith handlers
    |> thenDo [| clearEvent |]
    |> ignore

    let ringBuffer = disruptor.Start()

    { new IPipeline<'t> with
        member pipeline.Push(cmd: 't) =
          publish ringBuffer cmd

        member pipeline.Dispose() =
          disruptor.Shutdown() }


// * IrisNG

module IrisNG =

  // ** IDispatcher

  type IDispatcher =
    inherit IDisposable
    abstract Dispatch: StateMachine -> unit

  // ** IIris

  type IIris =
    abstract Config: IrisConfig with get
    abstract Publish: StateMachine -> unit

  // ** IRaft

  type IRaft =
    inherit IDisposable
    abstract Append: StateMachine -> unit
    abstract Subscribe: (RaftEvent -> unit) -> IDisposable

  // ** WebSocketEvent

  type WebSocketEvent =
    | OnConnect
    | OnMessage
    | OnDisconnect
    | OnError

  // ** IWebSocketSource

  type IWebSocketSource =
    inherit IDisposable
    abstract Subscribe: (WebSocketEvent -> unit) -> IDisposable

  // ** IWebSocketSink

  type IWebSocketSink =
    inherit IDisposable
    abstract Publish: StateMachine -> unit

  // ** createRaft

  let private createRaft () =
    { new IRaft with
        member raft.Append(cmd: StateMachine) =
          failwith "append"

        member raft.Subscribe(f) =
          failwith "subscribe"

        member raft.Dispose() =
          failwith "dispose" }

  // ** dispatchEvent

  let private dispatchEvent (pipeline: IPipeline<_>) (raft: IRaft) (cmd:StateMachine) =
    match cmd.DispatchStrategy with
    | Publish   -> pipeline.Push cmd
    | Replicate -> raft.Append cmd

  // ** stateMutator

  let private stateMutator (store: Store) (seqno: int64) (eob: bool) (cmd: StateMachine) =
    store.Dispatch cmd

  // ** pipelineProcesses

  let private pipelineProcesses (store: Store) =
    [| Pipeline.createHandler (stateMutator store) |]

  // ** createDispatcher

  let private createDispatcher (store: Store) (raft: IRaft) =
    let pipeline =
      store
      |> pipelineProcesses
      |> Pipeline.create

    { new IDispatcher with
        member dispatcher.Dispatch(cmd: StateMachine) =
          dispatchEvent pipeline raft cmd

        member dispatcher.Dispose() =
          dispose pipeline }

  // ** create

  let create(project: IrisProject) =

    let raft = createRaft ()
    let store = Store(State.Empty)

    let dispatcher = createDispatcher store raft

    { new IIris with
        member iris.Config
          with get () = store.State.Project.Config

        member iris.Publish (cmd: StateMachine) =
          dispatcher.Dispatch cmd }
