namespace Iris.Core.Types


[<AutoOpen>]
module Context =

  type Context =
    { Project : Project option ref
    }

