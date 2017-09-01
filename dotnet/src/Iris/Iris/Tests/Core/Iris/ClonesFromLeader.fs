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
        let checkGitStarted = WaitCount.Create()
        let electionDone = WaitCount.Create()
        let appendDone = WaitCount.Create()
        let pushDone = WaitCount.Create()

        let! (project, zipped) = mkCluster 2

        let handler = function
            | IrisEvent.GitPush _                      -> pushDone.Increment()
            | IrisEvent.Started ServiceType.Git        -> checkGitStarted.Increment()
            | IrisEvent.StateChanged(oldst, Leader)    -> electionDone.Increment()
            | IrisEvent.Append(Origin.Raft, AddCue _)  -> appendDone.Increment()
            | IrisEvent.Append(Origin.Raft, Command _) -> appendDone.Increment()
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

        do! waitFor "checkGitStarted to be 1" checkGitStarted 1

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

        do! waitFor "checkGitStarted to be 2" checkGitStarted 2
        do! waitFor "electionDone to be 1" electionDone 1

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

        do! waitFor "appendDone to be 1" appendDone 1
        do! waitFor "appendDone to be 2" appendDone 2

        AppCommand.Save
        |> Command
        |> leader.Append

        do! waitFor "appendDone to be 3" appendDone 3
        do! waitFor "appendDone to be 4" appendDone 4

        do! waitFor "pushDone to be 1" pushDone 1

        dispose service1
        dispose service2

        expect "Instance 1 should have same commit count" (num1 + 1) Git.Repo.commitCount repo1
        expect "Instance 2 should have same commit count" (num2 + 1) Git.Repo.commitCount repo2
      }
      |> noError
