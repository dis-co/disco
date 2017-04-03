module Iris.Tests.Main

open System.Threading
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
  Id.Create ()
  |> Logger.initialize

  Thread.CurrentThread.GetApartmentState()
  |> printfn "threading model: %A"

  let threadCount = System.Environment.ProcessorCount * 2
  ThreadPool.SetMinThreads(threadCount,threadCount)
  |> printfn "set min threads %b"

  ThreadPool.GetMinThreads()
  |> printfn "min threads (worker,io): %A"

  runTests defaultConfig all
