module Disco.Tests.Main

open System
open System.Threading
open Expecto
open Expecto.Impl
open Disco.Core
open Disco.Tests

let setupVM () =
  Thread.CurrentThread.GetApartmentState()
  |> printfn "threading model: %A"

  let threadCount = System.Environment.ProcessorCount * 8
  ThreadPool.SetMinThreads(threadCount,threadCount)
  |> printfn "set min threads %b"

  ThreadPool.GetMinThreads()
  |> printfn "min threads (worker,io): %A"

let expectoConfig =
  { defaultConfig with
      printer =
        { TestPrinters.defaultPrinter with
            passed = fun name ts -> async {
                Console.white      "{0}"     "["
                Console.green      "{0}"     "OK"
                Console.white      "{0}"     "]"
                Console.white      "{0}"     " "
                Console.white      "{0}"     name
                Console.white      "{0}"     " "
                Console.darkYellow "({0}ms)" ts.Milliseconds
                Console.Write System.Environment.NewLine
              }

            failed = fun name msg ts -> async {
                Console.white      "{0}"     "["
                Console.red        "{0}"     "ERROR"
                Console.white      "{0}"     "]"
                Console.white      "{0}"     " "
                Console.red        "{0}"     name
                Console.white      "{0}"     " "
                Console.white      "{0}"     msg
                Console.white      "{0}"     " "
                Console.darkYellow "({0}ms)" ts.Milliseconds
                Console.Write System.Environment.NewLine
              }
          }
      }

let parallelTests =
  testList "parallel tests" [
      utilTests
      pinTests
      stateTests
      serializationTests
      storeTests
      persistenceTests
      fsTests
      assetServiceTests
    ]

let serialTests =
  testList "serial tests" [
      gitTests
      raftTests
      apiTests
      assetTests
      configTests
      projectTests
      netIntegrationTests
      raftIntegrationTests
      discoServiceTests
    ] |> testSequenced

let all =
  testList "all" [
      syncTests
      parallelTests
      serialTests
    ]

[<EntryPoint>]
let main _ =
  // Tracing.enable()
  use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)
  do Logger.initialize LoggingSettings.defaultSettings
  do setupVM()

  runTests expectoConfig all
