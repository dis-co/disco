namespace Iris.Service

// * Imports

open System
open System.IO
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Service
open Iris.Service.Interfaces

// * AssetService

module AssetService =

  // ** tag

  let private tag (str: string) = sprintf "AssetService.%s" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** AssetEventProcessor

  type private AssetEventProcessor = MailboxProcessor<IrisEvent>

  // ** AssetServiceState

  [<NoComparison;NoEquality>]
  type private AssetServiceState =
    { Files: FsTree option
      Subscriptions: Subscriptions }

  // ** updateFiles

  let private updateFiles state files =
    { state with Files = files }

  // ** loop

  let private loop (store: IAgentStore<AssetServiceState>) (inbox: AssetEventProcessor) =
    let rec impl () =
      async {
        let! msg = inbox.Receive()
        let state = store.State
        match msg with
        | IrisEvent.Append (_, AddFsTree tree) as ev ->
          Observable.onNext state.Subscriptions ev
          updateFiles state (Some tree)
        | IrisEvent.FileSystem (FileSystemEvent.Created(_,path)) ->
          state.Files
          |> Option.map (FsTree.add path)
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Changed(_,path)) ->
          state.Files
          |> Option.map (FsTree.update path)
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Deleted(_,path)) ->
          state.Files
          |> Option.map (FsTree.remove path)
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Renamed(_,oldpath,_,path)) ->
          state.Files
          |> Option.map (FsTree.remove oldpath >> FsTree.add path)
          |> updateFiles state
        | _ -> state
        |> store.Update
        return! impl()
      }
    impl()

  // ** post

  let private post (agent: AssetEventProcessor) ev =
    agent.Post ev

  // ** delayed

  let private delayed agent ev =
    async {
      do! Async.Sleep(20)
      do post agent ev
    }
    |> Async.Start

  // ** onCreate

  let private onCreate agent (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Created
    |> IrisEvent.FileSystem
    |> delayed agent

  // ** onChange

  let private onChange agent (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Changed
    |> IrisEvent.FileSystem
    |> delayed agent

  // ** onRename

  let private onRename agent (args: RenamedEventArgs) =
    ( name (Path.GetFileName args.OldName)
    , filepath args.OldFullPath
    , name (Path.GetFileName args.Name)
    , filepath args.FullPath)
    |> FileSystemEvent.Renamed
    |> IrisEvent.FileSystem
    |> delayed agent

  // ** onDelete

  let private onDelete agent (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Deleted
    |> IrisEvent.FileSystem
    |> delayed agent

  // ** crawlDirectory

  let private crawlDirectory (machine:IrisMachine) =
    async {
      do machine.AssetDirectory
         |> String.format "Crawling asset directory {0}"
         |> Logger.info (tag "crawlDirectory")
      let filters = FsTree.parseFilters machine.AssetFilter
      let path = FsPath.parse machine.AssetDirectory
      return
        match FsEntry.create path with
        | Some root ->
          machine.AssetDirectory
          |> FileSystem.lsDir
          |> List.sort
          |> List.choose (FsPath.parse >> FsEntry.create)
          |> FsTree.inflate machine.MachineId root
          |> FsTree.setFilters filters
          |> FsTree.applyFilters
          |> Some
        | None ->
          machine.AssetDirectory
          |> String.format "Could not crawl {0}"
          |> Logger.err (tag "crawlDirectory")
          None
    }

  // ** startCrawler

  let private startCrawler machine agent =
    async {
      let! tree = crawlDirectory machine
      do Logger.info (tag "crawlDirectory") "Done."
      do Option.iter (AddFsTree >> IrisEvent.appendService >> post agent) tree
    }
    |> Async.Start

  // ** createWatcher

  let createWatcher (baseDir:FilePath) agent =
    let watcher = new FileSystemWatcher()
    let filter =
      NotifyFilters.LastWrite  |||
      NotifyFilters.FileName   |||
      NotifyFilters.DirectoryName
    watcher.Path <- unwrap baseDir
    watcher.NotifyFilter <- filter
    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents   <- true
    watcher.Created.Add(onCreate agent)
    watcher.Changed.Add(onChange agent)
    watcher.Renamed.Add(onRename agent)
    watcher.Deleted.Add(onDelete agent)
    watcher

  // ** create

  let create (machine: IrisMachine) =
    either {
      let cts = new CancellationTokenSource()
      let status = ref ServiceStatus.Stopped
      let subscriptions = Subscriptions()

      let store = AgentStore.create()
      store.Update {
        Files = None
        Subscriptions = subscriptions
      }

      let agent = new AssetEventProcessor(loop store, cts.Token)
      let watcher = createWatcher machine.AssetDirectory agent

      return {
        new IAssetService with
          member self.Start() =
            agent.Start()
            agent
            |> startCrawler machine
            |> Either.succeed

          member self.Stop() = Either.nothing

          member self.State = store.State.Files

          member self.Subscribe(cb) =
            Observable.subscribe cb subscriptions

          member self.Dispose() =
            try
              dispose watcher
              cts.Cancel()
              dispose agent
            with exn -> printfn "exn: %A" exn
      }
    }

#if INTERACTIVE

Logger.subscribe Logger.stdout

let machine =
  { IrisMachine.Default with
      AssetDirectory = filepath "/home/k/iris/assets"
      AssetFilter = ".file"
    }

let service = AssetService.create machine |> Either.get
service.Subscribe (printfn "%O")

service.Start()

service.State |> Option.map FsTree.fileCount

dispose service

#endif
