namespace Iris.Tests

open System
open System.IO
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module PersistenceTests =

  let mkState path : Either<IrisError,State> =
    either {
      let! project = mkProject path
      return
        { Project   = project
          PinGroups = Map.empty
          Cues      = Map.empty
          CueLists  = Map.empty
          Sessions  = Map.empty
          Users     = Map.empty
          Clients   = Map.empty
          DiscoveredServices = Map.empty }
    }

  let test_write_read_pingroups_correctly =
    testCase "should write and read pingroups correctly" <| fun _ ->
      either {
        let! state = mkState()
        let! info = Asset.write path (Payload payload)
        let! data = Asset.read path
        expect "Payload should be the same" payload id data
      }
      |> noError

  let assetTests =
    testList "Asset Tests" [
      test_write_read_pingroups_correctly
    ] |> testSequenced
