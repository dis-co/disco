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

  let test_broker_request_handling =
    testCase "broker request handling" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        let rand = new System.Random()

        let num = 5
        let frontend = url "tcp://127.0.0.1:5555"
        let backend =  url "inproc://backend"
        use! broker = Broker.create {
            Id = Id.Create()
            MinWorkers = uint8 num
            MaxWorkers = 20uy
            Frontend = frontend
            Backend = backend
            RequestTimeout = 200<ms>
          }

        let sloop (inbox: MailboxProcessor<RawServerRequest>) =
          let rec impl () = async {
              let! request = inbox.Receive()

              Tracing.trace "Agent responding" <| fun () ->
                // add the requesting clients id to the random number so can later on
                // check that each client has gotten the answer to its own question
                let response =
                  let id = request.From
                  BitConverter.ToInt64(request.Body,0) +
                  BitConverter.ToInt64(id.ToByteArray(),0)

                response
                |> BitConverter.GetBytes
                |> RawServerResponse.fromRequest request
                |> broker.Respond

              return! impl ()
            }
          impl ()

        let smbp = MailboxProcessor.Start(sloop)
        use obs = broker.Subscribe smbp.Post

        let responses = ResizeArray()

        let cloop (inbox: MailboxProcessor<RawClientResponse>) =
          let rec imp () = async {
              let! msg = inbox.Receive()
              match msg.Body with
              | Right response ->
                let converted = BitConverter.ToInt64(response, 0)
                responses.Add(msg.PeerId, converted)
              | Left error -> error |> string |> Logger.err "client response"
              printfn "got response!!"
              return! imp()
            }
          imp()

        let cmbp = MailboxProcessor.Start(cloop)

        let clients =
          [| for n in 0 .. num - 1 do
               let socket = Client.create {
                  PeerId = Id.Create()
                  Frontend = frontend
                  Timeout = 200<ms>
                }
               socket.Subscribe cmbp.Post |> ignore
               yield socket
           |]

        let mkRequest (client: IClient) =
          async {
            let value = rand.Next() |> int64

            let request =
              value
              |> BitConverter.GetBytes
              |> fun bytes -> { Body = bytes }

            request
            |> client.Request
            |> Either.mapError (string >> Logger.err "client request")
            |> ignore

            return (client.PeerId, request)
          }

        // prove that we can correlate the random request number with a client
        // by adding the clients id to the random number
        let requests =
          [| for i in 0 .. 50 do
              yield [| for client in clients do
                          yield mkRequest client |] |]
          |> Array.map (Async.Parallel >> Async.RunSynchronously)
          |> Array.concat

        let result = failwith "never"
          // (fun m' (id,request,response) ->
          //         if m'
          //         then
          //           let computed =
          //             id
          //             |> string
          //             |> Guid.Parse
          //             |> fun guid -> guid.ToByteArray()
          //             |> fun bytes -> BitConverter.ToInt64(bytes,0)
          //           (request + computed) = response
          //         else m')

        Array.iter dispose clients

        expect "Should be consistent" true id result
      }
      |> noError

  // let test_client_send_fail_restarts_socket =
  //   testCase "client send fail restarts socket" <| fun _ ->
  //     either {
  //       use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

  //       let addr = url "tcp://1.2.3.4:5555"
  //       use client = Client.create {
  //           PeerId = Id.Create()
  //           Frontend = addr
  //           Timeout = 10<ms>
  //         }

  //       for i in 0 .. 10 do
  //         do! i
  //             |> BitConverter.GetBytes
  //             |> client.Request
  //             |> Either.ignore
  //     }
  //     |> noError


  let test_worker_timeout_fail_restarts_socket =
    testCase "worker timeout fail restarts socket" <| fun _ ->
      either {
        use lobs = Logger.subscribe (Logger.filter Trace Logger.stdout)

        let mutable count = 0

        let num = 5                     // number of clients
        let requests = 10               // number of requests per client

        let frontend = url "tcp://127.0.0.1:5555"
        let backend = url "inproc://backend"

        use! broker = Broker.create {
            Id = Id.Create()
            MinWorkers = uint8 num
            MaxWorkers = 20uy
            Frontend = frontend
            Backend = backend
            RequestTimeout = 100<ms>
          }

        use bobs = broker.Subscribe (fun _ -> count <- Interlocked.Increment &count)

        let clients =
          [| for n in 0 .. num - 1 do
               yield Client.create {
                  PeerId = Id.Create()
                  Frontend = frontend
                  Timeout = 100<ms>
                 } |]

        let mkRequest (i: int) (client: IClient) =
          async {
            let response =
              i
              |> BitConverter.GetBytes
              |> fun body -> { Body = body }
              |> client.Request
            return (client.PeerId, response)
          }

        [| for i in 0 .. requests - 1 do
            yield [| for client in clients -> mkRequest i client |] |]
        |> Array.iter (Async.Parallel >> Async.RunSynchronously >> ignore)

        Array.iter dispose clients

        // Explanation:
        //
        // We are testing whether the workers are "self-healing" here, that is, they do mitigate
        // backend timeouts by re-registering themselves with the broker after timeout, such that
        // they can resume processing requests. This is proven here by comparing the number of
        // requests received on the broker subscription with the number of workers used, and if that
        // number is higher than the worker count, the workers have mitigated backend response
        // timeouts successfully.

        printfn "count: %d expected: %d" count (num * requests)

        expect "Should have passed on more requests than workers" true ((<) num) count
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let zmqIntegrationTests =
    testList "Zmq Integration Tests" [
      test_broker_request_handling
      // test_client_send_fail_restarts_socket
      test_worker_timeout_fail_restarts_socket
    ] |> testSequenced
