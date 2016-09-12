namespace Iris.Service

[<AutoOpen>]
module WebSocket =

  open System
  open System.Threading
  open Iris.Core
  open Iris.Service.Raft.Server
  open Fleck
  open Newtonsoft.Json

  type WsServer(config: Config, context: RaftServer) as this =

    let uri =
      sprintf "ws://%s:%d"
        config.RaftConfig.BindAddress
        config.PortConfig.WebSocket

    let server = new WebSocketServer(uri)

    let sessions : Map<Session,IWebSocketConnection> ref = ref Map.empty

    let getSession (socket: IWebSocketConnection) =
      let id = socket.ConnectionInfo.Id
      id.ToString()

    let onOpen (socket: IWebSocketConnection) _ =
      let session = getSession socket
      sessions := Map.add session socket !sessions
      printfn "[%s] onOpen!" session

    let onClose (socket: IWebSocketConnection) _ =
      let session = getSession socket
      sessions := Map.remove session !sessions
      printfn "[%s] onClose!" session

    let onMessage (socket: IWebSocketConnection) (msg: string) =
      printfn "[%s] onMessage: %s" (getSession socket) msg

      let ev : StateMachine option = Json.decode msg

      match ev with
      | Some (LogMsg (_, msg)) -> printfn "log: %s" msg
      | Some ev ->
        context.Append(ev)
        |> Option.map
          (fun resp ->
             LogMsg(Iris.Core.LogLevel.Debug, string resp)
             |> this.Broadcast)
        |> ignore
      | _ -> printfn "WebSocket onMesssage: unable to parse %A" msg

    let onError (socket: IWebSocketConnection) (exn: 'a when 'a :> Exception) =
      printfn "[%s] onError: %s" (getSession socket) exn.Message

    let handler (socket: IWebSocketConnection) =
      socket.OnOpen    <- new System.Action(onOpen socket)
      socket.OnClose   <- new System.Action(onClose socket)
      socket.OnMessage <- new System.Action<string>(onMessage socket)
      socket.OnError   <- new System.Action<exn>(onError socket)

    member self.Start() =
      server.Start(new System.Action<IWebSocketConnection>(handler))

    member self.Stop() =
      Map.iter (fun _ (socket: IWebSocketConnection) -> socket.Close()) !sessions
      dispose server

    member self.Broadcast(msg: StateMachine) =
      let send _ (socket: IWebSocketConnection) =
        msg |> Json.encode |> socket.Send |> ignore
      Map.iter send !sessions

    interface IDisposable with
      member self.Dispose() =
        self.Stop()
