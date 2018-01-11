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
open Disco.Raft
open Disco.Core
open Disco.Service

// * FsWatcher

module FsWatcher =

  // ** tag

  let private tag (str: string) = String.format "FsWatcher.{0}" str

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoEvent>

  // ** onCreate

  let private onCreate subscriptions (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Created
    |> DiscoEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onChange

  let private onChange subscriptions (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Changed
    |> DiscoEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onRename

  let private onRename subscriptions (args: RenamedEventArgs) =
    ( name (Path.GetFileName args.OldName)
    , filepath args.OldFullPath
    , name (Path.GetFileName args.Name)
    , filepath args.FullPath)
    |> FileSystemEvent.Renamed
    |> DiscoEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** onDelete

  let private onDelete subscriptions (args: FileSystemEventArgs) =
    (name (Path.GetFileName args.Name), filepath args.FullPath)
    |> FileSystemEvent.Deleted
    |> DiscoEvent.FileSystem
    |> Observable.onNext subscriptions

  // ** create

  let create (project: DiscoProject) =
    let subscriptions = Subscriptions()
    let watcher = new FileSystemWatcher()

    let filter =
      NotifyFilters.LastWrite  |||
      NotifyFilters.FileName   |||
      NotifyFilters.DirectoryName

    project.Path
    |> String.format "Starting new FileSystem watcher in: {0}"
    |> Logger.info (tag "create")

    watcher.Path                  <- string project.Path
    watcher.NotifyFilter          <- filter
    watcher.Filter                <- "*" + Constants.ASSET_EXTENSION
    watcher.IncludeSubdirectories <- true

    watcher.Created.Add(onCreate subscriptions)
    watcher.Changed.Add(onChange subscriptions)
    watcher.Renamed.Add(onRename subscriptions)
    watcher.Deleted.Add(onDelete subscriptions)

    watcher.EnableRaisingEvents   <- true

    { new IFsWatcher with
        member watcher.Subscribe(callback) =
          Observable.subscribe callback subscriptions

        member self.Dispose () =
          subscriptions.Clear()
          watcher.Dispose() }
