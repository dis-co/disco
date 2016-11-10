namespace Iris.Tests.Raft

open Expecto
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
      Expect.equal (Node.isVoting node) true "Should be voting"
