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
        let signal = ref 0

        let path = tmpPath()

        let machine = { MachineConfig.create() with WorkSpace = Path.GetDirectoryName path }
        let mem = Member.create machine.MachineId

        let cfg =
          Config.create "leader" machine
          |> Config.setMembers (Map.ofArray [| (mem.Id,mem) |])
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
            printfn "event: %A" ev
            match ev with
            | Git (Started _) -> signal := 1 + !signal
            | _ -> ())
          |> service.Subscribe

        // let start = DateTime.Now

        do! service.LoadProject(name, "admin", "Nsynk")

        // let int1 = DateTime.Now

        let! gitserver = service.GitServer

        // let int2 = DateTime.Now

        let! pid = gitserver.Pid

        // let int3 = DateTime.Now

        expect "Git should be running" true Process.isRunning pid
        expect "Should have emitted one Started event" 1 id !signal

        Process.kill pid

        // let int4 = DateTime.Now

        expect "Git should be running" false Process.isRunning pid

        // let int5 = DateTime.Now

        Thread.Sleep 100
        // let int6 = DateTime.Now

        let! gitserver = service.GitServer
        let! newpid = gitserver.Pid

        // let int7 = DateTime.Now

        expect "Should be a different pid" false ((=) pid) newpid
        expect "Git should be running" true Process.isRunning newpid
        expect "Should have emitted another Started event" 2 id !signal

        // let p tag (dt1: DateTime) (dt2: DateTime) =
        //   (dt2 - dt1).TotalMilliseconds
        //   |> printfn "%s took %f" tag

        // p "start -> int1" start int1
        // p "int1 -> int2" int1 int2
        // p "int2 -> int3" int2 int3
        // p "int3 -> int4" int3 int4
        // p "int4 -> int5" int4 int5
        // p "int5 -> int6" int5 int6
        // p "int6 -> int7" int6 int7
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
