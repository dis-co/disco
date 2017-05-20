namespace Iris.Core

// * Imports

open System
open System.Collections.Concurrent

// * Subscriptions

type Subscriptions<'t> = ConcurrentDictionary<Guid,IObserver<'t>>

// * Observable module

module Observable =

  // ** tag

  let private tag (str: string) = String.Format("Observable.{0}",str)

  // ** notify

  let notify<'t> (subscriptions: Subscriptions<'t>) (msg: 't) =
    let tmp = subscriptions.ToArray()
    for KeyValue(_,subscription) in tmp do
      try subscription.OnNext msg
      with
        | exn ->
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
