namespace Iris.Tests

open System
open System.IO
open System.Threading
open Expecto
open Iris.Core
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces
open System.Net
open FSharpx.Control
open FSharpx.Functional

[<AutoOpen>]
module UtilTests =

  //  _____ _ _      ____            _
  // |  ___(_) | ___/ ___| _   _ ___| |_ ___ _ __ ___
  // | |_  | | |/ _ \___ \| | | / __| __/ _ \ '_ ` _ \
  // |  _| | | |  __/___) | |_| \__ \ ||  __/ | | | | |
  // |_|   |_|_|\___|____/ \__, |___/\__\___|_| |_| |_|
  //                       |___/

  let test_rmdir_should_delete_recursively =
    ftestCase "should delete recursively" <| fun _ ->
      either {
        let dir = Path.GetRandomFileName() |> Directory.CreateDirectory
        let nested = dir.FullName </> Path.GetRandomFileName() |> Directory.CreateDirectory
        File.WriteAllText(nested.FullName </> Path.GetRandomFileName(), "hello")
        do! rmDir dir.FullName
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
