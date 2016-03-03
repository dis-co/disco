module Iris.Nodes.GraphApi

type GraphApi =
  member self.Evaluate (spreadMax : int) =
    printfn "current spread count is: %d" spreadMax
