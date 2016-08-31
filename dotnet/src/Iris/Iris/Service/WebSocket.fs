namespace Iris.Service

[<AutoOpen>]
module WebSocket =

  open System
  open System.Threading
  open Iris.Core
  open Fleck

  type WsServer(config: Config) =

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

    let onError (socket: IWebSocketConnection) (exn: 'a when 'a :> Exception) =
      printfn "[%s] onError: %s" (getSession socket) exn.Message

    let handler (socket: IWebSocketConnection) =
      socket.OnOpen    <- new Action(onOpen socket)
      socket.OnClose   <- new Action(onClose socket)
      socket.OnMessage <- new Action<string>(onMessage socket)
      socket.OnError   <- new Action<exn>(onError socket)

    member self.Start() =
      server.Start(new Action<IWebSocketConnection>(handler))

    member self.Stop() =
      Map.iter (fun _ (socket: IWebSocketConnection) -> socket.Close()) !sessions
      dispose server

    member self.Broadcast(msg: string) =
      Map.iter (fun _ (socket: IWebSocketConnection) ->
                  socket.Send(msg) |> ignore)
        !sessions

    interface IDisposable with
      member self.Dispose() =
        self.Stop()
