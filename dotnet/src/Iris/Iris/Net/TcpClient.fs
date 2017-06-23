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
    abstract PeerId: Id
    abstract ConnectionId: Guid
    abstract Socket: Socket
    abstract EndPoint: IPEndPoint
    abstract Connected: ManualResetEvent
    abstract Sent: ManualResetEvent
    abstract Buffer: byte array
    abstract PendingRequests: PendingRequests
    abstract Response: IResponseBuilder with get, set
    abstract FinishResponse: unit -> unit
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
        | exn ->
          exn.Message
          |> Logger.err (tag "connectCallback"))

  // ** beginConnect

  let private beginConnect (state: IState) =
    state.Socket.BeginConnect(state.EndPoint, connectCallback(), state) |> ignore
    state.Connected.WaitOne() |> ignore

  // *** receiveCallback

  let private receiveCallback() =
    AsyncCallback(fun (ar: IAsyncResult) ->
      try
        // Retrieve the state object and the client socket
        // from the asynchronous state object.
        let state = ar.AsyncState :?> IState

        // Read data from the remote device.
        let bytesRead = state.Socket.EndReceive(ar)

        if bytesRead > 0 then
          // this is a fresh response, so we start of nice and neat
          if isNull state.Response then
            let response = ResponseBuilder.create()
            state.Response <- response
            state.Response.Start state.Buffer 0L // start a new response at the start of the buffer

            state.Response.Append
              state.Buffer
              RequestBuilder.HeaderSize
              (int64 bytesRead - RequestBuilder.HeaderSize)

          elif not state.Response.IsFinished then
            let required = state.Response.BodyLength - state.Response.Position
            if required >= int64 Core.BUFFER_SIZE && bytesRead = Core.BUFFER_SIZE then
              // just add the entire current buffer
              state.Response.Append state.Buffer 0L (int64 Core.BUFFER_SIZE)
            elif required <= int64 bytesRead then
              state.Response.Append state.Buffer 0L required
              if state.Response.IsFinished then
                state.FinishResponse()
              let remaining = int64 bytesRead - required
              if remaining > 0L && remaining >= RequestBuilder.HeaderSize then
                let response = ResponseBuilder.create()
                state.Response <- response
                // start the new response after `require` offset
                state.Response.Start state.Buffer required
                state.Response.Append state.Buffer (required + RequestBuilder.HeaderSize) remaining
            else state.Response.Append state.Buffer 0L (int64 bytesRead)
          if state.Response.IsFinished then
            state.FinishResponse()
        beginReceive state
      with
        | exn ->
          exn.Message
          |> Logger.err (tag "receiveCallback"))

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
        exn.Message
        |> Logger.err (tag "beginReceive")

  let private sendCallback (ar: IAsyncResult) =
    try
      let state = ar.AsyncState :?> IState

      // Complete sending the data to the remote device.
      let bytesSent = state.Socket.EndSend(ar)

      printfn "Sent %d bytes to server." bytesSent

      // Signal that all bytes have been sent.
      state.Sent.Set() |> ignore
    with
      | exn ->
        exn.Message
        |> printfn "exn: %s"

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
      state.Sent.WaitOne() |> ignore
    with
      | exn ->
        exn.Message
        |> printfn "exn: %s"

  let private makeState id peer endpoint client =
      let buffer = Array.zeroCreate Core.BUFFER_SIZE
      let connected = new ManualResetEvent(false)
      let sent = new ManualResetEvent(false)
      let mutable response = null
      let pending = PendingRequests()
      let subscriptions = Subscriptions()
      { new IState with
          member state.PeerId
            with get () = peer

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

          member state.Response
            with get () = response
             and set builder = response <- builder

          member state.FinishResponse() =
            match response with
            | null -> ()
            | builder ->
              response <- null
              builder.Finish()
              |> TcpClientEvent.Response
              |> Observable.onNext subscriptions
              builder.Dispose()

          member state.Subscriptions
            with get () = subscriptions

          member state.Dispose() =
            Socket.dispose client
            if not (isNull response) then
              response.Dispose()
              response <- null
        }

  let create (options: ClientConfig) =
    let id = Guid.NewGuid()

    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    let state = makeState id options.PeerId endpoint client

    let checker =
      options.PeerId
      |> TcpClientEvent.Disconnected
      |> Socket.checkState client state.Subscriptions

    { new IClient with
        member socket.Request(request: Request) =
          send state request

        member socket.Start() =
          try
            beginConnect state
            Async.Start(checker, cts.Token)
            beginReceive state
            Either.nothing
          with
            | exn ->
              exn.Message
              |> Error.asSocketError (tag "Start")
              |> Either.fail

        member socket.PeerId
          with get () = options.PeerId

        member socket.ConnectionId
          with get () = id

        member socket.Subscribe (callback: TcpClientEvent -> unit) =
          Observable.subscribe callback state.Subscriptions

        member socket.Dispose () =
          cts.Cancel()
          state.Dispose() }
