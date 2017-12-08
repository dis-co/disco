namespace Disco.Tests.Raft

open System
open Expecto
open Disco.Raft

[<AutoOpen>]
module Validation =

  /////////////////////////////////////////////////////
  // __     __    _ _     _       _   _              //
  // \ \   / /_ _| (_) __| | __ _| |_(_) ___  _ __   //
  //  \ \ / / _` | | |/ _` |/ _` | __| |/ _ \| '_ \  //
  //   \ V / (_| | | | (_| | (_| | |_| | (_) | | | | //
  //    \_/ \__,_|_|_|\__,_|\__,_|\__|_|\___/|_| |_| //
  /////////////////////////////////////////////////////

  let validation_dsl_validation =
    let run c = runValidation c
    let a n a b = Expect.equal b a (sprintf "%d) should be %b" n a)
    testCase "Continue DSL validation" <| fun _ ->
      a 1 false <| fst (runValidation (validation {
        return! validate (fun x -> (x,0))  false true
        return (true, 0)
        }))

      a 2 true <| fst (runValidation (validation {
        return! validate (fun x -> (x,0)) false false
        return! validate (fun x -> (not x,0)) false true
        return (true,0)
        }))

      a 3 true <| fst (runValidation (validation {
        //               predicate   result  input
        return! validate (fun x -> (x,0))     true    true
        return! validate (fun x -> (x,0))     false   false
        return! validate (fun x -> (x,0))     false   false
        return! validate (fun x -> (x,0))     false   false
        return! validate (fun x -> (not x,0)) false   true
        return (false, 0)
        }))
