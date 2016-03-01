namespace Iris.Core.Types

[<AutoOpen>]
module Either =
  //  _____ _ _   _
  // | ____(_) |_| |__   ___ _ __
  // |  _| | | __| '_ \ / _ \ '__|
  // | |___| | |_| | | |  __/ |
  // |_____|_|\__|_| |_|\___|_|
  type Either<'err,'a> =
    | Left  of 'err
    | Right of 'a

  let isLeft = function
    | Left _ -> true
    |      _ -> false

  let isRight = function
    | Right _  -> true
    |       _  -> false
