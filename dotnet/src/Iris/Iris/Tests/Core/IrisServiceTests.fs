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
        let signal = ref 0

        let machine = MachineConfig.create ()
        let node = Node.create machine.MachineId

        let cfg =
          Config.create "leader" machine
          |> Config.setNodes [| node |]
          |> Config.setLogLevel (LogLevel.Debug)

        let name =
          Path.GetTempFileName()
          |> Path.GetFileName

        let author1 = "karsten"
        let path = Path.Combine(Directory.GetCurrentDirectory(),"tmp", name)

        let! (commit, project) =
          { Project.create name machine with
              Path = path
              Author = Some(author1)
              Config = cfg }
          |> Project.saveProject User.Admin

        let path = Project.filePath project

        let raw =
          Project.filePath project
          |> File.ReadAllText

        use! service = IrisService.create machine true
        use oobs =
          (fun ev ->
            match ev with
            | Git (Started _) -> signal := 1 + !signal
            | _ -> ())
          |> service.Subscribe

        do! service.Load(path)

        let! gitserver = service.GitServer
        let! pid = gitserver.Pid

        expect "Git should be running" true Process.isRunning pid
        expect "Should have emitted one Started event" 1 id !signal

        Process.kill pid

        expect "Git should be running" false Process.isRunning pid

        Thread.Sleep 100

        let! gitserver = service.GitServer
        let! newpid = gitserver.Pid

        expect "Should be a different pid" false ((=) pid) newpid
        expect "Git should be running" true Process.isRunning newpid
        expect "Should have emitted another Started event" 2 id !signal
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
    ]
