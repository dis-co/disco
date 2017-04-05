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
  //  ___      _     ____                  _            _____         _
  // |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___  |_   _|__  ___| |_ ___
  //  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \   | |/ _ \/ __| __/ __|
  //  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/   | |  __/\__ \ |_\__ \
  // |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|   |_|\___||___/\__|___/

  let test_ensure_gitserver_restart_on_premature_exit =
    testCase "ensure gitserver restart on premature exit" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        use checkStarted = new AutoResetEvent(false)

        let path = tmpPath()

        let machine = { MachineConfig.create() with WorkSpace = Path.GetDirectoryName path }
        let mem = Member.create machine.MachineId

        let site =
          { ClusterConfig.Default with
              Name = "Cool Cluster Yo"
              Members = Map.ofArray [| (mem.Id,mem) |] }

        let cfg =
          Config.create "leader" machine
          |> Config.addSiteAndSetActive site
          |> Config.setLogLevel (LogLevel.Debug)

        let name = Path.GetFileName path

        let author1 = "karsten"

        let! project = Project.create path name machine

        let updated =
          { project with
              Path = path
              Author = Some(author1)
              Config = cfg }

        let! commit = Asset.saveWithCommit path User.Admin.Signature updated

        let raw =
          Project.filePath project
          |> File.ReadAllText

        use! service = IrisService.create machine (fun _ -> Async.result (Right "ok"))

        use oobs =
          (fun ev ->
            match ev with
            | Git (Started _) -> checkStarted.Set() |> ignore
            | _ -> ())
          |> service.Subscribe

        do! service.LoadProject(name, "admin", "Nsynk")

        checkStarted.WaitOne() |> ignore

        let! gitserver = service.GitServer

        let! pid = gitserver.Pid

        expect "Git should be running" true Process.isRunning pid

        Process.kill pid

        expect "Git should be running" false Process.isRunning pid

        checkStarted.WaitOne() |> ignore

        let! gitserver = service.GitServer
        let! newpid = gitserver.Pid

        expect "Should be a different pid" false ((=) pid) newpid
        expect "Git should be running" true Process.isRunning newpid
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
    ] |> testSequenced
