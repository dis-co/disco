namespace Disco.Tests

open System
open System.IO
open Expecto
open Disco.Core
open Disco.Raft
open System.Net
open Disco.Core

[<AutoOpen>]
module AssetTests =

  type TestAsset() as data =
    [<DefaultValue>] val mutable Data: string

    do data.Data <- string (DiscoId.Create())

    member self.HasParent with get () = false

    member self.AssetPath
      with get () = filepath "test-asset.txt"

    member self.ToYaml() = self

    member self.Save(basePath: FilePath) =
      either {
        let path = basePath </> Asset.path self
        let data = Yaml.encode self
        let! info = DiscoData.write path (Payload data)
        return ()
      }

    static member Load(path: FilePath) : Either<DiscoError, TestAsset> =
      either {
        let! data = DiscoData.read path
        return Yaml.deserialize<TestAsset> data
      }

  let test_write_read_asset_correctly =
    testCase "should write and read asset correctly" <| fun _ ->
      either {
        let path = tmpPath()
        let payload = string (DiscoId.Create())
        let! info = DiscoData.write path (Payload payload)
        let! data = DiscoData.read path
        expect "Payload should be the same" payload id data
      }
      |> noError

  let test_save_load_asset_correctly =
    testCase "should save and load asset correctly" <| fun _ ->
      either {
        let path = tmpPath()
        let asset = TestAsset()
        do! Asset.save path asset
        let path = path </> Asset.path asset
        let! (reasset: TestAsset) = Asset.load path
        expect "Loaded asset should be the same" reasset.Data id asset.Data
      }
      |> noError

  let test_save_with_commit_adds_and_commits_an_asset =
    testCase "should save an asset with commit even if its new" <| fun _ ->
      either {
        let path = tmpPath()
        let! repo = Git.Repo.init path
        let signature = User.Admin.Signature
        let asset = TestAsset()
        let! commit = DiscoData.saveWithCommit path signature asset
        let path = path </> Asset.path asset
        let! (reasset: TestAsset) = Asset.load path
        expect "Loaded asset should be the same" reasset.Data id asset.Data
      }
      |> noError

  let assetTests =
    testList "Asset Tests" [
      test_write_read_asset_correctly
      test_save_load_asset_correctly
      test_save_with_commit_adds_and_commits_an_asset
    ] |> testSequenced
