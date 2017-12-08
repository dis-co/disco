namespace Disco.Tests

open System
open System.IO
open System.Threading
open Expecto

open Disco.Core
open Disco.Service
open Disco.Client
open Disco.Client.Interfaces
open Disco.Service.Interfaces
open Disco.Raft
open Disco.Net

[<AutoOpen>]
module AssetServiceTests =

  let rnd = Random()

  let rndString() =
    [| for _ in 0 .. rnd.Next(255,500) do
        let num = rnd.Next(0,255)
        yield Convert.ToChar num |]
    |> String

  let createAssetDirectory() =
    let basePath = Path.getFullPath(Path.getRandomFileName())
    do Directory.createDirectory basePath |> ignore
    for d in 0 .. rnd.Next(1,4) do
      let dirPath = basePath </> Path.getRandomFileName()
      do Directory.createDirectory dirPath |> ignore
      for f in 0 .. rnd.Next(1,4) do
        let filePath = dirPath </> Path.getRandomFileName()
        let contents = rndString()
        do File.writeText contents None filePath
    let machine = { DiscoMachine.Default with AssetDirectory = basePath }
    let filters = FsTree.parseFilters machine.AssetFilter
    let fsTree = FsTree.read machine.MachineId basePath filters |> Either.get
    machine, fsTree

  let testInitialCrawl =
    testCase "should crawl asset directory correctly" <| fun _ ->
      either {
        let machine, tree = createAssetDirectory()
        use crawlDone = new WaitEvent()
        use! service = AssetService.create machine

        let handler = function
          | DiscoEvent.Append (_,AddFsTree _) -> crawlDone.Set()
          | _ -> ()

        use subscription = service.Subscribe handler

        do! service.Start()
        do! waitFor "crawl to be done" crawlDone

        Expect.equal service.State (Some tree) "Trees should be equal"
      }
      |> noError

  let testAddEntry =
    testCase "add entry should work" <| fun _ ->
      either {
        let machine, tree = createAssetDirectory()
        use crawlDone = new WaitEvent()
        use addDone = new WaitEvent()

        use! service = AssetService.create machine

        let handler = function
          | DiscoEvent.Append (_,AddFsTree _)    -> crawlDone.Set()
          | DiscoEvent.Append (_,CommandBatch _) -> addDone.Set()
          | _ -> ()

        use subscription = service.Subscribe handler

        do! service.Start()
        do! waitFor "crawl to be done" crawlDone

        Expect.equal service.State (Some tree) "Trees should be equal"

        let filePath = machine.AssetDirectory </> Path.getRandomFileName()
        let payload = rndString()
        do File.writeText payload None filePath

        do! waitFor "add be done" addDone

        let! tree =
          machine.AssetFilter
          |> FsTree.parseFilters
          |> FsTree.read machine.MachineId machine.AssetDirectory

        Expect.equal service.State (Some tree) "Trees should be equal"
      }
      |> noError

  let testChangeEntry =
    testCase "change entry should work" <| fun _ ->
      either {
        let machine, tree = createAssetDirectory()
        use crawlDone = new WaitEvent()
        use changeDone = new WaitEvent()

        use! service = AssetService.create machine

        let handler = function
          | DiscoEvent.Append (_,AddFsTree _)    -> crawlDone.Set()
          | DiscoEvent.Append (_,CommandBatch _) -> changeDone.Set()
          | _ -> ()

        use subscription = service.Subscribe handler

        do! service.Start()
        do! waitFor "crawl to be done" crawlDone

        Expect.equal service.State (Some tree) "Trees should be equal"

        let filePath =
          tree
          |> FsTree.files
          |> List.head
          |> FsEntry.path
          |> string
          |> filepath

        let payload = rndString()
        do File.writeText payload None filePath

        do! waitFor "change be done" changeDone

        let! tree =
          machine.AssetFilter
          |> FsTree.parseFilters
          |> FsTree.read machine.MachineId machine.AssetDirectory

        Expect.equal service.State (Some tree) "Trees should be equal"
      }
      |> noError

  let testRemoveEntres =
    testCase "remove entries should work" <| fun _ ->
      either {
        let machine, tree = createAssetDirectory()
        use crawlDone = new WaitEvent()
        use removeDone = new WaitEvent()

        use! service = AssetService.create machine

        let handler = function
          | DiscoEvent.Append (_,AddFsTree _)    -> crawlDone.Set()
          | DiscoEvent.Append (_,CommandBatch _) -> removeDone.Set()
          | _ -> ()

        use subscription = service.Subscribe handler

        do! service.Start()
        do! waitFor "crawl to be done" crawlDone

        Expect.equal service.State (Some tree) "Trees should be equal"

        let dirPath =
          tree
          |> FsTree.directories
          |> FsEntry.flatten
          |> List.map (FsEntry.path >> string)
          |> List.sortBy String.length
          |> List.last
          |> filepath

        do! FileSystem.rmDir dirPath

        do! waitFor "remove be done" removeDone

        let! tree =
          machine.AssetFilter
          |> FsTree.parseFilters
          |> FsTree.read machine.MachineId machine.AssetDirectory

        Expect.equal service.State (Some tree) "Trees should be equal"
      }
      |> noError

  let assetServiceTests =
    /// apparently, these tests only work on "real" file systems and always fail on networked ones,
    /// hence we disable them there for CI builds
    let tests =
      if isNull (Environment.GetEnvironmentVariable "APPVEYOR_CI_BUILD") &&
         isNull (Environment.GetEnvironmentVariable "IN_VBOX")
      then
        [ testInitialCrawl
          testAddEntry
          testChangeEntry
          testRemoveEntres ]
      else List.empty
    tests
    |> testList "AssetService Tests"
    |> testSequenced
