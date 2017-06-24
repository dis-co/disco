namespace Iris.Tests

open System
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Net
open Microsoft.FSharp.Control

[<AutoOpen>]
module NetIntegrationTests =

  //  _   _      _
  // | \ | | ___| |_
  // |  \| |/ _ \ __|
  // | |\  |  __/ |_
  // |_| \_|\___|\__|

  let test_server_request_handling =
    testCase "server request handling" <| fun _ ->
      either {
        let rand = new System.Random()
        use stopper = new AutoResetEvent(false)

        let numclients = 5
        let numrequests = 2

        let ip = IpAddress.Localhost
        let prt = port 5555us

        let server = TcpServer.create {
            Id = Id.Create()
            Listen = ip
            Port = prt
          }

        let sloop (inbox: MailboxProcessor<TcpServerEvent>) =
          let rec impl () = async {
              let! ev = inbox.Receive()
              match ev with
              | TcpServerEvent.Connect(id, ip, port) ->
                printfn "new connection from %O" id
              | TcpServerEvent.Disconnect id ->
                printfn "disconnect from %O" id
              | TcpServerEvent.Request request ->
                printfn "new request: %A" request
                request.Body
                |> OutgoingResponse.fromRequest request
                |> server.Respond
              return! impl ()
            }
          impl ()

        let smbp = MailboxProcessor.Start(sloop)
        use obs = server.Subscribe smbp.Post
        do! server.Start()

        let responses = ResizeArray<Id * int64>()

        let cloop (inbox: MailboxProcessor<TcpClientEvent>) =
          let mutable count = 0
          let rec imp () = async {
              let! ev = inbox.Receive()
              try
                match ev with
                | TcpClientEvent.Response response ->
                  let converted = BitConverter.ToInt64(response.Body, 0)
                  responses.Add(Guid.toId response.PeerId, converted)
                | _ -> ()
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
               let socket = TcpClient.create {
                  PeerId = Id.Create()
                  PeerAddress = ip
                  PeerPort = prt
                  Timeout = 200<ms>
                }
               match socket.Start() with
               | Right () ->
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
              |> Request.create (Guid.ofId client.PeerId)
            client.Request request
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
        dispose server

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
        let mutable count = 0

        let num = 5                     // number of clients
        let requests = 10               // number of requests per client

        let ip = IpAddress.Localhost
        let prt = port 5555us

        use server = TcpServer.create {
          Id = Id.Create()
          Listen = ip
          Port = prt
        }

        use bobs = server.Subscribe (fun _ -> count <- Interlocked.Increment &count)

        do! server.Start()

        let clients =
          [| for n in 0 .. num - 1 do
               let socket = TcpClient.create {
                  PeerId = Id.Create()
                  PeerAddress = ip
                  PeerPort = prt
                  Timeout = 100<ms>
                 }
               match socket.Start() with
               | Right () -> yield socket
               | Left error -> failwithf "unable to create client socket: %O" error |]

        let mkRequest (i: int) (client: IClient) =
          async {
            i
            |> BitConverter.GetBytes
            |> Request.create (Guid.ofId client.PeerId)
            |> client.Request
            return (client.PeerId, ())
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

        let ip = IpAddress.Localhost
        let prt = port 5555us

        use client = TcpClient.create {
          PeerId = Id.Create()
          PeerAddress = ip
          PeerPort = prt
          Timeout = timeout
        }

        client.Subscribe (function
          | TcpClientEvent.Disconnected _ ->
              Interlocked.Increment &timedout |> ignore
          | _ ->
            Interlocked.Increment &count |> ignore
            if count = num then
              doneCheck.Set() |> ignore)
        |> ignore

        do! client.Start()

        let request (n: int) =
          n
          |> BitConverter.GetBytes
          |> Request.create (Guid.ofId client.PeerId)
          |> client.Request

        do! [| for n in 0 .. (num - 1) -> n |]
            |> Array.iter request
            |> Either.succeed

        Thread.Sleep(num * int timeout + 50)

        do! waitOrDie "doneCheck" doneCheck
      }
      |> noError

  let test_duplicate_server_fails_gracefully =
    testCase "duplicate server fails gracefully" <| fun _ ->
      either {
        let ip = IpAddress.Localhost
        let prt = port 5555us

        use server1 = TcpServer.create {
            Id = Id.Create()
            Listen = ip
            Port = prt
          }

        do! server1.Start()

        let server2 = TcpServer.create {
            Id = Id.Create()
            Listen = ip
            Port = prt
          }

        return!
          match server2.Start() with
          | Right _ -> Left(Other("test","should have failed"))
          | Left  _ -> Right ()
      }
      |> noError

  let test_pub_socket_disposes_properly =
    testCase "pub socket disposes properly" <| fun _ ->
      either {
        let id = Id.Create()
        use pub = PubSub.create id PubSub.defaultAddress (int Constants.MCAST_PORT)
        do! pub.Start()
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let netIntegrationTests =
    testList "Net Integration Tests" [
      test_server_request_handling
      test_client_timeout_keeps_socket_alive
      test_worker_timeout_fail_restarts_socket
      test_duplicate_server_fails_gracefully
      test_pub_socket_disposes_properly
    ] |> testSequenced
