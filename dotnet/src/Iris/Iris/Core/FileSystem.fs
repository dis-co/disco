namespace  Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import

#else

open System
open System.IO
open System.Linq

#endif

// * Path

#if !FABLE_COMPILER

[<AutoOpen>]
module Path =

  // ** </>

  /// ## </>
  ///
  /// Combine two FilePath (string) into one with the proper separator.
  ///
  /// ### Signature:
  /// - path1: first path
  /// - path2: second path
  ///
  /// Returns: FilePath (string)
  let (</>) (p1: FilePath) (p2: FilePath) : FilePath =
    #if FABLE_COMPILER
    String.Format "{0}/{1}" p1 p2
    |> filepath
    #else
    Path.Combine(unwrap p1, unwrap p2) |> filepath
    #endif

  let (<.>) (p1: string) (p2: string) : FilePath =
    #if FABLE_COMPILER
    String.Format "{0}/{1}" p1 p2
    |> filepath
    #else
    Path.Combine(p1, p2) |> filepath
    #endif

  let pmap (f: string -> string) (path: FilePath) =
    path |> unwrap |> f |> filepath

  let inline map (f: string -> 'a) (path: FilePath) =
    path |> unwrap |> f

  let baseName (path: FilePath) =
    pmap Path.GetFileName path

  let dirName (path: FilePath) =
    pmap Path.GetDirectoryName path

  let endsWith (suffix: string) (path: FilePath) =
    (unwrap path : string).EndsWith suffix

  let getRandomFileName () =
    Path.GetRandomFileName() |> filepath

  let getTempPath () =
    Path.GetTempPath() |> filepath

  let getDirectoryName (path: FilePath) =
    pmap Path.GetDirectoryName path

  let isPathRooted (path: FilePath) =
    map Path.IsPathRooted path

  let getFullPath (path: FilePath) =
    pmap Path.GetFullPath path

  let getFileName (path: FilePath) =
    pmap Path.GetFileName path

#endif

// * File

#if !FABLE_COMPILER

module File =

  let writeText (payload: string) (encoding: Text.Encoding option) (location: FilePath) =
    File.WriteAllText(unwrap location, payload)

  let writeBytes (payload: byte array) (location: FilePath) =
    File.WriteAllBytes(unwrap location, payload)

  let writeLines (payload: string array) (location: FilePath) =
    File.WriteAllLines(unwrap location, payload)

  let readText (location: FilePath) =
    location |> unwrap |> File.ReadAllText

  let readBytes (location: FilePath) =
    location |> unwrap |> File.ReadAllBytes

  let readLines (location: FilePath) =
    location |> unwrap |> File.ReadAllBytes

  // ** info

  let info (path: FilePath) =
    path |> unwrap |> FileInfo

  // ** exists

  let exists (path: FilePath) =
    path
    |> unwrap
    |> File.Exists

#endif

// * Directory

#if !FABLE_COMPILER && !IRIS_NODES

[<AutoOpen>]
module Directory =

  // ** createDirectory

  let createDirectory (path: FilePath) =
    path
    |> unwrap
    |> Directory.CreateDirectory

  // ** info

  let info (path: FilePath) =
    path |> unwrap |> DirectoryInfo

  // ** fileSystemEntries

  let fileSystemEntries (path: FilePath) =
    path
    |> unwrap
    |> Directory.GetFileSystemEntries
    |> Array.map filepath

  // ** exists

  let exists (path: FilePath) =
    path
    |> unwrap
    |> Directory.Exists

  // ** getFiles

  let getFiles (pattern: string) (dir: FilePath) =
    Directory.GetFiles(unwrap dir, pattern)
    |> Array.map filepath

  let getDirectories (path: FilePath) =
    path
    |> unwrap
    |> Directory.GetDirectories
    |> Array.map filepath

#endif

// * FileSystem

[<AutoOpen>]
module FileSystem =
  open Path

  // ** tmpDir

  #if !FABLE_COMPILER

  let tmpPath () =
    (Path.GetTempPath() |> filepath) </> (filepath <| Path.GetRandomFileName())

  #endif

  // ** moveFile

  #if !FABLE_COMPILER

  /// ## moveFile
  ///
  /// Move a file or directory from source to dest.
  ///
  /// ### Signature:
  /// - source: FilePath
  /// - dest: FilePath
  ///
  /// Returns: unit
  let moveFile (source: FilePath) (dest: FilePath) =
    try
      let info = new FileInfo(unwrap source)
      let attrs = info.Attributes
      if attrs.HasFlag(FileAttributes.Directory) then
        Directory.Move(unwrap source,unwrap dest)
      else
        File.Move(unwrap source, unwrap dest)
    with | _ -> ()

  #endif

  // ** rmDir

  #if !FABLE_COMPILER

  /// ## delete a file or directory
  ///
  /// recursively delete a directory or single File.
  ///
  /// ### Signature:
  /// - path: FilePath to delete
  ///
  /// Returns: Either<IrisError, unit>
  let rec rmDir (path: FilePath) : Either<IrisError,unit>  =
    try
      let info = new FileInfo(unwrap path)
      info.IsReadOnly <- false
      let attrs = info.Attributes
      if (attrs &&& FileAttributes.Directory) = FileAttributes.Directory then
        let children = DirectoryInfo(unwrap path).EnumerateFileSystemInfos()
        if children.Count() > 0 then
          either {
            do! Seq.fold
                  (fun (_: Either<IrisError, unit>) (child: FileSystemInfo) -> either {
                      return! child.FullName |> filepath |> rmDir
                    })
                  (Right ())
                  children
            return Directory.Delete(unwrap path)
          }
        else
          Directory.Delete(unwrap path)
          |> Either.succeed
      else
        path
        |> unwrap
        |> File.Delete
        |> Either.succeed
    with
      | exn ->
        ("FileSystem.rmDir", exn.Message)
        |> IOError
        |> Either.fail

  #endif

  // ** lsDir

  #if !FABLE_COMPILER

  /// ## lsDir
  ///
  /// Enumerate all files in a given path. Returns the empty list for non-existent paths.
  ///
  /// ### Signature:
  /// - path: FilePath
  ///
  /// Returns: FilePath list
  let rec lsDir (path: FilePath) : FilePath list =
    if path |> unwrap |>  File.Exists then
      [ path ]
    elif path |> unwrap |> Directory.Exists then
      let children =
        Array.fold
          (fun lst path' ->
            let children' = lsDir path'
            children' :: lst)
          []
          (Directory.fileSystemEntries path)
        |> List.concat
      path :: children
    else []

  #endif

  // ** mkDir

  #if !FABLE_COMPILER

  /// ## create a new directory
  ///
  /// Create a directory at Path.
  ///
  /// ### Signature:
  /// - path: FilePath
  ///
  /// Returns: Either<IrisError, unit>
  let mkDir (path: FilePath) =
    try
      if path |> unwrap |> Directory.Exists |> not then
        path
        |> unwrap
        |> Directory.CreateDirectory
        |> ignore
        |> Either.succeed
      else
        Either.succeed ()
    with
      | exn ->
        ("FileSystem.mkDir", exn.Message)
        |> IOError
        |> Either.fail

  #endif

  // ** copyDir

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## copyDir
  ///
  /// Copy the specified directory recursively to target.
  ///
  /// ### Signature:
  /// - source: FilePath
  /// - target: FilePath
  ///
  /// Returns: Either<IrisError,unit>

  let rec copyDir (source: FilePath) (target: FilePath) : Either<IrisError,unit> =
    try
      let source = Directory.info source

      let target =
        let info = Directory.info target
        if not info.Exists then
          Directory.CreateDirectory info.FullName
        else info

      for file in source.GetFiles() do
        let destpath = filepath target.FullName </> filepath file.Name
        file.CopyTo(unwrap destpath, false) |> ignore

      for dir in source.GetDirectories() do
        let destpath = filepath target.FullName </> filepath dir.Name
        copyDir (filepath dir.FullName) destpath |> ignore

      Either.succeed ()
    with
      | exn ->
        ("FileSystem.mkDir", exn.Message)
        |> IOError
        |> Either.fail

  #endif
