namespace Iris.Tests

open System
open System.IO
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module AssetTests =

  let test_write_read_asset_correctly =
    testCase "should write and read asset correctly" <| fun _ ->
      either {
        let path = tmpPath()
        let payload = string (Id.Create())
        let! info = Asset.write path (Payload payload)
        let! data = Asset.read path
        expect "Payload should be the same" payload id data
      }
      |> noError

  let test_save_load_asset_correctly =
    testCase "should save and load asset correctly" <| fun _ ->
      either {
        let path = tmpPath()
        Directory.CreateDirectory(path </> USER_DIR) |> ignore
        let user = User.Admin
        do! Asset.save path user
        let admin = path </> Asset.path user
        let! reuser = Asset.load admin
        expect "Loaded User should be the same" reuser id user
      }
      |> noError

  let assetTests =
    testList "Asset Tests" [
      test_write_read_asset_correctly
      test_save_load_asset_correctly
    ]
