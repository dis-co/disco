namespace Iris.Service.Types

open Fleck
open System
open Iris.Core.Types

[<AutoOpen>]
//[<RequireQualifiedAccess>]
module WebSockets =

  exception ConnectionException

  let private makeSession (socket : IWebSocketConnection) =
    socket.ConnectionInfo.Id.ToString()

  (*--------------------------------------------------------------------------*
    not ever called for some reaon
    *--------------------------------------------------------------------------*)
  let private openHandler : Action =
    new Action(fun _ -> printfn "socket now open")

  (*--------------------------------------------------------------------------*)
  let private closeHandler (session : SessionId) : Action =
    new Action(fun _ -> printfn "%s closed" session)

  (*--------------------------------------------------------------------------*)
  let private msgHandler (session : SessionId) : Action<string> =
    let handler str =
      printfn "%s said: %s" session str
    new Action<string>(handler)

  (*--------------------------------------------------------------------------*)
  let private errHandler : Action<exn> =
    let handler (exn : Exception) =
      printfn "Exception: %s" exn.Message
    new Action<exn>(handler)

  (*--------------------------------------------------------------------------*)
  let private binHandler : Action<byte[]> =
    let handler (bytes : byte []) =
      printfn "Received %d bytes in binary message" bytes.Length
    new Action<byte[]>(handler)

  let Create () = 
    let server = new WebSocketServer("ws://0.0.0.0:8181");
    server.Start(fun socket ->
      let session = makeSession socket
      socket.OnOpen    <- openHandler
      socket.OnClose   <- closeHandler session
      socket.OnMessage <- msgHandler   session
      socket.OnError   <- errHandler
      socket.OnBinary  <- binHandler)
