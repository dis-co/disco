namespace Iris.Tests

open System.IO
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Client
open Iris.Client.Interfaces
open Iris.Service.Interfaces
open Iris.Raft
open Iris.Net

[<AutoOpen>]
module IrisServiceTests =

  //  ___      _     ____                  _            _____         _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___  |_   _|__  ___| |_ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \   | |/ _ \/ __| __/ __|
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/   | |  __/\__ \ |_\__ \
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|   |_|\___||___/\__|___/

  let irisServiceTests =
    testList "IrisService Tests" [
      ClonesFromLeader.test
      CorrectPinPersistance.test
      EnsureClientCommandForward.test
      EnsureClientsReplicated.test
      EnsureClientUpdateNoLoop.test
      EnsureCueResolver.test
      EnsureMappingResolver.test
      PinBecomesOnlineOnClientConnect.test
      PinBecomesDirty.test
    ] |> testSequenced
