open Fuchu
open Iris.Tests

let all =
  testList "All tests" [
    raftTests
    raftIntegrationTests
    serializationTests
    projectTests
    storeTests
  ]

[<EntryPoint>]
let main _ = run all
