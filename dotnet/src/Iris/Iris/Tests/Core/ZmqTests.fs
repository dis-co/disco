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
        let rand = new System.Random()
        use stopper = new AutoResetEvent(false)

        let numclients = 5
        let numrequests = 50

        use ctx = new ZContext()

        let frontend = url "tcp://127.0.0.1:5555"
        let backend =  url "inproc://backend"
        let! broker = Broker.create ctx {
            Id = Id.Create()
            MinWorkers = uint8 numclients
            MaxWorkers = 20uy
            Frontend = frontend
            Backend = backend
            RequestTimeout = 200<ms>
          }

        let sloop (inbox: MailboxProcessor<RawServerRequest>) =
          let rec impl () = async {
              let! request = inbox.Receive()
              request.Body
              |> RawServerResponse.fromRequest request
              |> broker.Respond
              return! impl ()
            }
          impl ()

        let smbp = MailboxProcessor.Start(sloop)
        use obs = broker.Subscribe smbp.Post

        let responses = ResizeArray<Id * int64>()

        let cloop (inbox: MailboxProcessor<RawClientResponse>) =
          let mutable count = 0
          let rec imp () = async {
              let! msg = inbox.Receive()
              try
                match msg.Body with
                | Right response ->
                  let converted = BitConverter.ToInt64(response, 0)
                  responses.Add(msg.PeerId, converted)
                | Left error ->
                  error
                  |> string
                  |> Logger.err "client response"
              with
              | exn -> Logger.err "client loop" exn.Message

              count <- count + 1
              if count = (numrequests * numclients) then
                stopper.Set() |> ignore

              return! imp()
            }
          imp()

        let cmbp = MailboxProcessor.Start(cloop)

        let clients =
          [| for n in 0 .. (numclients - 1) do
               let result = Client.create ctx {
                  PeerId = Id.Create()
                  Frontend = frontend
                  Timeout = 200<ms>
                }
               match result with
               | Right socket ->
                  socket.Subscribe cmbp.Post |> ignore
                  yield socket
               | Left error -> failwithf "unable to create socket: %O" error
           |]

        let mkRequest (client: IClient) =
          async {
            let value = rand.Next() |> int64

            let request =
              value
              |> BitConverter.GetBytes
              |> fun bytes -> { RequestId = Guid.NewGuid(); Body = bytes }

            request
            |> client.Request
            |> Either.mapError (string >> Logger.err "client request")
            |> ignore

            return (client.PeerId, value)
          }

        // prove that we can correlate the random request number with a client
        // by adding the clients id to the random number
        let requests =
          [| for i in 0 .. (numrequests - 1) do
              yield [| for client in clients do
                          yield mkRequest client |] |]
          |> Array.map (Async.Parallel >> Async.RunSynchronously)
          |> Array.concat

        do! waitOrDie "stopper" stopper

        Array.iter dispose clients
        dispose broker

        expect "Should have same number of requests as responses" (Array.length requests) id responses.Count

        let result =
          responses.ToArray()
          |> Array.sort
          |> Array.zip (Array.sort requests)
          |> Array.fold
            (fun m' ((id1,request),(id2, response)) ->
              if m' then
                request = response
              else m')
            true

        expect "Should be consistent" true id result
      }
      |> noError

  let test_worker_timeout_fail_restarts_socket =
    testCase "worker timeout fail restarts socket" <| fun _ ->
      either {
        use ctx = new ZContext()

        let mutable count = 0

        let num = 5                     // number of clients
        let requests = 10               // number of requests per client

        let frontend = url "tcp://127.0.0.1:5555"
        let backend = url "inproc://backend"

        use! broker = Broker.create ctx {
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
               let result = Client.create ctx {
                  PeerId = Id.Create()
                  Frontend = frontend
                  Timeout = 100<ms>
                 }
               match result with
               | Right socket -> yield socket
               | Left error -> failwithf "unable to create client socket: %O" error |]

        let mkRequest (i: int) (client: IClient) =
          async {
            let response =
              i
              |> BitConverter.GetBytes
              |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
              |> client.Request
            return (client.PeerId, response)
          }

        [| for i in 0 .. requests - 1 do
            yield [| for client in clients -> mkRequest i client |] |]
        |> Array.iter (Async.Parallel >> Async.RunSynchronously >> ignore)

        Array.iter dispose clients

        // Explanation:
        //
        // We are testing whether the requests can still be issued even if the backend
        // does not, for whatever reason, answer in time.

        expect "Should have received all requests" count id count
      }
      |> noError

  let test_client_timeout_keeps_socket_alive =
    testCase "client timeout keeps socket alive" <| fun _ ->
      either {
        let num = 50
        let timeout = 10<ms>
        let mutable count = 0
        let mutable timedout = 0

        use doneCheck = new AutoResetEvent(false)

        use ctx = new ZContext()

        use! client = Client.create ctx {
          PeerId = Id.Create()
          Frontend = url "tcp://127.0.0.1:5555"
          Timeout = timeout
        }

        client.Subscribe (fun response ->
          if Either.isFail response.Body then
            Interlocked.Increment &timedout |> ignore
          Interlocked.Increment &count |> ignore
          if count = num then
            doneCheck.Set() |> ignore)
        |> ignore

        let request (n: int) =
          n
          |> BitConverter.GetBytes
          |> fun body -> { RequestId = Guid.NewGuid(); Body = body }
          |> client.Request
          |> ignore

        do! [| for n in 0 .. (num - 1) -> n |]
            |> Array.iter request
            |> Either.succeed

        Thread.Sleep(num * int timeout + 50)

        do! waitOrDie "doneCheck" doneCheck
      }
      |> noError

  let test_duplicate_broker_fails_gracefully =
    testCase "duplicate broker fails gracefully" <| fun _ ->
      either {
        use ctx = new ZContext()

        let frontend = url "tcp://127.0.0.1:5555"
        let backend = url "inproc://backend"

        use! broker1 = Broker.create ctx {
            Id = Id.Create()
            MinWorkers = 5uy
            MaxWorkers = 20uy
            Frontend = frontend
            Backend = backend
            RequestTimeout = 100<ms>
          }

        let broker2 = Broker.create ctx {
            Id = Id.Create()
            MinWorkers = 5uy
            MaxWorkers = 20uy
            Frontend = frontend
            Backend = backend
            RequestTimeout = 100<ms>
          }

        return!
          match broker2 with
          | Right _ -> Left(Other("test","should have failed"))
          | Left _ -> Right ()
      }
      |> noError

  let test_pub_socket_disposes_properly =
    testCase "pub socket disposes properly" <| fun _ ->
      either {
        use ctx = new ZContext()
        let uri = Uri.epgmUri
                    (IPv4Address "127.0.0.1")
                    (IPv4Address Constants.MCAST_ADDRESS)
                    (port Constants.MCAST_PORT)

        use pub = new Pub (unwrap uri, "myproject", ctx)
        do! pub.Start()
      }
      |> noError


  let test_sub_socket_disposes_properly =
    testCase "sub socket disposes properly" <| fun _ ->
      either {
        use ctx = new ZContext()
        let uri = Uri.epgmUri
                    (IPv4Address "127.0.0.1")
                    (IPv4Address Constants.MCAST_ADDRESS)
                    (port Constants.MCAST_PORT)

        use sub = new Sub (unwrap uri, "myproject", ctx)
        do! sub.Start()
      }
      |> noError
  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let zmqIntegrationTests =
    testList "Zmq Integration Tests" [
      test_client_timeout_keeps_socket_alive
      test_broker_request_handling
      test_worker_timeout_fail_restarts_socket
      test_duplicate_broker_fails_gracefully
      test_pub_socket_disposes_properly
      test_sub_socket_disposes_properly
    ] |> testSequenced
