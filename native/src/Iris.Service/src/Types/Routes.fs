namespace Iris.Service.Types

open Akka.Actor
open Akka.FSharp
open Akka.Routing

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Routes =

  let clients = "clients"
  let websocket = "websocket"

  let GetRouter (system : ActorSystem) cnf =
    let sel = select ("/user/" + cnf) system
    try 
      Async.AwaitTask<IActorRef>(sel.ResolveOne(System.TimeSpan.FromSeconds(1.)))
      |> Async.RunSynchronously
    with
      | _ ->
        system.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), cnf)
