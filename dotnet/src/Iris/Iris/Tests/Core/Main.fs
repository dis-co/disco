open Fuchu
open Iris.Tests

let all =
  testList "All tests" [
      raftIntegrationTests
      // serializationTests
      // projectTests
    ]

[<EntryPoint>]
let main _ = run all
