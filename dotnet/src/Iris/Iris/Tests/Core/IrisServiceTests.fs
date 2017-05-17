namespace Iris.Tests

open System
open System.IO
open System.Threading
open System.Text
open Expecto

open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Service.Interfaces
open Iris.Raft
open Iris.Service.Git
open Iris.Service.Iris
open FSharpx.Functional
open Microsoft.FSharp.Control
open ZeroMQ

[<AutoOpen>]
module IrisServiceTests =

  let private mkMachine () =
    { MachineConfig.create "127.0.0.1" None with
        WorkSpace = tmpPath() </> Path.getRandomFileName() }

  let private mkProject (machine: IrisMachine) (site: ClusterConfig) =
    either {
      let name = Path.GetRandomFileName()
      let path = machine.WorkSpace </> filepath name

      let author1 = "karsten"

      let cfg =
        Config.create "leader" machine
        |> Config.addSiteAndSetActive site
        |> Config.setLogLevel (LogLevel.Debug)

      let! project = Project.create path name machine

      let updated =
        { project with
            Path = path
            Author = Some(author1)
            Config = cfg }

      let! commit = Asset.saveWithCommit path User.Admin.Signature updated

      return updated
    }

  let private mkMember baseport (machine: IrisMachine) =
    { Member.create machine.MachineId with
        Port = baseport
        ApiPort = baseport + 1us
        GitPort = baseport + 2us
        WsPort = baseport + 3us }

  let private mkCluster (num: int) =
    either {
      let machines = [ for n in 0 .. num - 1 -> mkMachine () ]

      let baseport = 4000us

      let members =
        List.mapi
          (fun i machine ->
            let port = baseport + uint16 (i * 1000)
            mkMember port machine)
          machines

      let site =
        { ClusterConfig.Default with
            Name = name "Cool Cluster Yo"
            Members = members |> List.map (fun mem -> mem.Id,mem) |> Map.ofList }

      let project =
        List.fold
          (fun (i, project') machine ->
            if i = 0 then
              match mkProject machine site with
              | Right project -> (i + 1, project)
              | Left error -> failwithf "unable to create project: %O" error
            else
              match copyDir project'.Path (machine.WorkSpace </> (project'.Name |> unwrap |> filepath)) with
              | Right () -> (i + 1, project')
              | Left error -> failwithf "error copying project: %O" error)
          (0, Unchecked.defaultof<IrisProject>)
          machines
        |> snd

      let zipped = List.zip members machines

      return (project, zipped)
    }


  //  ___      _     ____                  _            _____         _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___  |_   _|__  ___| |_ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \   | |/ _ \/ __| __/ __|
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/   | |  __/\__ \ |_\__ \
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|   |_|\___||___/\__|___/

  let test_ensure_gitserver_restart_on_premature_exit =
    testCase "ensure gitserver restart on premature exit" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        use checkGitStarted = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 1

        let mem, machine = List.head zipped

        use service = IrisService.create {
          Machine = machine
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs =
          (fun ev ->
            match ev with
            | Git (Started _) -> checkGitStarted.Set() |> ignore
            | _ -> ())
          |> service.Subscribe

        do! service.Start()

        checkGitStarted.WaitOne() |> ignore

        let gitserver = service.GitServer

        let pid = gitserver.Pid

        expect "Git should be running" true Process.isRunning pid

        Process.kill pid

        expect "Git should be running" false Process.isRunning pid

        checkGitStarted.WaitOne() |> ignore

        let gitserver = service.GitServer
        let newpid = gitserver.Pid

        expect "Should be a different pid" false ((=) pid) newpid
        expect "Git should be running" true Process.isRunning newpid
      }
      |> noError

  let test_ensure_iris_server_clones_changes_from_leader =
    testCase "ensure iris server clones changes from leader" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        use checkGitStarted = new AutoResetEvent(false)
        use electionDone = new AutoResetEvent(false)
        use appendDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 2

        let! repo1 = Project.repository project

        let num1 = Git.Repo.commitCount repo1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem1, machine1 = List.head zipped

        let service1 = IrisService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
            | Git (Started _)                              -> checkGitStarted.Set() |> ignore
            | Raft (RaftEvent.StateChanged(oldst, Leader)) -> electionDone.Set() |> ignore
            | Raft (RaftEvent.ApplyLog _)                  -> appendDone.Set() |> ignore
            | _                                            -> ())
          |> service1.Subscribe

        do! service1.Start()

        checkGitStarted.WaitOne() |> ignore

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

        let service2 = IrisService.create {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 =
          (function
            | Git (Started _)                              -> checkGitStarted.Set() |> ignore
            | Raft (RaftEvent.StateChanged(oldst, Leader)) -> electionDone.Set() |> ignore
            | Raft (RaftEvent.ApplyLog _)                  -> appendDone.Set() |> ignore
            | _                                            -> ())
          |> service2.Subscribe

        do! service2.Start()

        checkGitStarted.WaitOne() |> ignore

        electionDone.WaitOne() |> ignore

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

        appendDone.WaitOne() |> ignore
        appendDone.Reset() |> ignore
        appendDone.WaitOne() |> ignore

        dispose service1
        dispose service2

        expect "Instance 1 should have same commit count" (num1 + 1) Git.Repo.commitCount repo1
        expect "Instance 2 should have same commit count" (num2 + 1) Git.Repo.commitCount repo2
      }
      |> noError


  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let irisServiceTests =
    testList "IrisService Tests" [
      test_ensure_gitserver_restart_on_premature_exit
      test_ensure_iris_server_clones_changes_from_leader
    ] |> testSequenced
