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

  let mkPin group =
    Pin.Toggle(mk(), rndstr(), group, mkTags(), [| true |])

  let mkProject () =
    either {
      let root = tmpPath()
      let name = rndstr()
      do! MachineConfig.init (konst "127.0.0.1") None (Some root)
      let machine = MachineConfig.get ()
      let! project = Project.create (root </> filepath name) name machine
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
        expect "state should contain PinGroup" true (Map.containsKey group.Id) state.PinGroups
        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_remove_pingroups_correctly =
    testCase "persist remove pingroups correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine state.Project.Path machine

        expect "state should contain PinGroup" true (Map.containsKey group.Id) state.PinGroups
        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.removePinGroup group loaded
        let! _ = Persistence.persistEntry state (RemovePinGroup group)
        let! loaded = Asset.loadWithMachine updated.Project.Path machine

        expect "state should contain PinGroup" true (Map.containsKey group.Id >> not)  updated.PinGroups
        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_add_pin_correctly =
    testCase "persist add pin correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let pin = mkPin group.Id
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine state.Project.Path machine

        expect "state should contain PinGroup" true (Map.containsKey group.Id) state.PinGroups
        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.addPin pin state
        let! _ = Persistence.persistEntry updated (AddPin pin)
        let! loaded = Asset.loadWithMachine updated.Project.Path machine

        expect "state should contain Pin" true (Map.containsPin pin.Id) updated.PinGroups
        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_remove_pin_correctly =
    testCase "persist remove pin correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let pin = mkPin group.Id
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine state.Project.Path machine

        expect "state should contain PinGroup" true (Map.containsKey group.Id) state.PinGroups
        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.addPin pin state
        let! _ = Persistence.persistEntry updated (AddPin pin)
        let! loaded = Asset.loadWithMachine updated.Project.Path machine

        expect "state should contain Pin" true (Map.containsPin pin.Id) updated.PinGroups
        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups

        let updated = State.removePin pin updated
        let! _ = Persistence.persistEntry updated (RemovePin pin)
        let! reloaded = Asset.loadWithMachine updated.Project.Path machine

        expect "state should not contain Pin" true (Map.containsPin pin.Id >> not) updated.PinGroups
        expect "PinGroups should be the same" updated.PinGroups id reloaded.PinGroups
      }
      |> noError

  let persistenceTests =
    testList "Persistence Tests" [
      test_persist_add_pingroups_correctly
      test_persist_remove_pingroups_correctly
      test_persist_add_pin_correctly
      test_persist_remove_pin_correctly
    ]
