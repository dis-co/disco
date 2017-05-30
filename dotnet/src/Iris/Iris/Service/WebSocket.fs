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
open Iris.Service.Interfaces
open Hopac
open Hopac.Infixes

// * WebSockets

[<AutoOpen>]
module WebSockets =

  // ** tag

  let private tag (str: string) = sprintf "WebSocket.%s" str

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,IWebSocketConnection>

  // ** Subscriptions

  type private Subscriptions = Subscriptions<IrisEvent>

  // ** SocketEventProcessor

  type private SocketEventProcessor = MailboxProcessor<IrisEvent>

  // ** getConnectionId

  let private getConnectionId (socket: IWebSocketConnection) : Id =
    string socket.ConnectionInfo.Id |> Id

  // ** buildSession

  let private buildSession (connections: Connections)
                           (socketId: Id)
                           (session: Session) =
    match connections.TryGetValue(socketId) with
    | true, socket ->
      let ua =
        if socket.ConnectionInfo.Headers.ContainsKey("User-Agent") then
          socket.ConnectionInfo.Headers.["User-Agent"]
        else
          "<no user agent specified>"
      { session with
          IpAddress = IpAddress.Parse socket.ConnectionInfo.ClientIpAddress
          UserAgent = ua }
      |> Either.succeed
    | false, _ ->
      socketId
      |> String.format "No connection found for {0}"
      |> Error.asSocketError (tag "buildSession")
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

    let sendAsync (id: Id) = job {
        let result = send connections id msg
        return result
      }

    let result : IrisError list =
      connections.Keys
      |> Seq.map sendAsync
      |> Job.conCollect
      |> Hopac.run
      |> fun arr -> arr.ToArray()
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
  let private onNewSocket (id: Id)
                          (connections: Connections)
                          (agent: SocketEventProcessor)
                          (socket: IWebSocketConnection) =
    socket.OnOpen <- fun () ->
      let sid = getConnectionId socket

      connections.TryAdd(sid, socket)
      |> ignore

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
        Observable.notify subscriptions msg
        return! act ()
      }
    act()

  // ** WebSocketServer

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  [<RequireQualifiedAccess>]
  module WebSocketServer =

    let create (mem: RaftMember) =
      either {
        let status = ref ServiceStatus.Stopped
        let connections = Connections()
        let subscriptions = Subscriptions()

        let agent = new SocketEventProcessor(loop subscriptions)

        let uri = sprintf "ws://%s:%d" (string mem.IpAddr) mem.WsPort

        let handler = onNewSocket mem.Id connections agent
        let server = new WebSocketServer(uri)

        return
          { new IWebSocketServer with
              member self.Send (id: Id) (cmd: StateMachine) =
                send connections id cmd

              member self.Broadcast (cmd: StateMachine) =
                broadcast connections cmd

              member self.BuildSession (id: Id) (session: Session) =
                buildSession connections id session

              member self.Subscribe (callback: IrisEvent -> unit) =
                let listener = Observable.createListener subscriptions
                { new IObserver<IrisEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value
                  }
                |> listener.Subscribe

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
                  for KeyValue(_, connection) in connections do
                    connection.Close()
                  connections.Clear()
                  subscriptions.Clear()
                  dispose server
                  status := ServiceStatus.Disposed }
      }

    let broadcast (cmd: StateMachine) (server: IWebSocketServer) =
      server.Broadcast cmd

    let send (id: Id) (cmd: StateMachine) (server: IWebSocketServer) =
      server.Send id cmd
