namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import

#else

open System
open System.IO
open System.Linq

#endif

// * FsInfo

type FsInfo =
  { FullPath: FilePath
    Name: Name
    Size: uint64 }

  static member FullPath_ =
    (fun { FullPath = path } -> path),
    (fun path fsinfo -> { fsinfo with FullPath = path })

  static member Name_ =
    (fun { Name = path } -> path),
    (fun path fsinfo -> { fsinfo with Name = path })

  static member Size_ =
    (fun { Size = path } -> path),
    (fun path fsinfo -> { fsinfo with Size = path })

// * FsEntry

[<RequireQualifiedAccess>]
type FsEntry =
  | File      of info:FsInfo
  | Directory of info:FsInfo * children:FsEntry list

  // ** optics

  static member Info_ =
    (function
      | FsEntry.File info -> info
      | FsEntry.Directory(info,_) -> info),
    (fun info -> function
      | FsEntry.File _ -> FsEntry.File info
      | FsEntry.Directory(_,children) -> FsEntry.Directory(info,children))

  static member File_ =
    (function
      | FsEntry.File info -> Some info
      | _ -> None),
    (fun info -> function
      | FsEntry.File _ -> FsEntry.File info
      | other -> other)

  static member Directory_ =
    (function
      | FsEntry.Directory (info,_) -> Some info
      | _ -> None),
    (fun info -> function
      | FsEntry.Directory (_, children) -> FsEntry.Directory (info, children)
      | other -> other)

  static member Children_ =
    (function
      | FsEntry.Directory (_, children) -> children
      | _ -> []),
    (fun children -> function
      | FsEntry.Directory(info,_) -> FsEntry.Directory(info, children)
      | other -> other)

// * FsTree

type FsTree =
  { Root: FsEntry }

  // ** optics

  static member Root_ =
    (fun { Root = root } -> root),
    (fun root tree -> { tree with Root = root })

// * Path


[<AutoOpen>]
module Path =
  // ** combine

  let combine (p1: string) (p2: string) : FilePath =
    #if FABLE_COMPILER
    sprintf "%s/%s" p1 p2 |> filepath
    #else
    Path.Combine(p1, p2) |> filepath
    #endif

  // ** concat

  let concat (p1: FilePath) (p2: FilePath) : FilePath =
    #if FABLE_COMPILER
    sprintf "%O/%O" p1 p2 |> filepath
    #else
    combine (unwrap p1) (unwrap p2)
    #endif

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
    concat p1 p2

  // ** <.>

  // ** <.>

  let (<.>) (p1: string) (p2: string) : FilePath =
    combine p1 p2

  // ** pmap

  // ** pmap

  let pmap (f: string -> string) (path: FilePath) =
    path |> unwrap |> f |> filepath

  // ** map

  let inline map (f: string -> 'a) (path: FilePath) =
    path |> unwrap |> f

  // ** endsWith

  let endsWith (suffix: string) (path: FilePath) =
    (unwrap path : string).EndsWith suffix

  // ** baseName

  #if !FABLE_COMPILER

  let baseName (path: FilePath) =
    pmap Path.GetFileName path

  // ** directoryName

  let directoryName (path: FilePath) =
    pmap Path.GetDirectoryName path

  // ** getRandomFileName

  let getRandomFileName () =
    Path.GetRandomFileName() |> filepath

  // ** getTempPath

  let getTempPath () =
    Path.GetTempPath() |> filepath

  // ** getDirectoryName

  let getDirectoryName (path: FilePath) =
    pmap Path.GetDirectoryName path

  // ** isPathRooted

  let isPathRooted (path: FilePath) =
    map Path.IsPathRooted path

  // ** getFullPath

  let getFullPath (path: FilePath) =
    pmap Path.GetFullPath path

  // ** getFileName

  let getFileName (path: FilePath) =
    pmap Path.GetFileName path

  #endif

// * File

#if !FABLE_COMPILER

module File =

  // ** tag

  let private tag (str: string) = String.Format("File.{0}", str)

  // ** writeText

  let writeText (payload: string) (encoding: Text.Encoding option) (location: FilePath) =
    match encoding with
    | Some encoding -> File.WriteAllText(unwrap location, payload, encoding)
    | None -> File.WriteAllText(unwrap location, payload)

  // ** writeBytes

  let writeBytes (payload: byte array) (location: FilePath) =
    File.WriteAllBytes(unwrap location, payload)

  // ** writeLines

  let writeLines (payload: string array) (location: FilePath) =
    File.WriteAllLines(unwrap location, payload)

  // ** readText

  let readText (location: FilePath) =
    location |> unwrap |> File.ReadAllText

  // ** readBytes

  let readBytes (location: FilePath) =
    location |> unwrap |> File.ReadAllBytes

  // ** readLines

  let readLines (location: FilePath) =
    location |> unwrap |> File.ReadAllLines

  // ** info

  let info (path: FilePath) =
    path |> unwrap |> FileInfo

  // ** exists

  let exists (path: FilePath) =
    path
    |> unwrap
    |> File.Exists

  // ** delete

  let delete (path: FilePath) =
    try
      path
      |> unwrap
      |> File.Delete
      |> Either.succeed
    with exn ->
      exn.Message
      |> Error.asIOError (tag "delete")
      |> Either.fail

  // ** ensurePath

  let ensurePath (path: FilePath) =
    try
      path
      |> Path.getDirectoryName
      |> Directory.createDirectory
      |> Either.ignore
    with exn ->
      exn.Message
      |> Error.asIOError (tag "ensurePath")
      |> Either.fail

#endif

// * Directory

#if !FABLE_COMPILER

[<AutoOpen>]
module Directory =

  // ** tag

  let private tag (str: string) = String.Format("Directory.{0}",str)

  // ** createDirectory

  /// <summary>
  ///   Create a new directory. Upon failure, return an IrisError
  /// </summary>
  /// <param name="path">FilePath</param>
  /// <returns>Either<IrisError,DirectoryInfo></returns>
  let createDirectory (path: FilePath) =
    try
      path
      |> unwrap
      |> Directory.CreateDirectory
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "createDirectory")
        |> Either.fail

  // ** removeDirectory

  let removeDirectory (path: FilePath) =
    try
      unwrap path
      |> Directory.Delete
      |> Either.succeed
    with | exn ->
      exn.Message
      |> Error.asIOError (tag "removeDirectory")
      |> Either.fail

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

  // ** isEmpty

  let isEmpty (path: FilePath) =
    try
      DirectoryInfo(unwrap path).GetFileSystemInfos().Length = 0
    with exn ->
      true

  // ** getFiles

  let rec getFiles (recursive: bool) (pattern: string) (dir: FilePath) : FilePath[] =
    let current =
      Directory.GetFiles(unwrap dir, pattern)
      |> Array.map (filepath >> Path.getFullPath)
    if recursive then
      (unwrap dir)
      |> Directory.GetDirectories
      |> Array.fold
          (fun m dir ->
            filepath dir
            |> getFiles recursive pattern
            |> Array.append m)
          Array.empty
      |> Array.append current
    else current

  // ** getDirectories

  // ** getDirectories

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
    Path.GetTempPath() <.> Path.GetRandomFileName()

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
      let info = FileInfo(unwrap source)
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

// * FsEntry module

module FsEntry =

  open Aether
  open Aether.Operators

  // ** getters

  let info = Optic.get FsEntry.Info_
  let name = Optic.get (FsEntry.Info_ >-> FsInfo.Name_)
  let fullPath = Optic.get (FsEntry.Info_ >-> FsInfo.FullPath_)
  let size = Optic.get (FsEntry.Info_ >-> FsInfo.Size_)
  let children = Optic.get FsEntry.Children_
  let childCount = Optic.get FsEntry.Children_ >> List.length
  let isFile = Optic.get FsEntry.File_ >> Option.isSome
  let isDirectory = Optic.get FsEntry.Directory_ >> Option.isSome
  let file = Optic.get FsEntry.File_
  let directory = Optic.get FsEntry.Directory_

  // ** setters

  let setInfo = Optic.set FsEntry.Info_
  let setName = Optic.set (FsEntry.Info_ >-> FsInfo.Name_)
  let setFullPath = Optic.set (FsEntry.Info_ >-> FsInfo.FullPath_)
  let setSize = Optic.set (FsEntry.Info_ >-> FsInfo.Size_)
  let setChildren = Optic.set FsEntry.Children_

  // ** add

  let add (entry: FsEntry) = function
    | FsEntry.Directory(info, children) ->
      FsEntry.Directory(info, entry :: children)
    | other -> other

  // ** remove

  let remove (entry: FsEntry) = function
    | FsEntry.Directory(info, children) ->
      FsEntry.Directory(info, List.filter ((=) entry) children)
    | other -> other

  // ** read

  #if !FABLE_COMPILER

  let rec read (path:FilePath): FsEntry option =
    if File.exists path then
      let info = FileInfo(unwrap path)
      FsEntry.File {
        FullPath = filepath info.FullName
        Name = Measure.name info.Name
        Size = uint64 info.Length
      }
      |> Some
    elif Directory.exists path then
      let di = DirectoryInfo (unwrap path)
      let info = {
        FullPath = filepath di.FullName
        Name = Measure.name di.Name
        Size = uint64 0
      }
      let children =
        Array.fold
          (fun lst path ->
            match read path with
            | Some entry -> entry :: lst
            | None -> lst)
          []
          (Directory.fileSystemEntries path)
      FsEntry.Directory (info, children)
      |> Some
    else None

  #endif

// * FsTree module

module FsTree =

  // ** read

  #if !FABLE_COMPILER

  let rec read (path:FilePath) =
    path
    |> FsEntry.read
    |> Option.map (fun entry -> { Root = entry })

  #endif

#if INTERACTIVE

let tree = FsTree.read (filepath "/home/k/iris/assets")

#endif
