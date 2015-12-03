namespace Iris.Service.Types

open Fleck
open System
open Akka
open Akka.Actor
open Akka.FSharp
open Akka.Routing

[<RequireQualifiedAccess>]
module WebSockets =

  type SessionId = string

  type WsMsg =
    | Broadcast        of string
    | Multicast        of SessionId * string
    | ClientDisconnect of SessionId

    with
      override self.ToString() =
        match self with
          | Broadcast(str)        -> sprintf "Broadcast: %s" str
          | Multicast(ses, str)   -> sprintf "Multicast %s %s" ses str
          | ClientDisconnect(str) -> sprintf "ClientDisconnect %s" str

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
  let private closeHandler (session : SessionId) (router : IActorRef) : Action =
    let handler _ = router <! ClientDisconnect session
    new Action(handler)

  (*--------------------------------------------------------------------------*)
  let private msgHandler (session : SessionId) (router : IActorRef) : Action<string> =
    let handler str =
      printfn "%s said: %s" session str
      // take the payload and wrap it up for sending to everybody else
      router <! Multicast(session,str)
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
  let private mkWorker (session : SessionId) (socket : IWebSocketConnection) =
    fun (mailbox : Actor<WsMsg>) ->
      let router = Routes.getRouter mailbox Routes.clients
      
      socket.OnOpen    <- openHandler
      socket.OnClose   <- closeHandler session router
      socket.OnMessage <- msgHandler   session router
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
  let private spawnSocket (parent : Actor<WsMsg>) =
    let handler (socket : IWebSocketConnection) =
      let sid = makeSession socket
      spawn parent sid
      |> applyTo (mkWorker sid socket)
      |> ignore
    new Action<IWebSocketConnection>(handler)


  let private logger (mailbox : Actor<obj>) =
    let rec loop () =
      actor {
        let! msg = mailbox.Receive()
        match msg with
          | :? string -> printfn "Logger: %s" (msg :?> string)
          | _ -> printfn @"Logger (ToString) ""%s""" <| msg.ToString()
        return! loop()
      }
    loop()

  let private strategy =
    Strategy.OneForOne <| fun excp ->
      Console.WriteLine("something happened here: " + excp.Message)
      match excp with
        | :? ConnectionException -> Directive.Stop
        | _ -> Directive.Escalate

  let Create system port =
    spawnOpt system Routes.websocket
      (fun mailbox ->
        let server = new WebSocketServer("ws://0.0.0.0:" + (port.ToString()))
        mailbox.Defer <| fun () -> server.Dispose()

        // mailbox.Watch(spawn mailbox "logger" logger) |> ignore
        server.Start(spawnSocket mailbox)

        mailbox.Self.Path.ToSerializationFormat()
        |> printfn "supervisor path: %s"

        let rec loop () =
          actor {
            let! msg = mailbox.Receive()
            return! loop()
          }
        loop())
      [ SupervisorStrategy(strategy) ]
