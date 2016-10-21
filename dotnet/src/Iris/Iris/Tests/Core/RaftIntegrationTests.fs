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

  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  let mkTmpPath snip =
    let basePath =
      match System.Environment.GetEnvironmentVariable("IN_NIX_SHELL") with
        | "1" -> "/tmp/"
        | _   -> System.IO.Path.GetTempPath()
    basePath </> snip

  open System.Linq

  //  ____  ____    _____         _
  // |  _ \| __ )  |_   _|__  ___| |_ ___
  // | | | |  _ \    | |/ _ \/ __| __/ __|
  // | |_| | |_) |   | |  __/\__ \ |_\__ \
  // |____/|____/    |_|\___||___/\__|___/


  let test_save_restore_raft_value_correctly =
    testCase "save/restore raft value correctly" <| fun _ ->
      let self =
        Config.getNodeId ()
        |> Either.map Node.create
        |> Either.get

      let node1 =
        { Node.create (Id.Create()) with
            HostName = "Hans"
            IpAddr = IpAddress.Parse "192.168.1.20"
            Port   = 8080us }

      let node2 =
        { Node.create (Id.Create()) with
            HostName = "Klaus"
            IpAddr = IpAddress.Parse "192.168.1.22"
            Port   = 8080us }

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        LogEntry(Id.Create(), 7u, 1u, DataSnapshot State.Empty,
          Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot State.Empty,
            Some <| Configuration(Id.Create(), 5u, 1u, [| node1 |],
              Some <| JointConsensus(Id.Create(), 4u, 1u, changes,
                Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, nodes, DataSnapshot State.Empty)))))
        |> Log.fromEntries

      let config =
        Config.create "default"
        |> Config.addNode self
        |> Config.addNode node1
        |> Config.addNode node2

      let raft =
        createRaft config
        |> Either.map
            (fun raft ->
              { raft with
                  Log = log
                  CurrentTerm = 666u })
        |> Either.get

      let path = mkTmpPath "save_restore-raft_value-correctly"

      saveRaft config raft
      |> Either.mapError Error.throw
      |> ignore

      let loaded = loadRaft config

      expect "Values should be equal" (Right raft) id loaded

      rmDir path

  let test_log_snapshotting_should_clean_all_logs =
    pending "log snapshotting should clean all logs"

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_raft_service_bind_correct_port =
    pending "validate raft service bind correct port"
    (*
    testCase "validate raft service bind correct port" <| fun _ ->
      let ctx = new ZContext()

      let leadercfg = Config.Create "leader"
      let leader = new RaftServer(leadercfg, ctx)
      leader.Start()

      let follower = new RaftServer(leadercfg, ctx)
      follower.Start()

      expect "Should be in failed state" true hasFailed follower.ServerState

      dispose follower
      dispose leader
      dispose ctx
    *)

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
        test_save_restore_raft_value_correctly
        test_log_snapshotting_should_clean_all_logs

        // raft
        test_validate_raft_service_bind_correct_port
        test_validate_follower_joins_leader_after_startup
        test_follower_join_should_fail_on_duplicate_raftid
        test_all_rafts_should_share_a_common_distributed_event_log
      ]
