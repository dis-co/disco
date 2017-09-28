namespace Iris.Tests

open System.IO
open Expecto
open Iris.Core

[<AutoOpen>]
module UtilTests =

  //  _____ _ _      ____            _
  // |  ___(_) | ___/ ___| _   _ ___| |_ ___ _ __ ___
  // | |_  | | |/ _ \___ \| | | / __| __/ _ \ '_ ` _ \
  // |  _| | | |  __/___) | |_| \__ \ ||  __/ | | | | |
  // |_|   |_|_|\___|____/ \__, |___/\__\___|_| |_| |_|
  //                       |___/

  let test_rmdir_should_delete_recursively =
    testCase "rmdir should delete recursively with read-only items" <| fun _ ->
      either {
        let! dir =
          Path.getRandomFileName()
          |> Directory.createDirectory

        let! nested =
          dir.FullName <.> Path.GetRandomFileName()
          |> Directory.createDirectory

        let fn = nested.FullName <.> Path.GetRandomFileName()
        File.writeText "hello" None fn

        let info = File.info fn
        info.IsReadOnly <- true

        do! rmDir (filepath dir.FullName)

        expect "Should be gone" false Directory.Exists dir.FullName
      }
      |> noError

  //     _    _ _
  //    / \  | | |
  //   / _ \ | | |
  //  / ___ \| | |
  // /_/   \_\_|_|

  let utilTests =
    testList "Util Tests" [
      test_rmdir_should_delete_recursively
    ]
