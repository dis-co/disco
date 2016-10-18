namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Raft
open Iris.Serialization.Raft
open System.Net
open FlatBuffers
open FSharpx.Functional
open LibGit2Sharp
open System.IO

[<AutoOpen>]
module GitTests =

  let testRepo () =
    let fn =
      Path.GetTempFileName()
      |> Path.GetFileName

    Directory.GetCurrentDirectory() </> "tmp" </> fn
    |> Directory.CreateDirectory
    |> fun info -> info.FullName
    |> fun path ->
      Repository.Init path |> ignore
      new Repository(path)

  let test_correct_remote_list =
    testCase "Validate Correct Remote Listing" <| fun _ ->
      let repo = testRepo()
      let remotes = Git.Config.remotes repo

      expect "Should be empty" Map.empty id remotes

      let upstream = "git@bla.com"
      Git.Config.addRemote repo "origin" upstream

      let remotes = Git.Config.remotes repo
      expect "Should be correct upstream" upstream (Map.find "origin") remotes

  let test_remove_remote =
    testCase "Validate Removal Of Remote" <| fun _ ->
      let repo = testRepo()
      let remotes = Git.Config.remotes repo

      let name = "origin"
      let upstream = "git@bla.com"
      Git.Config.addRemote repo name upstream

      let remotes = Git.Config.remotes repo
      expect "Should be correct upstream" upstream (Map.find name) remotes

      Git.Config.delRemote repo name

      let remotes = Git.Config.remotes repo
      expect "Should be empty" Map.empty id remotes

  let gitTests =
    testList "Git Tests" [
      test_correct_remote_list
      test_remove_remote
    ]
