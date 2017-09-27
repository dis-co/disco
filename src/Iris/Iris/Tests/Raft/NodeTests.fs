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

  let mem_init_test =
    testCase "When created, Mem should be in Voting state" <| fun _ ->
      let mem : RaftMember = Member.create (IrisId.Create())
      Expect.equal (Member.isVoting mem) true "Should be voting"
