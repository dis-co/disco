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
        let! state = mkTmpDir() |> mkState
        do! Asset.save state.Project.Path state
        let! loaded = Asset.loadWithMachine state.Project.Path state.Project.Config.Machine

        expect "Cues should be equal" state.Cues id loaded.Cues
        expect "CueLists should be equal" state.CueLists id loaded.CueLists
        expect "Patches should be equal" state.Patches id loaded.Patches

        let config =
          { loaded.Project.Config with
              Machine = state.Project.Config.Machine }
        expect "Config should be equal" state.Project.Config id config
      }
      |> noError

  let stateTests =
    testList "State Tests" [
      test_load_state_correctly
    ]
