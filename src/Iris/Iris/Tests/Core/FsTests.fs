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
    withTmpDir (filepath >> FsTree.create >> f)

  let test_should_have_correct_base_path =
    testCase "should have correct base path" <| fun _ ->
      withTmpDir <| fun path ->
        let tree = FsTree.create (filepath path)
        Expect.equal (FsTree.basePath tree) (filepath path) "Should have correct base path"

  let test_should_handle_base_path_with_slash =
    testCase "should handle base path with slash" <| fun _ ->
      withTmpDir <| fun path ->
        let withSlash = path + "/"
        let tree = FsTree.create (filepath withSlash)
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

  let fsTests =
    ftestList "FileSystem Tests" [
      test_should_have_correct_base_path
      test_should_handle_base_path_with_slash
      test_should_add_file_entry_at_correct_point
      test_should_remove_file_entry_at_correct_point
    ]

#if INTERACTIVE

open System
open System.IO



#endif
