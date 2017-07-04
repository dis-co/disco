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

// * TcpServer

//  _____         ____
// |_   _|__ _ __/ ___|  ___ _ ____   _____ _ __
//   | |/ __| '_ \___ \ / _ \ '__\ \ / / _ \ '__|
//   | | (__| |_) |__) |  __/ |   \ V /  __/ |
//   |_|\___| .__/____/ \___|_|    \_/ \___|_|
//          |_|

module TcpServer =

  // ** tag

  let private tag (str: string) = String.format "TcpServer.{0}" str

  // ** Subscriptions

  type private Subscriptions = ConcurrentDictionary<Guid,IObserver<TcpServerEvent>>

  // ** IConnection

  type private IConnection =
    inherit IDisposable
    abstract Socket: Socket
    abstract Request: Request -> unit
    abstract Respond: Response -> unit
    abstract Id: Guid
    abstract IPAddress: IPAddress
    abstract Port: int
    abstract Buffer: byte array
    abstract RequestBuilder: IRequestBuilder
    abstract Subscriptions: Subscriptions

  // ** Connections

  type private Connections = ConcurrentDictionary<Guid,IConnection>

  // ** IState

  type private IState =
    inherit IDisposable
    abstract DoneSignal: ManualResetEvent
    abstract Connections: Connections
    abstract Subscriptions: Subscriptions
    abstract Listener: Socket

  // ** SharedState module

  module private SharedState =

    let create (socket: Socket) =
      let signal = new ManualResetEvent(false)
      let connections = Connections()
      let subscriptions = Subscriptions()

      { new IState with
          member state.DoneSignal
            with get () = signal

          member state.Connections
            with get () = connections

          member state.Subscriptions
            with get () = subscriptions

          member state.Listener
            with get () = socket

          member state.Dispose() =
            Socket.dispose socket }

  // ** Connection module

  module private Connection =

    // ** sendCallback

    let private sendCallback (result: IAsyncResult) =
      try
        // Retrieve the socket from the state object.
        let handler = result.AsyncState :?> Socket

        // Complete sending the data to the remote device.
        handler.EndSend(result) |> ignore
      with
        | :? ObjectDisposedException -> ()
        | exn -> exn.Message |> Logger.err (tag "sendCallback")

    // ** send

    let private send (msg: ISocketMessage) (socket: Socket) id subscriptions =
      try
        let payload = RequestBuilder.serialize msg
        socket.BeginSend(
          payload,
          0,
          payload.Length,
          SocketFlags.None,
          AsyncCallback(sendCallback),
          socket)
        |> ignore
      with
        | :? ObjectDisposedException ->
          id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext subscriptions
        | exn ->
          exn.Message
          |> Logger.err (tag "Connection.sendCallback")
          id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext subscriptions

    // ** beginReceive

    let private beginReceive (connection: IConnection) callback =
      connection.Socket.BeginReceive(
        connection.Buffer,              // buffer to write to
        0,                              // offset in buffer
        Core.BUFFER_SIZE,               // size of internal buffer
        SocketFlags.None,               // no flags
        AsyncCallback(callback),        // when done, invoke this callback
        connection)                     // pass-on connection into callback
      |> ignore

    // ** receiveCallback

    let rec private receiveCallback (result: IAsyncResult) =
      // Retrieve the state object and the handler socket
      // from the asynchronous state object.
      let connection = result.AsyncState :?> IConnection

      try
        // Read data from the client socket.
        let bytesRead = connection.Socket.EndReceive(result)
        connection.RequestBuilder.Process bytesRead

        // keep trying to get more
        beginReceive connection receiveCallback
      with
        | :? ObjectDisposedException ->
          connection.Id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext connection.Subscriptions
        | exn ->
          exn.Message
          |> Logger.err (tag "receiveCallback")
          connection.Id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext connection.Subscriptions

    // ** create

    let create (id: Guid) (state: IState) (socket: Socket)  =
      let cts = new CancellationTokenSource()
      let endpoint = socket.RemoteEndPoint :?> IPEndPoint

      let buffer = Array.zeroCreate Core.BUFFER_SIZE

      let pending = ConcurrentDictionary<Guid,Request>()

      let builder = RequestBuilder.create buffer <| fun request client body ->
        let ev =
          match pending.TryRemove(request) with
          | true, _ ->
            body
            |> Response.create request client
            |> TcpServerEvent.Response
          | false, _ ->
            body
            |> Request.make request client
            |> TcpServerEvent.Request
        Observable.onNext state.Subscriptions ev

      let connection =
        { new IConnection with
            member connection.Socket
              with get () = socket

            member connection.Request (request: Request) =
              // if false, the dictionary already contains reuqest
              pending.TryAdd(request.RequestId, request) |> ignore
              send request socket id state.Subscriptions

            member connection.Respond (response: Response) =
              send response socket id state.Subscriptions

            member connection.Id
              with get () = id

            member connection.IPAddress
              with get () = endpoint.Address

            member connection.Port
              with get () = endpoint.Port

            member connection.Buffer
              with get () = buffer

            member connection.RequestBuilder
              with get () = builder

            member connection.Subscriptions
              with get () = state.Subscriptions

            member connection.Dispose() =
              try
                cts.Cancel()
              with
                | _ -> ()
              try
                socket.Shutdown(SocketShutdown.Both)
                socket.Close()
              with
                | _ -> ()
              socket.Dispose() }

      let checker =
        Socket.checkState
          connection.Socket
          connection.Subscriptions
          None
          (connection.Id |> TcpServerEvent.Disconnect |> Some)

      Async.Start(checker, cts.Token)
      beginReceive connection receiveCallback
      connection

  // ** Server module

  module private Server =

    //  ____
    // / ___|  ___ _ ____   _____ _ __
    // \___ \ / _ \ '__\ \ / / _ \ '__|
    //  ___) |  __/ |   \ V /  __/ |
    // |____/ \___|_|    \_/ \___|_|


    // *** acceptCallback

    let private acceptCallback()  =
      AsyncCallback(fun (result: IAsyncResult) ->
        let state = result.AsyncState :?> IState
        try
          let socket = state.Listener.EndAccept(result)

          state.DoneSignal.Set() |> ignore

          let buffer = Array.zeroCreate 16
          socket.Receive(buffer, 0, 16, SocketFlags.None) |> ignore

          let guid = Guid buffer
          let connection = Connection.create guid state socket

          match state.Connections.TryRemove(connection.Id) with
          | true, connection -> connection.Dispose()
          | _ -> ()

          state.Connections.TryAdd(connection.Id, connection)
          |> ignore

          guid
          |> String.format "New connection from {0}"
          |> Logger.info (tag "acceptCallback")
        with
          | :? ObjectDisposedException -> ()
          | exn ->
            exn.Message
            |> Logger.err (tag "acceptCallback"))

    // *** acceptor

    let private acceptor (state: IState) () =
      while true do
        state.DoneSignal.Reset() |> ignore
        state.Listener.BeginAccept(acceptCallback(), state) |> ignore
        state.DoneSignal.WaitOne() |> ignore

    // *** startAccepting

    let startAcceptingConnections (state: IState) =
      let thread = Thread(ThreadStart(acceptor state))
      thread.Start()
      { new IDisposable with
          member self.Dispose() = try thread.Abort() with | _ -> () }

    // *** cleanUp

    let cleanUp (connections: Connections) = function
      | TcpServerEvent.Disconnect id ->
        match connections.TryRemove id with
        | true, connection -> connection.Dispose()
        | false, _ -> ()
      | _ -> ()

  // ** create

  let create (options: ServerConfig) =
    let listener = Socket.createTcp()
    let endpoint = IPEndPoint(options.Listen.toIPAddress(), int options.Port)

    let state = SharedState.create listener
    let cleaner = Observable.subscribe (Server.cleanUp state.Connections) state.Subscriptions

    let mutable acceptor = Unchecked.defaultof<IDisposable>

    { new IServer with
        member Server.Id
          with get () = options.ServerId

        member server.Start() =
          either {
            try
              do! Network.ensureAvailability options.Listen options.Port
              do listener.Bind(endpoint)
              do listener.Listen(100)
              acceptor <- Server.startAcceptingConnections state
            with
              | exn ->
                return!
                  exn.Message
                  |> Error.asSocketError (tag "Start")
                  |> Either.fail
          }

        member server.Request (client: Guid) (request: Request) =
          try
            state.Connections.[client].Request request
          with
            | exn -> exn.Message |> Logger.err (tag "Send")

        member server.Respond (response: Response) =
          try
            state.Connections.[response.PeerId].Respond response
          with
            | exn -> exn.Message |> Logger.err (tag "Send")

        member server.Subscribe (callback: TcpServerEvent -> unit) =
          Observable.subscribe callback state.Subscriptions

        member server.Dispose() =
          tryDispose cleaner ignore

          for KeyValue(_,connection) in state.Connections.ToArray() do
            tryDispose connection ignore

          tryDispose acceptor ignore
          tryDispose state ignore }
