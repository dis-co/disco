namespace rec Iris.Core

// * Imports

open System
open System.Text.RegularExpressions

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open System.Linq
open FlatBuffers
open Iris.Serialization

#endif

// * FsPath

type FsPath =
  { Drive: char
    Platform: Platform
    Elements: string list }

  override path.ToString() =
    match path.Platform with
    | Platform.Windows -> string path.Drive + @":\\" + (String.concat @"\" path.Elements)
    | Platform.Unix -> "/" + (String.concat "/" path.Elements)

  // ** optics

  static member Drive_ =
    (fun path -> path.Drive),
    (fun drive path -> { path with Drive = drive })

  static member Platform_ =
    (fun path -> path.Platform),
    (fun platform path -> { path with Platform = platform })

  static member Elements_ =
    (fun path -> path.Elements),
    (fun elements path -> { path with Elements = elements })

  // ** ToOffset

  member path.ToOffset(builder:FlatBufferBuilder) =
    let platform = path.Platform.ToOffset(builder)
    let elements =
      path.Elements
      |> List.toArray
      |> Array.map
        (fun elm ->
          if elm <> null
          then builder.CreateString elm
          else Unchecked.defaultof<StringOffset>)
      |> fun arr -> FsPathFB.CreateElementsVector(builder, arr)

    FsPathFB.StartFsPathFB(builder)
    FsPathFB.AddDrive(builder, Convert.ToUInt16(path.Drive))
    FsPathFB.AddPlatform(builder, platform)
    FsPathFB.AddElements(builder, elements)
    FsPathFB.EndFsPathFB(builder)

  // ** FromFB

  static member FromFB(fb: FsPathFB) =
    either {
      let! platform = Platform.FromFB fb.Platform
      let drive = Convert.ToChar fb.Drive
      let! elements =
        Array.fold
          (fun (lst:Either<IrisError,string list>) idx -> either {
            let! elms = lst
            let elm = fb.Elements idx
            return elm :: elms
          })
          (Right List.empty)
          [| 0 .. fb.ElementsLength - 1 |]
      return {
        Drive = drive
        Platform = platform
        Elements = List.rev elements
      }
    }

// * FsInfo

type FsInfo =
  { Path: FsPath
    Name: Name
    Filtered: uint64
    Size: uint64 }

  // ** optics

  static member Path_ =
    (fun { Path = path } -> path),
    (fun path fsinfo -> { fsinfo with Path = path })

  static member Name_ =
    (fun { Name = name } -> name),
    (fun name fsinfo -> { fsinfo with Name = name })

  static member Size_ =
    (fun { Size = size } -> size),
    (fun size fsinfo -> { fsinfo with Size = size })

  static member Filtered_ =
    (fun { Filtered = filtered } -> filtered),
    (fun filtered fsinfo -> { fsinfo with Filtered = filtered })

// * FsEntry

[<RequireQualifiedAccess>]
type FsEntry =
  | File      of info:FsInfo
  | Directory of info:FsInfo * children:Map<FsPath,FsEntry>

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

  member self.Item(path:FsPath) =
    FsEntry.item path self

// * FsTree

type FsTree =
  { Filters: string array
    Root: FsEntry }

  // ** optics

  static member Root_ =
    (fun { Root = root } -> root),
    (fun root tree -> { tree with Root = root })

  static member Filters_ =
    (fun { Filters = filters } -> filters),
    (fun filters tree -> { tree with Filters = filters })

  // ** Item

  member self.Item(path:FsPath) =
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
    let path = Binary.toOffset builder info.Path
    FsInfoFB.StartFsInfoFB(builder)
    FsInfoFB.AddType(builder, tipe)
    FsInfoFB.AddSize(builder, info.Size)
    FsInfoFB.AddFiltered(builder, info.Filtered)
    Option.iter (fun offset -> FsInfoFB.AddName(builder, offset)) name
    FsInfoFB.AddPath(builder, path)
    FsInfoFB.EndFsInfoFB(builder)

  // ** toEntry

  static member toEntry(fb: FsInfoFB) =
    either {
      let! path =
        #if FABLE_COMPILER
        FsPath.FromFB fb.Path
        #else
        let pathish = fb.Path
        if pathish.HasValue then
          let path = pathish.Value
          FsPath.FromFB path
        else
          "Cannot parse empty path value"
          |> Error.asParseError "FsTree.toEntry"
          |> Either.fail
        #endif
      let info =
        { Path = path
          Name = name fb.Name
          Filtered = fb.Filtered
          Size = fb.Size }
      match fb.Type with
      #if FABLE_COMPILER
      | x when x = FsEntryTypeFB.DirectoryFB ->
      #else
      | FsEntryTypeFB.DirectoryFB ->
      #endif
        return FsEntry.Directory(info, Map.empty)
      #if FABLE_COMPILER
      | x when x = FsEntryTypeFB.FileFB ->
      #else
      | FsEntryTypeFB.FileFB ->
      #endif
        return FsEntry.File(info)
      | other ->
        return!
          other
          |> sprintf "%A is not a known FsEntry type"
          |> Error.asParseError "FsTree.toEntry"
          |> Either.fail
    }

  // ** FromBytes

  static member FromBytes (bytes: byte array) : Either<IrisError,FsTree> =
    bytes
    |> Binary.createBuffer
    |> FsTreeFB.GetRootAsFsTreeFB
    |> FsTree.FromFB

  // ** FromFB

  static member FromFB(fb:FsTreeFB) =
    either {
      let filters =
        fb.Filters.Split(' ')
        |> Array.filter (String.IsNullOrEmpty >> not)
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
      return children |> FsTree.inflate root |> FsTree.setFilters filters
    }

  // ** ToBytes

  member self.ToBytes () : byte array = Binary.buildBuffer self

  // ** ToOffset

  member tree.ToOffset(builder:FlatBufferBuilder) =
    let filters = tree.Filters |> String.concat " " |> builder.CreateString
    let root = FsTree.entryOffset builder tree.Root
    let children =
      tree
      |> FsTree.flatten
      |> List.map (FsTree.entryOffset builder)
      |> fun children -> FsTreeFB.CreateChildrenVector(builder, Array.ofList children)
    FsTreeFB.StartFsTreeFB(builder)
    FsTreeFB.AddRoot(builder, root)
    FsTreeFB.AddFilters(builder, filters)
    FsTreeFB.AddChildren(builder, children)
    FsTreeFB.EndFsTreeFB(builder)

// * Path

[<AutoOpen>]
module Path =
  // ** sanitize

  let sanitize (path:FilePath) =
    if Path.endsWith "/" path || Path.endsWith @"\" path
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

  // ** getDirectoryName

  let getDirectoryName (path: FilePath) =
    #if FABLE_COMPILER
    path
    #else
    pmap Path.GetDirectoryName path
    #endif

  // ** isParentOf

  let isParentOf (child:FilePath) (parent:FilePath) =
    let path = child |> sanitize |> getDirectoryName
    path = parent

  // ** isAncestorOf

  let isAncestorOf (child:FilePath) (ancestor:FilePath) =
    Path.beginsWith ancestor child

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

// * FsPath module

module FsPath =

  open Aether
  open Aether.Operators

  // ** getters

  let drive = Optic.get FsPath.Drive_
  let platform = Optic.get FsPath.Platform_
  let elements = Optic.get FsPath.Elements_

  // ** setters

  let setDrive = Optic.set FsPath.Drive_
  let setPlatform = Optic.set FsPath.Platform_
  let setElements = Optic.set FsPath.Elements_

  // ** path

  let path: FsPath -> FilePath = string >> filepath

  // ** fileName

  let fileName (path: FsPath): Name =
    if path.Elements.IsEmpty
    then name ""
    else path.Elements |> List.last |> name

  // ** parent

  let parent path =
    if path.Elements.Length > 0
    then { path with Elements = List.take (path.Elements.Length - 1) path.Elements }
    else path

  // ** isParentOf

  let isParentOf (child:FsPath) (ancestor:FsPath) =
    parent child = ancestor

  // ** isAncestorOf

  let isAncestorOf (child:FsPath) (ancestor:FsPath) =
    (string child).Contains(string ancestor)

  // ** parse

  #if !FABLE_COMPILER

  let parse (path:FilePath) =
    let drives: string [] =
      DriveInfo.GetDrives()
      |> Array.map
        (fun (drive:DriveInfo) ->
          string drive.RootDirectory
          |> filepath
          |> Path.sanitize
          |> unwrap)
    let segments:string list =
      Uri(unwrap path).Segments
      |> Array.collect
        (function
          | "/" -> Array.empty
          | other -> [| other |> filepath |> Path.sanitize |> unwrap |])
      |> Array.toList
    let platform = Platform.get()
    let drive, segments =
      match platform with
      | Windows ->
        let drive =
          if Array.contains segments.[0] drives
          then (string segments.[0]).[0]
          else 'C'
        let segments =
          try List.tail segments
          with _ -> List.empty
        drive, segments
      | Unix -> '/', segments
    { Drive = drive
      Platform = platform
      Elements = segments }

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
  let filtered = Optic.get (FsEntry.Info_ >-> FsInfo.Filtered_)
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
  let setFiltered = Optic.set (FsEntry.Info_ >-> FsInfo.Filtered_)
  let setChildren = Optic.set FsEntry.Children_

  // ** create

  #if !FABLE_COMPILER

  let create (fsPath:FsPath): FsEntry option =
    let path = string fsPath
    if Directory.Exists path then
      let di = DirectoryInfo path
      let subDirs = di.GetDirectories().Length
      let files = di.GetFiles()
      let info = {
        Path = fsPath
        Name = Measure.name di.Name
        Filtered = uint64 0
        Size = uint64 (files.Length + subDirs)
      }
      FsEntry.Directory (info, Map.empty) |> Some
    elif File.Exists path then
      let info = FileInfo(unwrap path)
      FsEntry.File {
        Path = fsPath
        Name = Measure.name info.Name
        Filtered = uint64 0
        Size = uint64 info.Length
      }
      |> Some
    else None

  #endif

  // ** isParentOf

  let isParentOf (child:FsEntry) (parent:FsEntry) =
    path parent = (child |> path |> FsPath.parent)

  // ** fold

  /// breadth-first fold on a tree
  let rec fold (f: 's -> FsEntry -> 's) (state: 's) = function
    | FsEntry.File _ as file -> f state file
    | FsEntry.Directory(info, children) as dir ->
      Map.fold (fun state _ entry -> f state entry) (f state dir) children

  // ** matches

  let matches (filters: string[]) (entry: FsEntry) =
    let name:string = unwrap (name entry)
    Array.fold
      (fun result filter -> result || name.EndsWith(filter))
      false
      filters

  // ** modify

  let rec modify (entry:FsPath) (f: FsEntry -> FsEntry) = function
    | FsEntry.File      _ as file when path file = entry -> f file
    | FsEntry.Directory _ as dir  when path dir  = entry -> f dir
    | FsEntry.Directory (_,children)
      as dir
      when FsPath.isParentOf entry (path dir) || FsPath.isAncestorOf entry (path dir) ->
      FsEntry.setChildren (Map.map (fun _ -> modify entry f) children) dir
    | other -> other

  // ** add

  let rec add (entry: FsEntry) filters =
    let adder = function
      | FsEntry.Directory(info, children) as dir ->
        let full = path entry
        if Map.containsKey full children
        then dir
        elif matches filters entry
        then setSize (size dir + uint64 1) dir
        else
          dir
          |> setChildren (Map.add full entry children)
          |> setSize (size dir + uint64 1)
      | other -> other
    modify (path entry) adder

  // ** remove

  #if !FABLE_COMPILER

  let rec remove (fp: FsPath) = function
    | FsEntry.Directory(info, children) as dir when FsPath.isParentOf fp (path dir) ->
      dir
      |> setChildren (Map.remove fp children)
      |> setSize (size dir - uint64 1)
    | FsEntry.Directory(info, children) when FsPath.isAncestorOf info.Path fp ->
      FsEntry.Directory(info, Map.map (fun _ -> remove fp) children)
    | other -> other

  #endif

  // ** update

  let update (entry:FsEntry) =
    modify (path entry) (fun _ -> entry)

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

  let rec filter (pred:FsEntry -> bool) = function
    | FsEntry.File _ as file -> file
    | FsEntry.Directory (info,children) ->
      let children =
        Map.fold
          (fun filtered path entry ->
            if pred entry
            then Map.add path (filter pred entry) filtered  /// keep when pred is true
            else filtered)                                  /// leave out when pred is false
          Map.empty
          children
      FsEntry.Directory (info, children)

  // ** tryFind

  #if !FABLE_COMPILER

  let rec tryFind (entry:FsPath) (tree:FsEntry) =
    match tree with
    | FsEntry.File      _ as file when path file = entry -> Some tree
    | FsEntry.Directory _ as dir  when path dir  = entry -> Some dir
    | FsEntry.Directory(_, children) as dir when FsPath.isParentOf entry (path dir) ->
      Map.tryFind entry children
    | FsEntry.Directory(_, children) as dir when FsPath.isAncestorOf entry (path dir) ->
      Map.tryPick (fun _ -> tryFind entry) children
    | _ -> None

  #endif

  // ** item

  let rec item (entry:FsPath) = function
    | FsEntry.File      _ as file when path file = entry -> file
    | FsEntry.Directory _ as dir  when path dir  = entry -> dir
    | FsEntry.Directory (_,children) as dir when FsPath.isAncestorOf entry (path dir) ->
      Map.pick
        (fun _ thing ->
          try item entry thing |> Some
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
    let filters = Array.empty
    directories
    |> List.sortBy (path >> string >> String.length)    /// sort by length of path to start with the
    |> List.fold (fun root dir -> add dir filters root) root /// bottom-most entries
    |> fun withDirs ->
      List.fold (fun root file -> add file filters root) withDirs files

  // ** directories

  let directories = filter isDirectory

  // ** files

  let files = flatten >> List.filter isFile

// * FsTree module

module FsTree =

  open Aether
  open Aether.Operators

  // ** getters

  let root = Optic.get FsTree.Root_
  let filters = Optic.get FsTree.Filters_

  // ** setters

  let setRoot = Optic.set FsTree.Root_
  let setFilters = Optic.set FsTree.Filters_

  // ** create

  #if !FABLE_COMPILER

  let create (basePath:FilePath) filters =
    let notFound () =
      basePath
      |> sprintf "%A was not found or is not a directory"
      |> Error.asAssetError "FsTree"
      |> Either.fail
    either {
      let! root =
        if Directory.exists basePath then
          let path = FsPath.parse basePath
          match FsEntry.create path with
          | Some root -> Right root
          | None -> notFound()
        else notFound()
      return {
        Filters = filters
        Root = root
      }
    }
  #endif

  // ** directories

  let directories = root >> FsEntry.directories

  // ** files

  let files = root >> FsEntry.files

  // ** basePath

  let basePath = root >> FsEntry.path

  // ** fileCount

  let fileCount (tree: FsTree) =
    FsEntry.fileCount tree.Root

  // ** directoryCount

  let directoryCount (tree: FsTree) =
    FsEntry.directoryCount tree.Root

  // ** tryFind

  #if !FABLE_COMPILER

  let tryFind (path:FsPath) (tree:FsTree) =
    FsEntry.tryFind path tree.Root

  #endif

  // ** modify

  let modify (path: FsPath) (f: FsEntry -> FsEntry) tree =
    tree |> root |> FsEntry.modify path f |> fun root -> setRoot root tree

  // ** add

  #if !FABLE_COMPILER

  let rec add (fp: FilePath) (tree: FsTree) =
    let fp =
      if Path.isPathRooted fp
      then fp |> Path.sanitize
      else fp |> Path.sanitize |> Path.getFullPath
    let fsPath = FsPath.parse fp
    if FsPath.isAncestorOf fsPath (basePath tree) then
      fsPath
      |> FsEntry.create
      |> Option.map (fun entry -> FsEntry.add entry tree.Filters tree.Root)
      |> Option.map (fun root -> { tree with Root = root })
      |> Option.defaultValue tree
    else tree

  #endif

  // ** remove

  #if !FABLE_COMPILER

  let remove (entry:FilePath) (tree: FsTree) =
    let entry =
      if Path.isPathRooted entry
      then entry |> Path.sanitize
      else entry |> Path.sanitize |> Path.getFullPath
    let fsPath = FsPath.parse entry
    if FsPath.isAncestorOf fsPath (basePath tree)  then
      tree.Root
      |> FsEntry.remove fsPath
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
    let fsPath = FsPath.parse path
    if FsPath.isAncestorOf fsPath (basePath tree) then
      fsPath
      |> FsEntry.create
      |> Option.filter (FsEntry.matches tree.Filters >> not)
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
    { Filters = Array.empty
      Root = FsEntry.inflate root entries }

  // ** filter

  let filter pred tree =
    tree
    |> root
    |> FsEntry.filter pred
    |> fun updated -> setRoot updated tree

// * FsTreeTesting

#if !FABLE_COMPILER

module FsTreeTesting =

  open System.IO

  let makeDir path name =
    let info = {
      Path = path
      Name = name
      Filtered = uint64 0
      Size = uint64 0
    }
    FsEntry.Directory(info,Map.empty)

  let makeFile path name =
    FsEntry.File {
      Path = path
      Name = name
      Filtered = uint64 0
      Size = uint64 0
    }

  let makeTree dirCount fileCount =
    let root, sub =
      let root = makeDir (FsPath.parse (filepath "/")) (Path.GetRandomFileName() |> name)
      let rootPath = FsEntry.path root
      let sub =
        [ for d in 1 .. dirCount do
            let dir = makeDir rootPath (Path.GetRandomFileName() |> name)
            let dirPath = FsEntry.path dir
            yield dir
            for f in 1 .. fileCount do
              yield makeFile dirPath (Path.GetRandomFileName() |> name) ]
      root, sub
    FsTree.inflate root sub

  let makeSubTree rootPath fileCount =
    let root = makeDir rootPath (Path.GetRandomFileName() |> name)
    let rootPath = FsEntry.path root
    [ yield root
      for n in fileCount do
        yield makeFile rootPath (Path.GetRandomFileName() |> name) ]

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

  let isAncestorOf (child:FilePath) (ancestor:FilePath) =
    Path.beginsWith ancestor child

  let fold p =
    let folder (acc:string list) (level:string list) =
      List.fold
        (fun (out:string list) path ->
          let perm = List.map (fun str -> Path.Combine(str,path)) acc
          List.append out perm)
        List.empty
        level
      |> List.append acc
    p
    |> List.fold folder (List.head p)
    |> List.sort
    |> List.map ((+) "/")

  let deepTree depth =
    let parsePath (str:string) =
      let split = str.Split('/') |> Array.filter (String.IsNullOrEmpty >> not)
      let name = split |> Array.last |> name
      let path = split |> Array.take (split.Length - 1) |> String.concat "/" |> (+) "/"
      name, filepath path
    let paths =
      fold [
        for d in 1 .. depth -> [ for n in 1 .. d -> Path.GetRandomFileName() ]
      ]
    let root, sub =
      let rootName, rootPath = List.head paths |> parsePath
      let rootPath = List.head paths |> filepath
      let root = makeDir (FsPath.parse (filepath "/")) rootName
      let files =
        [ for path in List.tail paths do
            let name,parsed = parsePath path
            let dir = makeDir (FsPath.parse parsed) name
            let file =
              makeFile
                (FsPath.parse (filepath path))
                (Path.GetRandomFileName() |> Measure.name)
            yield dir
            yield file ]
      root, files
    FsTree.inflate root sub

#endif
