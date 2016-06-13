namespace Pallet.Tests

open Fuchu
open Fuchu.Test
open Pallet.Core.RaftMonad
open FSharpx.Functional

[<AutoOpen>]
module RaftMonad = 

  let test_raft_monad =
    testCase "test the raft mondad" <| fun _ ->
      skiptest "test the raft mondad not yet implemented"

