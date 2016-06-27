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
      let bytes = Raft.encode msg

      expect "Should deserialize correctly" true (Raft.decode >> Option.isSome) bytes 


  let serializationTests =
    testList "Serialization Tests" [
        test_raft_msg_serialization
      ]
