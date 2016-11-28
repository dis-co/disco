namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Service
open FSharpx.Functional
open Fleck

// * WebSockets

module WebSockets =

  // ** tag

  [<Literal>]
  let private tag = "WebSocket"

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,IWebSocketConnection>

  // ** IWsServerCallbacks

  type IWsServerCallbacks =
    abstract OnOpen    : Id -> unit
    abstract OnClose   : Id -> unit
    abstract OnError   : Id -> Exception -> unit
    abstract OnMessage : Id -> StateMachine -> unit

  // ** IWsServer

  type IWsServer =
    inherit System.IDisposable
    abstract Send         : Id -> StateMachine -> Either<IrisError,unit>
    abstract Broadcast    : StateMachine -> Either<IrisError list,unit>
    abstract BuildSession : Id -> Session -> Either<IrisError,Session>

  // ** getConnectionId

  let private getConnectionId (socket: IWebSocketConnection) : Id =
    string socket.ConnectionInfo.Id |> Id

  // ** buildSession

  let private buildSession (connections: Connections)
                           (socketId: Id)
                           (session: Session) :
                           Either<IrisError,Session> =
    match connections.TryGetValue socketId with
    | true, socket ->
      let ua =
        if socket.ConnectionInfo.Headers.ContainsKey("User-Agent") then
          socket.ConnectionInfo.Headers.["User-Agent"]
        else
          "<no user agent specified>"
      { session with
          // TODO: Set the sessions as unauthorized?
          IpAddress = IpAddress.Parse socket.ConnectionInfo.ClientIpAddress
          UserAgent = ua }
      |> Either.succeed
    | false, _ ->
      sprintf "No socket open with id %O" socketId
      |> SocketError
      |> Either.fail

  // ** send

  /// ## send
  ///
  /// Send a `StateMachine` command to the requested session.
  ///
  /// ### Signature:
  /// - sessionid: Id of session to send the command to
  /// - msg: StateMachine command to send
  ///
  /// Returns: Either<IrisError,unit>
  let private send (connections: Connections) (sid: Id) (msg: StateMachine) =
    match connections.TryGetValue(sid) with
    | true, socket ->
      try
        msg
        |> Binary.encode
        |> socket.Send
        |> ignore
        |> Either.succeed
      with
        | exn ->
          let _, _ = connections.TryRemove(sid)
          exn.Message
          |> SocketError
          |> Either.fail
    | false, _ ->
      sid
      |> string
      |> sprintf "could not send message to session %s. not found."
      |> SocketError
      |> Either.fail

  // ** broadcast

  /// ## Broadcast
  ///
  /// Send a `StateMachine` command to all open connections.
  ///
  /// ### Signature:
  /// - msg: StateMachine command to send
  ///
  /// Returns: unit
  let private broadcast (connections: Connections)
                        (msg: StateMachine) :
                        Either<IrisError list, unit> =
    let sendAsync (id: Id) = async {
        let result = send connections id msg
        return result
      }

    let result : IrisError list =
      connections.Keys
      |> Seq.map sendAsync
      |> Async.Parallel
      |> Async.RunSynchronously
      |> Array.fold
        (fun lst (result: Either<IrisError,unit>) ->
          match result with
          | Right _ -> lst
          | Left error -> error :: lst)
        []

    match result with
    | [ ] -> Right ()
    | _   -> Left result

  // ** onOpen

  /// ## onOpen
  ///
  /// Callback which is run when a new connection was established to a browser client. The
  /// connections session Id gets stored in the global sessions map for later use.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection that was newly established
  ///
  /// Returns: unit
  let private onOpen (id: Id)
                     (connections: Connections)
                     (callbacks: IWsServerCallbacks)
                     (socket: IWebSocketConnection) () =
    let sid = getConnectionId socket
    connections.AddOrUpdate(sid, socket, fun _ s -> s) |> ignore
    callbacks.OnOpen sid

    sprintf "Connection added: %O" sid
    |> Logger.debug id tag

  // ** onClose

  /// ## onClose
  ///
  /// Callback to be run when a connection was gracefully closed by the peer. The connections
  /// session Id will be removed from the global sessions map.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection which was closed
  ///
  /// Returns: unit
  let private onClose (id: Id)
                      (connections: Connections)
                      (callbacks: IWsServerCallbacks)
                      (socket: IWebSocketConnection) () =
    let sid = getConnectionId socket
    let success, _ = connections.TryRemove(sid)
    callbacks.OnClose sid

    sprintf "Removing connection: %O - Succeeded: %b" sid success
    |> Logger.debug id tag

  // ** onMessage

  /// ## onMessage
  ///
  /// Callback which responds to newly arriving messages on the WebSocket connections. If a handler
  /// is registered with this server, it is invoked with the result of the decoding process of the
  /// arrived byte buffer.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection which the new message arrived on
  /// - msg: Binary.Buffer byte array that was received
  ///
  /// Returns: unit
  let private onMessage (id: Id)
                        (callbacks: IWsServerCallbacks)
                        (socket: IWebSocketConnection)
                        (msg: Binary.Buffer) =
    /// get the Id for this connection
    let sid = getConnectionId socket

    /// decode the binary buffer as `StateMachine` command
    let entry : Either<IrisError,StateMachine> = Binary.decode msg

    /// Process the result of decoding the received message
    match entry with
    | Right command ->
      /// handle the result
      callbacks.OnMessage sid command

      /// log some debugging messages
      command
      |> string
      |> sprintf "command received from session %s and decoded as %s" (string sid)
      |> Logger.debug id tag

    | Left  error   ->
      /// log the error globally
      error
      |> string
      |> sprintf "command received from session %s could not be decoded: %s" (string sid)
      |> Logger.debug id tag

  // ** onError

  /// ## onError
  ///
  /// Callback invoked when a connection was closed due to an error. Removes the corresponding
  /// session from the global session map.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection that errored out
  /// - exn: Exception thrown on the connection
  ///
  /// Returns: unit
  let private onError (id: Id)
                      (connections: Connections)
                      (callbacks: IWsServerCallbacks)
                      (socket: IWebSocketConnection)
                      (exn: 'a when 'a :> Exception) =
    let sid = getConnectionId socket
    let success, _ = connections.TryRemove(sid)
    callbacks.OnError sid exn

    sprintf "Removing session %O because fo error: %s - Succeeded %b" sid exn.Message success
    |> Logger.err id tag

  // ** onNewSocket

  /// ## onNewSocket
  ///
  /// Register all callbacks on a newly created socket connection.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection to add handlers to
  ///
  /// Returns: unit
  let private onNewSocket (id: Id)
                          (connections: Connections)
                          (callbacks: IWsServerCallbacks)
                          (socket: IWebSocketConnection) =
    socket.OnOpen   <- new System.Action(onOpen id connections callbacks socket)
    socket.OnClose  <- new System.Action(onClose id connections callbacks socket)
    socket.OnBinary <- new System.Action<Binary.Buffer>(onMessage id callbacks socket)
    socket.OnError  <- new System.Action<exn>(onError id connections callbacks socket)

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  [<RequireQualifiedAccess>]
  module WsServer =

    let start (node: RaftNode) (callbacks: IWsServerCallbacks) =
      either {
        let connections = new Connections()
        let uri = sprintf "ws://%s:%d" (string node.IpAddr) node.WsPort

        uri
        |> sprintf "Starting WebSocketServer on: %s"
        |> Logger.debug node.Id tag

        let handler = onNewSocket node.Id connections callbacks
        let server = new WebSocketServer(uri)

        try
          server.Start(new System.Action<IWebSocketConnection>(handler))

          "WebSocketServer successfully started"
          |> Logger.debug node.Id tag

          return
            { new IWsServer with
                member self.Send (id: Id) (cmd: StateMachine) =
                  send connections id cmd

                member self.Broadcast (cmd: StateMachine) =
                  broadcast connections cmd

                member self.BuildSession (id: Id) (session: Session) =
                  buildSession connections id session

                member self.Dispose () =
                  for KeyValue(_, connection) in connections do
                    connection.Close()
                  connections.Clear()
                  dispose server }
        with
          | exn ->
            return!
              exn.Message
              |> SocketError
              |> Either.fail
      }
