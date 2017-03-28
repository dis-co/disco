module Iris.Tests.Main

open Expecto
open Iris.Core
open Iris.Tests

let all =
  testList "All tests" [
      apiTests
      // assetTests
      // configTests
      // gitTests
      // irisServiceTests
      // pinTests
      // projectTests
      // raftIntegrationTests
      // raftTests
      // serializationTests
      // stateTests
      // storeTests
      // zmqIntegrationTests
    ]

[<EntryPoint>]
let main _ =
  runTests defaultConfig all
