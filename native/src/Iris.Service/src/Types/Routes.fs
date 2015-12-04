namespace Iris.Service.Types

open Akka.Actor
open Akka.FSharp
open Akka.Routing

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Routes =

  let clients = "clients"
  let websocket = "websocket"

  let GetRouter (actor : Actor<'a>) cnf =
    let sel = select ("/user/" + cnf) actor.Context.System
    try 
      Async.AwaitTask<IActorRef>(sel.ResolveOne(System.TimeSpan.FromSeconds(1.)))
      |> Async.RunSynchronously
    with
      | _ ->
        actor.Context.System.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), cnf)
