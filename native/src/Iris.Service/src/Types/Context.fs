namespace Iris.Service.Types

open Akka.Actor

[<AutoOpen>]
module Context =

  type Ctx =
    { system  : ActorSystem
    ; remotes : IActorRef
    ; clients : IActorRef }
