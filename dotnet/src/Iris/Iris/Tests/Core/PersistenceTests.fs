namespace Iris.Tests

open System
open System.IO
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core
open Iris.Service

[<AutoOpen>]
module PersistenceTests =

  let mkProject () =
    either {
      let root = tmpPath()
      let name = rndstr()
      do! MachineConfig.init (Some root)
      let machine = MachineConfig.get ()
      let! project = Project.create (root </> name) name machine
      return machine, project
    }

  let mkState () =
    either {
      let! (machine, project) = mkProject ()
      return
        machine,
        { Project   = project
          PinGroups = Map.empty
          Cues      = Map.empty
          CueLists  = Map.empty
          Sessions  = Map.empty
          Users     = Map.empty
          Clients   = Map.empty
          DiscoveredServices = Map.empty }
    }

  let test_persist_add_pingroups_correctly =
    testCase "persist add pingroups correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine state.Project.Path machine
        expect "state should contain PinGroup" state.PinGroups Map.containsKey group.Id
        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups
      }
      |> noError

  let persistenceTests =
    testList "Persistence Tests" [
      test_persist_add_pingroups_correctly
    ]
