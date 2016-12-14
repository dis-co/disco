namespace Iris.Tests

open System
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module StateTests =

  let test_load_state_correctly =
    testCase "should load state correctly" <| fun _ ->
      either {
        let! state = mkState()
        do! Asset.save state
        let! loaded = Asset.load state.Project.Path

        expect "Should have equal number of Users" false id true
      }

  let stateTests =
    testList "State Tests" [
      test_load_state_correctly
    ]
