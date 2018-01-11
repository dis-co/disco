(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

open System

// * Either Type

//  _____ _ _   _
// | ____(_) |_| |__   ___ _ __
// |  _| | | __| '_ \ / _ \ '__|
// | |___| | |_| | | |  __/ |
// |_____|_|\__|_| |_|\___|_|

type Either<'err,'a> =
  | Right of 'a
  | Left  of 'err

// * Either Module

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Either =

  // ** ofNullable

  // FB types are not modeled with nullables in JS
  #if FABLE_COMPILER
  let ofNullable (v: 'T) (er: string -> 'Err) =
    Right v
  #else
  let ofNullable (v: Nullable<'T>) (er: string -> 'Err) =
    if v.HasValue
    then Right v.Value
    else "Item has no value" |> er |> Left
  #endif

  // ** succeed

  /// ## lift a regular value into Either
  ///
  /// ### Signature:
  /// - v: value to lift into Either
  ///
  /// Returns: Either<^err, ^t>
  let succeed v = Right v

  // ** fail

  /// ## lift an error value into Either
  ///
  /// ### Signature:
  /// - v: error to lift into Either
  ///
  /// Returns: Either<^err, ^t>
  let fail v = Left v

  // ** isFail

  /// ## Check if Either is a failure
  ///
  /// Check passed value of type Either<^err, ^t> for being a failure.
  ///
  /// ### Signature:
  /// - value: value to be checked
  ///
  /// Returns: bool
  let isFail = function
    | Left _ -> true
    |      _ -> false

  // ** isSuccess

  /// ## Check if Either value is a success
  ///
  /// Check the passed value for being a success constructor.g
  ///
  /// ### Signature:
  /// - value: value to be checked
  ///
  /// Returns: bool
  let isSuccess = function
    | Right _ -> true
    |       _ -> false

  // ** get

  /// ## Extract success value from Either wrapper type
  ///
  /// Extracts the result of a computation from the wrapper
  /// type. Crashes hard if the constructor is a Left (failure).
  ///
  /// ### Signature:
  /// - value: Either<^err,^t> to extract result from
  ///
  /// Returns: ^t
  let get = function
    | Right result -> result
    | Left   error ->
      failwithf "Either: cannot get result from failure: %A" error

  // ** error

  /// ## Extract the embedded error value from an Either
  ///
  /// Extracts the embedded error value from the passed Either
  /// wrapper. Crashed hard if the constructor was actually a success.
  ///
  /// ### Signature:
  /// - value: value of type Either<^err,^t> to extract error from
  ///
  /// Returns: ^err
  let error = function
    | Left error -> error
    | Right _    ->
      failwith "Either: cannot get error from regular result"

  // ** iter

  let inline iter< ^a, ^err >(f: ^a -> unit) (a: Either< ^err, ^a >) =
    match a with
    | Right value -> f value
    | Left _ -> ()

  // ** iterError

  let inline iterError< ^a, ^err >(f: ^err -> unit) (a: Either< ^err, ^a >) =
    match a with
    | Left error -> f error
    | Right _ -> ()

  // ** unwrap

  /// Gets the value if it's successful and runs the provided function otherwise
  let inline unwrap< ^a, ^err > (fail: ^err -> ^a) (a: Either< ^err, ^a >) =
    match a with
    | Right value -> value
    | Left err    -> fail err

  // ** bind

  /// ## Bind a function to the result of a computation
  ///
  /// Inspects the passed value `a` and applies the function `f` to
  /// the embedded value, *if* `a` was a `Right` (or success). Errors
  /// are just passed through.
  ///
  /// ### Signature:
  /// - `f`: function to apply to the embedded value of `a`
  /// - `a`: value of type Either<^err, ^t> to apply `f` to
  ///
  /// Returns: Either<^err, ^t>
  let inline bind< ^a, ^b, ^err >
                 (f: ^a -> Either< ^err, ^b >)
                 (a: Either< ^err, ^a >)
                 : Either< ^err, ^b > =
    match a with
    | Right value -> f value
    | Left err    -> Left err

  // ** map

  /// ## Map over an embedded value
  ///
  /// Applies a function `f` to the inner value of `a`, *if* `a`
  /// indeed is a `Right`.
  ///
  /// ### Signature:
  /// - `f`: function to apply to the inner value of `a`
  /// - `a`: value to extract and apply `f` to
  ///
  /// Returns: Either<^err, ^t>
  let inline map< ^a, ^b, ^err >
                (f: ^a -> ^b)
                (a: Either< ^err, ^a >)
                : Either< ^err, ^b > =
    match a with
    | Right value -> f value |> succeed
    | Left  error -> Left error

  let bindArray(f: 'a -> Either<'err,'b>) (arr:'a[]): Either<'err,'b[]> =
    let mutable i = 0
    let mutable error = None
    let arr2 = Array.zeroCreate arr.Length
    while i < arr.Length && Option.isNone error do
      match f arr.[i] with
      | Right value -> arr2.[i] <- value; i <- i + 1
      | Left err -> error <- Some err
    match error with
    | Some err -> Left err
    | None -> Right arr2

  // ** mapError

  /// ## Map over the embedded error value
  ///
  /// Inspects the passed value `a` and applies the function `f`, *if*
  /// `a` is a `Left`.
  ///
  /// ### Signature:
  /// - `f`: function to apply to the inner error value
  /// - `a`: value of type Either<^err,^t>
  ///
  /// Returns: Either<^err, ^t>
  let inline mapError< ^a, ^err1, ^err2 >
                    (f: ^err1 -> ^err2)
                    (a: Either< ^err1, ^a >)
                    : Either< ^err2, ^a> =
    match a with
    | Right value -> Right value
    | Left error  -> Left(f error)

  // ** combine

  let inline combine< ^a, ^b, ^err >
                    (v1 : ^a)
                    (v2 : Either< ^err, ^b >)
                    : Either< ^err, (^a * ^b) > =
    match v2 with
    | Right value2 -> succeed (v1, value2)
    | Left err     -> Left err

  // ** ofOption

  /// ## Transform an Option value into an Either
  ///
  /// Converts the passed value of type `'t option` into an
  /// Either<^err, ^t>. If the passed value is a `None`, use the
  /// provided error value in the `Left`.
  ///
  /// ### Signature:
  /// - err: error value to use when `a` is `None`
  /// - `a`: value to convert
  ///
  /// Returns: Either<^err,^t>
  let inline ofOption< ^a, ^b, ^err >
                     (err: ^err)
                     (a: ^a option)
                     : Either< ^err, ^a > =
    match a with
    | Some value -> Right value
    | None       -> Left err

  // ** nothing

  let inline nothing< ^err > : Either< ^err,unit > =
    succeed ()

  // ** ignore

  let inline ignore< ^err > _ : Either< ^err, unit > =
    succeed ()

  // ** tryWith

  let inline tryWith< ^a, ^err >
                    (err: (string -> ^err))
                    (f: unit -> ^a)
                    : Either< ^err, ^a > =
    try
      f() |> succeed
    with
      | exn ->
        exn.Message
        |> err
        |> fail


  // ** orElse

  let inline orElse value = function
    | Right _ as good -> good
    | Left _ -> Right value

  // ** defaultValue

  let defaultValue def = function
    | Right value -> value
    | Left _ -> def

// * Either Builder

//  _____ _ _   _                 ____        _ _     _
// | ____(_) |_| |__   ___ _ __  | __ ) _   _(_) | __| | ___ _ __
// |  _| | | __| '_ \ / _ \ '__| |  _ \| | | | | |/ _` |/ _ \ '__|
// | |___| | |_| | | |  __/ |    | |_) | |_| | | | (_| |  __/ |
// |_____|_|\__|_| |_|\___|_|    |____/ \__,_|_|_|\__,_|\___|_|

[<AutoOpen>]
module EitherUtils =

  type EitherBuilder() =

    member self.Return(v: 'a): Either<'err, 'a> = Right v

    member self.ReturnFrom(v: Either<'err, 'a>): Either<'err, 'a> = v

    member self.Bind(m: Either<'err, 'a>, f: 'a -> Either<'err, 'b>): Either<'err, 'b> =
      match m with
      | Right value -> f value
      | Left err    -> Left err

    member self.Zero(): Either<'err, unit> = Right ()

    member self.Delay(f: unit -> Either<'err, 'a>) = f

    member self.Run(f: unit -> Either<'err, 'a>) = f()

    member self.While(guard: unit -> bool, body: unit -> Either<'err, unit>): Either<'err, unit> =
      if guard ()
      then self.Bind(body(), fun () -> self.While(guard, body))
      else self.Zero()

    member self.For(sequence:seq<'a>, body: 'a -> Either<'err, unit>): Either<'err, unit> =
      self.Using(sequence.GetEnumerator(), fun enum ->
        self.While(enum.MoveNext, fun () -> body enum.Current))

    member self.Combine(a, b) =
      match a with
      | Right _ -> a
      | Left  _ -> b

    member self.TryWith(body, handler) =
      try body() |> self.ReturnFrom
      with e -> handler e

    member self.TryFinally(body, handler) =
      try
        self.ReturnFrom(body())
      finally
        handler ()

    member self.Using<'a, 'b, 'err when 'a :> IDisposable>
                     (disposable: 'a, body: 'a -> Either<'err, 'b>): Either<'err, 'b> =

      let body' = fun () -> body disposable
      self.TryFinally(body', fun () ->
        disposable.Dispose())

  let either = EitherBuilder()

#if INTERACTIVE
module Test =
  open EitherUtils

  type DisposableAction(f) =
      interface IDisposable with
          member __.Dispose() = f()

  let equal expected actual =
      let areEqual = expected = actual
      printfn "%A = %A > %b" expected actual areEqual
      if not areEqual then
          failwithf "Expected %A but got %A" expected actual

  let orFail x =
    match x with
    | Left err -> printfn "ERROR: %O" err
    | Right v -> printfn "OK: %O" v

  let riskyOp x =
    printfn "Evaluating %O..." x
    if x = 0 then Left "boom!" else Right ()

  let test() =
    let test = either {
      printfn "This should be lazy but it's evaluated eagerly"
      let ar = [|1;2;0;3|]
      let mutable i = 0
      while i < 3 do
        do! riskyOp ar.[i]
        i <- i + 1
    }

    printfn "The either expression is supposed to be evaluated here"
    orFail test

    // No problem here
    either {
      for x in [1;2;3] do
        do! riskyOp x
    } |> orFail

    // Boom!
    either {
      for x in [|1;2;0;3|] do
        do! riskyOp x
    } |> orFail

  let testUse() =
      let isDisposed = ref false
      let step1ok = ref false
      let step2ok = ref false
      let resource = either {
          return new DisposableAction(fun () -> isDisposed := true)
      }
      either {
          use! r = resource
          step1ok := not !isDisposed
      } |> ignore
      step2ok := !isDisposed
      (!step1ok && !step2ok) |> equal true

#endif


// * Option Builder

//   ___        _   _               ____        _ _     _
//  / _ \ _ __ | |_(_) ___  _ __   | __ ) _   _(_) | __| | ___ _ __
// | | | | '_ \| __| |/ _ \| '_ \  |  _ \| | | | | |/ _` |/ _ \ '__|
// | |_| | |_) | |_| | (_) | | | | | |_) | |_| | | | (_| |  __/ |
//  \___/| .__/ \__|_|\___/|_| |_| |____/ \__,_|_|_|\__,_|\___|_|
//       |_|

[<AutoOpen>]
module OptionUtils =

  type MaybeBuilder() =
    member __.Return (v) = Some v

    member __.ReturnFrom (v) = v

    member __.Bind (m, f) = Option.bind f m

    member __.Zero () = Some ()

    member __.Delay (f) = fun () -> f()

    member __.Run (f) = f()

  let maybe = new MaybeBuilder()
