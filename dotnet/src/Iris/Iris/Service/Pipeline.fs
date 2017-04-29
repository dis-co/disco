namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Threading.Tasks
open Disruptor
open Disruptor.Dsl
open Interfaces

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
