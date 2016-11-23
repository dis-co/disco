namespace Iris.Tests

open System
open System.Threading
open System.Text
open Expecto

open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Raft
open Iris.Service
open FSharpx.Functional
open Microsoft.FSharp.Control
open ZeroMQ

[<AutoOpen>]
module ZmqIntegrationTests =

  //  _____                  _____         _
  // |__  /_ __ ___   __ _  |_   _|__  ___| |_ ___
  //   / /| '_ ` _ \ / _` |   | |/ _ \/ __| __/ __|
  //  / /_| | | | | | (_| |   | |  __/\__ \ |_\__ \
  // /____|_| |_| |_|\__, |   |_|\___||___/\__|___/
  //                    |_|

  let test_proper_cleanup_of_request_sockets =
    testCase "validate Req sockets are cleaned up properly" <| fun _ ->
      let srv = "tcp://127.0.0.1:8989"

      let n = 12
      let msgs = [ "hi"; "yep"; "bye" ]
      let count = ref 0

      let handler (msg: byte array) =
        lock count <| fun _ ->
          let next = !count + 1
          count := next
        msg

      use rep = new Zmq.Rep(srv, handler)
      rep.Start()

      let socks =
        [ for _ in 0 .. (n - 1) do
            let sock = new Zmq.Req(Id.Create(), srv, 50)
            sock.Start()
            yield sock ]

      let request (str: string) (sck: Zmq.Req) =
        async {
          let result = str |> Encoding.UTF8.GetBytes |> sck.Request
          return result
        }

      msgs
      |> List.fold (fun lst str ->
                   List.fold
                     (fun inner sock -> request str sock :: inner)
                     lst
                     socks)
                  []
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Array.iter (expect "Should be a success" true Either.isSuccess)

      expect "Should have correct number of requests" (n * List.length msgs) id !count

      List.iter dispose socks

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let zmqIntegrationTests =
    testList "Zmq Integration Tests" [
      test_proper_cleanup_of_request_sockets
    ]
