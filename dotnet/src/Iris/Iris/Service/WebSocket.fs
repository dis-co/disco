namespace Iris.Service

[<AutoOpen>]
module WebSocket =

  open System
  open System.Threading
  open Iris.Core
  open Iris.Service
  open Fleck

  #if MOCKSERVICE
  type RaftServer = class end
  #endif

  type WsServer(?config: IrisConfig, ?context: RaftServer) =
    let [<Literal>] defaultUri = "ws://127.0.0.1:8000"

    let mutable onOpenCb    : Option<Session -> unit> = None
    let mutable onCloseCb   : Option<Id -> unit> = None
    let mutable onErrorCb   : Option<Id -> unit> = None
    let mutable onMessageCb : Option<Id -> StateMachine -> unit> = None

    let uri =
      match config with
      | Some config ->
        let result =
          Config.getNodeId ()
          |> Either.bind (Config.findNode config)

        match result with
        | Right node -> sprintf "ws://%s:%d" (string node.IpAddr) node.WsPort
        | Left error -> Error.exitWith error
      | None -> defaultUri

    let server = new WebSocketServer(uri)

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

    //   ___
    //  / _ \ _ __   ___ _ __
    // | | | | '_ \ / _ \ '_ \
    // | |_| | |_) |  __/ | | |
    //  \___/| .__/ \___|_| |_|
    //       |_|

    let onOpen (socket: IWebSocketConnection) _ =
      let session : Session = buildSession socket
      let sid = getSessionId socket
      sessions <- Map.add sid socket sessions
      Option.map (fun cb -> cb session) onOpenCb |> ignore
      printfn "[%s] onOpen!" (getSessionId socket |> string)

    //   ____ _
    //  / ___| | ___  ___  ___
    // | |   | |/ _ \/ __|/ _ \
    // | |___| | (_) \__ \  __/
    //  \____|_|\___/|___/\___|

    let onClose (socket: IWebSocketConnection) _ =
      let session = getSessionId socket
      sessions <- Map.remove session sessions
      Option.map (fun cb -> cb session) onCloseCb |> ignore
      printfn "[%s] onClose!" (string session)

    //  __  __
    // |  \/  | ___  ___ ___  __ _  __ _  ___
    // | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
    // | |  | |  __/\__ \__ \ (_| | (_| |  __/
    // |_|  |_|\___||___/___/\__,_|\__, |\___|
    //                             |___/

    let onMessage (socket: IWebSocketConnection) (msg: Binary.Buffer) =
      Option.map
        (fun cb ->
          let session = getSessionId socket
          let entry : Either<IrisError,StateMachine> = Binary.decode msg

          match entry with
          | Right command -> cb session command
          | Left  error   ->
            (Err, sprintf "Decoding Error: %A" error)
            |> LogMsg
            |> cb session)
        onMessageCb
      |> ignore

    //  _____
    // | ____|_ __ _ __ ___  _ __
    // |  _| | '__| '__/ _ \| '__|
    // | |___| |  | | | (_) | |
    // |_____|_|  |_|  \___/|_|

    let onError (socket: IWebSocketConnection) (exn: 'a when 'a :> Exception) =
      let session = getSessionId socket
      sessions <- Map.remove session sessions
      Option.map (fun cb -> cb session) onErrorCb |> ignore
      printfn "[%s] onError: %s" (string session) exn.Message

    let handler (socket: IWebSocketConnection) =
      socket.OnOpen    <- new System.Action(onOpen socket)
      socket.OnClose   <- new System.Action(onClose socket)
      // socket.OnMessage <- new System.Action<string>(onMessage socket)
      socket.OnBinary  <- new System.Action<Binary.Buffer>(onMessage socket)
      socket.OnError   <- new System.Action<exn>(onError socket)

    member self.Start() =
      printfn "Starting Http Server on: %s" uri
      server.Start(new System.Action<IWebSocketConnection>(handler))

    member self.Stop() =
      Map.iter (fun _ (socket: IWebSocketConnection) -> socket.Close()) sessions
      dispose server

    member self.Broadcast(msg: StateMachine) =
      let send _ (socket: IWebSocketConnection) =
        msg |> Binary.encode |> socket.Send |> ignore
      Map.iter send sessions

    member self.Send (sessionid: Id) (msg: StateMachine) =
      match Map.tryFind sessionid sessions with
      | Some socket ->
        msg |> Binary.encode |> socket.Send |> ignore
      | _ -> printfn "could not send message to %A. not found." sessionid

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
