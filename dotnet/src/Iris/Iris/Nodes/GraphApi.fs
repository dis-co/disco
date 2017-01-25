namespace Iris.Nodes

type GraphApi =

  member self.Evaluate (spreadMax : int) =
    printfn "current spread count is: %d" spreadMax
