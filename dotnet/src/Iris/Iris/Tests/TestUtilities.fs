namespace Iris.Tests

open Fuchu
open Fuchu.Test

[<AutoOpen>]
module TestUtilities =

  /// abstract over Assert.Equal to create pipe-lineable assertions
  let expect (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Assert.Equal(msg, a, b t) // apply t to b

  let assume (msg : string) (a : 'a) (b : 't -> 'a) (t : 't) =
    Assert.Equal(msg, a, b t) // apply t to b
    t

  let pending (msg: string) =
    testCase msg <| fun _ -> skiptest "NOT YET IMPLEMENTED"
