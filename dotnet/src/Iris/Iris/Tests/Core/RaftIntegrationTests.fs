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
open Iris.Service.Raft
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
      either {
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

        printfn "create leader"

        let! leader = RaftServer.create leadercfg

        do! expectE "Leader should have no connections" 0 count leader.Connections

        printfn "start leader"

        do! leader.Start()

        printfn "start leader done"

        do! expectE "Leader should have one connection" 1 count leader.Connections

        setNodeId nid2

        printfn "create follower"

        let! follower = RaftServer.create followercfg

        do! expectE "Follower should have no connections" 0 count follower.Connections

        printfn "start follower"

        do! follower.Start()

        printfn "start follower done"

        do! expectE "Follower should have one connection" 1 count follower.Connections

        dispose leader
        dispose follower

        do! expectE "Leader should have no connections" 0 count leader.Connections
        do! expectE "Follower should have no connections" 0 count follower.Connections
      }
      |> noError

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      either {
        let port = 12000us

        let! nodeid = Config.getNodeId()

        let node =
          nodeid
          |> Node.create
          |> Node.setPort port

        let leadercfg =
          Config.create "leader"
          |> Config.addNode node

          // |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create leadercfg

        do! leader.Start()

        do! expectE "Should be running" true Service.isRunning leader.Status

        use! follower = RaftServer.create leadercfg

        do! match follower.Start() with
            | Right ()   -> Left (Other "Should have failed to start")
            | Left error -> Right ()

        do! expectE "Should be failed" true Service.hasFailed follower.Status
      }
      |> noError

  let test_validate_follower_joins_leader_after_startup =
    testCase "validate follower joins leader after startup" <| fun _ ->
      either {
        let state = ref None

        let setState (ev: RaftEvent) =
          match !state, ev with
          | None, StateChanged (_,Leader) ->
            lock state <| fun _ ->
              state := Some Leader
          | _ -> ()

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

        use! leader = RaftServer.create leadercfg

        use obs1 = leader.Subscribe setState

        do! leader.Start()

        setNodeId nid2

        use! follower = RaftServer.create followercfg

        use obs2 = follower.Subscribe setState

        do! follower.Start()

        let! state1 = leader.State
        let! state2 = follower.State

        max
          state1.Raft.ElectionTimeout
          state2.Raft.ElectionTimeout
        |> (int >> ((+) 100))
        |> Thread.Sleep

        expect "Should have elected a leader" (Some Leader) id !state
      }
      |> noError

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
      test_validate_correct_req_socket_tracking
      // test_validate_raft_service_bind_correct_port
      // test_validate_follower_joins_leader_after_startup

      // db
      // test_log_snapshotting_should_clean_all_logs


      // test_follower_join_should_fail_on_duplicate_raftid
      // test_all_rafts_should_share_a_common_distributed_event_log
    ]
