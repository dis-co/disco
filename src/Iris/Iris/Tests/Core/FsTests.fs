namespace Iris.Tests

open System
open System.IO
open Expecto
open Expecto.Expect
open Iris.Core

[<AutoOpen>]
module FsTests =

  let withTmpDir (f: string -> unit)  =
    let basePath = Path.GetTempPath()
    Directory.CreateDirectory(basePath) |> ignore
    let path = Path.Combine(basePath, Path.GetRandomFileName())
    Directory.CreateDirectory(path) |> ignore
    f path
    FileSystem.rmDir (filepath path) |> ignore

  let withTree (f: FsTree -> unit) =
    withTmpDir (filepath >> flip FsTree.create Array.empty >> Either.get >> f)

  let test_should_have_correct_base_path =
    testCase "should have correct base path" <| fun _ ->
      withTmpDir <| fun path ->
        let tree = FsTree.create (filepath path) Array.empty |> Either.get
        Expect.equal (FsTree.basePath tree) (filepath path) "Should have correct base path"

  let test_should_handle_base_path_with_slash =
    testCase "should handle base path with slash" <| fun _ ->
      withTmpDir <| fun path ->
        let withSlash = path + "/"
        let tree = FsTree.create (filepath withSlash) Array.empty |> Either.get
        Expect.equal (FsTree.basePath tree) (filepath path) "Should have correct base path"

  let test_should_add_file_entry_at_correct_point =
    testCase "should add file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path </> filepath "dir1"
        let dir2 = path </> filepath "dir2"
        let dir3 = dir2 </> filepath "dir3"

        let file1 = dir1 </> filepath "file1.txt"
        let file2 = dir3 </> filepath "file2.txt"

        do Directory.createDirectory dir1 |> ignore
        do Directory.createDirectory dir2 |> ignore
        do Directory.createDirectory dir3 |> ignore

        do File.writeText "Hello!" None file1
        do File.writeText "Bye!" None file2

        let tree =
          tree
          |> FsTree.add dir1
          |> FsTree.add dir2
          |> FsTree.add dir3
          |> FsTree.add file1
          |> FsTree.add file2

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 2 "Should have 2 files"

        let fileEntry1 = FsTree.tryFind file1 tree |> Option.get
        let fileEntry2 = FsTree.tryFind file2 tree |> Option.get
        let dirEntry1 = FsTree.tryFind dir1 tree |> Option.get
        let dirEntry2 = FsTree.tryFind dir2 tree |> Option.get
        let dirEntry3 = FsTree.tryFind dir3 tree |> Option.get

        Expect.isTrue (FsEntry.isParentOf fileEntry1 dirEntry1) "Should be the parent"
        Expect.isTrue (FsEntry.isParentOf fileEntry2 dirEntry3) "Should be the parent"
        Expect.isTrue (FsEntry.isParentOf dirEntry1 tree.Root) "Should be the parent"
        Expect.isTrue (FsEntry.isParentOf dirEntry2 tree.Root) "Should be the parent"
        Expect.isTrue (FsEntry.isParentOf dirEntry3 dirEntry2) "Should be the parent"

        Expect.equal tree.[file1] fileEntry1 "Should be equal"
        Expect.equal tree.[file2] fileEntry2 "Should be equal"
        Expect.equal tree.[dir1] dirEntry1 "Should be equal"
        Expect.equal tree.[dir2] dirEntry2 "Should be equal"
        Expect.equal tree.[dir3] dirEntry3 "Should be equal"

  let test_should_remove_file_entry_at_correct_point =
    testCase "should remove file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path </> filepath "dir1"
        let dir2 = path </> filepath "dir2"
        let dir3 = dir2 </> filepath "dir3"

        let file1 = dir1 </> filepath "file1.txt"
        let file2 = dir3 </> filepath "file2.txt"

        do Directory.createDirectory dir1 |> ignore
        do Directory.createDirectory dir2 |> ignore
        do Directory.createDirectory dir3 |> ignore

        do File.writeText "Hello!" None file1
        do File.writeText "Bye!" None file2

        let tree =
          tree
          |> FsTree.add dir1
          |> FsTree.add dir2
          |> FsTree.add dir3
          |> FsTree.add file1
          |> FsTree.add file2

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 2 "Should have 2 files"

        let processed1 = FsTree.remove file2 tree

        Expect.isNone (FsTree.tryFind file2 processed1) "File should be gone"
        Expect.equal (FsTree.directoryCount processed1) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount processed1) 1 "Should have 1 file"

        let processed2 = FsTree.remove file1 processed1

        Expect.isNone (FsTree.tryFind file1 processed2) "File should be gone"
        Expect.equal (FsTree.directoryCount processed2) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount processed2) 0 "Should have 0 files"

        let processed3 = FsTree.remove dir1 tree

        Expect.isNone (FsTree.tryFind dir1 processed3) "Dir should be gone"
        Expect.equal (FsTree.directoryCount processed3) 3 "Should be 3 directories"
        Expect.equal (FsTree.fileCount processed3) 1 "Should have 1 file"

        let processed4 = FsTree.remove dir2 processed3

        Expect.isNone (FsTree.tryFind dir2 processed4) "Dir should be gone"
        Expect.equal (FsTree.directoryCount processed4) 1 "Should be 1 directory"
        Expect.equal (FsTree.fileCount processed4) 0 "Should have 0 files"

  let test_should_update_file_entry_at_correct_point =
    testCase "should update file entry at correct points" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path </> filepath "dir1"
        let dir2 = path </> filepath "dir2"
        let dir3 = dir2 </> filepath "dir3"

        let file1 = dir1 </> filepath "file1.txt"
        let file2 = dir3 </> filepath "file2.txt"

        do Directory.createDirectory dir1 |> ignore
        do Directory.createDirectory dir2 |> ignore
        do Directory.createDirectory dir3 |> ignore

        let content1 = "Hello!"
        let content2 = "Bye!"

        do File.writeText content1 None file1
        do File.writeText content2 None file2

        let tree =
          tree
          |> FsTree.add dir1
          |> FsTree.add dir2
          |> FsTree.add dir3
          |> FsTree.add file1
          |> FsTree.add file2

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 2 "Should have 2 files"

        let fileEntry1 = FsTree.tryFind file1 tree |> Option.get
        let fileEntry2 = FsTree.tryFind file2 tree |> Option.get

        do File.writeText content1 None file2

        let tree = FsTree.update file2 tree

        let fileEntry3 = FsTree.tryFind file2 tree |> Option.get

        Expect.equal (FsEntry.size fileEntry3) (FsEntry.size fileEntry1) "Should have same size now"

  let test_should_correctly_flatten_and_inflate_tree =
    testCase "should correctly flatten and inflate tree" <| fun _ ->
      withTree <| fun tree ->
        let path = FsTree.basePath tree
        let dir1 = path </> filepath "dir1"
        let dir2 = path </> filepath "dir2"
        let file1 = dir1 </> filepath "file1.txt"
        let file2 = dir2 </> filepath "file2.txt"
        do Directory.createDirectory dir1 |> ignore
        do Directory.createDirectory dir2 |> ignore
        do File.writeText "Hello!" None file1
        do File.writeText "Bye!"   None file2

        let tree =
          tree
          |> FsTree.add dir1
          |> FsTree.add dir2
          |> FsTree.add file1
          |> FsTree.add file2

        let flattened = FsTree.flatten tree
        let root = FsEntry.setChildren Map.empty tree.Root
        let inflated = FsTree.inflate root flattened Array.empty
        Expect.equal inflated tree "Inflated tree should be equal to original"

  let test_should_have_correct_counts =
    testCase "should have correct counts" <| fun _ ->
      let rnd = System.Random()
      let dirCount = rnd.Next(2,10)
      let fileCount = rnd.Next(3,9000)
      let tree = FsTreeTesting.makeTree dirCount fileCount
      Expect.equal (FsTree.fileCount tree) (fileCount * dirCount)  "Should have correct count"
      Expect.equal (FsTree.directoryCount tree) (dirCount + 1) "Should have correct count"

  let test_should_apply_filters_on_inflate =
    testCase "should apply filters on inflate" <| fun _ ->
      let rnd = System.Random()
      let dirCount = 2 /// rnd.Next(2,10)
      let fileCount = 4 /// rnd.Next(3,9000)
      let tree = FsTreeTesting.makeTree dirCount fileCount
      let flattened = FsTree.flatten tree

      let filter =
        flattened
        |> List.filter FsEntry.isFile
        |> List.map
          (fun file ->
            let name:string = FsEntry.name file |> unwrap
            String.subString (name.Length - 4) 4 name)
        |> Array.ofList
        |> Array.take (fileCount / 2)
        |> Array.distinct

      let matches = List.filter (FsEntry.matches filter) flattened
      let root = FsEntry.setChildren Map.empty tree.Root
      let inflated = FsTree.inflate root flattened filter

      Expect.equal
        (FsTree.fileCount inflated)
        ((dirCount * fileCount) - matches.Length)
        "Should have correct number of files"

  let test_should_apply_filters_on_add =
    testCase "should apply filters on add" <| fun _ ->
      withTree <| fun tree ->
        let tree = FsTree.setFilters [| ".txt" |] tree
        let path = FsTree.basePath tree
        let dir1 = path </> filepath "dir1"
        let dir2 = path </> filepath "dir2"
        let dir3 = dir2 </> filepath "dir3"

        let file1 = dir1 </> filepath "file1.txt"
        let file2 = dir3 </> filepath "file2.txt"

        do Directory.createDirectory dir1 |> ignore
        do Directory.createDirectory dir2 |> ignore
        do Directory.createDirectory dir3 |> ignore

        do File.writeText "Hello!" None file1
        do File.writeText "Bye!" None file2

        let tree =
          tree
          |> FsTree.add dir1
          |> FsTree.add dir2
          |> FsTree.add dir3
          |> FsTree.add file1
          |> FsTree.add file2

        Expect.equal (FsTree.directoryCount tree) 4 "Should be 4 directories"
        Expect.equal (FsTree.fileCount tree) 0 "Should have no files"

  let fsTests =
    ftestList "FileSystem Tests" [
      test_should_have_correct_base_path
      test_should_handle_base_path_with_slash
      test_should_add_file_entry_at_correct_point
      test_should_remove_file_entry_at_correct_point
      test_should_update_file_entry_at_correct_point
      test_should_correctly_flatten_and_inflate_tree
      test_should_have_correct_counts
      test_should_apply_filters_on_inflate
      test_should_apply_filters_on_add
    ]
