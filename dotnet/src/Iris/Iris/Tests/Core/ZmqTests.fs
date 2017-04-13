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
        let frontend = "tcp://127.0.0.1:5555"
        let backend = "inproc://backend"
        use! broker = Broker.create (Id.Create()) num frontend backend

        let loop (inbox: MailboxProcessor<RawRequest>) =
          let rec impl () = async {
              let! request = inbox.Receive()

              Tracing.trace "Agent responding" <| fun () ->
                // add the requesting clients id to the random number so can later on
                // check that each client has gotten the answer to its own question
                let response = BitConverter.ToInt64(request.Body,0) +
                               BitConverter.ToInt64(request.From.ToByteArray(),0)

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
               yield Client.create (Id.Create()) frontend |]

        let mkRequest (client: IClient) =
          async {
            let request = rand.Next() |> int64

            let response =
              request
              |> BitConverter.GetBytes
              |> client.Request
              |> Either.map (fun ba -> BitConverter.ToInt64(ba, 0))
              |> Either.get

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
                    then
                      let computed =
                        id
                        |> string
                        |> Guid.Parse
                        |> fun guid -> guid.ToByteArray()
                        |> fun bytes -> BitConverter.ToInt64(bytes,0)
                      (request + computed) = response
                    else m')
                  true
                  batch
              else m)
            true

        let total = DateTime.Now

        // printfn "took %fms" ((total - now).TotalMilliseconds)

        Array.iter dispose clients

        expect "Should be consistent" true id result
      }
      |> noError

  let test_client_send_fail_restarts_socket =
    ftestCase "client send fail restarts socket" <| fun _ ->
      either {
        use obs = Logger.subscribe Logger.stdout
        let addr = "tcp://1.2.3.4:5555"
        let client = Client.create (Id.Create()) addr

        for i in 0 .. 10 do
          do! i
              |> BitConverter.GetBytes
              |> client.Request
              |> Either.ignore
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
      test_client_send_fail_restarts_socket
    ] |> testSequenced
