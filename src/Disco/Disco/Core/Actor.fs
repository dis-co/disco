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

#if !FABLE_COMPILER

module Periodically =

  let run (interval: int) (task: unit -> unit) =
    let mutable run = true
    let thread = Thread(ThreadStart(fun () ->
      try
        while run do
          do task()
          do Thread.Sleep(interval)
      with
        | :? ThreadAbortException -> ()
        | exn -> printfn "Periodically: %A" exn))
    thread.IsBackground <- true
    thread.Start()
    { new IDisposable with
      member self.Dispose() = run <- false }

#endif

// * Continuously

#if !FABLE_COMPILER

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

#endif

// * AsyncActor

module AsyncActor =

  // ** loop

  let private loop<'a> tag actor (f: AsyncActorTask<'a>) (inbox: MailboxProcessor<'a>) =
    let rec _loop () =
      async {
        let! msg = inbox.Receive()
        do! f actor msg
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
        #if !FABLE_COMPILER
        mbp.Error.Add(sprintf "unhandled error on loop: %O" >> printfn "%s: %s" tag)
        #endif
      member actor.Post value = try mbp.Post value with _ -> ()
      member actor.CurrentQueueLength =
        #if FABLE_COMPILER
        0
        #else
        try mbp.CurrentQueueLength with _ -> 0
        #endif
      member actor.Dispose () = cts.Cancel() }


// * ThreadActor

#if !FABLE_COMPILER

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
    with
      | :? ThreadAbortException -> ()
      | _ -> () // printfn "ThreadActor: %A" exn

  // ** create

  let create<'a> tag (f: ActorTask<'a>) =
    let queue = new BlockingCollection<'a>()
    { new IActor<'a> with
      member actor.Start() =
        let thread = Thread(ThreadStart(loop<'a> tag queue actor f))
        thread.Name <- tag
        thread.IsBackground <- true
        thread.Start()
      member actor.Post value = try queue.Add value with _ -> ()
      member actor.CurrentQueueLength = try queue.Count with _ -> 0
      member actor.Dispose() = tryDispose queue ignore }

#endif
