module Iris.Tests.Main

open Expecto
open Iris.Core
open Iris.Tests

let all =
  testList "All tests" [
    // raftTests
    // zmqIntegrationTests
    // raftIntegrationTests
    // serializationTests
    // projectTests
    // storeTests
    gitTests
  ]

[<EntryPoint>]
let main _ =
  run defaultConfig.printer all
