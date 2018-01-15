(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open System
open System.IO
open System.Threading
open System.Collections.Concurrent
open Disco.Core
open Disco.Service
open Disco.Service.Interfaces

// * AssetService

module AssetService =

  // ** tag

  let private tag (str: string) = sprintf "AssetService.%s" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoEvent>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Fs of FileSystemEvent
    | Command of StateMachine
    | Flush

  // ** AssetEventProcessor

  type private AssetEventProcessor = IActor<Msg>

  // ** AssetServiceState

  [<NoComparison;NoEquality>]
  type private AssetServiceState =
    { Machine: DiscoMachine
      Files: FsTree option
      Updates: FileSystemEvent list
      Subscriptions: Subscriptions }

  // ** updateFiles

  let private updateFiles state files =
    { state with Files = files }

  // ** addUpdate

  let private addUpdate update state =
    { state with Updates = update :: state.Updates }

  // ** flushUpdates

  let private flushUpdates (state:AssetServiceState) =
    match state.Files with
    | None      -> { state with Updates = List.empty }
    | Some tree ->
      let commands =
        List.fold
          (fun map -> function
            | FileSystemEvent.Created(_,path) ->
              let path = FsPath.parse path
              if FsEntry.matches tree.Filters (FsPath.fileName path) then
                /// this entry was filtered, so we just update the parent to get the right counts
                let parent = FsPath.parent path
                match FsTree.tryFind parent tree with
                | Some entry ->
                  let cmd = UpdateFsEntry (state.Machine.MachineId, entry)
                  Map.add parent cmd map
                | None -> map
              else
                match FsTree.tryFind path tree with
                | Some entry ->
                  let cmd = AddFsEntry (state.Machine.MachineId, entry)
                  Map.add path cmd map
                | None -> map
            | FileSystemEvent.Changed(_,path) ->
              let path = FsPath.parse path
              match Map.tryFind path map with
              | Some (AddFsEntry _) ->
                /// there is already an AddFsEntry for this path in the batch
                /// so we don't want to overwrite it with an UpdateFsEntry, which
                /// will get ignored
                match FsTree.tryFind path tree with
                | Some entry ->
                  let cmd = AddFsEntry (state.Machine.MachineId, entry)
                  Map.add path cmd map
                | None -> map
              | _ ->
                /// no entry there for the path yet, so we just update
                match FsTree.tryFind path tree with
                | Some entry ->
                  let cmd = UpdateFsEntry (state.Machine.MachineId, entry)
                  Map.add path cmd map
                | None -> map
            | FileSystemEvent.Deleted(_,path) ->
              let path = FsPath.parse path
              let cmd = RemoveFsEntry (state.Machine.MachineId, path)
              Map.add path cmd map
            | FileSystemEvent.Renamed(_,oldpath,_,path) ->
              let oldpath = FsPath.parse oldpath
              let path = FsPath.parse path
              let remove = RemoveFsEntry (state.Machine.MachineId, oldpath)
              if FsEntry.matches tree.Filters (FsPath.fileName path) then
                let parent = FsPath.parent path
                match FsTree.tryFind parent tree with
                | None -> Map.add oldpath remove map
                | Some entry ->
                  let add = UpdateFsEntry (state.Machine.MachineId, entry)
                  map
                  |> Map.add oldpath remove
                  |> Map.add parent add
              else
                match FsTree.tryFind path tree with
                | None -> Map.add oldpath remove map
                | Some entry ->
                  let add = AddFsEntry (state.Machine.MachineId, entry)
                  map
                  |> Map.add oldpath remove
                  |> Map.add path add)
          Map.empty
          (List.rev state.Updates)      /// oldes updates first
      if not (Map.isEmpty commands) then
        Map.count commands
        |> String.format "Processed {0} updates in asset directory"
        |> Logger.info (tag "flushUpdates")
        commands
        |> Map.toList
        |> List.map snd
        |> CommandBatch.ofList
        |> DiscoEvent.appendService
        |> Observable.onNext state.Subscriptions
      { state with Updates = List.empty }

  // ** loop

  let private loop (store: IAgentStore<AssetServiceState>) inbox msg =
    let state = store.State
    match msg with
    | Msg.Command(AddFsTree tree as ev) ->
      ev
      |> DiscoEvent.appendService
      |> Observable.onNext state.Subscriptions
      updateFiles state (Some tree)
    | Msg.Fs(FileSystemEvent.Created(_,path) as update) ->
      state.Files
      |> Option.map (FsTree.add path)
      |> updateFiles state
      |> addUpdate update
    | Msg.Fs(FileSystemEvent.Changed(_,path) as update) ->
      state.Files
      |> Option.map (FsTree.update path)
      |> updateFiles state
      |> addUpdate update
    | Msg.Fs(FileSystemEvent.Deleted(_,path) as update) ->
      state.Files
      |> Option.map (FsTree.remove path)
      |> updateFiles state
      |> addUpdate update
    | Msg.Fs(FileSystemEvent.Renamed(_,oldpath,_,path) as update) ->
      state.Files
      |> Option.map (FsTree.remove oldpath >> FsTree.add path)
      |> updateFiles state
      |> addUpdate update
    | Msg.Flush -> flushUpdates state
    | _ -> state
    |> store.Update

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
    |> Msg.Fs
    |> delayed agent

  // ** onChange

  let private onChange agent (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Changed
    |> Msg.Fs
    |> delayed agent

  // ** onRename

  let private onRename agent (args: RenamedEventArgs) =
    ( name (Path.GetFileName args.OldName)
    , filepath args.OldFullPath
    , name (Path.GetFileName args.Name)
    , filepath args.FullPath)
    |> FileSystemEvent.Renamed
    |> Msg.Fs
    |> delayed agent

  // ** onDelete

  let private onDelete agent (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Deleted
    |> Msg.Fs
    |> delayed agent

  // ** crawlDirectory

  let private crawlDirectory (machine:DiscoMachine) =
    async {
      do machine.AssetDirectory
         |> String.format "Crawling asset directory {0}"
         |> Logger.info (tag "crawlDirectory")
      let filters = FsTree.parseFilters machine.AssetFilter
      let path = FsPath.parse machine.AssetDirectory
      return
        match FsEntry.create path with
        | Some root ->
          let result =
            machine.AssetDirectory
            |> FileSystem.lsDir
            |> List.sort
            |> List.choose (FsPath.parse >> FsEntry.create)
            |> FsTree.inflate machine.MachineId root
            |> FsTree.setFilters filters
            |> FsTree.applyFilters
          let files = FsTree.fileCount result
          let filtered = FsTree.filteredCount result
          let directories = FsTree.directoryCount result
          filtered
          |> sprintf "Found %d directories, %d files (%d filtered)" files directories
          |> Logger.info (tag "crawlDirectory")
          Some result
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
      do Option.iter (AddFsTree >> Msg.Command >> post agent) tree
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

  // ** flushClock

  let private flushClock agent () =
    do post agent Msg.Flush

  // ** create

  let create (machine: DiscoMachine) =
    either {
      do! Directory.createDirectory machine.AssetDirectory |> Either.map ignore
      let subscriptions = Subscriptions()
      let store = AgentStore.create()

      store.Update {
        Machine = machine
        Files = None
        Updates = List.empty
        Subscriptions = subscriptions
      }

      let agent = ThreadActor.create "AssetService" (loop store)
      let watcher = createWatcher machine.AssetDirectory agent
      let mutable flusher = Unchecked.defaultof<IDisposable>
      let metrics = Periodically.run 1000 <| fun () ->
        Metrics.collect Constants.METRIC_ASSET_SERVICE_QUEUE agent.CurrentQueueLength

      return {
        new IAssetService with
          member self.Start() =
            agent.Start()
            flusher <- Periodically.run 2000 (flushClock agent)
            agent
            |> startCrawler machine
            |> Either.succeed

          member self.Stop() = Either.nothing

          member self.State = store.State.Files

          member self.Subscribe(cb) =
            Observable.subscribe cb subscriptions

          member self.Dispose() =
            try
              dispose flusher
              dispose watcher
              dispose metrics
              dispose agent
            with exn -> printfn "exn: %A" exn
      }
    }

// * Playground

#if INTERACTIVE

Logger.subscribe Logger.stdout

let machine =
  { DiscoMachine.Default with
      AssetDirectory = filepath "/home/k/disco/assets"
      AssetFilter = ".file"
    }

let service = AssetService.create machine |> Either.get
service.Subscribe (printfn "%O")

service.Start()
service.State |> Option.map FsTree.fileCount
dispose service

#endif
