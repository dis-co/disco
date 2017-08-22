module Iris.Tests.Main

open System.Threading
open Expecto
open Iris.Core
open Iris.Tests

let parallelTests =
  testList "parallel tests" [
      utilTests
      pinTests
      stateTests
      raftTests
      serializationTests
      storeTests
      persistenceTests
    ]

let serialTests =
  testList "serial tests" [
      gitTests
      apiTests
      assetTests
      configTests
      projectTests
      netIntegrationTests
      raftIntegrationTests
      irisServiceTests
    ] |> testSequenced

let all =
  testList "all" [
      parallelTests
      serialTests
    ]

[<EntryPoint>]
let main _ =
  // Tracing.enable()
  use lobs = Logger.subscribe Logger.stdout // (Logger.filter Trace Logger.stdout)

  Logger.initialize LoggingSettings.defaultSettings

  Thread.CurrentThread.GetApartmentState()
  |> printfn "threading model: %A"

  let threadCount = System.Environment.ProcessorCount * 8
  ThreadPool.SetMinThreads(threadCount,threadCount)
  |> printfn "set min threads %b"

  ThreadPool.GetMinThreads()
  |> printfn "min threads (worker,io): %A"

  runTests defaultConfig all
