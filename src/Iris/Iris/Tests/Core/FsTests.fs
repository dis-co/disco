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

        printfn "tree: \n%O" tree

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

  let fsTests =
    ftestList "FileSystem Tests" [
      test_should_have_correct_base_path
      test_should_handle_base_path_with_slash
      test_should_add_file_entry_at_correct_point
    ]

#if INTERACTIVE

open System
open System.IO



#endif
