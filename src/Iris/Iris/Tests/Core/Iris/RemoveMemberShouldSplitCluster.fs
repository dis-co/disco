namespace Iris.Tests

open System.IO
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Client
open Iris.Client.Interfaces
open Iris.Service.Interfaces
open Iris.Raft
open Iris.Net

open Common

module RemoveMemberShouldSplitCluster =

  let test =
    testCase "ensure follower forwards fstree to leader" <| fun _ ->
      either {
        use electionDone = new WaitEvent()
        use appendDone = new WaitEvent()
        use pushDone = new WaitEvent()
        use removeDone = new WaitEvent()
        use updateDone = new WaitEvent()

        let! (project, zipped) = mkCluster 2

        let handler = function
          | IrisEvent.GitPush _                           -> pushDone.Set()
          | IrisEvent.StateChanged(oldst, Leader)         -> electionDone.Set()
          | IrisEvent.Append(Origin.Service, AddFsTree _) -> appendDone.Set()
          | IrisEvent.Append(_, UpdateProject p)          -> updateDone.Set()
          | IrisEvent.ConfigurationDone _                 -> removeDone.Set()
          | ev -> () // printfn "ev: %A" ev

        let! repo1 = Project.repository project

        let mem1, machine1 = List.head zipped
        let mem2, machine2 = List.last zipped

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let! service1 = IrisService.create {
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

        let! service2 = IrisService.create {
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

        do leader.RemoveMember otherId

        do! waitFor "removeDone" removeDone /// remove done on first service
        do! waitFor "removeDone" removeDone /// remove done on other service

        Expect.equal (Map.count service1.RaftServer.Raft.Peers) 1 "Should only have one peer"
        Expect.equal (Map.count service2.RaftServer.Raft.Peers) 1 "Should only have one peer"

        do! waitFor "update project" updateDone
        do! waitFor "update project" updateDone

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
