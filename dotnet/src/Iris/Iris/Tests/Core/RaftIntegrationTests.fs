namespace Iris.Tests

open System
open System.Threading
open System.Text
open Expecto

open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Service.Interfaces
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
        let machine1 = MachineConfig.create ()
        let machine2 = MachineConfig.create ()

        let mem1 =
          machine1.MachineId
          |> Member.create
          |> Member.setPort 8000us

        let mem2 =
          machine2.MachineId
          |> Member.create
          |> Member.setPort 8001us

        let leadercfg =
          Config.create "leader" machine1
          |> Config.setMembers (Map.ofArray [| (mem1.Id,mem1); (mem2.Id,mem2) |])
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          Config.create "follower" machine2
          |> Config.setMembers (Map.ofArray [| (mem1.Id,mem1); (mem2.Id,mem2) |])
          |> Config.setLogLevel (LogLevel.Debug)

        let! leader = RaftServer.create ()
        do! leader.Load(leadercfg)
        do! expectE "Leader should have one connection" 1 count leader.Connections

        let! follower = RaftServer.create ()
        do! follower.Load(followercfg)
        do! expectE "Follower should have one connection" 1 count follower.Connections

        dispose leader
        dispose follower

        do! expectE "Leader should be stopped"   true Service.isStopped leader.Status
        do! expectE "Follower should be stopped" true Service.isStopped follower.Status
      }
      |> noError

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      either {
        let port = 12000us
        let machine = MachineConfig.create ()

        let mem =
          machine.MachineId
          |> Member.create
          |> Member.setPort port

        let leadercfg =
          Config.create "leader" machine
          |> Config.addMember mem

        use! leader = RaftServer.create ()
        do! leader.Load leadercfg

        do! expectE "Should be running" true Service.isRunning leader.Status

        use! follower = RaftServer.create ()

        do! match follower.Load leadercfg with
            | Right ()   -> Left (Other("loco","Should have failed to start"))
            | Left error -> Right ()

        do! expectE "Should be failed" true Service.isStopped follower.Status
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

        let machine1 = MachineConfig.create ()
        let machine2 = MachineConfig.create ()

        let mem1 =
          machine1.MachineId
          |> Member.create
          |> Member.setPort 8000us

        let mem2 =
          machine2.MachineId
          |> Member.create
          |> Member.setPort 8001us

        let leadercfg =
          Config.create "leader" machine1
          |> Config.setMembers (Map.ofArray [| (mem1.Id,mem1); (mem2.Id,mem2) |])
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          Config.create "follower" machine2
          |> Config.setMembers (Map.ofArray [| (mem1.Id,mem1); (mem2.Id,mem2) |])
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create ()

        use obs1 = leader.Subscribe setState

        do! leader.Load leadercfg

        use! follower = RaftServer.create ()

        use obs2 = follower.Subscribe setState

        do! follower.Load(followercfg)

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
      test_validate_raft_service_bind_correct_port
      test_validate_follower_joins_leader_after_startup

      // db
      // test_log_snapshotting_should_clean_all_logs


      // test_follower_join_should_fail_on_duplicate_raftid
      // test_all_rafts_should_share_a_common_distributed_event_log
    ] |> testSequenced
