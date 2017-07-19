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
    abstract BufferManager: IBufferManager
    abstract PendingRequests: PendingRequests
    abstract ResponseBuilder: IResponseBuilder
    abstract Subscriptions: Subscriptions

  // ** makeState

  let private makeState (options: ClientConfig) (subscriptions: Subscriptions) =
    let guid =
      options.ClientId
      |> Guid.ofId
      |> fun guid -> guid.ToByteArray()

    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let mutable client = Socket.createTcp()

    let manager = BufferManager.create Core.MAX_CONNECTIONS Core.BUFFER_SIZE

    let pending = PendingRequests()
    let mutable status = ServiceStatus.Stopped

    let builder = ResponseBuilder.create <| fun request client body  ->
      Logger.warn (tag "finalize") (string request)
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

    Socket.checkState
      client
      subscriptions
      (TcpClientEvent.Connected options.ClientId |> Some)
      ((options.ClientId, Error.asSocketError (tag "checkState") "Connection closed")
       |> TcpClientEvent.Disconnected
       |> Some)
    |> fun checkFun -> Async.Start(checkFun, cts.Token)

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

        member state.BufferManager
          with get () = manager

        member state.Dispose() =
          if not cts.IsCancellationRequested then
            try cts.Cancel() with | _ -> ()
            for KeyValue(id,_) in pending.ToArray() do
              pending.TryRemove(id) |> ignore
            dispose builder
            dispose client
            status <- ServiceStatus.Disposed
      }

  // ** onError

  let private onError location (args: SocketAsyncEventArgs) =
    let buffer, state = args.UserToken :?> IBuffer * IState
    let msg = String.Format("{0} in socket operation", args.SocketError)
    let error = Error.asSocketError (tag location) msg
    dispose buffer
    do Logger.err (tag location) msg
    state.Status <- ServiceStatus.Failed error
    (state.ClientId, error)
    |> TcpClientEvent.Disconnected
    |> Observable.onNext state.Subscriptions

  // ** onSend

  let private onSend (args: SocketAsyncEventArgs) =
    if args.SocketError <> SocketError.Success then
      let state = args.UserToken :?> IState
      let msg = String.Format("{0} in socket operation", args.SocketError)
      let error = Error.asSocketError (tag "onError") msg
      do Logger.err (tag "onError") msg
      state.Status <- ServiceStatus.Failed error
      (state.ClientId, error)
      |> TcpClientEvent.Disconnected
      |> Observable.onNext state.Subscriptions
    else
      sprintf "sent %d bytes" args.BytesTransferred
      |> Logger.err (tag "onSend")

  // ** sendAsync

  let private sendAsync (state: IState) (bytes: byte array) =
    let args = new SocketAsyncEventArgs()
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- state
    do args.SetBuffer(bytes, 0, bytes.Length)
    do args.Completed.Add onSend
    try
      match state.Socket.SendAsync(args) with
      | true -> ()
      | false -> onSend args
    with
      | :? ObjectDisposedException -> ()

  // ** onReceive

  let private onReceive (args: SocketAsyncEventArgs) =
    if args.BytesTransferred > 0 && args.SocketError = SocketError.Success then
      let buffer, state = args.UserToken :?> IBuffer * IState
      do state.ResponseBuilder.Process buffer args.Offset args.BytesTransferred
      do args.Dispose()

      sprintf "received %d bytes" args.BytesTransferred
      |> Logger.err (tag "onReceive")

      do receiveAsync state
    else onError "onReceive" args

  // ** receiveAsync

  let private receiveAsync (state: IState) =
    let buffer = state.BufferManager.TakeBuffer()
    let args = new SocketAsyncEventArgs()
    args.Completed.Add onReceive
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- buffer, state
    args.SetBuffer(buffer.Data, 0, buffer.Length)
    if not state.Disposed then
      match state.Socket.ReceiveAsync(args) with
      | true -> ()
      | false -> onReceive args

  // ** onInitialize

  let private onInitialize (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let buffer, state = args.UserToken :?> IBuffer * IState
      let tmp = Array.zeroCreate Core.CONNECTED.Length
      Array.blit args.Buffer 0 tmp 0 Core.CONNECTED.Length
      if tmp = Core.CONNECTED then
        Logger.debug (tag "onInitialize") "connection successful"
        state.Status <- ServiceStatus.Running
        do state.ClientId
          |> TcpClientEvent.Connected
          |> Observable.onNext state.Subscriptions
        do receiveAsync state
      else
        let msg = "Incorrect response from server, disconnected"
        let error = Error.asSocketError (tag "onInitialize") msg
        do Logger.err (tag "onInitialize") msg
        do (state.ClientId, error)
          |> TcpClientEvent.Disconnected
          |> Observable.onNext state.Subscriptions
      do args.Dispose()
      do dispose buffer
    else onError "onInitialize" args

  // ** initializeAsync

  let private initializeAsync (state: IState) =
    do sendAsync state state.ConnectionId
    let buffer = state.BufferManager.TakeBuffer()
    let args = new SocketAsyncEventArgs()
    args.Completed.Add onInitialize
    args.UserToken <- buffer, state
    args.SetBuffer(buffer.Data, 0, Core.CONNECTED.Length)
    if not state.Disposed then
      match state.Socket.ReceiveAsync(args) with
      | true -> ()
      | false -> onInitialize args

  // ** onConnected

  let private onConnected (args: SocketAsyncEventArgs) =
    if args.SocketError = SocketError.Success then
      let buffer, state = args.UserToken :?> IBuffer * IState
      do initializeAsync state
      do args.Dispose()
      do dispose buffer
    else onError "onConnected" args

  // ** connectAsync

  let private connectAsync (state: IState) =
    state.Status <- ServiceStatus.Starting
    let buffer = state.BufferManager.TakeBuffer()
    let args = new SocketAsyncEventArgs()
    do args.Completed.Add onConnected
    args.RemoteEndPoint <- state.EndPoint
    args.UserToken <- buffer, state
    match state.Socket.ConnectAsync args with
    | true  -> ()
    | false -> onConnected args

  // ** create

  let create (options: ClientConfig) =
    let subscriptions = Subscriptions()
    let mutable state = makeState options subscriptions

    let listener =
      flip Observable.subscribe subscriptions <| function
        | TcpClientEvent.Disconnected(id, error) ->
          dispose state
          Async.Start(async {
              do! Async.Sleep(500);
              state <- makeState options subscriptions
              connectAsync state
            })
        | _ -> ()

    { new IClient with
        member socket.Status
          with get () = state.Status

        member socket.Connect() =
          connectAsync state

        member socket.Request(request: Request) =
          if Service.isRunning state.Status then
            // this socket is asking soemthing, so we need to track this in pending requests
            state.PendingRequests.TryAdd(request.RequestId, request) |> ignore
            do request |> RequestBuilder.serialize |> sendAsync state
          else
            string state.Status
            |> String.format "not sending, wrong state {0}"
            |> Logger.err (tag "Request")

        member socket.Respond(response: Response) =
          if Service.isRunning state.Status then
            do response |> RequestBuilder.serialize |> sendAsync state
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
