(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Core

open System

// * Result Module

[<RequireQualifiedAccess>]
module Result =

  // ** ofNullable

  // FB types are not modeled with nullables in JS
  #if FABLE_COMPILER
  let ofNullable (v: 'T) (er: string -> 'Err) =
    Ok v
  #else
  let ofNullable (v: Nullable<'T>) (er: string -> 'Err) =
    if v.HasValue
    then Ok v.Value
    else "Item has no value" |> er |> Error
  #endif

  // ** succeed

  /// ## lift a regular value into Result
  ///
  /// ### Signature:
  /// - v: value to lift into Result
  ///
  /// Returns: Result<^err, ^t>
  let succeed v = Ok v

  // ** fail

  /// ## lift an error value into Result
  ///
  /// ### Signature:
  /// - v: error to lift into Result
  ///
  /// Returns: Result<^err, ^t>
  let fail v = Error v

  // ** isFail

  /// ## Check if Result is a failure
  ///
  /// Check passed value of type Result<^err, ^t> for being a failure.
  ///
  /// ### Signature:
  /// - value: value to be checked
  ///
  /// Returns: bool
  let isFail = function
    | Error _ -> true
    |      _ -> false

  // ** isSuccess

  /// ## Check if Result value is a success
  ///
  /// Check the passed value for being a success constructor.g
  ///
  /// ### Signature:
  /// - value: value to be checked
  ///
  /// Returns: bool
  let isSuccess = function
    | Ok _ -> true
    |       _ -> false

  // ** get

  /// ## Extract success value from Result wrapper type
  ///
  /// Extracts the result of a computation from the wrapper
  /// type. Crashes hard if the constructor is a Error (failure).
  ///
  /// ### Signature:
  /// - value: Result<^err,^t> to extract result from
  ///
  /// Returns: ^t
  let get = function
    | Ok result -> result
    | Error   error ->
      failwithf "Result: cannot get result from failure: %A" error

  // ** error

  /// ## Extract the embedded error value from an Result
  ///
  /// Extracts the embedded error value from the passed Result
  /// wrapper. Crashed hard if the constructor was actually a success.
  ///
  /// ### Signature:
  /// - value: value of type Result<^err,^t> to extract error from
  ///
  /// Returns: ^err
  let error = function
    | Error error -> error
    | Ok _    ->
      failwith "Result: cannot get error from regular result"

  // ** iter

  let inline iter< ^a, ^err >(f: ^a -> unit) (a: Result< ^a,^err >) =
    match a with
    | Ok value -> f value
    | Error _ -> ()

  // ** iterError

  let inline iterError< ^a, ^err >(f: ^err -> unit) (a: Result< ^a, ^err >) =
    match a with
    | Error error -> f error
    | Ok _ -> ()

  // ** bindArray

  let bindArray(f: 'a -> Result<'b,'err>) (arr:'a[]): Result<'b[],'err> =
    let mutable i = 0
    let mutable error = None
    let arr2 = Array.zeroCreate arr.Length
    while i < arr.Length && Option.isNone error do
      match f arr.[i] with
      | Ok value -> arr2.[i] <- value; i <- i + 1
      | Error err -> error <- Some err
    match error with
    | Some err -> Error err
    | None -> Ok arr2

  // ** unwrap

  /// Gets the value if it's successful and runs the provided function otherwise
  let inline unwrap< ^a, ^err > (fail: ^err -> ^a) (a: Result< ^a,^err >) =
    match a with
    | Ok value -> value
    | Error err    -> fail err

  // ** combine

  let inline combine< ^a, ^b, ^err >
                    (v1 : ^a)
                    (v2 : Result< ^b,^err >)
                    : Result< (^a * ^b),^err > =
    match v2 with
    | Ok value2 -> succeed (v1, value2)
    | Error err     -> Error err

  // ** ofOption

  /// ## Transform an Option value into a Result
  ///
  /// Converts the passed value of type `'t option` into an
  /// Result<^t,^err>. If the passed value is a `None`, use the
  /// provided error value in the `Error`.
  ///
  /// ### Signature:
  /// - err: error value to use when `a` is `None`
  /// - `a`: value to convert
  ///
  /// Returns: Result<^t,^err>
  let inline ofOption< ^a, ^b, ^err >
                     (err: ^err)
                     (a: ^a option)
                     : Result< ^a,^err > =
    match a with
    | Some value -> Ok value
    | None       -> Error err

  // ** nothing

  let inline nothing< ^err > : Result<unit,^err> =
    succeed ()

  // ** ignore

  let inline ignore< ^err > _ : Result<unit, ^err> =
    succeed ()

  // ** tryWith

  let inline tryWith< ^a, ^err >
                    (err: (string -> ^err))
                    (f: unit -> ^a)
                    : Result< ^a, ^err > =
    try
      f() |> succeed
    with
      | exn ->
        exn.Message
        |> err
        |> fail


  // ** orElse

  let inline orElse value = function
    | Ok _ as good -> good
    | Error _ -> Ok value

  // ** defaultValue

  let defaultValue def = function
    | Ok value -> value
    | Error _ -> def

// * Result Builder

[<AutoOpen>]
module ResultUtils =

  type ResultBuilder() =

    member self.Return(v: 'a): Result<'a,'err> = Ok v

    member self.ReturnFrom(v: Result<'a,'err>): Result<'a,'err> = v

    member self.Bind(m: Result<'a,'err>, f: 'a -> Result<'b,'err>): Result<'b,'err> =
      Result.bind f m

    member self.Zero(): Result<unit,'err> = Ok ()

    member self.Delay(f: unit -> Result<'a,'err>) = f

    member self.Run(f: unit -> Result<'a,'err>) = f()

    member self.While(guard: unit -> bool, body: unit -> Result<unit,'err>): Result<unit,'err> =
      if guard ()
      then self.Bind(body(), fun () -> self.While(guard, body))
      else self.Zero()

    member self.For(sequence:seq<'a>, body: 'a -> Result<unit,'err>): Result<unit,'err> =
      self.Using(sequence.GetEnumerator(), fun enum ->
        self.While(enum.MoveNext, fun () -> body enum.Current))

    member self.Combine(a, b) =
      match a with
      | Ok _ -> a
      | Error  _ -> b

    member self.TryWith(body, handler) =
      try body() |> self.ReturnFrom
      with e -> handler e

    member self.TryFinally(body, handler) =
      try
        self.ReturnFrom(body())
      finally
        handler ()

    member self.Using<'a, 'b, 'err when 'a :> IDisposable>
                     (disposable: 'a, body: 'a -> Result<'b,'err>): Result<'b,'err> =

      let body' = fun () -> body disposable
      self.TryFinally(body', fun () ->
        disposable.Dispose())

  let result = ResultBuilder()

#if INTERACTIVE
module Test =
  open ResultUtils

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
    | Error err -> printfn "ERROR: %O" err
    | Ok v -> printfn "OK: %O" v

  let riskyOp x =
    printfn "Evaluating %O..." x
    if x = 0 then Error "boom!" else Ok ()

  let test() =
    let test = result {
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
    result {
      for x in [1;2;3] do
        do! riskyOp x
    } |> orFail

    // Boom!
    result {
      for x in [|1;2;0;3|] do
        do! riskyOp x
    } |> orFail

  let testUse() =
      let isDisposed = ref false
      let step1ok = ref false
      let step2ok = ref false
      let resource = result {
          return new DisposableAction(fun () -> isDisposed := true)
      }
      result {
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

  let maybe = MaybeBuilder()
