namespace Iris.Tests

open Expecto

open Iris.Core
open Iris.Raft
open Iris.Service
open System.Threading
open LibGit2Sharp

[<AutoOpen>]
module GitTests =
  let mkEnvironment p =
    let machine = MachineConfig.create "127.0.0.1" None

    let tmpdir = mkTmpDir ()

    let mem =
      machine.MachineId
      |> Member.create
      |> Member.setGitPort (port p)

    let config =
      Config.create "Test Project" machine
      |> Config.setMembers (Map.ofArray [| (mem.Id,mem) |])
      |> Config.setLogLevel Debug

    let project =
      let p =
        Project.create (Project.ofFilePath tmpdir) "Test Project" machine
        |> Either.get
      in { p with Config = config }

    machine, tmpdir, project, mem, project

  //  ____                      _
  // |  _ \ ___ _ __ ___   ___ | |_ ___  ___
  // | |_) / _ \ '_ ` _ \ / _ \| __/ _ \/ __|
  // |  _ <  __/ | | | | | (_) | ||  __/\__ \
  // |_| \_\___|_| |_| |_|\___/ \__\___||___/

  let test_correct_remote_list =
    testCase "Validate Correct Remote Listing" <| fun _ ->
      let repo = testRepo()
      let remotes = Git.Config.remotes repo

      expect "Should be empty" Map.empty id remotes

      let upstream = url "git@bla.com"
      Git.Config.addRemote repo "origin" upstream
      |> ignore

      let remote = Git.Config.tryFindRemote repo "origin" |> Option.get
      expect "Should be correct upstream" upstream (fun (remote: Remote) -> url remote.Url) remote

  let test_remove_remote =
    testCase "Validate Removal Of Remote" <| fun _ ->
      let repo = testRepo()
      let remotes = Git.Config.remotes repo

      let name = "origin"
      let upstream = url "git@bla.com"
      Git.Config.addRemote repo name upstream
      |> ignore

      let remote = Git.Config.tryFindRemote repo name |> Option.get
      expect "Should be correct upstream" upstream (fun (rmt: Remote) -> url rmt.Url) remote

      Git.Config.delRemote repo name
      |> ignore

      let remotes = Git.Config.remotes repo
      expect "Should be empty" Map.empty id remotes

  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  // |____/ \___|_|    \_/ \___|_|

  let test_server_startup =
    testCase "Server startup" <| fun _ ->
      either {
        let uuid, tmpdir, project, mem, path =
          mkEnvironment 10000us

        use gitserver = GitServer.create mem path
        do! gitserver.Start()

        expect "Should be running" true Service.isRunning gitserver.Status
      }
      |> noError

  let test_server_startup_should_error_on_eaddrinuse =
    testCase "Server should fail on EADDRINUSE" <| fun _ ->
      either {
        let uuid, tmpdir, project, mem, path =
          mkEnvironment 10001us

        use started = new AutoResetEvent(false)

        let handleStarted = function
          | IrisEvent.Started _ -> started.Set() |> ignore
          | _ -> ()

        use gitserver1 = GitServer.create mem path
        use gobs1 = gitserver1.Subscribe(handleStarted)
        do! gitserver1.Start()

        do! waitOrDie "started" started

        expect "Should be running" true Service.isRunning gitserver1.Status

        use gitserver2 = GitServer.create mem path
        do! match gitserver2.Start() with
            | Right ()   -> Left (Other("test","Should have failed to start"))
            | Left error -> Right ()

        expect "Should not be runnning" true Service.isStopped gitserver2.Status
      }
      |> noError

  let test_server_availability =
    testCase "Server availability" <| fun _ ->
      either {
        let port = 10002us
        let started = new AutoResetEvent(false)

        let handleStarted = function
          | IrisEvent.Started _ -> started.Set() |> ignore
          | _ -> ()

        let uuid, tmpdir, project, mem, path =
          mkEnvironment port

        use gitserver = GitServer.create mem path
        use gobs1 = gitserver.Subscribe(handleStarted)

        do! gitserver.Start()

        do! waitOrDie "started" started

        expect "Should be running" true Service.isRunning gitserver.Status

        let target = mkTmpDir ()

        let repo =
          mem
          |> Uri.gitUri path.Name
          |> unwrap
          |> Git.Repo.clone target

        expect "Should have successfully clone project" true Either.isSuccess repo
      }
      |> noError

  //  _____         _     _     _     _
  // |_   _|__  ___| |_  | |   (_)___| |_
  //   | |/ _ \/ __| __| | |   | / __| __|
  //   | |  __/\__ \ |_  | |___| \__ \ |_
  //   |_|\___||___/\__| |_____|_|___/\__|

  let gitTests =
    testList "Git Tests" [
      // REMOTES
      test_correct_remote_list
      test_remove_remote

      // SERVER
      test_server_startup
      test_server_availability
      test_server_startup_should_error_on_eaddrinuse
    ] |> testSequenced
