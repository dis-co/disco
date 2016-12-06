namespace Iris.Tests

open System
open System.IO
open Expecto
open Iris.Core

[<AutoOpen>]
module ConfigTests =

  //   _                    _    ______
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //
  let loadSaveTest =
    testCase "Save/Load MachineConfig with default path should render equal values" <| fun _ ->
      either {
        let config = MachineConfig.create ()

        do! MachineConfig.save None config

        let! loaded = MachineConfig.load None

        expect "MachineConfigs should be equal" config id loaded
      }
      |> noError

  let loadSaveCustomPathTest =
    testCase "Save/Load MachineConfig with default path should render equal values" <| fun _ ->
      either {
        let path = Path.GetTempFileName()

        let config = MachineConfig.create ()

        do! MachineConfig.save (Some path) config

        let! loaded = MachineConfig.load (Some path)

        expect "MachineConfigs should be equal" config id loaded
      }
      |> noError
  [<Tests>]
  let configTests =
    testList "Load/Save MachineConfig" [
        loadSaveTest
        loadSaveCustomPathTest
      ]
