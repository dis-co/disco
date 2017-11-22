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
    { Files: FsTree
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
        | IrisEvent.FileSystem (FileSystemEvent.Created(_,path)) ->
          state.Files
          |> FsTree.add path
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Changed(_,path)) ->
          state.Files
          |> FsTree.update path
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Deleted(_,path)) ->
          state.Files
          |> FsTree.remove path
          |> updateFiles state
        | IrisEvent.FileSystem (FileSystemEvent.Renamed(_,oldpath,_,path)) ->
          state.Files
          |> FsTree.remove oldpath
          |> FsTree.add path
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

  let rec private crawlDirectory dir agent =
    async {
      if Directory.exists dir then
        (dir |> Path.getFileName |> unwrap |> name, dir)
        |> FileSystemEvent.Created
        |> IrisEvent.FileSystem
        |> post agent
      for child in Directory.getFiles false "*" dir do
        (child |> Path.getFileName |> unwrap |> name, child)
        |> FileSystemEvent.Created
        |> IrisEvent.FileSystem
        |> post agent
      for child in Directory.getDirectories dir do
        do! crawlDirectory child agent
    }

  // ** startCrawler

  let private startCrawler baseDir agent =
    async {
      do! crawlDirectory baseDir agent
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

      let! tree =
        machine.AssetFilter.Split([| ' '; ';'; ',' |])
        |> Array.filter (String.IsNullOrEmpty >> not)
        |> FsTree.create machine.MachineId machine.AssetDirectory

      let store = AgentStore.create()
      store.Update {
        Files = tree
        Subscriptions = subscriptions
      }

      let agent = new AssetEventProcessor(loop store, cts.Token)
      let watcher = createWatcher machine.AssetDirectory agent

      return {
        new IAssetService with
          member self.Start() =
            agent.Start()
            agent
            |> startCrawler machine.AssetDirectory
            |> Either.succeed

          member self.Stop() = Either.nothing

          member self.State = store.State.Files

          member self.Dispose() =
            try
              dispose watcher
              cts.Cancel()
              dispose agent
            with exn -> printfn "exn: %A" exn
      }
    }

#if INTERACTIVE

let path = filepath "/home/k/iris/assets"
let service = AssetService.create path |> Either.get

service.Start()

FsTree.fileCount service.State
FsTree.directoryCount service.State

dispose service

#endif
