(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

open System
open System.Threading

type IActor<'a> =
  inherit IDisposable
  abstract Post: 'a -> unit
  abstract Start: unit -> unit
  abstract CurrentQueueLength: int

type AsyncActorTask<'a> = IActor<'a> -> 'a -> Async<unit>
type ActorTask<'a> = IActor<'a> -> 'a -> unit

// * Periodically

module Periodically =

  let run interval (task: unit -> unit) =
    let cts = new CancellationTokenSource()
    let rec _runner () =
      async {
        do task ()
        do! Async.Sleep(interval)
        return! _runner ()
      }
    Async.Start(_runner(),cts.Token)
    { new IDisposable with
      member self.Dispose() =
        cts.Cancel()
        dispose cts }

// * Continuously

module Continuously =

  let run (f: unit -> bool) =
    let mutable run = true
    let thread = Thread(ThreadStart(fun () ->
      try while run do run <- f()
      with
        | :? ThreadAbortException -> ()
        | exn -> printfn "Continously: %A" exn))
    thread.IsBackground <- true
    thread.Start()
    { new IDisposable with
      member self.Dispose () =
        run <- false }

// * Actor

module Actor =

  // ** warnQueueLength

  let warnQueueLength t (inbox: IActor<_>) =
    // wa't when 't :> rn if the queue length surpasses threshold
    let count = inbox.CurrentQueueLength
    if count > Constants.QUEUE_LENGTH_THRESHOLD then
      count
      |> String.format "Queue length threshold was reached: {0}"
      |> printfn "[WARNING-%s]: %s" t


// * AsyncActor

module AsyncActor =

  // ** loop

  let private loop<'a> tag actor (f: AsyncActorTask<'a>) (inbox: MailboxProcessor<'a>) =
    let rec _loop () =
      async {
        let! msg = inbox.Receive()
        do! f actor msg
        do Actor.warnQueueLength tag actor
        return! _loop ()
      }
    _loop ()

  // ** create

  let create<'a> tag (f: AsyncActorTask<'a>) =
    let cts = new CancellationTokenSource()
    let mutable mbp = Unchecked.defaultof<MailboxProcessor<'a>>
    { new IActor<'a> with
      member actor.Start () =
        mbp <- MailboxProcessor.Start(loop<'a> tag actor f, cts.Token)
        mbp.Error.Add(sprintf "unhandled error on loop: %O" >> printfn "%s: %s" tag)
      member actor.Post value = try mbp.Post value with _ -> ()
      member actor.CurrentQueueLength = try mbp.CurrentQueueLength with _ -> 0
      member actor.Dispose () = cts.Cancel() }

// * ThreadActor

module ThreadActor =

  open System.Collections.Concurrent

  type Queue<'a> = BlockingCollection<'a>

  // ** loop

  let private loop<'a> tag (queue: Queue<'a>) (actor: IActor<'a>) (f: ActorTask<'a>) () =
    let mutable run = true
    try
      while run do
        let msg = queue.Take()
        do f actor msg
        do Actor.warnQueueLength tag actor
    with
      | :? ThreadAbortException -> ()
      | exn -> () // printfn "ThreadActor: %A" exn

  // ** create

  let create<'a> tag (f: ActorTask<'a>) =
    let queue = new BlockingCollection<'a>()
    { new IActor<'a> with
      member actor.Start() =
        let thread = Thread(ThreadStart(loop<'a> tag queue actor f))
        thread.IsBackground <- true
        thread.Start()
      member actor.Post value = try queue.Add value with _ -> ()
      member actor.CurrentQueueLength = try queue.Count with _ -> 0
      member actor.Dispose() = tryDispose queue ignore }
