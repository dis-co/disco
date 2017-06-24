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

  type private PendingRequests = ConcurrentDictionary<RequestId,int64>

  // ** IState

  type private IState =
    inherit IDisposable
    abstract Status: ServiceStatus with get, set
    abstract Start: unit -> unit
    abstract PeerId: Id
    abstract ConnectionId: Guid
    abstract Socket: Socket
    abstract EndPoint: IPEndPoint
    abstract Connected: ManualResetEvent
    abstract Sent: ManualResetEvent
    abstract Buffer: byte array
    abstract PendingRequests: PendingRequests
    abstract ResponseBuilder: IResponseBuilder
    abstract Subscriptions: Subscriptions

  // ** connectCallback

  let private connectCallback() =
    AsyncCallback(fun (ar: IAsyncResult) ->
      try
        let state = ar.AsyncState :?> IState
        state.Socket.EndConnect(ar)     // complete the connection
        state.PeerId
        |> TcpClientEvent.Connected
        |> Observable.onNext state.Subscriptions
        state.Connected.Set() |> ignore  // Signal that the connection has been made.
      with
        | exn -> exn.Message |> Logger.err (tag "connectCallback"))

  // ** beginConnect

  let private beginConnect (state: IState) =
    state.Socket.BeginConnect(state.EndPoint, connectCallback(), state) |> ignore
    if not (state.Connected.WaitOne(TimeSpan.FromMilliseconds 1000.0)) then
      state.Status <-
        "Connection Timeout"
        |> Error.asSocketError (tag "beginConnect")
        |> ServiceStatus.Failed
    else
      state.Status <- ServiceStatus.Running

  // ** receiveCallback

  let private receiveCallback() =
    AsyncCallback(fun (ar: IAsyncResult) ->
      let state = ar.AsyncState :?> IState
      try
        // Read data from the remote device.
        let bytesRead = state.Socket.EndReceive(ar)
        state.ResponseBuilder.Process bytesRead
        beginReceive state
      with
        | exn ->
          exn.Message |> Logger.err (tag "receiveCallback")
          state.Status <-
            exn.Message
            |> Error.asSocketError (tag "receiveCallback")
            |> ServiceStatus.Failed)

  // ** beginReceive

  let private beginReceive (state: IState) =
    try
      // Begin receiving the data from the remote device.
      state.Socket.BeginReceive(
        state.Buffer,
        0,
        Core.BUFFER_SIZE,
        SocketFlags.None,
        receiveCallback(),
        state)
      |> ignore
    with
      | exn ->
        exn.Message |> Logger.err (tag "beginReceive")
        state.Status <-
          exn.Message
          |> Error.asSocketError (tag "beginReceive")
          |> ServiceStatus.Failed

  // ** sendCallback

  let private sendCallback (ar: IAsyncResult) =
    let state = ar.AsyncState :?> IState
    try
      // Complete sending the data to the remote device.
      state.Socket.EndSend(ar) |> ignore
      // Signal that all bytes have been sent.
      state.Sent.Set() |> ignore
    with
      | exn ->
        exn.Message |> Logger.err (tag "sendCallback")
        state.Status <-
          exn.Message
          |> Error.asSocketError (tag "sendCallback")
          |> ServiceStatus.Failed

  // ** send

  let private send (state: IState) (request: Request) =
    try
      let payload = Request.serialize request
      // Begin sending the data to the remote device.
      state.Socket.BeginSend(
        payload,
        0,
        payload.Length,
        SocketFlags.None,
        AsyncCallback(sendCallback),
        state)
      |> ignore
      if not (state.Sent.WaitOne(TimeSpan.FromMilliseconds 3000.0)) then
        state.Status <-
          "Send Timeout"
          |> Error.asSocketError (tag "send")
          |> ServiceStatus.Failed
      else
        state.Status <- ServiceStatus.Running
    with
      | exn ->
        exn.Message |> Logger.err (tag "send")
        state.Status <-
          exn.Message
          |> Error.asSocketError (tag "receiveCallback")
          |> ServiceStatus.Failed

  // ** makeSocket

  let private makeSocket () =
    new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)

  // ** start

  let private start (state: IState) checker (cts: CancellationTokenSource) =
    if not (Service.isRunning state.Status) then
      beginConnect state
      Async.Start(checker, cts.Token)
      beginReceive state

  // ** makeState

  let private makeState id (options: ClientConfig) (subscriptions: Subscriptions) =
    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let client = makeSocket()

    let buffer = Array.zeroCreate Core.BUFFER_SIZE
    let connected = new ManualResetEvent(false)
    let sent = new ManualResetEvent(false)
    let pending = PendingRequests()
    let mutable status = ServiceStatus.Stopped

    let builder = ResponseBuilder.create buffer <| fun request client body  ->
      body
      |> Response.create request client
      |> TcpClientEvent.Response
      |> Observable.onNext subscriptions

    let checker =
      Socket.checkState
        client
        subscriptions
        (options.PeerId |> TcpClientEvent.Connected    |> Some)
        (options.PeerId |> TcpClientEvent.Disconnected |> Some)

    { new IState with
        member state.Start () =
          start state checker cts

        member state.Status
          with get () = status
          and set st = status <- st

        member state.PeerId
          with get () = options.PeerId

        member state.ConnectionId
          with get () = id

        member state.Socket
          with get () = client

        member state.EndPoint
          with get () = endpoint

        member state.Connected
          with get () = connected

        member state.Sent
          with get () = sent

        member state.Buffer
          with get () = buffer

        member state.PendingRequests
          with get () = pending

        member state.ResponseBuilder
          with get () = builder

        member state.Subscriptions
          with get () = subscriptions

        member state.Dispose() =
          dispose cts
          builder.Dispose()
          Socket.dispose client
      }

  // ** create

  let create (options: ClientConfig) =
    let id = Guid.NewGuid()
    let subscriptions = Subscriptions()
    let mutable state = makeState id options subscriptions

    { new IClient with
        member socket.Status
          with get () = state.Status

        member socket.Request(request: Request) =
          if not (Service.isRunning state.Status) then
            state.Dispose()
            state <- makeState id options subscriptions
            state.Start()
            send state request
          else
            send state request

        member socket.Start() =
          try
            state.Start()
            Either.nothing
          with
            | exn ->
              exn.Message
              |> Error.asSocketError (tag "Start")
              |> Either.fail

        member socket.PeerId
          with get () = options.PeerId

        member socket.Subscribe (callback: TcpClientEvent -> unit) =
          Observable.subscribe callback subscriptions

        member socket.Dispose () =
          state.Dispose() }
