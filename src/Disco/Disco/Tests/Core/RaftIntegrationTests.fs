(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System.Threading
open Expecto

open Disco.Core
open Disco.Service.Interfaces
open Disco.Service
open Disco.Raft
open Disco.Net

[<AutoOpen>]
module RaftIntegrationTests =

  //  ____        __ _     _____         _
  // |  _ \ __ _ / _| |_  |_   _|__  ___| |_ ___
  // | |_) / _` | |_| __|   | |/ _ \/ __| __/ __|
  // |  _ < (_| |  _| |_    | |  __/\__ \ |_\__ \
  // |_| \_\__,_|_|  \__|   |_|\___||___/\__|___/

  let test_validate_correct_req_socket_tracking =
    testCase "validate correct req socket tracking" <| fun _ ->
      result {
        let machine1 = MachineConfig.create "127.0.0.1" None
        let machine2 = MachineConfig.create "127.0.0.1" None

        let mem1 =
          machine1
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8000us)

        let mem2 =
          machine2
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8001us)

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members =
                Map.ofArray [|
                  (mem1.Id, mem1)
                  (mem2.Id, mem2)
                |] }
        let leadercfg =
          machine1
          |> Config.create
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          machine2
          |> Config.create
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let! leader = RaftServer.create leadercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }
        do! leader.Start()
        expect "Leader should have one connection" 1 count leader.Connections

        let! follower = RaftServer.create followercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }
        do! follower.Start()
        expect "Follower should have one connection" 1 count follower.Connections

        dispose follower
        dispose leader

        expect "Leader should be disposed"   true Service.isDisposed leader.Status
        expect "Follower should be disposed" true Service.isDisposed follower.Status
      }
      |> noError

  let test_validate_raft_service_bind_correct_port =
    testCase "validate raft service bind correct port" <| fun _ ->
      result {
        use started = new WaitEvent()
        let port = port 12000us
        let machine = MachineConfig.create "127.0.0.1" None

        let mem =
          machine
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort port

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem.Id, mem) |] }

        let leadercfg =
          machine
          |> Config.create
          |> Config.addSiteAndSetActive site

        use! leader = RaftServer.create leadercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }

        let handle = function
          | DiscoEvent.Started ServiceType.Raft -> started.Set() |> ignore
          | _ -> ()

        use sobs = leader.Subscribe(handle)

        do! leader.Start()

        do! waitFor "started" started

        expect "Should be running" true Service.isRunning leader.Status

        use! follower = RaftServer.create leadercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }

        do! match follower.Start() with
            | Ok _ -> Error (Other("test","follower should have failed"))
            | Error _ -> Ok ()

        expect "Should be failed" true Service.hasFailed follower.Status
      }
      |> noError

  let test_validate_follower_joins_leader_after_startup =
    testCase "validate follower joins leader after startup" <| fun _ ->
      result {
        use check1 = new WaitEvent()

        let setState (id: DiscoId) (are: WaitEvent) = function
          | DiscoEvent.StateChanged (_,Leader) ->
            id
            |> sprintf "%O became leader"
            |> Logger.debug "test"
            are.Set() |> ignore
          | _ -> ()

        let machine1 = MachineConfig.create "127.0.0.1" None
        let machine2 = MachineConfig.create "127.0.0.1" None

        let mem1 =
          machine1
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8000us)

        let mem2 =
          machine2
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8001us)

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1)
                                       (mem2.Id, mem2) |] }

        let leadercfg =
          machine1
          |> Config.create
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          machine2
          |> Config.create
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create leadercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }

        use obs1 = leader.Subscribe (setState mem1.Id check1)

        do! leader.Start()

        use! follower = RaftServer.create followercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() = None
          }

        use obs2 = follower.Subscribe (setState mem2.Id check1)

        do! follower.Start()

        do! waitFor "Leader-Check" check1
      }
      |> noError

  let test_log_snapshotting_should_clean_all_logs =
    testCase "log snapshotting should clean all logs" <| fun _ ->
      result {
        use snapshotCheck = new WaitEvent()
        use expectedCheck = new WaitEvent()

        let state = ref None

        let machine1 = MachineConfig.create "127.0.0.1" None

        let store = Store(State.Empty)

        let mem1 =
          machine1
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8000us)

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1) |] }

        let leadercfg =
          machine1
          |> Config.create
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create leadercfg {
            new IRaftSnapshotCallbacks with
              member self.RetrieveSnapshot() = None
              member self.PrepareSnapshot() =
                snapshotCheck.Set() |> ignore
                Some store.State
          }

        let expected = int leadercfg.Raft.MaxLogDepth * 2

        let evHandler = function
          | DiscoEvent.Append(Origin.Raft, sm) ->
            store.Dispatch sm
            if store.State.Users.Count = expected then
              expectedCheck.Set() |> ignore
          | _ -> ()

        use obs1 = leader.Subscribe evHandler
        do! leader.Start()

        let cmds =
          [ for n in 0 .. expected - 1 do
              yield AddUser (mkUser ()) ]
          |> List.map leader.Append

        do! waitFor "snapshot" snapshotCheck
        do! waitFor "expectedCheck" expectedCheck

        expect "Should have expected number of Users" expected id store.State.Users.Count
      }
      |> noError

  let test_validate_add_member_works =
    testCase "validate add member works" <| fun _ ->
      result {
        use added = new WaitEvent()
        use configured = new WaitEvent()
        use check1 = new WaitEvent()
        use check2 = new WaitEvent()

        let setState (id: DiscoId) (are: WaitEvent) = function
          | DiscoEvent.StateChanged (_,Leader) ->
            id
            |> sprintf "%O became leader"
            |> Logger.debug "test"
            are.Set() |> ignore
          | DiscoEvent.StateChanged (_,Follower) ->
            id
            |> sprintf "%O became follower"
            |> Logger.debug "test"
            are.Set() |> ignore
          | DiscoEvent.Append(Origin.Raft, AddMachine mem) ->
            mem.Id
            |> sprintf "%O was added"
            |> Logger.debug "test"
            added.Set() |> ignore
          | DiscoEvent.ConfigurationDone mems ->
            Array.length mems
            |> sprintf "new cluster configuration active with %d members"
            |> Logger.debug "test"
            configured.Set() |> ignore
          | _ -> ()

        let machine1 = MachineConfig.create "127.0.0.1" None
        let machine2 = MachineConfig.create "127.0.0.1" None

        let mem1 =
          machine1
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8000us)

        let mem2 =
          machine2
          |> Machine.toClusterMember
          |> ClusterMember.setRaftPort (port 8001us)

        let raftMem2 =
          machine2
          |> Machine.toRaftMember
          |> Member.setRaftPort (port 8001us)

        let site1 =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (mem1.Id, mem1) |] }

        let site2 =
          { site1 with Members = Map.ofArray [| (mem2.Id, mem2) |] }

        let leadercfg =
          machine1
          |> Config.create
          |> Config.addSiteAndSetActive site1
          |> Config.setLogLevel (LogLevel.Debug)

        let followercfg =
          machine2
          |> Config.create
          |> Config.addSiteAndSetActive site2
          |> Config.setLogLevel (LogLevel.Debug)

        use! leader = RaftServer.create leadercfg {
          new IRaftSnapshotCallbacks with
            member self.RetrieveSnapshot() = None
            member self.PrepareSnapshot() = None
        }

        use obs1 = leader.Subscribe (setState mem1.Id check1)

        do! leader.Start()

        use! follower = RaftServer.create followercfg {
          new IRaftSnapshotCallbacks with
            member self.RetrieveSnapshot() = None
            member self.PrepareSnapshot() = None
        }

        use obs2 = follower.Subscribe (setState mem2.Id check2)

        do! follower.Start()

        do! waitFor "check1" check1
        do! waitFor "check2" check2

        leader.AddMachine raftMem2           // add mem2 to cluster

        do! waitFor "added" added
        do! waitFor "configured" configured
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let raftIntegrationTests =
    testList "Raft Integration Tests" [
      test_validate_correct_req_socket_tracking
      test_validate_raft_service_bind_correct_port
      test_validate_follower_joins_leader_after_startup
      test_log_snapshotting_should_clean_all_logs
      test_validate_add_member_works
    ] |> testSequenced
