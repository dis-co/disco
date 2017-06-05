namespace Iris.Core

// * Imports

open System
open System.IO
open System.Text
open Path

// * Asset

[<RequireQualifiedAccess>]
module Asset =

  let private tag (site: string) = sprintf "Asset.%s" site

  // ** path

  /// ## path
  ///
  /// Return the realive path the given asset should be saved under.
  ///
  /// ### Signature:
  /// - thing: ^t
  ///
  /// Returns: FilePath
  let inline path< ^t when ^t : (member AssetPath : FilePath)> (thing: ^t) =
    (^t : (member AssetPath: FilePath) thing)


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
  /// Returns: Either<IrisError,FileInfo>
  let write (location: FilePath) (payload: StringPayload) =
    either {
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
            |> Either.fail
    }

  #endif

  // ** delete

  #if !FABLE_COMPILER

  /// ## delete
  ///
  /// Delete an asset from disk.
  ///
  /// ### Signature:
  /// - location: FilePath to asset
  ///
  /// Returns: Either<IrisError,bool>
  let delete (location: FilePath) =
    either {
      try
        if File.exists location then
          Path.map File.Delete location
          return ()
        else
          return ()
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError (tag "delete")
            |> Either.fail
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
  /// Returns: Either<IrisError,string>
  let read (location: FilePath) : Either<IrisError, string> =
    either {
      if File.exists location then
        try
          return File.readText location
        with
          | exn ->
            return!
              exn.Message
              |> Error.asAssetError (tag "read")
              |> Either.fail
      else
        return!
          sprintf "File not found: %O" location
          |> Error.asAssetError (tag "read")
          |> Either.fail
    }
  #endif

  // ** save

  #if !FABLE_COMPILER

  let inline save< ^t when ^t : (member Save: FilePath -> Either<IrisError, unit>)>
                 (path: FilePath)
                 (t: ^t) =
    (^t : (member Save: FilePath -> Either<IrisError, unit>) (t, path))

  #endif

  // ** saveMap

  #if !FABLE_COMPILER

  let inline saveMap (basepath: FilePath) (guard: Either<IrisError,unit>) _ (t: ^t) =
    either {
      do! guard
      do! save basepath t
    }

  #endif

  // ** load

  #if !FABLE_COMPILER

  let inline load< ^t when ^t : (static member Load: FilePath -> Either<IrisError, ^t>)>
                 (path: FilePath) =
    (^t : (static member Load: FilePath -> Either<IrisError, ^t>) path)

  #endif

  // ** loadWithMachine

  #if !FABLE_COMPILER

  let inline loadWithMachine< ^t when ^t : (static member Load: FilePath * IrisMachine -> Either<IrisError, ^t>)>
                 (path: FilePath)
                 (machine: IrisMachine) =
    (^t : (static member Load: FilePath * IrisMachine -> Either<IrisError, ^t>) (path,machine))

  #endif

  // ** loadAll

  #if !FABLE_COMPILER && !IRIS_NODES

  let inline loadAll< ^t when ^t : (static member LoadAll: FilePath -> Either<IrisError, ^t array>)>
                    (basePath: FilePath) =
    (^t : (static member LoadAll: FilePath -> Either<IrisError, ^t array>) basePath)

  #endif

  // ** commit

  #if !FABLE_COMPILER

  let inline commit (basepath: FilePath) (msg: string) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      use! repo = Git.Repo.repository basepath

      let target =
        if Path.isPathRooted basepath then
          basepath </> path t
        else
          Path.getFullPath basepath </> path t

      do! Git.Repo.stage repo target
      let! commit = Git.Repo.commit repo msg signature
      return commit
    }

  #endif

  // ** saveWithCommit

  #if !FABLE_COMPILER

  let inline saveWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      do! save basepath t
      let filename = t |> path |> Path.getFileName
      let msg = sprintf "%s saved %A" signature.Name filename
      return! commit basepath msg signature t
    }

  #endif

  // ** deleteWithCommit

  #if !FABLE_COMPILER

  let inline deleteWithCommit (basepath: FilePath) (signature: LibGit2Sharp.Signature) (t: ^t) =
    either {
      let filepath = basepath </> path t
      let! _ = delete filepath
      let msg = sprintf "%s deleted %A" signature.Name (Path.getFileName filepath)
      return! commit basepath msg signature t
    }

  #endif


// * IrisData

module IrisData =

  // ** load

  let inline load (path: FilePath) =
    either {
      let! data = Asset.read path
      let! group = Yaml.decode data
      return group
    }

  // ** loadAll

  let inline loadAll (basePath: FilePath) =
    either {
      try
        let files = Directory.getFiles (sprintf "*%s" ASSET_EXTENSION) basePath
        let! (_,groups) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate
          Array.fold
            (fun (m: Either<IrisError, int * ^t array>) path ->
              either {
                let! (idx,groups) = m
                let! group = load path
                groups.[idx] <- group
                return (idx + 1, groups)
              })
            (Right(0, arr))
            files
        return groups
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "PinGroup.LoadAll"
            |> Either.fail
    }

  // ** save

  let inline save (basePath: FilePath) asset =
    either {
      let path = basePath </> Asset.path asset
      let data = Yaml.encode asset
      let! _ = Asset.write path (Payload data)
      return ()
    }
