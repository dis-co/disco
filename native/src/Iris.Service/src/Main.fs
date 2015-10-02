open Fleck
open System.Diagnostics
open System.IO

open Iris.Service.AssetServer

let rec loop (sckt : IWebSocketConnection option ref) =
  let res = System.Console.ReadLine()

  match !sckt with
    | Some(s) -> s.Send(res) |> ignore
    | _ -> printfn "not connected"

  if not <| (res = "quit") then loop sckt
  

[<EntryPoint>]
let main argv = 
  let socketServer = new WebSocketServer "ws://0.0.0.0:8080"
  let assetServer = new AssetServer("0.0.0.0", 3000)
  assetServer.Start ()

  let sckt = ref Option<IWebSocketConnection>.None

  socketServer.Start(fun socket ->
    socket.OnOpen    <- (fun () -> sckt := Some(socket))
    socket.OnClose   <- (fun () -> printfn "Close!")
    socket.OnMessage <- (fun msg ->
                           socket.Send "{ \"msgtype\": \"empty\" }"
                           |> ignore))

  loop sckt

  0 // return an integer exit code
