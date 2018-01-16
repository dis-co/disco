(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System
open Expecto
open Disco.Core
open Disco.Raft

[<AutoOpen>]
module SynchronizationTests =

  let test_should_call_monitor_correct_number_of_times =
    testCase "should call monitor correct number of times" <| fun _ ->
      either {
        use ev = new WaitEvent()
        async {
          do! Async.Sleep(1000)
          ev.Set()
        }
        |> Async.Start
        do! waitFor "Should not timeout" ev
      }
      |> noError

  let syncTests =
    testList "Synchronization Tests" [
      test_should_call_monitor_correct_number_of_times
    ]
