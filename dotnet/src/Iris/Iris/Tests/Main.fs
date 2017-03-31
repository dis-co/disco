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

  // Tracing.enable()
  // use lobs = Logger.subscribe Logger.stdout
              // (fun log ->
              //   match log.LogLevel with
              //   | Trace -> log |> string |> printfn "%s"
              //   | _ -> ())

  Thread.CurrentThread.GetApartmentState()
  |> printfn "threading model: %A"

  ThreadPool.GetMinThreads()
  |> printfn "min threads (worker,io): %A"

  runTests defaultConfig all
