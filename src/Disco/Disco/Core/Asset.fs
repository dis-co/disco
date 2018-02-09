(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

// * Imports

open System
open System.IO
open System.Text
open Path

// * Asset

/// ## Contracts for manipulating on-disk data

[<RequireQualifiedAccess>]
module Asset =

  // ** tag

  let private tag (site: string) = String.format "Asset.{0}" site

  // ** path

  let inline path< ^t when ^t : (member AssetPath : FilePath)> (thing: ^t) =
    (^t : (member AssetPath: FilePath) thing)


  // ** save

  #if !FABLE_COMPILER

  let inline save< ^t when ^t : (member Save: FilePath -> DiscoResult<unit>)>
                 (path: FilePath)
                 (t: ^t) =
    (^t : (member Save: FilePath -> DiscoResult<unit>) (t, path))


  // ** delete

  let inline delete< ^t when ^t : (member Delete: FilePath -> DiscoResult<unit>)>
                   (path: FilePath)
                   (t: ^t) =
    (^t : (member Delete: FilePath -> DiscoResult<unit>) (t, path))

  // ** saveMap


  let inline saveMap (basepath: FilePath) (guard: DiscoResult<unit>) _ (t: ^t) =
    result {
      do! guard
      do! save basepath t
    }

  // ** load

  let inline load< ^t when ^t : (static member Load: FilePath -> DiscoResult< ^t>)>
                 (path: FilePath) =
    (^t : (static member Load: FilePath -> DiscoResult< ^t>) path)

  // ** loadWithMachine

  let inline loadWithMachine< ^t when ^t : (static member Load: FilePath * DiscoMachine -> DiscoResult< ^t>)>
                 (path: FilePath)
                 (machine: DiscoMachine) =
    (^t : (static member Load: FilePath * DiscoMachine -> DiscoResult< ^t>) (path,machine))

  // ** loadAll

  let inline loadAll< ^t when ^t : (static member LoadAll: FilePath -> DiscoResult< ^t array>)>
                    (basePath: FilePath) =
    (^t : (static member LoadAll: FilePath -> DiscoResult< ^t array>) basePath)

  // ** hasParent

  let inline hasParent< ^t when ^t : (member HasParent: bool)> asset =
    (^t : (member HasParent: bool) asset)

  #endif


// * DiscoData

module DiscoData =

  // ** tag

  let private tag (str: string) = String.format "DiscoData.{0}" str

  // ** write

  #if !FABLE_COMPILER

  /// ## write
  ///
  /// Description
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  /// - payload: string payload to save
  ///
  /// Returns: DiscoResult<FileInfo>
  let write (location: FilePath) (payload: StringPayload) =
    result {
      try
        let data = match payload with | Payload data -> data
        let info = File.info location
        do! info.Directory.FullName |> filepath |> mkDir
        File.writeText data (Some Encoding.UTF8) location
        info.Refresh()
        return info
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError (tag "write")
            |> Result.fail
    }

  #endif

  // ** remove

  #if !FABLE_COMPILER

  /// ## delete
  ///
  /// Delete an asset from disk.
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  ///
  /// Returns: DiscoResult<bool>
  let remove (location: FilePath) =
    result {
      try
        if File.exists location then
          Path.map File.Delete location
          return ()
        else
          return ()
      with | exn ->
        return!
          exn.Message
          |> Error.asAssetError (tag "remove")
          |> Result.fail
    }

  #endif

  // ** read

  #if !FABLE_COMPILER

  /// ## read
  ///
  /// Load a text file from disk. If the file could not be loaded,
  /// return IOError.
  ///
  /// ### Signature:
  /// - locationg: FilePath to asset
  ///
  /// Returns: DiscoResult<string>
  let read (location: FilePath) : DiscoResult< string> =
    result {
      if File.exists location then
        try
          return File.readText location
        with
          | exn ->
            return!
              exn.Message
              |> Error.asAssetError (tag "read")
              |> Result.fail
      else
        return!
          sprintf "File not found: %O" location
          |> Error.asAssetError (tag "read")
          |> Result.fail
    }
  #endif

  // ** load

  let inline load (path: FilePath) =
    result {
      let! data = read path
      let! group = Yaml.decode data
      return group
    }

  // ** loadAll

  let inline loadAll (basePath: FilePath) =
    result {
      try
        let files = Directory.getFiles true ("*" + Constants.ASSET_EXTENSION) basePath
        let! (_,groups) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate
          Array.fold
            (fun (m: DiscoResult<int * ^t array>) path ->
              result {
                let! (idx,groups) = m
                let! group = load path
                groups.[idx] <- group
                return (idx + 1, groups)
              })
            (Ok(0, arr))
            files
        return groups
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "PinGroup.LoadAll"
            |> Result.fail
    }

  // ** ensureDirectoryExists

  let inline ensureDirectoryExists (path: FilePath) asset =
    if Asset.hasParent asset then
      path
      |> Path.getDirectoryName
      |> Directory.createDirectory
      |> Result.ignore
    else
      Result.nothing

  // ** ensureDirectoryGone

  let inline ensureDirectoryGone (dir: FilePath) asset =
    if Asset.hasParent asset then
      if Directory.isEmpty dir then
        Directory.removeDirectory dir
        |> Result.ignore
      else Result.nothing
    else Result.nothing

  // ** save

  let inline save (basePath: FilePath) asset =
    result {
      let path = basePath </> Asset.path asset
      do! ensureDirectoryExists path asset
      let data = Yaml.encode asset
      let! _ = write path (Payload data)
      return ()
    }

  // ** delete

  let inline delete (basePath: FilePath) asset =
    result {
      let path = basePath </> Asset.path asset
      do! path |> Path.concat basePath |> remove
      do! ensureDirectoryGone (Path.directoryName path) asset
    }

  // ** commit

  #if !FABLE_COMPILER

  let inline commit (basepath: FilePath) (msg: string) (signature: LibGit2Sharp.Signature) (t: ^t) =
    result {
      use! repo = Git.Repo.repository basepath

      let target =
        if Path.isPathRooted basepath then
          basepath </> Asset.path t
        else
          Path.getFullPath basepath </> Asset.path t

      do! Git.Repo.stage repo target
      let! commit = Git.Repo.commit repo msg signature
      return commit
    }

  #endif

  // ** saveWithCommit

  #if !FABLE_COMPILER

  let inline saveWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    result {
      do! save basepath t
      let filename = t |> Asset.path |> Path.getFileName
      let msg = sprintf "%s saved %A" signature.Name filename
      return! commit basepath msg signature t
    }

  #endif

  // ** deleteWithCommit

  #if !FABLE_COMPILER

  let inline deleteWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    result {
      let filepath = basepath </> Asset.path t
      let! _ = remove filepath
      let msg = sprintf "%s deleted %A" signature.Name (Path.getFileName filepath)
      return! commit basepath msg signature t
    }

  #endif
