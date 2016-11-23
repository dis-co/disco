namespace Iris.Tests

open System
open System.Threading
open System.Text
open Expecto

open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Raft
open Iris.Service
open FSharpx.Functional
open Microsoft.FSharp.Control
open ZeroMQ

[<AutoOpen>]
module RaftIntegrationTests =

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_correct_req_socket_tracking =
    testCase "validate correct req socket tracking" <| fun _ ->
      let nid1 = mkUuid()
      let nid2 = mkUuid()

      let node1 =
        Id nid1
        |> Node.create
        |> Node.setPort 8000us

      let node2 =
        Id nid2
        |> Node.create
        |> Node.setPort 8001us

      setNodeId nid1

      let leadercfg =
        Config.create "leader"
        |> Config.setNodes [| node1; node2 |]
        |> Config.setLogLevel (LogLevel.Debug)

      setNodeId nid2

      let followercfg =
        Config.create "follower"
        |> Config.setNodes [| node1; node2 |]
        |> Config.setLogLevel (LogLevel.Debug)

      setNodeId nid1

      let leader = new RaftServer(leadercfg)
      expect "Leader should have no connections" true ((=) 0) leader.State.Connections.Count
      leader.Start()
      expect "Leader should have one connection" true ((=) 1) leader.State.Connections.Count

      setNodeId nid2

      let follower = new RaftServer(followercfg)
      expect "Follower should have no connections" true ((=) 0) follower.State.Connections.Count
      follower.Start()
      expect "Follower should have one connection" true ((=) 1) follower.State.Connections.Count

      dispose leader
      dispose follower

      expect "Leader should have no connections" true ((=) 0) leader.State.Connections.Count
      expect "Follower should have no connections" true ((=) 0) follower.State.Connections.Count

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      let port = 12000us

      let node =
        Config.getNodeId()
        |> Either.get
        |> Node.create
        |> Node.setPort port

      let leadercfg =
        Config.create "leader"
        |> Config.addNode node

        // |> Config.setLogLevel (LogLevel.Debug)

      let leader = new RaftServer(leadercfg)
      leader.Start()

      expect "Should be running" true Service.isRunning leader.ServerState

      let follower = new RaftServer(leadercfg)
      follower.Start()

      expect "Should be failed" true Service.hasFailed follower.ServerState

      dispose follower
      dispose leader

  let test_validate_follower_joins_leader_after_startup =
    testCase "validate follower joins leader after startup" <| fun _ ->
      printfn "---------------------------- follower joins leader --------------------------------"

      use obs = Observable.subscribe Logger.stdout Logger.listener

      let nid1 = mkUuid()
      let nid2 = mkUuid()

      let node1 =
        Id nid1
        |> Node.create
        |> Node.setPort 8000us

      let node2 =
        Id nid2
        |> Node.create
        |> Node.setPort 8001us

      setNodeId nid1

      let leadercfg =
        Config.create "leader"
        |> Config.setNodes [| node1; node2 |]
        |> Config.setLogLevel (LogLevel.Debug)

      setNodeId nid2

      let followercfg =
        Config.create "follower"
        |> Config.setNodes [| node1; node2 |]
        |> Config.setLogLevel (LogLevel.Debug)

      setNodeId nid1

      let leader = new RaftServer(leadercfg)
      leader.Start()

      setNodeId nid2

      let follower = new RaftServer(followercfg)
      follower.Start()

      Thread.Sleep 10000

      printfn "----------------------------- disposing -------------------------------"

      dispose leader
      dispose follower

      printfn "----------------------------- done -------------------------------"

  //                       _ _
  //  _ __   ___ _ __   __| (_)_ __   __ _
  // | '_ \ / _ \ '_ \ / _` | | '_ \ / _` |
  // | |_) |  __/ | | | (_| | | | | | (_| |
  // | .__/ \___|_| |_|\__,_|_|_| |_|\__, |
  // |_|                             |___/

  let test_log_snapshotting_should_clean_all_logs =
    pending "log snapshotting should clean all logs"

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
      // raft
      test_validate_raft_service_bind_correct_port
      test_validate_correct_req_socket_tracking

      // db
      // test_log_snapshotting_should_clean_all_logs

      // test_validate_follower_joins_leader_after_startup

      // test_follower_join_should_fail_on_duplicate_raftid
      // test_all_rafts_should_share_a_common_distributed_event_log
    ]
