namespace Iris.Service

open Akka.Actor
open Akka.Serialization

module Serialization =

  type IrisSerializer(system : ExtendedActorSystem) =
    inherit Serializer(system)

    override self.Identifier
      with get () : int = 19834192

    override self.IncludeManifest
      with get () : bool = false

    override self.ToBinary(o : obj) : byte [] =
      printfn "ToBinary called"
      Array.empty

    override self.FromBinary(bytes : byte [], t : System.Type) : obj =
      printfn "FromBinary called"
      Array.empty :> obj
