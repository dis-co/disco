namespace Iris.Tests

open System.Threading
open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Raft
open FSharpx.Functional
open ZeroMQ

[<AutoOpen>]
module RaftIntegrationTests =

  let test_log_snapshotting_should_clean_all_logs =
    pending "log snapshotting should clean all logs"

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      let ctx = new ZContext()

      let leadercfg = Config.create "leader"
      let leader = new RaftServer(leadercfg, ctx)
      leader.Start()

      let follower = new RaftServer(leadercfg, ctx)
      follower.Start()

      expect "Should be in failed state" true hasFailed follower.ServerState

      dispose follower
      dispose leader
      dispose ctx

  //                       _ _
  //  _ __   ___ _ __   __| (_)_ __   __ _
  // | '_ \ / _ \ '_ \ / _` | | '_ \ / _` |
  // | |_) |  __/ | | | (_| | | | | | (_| |
  // | .__/ \___|_| |_|\__,_|_|_| |_|\__, |
  // |_|                             |___/

  let test_validate_follower_joins_leader_after_startup =
    pending "follower join should fail on duplicate raftid"

  let test_follower_join_should_fail_on_duplicate_raftid =
    pending "follower join should fail on duplicate raftid"

  let test_all_rafts_should_share_a_common_distributed_event_log =
    pending "all rafts should share a common distributed event log"

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let raftIntegrationTests =
    testList "Raft Integration Tests" [
      // db
      test_log_snapshotting_should_clean_all_logs

      // raft
      test_validate_raft_service_bind_correct_port
      test_validate_follower_joins_leader_after_startup
      test_follower_join_should_fail_on_duplicate_raftid
      test_all_rafts_should_share_a_common_distributed_event_log
    ]
