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
open Iris.Zmq
open Iris.Service.Raft
open FSharpx.Functional
open Microsoft.FSharp.Control
open ZeroMQ
open Hopac

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
        use ctx = new ZContext()
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        let machine1 = MachineConfig.create "127.0.0.1" None
        let machine2 = MachineConfig.create "127.0.0.1" None

        let mem1 =
          machine1.MachineId
          |> Member.create
          |> Member.setPort 8000us

        let mem2 =
          machine2.MachineId
          |> Member.create
          |> Member.setPort 8001us

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1)
                                       (mem2.Id, mem2) |] }
        let leadercfg =
          Config.create "leader" machine1
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          Config.create "follower" machine2
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let! leader = RaftServer.create ctx leadercfg
        do! leader.Start()
        expect "Leader should have one connection" 1 count leader.Connections

        let! follower = RaftServer.create ctx followercfg
        do! follower.Start()
        expect "Follower should have one connection" 1 count follower.Connections

        dispose leader
        dispose follower

        expect "Leader should be disposed"   true Service.isDisposed leader.Status
        expect "Follower should be disposed" true Service.isDisposed follower.Status
      }
      |> noError

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        use ctx = new ZContext()
        use started = new AutoResetEvent(false)
        let port = 12000us
        let machine = MachineConfig.create "127.0.0.1" None

        let mem =
          machine.MachineId
          |> Member.create
          |> Member.setPort port

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem.Id, mem) |] }

        let leadercfg =
          Config.create "leader" machine
          |> Config.addSiteAndSetActive site

        use! leader = RaftServer.create ctx leadercfg

        let handle = function
          | RaftEvent.Started -> started.Set() |> ignore
          | _ -> ()

        use sobs = leader.Subscribe(handle)

        do! leader.Start()

        started.WaitOne(int Constants.REQ_TIMEOUT) |> ignore

        expect "Should be running" true Service.isRunning leader.Status

        use! follower = RaftServer.create ctx leadercfg

        do! match follower.Start() with
            | Right _ -> Left (Other("test","follower should have failed"))
            | Left _ -> Right ()

        expect "Should be failed" true Service.hasFailed follower.Status
      }
      |> noError

  let test_validate_follower_joins_leader_after_startup =
    testCase "validate follower joins leader after startup" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        use ctx = new ZContext()
        use check1 = new AutoResetEvent(false)
        use check2 = new AutoResetEvent(false)

        let setState (id: Id) (are: AutoResetEvent) = function
          | RaftEvent.StateChanged (_,Leader) ->
            id
            |> sprintf "%O became leader"
            |> Logger.debug "test"
            are.Set() |> ignore
          | RaftEvent.StateChanged (_,Follower) ->
            id
            |> sprintf "%O became follower"
            |> Logger.debug "test"
            are.Set() |> ignore
          | _ -> ()

        let machine1 = MachineConfig.create "127.0.0.1" None
        let machine2 = MachineConfig.create "127.0.0.1" None

        let mem1 =
          machine1.MachineId
          |> Member.create
          |> Member.setPort 8000us

        let mem2 =
          machine2.MachineId
          |> Member.create
          |> Member.setPort 8001us

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1)
                                       (mem2.Id, mem2) |] }

        let leadercfg =
          Config.create "leader" machine1
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          Config.create "follower" machine2
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create ctx leadercfg

        use obs1 = leader.Subscribe (setState mem1.Id check1)

        do! leader.Start()

        use! follower = RaftServer.create ctx followercfg

        use obs2 = follower.Subscribe (setState mem2.Id check2)

        do! follower.Start()

        check1.WaitOne() |> ignore
        check2.WaitOne() |> ignore
      }
      |> noError

  let test_log_snapshotting_should_clean_all_logs =
    testCase "log snapshotting should clean all logs" <| fun _ ->
      either {
        // Tracing.enable()

        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)
        use ctx = new ZContext()
        use snapshotCheck = new AutoResetEvent(false)
        use expectedCheck = new AutoResetEvent(false)

        let state = ref None

        let machine1 = MachineConfig.create "127.0.0.1" None

        let store = Store(State.Empty)

        let mem1 =
          machine1.MachineId
          |> Member.create
          |> Member.setPort 8000us

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1) |] }

        let leadercfg =
          Config.create "leader" machine1
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create ctx leadercfg

        let expected = int leadercfg.Raft.MaxLogDepth * 2

        let evHandler (ev: RaftEvent) =
          match ev with
          | RaftEvent.ApplyLog sm ->
            store.Dispatch sm
            if store.State.Users.Count = expected then
              expectedCheck.Set() |> ignore
          | RaftEvent.CreateSnapshot ch ->
            store.State
            |> Some
            |> Ch.send ch
            |> Hopac.queue
            snapshotCheck.Set() |> ignore
          | _ -> ()

        use obs1 = leader.Subscribe evHandler
        do! leader.Start()

        let cmds =
          [ for n in 0 .. expected - 1 do
              yield AddUser (mkUser ()) ]
          |> List.map leader.Append

        snapshotCheck.WaitOne() |> ignore
        expectedCheck.WaitOne() |> ignore

        expect "Should have expected number of Users" expected id store.State.Users.Count
      }
      |> noError

  //                       _ _
  //  _ __   ___ _ __   __| (_)_ __   __ _
  // | '_ \ / _ \ '_ \ / _` | | '_ \ / _` |
  // | |_) |  __/ | | | (_| | | | | | (_| |
  // | .__/ \___|_| |_|\__,_|_|_| |_|\__, |
  // |_|                             |___/

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
      test_log_snapshotting_should_clean_all_logs

      // test_follower_join_should_fail_on_duplicate_raftid
      // test_all_rafts_should_share_a_common_distributed_event_log
    ] |> testSequenced
