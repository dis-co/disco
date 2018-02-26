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

open Common

module RemoveMemberShouldSplitCluster =

  let test =
    testCase "ensure follower forwards fstree to leader" <| fun _ ->
      result {
        use electionDone = new WaitEvent()
        use appendDone = new WaitEvent()
        use pushDone = new WaitEvent()
        use removeDone = new WaitEvent()

        let! (project, zipped) = mkCluster 2

        let handler = function
          | DiscoEvent.GitPush _                           -> pushDone.Set()
          | DiscoEvent.StateChanged(oldst, Leader)         -> electionDone.Set()
          | DiscoEvent.Append(Origin.Service, AddFsTree _) -> appendDone.Set()
          | DiscoEvent.ConfigurationDone _                 -> removeDone.Set()
          | ev -> () // printfn "ev: %A" ev

        let! repo1 = Project.repository project

        let mem1, machine1 = List.head zipped
        let mem2, machine2 = List.last zipped

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe handler
        do! service1.Start()

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| start

        let! repo2 = Project.repository {
          project with
            Path = machine2.WorkSpace </> (project.Name |> unwrap |> filepath)
        }

        let! service2 = DiscoService.create {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe handler
        do! service2.Start()

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ then cut the cord

        do! waitFor "electionDone" electionDone

        let leader, otherId =
          match service1.RaftServer.IsLeader, service2.RaftServer.IsLeader with
          | true, false -> service1, service2.Machine.MachineId
          | false, true -> service2, service1.Machine.MachineId
          | false, false -> failwith "no leader"
          | true, true -> failwith "two leaders!!"

        do leader.RemoveMachine otherId

        do! waitFor "removeDone" removeDone /// remove done on first service
        do! waitFor "removeDone" removeDone /// remove done on other service

        Expect.equal (Map.count service1.RaftServer.Raft.Peers) 1 "Should only have one peer"
        Expect.equal (Map.count service2.RaftServer.Raft.Peers) 1 "Should only have one peer"

        let activeSite =
          leader.State
          |> State.project
          |> Project.config
          |> Config.getActiveSite
          |> Option.get

        Expect.equal (Map.count activeSite.Members) 1 "Should have only one member in site"

        dispose service1
        dispose service2
      }
      |> noError
