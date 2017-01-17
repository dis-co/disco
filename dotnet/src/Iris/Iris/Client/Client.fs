namespace Iris.Client

open Iris.Core

type IrisClient =
  { Id: Id
    Name: string }

[<RequireQualifiedAccess>]
module Client =

  let create () = failwith "never"
