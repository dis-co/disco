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
    abstract ResponseId: Guid option with get, set
    abstract ResponseLength: int with get, set
    abstract Response: MemoryStream
    abstract SetResponseId: offset:int -> unit
    abstract SetResponseLength: offset:int -> unit
    abstract CopyData: offset:int -> count:int -> unit
    abstract FinishResponse: unit -> unit
    abstract Subscriptions: Subscriptions

  // ** connectCallback

  let private connectCallback() =
    AsyncCallback(fun (ar: IAsyncResult) ->
      try
        let state = ar.AsyncState :?> IState
        state.Socket.EndConnect(ar)     // complete the connection
        (state.PeerId, state.ConnectionId)
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
          if state.Response.Position = 0L && Option.isNone state.ResponseId then
            state.SetResponseLength 0
            state.SetResponseId Core.MSG_LENGTH_OFFSET
            state.CopyData Core.HEADER_OFFSET (bytesRead - Core.HEADER_OFFSET)
          elif int state.Response.Position < state.ResponseLength then
            let required = state.ResponseLength - int state.Response.Position
            if required >= Core.BUFFER_SIZE && bytesRead = Core.BUFFER_SIZE then
              // just add the entire current buffer
              state.CopyData 0 Core.BUFFER_SIZE
            elif required <= bytesRead then
              state.CopyData 0 required
              if int state.Response.Position = state.ResponseLength then
                state.FinishResponse()
              let remaining = bytesRead - required
              if remaining > 0 && remaining >= Core.HEADER_OFFSET then
                state.SetResponseLength required
                state.SetResponseId (required + Core.MSG_LENGTH_OFFSET)
                state.CopyData (required + Core.HEADER_OFFSET) remaining
            else state.CopyData 0 bytesRead
          if state.ResponseLength = int state.Response.Position then
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
      let header =
        Array.append
          (BitConverter.GetBytes request.Body.Length)
          (request.RequestId.ToByteArray())

      let payload = Array.append header request.Body

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
      let mutable responseId = None
      let mutable responseLength = 0
      let response = new MemoryStream()
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

          member state.Response
            with get () = response

          member state.ResponseId
            with get () = responseId
              and set id = responseId <- id

          member state.ResponseLength
            with get ()  = responseLength
              and set len = responseLength <- len

          member state.SetResponseLength(offset: int) =
            responseLength <- BitConverter.ToInt32(buffer, offset)

          member state.SetResponseId(offset: int) =
            responseId <-
              let intermediary = Array.zeroCreate Core.ID_LENGTH_OFFSET
              Array.blit buffer offset intermediary 0 Core.ID_LENGTH_OFFSET
              intermediary
              |> Guid
              |> Some

          member state.CopyData (offset: int) (count: int) =
            response.Write(buffer, offset, count)

          member state.FinishResponse() =
            match responseId with
            | Some guid ->
              { ConnectionId = id
                RequestId = guid
                Body = response.ToArray() }
              |> TcpClientEvent.Response
              |> Observable.onNext subscriptions
              responseLength <- 0
              responseId <- None
              response.Seek(0L, SeekOrigin.Begin) |> ignore
            | None -> ()

          member state.Subscriptions
            with get () = subscriptions

          member state.Dispose() =
            Socket.dispose client
            response.Close()
            response.Dispose()
        }

  let create (options: ClientConfig) =
    let id = Guid.NewGuid()

    let cts = new CancellationTokenSource()
    let endpoint = IPEndPoint(options.PeerAddress.toIPAddress(), int options.PeerPort)
    let client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    let state = makeState id options.PeerId endpoint client

    let checker =
      (options.PeerId, id)
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
