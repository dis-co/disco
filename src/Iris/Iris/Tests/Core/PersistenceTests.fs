namespace Iris.Tests

open Expecto
open Iris.Core
open Iris.Raft
open Iris.Core
open Iris.Service

[<AutoOpen>]
module PersistenceTests =

  let mkPin group =
    Pin.Sink.toggle (mk()) (rndname()) group (mk()) [| true |]

  let mkProject () =
    either {
      let root = tmpPath()
      let name = rndstr()
      do! MachineConfig.init (konst "127.0.0.1") None (Some root)
      let machine = MachineConfig.get ()
      let path = Project.ofFilePath (root </> filepath name)
      let! project = Project.create path name machine
      return machine, project
    }

  let mkState () =
    either {
      let! (machine, project) = mkProject ()
      return
        machine,
        { Project            = project
          PinGroups          = PinGroupMap.empty
          PinMappings        = Map.empty
          PinWidgets         = Map.empty
          Cues               = Map.empty
          CueLists           = Map.empty
          Sessions           = Map.empty
          Users              = Map.empty
          Clients            = Map.empty
          CuePlayers         = Map.empty
          FsTrees            = Map.empty
          DiscoveredServices = Map.empty }
    }

  let test_persist_add_pinwidgets_correctly =
    testCase "persist add pinwidgets correctly" <| fun _ ->
      either {
        let widget = mkPinWidget()
        let! (machine, state) = mkState () |> Either.map (State.addPinWidget widget |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinWidget widget)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine
        expect "state should contain PinWidget" true (Map.containsKey widget.Id) state.PinWidgets
        expect "PinWidgets should be the same" state.PinWidgets id loaded.PinWidgets
      }
      |> noError

  let test_persist_add_pinmappings_correctly =
    testCase "persist add pinmappings correctly" <| fun _ ->
      either {
        let mapping = mkPinMapping()
        let! (machine, state) =
          mkState ()
          |> Either.map (State.addPinMapping mapping |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinMapping mapping)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine
        expect "state should contain PinMapping" true (Map.containsKey mapping.Id) state.PinMappings
        expect "PinMappings should be the same" state.PinMappings id loaded.PinMappings
      }
      |> noError

  let test_persist_add_pingroups_correctly =
    testCase "persist add pingroups correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinGroup"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id)
          state.PinGroups

        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_remove_pinwidgets_correctly =
    testCase "persist remove pinwidgets correctly" <| fun _ ->
      either {
        let widget = mkPinWidget()
        let! (machine, state) = mkState () |> Either.map (State.addPinWidget widget |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinWidget widget)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinWidget" true (Map.containsKey widget.Id) state.PinWidgets
        expect "PinWidgets should be the same" state.PinWidgets id loaded.PinWidgets

        let updated = State.removePinWidget widget loaded
        let! _ = Persistence.persistEntry state (RemovePinWidget widget)
        let! loaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should contain PinWidget" true (Map.containsKey widget.Id >> not)  updated.PinWidgets
        expect "PinWidgets should be the same" updated.PinWidgets id loaded.PinWidgets
      }
      |> noError

  let test_persist_remove_pinmappings_correctly =
    testCase "persist remove pinmappings correctly" <| fun _ ->
      either {
        let mapping = mkPinMapping()
        let! (machine, state) =
          mkState ()
          |> Either.map (State.addPinMapping mapping |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinMapping mapping)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinMapping" true (Map.containsKey mapping.Id) state.PinMappings
        expect "PinMappings should be the same" state.PinMappings id loaded.PinMappings

        let updated = State.removePinMapping mapping loaded
        let! _ = Persistence.persistEntry state (RemovePinMapping mapping)
        let! loaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should contain PinMapping"
          true
          (Map.containsKey mapping.Id >> not)
          updated.PinMappings

        expect "PinMappings should be the same" updated.PinMappings id loaded.PinMappings
      }
      |> noError

  let test_persist_remove_pingroups_correctly =
    testCase "persist remove pingroups correctly" <| fun _ ->
      either {
        let group = mkPinGroup()
        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinGroup"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id)
          state.PinGroups

        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.removePinGroup group loaded
        let! _ = Persistence.persistEntry state (RemovePinGroup group)
        let! loaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should contain PinGroup"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id >> not)
          updated.PinGroups

        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_add_cueplayers_correctly =
    testCase "persist add cueplayers correctly" <| fun _ ->
      either {
        let player = mkCuePlayer()
        let! (machine, state) = mkState () |> Either.map (State.addCuePlayer player |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddCuePlayer player)
        let! loaded = Asset.loadWithMachine state.Project.Path machine
        expect "state should contain CuePlayer" true (Map.containsKey player.Id) state.CuePlayers
        expect "Cueplayers should be the same" state.CuePlayers id loaded.CuePlayers
      }
      |> noError

  let test_persist_remove_cueplayers_correctly =
    testCase "persist remove cueplayers correctly" <| fun _ ->
      either {
        let player = mkCuePlayer()
        let! (machine, state) = mkState () |> Either.map (State.addCuePlayer player |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddCuePlayer player)
        let! loaded = Asset.loadWithMachine state.Project.Path machine

        expect "state should contain PinGroup" true (Map.containsKey player.Id) state.CuePlayers
        expect "Cueplayers should be the same" state.CuePlayers id loaded.CuePlayers

        let updated = State.removeCuePlayer player loaded
        let! _ = Persistence.persistEntry state (RemoveCuePlayer player)
        let! loaded = Asset.loadWithMachine updated.Project.Path machine

        expect "state should not contain CuePlayer"
          false
          (Map.containsKey player.Id)
          updated.CuePlayers

        expect "Cueplayers should be the same" updated.CuePlayers id loaded.CuePlayers
      }
      |> noError

  let test_persist_add_pin_correctly =
    testCase "persist add pin correctly" <| fun _ ->
      either {
        let group = mkPinGroup()

        let pin =
          Pin.Sink.toggle
            (IrisId.Create())
            (name "ohai")
            group.Id
            group.ClientId
            [| true |]
          |> Pin.setPersisted true

        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinGroup"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id)
          state.PinGroups

        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.addPin pin state
        let! _ = Persistence.persistEntry updated (AddPin pin)
        let! loaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should contain Pin"
          true
          (PinGroupMap.containsPin pin.ClientId pin.PinGroupId pin.Id)
          updated.PinGroups

        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups
      }
      |> noError

  let test_persist_remove_pin_correctly =
    testCase "persist remove pin correctly" <| fun _ ->
      either {
        let group = mkPinGroup()

        let pin =
          Pin.Sink.toggle
            (IrisId.Create())
            (name "ohai")
            group.Id
            group.ClientId
            [| true |]
          |> Pin.setPersisted true

        let! (machine, state) = mkState () |> Either.map (State.addPinGroup group |> Tuple.mapSnd)
        let! _ = Persistence.persistEntry state (AddPinGroup group)
        let! loaded = Asset.loadWithMachine (Project.toFilePath state.Project.Path) machine

        expect "state should contain PinGroup"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id)
          state.PinGroups

        expect "PinGroups should be the same" state.PinGroups id loaded.PinGroups

        let updated = State.addPin pin state
        let! _ = Persistence.persistEntry updated (AddPin pin)
        let! loaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should contain Pin"
          true
          (PinGroupMap.containsPin pin.ClientId pin.PinGroupId pin.Id)
          updated.PinGroups

        expect "PinGroups should be the same" updated.PinGroups id loaded.PinGroups

        let updated = State.removePin pin updated
        let! _ = Persistence.persistEntry updated (RemovePin pin)
        let! reloaded = Asset.loadWithMachine (Project.toFilePath updated.Project.Path) machine

        expect "state should not contain Pin"
          true
          (PinGroupMap.containsPin pin.ClientId pin.PinGroupId pin.Id >> not)
          updated.PinGroups

        expect "PinGroups should be the same" updated.PinGroups id reloaded.PinGroups
      }
      |> noError


  let persistenceTests =
    testList "Persistence Tests" [
      test_persist_add_pinwidgets_correctly
      test_persist_add_pinmappings_correctly
      test_persist_add_pingroups_correctly
      test_persist_remove_pinwidgets_correctly
      test_persist_remove_pinmappings_correctly
      test_persist_remove_pingroups_correctly
      test_persist_add_pin_correctly
      test_persist_remove_pin_correctly
      test_persist_add_cueplayers_correctly
      test_persist_remove_cueplayers_correctly
    ]
