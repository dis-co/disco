namespace Iris.Tests

open System
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

[<AutoOpen>]
module AssetServiceTests =

  let rnd = Random()

  let rndString() =
    [| for _ in 0 .. rnd.Next(255,500) do
        let num = rnd.Next(0,255)
        yield Convert.ToChar num |]
    |> String

  let createAssetDirectory() =
    let basePath = Path.getTempPath()
    for d in 0 .. rnd.Next(1,1) do
      let dirPath = basePath </> Path.getRandomFileName()
      Directory.createDirectory dirPath |> ignore
      for f in 0 .. rnd.Next(1,1) do
        let filePath = dirPath </> Path.getRandomFileName()
        let contents = rndString()
        File.writeText contents None filePath
    let machine = { IrisMachine.Default with AssetDirectory = basePath }
    let filters = FsTree.parseFilters machine.AssetFilter
    let fsTree = FsTree.read machine.MachineId basePath filters |> Either.get
    machine, fsTree

  let testInitialCrawl =
    testCase "should crawl asset directory correctly" <| fun _ ->
      either {
        let machine, tree = createAssetDirectory()
        use crawlDone = new WaitEvent()
        let! service = AssetService.create machine

        let handler = function
          | IrisEvent.Append (_,AddFsTree _) -> crawlDone.Set()
          | _ -> ()

        use subscription = service.Subscribe handler

        do! service.Start()
        do! waitFor "crawl to be done" crawlDone

        Expect.equal service.State (Some tree) "Trees should be equal"
      }
      |> noError

  let assetServiceTests =
    ftestList "AssetService Tests" [
      testInitialCrawl
    ]
