(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests.Raft

open Expecto
open Disco.Core
open Disco.Raft

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
      let mem : RaftMember = Member.create (DiscoId.Create())
      Expect.equal (Member.isVoting mem) true "Should be voting"
