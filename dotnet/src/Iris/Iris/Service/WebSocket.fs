namespace Iris.Service

// * Imports

open System
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Service
open Fleck
open Iris.Service.Interfaces

// * WebSocketServer

module WebSocketServer =

  // ** tag

  let private tag (str: string) = sprintf "WebSocket.%s" str

  // ** Connections

  type private Connections = ConcurrentDictionary<SessionId,IWebSocketConnection * Session option>

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<IrisEvent>

  // ** SocketEventProcessor

  type private SocketEventProcessor = MailboxProcessor<IrisEvent>

  // ** getConnectionId

  let private getConnectionId (socket: IWebSocketConnection) : SessionId =
    IrisId.FromGuid socket.ConnectionInfo.Id

  // ** buildSession

  let private buildSession (connections: Connections)
                           (socketId: SessionId)
                           (session: Session) =
    match connections.TryGetValue(socketId) with
    | true, (socket, _ as current) ->
      let ua =
        if socket.ConnectionInfo.Headers.ContainsKey("User-Agent") then
          socket.ConnectionInfo.Headers.["User-Agent"]
        else
          "<no user agent specified>"
      let updated =
        { session with
            IpAddress = IpAddress.Parse socket.ConnectionInfo.ClientIpAddress
            UserAgent = ua }
      if connections.TryUpdate(socketId, (socket, Some updated), current) then
        Either.succeed updated
      elif connections.TryUpdate(socketId, (socket, Some updated), current) then
        Either.succeed updated
      else
        "Updating connections failed after one retry"
        |> Error.asSocketError (tag "buildSession")
        |> Either.fail
    | false, _ ->
      socketId
      |> String.format "No connection found for {0}"
      |> Error.asSocketError (tag "buildSession")
      |> Either.fail

  // ** ucast

  /// ## ucast
  ///
  /// Send a `StateMachine` command to the requested session.
  ///
  /// ### Signature:
  /// - sessionid: Id of session to send the command to
  /// - msg: StateMachine command to send
  ///
  /// Returns: Either<IrisError,unit>
  let private ucast (connections: Connections) (sid: SessionId) (msg: StateMachine) =
    match connections.TryGetValue(sid) with
    | true, (socket, _) ->
      try
        msg
        |> Binary.encode
        |> socket.Send
        |> ignore
        |> Either.succeed
      with
        | exn ->
          exn.Message + exn.StackTrace
          |> Logger.err (tag "send")

          exn.Message
          |> Error.asSocketError (tag "send")
          |> Either.fail
    | false, _ ->
      sid
      |> string
      |> sprintf "could not send message to session %s. not found."
      |> Error.asSocketError (tag "send")
      |> Either.fail

  // ** bcast

  /// ## Broadcast
  ///
  /// Send a `StateMachine` command to all open connections.
  ///
  /// ### Signature:
  /// - msg: StateMachine command to send
  ///
  /// Returns: unit
  let private bcast (connections: Connections) (msg: StateMachine) =
    let result : IrisError list =
      connections.Keys
      |> Seq.toArray
      |> Array.map (fun id -> ucast connections id msg)
      |> Array.fold
        (fun lst (result: Either<IrisError,unit>) ->
          match result with
          | Right _ -> lst
          | Left error -> error :: lst)
        []

    match result with
    | [ ] -> Right ()
    | _   -> Left result

  // ** mcast

  /// ## mcast
  ///
  /// Send a `StateMachine` command to all open connections except the one that matches the passed
  /// session id.
  ///
  /// ### Signature:
  /// - id: Id to exclude
  /// - msg: StateMachine command to send
  ///
  /// Returns: unit
  let private mcast (connections: Connections)
                    (id: SessionId)
                    (msg: StateMachine) :
                    Either<IrisError list, unit> =
    let sendAsync (id: SessionId) = async {
        let result = ucast connections id msg
        return result
      }

    let result : IrisError list =
      connections.Keys
      |> Seq.filter (fun sid -> id <> sid)
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

  // ** onNewSocket

  /// ## onNewSocket
  ///
  /// Register all callbacks on a newly created socket connection.
  ///
  /// ### Signature:
  /// - socket: IWebSocketConnection to add handlers to
  ///
  /// Returns: unit
  let private onNewSocket (connections: Connections)
                          (agent: SocketEventProcessor)
                          (socket: IWebSocketConnection) =
    socket.OnOpen <- fun () ->
      let sid = getConnectionId socket

      connections.TryAdd(sid, (socket, None)) |> ignore

      sid
      |> SessionOpened
      |> agent.Post

      sid
      |> string
      |> sprintf "New connection opened: %s"
      |> Logger.info (tag "onNewSocket")

    socket.OnClose <- fun () ->
      let sid = getConnectionId socket
      connections.TryRemove(sid) |> ignore

      sid
      |> SessionClosed
      |> agent.Post

      sid
      |> string
      |> sprintf "Connection closed: %s"
      |> Logger.info (tag "onCloseSocket")

    socket.OnBinary <- fun bytes ->
      let sid = getConnectionId socket
      match Binary.decode bytes with
      | Right cmd -> IrisEvent.Append(Origin.Web sid, cmd) |> agent.Post
      | Left err  ->
        err
        |> string
        |> sprintf "Could not decode message: %s"
        |> Logger.err (tag "onSocketMessage")

    socket.OnError <- fun exn ->
      let sid = getConnectionId socket
      connections.TryRemove(sid) |> ignore

      sid
      |> SessionClosed
      |> agent.Post

      sid
      |> string
      |> sprintf "Error %A on websocket: %s" exn.Message
      |> Logger.err (tag "onSocketError")

  // ** loop

  let private loop (subscriptions: Subscriptions) (inbox: SocketEventProcessor) =
    let rec act () = async {
        let! msg = inbox.Receive()
        Observable.onNext subscriptions msg
        return! act ()
      }
    act()

  // ** broadcast

  let broadcast (cmd: StateMachine) (server: IWebSocketServer) =
    server.Broadcast cmd

  // ** send

  let send (id: SessionId) (cmd: StateMachine) (server: IWebSocketServer) =
    server.Send id cmd

  // ** create

  let create (mem: RaftMember) =
    either {
      let status = ref ServiceStatus.Stopped
      let connections = Connections()
      let subscriptions = Subscriptions()

      let agent = new SocketEventProcessor(loop subscriptions)

      let uri = sprintf "ws://%s:%d" (string mem.IpAddr) mem.WsPort

      FleckLog.LogAction <- Action<Fleck.LogLevel,string,exn>(fun level msg ex ->
        match level with
        // this will produce a loop due to messages occuring on every send operation, thus procucing
        // a new send operation and so on.
        | Fleck.LogLevel.Debug -> ()
        | Fleck.LogLevel.Info  -> Logger.debug (tag "logger") msg
        | Fleck.LogLevel.Warn  -> Logger.debug (tag "logger") msg
        | Fleck.LogLevel.Error -> Logger.debug (tag "logger") msg
        |                _     -> Logger.debug (tag "logger") msg)

      let handler = onNewSocket connections agent
      let server = new WebSocketServer(uri)
      server.RestartAfterListenError <- true
      server.ListenerSocket.NoDelay <- true

      return
        { new IWebSocketServer with
            member self.Publish (ev: IrisEvent) =
              match ev with
              | IrisEvent.Append (_, cmd) ->
                match bcast connections cmd with
                | Right _ -> ()
                | Left error ->
                  error
                  |> string
                  |> Logger.err (tag "Publish")
              | _ -> ()

            member self.Send (id: SessionId) (cmd: StateMachine) =
              ucast connections id cmd

            member self.Broadcast (cmd: StateMachine) =
              bcast connections cmd

            member self.Multicast (except: SessionId) (cmd: StateMachine) =
              mcast connections except cmd

            member self.BuildSession (id: SessionId) (session: Session) =
              buildSession connections id session

            member self.Sessions
              with get () =
                Array.fold
                  (fun m (KeyValue(id, (_,session))) ->
                    match session with
                    | Some session -> Map.add id session m
                    | _ -> m)
                  Map.empty
                  (connections.ToArray())

            member self.Subscribe (callback: IrisEvent -> unit) =
              Observable.subscribe callback subscriptions

            member self.Start () =
              status := ServiceStatus.Starting
              try
                uri
                |> sprintf "Starting WebSocketServer on: %s"
                |> Logger.debug (tag "Start")

                agent.Start()
                server.Start(new Action<IWebSocketConnection>(handler))

                status := ServiceStatus.Running
                "WebSocketServer successfully started"
                |> Logger.debug (tag "Start")
                |> Either.succeed
              with
                | exn ->
                  exn.Message
                  |> Error.asSocketError (tag "Start")
                  |> Either.fail

            member self.Dispose () =
              if Service.isRunning !status then
                for KeyValue(_, (connection, _)) in connections do
                  connection.Close()
                connections.Clear()
                subscriptions.Clear()
                dispose server
                status := ServiceStatus.Disposed }
    }
