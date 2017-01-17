namespace Iris.Client

open Iris.Core

// * IrisClient

type IrisClient =
  { Id: Id
    Name: string }

[<RequireQualifiedAccess>]
module Client =

  let create () = failwith "never"
