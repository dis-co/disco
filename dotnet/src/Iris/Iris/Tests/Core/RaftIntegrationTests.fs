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

      printfn "connections: %A" (Map.isEmpty leader.State.Connections)

      printfn "----------------------------- done -------------------------------"

  let test_proper_cleanup_of_request_sockets =
    testCase "validate Req sockets are cleaned up properly" <| fun _ ->
      let srv = "tcp://127.0.0.1:8989"

      let n = 12
      let msgs = [ "hi"; "yep"; "bye" ]
      let count = ref 0

      let handler (msg: byte array) =
        lock count <| fun _ ->
          let next = !count + 1
          count := next
        msg

      use rep = new Zmq.Rep(srv, handler)
      rep.Start()

      let socks =
        [ for _ in 0 .. (n - 1) do
            let sock = new Zmq.Req(Id.Create(), srv, 50)
            sock.Start()
            yield sock ]

      let request (str: string) (sck: Zmq.Req) =
        async {
          let result = str |> Encoding.UTF8.GetBytes |> sck.Request
          return result
        }

      msgs
      |> List.fold (fun lst str ->
                   List.fold
                     (fun inner sock -> request str sock :: inner)
                     lst
                     socks)
                  []
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Array.iter (expect "Should be a success" true Either.isSuccess)

      expect "Should have correct number of requests" (n * List.length msgs) id !count

      List.iter dispose socks

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
      // db
      // test_log_snapshotting_should_clean_all_logs

      // raft
      test_proper_cleanup_of_request_sockets
      test_validate_raft_service_bind_correct_port
      // test_validate_follower_joins_leader_after_startup

      // test_follower_join_should_fail_on_duplicate_raftid
      // test_all_rafts_should_share_a_common_distributed_event_log
    ]
