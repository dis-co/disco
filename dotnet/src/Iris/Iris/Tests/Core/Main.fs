open Fuchu
open Iris.Tests

let all =
  testList "All tests" [
      projectTests
      serializationTests
    ]

[<EntryPoint>]
let main _ = run all
