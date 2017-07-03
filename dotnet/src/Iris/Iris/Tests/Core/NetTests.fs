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

  let test_server_should_fail_on_start_with_duplicate_port =
    testCase "server should fail on start with duplicate port" <| fun _ ->
      either {
        use log = Logger.subscribe Logger.stdout
        let ip = IpAddress.Localhost
        let prt = port 5555us

        use server1 = TcpServer.create {
            ServerId = Id.Create()
            Listen = ip
            Port = prt
          }

        use server2 = TcpServer.create {
            ServerId = Id.Create()
            Listen = ip
            Port = prt
          }

        do! server1.Start()

        do! match server2.Start() with
            | Right () -> Left(Other("test", "should have failed"))
            | Left _   -> Right ()
      }
      |> noError

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
            ServerId = Id.Create()
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

        let cloop (inbox: MailboxProcessor<TcpClientEvent>) =
          let mutable count = 0
          let rec imp () = async {
              let! ev = inbox.Receive()
              try
                match ev with
                | TcpClientEvent.Response response -> responses.Add(response)
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
                  ClientId = Id.Create()
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

        do! waitOrDie "stopper" stopper

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
            ServerId = Id.Create()
            Listen = ip
            Port = prt
          }

        do! server1.Start()

        let server2 = TcpServer.create {
            ServerId = Id.Create()
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
      test_server_should_fail_on_start_with_duplicate_port
      test_server_request_handling
      test_duplicate_server_fails_gracefully
      test_pub_socket_disposes_properly
    ] |> testSequenced
