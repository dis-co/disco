namespace Disco.Tests

open System
open System.Threading
open System.Collections.Concurrent
open Expecto
open FsCheck
open Disco.Core
open Disco.Raft
open Disco.Service
open Disco.Net
open Microsoft.FSharp.Control

[<AutoOpen>]
module NetIntegrationTests =

  //  _   _      _
  // | \ | | ___| |_
  // |  \| |/ _ \ __|
  // | |\  |  __/ |_
  // |_| \_|\___|\__|

  let test_client_should_automatically_reconnect =
    testCase "client should automatically reconnect" <| fun _ ->
      either {
        let ip = IpAddress.Localhost
        let prt = port 5555us

        use onConnected = new WaitEvent()
        use onDisconnected = new WaitEvent()

        use client = TcpClient.create {
            ClientId = DiscoId.Create()
            PeerAddress = ip
            PeerPort = prt
            Timeout = 0<ms>
          }

        use clientHandler =
          client.Subscribe <| function
            | TcpClientEvent.Connected    _ -> onConnected.Set() |> ignore
            | TcpClientEvent.Disconnected _ -> onDisconnected.Set() |> ignore
            | _ -> ()

        do client.Connect()

        do! waitFor "onDisconnected" onDisconnected
        do! waitFor "onDisconnected" onDisconnected

        use server = TcpServer.create {
            ServerId = DiscoId.Create()
            Listen = ip
            Port = prt
          }

        do! server.Start()

        do! waitFor "onConnected" onConnected
      }
      |> noError

  let test_server_request_handling =
    testCase "server request handling" <| fun _ ->
      either {

        let rand = new System.Random()
        use stopper = new WaitEvent()

        let numclients = 5
        let numrequests = 2

        let apirequests = ConcurrentBag()

        apirequests.Add
        |> Prop.forAll Generators.apiRequestArb
        |> Check.QuickThrowOnFailure

        let ip = IpAddress.Localhost
        let prt = port 5555us

        let server = TcpServer.create {
            ServerId = DiscoId.Create()
            Listen = ip
            Port = prt
          }

        let sloop (inbox: MailboxProcessor<TcpServerEvent>) =
          let rec impl () = async {
              let! ev = inbox.Receive()
              match ev with
              | TcpServerEvent.Request request ->
                request.Body
                |> Response.fromRequest request
                |> server.Respond
              | _ -> ()
              return! impl ()
            }
          impl ()

        let smbp = MailboxProcessor.Start(sloop)
        use obs = server.Subscribe smbp.Post

        do! server.Start()

        let responses = ResizeArray<Response>()

        let clientsLive = new WaitEvent()
        let mutable liveClients = 0

        let cloop (inbox: MailboxProcessor<TcpClientEvent>) =
          let mutable count = 0
          let rec imp () = async {
              let! ev = inbox.Receive()
              try
                match ev with
                | TcpClientEvent.Connected _ ->
                  Interlocked.Increment &liveClients |> ignore
                  if liveClients = numclients then
                    clientsLive.Set() |> ignore
                | TcpClientEvent.Response response ->
                  responses.Add(response)
                  count <- count + 1
                  if count = (numrequests * numclients) then
                    stopper.Set() |> ignore
                | _ -> ()
              with
              | exn -> Logger.err "client loop" exn.Message
              return! imp()
            }
          imp()

        let cmbp = MailboxProcessor.Start(cloop)

        let clients =
          [| for n in 0 .. (numclients - 1) do
               let socket = TcpClient.create {
                  ClientId = DiscoId.Create()
                  PeerAddress = ip
                  PeerPort = prt
                  Timeout = 200<ms>
               }
               socket.Subscribe cmbp.Post |> ignore
               socket.Connect()
               yield socket
           |]

        do! waitFor "clientsLive" clientsLive

        let mkRequest (client: ITcpClient) =
          async {
            let request =
              apirequests.TryTake()
              |> snd
              |> Binary.encode
              |> Request.create (Guid.ofId client.ClientId)
            client.Request request
            return request
          }

        // prove that we can correlate the random request number with a client
        // by adding the clients id to the random number
        let requests =
          [| for i in 0 .. (numrequests - 1) do
              yield [| for client in clients do
                          yield mkRequest client |] |]
          |> Array.map (Async.Parallel >> Async.RunSynchronously)
          |> Array.concat

        do! waitFor "stopper" stopper

        Array.iter dispose clients
        dispose server

        expect "Should have same number of requests as responses" (Array.length requests) id responses.Count

        let result =
          responses.ToArray()
          |> Array.sortBy (fun (response: Response) -> response.RequestId)
          |> Array.zip (Array.sortBy (fun (request: Request) -> request.RequestId) requests)
          |> Array.fold
            (fun m' (request, response) ->
              if m'
              then request.Body = response.Body
              else m')
            true

        expect "Should be consistent" true id result
      }
      |> noError

  let test_duplicate_server_fails_gracefully =
    testCase "duplicate server fails gracefully" <| fun _ ->
      either {
        let ip = IpAddress.Localhost
        let prt = port 5555us

        use server1 = TcpServer.create {
          ServerId = DiscoId.Create()
          Listen = ip
          Port = prt
        }

        do! server1.Start()

        let server2 = TcpServer.create {
          ServerId = DiscoId.Create()
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
        let mem = DiscoId.Create() |> Member.create
        use pub = PubSub.create mem
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
      test_client_should_automatically_reconnect
      test_server_request_handling
      test_duplicate_server_fails_gracefully
      test_pub_socket_disposes_properly
    ] |> testSequenced
