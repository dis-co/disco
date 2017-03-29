module Iris.Tests.Main

open Expecto
open Iris.Core
open Iris.Tests

let all =
  testList "All tests" [
      pinTests
      assetTests
      stateTests
      irisServiceTests
      raftTests
      configTests
      zmqIntegrationTests
      raftIntegrationTests
      serializationTests
      projectTests
      storeTests
      gitTests
      apiTests
    ]

[<EntryPoint>]
let main _ =
  runTests defaultConfig all
