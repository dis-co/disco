namespace Iris.Service

// * Imports

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Service

// * FsWatcher

module FsWatcher =

  // ** tag

  let private tag (str: string) = String.format "FsWatcher.{0}" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** onCreate

  let private onCreate subscriptions (args: FileSystemEventArgs) =
    (name args.Name, filepath args.FullPath)
    |> FileSystemEvent.Created
    |> IrisEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onChange

  let private onChange subscriptions (args: FileSystemEventArgs) =
    (name args.Name, filepath args.FullPath)
    |> FileSystemEvent.Changed
    |> IrisEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onRename

  let private onRename subscriptions (args: RenamedEventArgs) =
    ( name args.OldName
    , filepath args.OldFullPath
    , name args.Name
    , filepath args.FullPath)
    |> FileSystemEvent.Renamed
    |> IrisEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onDelete

  let private onDelete subscriptions (args: FileSystemEventArgs) =
    (name args.Name, filepath args.FullPath)
    |> FileSystemEvent.Deleted
    |> IrisEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** create

  let create (project: IrisProject) =
    let status = ref ServiceStatus.Stopped
    let subscriptions = Subscriptions()

    let watcher = new FileSystemWatcher()

    let filter =
      NotifyFilters.LastWrite  |||
      NotifyFilters.FileName   |||
      NotifyFilters.DirectoryName

    watcher.Path                  <- string project.Path
    watcher.NotifyFilter          <- filter
    watcher.Filter                <- "*" + Constants.ASSET_EXTENSION
    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents   <- true

    let creations = watcher.Created.Subscribe(onCreate subscriptions)
    let changes   = watcher.Changed.Subscribe(onChange subscriptions)
    let rename    = watcher.Renamed.Subscribe(onRename subscriptions)
    let deletions = watcher.Deleted.Subscribe(onDelete subscriptions)

    { new IFsWatcher with
        member watcher.Subscribe(callback) =
          Observable.subscribe callback subscriptions

        member self.Dispose () =
          dispose creations
          dispose changes
          dispose rename
          dispose deletions
          subscriptions.Clear() }
