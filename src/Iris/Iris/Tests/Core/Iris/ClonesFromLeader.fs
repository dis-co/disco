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

module ClonesFromLeader =

  let test =
    testCase "ensure iris server clones changes from leader" <| fun _ ->
      either {
        use checkGitStarted = new WaitEvent()
        use electionDone = new WaitEvent()
        use appendDone = new WaitEvent()
        use pushDone = new WaitEvent()

        let! (project, zipped) = mkCluster 2

        let handler = function
            | IrisEvent.GitPush _                      -> pushDone.Set()
            | IrisEvent.Started ServiceType.Git        -> checkGitStarted.Set()
            | IrisEvent.StateChanged(oldst, Leader)    -> electionDone.Set()
            | IrisEvent.Append(Origin.Raft, AddCue _)  -> appendDone.Set()
            | IrisEvent.Append(Origin.Raft, Command _) -> appendDone.Set()
            | _ -> ()

        let! repo1 = Project.repository project

        let num1 = Git.Repo.commitCount repo1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem1, machine1 = List.head zipped

        let! service1 = IrisService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe handler

        do! service1.Start()

        do! waitFor "checkGitStarted" checkGitStarted

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| start

        let mem2, machine2 = List.last zipped

        let! repo2 = Project.repository {
          project with
            Path = machine2.WorkSpace </> (project.Name |> unwrap |> filepath)
        }

        let num2 = Git.Repo.commitCount repo2

        let! service2 = IrisService.create {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe handler

        do! service2.Start()

        do! waitFor "checkGitStarted" checkGitStarted
        do! waitFor "electionDone" electionDone

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ do some work

        let raft1 = service1.RaftServer
        let raft2 = service2.RaftServer

        let leader =
          match raft1.IsLeader, raft2.IsLeader with
          | true, false  -> raft1
          | false, true  -> raft2
          | false, false -> failwith "no leader is bad news"
          | true, true   -> failwith "two leaders is really bad news"

        mkCue()
        |> AddCue
        |> leader.Append

        do! waitFor "appendDone" appendDone
        do! waitFor "appendDone" appendDone

        AppCommand.Save
        |> Command
        |> leader.Append

        do! waitFor "appendDone" appendDone
        do! waitFor "appendDone" appendDone

        do! waitFor "pushDone" pushDone

        dispose service1
        dispose service2

        expect "Instance 1 should have same commit count" (num1 + 1) Git.Repo.commitCount repo1
        expect "Instance 2 should have same commit count" (num2 + 1) Git.Repo.commitCount repo2
      }
      |> noError
