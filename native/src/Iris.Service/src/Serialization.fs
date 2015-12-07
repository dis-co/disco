namespace Iris.Service

open Iris.Core.Types
open Iris.Core.Serialization
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
      serializeBytes (o :?> WsMsg)

    override self.FromBinary(bytes : byte [], t : System.Type) : obj =
      let msg : WsMsg = unserializeBytes bytes t
      msg :> obj
