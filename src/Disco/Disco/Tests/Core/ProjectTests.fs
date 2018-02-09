(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System
open System.IO
open System.Linq
open Expecto
open Disco.Core
open Disco.Raft
open LibGit2Sharp

[<AutoOpen>]
module ProjectTests =

  //   _                    _    ______
  //  | |    ___   __ _  __| |  / / ___|  __ ___   _____
  //  | |   / _ \ / _` |/ _` | / /\___ \ / _` \ \ / / _ \
  //  | |__| (_) | (_| | (_| |/ /  ___) | (_| |\ V /  __/
  //  |_____\___/ \__,_|\__,_/_/  |____/ \__,_| \_/ \___|ed
  //
  let loadSaveTest =
    testCase "Save/Load Project should render equal project values" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let! project = Project.create path name machine

        let result = Asset.loadWithMachine project.Path machine

        do! expectE "Projects should be equal" true ((=) project) result
      }
      |> noError

  //  ____  _
  // |  _ \(_)_ __ _   _
  // | | | | | '__| | | |
  // | |_| | | |  | |_| |
  // |____/|_|_|   \__, |
  //               |___/

  let dirtyTest =
    testCase "Project create should render clean repo" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let! project = Project.create path name machine
        let! repo = Project.repository project
        let! status = Git.Repo.status repo
        let untracked = status.Untracked.Count()

        expect "Projects should not be dirty" false id status.IsDirty
        expect "Projects should not have untracked files" 0 id untracked
      }
      |> noError

  //  ____       _   _
  // |  _ \ __ _| |_| |__
  // | |_) / _` | __| '_ \
  // |  __/ (_| | |_| | | |
  // |_|   \__,_|\__|_| |_|

  let relpathTest =
    testCase "Project create should only work on absolute paths" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = Path.getRandomFileName()

        let result = Project.create path (unwrap path) machine

        expect "Create should have failed" false Result.isSuccess result

        return!
          match result with
          | Error (GitError("Git.Repo.stage",_)) -> Ok ()
          | Error other  -> Error other
          | Ok other -> Error (Other("relpathTest", sprintf "Should have failed: %A" other))
      }
      |> noError

  //    ____          _                  _             _
  //   / ___|   _ ___| |_ ___  _ __ ___ (_)_______  __| |
  //  | |  | | | / __| __/ _ \| '_ ` _ \| |_  / _ \/ _` |
  //  | |__| |_| \__ \ || (_) | | | | | | |/ /  __/ (_| |
  //   \____\__,_|___/\__\___/|_| |_| |_|_/___\___|\__,_| load/saved
  //
  let testCustomizedCfg =
    testCase "Save/Load of Project with customized configs" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = tmpPath()
        let fn = Path.getFileName path

        let engineCfg = RaftConfig.Default

        let clientCfg =
          ClientConfig.ofList
            [{ Id         = DiscoId.Create()
               Executable = filepath "/pth/to/nowhere"
               Version    = version "0.0.0.0.0.0.1"
               Required   = true };
             { Id         = DiscoId.Create()
               Executable = filepath "/antoher/path"
               Version    = version "1.2.34.4"
               Required   = false }]

        let raftMemA =
          { Member.create (DiscoId.Create()) with
              IpAddress = IpAddress.Parse "182.123.18.2"
              Status    = Running
              RaftPort  = port 1234us }

        let raftMemB =
          { Member.create (DiscoId.Create()) with
              IpAddress = IpAddress.Parse "118.223.8.12"
              Status    = Joining
              RaftPort  = port 1234us }

        let clusterMemA =
          { Machine.toClusterMember machine with
              Id = raftMemA.Id }

        let clusterMemB =
          { Machine.toClusterMember machine with
              Id = raftMemB.Id }

        let groupA: HostGroup =
          { Name    = name "Group A"
            Members = [| DiscoId.Create() |] }

        let groupB: HostGroup =
          { Name    = name "Group B"
            Members = [| DiscoId.Create() |] }

        let cluster =
          { Id = DiscoId.Create()
            Name   = name "A mighty cool cluster"
            Members =
              Map.ofArray [|
                (raftMemA.Id, clusterMemA)
                (raftMemB.Id, clusterMemB)
              |]
            Groups = [| groupA; groupB |] }

        let! project = Project.create path (unwrap fn) machine

        let updated =
          Project.setConfig
            { project.Config with
                Raft       = engineCfg
                Clients    = clientCfg
                ActiveSite = Some cluster.Id
                Sites      = Map [ cluster.Id,cluster ] }
            project

        let! commit = DiscoData.saveWithCommit path User.Admin.Signature updated
        let! loaded = Asset.loadWithMachine path machine

        // the only difference will be the automatically assigned timestamp
        expect "CreatedOn should be structurally equal" true
          ((=) loaded.CreatedOn)
          updated.CreatedOn

        expect "ClientConfig should be structurally equal" true
          ((=) loaded.Config.Clients)
          updated.Config.Clients

        expect "RaftConfig should be structurally equal" true
          ((=) loaded.Config.Raft)
          updated.Config.Raft

        expect "Timing should be structurally equal" true
          ((=) loaded.Config.Timing)
          updated.Config.Timing

        expect "Sites should be structurally equal" true
          ((=) loaded.Config.Sites)
          updated.Config.Sites
      }
      |> noError

  // Adapted from http://stackoverflow.com/a/648055
  let rec deleteFileSystemInfo (fileSystemInfo: FileSystemInfo) =
    try
        match fileSystemInfo with
        | :? DirectoryInfo as dirInfo ->
            for childInfo in dirInfo.GetFileSystemInfos("*", SearchOption.AllDirectories) do
                deleteFileSystemInfo childInfo
        | _ -> ()
        fileSystemInfo.Attributes <- FileAttributes.Normal
        fileSystemInfo.Delete()
    with _ -> ()


  //    ____ _ _
  //   / ___(_) |_
  //  | |  _| | __|
  //  | |_| | | |_
  //   \____|_|\__| initialzation
  //
  let saveInitsGit =
    testCase "Saved Project should be a git repository with yaml file." <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None
        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let! _ = Project.create path name machine

        let loaded = Asset.loadWithMachine path machine

        expect "Projects should be a folder"   true  Directory.exists path
        expect "Projects should be a git repo" true  Directory.exists (path </> filepath ".git")

        let projectFile = path </> filepath (PROJECT_FILENAME + ASSET_EXTENSION)

        expect "Projects should have project yml" true  File.exists projectFile

        let getRepo =
          Project.repository
          >> Result.isSuccess

        do! expectE "Projects should have repo" true getRepo loaded

        let checkDirty (project: DiscoProject) =
          project
          |> Project.repository
          |> Result.bind Git.Repo.isDirty
          |> Result.get

        do! expectE "Projects should not be dirty" false checkDirty loaded

        let commitCount (project: DiscoProject) =
          project
          |> Project.repository
          |> Result.map Git.Repo.commitCount
          |> Result.get

        do! expectE "Projects should have initial commit" 1  commitCount loaded
      }
      |> noError

  //    ____                          _ _
  //   / ___|___  _ __ ___  _ __ ___ (_) |_ ___
  //  | |   / _ \| '_ ` _ \| '_ ` _ \| | __/ __|
  //  | |__| (_) | | | | | | | | | | | | |_\__ \
  //   \____\___/|_| |_| |_|_| |_| |_|_|\__|___/ per save
  //
  let savesMultipleCommits =
    testCase "Saving project should contain multiple commits" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let author1 = "karsten"

        let! project = Project.create path name machine

        let updated = { project with Author = Some author1 }
        let! commit = DiscoData.saveWithCommit path User.Admin.Signature updated

        let! loaded = Asset.loadWithMachine path machine
        let! repo = Project.repository loaded

        let checkAuthor = (Option.get >> (=)) loaded.Author
        let checkCount = (=) (Git.Repo.commitCount repo)

        expect "Authors should be equal"                true checkAuthor author1
        expect "Project should have one initial commit" true checkCount 2

        let author2 = "ingolf"

        let updated = { updated with Author = Some author2 }
        let! commit2 = DiscoData.saveWithCommit path User.Admin.Signature updated

        let! loaded = Asset.loadWithMachine path machine

        expect "Authors should be equal"     true ((=) (Option.get loaded.Author)) author2
        expect "Projects should two commits" true ((=) (Git.Repo.commitCount repo)) 3

        let author3 = "eno"

        let updated = { updated with Author = Some author3 }
        let! commit3 = DiscoData.saveWithCommit path User.Admin.Signature updated

        let! loaded = Asset.loadWithMachine path machine

        expect "Authors should be equal"           true ((=) (Option.get loaded.Author)) author3
        expect "Projects should have four commits" true ((=) (Git.Repo.commitCount repo)) 4
      }
      |> noError

  let upToDatePath =
    testCase "Saving project should always contain an up-to-date path" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None
        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let! project = Project.create path name machine
        let! (loaded: DiscoProject) = Asset.loadWithMachine path machine

        expect "Project should have correct path" path id loaded.Path

        let newpath = tmpPath()

        FileSystem.moveFile path newpath

        let! (loaded: DiscoProject) = Asset.loadWithMachine newpath machine

        expect "Project should have correct path" newpath id loaded.Path
      }
      |> noError

  let saveAsset =
    testCase "Should save an asset in new commit" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None

        let path = tmpPath()
        let fn = Path.getFileName path |> unwrap

        let! project = Project.create path fn machine

        let user =
          { Id = DiscoId.Create()
            UserName = name "krgn"
            FirstName = name "karsten"
            LastName = name "gebbert"
            Email = email "k@lazy.af"
            Password = checksum "1234"
            Salt = checksum "56789"
            Joined = DateTime.Now
            Created = DateTime.Now }

        let! (commit, project) = Project.saveAsset user User.Admin project

        let! (loaded: User) =
          let userpath = project.Path </> Asset.path user
          File.readText(userpath)
          |> Yaml.decode

        expect "Should be the same" true ((=) user) loaded
      }
      |> noError

  let createDefaultUser =
    testCase "Should create a default admin user" <| fun _ ->
      result {
        let machine = MachineConfig.create "127.0.0.1" None
        let path = tmpPath()
        let name = Path.getFileName path |> unwrap

        let! project = Project.create path name machine

        let! (admin: User) =
          project.Path </> Asset.path User.Admin
          |> File.readText
          |> Yaml.decode

        // Don't compare Joined and Created as they may differ a bit
        let isUserAdmin (admin: User) =
          User.Admin.Id               = admin.Id              &&
          User.Admin.UserName         = admin.UserName        &&
          User.Admin.FirstName        = admin.FirstName       &&
          User.Admin.LastName         = admin.LastName        &&
          User.Admin.Email            = admin.Email

        expect "Should have create the admin user" true isUserAdmin admin
      }
      |> noError

  // For tests async stuff:
  //
  // let testTests =
  //   testCase "making a case" <| (timeout 1000
  //     (fun _ ->
  //       Thread.Sleep(900)
  //       failtest "nop"))

  [<Tests>]
  let projectTests =
    testList "Load/Save tests" [
        dirtyTest
        relpathTest
        loadSaveTest
        testCustomizedCfg
        saveInitsGit
        savesMultipleCommits
        upToDatePath
        saveAsset
        createDefaultUser
      ] |> testSequenced
