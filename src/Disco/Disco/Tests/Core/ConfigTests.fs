(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open Expecto
open Disco.Core

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
      result {
        let config = MachineConfig.create "127.0.0.1" None
        do! MachineConfig.save None config

        do! MachineConfig.init (konst "127.0.0.1") None None
        let loaded = MachineConfig.get()

        expect "MachineConfigs should be equal" config id loaded
      }
      |> noError

  let loadSaveCustomPathTest =
    testCase "Save/Load MachineConfig with default path should render equal values" <| fun _ ->
      result {
        let path = tmpPath()

        let config = MachineConfig.create "127.0.0.1" None
        do! MachineConfig.save (Some path) config

        do! MachineConfig.init (konst "127.0.0.1") None (Some path)
        let loaded = MachineConfig.get()

        expect "MachineConfigs should be equal" config id loaded
      }
      |> noError
  [<Tests>]
  let configTests =
    testList "Load/Save MachineConfig" [
        loadSaveTest
        loadSaveCustomPathTest
      ] |> testSequenced
