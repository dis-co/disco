namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open FSharpx.Functional

[<AutoOpen>]
module SerializationTests =

  let test_raft_msg_serialization =
    testCase "validate raft msg serialization" <| fun _ ->
      let msg : RaftMsg = EmptyResponse
      let bytes = msg.ToBytes()

      expect "Should deserialize correctly" true (RaftMsg.FromBytes >> Option.isSome) bytes 


  let serializationTests =
    testList "Serialization Tests" [
        test_raft_msg_serialization
      ]
