namespace Iris.Core

//  _____ _ _   _
// | ____(_) |_| |__   ___ _ __
// |  _| | | __| '_ \ / _ \ '__|
// | |___| | |_| | | |  __/ |
// |_____|_|\__|_| |_|\___|_|

type Either<'err,'a> =
  | Right of 'a
  | Left  of 'err

  static member Get(v : Either<'err,'a>) : 'a =
    match v with
      | Right v1 -> v1
      | _ -> failwith "Either: cannot get on a Left value"

  static member Error(v : Either<'err,'a>) : 'err =
    match v with
      | Left v1 -> v1
      | _ -> failwith "Either: cannot get error from Right Value"

  static member Map(f : 'a -> Either<'err,'b>) (v : Either<'err,'a>) : Either<'err, 'b> =
    match v with
      | Right value -> f value
      | Left err -> Left err


[<RequireQualifiedAccess>]
module Either =

  let succeed v = Right v
  let fail v = Left v

  let isFail = function
    | Left _ -> true
    |      _ -> false

  let isSuccess = function
    | Right _ -> true
    |       _ -> false

  let inline bind< ^a, ^b, ^err > (f: ^a -> Either< ^err, ^b >) (a: Either< ^err, ^a >) : Either< ^err, ^b > =
    match a with
    | Right value -> f value
    | Left err    -> Left err

  let inline map< ^a, ^b, ^err > (f: ^a -> ^b) (a: Either< ^err, ^a >) : Either< ^err, ^b > =
    match a with
    | Right value -> f value |> succeed
    | Left  error -> Left error

  let inline combine< ^a, ^b, ^err > (v1 : ^a) (v2 : Either< ^err, ^b >) : Either< ^err, (^a * ^b) > =
    match v2 with
    | Right value2 -> succeed (v1, value2)
    | Left err     -> Left err
