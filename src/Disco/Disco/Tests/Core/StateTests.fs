namespace Disco.Tests

open Expecto
open Disco.Core
open Disco.Raft

[<AutoOpen>]
module StateTests =

  let test_apply_fstree_add_correctly =
    testCase "should apply fstree add correctly" <| fun _ ->
      either {
        let initial = State.Empty
        let tree = FsTreeTesting.deepTree 2
        let state = State.addFsTree tree initial
        Expect.equal state.FsTrees.[tree.HostId] tree "Should have tree"
      }
      |> noError

  let test_apply_fsentry_add_correctly =
    testCase "should apply fsentry add correctly" <| fun _ ->
      either {
        let tree = FsTreeTesting.deepTree 2
        let initial = State.addFsTree tree State.Empty
        let directory =
          tree
          |> FsTree.directories
          |> FsEntry.flatten
          |> List.last
          |> FsEntry.path
        let entry =
          let path = directory + Path.getRandomFileName()
          FsEntry.File(
            { Path = path
              Name = FsPath.fileName path
              Size = 234u
              Filtered = 0u
              MimeType = "text/plain" })
        let state = State.addFsEntry tree.HostId entry initial
        let actual = FsTree.tryFind (FsEntry.path entry) state.FsTrees.[tree.HostId]
        Expect.equal actual (Some entry) "Should have entry"
      }
      |> noError

  let test_apply_fsentry_remove_correctly =
    testCase "should apply fsentry remove correctly" <| fun _ ->
      either {
        let tree = FsTreeTesting.deepTree 2
        let initial = State.addFsTree tree State.Empty
        let entry =
          tree
          |> FsTree.files
          |> List.last
          |> FsEntry.path
        let state = State.removeFsEntry tree.HostId entry initial
        let actual = FsTree.tryFind entry state.FsTrees.[tree.HostId]
        Expect.equal actual None "Should not have entry"
      }
      |> noError

  let stateTests =
    testList "State Tests" [
      test_apply_fstree_add_correctly
      test_apply_fsentry_add_correctly
      test_apply_fsentry_remove_correctly
    ]
