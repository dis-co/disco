namespace Iris.Nodes

[<RequireQualifiedAccess>]
module Util =

  let inline isNull (o: 't) =
    obj.ReferenceEquals(o, null)
