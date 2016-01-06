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

  let private applyTo a f = f a

  (*--------------------------------------------------------------------------*
    not ever called for some reaon
    *--------------------------------------------------------------------------*)
  let private openHandler : Action =
    new Action(fun _ -> printfn "socket now open")

  (*--------------------------------------------------------------------------*)
  let private closeHandler (session : SessionId) (ctx : Ctx) : Action =
    let handler _ =
      ctx.clients <! ClientDisconnect session
    new Action(handler)

  (*--------------------------------------------------------------------------*)
  let private msgHandler (session : SessionId) (ctx : Ctx) : Action<string> =
    let handler str =
      printfn "%s said: %s" session str
      // take the payload and wrap it up for sending to everybody else
      ctx.clients <! Multicast(session,str)
      ctx.remotes <! Broadcast(str)
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

  (*--------------------------------------------------------------------------*)
  let private mkWorker (session : SessionId) (ctx : Ctx) (socket : IWebSocketConnection) =
    fun (mailbox : Actor<WsMsg>) ->
      socket.OnOpen    <- openHandler
      socket.OnClose   <- closeHandler session ctx
      socket.OnMessage <- msgHandler   session ctx
      socket.OnError   <- errHandler
      socket.OnBinary  <- binHandler

      mailbox.Context.Self.Path.ToSerializationFormat()
      |> printfn "socket connection worker path: %s"

      let rec loop() = actor {
        let! msg = mailbox.Receive()
        match msg with
          | Broadcast(payload) ->
            socket.Send(payload)
            |> ignore
          | Multicast(sess,payload) ->
            if sess <> session
            then socket.Send(payload) |> ignore
          | ClientDisconnect(sess) ->
            if sess <> session
            then socket.Send(sprintf "%s exited" sess) |> ignore
            else mailbox.Context.Stop(mailbox.Self)
        return! loop()
      }

      loop()

  (*--------------------------------------------------------------------------*)
  let private spawnSocket (parent : Actor<WsMsg>) ctx =
    let handler (socket : IWebSocketConnection) =
      let sid = makeSession socket
      spawn parent sid
      |> applyTo (mkWorker sid ctx socket)
      |> ignore
    new Action<IWebSocketConnection>(handler)


  let private logger (mailbox : Actor<obj>) =
    let rec loop () =
      actor {
        let! msg = mailbox.Receive()
        match msg with
          | :? string -> printfn "Logger: %s" (msg :?> string)
          | _ -> printfn @"Logger (type: %s) ""%s""" (msg.GetType().ToString()) (msg.ToString())
        return! loop()
      }
    loop()

  let private strategy =
    Strategy.OneForOne <| fun excp ->
      Console.WriteLine("something happened here: " + excp.Message)
      match excp with
        | :? ConnectionException -> Directive.Stop
        | _ -> Directive.Escalate

  let Create ctx port =
    spawnOpt ctx.system Routes.websocket
      (fun mailbox ->
        let server = new WebSocketServer("ws://0.0.0.0:" + (port.ToString()))
        mailbox.Defer <| fun () -> server.Dispose()

        // mailbox.Watch(spawn mailbox "logger" logger) |> ignore
        server.Start(spawnSocket mailbox ctx)

        mailbox.Self.Path.ToSerializationFormat()
        |> printfn "supervisor path: %s"

        spawn mailbox "logger" logger
        |> ignore

        let rec loop () =
          actor {
            let! msg = mailbox.Receive()
            return! loop()
          }
        loop())
      [ SupervisorStrategy(strategy) ]
