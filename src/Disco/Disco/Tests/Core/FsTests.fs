namespace Disco.Tests

open System
open System.IO
open Expecto
open Expecto.Expect
open Disco.Core

[<AutoOpen>]
module FsTests =

  let withTmpDir (f: FilePath -> unit)  =
    let basePath = Path.GetTempPath()
    Directory.CreateDirectory(basePath) |> ignore
    let path = Path.Combine(basePath, Path.GetRandomFileName())
    Directory.CreateDirectory(path) |> ignore
    path |> filepath |> f
    FileSystem.rmDir (filepath path) |> ignore

  let withTree (f: FsTree -> unit) =
    withTmpDir (fun path -> FsTree.create (DiscoId.Create()) path Array.empty |> Either.get |> f)

  let test_should_have_correct_base_path =
    testCase "should have correct base path" <| fun _ ->
      withTmpDir <| fun path ->
        let tree = FsTree.create (DiscoId.Create()) path Array.empty |> Either.get
        Expect.equal (FsTree.basePath tree) (FsPath.parse path) "Should have correct base path"

  let test_should_handle_base_path_with_slash =
    testCase "should handle base path with slash" <| fun _ ->
      withTmpDir <| fun path ->
        let withSlash = filepath (unwrap path + "/")
        let tree = FsTree.create (DiscoId.Create()) withSlash Array.empty |> Either.get
        Expect.equal (FsTree.basePath tree) (FsPath.parse path) "Should have correct base path"

  let test_fspath_is_sane =
    testCase "fspath is sane" <| fun _ ->
      let tmp = Path.getTempPath()
      let dir = tmp </> Path.getRandomFileName()
      let path = dir </> Path.getRandomFileName()
      let other = Path.getTempFile()

      let fsDir = FsPath.parse dir
      let fsPath = FsPath.parse path
      let fsTmp = FsPath.parse tmp
      let fsOther = FsPath.parse other

      Expect.isTrue   (fsTmp.isParentOf        fsDir)  "fsTmp should be the parent of fsDir"
      Expect.isTrue   (fsDir.isParentOf        fsPath) "fsDir should be the parent of fsPath"
      Expect.isTrue   (fsTmp.isAncestorOf      fsPath) "fsTmp should be the ancestor of fsPath"
      Expect.isFalse  (fsPath.isAncestorOf     fsPath) "should not be the ancestor"
      Expect.isFalse  (fsPath.isParentOf       fsPath) "should not be the parent"
      Expect.notEqual (FsPath.parent fsOther)  fsDir   "parent should work correctly"
      Expect.equal    (FsPath.parent fsPath)   fsDir   "parent should work correctly"
      Expect.equal    (FsPath.parent fsDir)    fsTmp   "parent should work correctly"
      Expect.equal    (FsPath.filePath fsPath) path    "filePath should work correctly"

  let test_modify_should_be_correct =
    testCase "modify should be correct" <| fun _ ->
      withTree <| fun tree ->
        let basePath = FsTree.basePath tree
        let dirPath = Path.getRandomFileName()
        let file1Path = Path.getRandomFileName()
        let file2Path = Path.getRandomFileName()

        let dir =
          FsEntry.Directory(
            { Path = basePath + dirPath
              Name = name (unwrap dirPath)
              MimeType = "application/x-directory"
              Filtered = 0u
              Size = 0u
            }, Map.empty)

        let tree1 = FsTree.modify basePath (FsEntry.addChild dir) tree

        Expect.equal (tree1.[basePath + dirPath]) dir "it should contain dir"

        let file1 =
          FsEntry.File(
            { Path = basePath + dirPath + file1Path
              Name = name (unwrap file1Path)
              MimeType = "application/bla"
              Filtered = 0u
              Size = 0u
            })

        let tree2 =
          FsTree.modify
            (file1 |> FsEntry.path |> FsPath.parent)
            (FsEntry.addChild file1)
            tree1

        Expect.equal (tree2.[basePath + dirPath + file1Path]) file1 "it should contain file1"

        let file2 =
          FsEntry.File(
            { Path = basePath + file2Path
              Name = name (unwrap file2Path)
              MimeType = "application/bla"
              Filtered = 0u
              Size = 0u
            })

        let tree3 =
          FsTree.modify
            (file2 |> FsEntry.path |> FsPath.parent)
            (FsEntry.addChild file2)
            tree2

        Expect.equal (tree3.[basePath + file2Path]) file2 "it should contain file2"

        let size = 666u

        let tree4 =
          FsTree.modify
            (FsEntry.path file2)
            (FsEntry.setSize size)
            tree3

        Expect.equal (FsEntry.size tree4.[basePath + file2Path]) size "it should have correct size"


  let test_should_add_file_entry_at_correct_point =
    testCase "should add file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path + filepath "dir1"
        let dir2 = path + filepath "dir2"
        let dir3 = dir2 + filepath "dir3"

        let file1 = dir1 + filepath "file1.txt"
        let file2 = dir3 + filepath "file2.txt"

        do Directory.createDirectory (FsPath.filePath dir1) |> ignore
        do Directory.createDirectory (FsPath.filePath dir2) |> ignore
        do Directory.createDirectory (FsPath.filePath dir3) |> ignore

        do File.writeText "Hello!" None (FsPath.filePath file1)
        do File.writeText "Bye!"   None (FsPath.filePath file2)

        let tree =
          tree
          |> FsTree.add (FsPath.filePath dir1)
          |> FsTree.add (FsPath.filePath dir2)
          |> FsTree.add (FsPath.filePath dir3)
          |> FsTree.add (FsPath.filePath file1)
          |> FsTree.add (FsPath.filePath file2)

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree)      2 "Should have 2 files"

        let fileEntry1 = FsTree.tryFind file1 tree |> Option.get
        let fileEntry2 = FsTree.tryFind file2 tree |> Option.get
        let dirEntry1 = FsTree.tryFind dir1 tree |> Option.get
        let dirEntry2 = FsTree.tryFind dir2 tree |> Option.get
        let dirEntry3 = FsTree.tryFind dir3 tree |> Option.get

        Expect.isTrue (dirEntry1.isParentOf   fileEntry1) "dirEntry1 should be the parent of fileEntry1"
        Expect.isTrue (dirEntry2.isAncestorOf fileEntry2) "dirEntry2 should be the ancestor of fileEntry2"
        Expect.isTrue (dirEntry3.isParentOf   fileEntry2) "dirEntry2 should be the parent of fileEntry2"
        Expect.isTrue (tree.Root.isParentOf  dirEntry1) "root should be the parent dirEntry1"
        Expect.isTrue (tree.Root.isParentOf  dirEntry2) "root should be the parent dirEntry2"
        Expect.isTrue (dirEntry2.isParentOf  dirEntry3) "dirEntry2 should be the parent dirEntry3"

        Expect.equal tree.[file1] fileEntry1 "Should be equal"
        Expect.equal tree.[file2] fileEntry2 "Should be equal"
        Expect.equal tree.[dir1] dirEntry1 "Should be equal"
        Expect.equal tree.[dir2] dirEntry2 "Should be equal"
        Expect.equal tree.[dir3] dirEntry3 "Should be equal"

  let test_should_remove_file_entry_at_correct_point =
    testCase "should remove file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path + filepath "dir1"
        let dir2 = path + filepath "dir2"
        let dir3 = dir2 + filepath "dir3"

        let file1 = dir1 + filepath "file1.txt"
        let file2 = dir3 + filepath "file2.txt"

        do Directory.createDirectory (FsPath.filePath dir1) |> ignore
        do Directory.createDirectory (FsPath.filePath dir2) |> ignore
        do Directory.createDirectory (FsPath.filePath dir3) |> ignore

        do File.writeText "Hello!" None (FsPath.filePath file1)
        do File.writeText "Bye!"   None (FsPath.filePath file2)

        let tree =
          tree
          |> FsTree.add (FsPath.filePath dir1)
          |> FsTree.add (FsPath.filePath dir2)
          |> FsTree.add (FsPath.filePath dir3)
          |> FsTree.add (FsPath.filePath file1)
          |> FsTree.add (FsPath.filePath file2)

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 2 "Should have 2 files"

        let processed1 = FsTree.remove (FsPath.filePath file2) tree

        Expect.isNone (FsTree.tryFind file2 processed1) "File should be gone"
        Expect.equal (FsTree.directoryCount processed1) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount processed1) 1 "Should have 1 file"

        let processed2 = FsTree.remove (FsPath.filePath file1) processed1

        Expect.isNone (FsTree.tryFind file1 processed2) "File should be gone"
        Expect.equal (FsTree.directoryCount processed2) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount processed2) 0 "Should have 0 files"

        let processed3 = FsTree.remove (FsPath.filePath dir1) tree

        Expect.isNone (FsTree.tryFind dir1 processed3) "Dir should be gone"
        Expect.equal (FsTree.directoryCount processed3) 3 "Should be 3 directories"
        Expect.equal (FsTree.fileCount processed3) 1 "Should have 1 file"

        let processed4 = FsTree.remove (FsPath.filePath dir2) processed3

        Expect.isNone (FsTree.tryFind dir2 processed4) "Dir should be gone"
        Expect.equal (FsTree.directoryCount processed4) 1 "Should be 1 directory"
        Expect.equal (FsTree.fileCount processed4) 0 "Should have 0 files"

  let test_should_update_file_entry_at_correct_point =
    testCase "should update file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path + filepath "dir1"
        let dir2 = path + filepath "dir2"
        let dir3 = dir2 + filepath "dir3"

        let file1 = dir1 + filepath "file1.txt"
        let file2 = dir3 + filepath "file2.txt"

        do Directory.createDirectory (FsPath.filePath dir1) |> ignore
        do Directory.createDirectory (FsPath.filePath dir2) |> ignore
        do Directory.createDirectory (FsPath.filePath dir3) |> ignore

        let content1 = "Hello!"
        let content2 = "Bye!"

        do File.writeText content1 None (FsPath.filePath file1)
        do File.writeText content2 None (FsPath.filePath file2)

        let tree =
          tree
          |> FsTree.add (FsPath.filePath dir1)
          |> FsTree.add (FsPath.filePath dir2)
          |> FsTree.add (FsPath.filePath dir3)
          |> FsTree.add (FsPath.filePath file1)
          |> FsTree.add (FsPath.filePath file2)

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 2 "Should have 2 files"

        let fileEntry1 = FsTree.tryFind file1 tree |> Option.get
        let fileEntry2 = FsTree.tryFind file2 tree |> Option.get

        do File.writeText content1 None (FsPath.filePath file2)

        let tree = FsTree.update (FsPath.filePath file2) tree

        let fileEntry3 = FsTree.tryFind file2 tree |> Option.get

        Expect.equal (FsEntry.size fileEntry3) (FsEntry.size fileEntry1) "Should have same size now"

  let test_should_correctly_flatten_and_inflate_tree =
    testCase "should correctly flatten and inflate tree" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path + filepath "dir1"
        let dir2 = path + filepath "dir2"
        let file1 = dir1 + filepath "file1.txt"
        let file2 = dir2 + filepath "file2.txt"
        do Directory.createDirectory (FsPath.filePath dir1) |> ignore
        do Directory.createDirectory (FsPath.filePath dir2) |> ignore
        do File.writeText "Hello!" None (FsPath.filePath file1)
        do File.writeText "Bye!"   None (FsPath.filePath file2)

        let tree =
          tree
          |> FsTree.add (FsPath.filePath dir1)
          |> FsTree.add (FsPath.filePath dir2)
          |> FsTree.add (FsPath.filePath file1)
          |> FsTree.add (FsPath.filePath file2)

        let flattened = FsTree.flatten tree
        let root = FsEntry.setChildren Map.empty tree.Root
        let inflated = FsTree.inflate tree.HostId root flattened
        Expect.equal inflated tree "Inflated tree should be equal to original"

  let test_should_have_correct_counts =
    testCase "should have correct counts" <| fun _ ->
      let rnd = System.Random()
      let dirCount = rnd.Next(2,10)
      let fileCount = rnd.Next(3,9000)
      let tree = FsTreeTesting.makeTree dirCount fileCount
      Expect.equal (FsTree.fileCount tree) (fileCount * dirCount)  "Should have correct count"
      Expect.equal (FsTree.directoryCount tree) (dirCount + 1) "Should have correct count"

  let test_should_apply_filters_on_add =
    testCase "should apply filters on add" <| fun _ ->
      withTree <| fun tree ->
        let tree = FsTree.setFilters [| ".txt" |] tree
        let path = FsTree.basePath tree
        let dir1 = path + filepath "dir1"
        let dir2 = path + filepath "dir2"
        let dir3 = dir2 + filepath "dir3"

        let file1 = dir1 + filepath "file1.txt"
        let file2 = dir3 + filepath "file2.txt"

        do Directory.createDirectory (FsPath.filePath dir1) |> ignore
        do Directory.createDirectory (FsPath.filePath dir2) |> ignore
        do Directory.createDirectory (FsPath.filePath dir3) |> ignore

        do File.writeText "Hello!" None (FsPath.filePath file1)
        do File.writeText "Bye!"   None (FsPath.filePath file2)

        let tree =
          tree
          |> FsTree.add (FsPath.filePath dir1)
          |> FsTree.add (FsPath.filePath dir2)
          |> FsTree.add (FsPath.filePath dir3)
          |> FsTree.add (FsPath.filePath file1)
          |> FsTree.add (FsPath.filePath file2)

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 0 "Should have no files"

  let test_should_track_size_filtered_correctly =
    testCase "should track size/filtered correctly" <| fun _ ->
      let rootPath = FsPath.parse (Path.getTempPath())
      let dirPath1 = rootPath + Path.getRandomFileName()
      let dirPath2 = rootPath + Path.getRandomFileName()
      let dirPath3 = rootPath + Path.getRandomFileName()
      let filePath1 = dirPath1 + Path.getRandomFileName()
      let filePath2 = dirPath2 + Path.getRandomFileName()
      let filePath3 = dirPath3 + (filepath "files.org")
      let dir1 = FsTreeTesting.makeDir dirPath1
      let dir2 = FsTreeTesting.makeDir dirPath2
      let dir3 = FsTreeTesting.makeDir dirPath3
      let file1 = FsTreeTesting.makeFile filePath1
      let file2 = FsTreeTesting.makeFile filePath2
      let file3 = FsTreeTesting.makeFile filePath3

      let ext = Path.getExtension (FsPath.filePath filePath3)
      let tree = FsTreeTesting.makeDir rootPath

      Expect.equal (FsEntry.size tree)     0u "tree should have size 0"
      Expect.equal (FsEntry.filtered tree) 0u "tree should have filtered 0"

      ///     _       _     _   ____  _
      ///    / \   __| | __| | |  _ \(_)_ __ ___
      ///   / _ \ / _` |/ _` | | | | | | '__/ __|
      ///  / ___ \ (_| | (_| | | |_| | | |  \__ \
      /// /_/   \_\__,_|\__,_| |____/|_|_|  |___/

      let tree = FsEntry.add dir1 Array.empty tree

      Expect.equal (FsEntry.size tree)     1u "tree should have size 1"
      Expect.equal (FsEntry.filtered tree) 0u "tree should have filtered 0"

      let tree = FsEntry.add dir2 Array.empty tree

      Expect.equal (FsEntry.size tree)     2u "tree should have size 2"
      Expect.equal (FsEntry.filtered tree) 0u "tree should have filtered 0"

      let tree = FsEntry.add dir3 Array.empty tree

      Expect.equal (FsEntry.size tree)     3u "tree should have size 3"
      Expect.equal (FsEntry.size tree.[dirPath1]) 0u "dir1 should have size 0"
      Expect.equal (FsEntry.filtered tree) 0u "tree should have filtered 0"

      ///     _       _     _   _____ _ _
      ///    / \   __| | __| | |  ___(_) | ___
      ///   / _ \ / _` |/ _` | | |_  | | |/ _ \
      ///  / ___ \ (_| | (_| | |  _| | | |  __/
      /// /_/   \_\__,_|\__,_| |_|   |_|_|\___|

      let tree = FsEntry.add file1 Array.empty tree

      Expect.equal (FsEntry.size tree)     3u "tree should have size 3"
      Expect.equal (FsEntry.size tree.[dirPath1]) 1u "dir1 should have size 1"
      Expect.equal (FsEntry.filtered tree) 0u "tree should have filtered 0"

      ///     _       _     _   _____ _ _ _                    _
      ///    / \   __| | __| | |  ___(_) | |_ ___ _ __ ___  __| |
      ///   / _ \ / _` |/ _` | | |_  | | | __/ _ \ '__/ _ \/ _` |
      ///  / ___ \ (_| | (_| | |  _| | | | ||  __/ | |  __/ (_| |
      /// /_/   \_\__,_|\__,_| |_|   |_|_|\__\___|_|  \___|\__,_|

      let tree = FsEntry.add file3 [| ext |] tree

      Expect.equal (FsEntry.size tree.[dirPath3]) 1u "dir3 should have size 1"
      Expect.equal (FsEntry.filtered tree.[dirPath3]) 1u "dir3 should have filtered 1"
      Expect.throws (fun _ -> ignore tree.[filePath3]) "file3 should not be present"

      ///  ____                                 _____ _ _ _                    _
      /// |  _ \ ___ _ __ ___   _____   _____  |  ___(_) | |_ ___ _ __ ___  __| |
      /// | |_) / _ \ '_ ` _ \ / _ \ \ / / _ \ | |_  | | | __/ _ \ '__/ _ \/ _` |
      /// |  _ <  __/ | | | | | (_) \ V /  __/ |  _| | | | ||  __/ | |  __/ (_| |
      /// |_| \_\___|_| |_| |_|\___/ \_/ \___| |_|   |_|_|\__\___|_|  \___|\__,_|

      let tree = FsEntry.remove filePath3 [| ext |] tree

      Expect.equal (FsEntry.size tree.[dirPath3]) 0u "dir3 should have size 0"
      Expect.equal (FsEntry.filtered tree.[dirPath3]) 0u "dir3 should have filtered 0"

  let test_map_should_be_correct =
    testCase "map should be correct" <| fun _ ->
      let tree = FsTreeTesting.deepTree 2
      let mapped = FsTree.map id tree
      Expect.equal mapped tree "Mapped with id should be equal"

      let mapped = FsTree.map (FsEntry.setSize 666u) tree
      Expect.notEqual mapped tree "Mapped with size bump should not be equal"

      /// all should have 666 as size
      for entry in FsTree.flatten mapped do
        Expect.equal (FsEntry.size entry) 666u "Should have correct size"

  let test_apply_filters_should_be_correct =
    testCase "map should be correct" <| fun _ ->
      let rnd = Random()
      let tree = FsTreeTesting.deepTree 2
      let fileCount = FsTree.fileCount tree
      let extensions =
        tree
        |> FsTree.files
        |> List.fold
          (fun state entry ->
            let ext = entry |> FsEntry.name |> unwrap |> Path.GetExtension
            match Map.tryFind ext state with
            | Some count -> Map.add ext (count + 1) state
            | None -> Map.add ext 1 state)
          Map.empty
      let keep, filters =
        let arr = extensions |> Map.toArray
        let size =
          let len = Array.length arr
          if len % 2 = 0
          then len / 2
          else (len / 2) + 1
        let chunked = Array.chunkBySize size arr
        Expect.equal chunked.Length 2 "Should have exactly two elements"
        Array.head chunked, Array.last chunked
      let filterSize = Array.fold (fun s (_, n) -> s + n) 0 filters
      let filtered =
        tree
        |> FsTree.setFilters (Array.map fst filters)
        |> FsTree.applyFilters
      let filteredCount = FsTree.fileCount filtered
      Expect.equal filteredCount (fileCount - filterSize) "Should have correct file count"

  let fsTests =
    testList "FileSystem Tests" [
      test_should_have_correct_base_path
      test_should_handle_base_path_with_slash
      test_fspath_is_sane
      test_modify_should_be_correct
      test_should_add_file_entry_at_correct_point
      test_should_remove_file_entry_at_correct_point
      test_should_update_file_entry_at_correct_point
      test_should_correctly_flatten_and_inflate_tree
      test_should_have_correct_counts
      test_should_apply_filters_on_add
      test_should_track_size_filtered_correctly
      test_map_should_be_correct
      test_apply_filters_should_be_correct
    ]
