namespace Iris.Tests.Raft

open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Raft

[<AutoOpen>]
module NodeTests =
  ////////////////////////////////////////
  //  _   _           _                 //
  // | \ | | ___   __| | ___            //
  // |  \| |/ _ \ / _` |/ _ \           //
  // | |\  | (_) | (_| |  __/           //
  // |_| \_|\___/ \__,_|\___|           //
  ////////////////////////////////////////

  let node_init_test =
    testCase "When created, Node should be in Voting state" <| fun _ ->
      let node : RaftNode = Node.create (Id.Create())
      Assert.Equal("Should be voting", true, Node.isVoting node)
