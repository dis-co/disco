namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open Pallet.Core
open System
open System.Net
open FSharpx.Functional

[<AutoOpen>]
module RaftIntegrationTests =

  //  ____                            _ __     __    _
  // |  _ \ ___  __ _ _   _  ___  ___| |\ \   / /__ | |_ ___
  // | |_) / _ \/ _` | | | |/ _ \/ __| __\ \ / / _ \| __/ _ \
  // |  _ <  __/ (_| | |_| |  __/\__ \ |_ \ V / (_) | ||  __/
  // |_| \_\___|\__, |\__,_|\___||___/\__| \_/ \___/ \__\___|
  //               |_|

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->

      failwith "FIXME: FAILEDD"

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let raftIntegrationTests =
    testList "Raft Integration Tests" [
        test_validate_raft_service_bind_correct_port
      ]
