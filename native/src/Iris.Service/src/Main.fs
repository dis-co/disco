namespace Iris.Service

open Fleck
open System.Diagnostics
open System

open Akka
open Akka.Actor
open Akka.FSharp

open Iris.Core.Types
open Iris.Service.Types

module Main =
  type SessionId = string

  type WsMsg =
    | Broadcast of string
    | Multicast of SessionId * string

    with
      override self.ToString() =
        match self with
          | Broadcast(str) -> sprintf "Broadcast: %s" str
          | Multicast(ses, str) -> sprintf "Multicast %s %s" ses str

  exception ConnectionException

  let makeSession (socket : IWebSocketConnection) =
    socket.ConnectionInfo.Id.ToString()

  let applyTo a f = f a

  (*--------------------------------------------------------------------------*
    not ever called for some reaon
    *--------------------------------------------------------------------------*)
  let openHandler : Action =
    new Action(fun _ -> printfn "socket now open")

  (*--------------------------------------------------------------------------*)
  let closeHandler (session : SessionId) (mailbox : Actor<WsMsg>) : Action =
    let handler _ =
      let msg = Multicast(session,sprintf "session %s was closed" session)
       in mailbox.ActorSelection("../*") <! msg
      printfn "%s left" session
      mailbox.Context.Stop(mailbox.Self)
    new Action(handler)

  (*--------------------------------------------------------------------------*)
  let msgHandler (session : SessionId) (mailbox : Actor<WsMsg>) : Action<string> =
    let handler str =
      printfn "%s said: %s" session str
      // take the payload and wrap it up for sending to everybody else
      mailbox.ActorSelection("../*") <! Multicast(session,str)
    new Action<string>(handler)

  (*--------------------------------------------------------------------------*)
  let errHandler : Action<exn> =
    let handler (exn : Exception) =
      printfn "Exception: %s" exn.Message
    new Action<exn>(handler)

  (*--------------------------------------------------------------------------*)
  let binHandler : Action<byte[]> =
    let handler (bytes : byte []) =
      printfn "Received %d bytes in binary message" bytes.Length
    new Action<byte[]>(handler)

  (*--------------------------------------------------------------------------*)
  let mkWorker (session : SessionId) (socket : IWebSocketConnection) =
    fun (mailbox : Actor<WsMsg>) ->
      socket.OnOpen    <- openHandler
      socket.OnClose   <- closeHandler session mailbox
      socket.OnMessage <- msgHandler session mailbox
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
        return! loop()
      }
      loop()

  (*--------------------------------------------------------------------------*)
  let spawnSocket (parent : Actor<WsMsg>) =
    let handler (socket : IWebSocketConnection) =
      let sid = makeSession socket
      spawn parent sid
      |> applyTo (mkWorker sid socket)
      |> ignore
    new Action<IWebSocketConnection>(handler)


  let logger (mailbox : Actor<obj>) =
    let rec loop () =
      actor {
        let! msg = mailbox.Receive()
        match msg with
          | :? string -> printfn "Logger: %s" (msg :?> string)
          | _ -> printfn @"Logger (ToString) ""%s""" <| msg.ToString()
        return! loop()
      }
    loop()

  [<EntryPoint>]
  let main argv =

    let system = ActorSystem.Create "iris"
    let scktSrv = new WebSocketServer "ws://0.0.0.0:8080"

    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    let strategy =
      Strategy.OneForOne <| fun excp ->
        match excp with
          | :? ConnectionException -> Directive.Stop
          | _ -> Directive.Escalate

    let supervisor =
      spawnOpt system "clients"
        (fun mailbox ->

          mailbox.Watch(spawn mailbox "logger" logger) |> ignore
          scktSrv.Start(spawnSocket mailbox)

          mailbox.Self.Path.ToSerializationFormat()
          |> printfn "supervisor path: %s"

          let rec loop () =
            actor {
              let! msg = mailbox.Receive()
              mailbox.ActorSelection("*") <! msg
              return! loop()
            }
          loop())
        [ SupervisorStrategy(strategy) ]

    (*~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~*)

    while true do
      let cmd = Console.ReadLine()
      supervisor <! Broadcast cmd

    0
