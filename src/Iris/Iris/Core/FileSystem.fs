namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System
open System.IO
open System.Linq
open FlatBuffers
open Iris.Serialization

#endif

// * FsInfo

type FsInfo =
  { Path: FilePath
    Name: Name
    Size: uint64 }

  // ** optics

  static member Path_ =
    (fun { Path = path } -> path),
    (fun path fsinfo -> { fsinfo with Path = path })

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
  | Directory of info:FsInfo * children:Map<FilePath,FsEntry>

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
      | _ -> Map.empty),
    (fun children -> function
      | FsEntry.Directory(info,_) -> FsEntry.Directory(info, children)
      | other -> other)

  // ** Item

  member self.Item(path:FilePath) =
    FsEntry.item path self


// * FsTree

type FsTree =
  { Root: FsEntry }

  // ** optics

  static member Root_ =
    (fun { Root = root } -> root),
    (fun root tree -> { tree with Root = root })

  // ** Item

  member self.Item(path:FilePath) =
    self.Root.[path]

  // ** entryOffset

  static member entryOffset (builder:FlatBufferBuilder) (entry: FsEntry) =
    let mapNull = function
      | null -> None
      | str -> builder.CreateString str |> Some
    let tipe = entry |> function
      | FsEntry.Directory _ -> FsEntryTypeFB.DirectoryFB
      | FsEntry.File _ -> FsEntryTypeFB.FileFB
    let info = FsEntry.info entry
    let name = info.Name |> unwrap |> mapNull
    let path = info.Path |> unwrap |> mapNull
    FsInfoFB.StartFsInfoFB(builder)
    FsInfoFB.AddType(builder, tipe)
    FsInfoFB.AddSize(builder, info.Size)
    Option.iter (fun offset -> FsInfoFB.AddName(builder, offset)) name
    Option.iter (fun offset -> FsInfoFB.AddPath(builder, offset)) path
    FsInfoFB.EndFsInfoFB(builder)

  // ** toEntry

  static member toEntry(fb: FsInfoFB) =
    let info =
      { Path = filepath fb.Path
        Name = name fb.Name
        Size = fb.Size }
    match fb.Type with
    #if FABLE_COMPILER
    | x when x = FsEntryTypeFB.DirectoryFB ->
    #else
    | FsEntryTypeFB.DirectoryFB ->
    #endif
      FsEntry.Directory(info, Map.empty)
      |> Either.succeed
    #if FABLE_COMPILER
    | x when x = FsEntryTypeFB.FileFB ->
    #else
    | FsEntryTypeFB.FileFB ->
    #endif
      FsEntry.File(info)
      |> Either.succeed
    | other ->
      other
      |> sprintf "%A is not a known FsEntry type"
      |> Error.asParseError "FsTree.toEntry"
      |> Either.fail

  // ** FromBytes

  static member FromBytes (bytes: byte array) : Either<IrisError,FsTree> =
    bytes
    |> Binary.createBuffer
    |> FsTreeFB.GetRootAsFsTreeFB
    |> FsTree.FromFB

  // ** FromFB

  static member FromFB(fb:FsTreeFB) =
    either {
      let! root =
        #if FABLE_COMPILER
        FsTree.toEntry fb.Root
        #else
        let rootish = fb.Root
        if rootish.HasValue then
          let rootFb = rootish.Value
          FsTree.toEntry rootFb
        else
          "Could not parse empty root"
          |> Error.asParseError "FsTree.FromtFB"
          |> Either.fail
        #endif
      let! children =
        Array.fold
          (fun (m:Either<IrisError,FsEntry list>) idx -> either {
              let! list = m
              let! child =
                #if FABLE_COMPILER
                idx |> fb.Children |> FsTree.toEntry
                #else
                let childish = fb.Children(idx)
                if childish.HasValue then
                  let child = childish.Value
                  FsTree.toEntry child
                else
                  "Could not parse empty child"
                  |> Error.asParseError "FsTree.FromFB"
                  |> Either.fail
                #endif
              return child :: list
            })
          (Right List.empty)
          [| 0 .. fb.ChildrenLength - 1 |]
      return FsTree.inflate root children
    }

  // ** ToBytes

  member self.ToBytes () : byte array = Binary.buildBuffer self

  // ** ToOffset

  member tree.ToOffset(builder:FlatBufferBuilder) =
    let root = FsTree.entryOffset builder tree.Root
    let children =
      tree
      |> FsTree.flatten
      |> List.map (FsTree.entryOffset builder)
      |> fun children -> FsTreeFB.CreateChildrenVector(builder, Array.ofList children)
    FsTreeFB.StartFsTreeFB(builder)
    FsTreeFB.AddRoot(builder, root)
    FsTreeFB.AddChildren(builder, children)
    FsTreeFB.EndFsTreeFB(builder)

// * Path

[<AutoOpen>]
module Path =
  // ** sanitize

  let sanitize (path:FilePath) =
    if Path.endsWith "/" path
    then Path.substring 0 (Path.length path - 1) path
    else path

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

  // ** beginsWith

  let beginsWith (prefix: FilePath) (path: FilePath) =
   let prefix:string = unwrap prefix
   let path: string = unwrap path
   path.StartsWith(prefix)

  // ** endsWith

  let endsWith (suffix: string) (path: FilePath) =
    (unwrap path : string).EndsWith suffix

  // ** contains

  let contains (path:FilePath) (contains:FilePath): bool =
    let path:string = unwrap path
    path.Contains(unwrap contains)

  // ** substring

  let substring (idx:int) (length:int) (path: FilePath) =
    let path:string = unwrap path
    path.Substring(idx, length) |> filepath

  // ** length

  let length (path: FilePath) =
    let path:string = unwrap path
    path.Length

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

  // ** getTempFile

  let getTempFile () =
    Path.GetTempFileName() |> filepath

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

  // ** isParentOf

  let isParentOf (child:FilePath) (parent:FilePath) =
    let path = child |> sanitize |> getDirectoryName
    path = parent

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
  let path = Optic.get (FsEntry.Info_ >-> FsInfo.Path_)
  let size = Optic.get (FsEntry.Info_ >-> FsInfo.Size_)
  let children = Optic.get FsEntry.Children_
  let childCount = Optic.get FsEntry.Children_ >> Map.count
  let isFile = Optic.get FsEntry.File_ >> Option.isSome
  let isDirectory = Optic.get FsEntry.Directory_ >> Option.isSome
  let file = Optic.get FsEntry.File_
  let directory = Optic.get FsEntry.Directory_

  // ** setters

  let setInfo = Optic.set FsEntry.Info_
  let setName = Optic.set (FsEntry.Info_ >-> FsInfo.Name_)
  let setPath = Optic.set (FsEntry.Info_ >-> FsInfo.Path_)
  let setSize = Optic.set (FsEntry.Info_ >-> FsInfo.Size_)
  let setChildren = Optic.set FsEntry.Children_

  // ** create

  #if !FABLE_COMPILER

  let create (path:FilePath): FsEntry option =
    if Directory.exists path then
      let di = DirectoryInfo (unwrap path)
      let info = {
        Path = filepath (Path.GetDirectoryName di.FullName)
        Name = Measure.name di.Name
        Size = uint64 0
      }
      FsEntry.Directory (info, Map.empty) |> Some
    elif File.exists path then
      let info = FileInfo(unwrap path)
      FsEntry.File {
        Path = filepath (Path.GetDirectoryName info.FullName)
        Name = Measure.name info.Name
        Size = uint64 info.Length
      }
      |> Some
    else None

  #endif

  // ** isParentOf

  let isParentOf (entry:FsEntry) (tree:FsEntry) =
    fullPath tree = path entry

  // ** fullPath

  let fullPath entry =
    path entry </> filepath (entry |> name |> unwrap)

  // ** add

  let rec add (entry: FsEntry) tree =
    match tree with
    | FsEntry.Directory(info, children) as dir when isParentOf entry tree ->
      let full = fullPath entry
      if Map.containsKey full children
      then dir
      else setChildren (Map.add full entry children) dir
    | FsEntry.Directory(info, children) as dir ->
      if Path.contains (fullPath entry) (fullPath dir) then
        setChildren (Map.map (fun _ -> add entry) children) dir
      else dir
    | other -> other

  // ** remove

  let rec remove (fp: FilePath) (tree:FsEntry) =
    match tree with
    | FsEntry.Directory(info, children) as dir when Path.isParentOf fp (fullPath dir) ->
      let children = Map.filter (fun existing _ -> existing <> fp) children
      FsEntry.Directory(info, children)
    | FsEntry.Directory(info, children) when Path.beginsWith info.Path fp ->
      FsEntry.Directory(info, Map.map (fun _ -> remove fp) children)
    | other -> other

  // ** update

  let update (entry:FsEntry) (tree:FsEntry) =
    match tree with
    | FsEntry.Directory(info, children) as dir when isParentOf entry tree ->
      let children =
        Map.map
          (fun path existing ->
            if fullPath entry = path
            then entry
            else existing)
          children
      setChildren children dir
    | FsEntry.Directory(info, children) as dir when Path.contains (fullPath entry) (fullPath dir) ->
      setChildren (Map.map (fun _ -> update entry) children) dir
    | other -> other

  // ** fileCount

  let fileCount tree =
    let rec count current = function
      | FsEntry.File _ -> current + 1
      | FsEntry.Directory(_, children) ->
        current + Map.fold (fun current _ entry -> count current entry) 0 children
    count 0 tree

  // ** directoryCount

  let directoryCount tree =
    let rec count current = function
      | FsEntry.File _ -> current
      | FsEntry.Directory(_, children) ->
        current + Map.fold (fun current _ entry -> count current entry) 1 children
    count 0 tree

  // ** filter

  let rec filter (pred:FsEntry -> bool) tree =
    failwith "filter"

  let directories tree = failwith "directories"
  let files tree = failwith "files"

  // ** tryFind

  let rec tryFind (path:FilePath) (tree:FsEntry) =
    let path = Path.sanitize path
    match tree with
    | FsEntry.File _ when fullPath tree = path -> Some tree
    | FsEntry.Directory _ as dir when fullPath dir = path -> Some dir
    | FsEntry.Directory(_, children) as dir when fullPath dir = Path.getDirectoryName path ->
      Map.tryFind path children
    | FsEntry.Directory(_, children) as dir when Path.beginsWith (fullPath dir) path ->
      Map.tryPick (fun _ -> tryFind path) children
    | _ -> None

  // ** item

  let rec item (path:FilePath) (tree:FsEntry) =
    let path = Path.sanitize path
    match tree with
    | FsEntry.File _ as file when fullPath file = path -> file
    | FsEntry.Directory _ as dir when fullPath dir = path -> dir
    | FsEntry.Directory (_,children) as dir when Path.beginsWith (fullPath dir) path ->
      Map.pick
        (fun _ entry ->
          try item path entry |> Some
          with _ -> None)
        children
    | _ -> failwithf "item %A not found" path

  // ** flatten

  let rec flatten = function
    | FsEntry.File _ as file -> [ file ]
    | FsEntry.Directory (_,children) as dir ->
      Map.fold
        (fun lst _ child -> List.append (flatten child) lst)
        [ setChildren Map.empty dir ]
        children

  // ** inflate

  let inflate (root:FsEntry) (entries:FsEntry list) =
    let directories = List.filter isDirectory entries
    let files = List.filter isFile entries
    directories
    |> List.sortBy (fullPath >> unwrap >> String.length) /// sort by length of path to start with the
    |> List.fold (fun root dir -> add dir root) root      /// bottom-most entries
    |> fun withDirs ->
      List.fold (fun root file -> add file root) withDirs files

// * FsTree module

module FsTree =

  // ** create

  #if !FABLE_COMPILER

  let create (basePath:FilePath) =
    if Directory.exists basePath then
      let basePath =
        if Path.endsWith "/" basePath
        then Path.substring 0 (Path.length basePath - 1) basePath
        else basePath
      let path = Path.getDirectoryName basePath
      let name = Path.baseName basePath |> string |> name
      let info = {
        Path = path
        Name = name
        Size = uint64 0
      }
      Either.succeed {
        Root = FsEntry.Directory(info, Map.empty)
      }
    else
      basePath
      |> sprintf "%A was not found or is not a directory"
      |> Error.asAssetError "FsTree"
      |> Either.fail

  #endif

  // ** root

  let root (tree:FsTree) = tree.Root

  // ** directories

  let directories: FsTree -> FsEntry = root >> FsEntry.directories

  // ** files

  let files: FsTree -> FsEntry = root >> FsEntry.files

  // ** basePath

  let basePath (tree: FsTree) =
    FsEntry.fullPath tree.Root

  // ** fileCount

  let fileCount (tree: FsTree) =
    FsEntry.fileCount tree.Root

  // ** directoryCount

  let directoryCount (tree: FsTree) =
    FsEntry.directoryCount tree.Root

  // ** tryFind

  let tryFind (path:FilePath) (tree:FsTree) =
    FsEntry.tryFind path tree.Root

  // ** add

  #if !FABLE_COMPILER

  let rec add (path: FilePath) (tree: FsTree) =
    let path =
      if Path.isPathRooted path
      then path |> Path.sanitize
      else path |> Path.sanitize |> Path.getFullPath
    if Path.beginsWith (basePath tree) path then
      path
      |> FsEntry.create
      |> Option.map (fun entry -> FsEntry.add entry tree.Root)
      |> Option.map (fun root -> { Root = root })
      |> Option.defaultValue tree
    else tree

  #endif

  // ** remove

  #if !FABLE_COMPILER

  let remove (path:FilePath) (tree: FsTree) =
    let path =
      if Path.isPathRooted path
      then path |> Path.sanitize
      else path |> Path.sanitize |> Path.getFullPath
    if Path.beginsWith (basePath tree) path then
      tree.Root
      |> FsEntry.remove path
      |> fun root -> { tree with Root = root }
    else tree

  #endif

  // ** update

  #if !FABLE_COMPILER

  let update (path:FilePath) (tree: FsTree) =
    let path =
      if Path.isPathRooted path
      then path |> Path.sanitize
      else path |> Path.sanitize |> Path.getFullPath
    if Path.beginsWith (basePath tree) path then
      path
      |> FsEntry.create
      |> Option.map (fun entry -> FsEntry.update entry tree.Root)
      |> Option.map (fun root -> { tree with Root = root })
      |> Option.defaultValue tree
    else tree

  #endif

  // ** flatten

  let flatten (tree:FsTree) =
    FsEntry.flatten tree.Root

  // ** inflate

  let inflate (root:FsEntry) (entries:FsEntry list) =
    { Root = FsEntry.inflate root entries }

  // ** read

  #if !FABLE_COMPILER

  let rec read (path:FilePath) =
    path
    |> FsEntry.create
    |> Option.map (fun entry -> { Root = entry })

  #endif

// * FsTreeTesting

#if !FABLE_COMPILER

module FsTreeTesting =

  open System.IO

  let makeDir path name =
    let info = {
      Path = path
      Name = name
      Size = uint64 0
    }
    FsEntry.Directory(info,Map.empty)

  let makeFile path name =
    FsEntry.File {
      Path = path
      Name = name
      Size = uint64 0
    }

  let makeTree dirCount fileCount =
    let root, sub =
      let root = makeDir (filepath "/") (Path.GetRandomFileName() |> name)
      let rootPath = FsEntry.fullPath root
      let sub =
        [ for d in 1 .. dirCount do
            let dir = makeDir rootPath (Path.GetRandomFileName() |> name)
            let dirPath = FsEntry.fullPath dir
            yield dir
            for f in 1 .. fileCount do
              yield makeFile dirPath (Path.GetRandomFileName() |> name) ]
      root, sub
    FsTree.inflate root sub

  let writeTree fp (tree:FsTree) =
    let bytes = Binary.encode tree
    File.writeBytes bytes fp

  let readTree fp: FsTree =
    fp
    |> File.readBytes
    |> Binary.decode
    |> Either.get

  let roundTrip dirCount fileCount =
    let fp = Path.getTempFile()
    printfn "creating tree"
    let tree = makeTree dirCount fileCount
    printfn "writing to %A" fp
    do writeTree fp tree
    printfn "loading from %A" fp
    let loaded = readTree fp
    printfn "result:"
    printfn "   trees equal: %b" (tree = loaded)
    printfn "   dir count tree: %b" (FsTree.directoryCount tree = dirCount + 1)
    printfn "   file count tree: %b" (FsTree.fileCount tree = fileCount * dirCount)
    printfn "   dir count loaded: %b" (FsTree.directoryCount loaded = dirCount + 1)
    printfn "   file count loaded: %b" (FsTree.fileCount loaded = fileCount * dirCount)

#endif
