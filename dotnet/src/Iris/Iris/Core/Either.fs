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

  /// ## lift a regular value into Either
  ///
  /// ### Signature:
  /// - v: value to lift into Either
  ///
  /// Returns: Either<^err, ^t>
  let succeed v = Right v

  /// ## lift an error value into Either
  ///
  /// ### Signature:
  /// - v: error to lift into Either
  ///
  /// Returns: Either<^err, ^t>
  let fail v = Left v

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

  let inline combine< ^a, ^b, ^err >
                    (v1 : ^a)
                    (v2 : Either< ^err, ^b >)
                    : Either< ^err, (^a * ^b) > =
    match v2 with
    | Right value2 -> succeed (v1, value2)
    | Left err     -> Left err

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

  let inline tryWith< ^a, ^err >
                    (err: (string -> ^err))
                    (loc: string)
                    (f: unit -> ^a)
                    : Either< ^err, ^a > =
    try
      f() |> succeed
    with
      | exn ->
        sprintf "Could not parse %s: %s" loc exn.Message
        |> err
        |> fail

//  _____ _ _   _                 ____        _ _     _
// | ____(_) |_| |__   ___ _ __  | __ ) _   _(_) | __| | ___ _ __
// |  _| | | __| '_ \ / _ \ '__| |  _ \| | | | | |/ _` |/ _ \ '__|
// | |___| | |_| | | |  __/ |    | |_) | |_| | | | (_| |  __/ |
// |_____|_|\__|_| |_|\___|_|    |____/ \__,_|_|_|\__,_|\___|_|

[<AutoOpen>]
module EitherUtils =

  type EitherBuilder() =

    member self.Return(v) = Right v

    member self.ReturnFrom(v) = v

    member inline self.Bind(m, f) = Either.bind f m

    member self.Zero() = Right ()

    member self.Delay(f) = fun () -> f()

    member self.Run(f) = f()              // needed for lazyness to work

    member self.While(guard, body) =
      if guard () then
        let cont () =
          self.While(guard, body)
        self.Bind(body(), cont)
      else
        self.Zero()

    member self.Combine(a, b) =
      match a with
      | Right _ -> a
      | Left  _ -> b

    member self.TryWith(body, handler) =
      try body() |> self.ReturnFrom
      with e -> handler e


  let either = new EitherBuilder()

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
