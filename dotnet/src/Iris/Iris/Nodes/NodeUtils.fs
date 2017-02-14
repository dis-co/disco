namespace Iris.Nodes

[<RequireQualifiedAccess>]
module Util =

  let inline isNullReference (o: 't) =
    obj.ReferenceEquals(o, null)
