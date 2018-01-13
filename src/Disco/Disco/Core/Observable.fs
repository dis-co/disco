(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

open System
open System.Collections.Concurrent

// * Observable module

module Observable =

  // ** tag

  let private tag (str: string) = String.Format("Observable.{0}",str)

  // ** Subscriptions

  type Subscriptions<'t> = ConcurrentDictionary<Guid,IObserver<'t>>

  // ** onNext

  let onNext<'t> (subscriptions: Subscriptions<'t>) (msg: 't) =
    let tmp = subscriptions.ToArray()
    for KeyValue(_,subscription) in tmp do
      try subscription.OnNext msg
      with
        | exn ->
          printfn "%O" exn
          exn.Message
          |> Logger.err (tag "notify")

  // ** createListener

  let createListener<'t> (subs: Subscriptions<'t>) =
    let guid = Guid.NewGuid()
    { new IObservable<'t> with
        member self.Subscribe (obs) =
          if not (subs.TryAdd(guid, obs)) then
            Logger.err (tag "createListener") "could not add listener to subscriptions"
          { new IDisposable with
              member self.Dispose() =
                match subs.TryRemove(guid) with
                | true, _  -> ()
                | _ -> subs.TryRemove(guid) |> ignore } }

  // ** subscribe

  let subscribe<'t> (f: 't -> unit) (subscriptions: Subscriptions<'t>) =
    let listener = createListener subscriptions
    { new IObserver<'t> with
        member self.OnCompleted() = ()
        member self.OnError(error) = ()
        member self.OnNext(value) = f value }
    |> listener.Subscribe
