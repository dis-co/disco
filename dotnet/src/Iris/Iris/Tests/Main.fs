open Fuchu
open Iris.Tests

let all =
  testList "All tests" [
    raftTests
    raftIntegrationTests
    serializationTests
    projectTests
    storeTests
    gitTests
  ]

[<EntryPoint>]
let main _ = run all
