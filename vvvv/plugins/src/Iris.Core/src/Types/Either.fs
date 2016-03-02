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
        | Success v -> v
        | _ -> failwith "Either: cannot get on a Fail value"

  let isFail = function
    | Fail _ -> true
    |      _ -> false

  let isSuccess = function
    | Success _  -> true
      |       _  -> false

  let bindE (a : Either<'err,'a>) (f : 'a -> Either<'err,'b>) : Either<'err,'b> =
    match a with
      | Success value -> f value
      | Fail err      -> Fail err

  let returnE v : Either<'err,'t> = Success v

  let (>>>) v f = bindE v (fun _ -> f())
  let (>>=) = bindE
