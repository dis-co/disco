namespace Iris.Core

//  _____ _ _   _
// | ____(_) |_| |__   ___ _ __
// |  _| | | __| '_ \ / _ \ '__|
// | |___| | |_| | | |  __/ |
// |_____|_|\__|_| |_|\___|_|

type Either<'err,'a> =
  | Right of 'a
  | Left  of 'err


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

  let get = function
    | Right result -> result
    | Left   error -> failwithf "Either: cannot get result from failure: %A" error

  let error = function
    | Left error -> error
    | Right _    -> failwith "Either: cannot get error from regular result"

  let inline bind< ^a, ^b, ^err > (f: ^a -> Either< ^err, ^b >) (a: Either< ^err, ^a >) : Either< ^err, ^b > =
    match a with
    | Right value -> f value
    | Left err    -> Left err

  let inline map< ^a, ^b, ^err > (f: ^a -> ^b) (a: Either< ^err, ^a >) : Either< ^err, ^b > =
    match a with
    | Right value -> f value |> succeed
    | Left  error -> Left error

  let inline mapError< ^a, ^err1, ^err2 > (f: ^err1 -> ^err2) (a: Either< ^err1, ^a >) : Either< ^err2, ^a> =
    match a with
    | Right value -> Right value
    | Left error  -> Left(f error)

  let inline combine< ^a, ^b, ^err > (v1 : ^a) (v2 : Either< ^err, ^b >) : Either< ^err, (^a * ^b) > =
    match v2 with
    | Right value2 -> succeed (v1, value2)
    | Left err     -> Left err

  let inline orExit< ^a, ^b > (f: ^a -> ^b) (a: Either< IrisError, ^a>) : ^b =
    match a with
    | Right value -> f value
    | Left error  -> Error.exitWith error

  let inline ofOption< ^a, ^b, ^err > (err: ^err) (a: ^a option) : Either< ^err, ^a > =
    match a with
    | Some value -> Right value
    | None       -> Left err

[<AutoOpen>]
module EitherUtils =

  type EitherBuilder() =

    member __.Return(v) = Right v

    member __.ReturnFrom(v) = v

    member __.Bind(m, f) =
      match m with
      | Right value -> f value
      | Left error  -> Left error

    member __.Zero() = Right ()

    member __.Delay(f) = fun () -> f()

    member __.Run(f) = f()              // needed for lazyness to work

  let either = new EitherBuilder()
