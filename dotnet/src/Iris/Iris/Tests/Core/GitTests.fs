namespace Iris.Tests

open Expecto

open Iris.Core
open Iris.Raft
open Iris.Service
open Iris.Serialization.Raft
open System.Net
open System.Threading
open FlatBuffers
open FSharpx.Functional
open LibGit2Sharp
open System.IO

[<AutoOpen>]
module GitTests =
  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  let mkTmpDir () =
    let fn =
      Path.GetTempFileName()
      |> Path.GetFileName

    Directory.GetCurrentDirectory() </> "tmp" </> fn
    |> Directory.CreateDirectory

  let testRepo () =
    mkTmpDir ()
    |> fun info -> info.FullName
    |> fun path ->
      Repository.Init path |> ignore
      new Repository(path)

  let mkEnvironment port =
    let uuid = mkUuid ()
    setNodeId uuid

    let user = User.Admin

    let tmpdir = mkTmpDir ()

    let node =
      Id uuid
      |> Node.create
      |> Node.setGitPort port

    let config =
      "cool project mate"
      |> Config.create
      |> Config.setNodes [| node |]
      |> Config.setLogLevel Debug

    let commit, project =
      Project.create "cool-project"
      |> Project.updatePath tmpdir.FullName
      |> Project.updateConfig config
      |> Project.save user.Signature "Project Initialized"
      |> Either.get

    uuid, tmpdir, project

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

      let upstream = "git@bla.com"
      Git.Config.addRemote repo "origin" upstream

      let remotes = Git.Config.remotes repo
      expect "Should be correct upstream" upstream (Map.find "origin") remotes

  let test_remove_remote =
    testCase "Validate Removal Of Remote" <| fun _ ->
      let repo = testRepo()
      let remotes = Git.Config.remotes repo

      let name = "origin"
      let upstream = "git@bla.com"
      Git.Config.addRemote repo name upstream

      let remotes = Git.Config.remotes repo
      expect "Should be correct upstream" upstream (Map.find name) remotes

      Git.Config.delRemote repo name

      let remotes = Git.Config.remotes repo
      expect "Should be empty" Map.empty id remotes


  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  // |____/ \___|_|    \_/ \___|_|

  let test_server_startup =
    testCase "Server startup" <| fun _ ->
      let uuid, tmpdir, project =
        mkEnvironment 10000us

      use gitserver = new GitServer(project)
      gitserver.Start()

      expect "Should be running" true Service.isRunning gitserver.Status

  let test_server_startup_should_error_on_eaddrinuse =
    testCase "Server should fail on EADDRINUSE" <| fun _ ->
      let uuid, tmpdir, project =
        mkEnvironment 10001us

      use gitserver1 = new GitServer(project)
      // gitserver1.OnLogMsg <- Logger.log (Id uuid) Debug
      gitserver1.Start()

      expect "Should be running" true Service.isRunning gitserver1.Status

      use gitserver2 = new GitServer(project)
      gitserver2.Start()

      expect "Should have failed" true Service.hasFailed gitserver2.Status

  let test_server_availability =
    testCase "Server availability" <| fun _ ->
      let port = 10002us

      let uuid, tmpdir, project =
        mkEnvironment port

      use gitserver = new GitServer(project)
      gitserver.Start()

      expect "Should be running" true Service.isRunning gitserver.Status

      let target = mkTmpDir ()

      let repo =
        tmpdir.FullName
        |> Path.baseName
        |> sprintf "git://localhost:%d/%s/.git" port
        |> Git.Repo.clone target.FullName

      expect "Should have successfully clone project" true Either.isSuccess repo

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
    ]
