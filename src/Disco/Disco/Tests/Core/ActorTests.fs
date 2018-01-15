(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System
open System.Threading
open System.Collections.Concurrent
open Expecto
open FsCheck
open Disco.Core
open Disco.Raft
open Disco.Service
open Disco.Net
open Microsoft.FSharp.Control

[<AutoOpen>]
module ActorTests =

  let test_actor_properly =
    testCase "actor disposes properly" <| fun _ ->
      either {
        let actor = Actor.create "test" <| fun actor -> function
          | "done" -> async { dispose actor }
          | msg -> async { ignore msg }
        do actor.Start()
        do ignore [ for n in 0 .. 9 -> actor.Post (string n) ]
        do actor.Post "done"
      }
      |> noError

  let actorTests =
    testList "Actor Tests" [
      test_actor_properly
    ]
