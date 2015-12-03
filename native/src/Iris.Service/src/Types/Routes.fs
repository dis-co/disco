namespace Iris.Service.Types

open Akka.Actor
open Akka.FSharp
open Akka.Routing

[<AutoOpen>]
[<RequireQualifiedAccess>]
module Routes =

  let clients = "clients"
  let websocket = "websocket"

  let getRouter (mbx : Actor<'a>) cnf =
    if   mbx.Context.Child(cnf).Equals(ActorRefs.Nobody) 
    then mbx.Context.ActorOf(Props.Empty.WithRouter(FromConfig.Instance), cnf)
    else mbx.Context.Child(cnf)
    
