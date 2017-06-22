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
    abstract Send: Response -> unit
    abstract Id: Guid
    abstract IPAddress: IPAddress
    abstract Port: int
    abstract Buffer: byte array
    abstract RequestId: Guid option with get, set
    abstract RequestLength: int with get, set
    abstract Request: MemoryStream
    abstract SetRequestLength: offset:int -> unit
    abstract SetRequestId: offset:int -> unit
    abstract CopyData: offset:int -> count:int -> unit
    abstract FinishRequest: unit -> unit
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

    let sendCallback (result: IAsyncResult) =
      try
        // Retrieve the socket from the state object.
        let handler = result.AsyncState :?> Socket

        // Complete sending the data to the remote device.
        let bytesSent = handler.EndSend(result)

        printfn "Sent %d bytes to client." bytesSent
      with
        | :? ObjectDisposedException -> ()
        | exn ->
          exn.Message
          |> printfn "sendCallback: exn: %s"

    let send (response: Response) (socket: Socket) id subscriptions =
      try
        let header =
          Array.append
            (BitConverter.GetBytes response.Body.Length)
            (response.RequestId.ToByteArray())
        let payload = Array.append header response.Body
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

    let private beginReceive (connection: IConnection) callback =
      connection.Socket.BeginReceive(
        connection.Buffer,              // buffer to write to
        0,                              // offset in buffer
        Core.BUFFER_SIZE,          // size of internal buffer
        SocketFlags.None,               // no flags
        AsyncCallback(callback),        // when done, invoke this callback
        connection)                     // pass-on connection into callback
      |> ignore

    let rec receiveCallback (result: IAsyncResult) =
      let mutable content = String.Empty

      // Retrieve the state object and the handler socket
      // from the asynchronous state object.
      let connection = result.AsyncState :?> IConnection

      try
        // Read data from the client socket.
        let bytesRead = connection.Socket.EndReceive(result)

        if bytesRead > 0 then

          printfn "received %d bytes" bytesRead

          // this is a new request, so we determine the length of the request
          // and the request id
          if connection.Request.Position = 0L && Option.isNone connection.RequestId then
            connection.SetRequestLength (offset = 0)
            connection.SetRequestId Core.MSG_LENGTH_OFFSET
            connection.CopyData Core.HEADER_OFFSET (bytesRead - Core.HEADER_OFFSET)
          elif int connection.Request.Position < connection.RequestLength then
            // we are currently working on a request, so we add to request array whatever data is
            // available
            let required = connection.RequestLength - int connection.Request.Position
            if required >= Core.BUFFER_SIZE && bytesRead = Core.BUFFER_SIZE then
              // just add the entire current buffer
              connection.CopyData 0 Core.BUFFER_SIZE
            elif required <= bytesRead then
              connection.CopyData 0 required
              if int connection.Request.Position = connection.RequestLength then
                connection.FinishRequest()
              let remaining = bytesRead - required
              if remaining > 0 && remaining >= Core.HEADER_OFFSET then
                connection.SetRequestLength required
                connection.SetRequestId (required + Core.MSG_LENGTH_OFFSET)
                connection.CopyData (required + Core.HEADER_OFFSET) remaining
            else connection.CopyData 0 bytesRead

          if connection.RequestLength = int connection.Request.Position then
            connection.FinishRequest()

        // keep trying to get more
        beginReceive connection receiveCallback
      with
        | :? ObjectDisposedException ->
          connection.Id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext connection.Subscriptions
        | exn ->
          exn.Message
          |> printfn "EXN: receiveCallback: %s"
          connection.Id
          |> TcpServerEvent.Disconnect
          |> Observable.onNext connection.Subscriptions

    let create (state: IState) (socket: Socket)  =
      let id = Guid.NewGuid()
      let cts = new CancellationTokenSource()
      let endpoint = socket.RemoteEndPoint :?> IPEndPoint

      let buffer = Array.zeroCreate Core.BUFFER_SIZE

      let mutable requestLength = 0
      let mutable requestId = None
      let request = new MemoryStream()

      let connection =
        { new IConnection with
            member connection.Socket
              with get () = socket

            member connection.Send (response: Response) =
              send response socket id state.Subscriptions

            member connection.Id
              with get () = id

            member connection.IPAddress
              with get () = endpoint.Address

            member connection.Port
              with get () = endpoint.Port

            member connection.Buffer
              with get () = buffer

            member connection.RequestId
              with get () = requestId
               and set id = requestId <- id

            member connection.RequestLength
              with get () = requestLength
               and set len = requestLength <- len

            member connection.Request
              with get () = request

            member connection.SetRequestLength(offset: int) =
              requestLength <- BitConverter.ToInt32(buffer, offset)

            member connection.SetRequestId(offset: int) =
              requestId <-
                let intermediary = Array.zeroCreate Core.ID_LENGTH_OFFSET
                Array.blit buffer offset intermediary 0 Core.ID_LENGTH_OFFSET
                intermediary
                |> Guid
                |> Some

            member connection.CopyData (offset: int) (count: int) =
              connection.Request.Write(buffer, offset, count)

            member connection.FinishRequest() =
              match requestId with
              | Some guid ->
                { ConnectionId = id
                  RequestId = guid
                  Body = request.ToArray() }
                |> TcpServerEvent.Request
                |> Observable.onNext connection.Subscriptions
                requestLength <- 0
                requestId <- None
                request.Seek(0L, SeekOrigin.Begin) |> ignore
              | None -> ()

            member connection.Subscriptions
              with get () = state.Subscriptions

            member connection.Dispose() =
              printfn "disposing %O" id
              try
                cts.Cancel()
                cts.Dispose()
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
          (TcpServerEvent.Disconnect connection.Id)

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
        state.DoneSignal.Set() |> ignore
        try
          let connection =
            result
            |> state.Listener.EndAccept
            |> Connection.create state

          while not (state.Connections.TryAdd(connection.Id, connection)) do
            ignore ()
        with
          | exn ->
            exn.Message
            |> Logger.err (tag "Server.acceptCallback"))

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
          member self.Dispose() =
            try
              thread.Abort()
            with | _ -> () }

    // *** cleanUp

    let cleanUp (connections: Connections) = function
      | TcpServerEvent.Disconnect id ->
        match connections.TryRemove id with
        | true, connection -> connection.Dispose()
        | false, _ -> ()
      | _ -> ()

  // ** create

  let create (options: ServerConfig) =
    let listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
    let endpoint = IPEndPoint(options.Listen.toIPAddress(), int options.Port)

    let state = SharedState.create listener
    let cleaner = Observable.subscribe (Server.cleanUp state.Connections) state.Subscriptions
    let mutable acceptor = Unchecked.defaultof<IDisposable>

    { new IServer with
        member server.Start() =
          try
            listener.Bind(endpoint)
            listener.Listen(100)

            acceptor <- Server.startAcceptingConnections state
            Either.nothing
          with
            | exn ->
              exn.Message
              |> Error.asSocketError (tag "Start")
              |> Either.fail

        member server.Respond (response: Response) =
          try
            state.Connections.[response.ConnectionId].Send response
          with
            | exn ->
              exn.Message
              |> Logger.err (tag "Send")

        member server.Subscribe (callback: TcpServerEvent -> unit) =
          Observable.subscribe callback state.Subscriptions

        member server.Dispose() =
          cleaner.Dispose()

          for KeyValue(_,connection) in state.Connections.ToArray() do
            connection.Dispose()

          acceptor.Dispose()
          state.Dispose() }
