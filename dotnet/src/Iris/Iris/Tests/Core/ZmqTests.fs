namespace Iris.Tests

open System
open System.Threading
open System.Text
open Expecto

open Iris.Core
open Iris.Service
open Iris.Service.Utilities
open Iris.Service.Persistence
open Iris.Zmq
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
      either {
        let srv = "tcp://127.0.0.1:8989"

        let n = 12
        let msgs = [ "hi"; "yep"; "bye" ]
        let count = ref 0

        let handler (msg: byte array) =
          lock count <| fun _ ->
            let next = !count + 1
            count := next
          msg

        use rep = new Rep(Id.Create(), srv, handler)

        do! rep.Start()

        let socks =
          [ for _ in 0 .. (n - 1) do
              let sock = new Req(Id.Create(), srv, Constants.REQ_TIMEOUT)
              sock.Start()
              yield sock ]

        let request (str: string) (sck: Req) =
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
      }
      |> noError

  let test_broker_request_handling =
    testCase "broker request handling" <| fun _ ->
      either {
        let rand = new System.Random()

        let num = 5
        let frontend = "inproc://frontend"
        let backend = "inproc://backend"
        use broker = Broker.create num frontend backend

        let loop (inbox: MailboxProcessor<RawRequest>) =
          let rec impl () = async {
              let! request = inbox.Receive()

              // add the requesting clients id to the random number so can later on
              // check that each client has gotten the answer to its own question
              let response = BitConverter.ToInt64(request.Body,0) + request.From

              response
              |> BitConverter.GetBytes
              |> RawResponse.fromRequest request
              |> broker.Respond
              return! impl ()
            }
          impl ()

        let mbp = MailboxProcessor.Start(loop)
        use obs = broker.Subscribe mbp.Post

        let clients =
          [| for n in 0 .. num - 1 do
               yield Client.create frontend |]

        let mkRequest (client: IClient) =
          async {
            let request = rand.Next() |> int64

            let response =
              request
              |> BitConverter.GetBytes
              |> client.Request
              |> fun ba -> BitConverter.ToInt64(ba, 0)

            return (client.Id, request, response)
          }

        let now = DateTime.Now

        // prove that we can correlate the random request number with a client
        // by adding the clients id to the random number
        let result =
          [| for i in 0 .. 50 do
              yield [| for client in clients do
                         yield mkRequest client |] |]
          |> Array.map (Async.Parallel >> Async.RunSynchronously)
          |> Array.fold
            (fun m batch ->
              if m then
                Array.fold
                  (fun m' (id,request,response) ->
                    if m'
                    then (request + id) = response
                    else m')
                  true
                  batch
              else m)
            true

        let total = DateTime.Now

        // printfn "took %fms" ((total - now).TotalMilliseconds)

        Array.iter dispose clients

        expect "Should be consitent" true id result
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let zmqIntegrationTests =
    testList "Zmq Integration Tests" [
      test_proper_cleanup_of_request_sockets
      test_broker_request_handling
    ]
