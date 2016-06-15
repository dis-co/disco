namespace Iris.Core.Types

[<AutoOpen>]
module Either =

  //  _____ _ _   _
  // | ____(_) |_| |__   ___ _ __
  // |  _| | | __| '_ \ / _ \ '__|
  // | |___| | |_| | | |  __/ |
  // |_____|_|\__|_| |_|\___|_|
  type Either<'err,'a> =
    | Success of 'a
    | Fail    of 'err

    static member get(v : Either<'err,'a>) : 'a =
      match v with
        | Success v1 -> v1
        | _ -> failwith "Either: cannot get on a Fail value"

    static member error(v : Either<'err,'a>) : 'err =
      match v with
        | Fail v1 -> v1
        | _ -> failwith "Either: cannot get error from Success Value"
      
    //   __                  _
    //  / _|_   _ _ __   ___| |_ ___  _ __
    // | |_| | | | '_ \ / __| __/ _ \| '__|
    // |  _| |_| | | | | (__| || (_) | |
    // |_|  \__,_|_| |_|\___|\__\___/|_|
    static member map(f : 'a -> Either<'err,'b>) (v : Either<'err,'a>) : Either<'err, 'b> =
      match v with
        | Success value -> f value
        | Fail err -> Fail err

  let isFail = function
    | Fail _ -> true
    |      _ -> false

  let isSuccess = function
    | Success _  -> true
    |         _  -> false

  let bindE (a : Either<'err,'a>) (f : 'a -> Either<'err,'b>) : Either<'err,'b> =
    match a with
      | Success value -> f value
      | Fail err      -> Fail err

  let succeed v : Either<'err,'t> = Success v
  let fail v : Either<'err,'t> = Fail v

  let (>>>) v f = bindE v (fun _ -> f())
  let (>>=) = bindE

  let combine (v1 : 'a) (v2 : Either<'err,'b>) : Either<'err,('a * 'b)> =
    match v2 with
      | Success value2 -> succeed (v1, value2)
      | Fail err -> Fail err
