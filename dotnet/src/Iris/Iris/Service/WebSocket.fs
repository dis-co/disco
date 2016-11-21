namespace Iris.Service

// * Imports

open System
open System.Threading
open Iris.Core
open Iris.Service
open FSharpx.Functional
open Fleck

#if MOCKSERVICE
type RaftServer = class end
#endif

// * WsServer

type WsServer(?config: IrisConfig, ?context: RaftServer) =
  let [<Literal>] tag = "WsServer"

  let nodeid =
    Config.getNodeId()
    |> Error.orExit id

  // ** uri

  let mutable onOpenCb    : Option<Session -> unit> = None
  let mutable onCloseCb   : Option<Id -> unit> = None
  let mutable onErrorCb   : Option<Id -> unit> = None
  let mutable onMessageCb : Option<Id -> StateMachine -> unit> = None

  let uri =
    match config with
    | Some config ->
      Config.getNodeId ()
      |> Either.bind (Config.findNode config)
      |> Error.orExit (fun node -> sprintf "ws://%s:%d" (string node.IpAddr) node.WsPort)
    | None ->
      sprintf "ws://%s:%d" Constants.DEFAULT_IP
        (Constants.WEB_SERVER_DEFAULT_PORT + Constants.SOCKET_SERVER_PORT_DIFF)


  let server = new WebSocketServer(uri)

  // ** sessions

  let mutable sessions : Map<Id,IWebSocketConnection> = Map.empty

  let getSessionId (socket: IWebSocketConnection) : Id =
    string socket.ConnectionInfo.Id |> Id

  let buildSession (socket: IWebSocketConnection) : Session =
    let ua =
      if socket.ConnectionInfo.Headers.ContainsKey("User-Agent") then
        socket.ConnectionInfo.Headers.["User-Agent"]
      else
        "<no user agent specified>"

    { Id        = getSessionId socket
    ; UserName  = ""
    ; IpAddress = IpAddress.Parse socket.ConnectionInfo.ClientIpAddress
    ; UserAgent = ua }

  // ** onOpen

  //   ___
  //  / _ \ _ __   ___ _ __
  // | | | | '_ \ / _ \ '_ \
  // | |_| | |_) |  __/ | | |
  //  \___/| .__/ \___|_| |_|
  //       |_|

  /// ## onOpen
  ///
  /// Callback which is run when a new connection was established to a browser client. The
  /// connections session Id gets stored in the global sessions map for later use.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection that was newly established
  ///
  /// Returns: unit
  let onOpen (socket: IWebSocketConnection) () =
    let session : Session = buildSession socket
    let sid = getSessionId socket
    sessions <- Map.add sid socket sessions
    Option.map (fun cb -> cb session) onOpenCb |> ignore

    socket
    |> getSessionId
    |> string
    |> sprintf "new session was opened: %s"
    |> Logger.debug nodeid tag


  // ** onClose

  //   ____ _
  //  / ___| | ___  ___  ___
  // | |   | |/ _ \/ __|/ _ \
  // | |___| | (_) \__ \  __/
  //  \____|_|\___/|___/\___|

  /// ## onClose
  ///
  /// Callback to be run when a connection was gracefully closed by the peer. The connections
  /// session Id will be removed from the global sessions map.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection which was closed
  ///
  /// Returns: unit
  let onClose (socket: IWebSocketConnection) () =
    let session = getSessionId socket
    sessions <- Map.remove session sessions
    Option.map (fun cb -> cb session) onCloseCb |> ignore

    session
    |> string
    |> sprintf "session was closed: %s"
    |> Logger.debug nodeid tag

  // ** onMessage

  //  __  __
  // |  \/  | ___  ___ ___  __ _  __ _  ___
  // | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
  // | |  | |  __/\__ \__ \ (_| | (_| |  __/
  // |_|  |_|\___||___/___/\__,_|\__, |\___|
  //                             |___/

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
  let onMessage (socket: IWebSocketConnection) (msg: Binary.Buffer) =
    let msgHandler cb =
      /// get the session Id for this connection
      let session = getSessionId socket

      /// decode the binary buffer as `StateMachine` command
      let entry : Either<IrisError,StateMachine> = Binary.decode msg

      /// Process the result of decoding the received message
      match entry with
      | Right command ->
        /// handle the result
        cb session command

        /// log some debugging messages
        command
        |> string
        |> sprintf "command received from session %s and decoded as %s" (string session)
        |> Logger.debug nodeid tag


      | Left  error   ->
        /// log the error globally
        error
        |> string
        |> sprintf "command received from session %s could not be decoded: %s" (string session)
        |> Logger.debug nodeid tag

    /// Reach into the `onMessageCb` and apply `msgHandler` to the stored callback
    onMessageCb
    |> Option.map msgHandler
    |> ignore

  // ** onError

  //  _____
  // | ____|_ __ _ __ ___  _ __
  // |  _| | '__| '__/ _ \| '__|
  // | |___| |  | | | (_) | |
  // |_____|_|  |_|  \___/|_|

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
  let onError (socket: IWebSocketConnection) (exn: 'a when 'a :> Exception) =
    let session = getSessionId socket
    sessions <- Map.remove session sessions
    Option.map (fun cb -> cb session) onErrorCb |> ignore

    exn.Message
    |> sprintf "an error occurred on session %s. error: %s" (string session)
    |> Logger.err nodeid tag

  // ** onNewSocket

  /// ## onNewSocket
  ///
  /// Register all callbacks on a newly created socket connection.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection to add handlers to
  ///
  /// Returns: unit
  let onNewSocket (socket: IWebSocketConnection) =
    socket.OnOpen    <- new System.Action(onOpen socket)
    socket.OnClose   <- new System.Action(onClose socket)
    // socket.OnMessage <- new System.Action<string>(onMessage socket)
    socket.OnBinary  <- new System.Action<Binary.Buffer>(onMessage socket)
    socket.OnError   <- new System.Action<exn>(onError socket)

  // ** closeConnection

  let closeConnection _ (socket: IWebSocketConnection) =
    lock sessions <| fun _ ->
      let session = getSessionId socket

      session
      |> string
      |> sprintf "Closing connection %s"
      |> Logger.debug nodeid tag

      socket.Close()

      sessions <- Map.remove session sessions

  // ** Start

  /// ## Start
  ///
  /// Start a WebSocketServer with the given action.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Start() =
    uri
    |> sprintf "Starting WebSocketServer on: %s"
    |> Logger.debug nodeid tag

    server.Start(new System.Action<IWebSocketConnection>(onNewSocket))

    "WebSocketServer successfully started"
    |> Logger.debug nodeid tag

  // ** Stop

  /// ## Stop
  ///
  /// Stop the WebSocketServer and close all open connections.
  ///
  /// ### Signature:
  /// - unit: unit
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: unit
  member self.Stop() =
    Logger.debug nodeid tag "Stopping WebSocketServer and closing all connections"
    Map.iter closeConnection sessions

    Logger.debug nodeid tag "Disposing WebSocketServer"
    dispose server

  // ** Broadcast

  /// ## Broadcast
  ///
  /// Send a `StateMachine` command to all open connections.
  ///
  /// ### Signature:
  /// - msg: StateMachine command to send
  ///
  /// Returns: unit
  member self.Broadcast(msg: StateMachine) =
    let send _ (socket: IWebSocketConnection) =
      msg |> Binary.encode |> socket.Send |> ignore
    Map.iter send sessions

  // ** Send

  /// ## Send
  ///
  /// Send a `StateMachine` command to the requested session.
  ///
  /// ### Signature:
  /// - sessionid: Id of session to send the command to
  /// - msg: StateMachine command to send
  ///
  /// Returns: unit
  member self.Send (sessionid: Id) (msg: StateMachine) =
    match Map.tryFind sessionid sessions with
    | Some socket ->
      msg
      |> Binary.encode
      |> socket.Send
      |> ignore

    | _ ->
      lock sessions <| fun _ ->
        sessionid
        |> string
        |> sprintf "could not send message to session %s. not found."
        |> Logger.debug nodeid tag

        sessions <- Map.remove sessionid sessions

  member self.OnOpen
    with set cb = onOpenCb <- Some cb

  member self.OnClose
    with set cb = onCloseCb <- Some cb

  member self.OnError
    with set cb = onErrorCb <- Some cb

  member self.OnMessage
    with set cb = onMessageCb <- Some cb

  interface IDisposable with
    member self.Dispose() =
      self.Stop()
