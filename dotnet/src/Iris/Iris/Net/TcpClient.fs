namespace Iris.Net

// * Imports

open System
open System.IO
open System.Net
open System.Net.Sockets
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open Iris.Core

// * TcpClient

module rec TcpClient =

  // ** tag

  let private tag (str: string) = String.format "TcpClient.{0}" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid,IObserver<TcpClientEvent>>

  // ** PendingRequests

  type private RequestId = Guid

  type private PendingRequests = ConcurrentDictionary<RequestId,Request>

  // ** IState

  type private IState =
    inherit IDisposable
    abstract Disposed: bool
    abstract Status: ServiceStatus with get, set
    abstract ClientId: Id
    abstract ConnectionId: byte array
    abstract Socket: Socket
    abstract EndPoint: IPEndPoint
    abstract PendingRequests: PendingRequests
    abstract ResponseBuilder: IResponseBuilder
    abstract Subscriptions: Subscriptions
    abstract Request: Request -> unit
    abstract Respond: Response -> unit
    abstract StartReceiving: unit -> unit

  // ** makeState

  let private makeState (options: ClientConfig) (subscriptions: Subscriptions) =
    let guid =
      options.ClientId
      |> Guid.ofId
      |> fun guid -> guid.ToByteArray()

    let pending = PendingRequests()
    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let client = Socket.createTcp()
    let mutable stream = Unchecked.defaultof<NetworkStream>
    let mutable status = ServiceStatus.Stopped

    let builder = ResponseBuilder.create <| fun request client body  ->
      if pending.ContainsKey request then
        pending.TryRemove(request) |> ignore
        body
        |> Response.create request client
        |> TcpClientEvent.Response
        |> Observable.onNext subscriptions
      else
        body
        |> Request.make request client
        |> TcpClientEvent.Request
        |> Observable.onNext subscriptions

    //                     _ _
    //  ___  ___ _ __   __| (_)_ __   __ _
    // / __|/ _ \ '_ \ / _` | | '_ \ / _` |
    // \__ \  __/ | | | (_| | | | | | (_| |
    // |___/\___|_| |_|\__,_|_|_| |_|\__, |
    //                               |___/
    let rec sendLoop (inbox: MailboxProcessor<byte array>) = async {
        let! msg = inbox.Receive()
        do stream.Write(msg, 0, msg.Length)
        return! sendLoop inbox
      }

    //                    _       _
    //  _ __ ___  ___ ___(_)_   _(_)_ __   __ _
    // | '__/ _ \/ __/ _ \ \ \ / / | '_ \ / _` |
    // | | |  __/ (_|  __/ |\ V /| | | | | (_| |
    // |_|  \___|\___\___|_| \_/ |_|_| |_|\__, |
    //                                    |___/
    let receiveLoop =
      async {
        let mutable run = true
        while run do
          try
            let result = stream.ReadByte()
            if result = -1 then
              let error =
                "Reached end of Stream. Disconnected"
                |> Error.asSocketError (tag "receiveLoop")
              (options.ClientId, error)
              |> TcpClientEvent.Disconnected
              |> Observable.onNext subscriptions
              run <- false
            else
              result |> byte |> builder.Write
          with
            | :? IOException -> run <- false
            | exn ->
              let error = Error.asSocketError (tag "receiveLoop") exn.Message
              (options.ClientId, error)
              |> TcpClientEvent.Disconnected
              |> Observable.onNext subscriptions
              Logger.err (tag "receiveLoop") exn.Message
              run <- false
      }

    let sender = new MailboxProcessor<byte[]>(sendLoop, cts.Token)

    { new IState with
        member state.Status
          with get () = status
          and set st = status <- st

        member state.ClientId
          with get () = options.ClientId

        member state.ConnectionId
          with get () = guid

        member state.Socket
          with get () = client

        member state.EndPoint
          with get () = endpoint

        member state.PendingRequests
          with get () = pending

        member state.Disposed
          with get () = cts.IsCancellationRequested

        member state.ResponseBuilder
          with get () = builder

        member state.Subscriptions
          with get () = subscriptions

        member state.Request (request: Request) =
          // this socket is asking something, so we need to track this in pending requests
          do request.RequestId
             |> sprintf "sending to %A (id: %A)" request.PeerId
             |> Logger.debug (tag "Request")
          do pending.TryAdd(request.RequestId, request) |> ignore
          do request |> RequestBuilder.serialize |> sender.Post

        member state.Respond (response: Response) =
          do response.RequestId
             |> sprintf "sending to %A (id: %A)" response.PeerId
             |> Logger.debug (tag "Respond")
          do response |> RequestBuilder.serialize |> sender.Post

        member state.StartReceiving() =
          stream <- new NetworkStream(client)
          Async.Start(receiveLoop, cts.Token)
          sender.Start()

        member state.Dispose() =
          if not cts.IsCancellationRequested then
            try cts.Cancel() with | _ -> ()
            for KeyValue(id,_) in pending.ToArray() do
              pending.TryRemove(id) |> ignore
            dispose builder
            tryDispose stream ignore
            dispose client
            status <- ServiceStatus.Disposed
      }

  // ** onError

  let private onError location (args: SocketAsyncEventArgs) =
    let listener, state = args.UserToken :?> IDisposable * IState
    let msg = String.Format("{0} in socket operation", args.SocketError)
    let error = Error.asSocketError (tag location) msg
    do dispose listener
    do Logger.err (tag location) msg
    state.Status <- ServiceStatus.Failed error
    (state.ClientId, error)
    |> TcpClientEvent.Disconnected
    |> Observable.onNext state.Subscriptions

  // ** onSend

  let private onSend (args: SocketAsyncEventArgs) =
    let listener, state = args.UserToken :?> IDisposable * IState
    if args.SocketError <> SocketError.Success then
      let msg = String.Format("{0} in socket operation", args.SocketError)
      let error = Error.asSocketError (tag "onError") msg
      do Logger.err (tag "onError") msg
      state.Status <- ServiceStatus.Failed error
      (state.ClientId, error)
      |> TcpClientEvent.Disconnected
      |> Observable.onNext state.Subscriptions
    else
      args.BytesTransferred
      |> String.format "sent {0} bytes"
      |> Logger.debug (tag "onSend")
    do dispose listener

  // ** sendAsync

  let private sendAsync (state: IState) (bytes: byte array) =
    let args = new SocketAsyncEventArgs()
    do args.SetBuffer(bytes, 0, bytes.Length)
    let listener = args.Completed.Subscribe onSend
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- listener, state
    try
      match state.Socket.SendAsync(args) with
      | true -> ()
      | false -> onSend args
    with
      | :? ObjectDisposedException -> ()

  // ** onInitialize

  let private onInitialize (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let listener, state = args.UserToken :?> IDisposable * IState
      if args.Buffer = Core.CONNECTED then
        state.Status <- ServiceStatus.Running
        state.StartReceiving()
        do state.ClientId
          |> TcpClientEvent.Connected
          |> Observable.onNext state.Subscriptions
      else
        let msg = "Incorrect response from server, disconnected"
        let error = Error.asSocketError (tag "onInitialize") msg
        do Logger.err (tag "onInitialize") msg
        do (state.ClientId, error)
          |> TcpClientEvent.Disconnected
          |> Observable.onNext state.Subscriptions
      do dispose listener
    else onError "onInitialize" args

  // ** initializeAsync

  let private initializeAsync (state: IState) =
    do sendAsync state state.ConnectionId
    let args = new SocketAsyncEventArgs()
    let buffer = Array.zeroCreate Core.CONNECTED.Length
    let listener = args.Completed.Subscribe onInitialize
    args.UserToken <- listener, state
    args.SetBuffer(buffer, 0, buffer.Length)
    if not state.Disposed then
      match state.Socket.ReceiveAsync(args) with
      | true -> ()
      | false -> onInitialize args

  // ** onConnected

  let private onConnected (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let listener, state = args.UserToken :?> IDisposable * IState
      do initializeAsync state
      do dispose listener
      do dispose args
    else onError "onConnected" args

  // ** connectAsync

  let private connectAsync (state: IState) =
    state.Status <- ServiceStatus.Starting
    let args = new SocketAsyncEventArgs()
    let listener = args.Completed.Subscribe onConnected
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- listener, state
    match state.Socket.ConnectAsync args with
    | true  -> ()
    | false -> onConnected args

  // ** create

  let create (options: ClientConfig) =
    let subscriptions = Subscriptions()
    let mutable state = makeState options subscriptions

    let listener =
      flip Observable.subscribe subscriptions <| function
        | TcpClientEvent.Disconnected _ ->
          dispose state
          Async.Start(async {
              do! Async.Sleep(500);
              state <- makeState options subscriptions
              connectAsync state
            })
        | _ -> ()

    { new ITcpClient with
        member socket.Status
          with get () = state.Status

        member socket.Connect() =
          connectAsync state

        member socket.Request(request: Request) =
          if Service.isRunning state.Status then
            do state.Request request
          else
            string state.Status
            |> String.format "not sending, wrong state {0}"
            |> Logger.err (tag "Request")

        member socket.Respond(response: Response) =
          if Service.isRunning state.Status then
            do state.Respond response
          else
            string state.Status
            |> String.format "not sending, wrong state {0}"
            |> Logger.err (tag "Respond")

        member socket.Disconnect() =
          do Socket.disconnect state.Socket

        member socket.ClientId
          with get () = options.ClientId

        member socket.Subscribe (callback: TcpClientEvent -> unit) =
          Observable.subscribe callback subscriptions

        member socket.Dispose () =
          listener.Dispose()
          state.Dispose() }
