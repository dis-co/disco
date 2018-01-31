(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System.IO
open System.Threading
open Expecto

open Disco.Core
open Disco.Service
open Disco.Client
open Disco.Client.Interfaces
open Disco.Service.Interfaces
open Disco.Raft
open Disco.Net

[<AutoOpen>]
module DiscoServiceTests =

  //  ___      _     ____                  _            _____         _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___  |_   _|__  ___| |_ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \   | |/ _ \/ __| __/ __|
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/   | |  __/\__ \ |_\__ \
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|   |_|\___||___/\__|___/

  let discoServiceTests =
    testList "DiscoService Tests" [
      ClonesFromLeader.test
      CorrectPinPersistance.test
      EnsureClientCommandForward.test
      EnsureClientsReplicated.test
      EnsureClientUpdateNoLoop.test
      EnsureCueResolver.test
      EnsureMappingResolver.test
      PinBecomesOnlineOnClientConnect.test
      PinBecomesDirty.test
      StateShouldBeCleanedOnClientRemove.test
      RemoveMemberShouldSplitCluster.test
      AddPreviousMemberShouldPull.test
    ] |> testSequenced
