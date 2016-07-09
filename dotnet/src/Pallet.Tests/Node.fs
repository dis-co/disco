namespace Pallet.Tests

open System
open System.Net
open Fuchu
open Fuchu.Test
open Pallet.Core

[<AutoOpen>]
module Node =
  ////////////////////////////////////////
  //  _   _           _                 //
  // | \ | | ___   __| | ___            //
  // |  \| |/ _ \ / _` |/ _ \           //
  // | |\  | (_) | (_| |  __/           //
  // |_| \_|\___/ \__,_|\___|           //
  ////////////////////////////////////////

  let node_init_test =
    testCase "When created, Node should be in Voting state" <| fun _ ->
      let node : Node<unit> = Node.create (RaftId.Create()) ()
      Assert.Equal("Should be voting", true, Node.isVoting node)
