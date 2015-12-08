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

    override self.FromBinary(bytes : byte [], _) : obj =
      // need this construct to provide it with the result type
      let msg : WsMsg = unserializeBytes bytes
      msg :> obj
