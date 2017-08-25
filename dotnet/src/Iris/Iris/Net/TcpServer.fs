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

  // ** PendingRequests

  type PendingRequests = ConcurrentDictionary<Guid,Request>

  // ** IConnection

  type private IConnection =
    inherit IDisposable
    abstract Socket: Socket
    abstract Id: Guid
    abstract IPAddress: IPAddress
    abstract Port: int
    abstract PendingRequests: PendingRequests
    abstract RequestBuilder: IRequestBuilder
    abstract Subscriptions: Subscriptions
    abstract Send: byte array -> unit

  // ** Connections

  type private Connections = ConcurrentDictionary<Guid,IConnection>

  // ** BufferedArgs

  type BufferedArgs = ConcurrentBag<SocketAsyncEventArgs>

  // ** IState

  type private IState =
    inherit IDisposable
    abstract Connections: Connections
    abstract Subscriptions: Subscriptions
    abstract BufferManager: IBufferManager
    abstract BufferedArgs: BufferedArgs
    abstract Listener: Socket
    abstract EndPoint: IPEndPoint

  // ** SharedState module

  module private SharedState =

    // *** cleanUp

    let cleanUp (connections: Connections) = function
      | TcpServerEvent.Disconnect id ->
        match connections.TryRemove id with
        | true, connection ->
          connection.Id
          |> String.format "removing connection: {0}"
          |> Logger.info (tag "cleanUp")
          connection.Dispose()
        | false, _ -> ()
      | _ -> ()

    // *** create

    let create (options: ServerConfig) =
      let socket = Socket.createTcp()
      let endpoint = IPEndPoint(options.Listen.toIPAddress(), int options.Port)

      let connections = Connections()
      let subscriptions = Subscriptions()

      let args = BufferedArgs()
      for _ in 1 .. Core.MAX_CONNECTIONS do
        new SocketAsyncEventArgs()
        |> args.Add

      let manager = BufferManager.create Core.MAX_CONNECTIONS Core.BUFFER_SIZE

      let cleaner =
        connections
        |> cleanUp
        |> flip Observable.subscribe subscriptions

      { new IState with
          member state.BufferManager
            with get () = manager

          member state.BufferedArgs
            with get () = args

          member state.Connections
            with get () = connections

          member state.Subscriptions
            with get () = subscriptions

          member state.EndPoint
            with get () = endpoint

          member state.Listener
            with get () = socket

          member state.Dispose() =
            tryDispose cleaner ignore

            for KeyValue(_,connection) in state.Connections.ToArray() do
              tryDispose connection ignore

            Socket.dispose socket }

  // ** Connection module

  module private Connection =

    // *** create

    let create (id: Guid) (state: IState) (socket: Socket)  =
      let cts = new CancellationTokenSource()
      let endpoint = socket.RemoteEndPoint :?> IPEndPoint
      let pending = ConcurrentDictionary<Guid,Request>()
      let stream = new NetworkStream(socket)

      let builder = RequestBuilder.create <| fun request client body ->
        match pending.TryRemove(request) with
        | true, _ ->
          body
          |> Response.create request client
          |> TcpServerEvent.Response
          |> Observable.onNext state.Subscriptions
        | false, _ ->
          body
          |> Request.make request client
          |> TcpServerEvent.Request
          |> Observable.onNext state.Subscriptions

      //                     _ _
      //  ___  ___ _ __   __| (_)_ __   __ _
      // / __|/ _ \ '_ \ / _` | | '_ \ / _` |
      // \__ \  __/ | | | (_| | | | | | (_| |
      // |___/\___|_| |_|\__,_|_|_| |_|\__, |
      //                               |___/
      let rec sendLoop (inbox: MailboxProcessor<byte[]>) = async {
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
      let receiveLoop = async {
          let mutable run = true
          while run do
            try
              let data = stream.ReadByte()
              if data <> -1
              then data |> byte |> builder.Write
              else // once the stream returns -1, the underlying stream has ended
                id
                |> String.format "Reached end of Stream. Disconnected. {0}"
                |> Logger.info (tag "receiveLoop")
                id
                |> TcpServerEvent.Disconnect
                |> Observable.onNext state.Subscriptions
                run <- false
            with
              | :? IOException -> run <- false
              | exn ->
                run <- false
                id
                |> TcpServerEvent.Disconnect
                |> Observable.onNext state.Subscriptions
                Logger.err (tag "receiveLoop") exn.Message
        }

      let sender = MailboxProcessor.Start(sendLoop, cts.Token)
      Async.Start(receiveLoop, cts.Token)

      { new IConnection with
          member connection.Socket
            with get () = socket

          member connection.PendingRequests
            with get () = pending

          member connection.Id
            with get () = id

          member connection.IPAddress
            with get () = endpoint.Address

          member connection.Port
            with get () = endpoint.Port

          member connection.RequestBuilder
            with get () = builder

          member connection.Subscriptions
            with get () = state.Subscriptions

          member connection.Send (data: byte array) =
            sender.Post data

          member connection.Dispose() =
            try cts.Cancel() with | _ -> ()
            Socket.dispose socket
            dispose stream
            dispose builder }

  // ** Server module

  module private rec Server =

    //  ____
    // / ___|  ___ _ ____   _____ _ __
    // \___ \ / _ \ '__\ \ / / _ \ '__|
    //  ___) |  __/ |   \ V /  __/ |
    // |____/ \___|_|    \_/ \___|_|

    // *** withState

    let private withState (state: IState) f =
      match state.BufferedArgs.TryTake() with
      | (true, args) -> f args
      | (false, _) ->
        "Error: too many connections"
        |> Logger.err (tag "withState")

    // *** returnArgs

    let private returnArgs (state: IState) (args: SocketAsyncEventArgs) =
      args.UserToken <- null
      args.AcceptSocket <- null
      args.SetBuffer(null, 0, 0)
      state.BufferedArgs.Add args

    // *** onError

    let private onError location state (args: SocketAsyncEventArgs) =
      let msg =
        if args.BytesTransferred = 0 then
          "Connection closed by peer"
        else
          String.format "{0} error in Socket operation" args.SocketError
      do returnArgs state args
      Logger.err (tag location) msg

    // *** onSend

    let private onSend (args: SocketAsyncEventArgs) =
      let listener, _, state = args.UserToken :?> (IDisposable * IConnection * IState)
      if args.SocketError <> SocketError.Success then
        do onError "onSend" state args
      else
        args.BytesTransferred
        |> String.format "sent {0} bytes"
        |> Logger.debug (tag "onSend")
        do returnArgs state args
      do dispose listener

    // *** sendAsync

    let private sendAsync (state: IState) (connection: IConnection) (bytes: byte array) =
      withState state <| fun args ->
        let listener = args.Completed.Subscribe onSend
        do args.SetBuffer(bytes, 0, bytes.Length)
        args.UserToken <- listener, connection, state
        try
          match connection.Socket.SendAsync args with
          | true -> ()
          | false -> onSend args
        with
          | :? ObjectDisposedException -> ()
          | exn -> Logger.err (tag "sendAsync") exn.Message

    // *** onConnection

    let private onConnection (args: SocketAsyncEventArgs) =
      let listener, socket, state = args.UserToken :?> (IDisposable * Socket * IState)
      dispose listener
      if args.SocketError = SocketError.Success then
        let guid = Guid args.Buffer
        let connection = Connection.create guid state socket

        match state.Connections.TryRemove(connection.Id) with
        | true, connection ->
          connection.Id
          |> String.format "removing connection: {0}"
          |> Logger.info (tag "onConnection")
          connection.Dispose()
        | _ -> ()

        state.Connections.TryAdd(connection.Id, connection) |> ignore

        do Core.CONNECTED |> sendAsync state connection

        guid
        |> String.format "New connection from {0}"
        |> Logger.info (tag "acceptCallback")

        do returnArgs state args
      else
        do onError "onConnection" state args

    // *** initializeAsync

    let private initializeAsync (state: IState) (socket: Socket) =
      withState state <| fun args ->
        let idbuf = Array.zeroCreate Core.ID_SIZE
        do args.SetBuffer(idbuf, 0, Core.ID_SIZE)
        let listener = args.Completed.Subscribe onConnection
        args.UserToken <- listener, socket, state
        try
          match socket.ReceiveAsync(args) with
          | true -> ()
          | false -> do onConnection args
        with
          | :? ObjectDisposedException -> ()
          | exn -> Logger.err (tag "initializeAsync") exn.Message

    // *** onAccept

    let private onAccept (args: SocketAsyncEventArgs) =
      let listener, state = args.UserToken :?> (IDisposable * IState)
      dispose listener
      let socket = args.AcceptSocket
      do initializeAsync state socket
      do returnArgs state args
      do acceptAsync state

    // *** acceptAsync

    let acceptAsync (state: IState) =
      withState state <| fun args ->
        let listener = args.Completed.Subscribe onAccept
        args.UserToken <- listener, state
        try
          match state.Listener.AcceptAsync(args) with
          | true -> ()
          | false -> do onAccept args
        with
          | :? ObjectDisposedException -> ()
          | exn -> Logger.err (tag "acceptAsync") exn.Message

  // ** create

  let create (options: ServerConfig) =
    let state = SharedState.create options
    { new ITcpServer with
        member Server.Id
          with get () = options.ServerId

        member server.Start() =
          either {
            try
              do! Network.ensureAvailability options.Listen options.Port
              do state.Listener.Bind(state.EndPoint)
              do state.Listener.Listen(Core.MAX_CONNECTIONS)
              do Server.acceptAsync state
            with
              | exn ->
                return!
                  exn.Message
                  |> Error.asSocketError (tag "Start")
                  |> Either.fail
          }

        member server.Request (client: Guid) (request: Request) =
          try
            request
            |> RequestBuilder.serialize
            |> state.Connections.[client].Send
          with
            | exn ->
              String.Format("{0} {1}", exn.Message, request.PeerId)
              |> Logger.err (tag "Request")

              state.Connections.Keys
              |> sprintf "current peers: %A"
              |> Logger.err (tag "Request")

        member server.Respond (response: Response) =
          try
            response
            |> RequestBuilder.serialize
            |> state.Connections.[response.PeerId].Send
          with
            | exn ->
              String.Format("{0} {1}", exn.Message, response.PeerId)
              |> Logger.err (tag "Respond")

              state.Connections.Keys
              |> sprintf "current peers: %A"
              |> Logger.err (tag "Respond")

        member server.Subscribe (callback: TcpServerEvent -> unit) =
          Observable.subscribe callback state.Subscriptions

        member server.Dispose() =
          tryDispose state ignore }
