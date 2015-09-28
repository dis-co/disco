open Fleck
open System.Diagnostics

let rec loop (sckt : IWebSocketConnection option ref) =
  let res = System.Console.ReadLine()

  match !sckt with
    | Some(s) -> s.Send(res) |> ignore
    | _ -> printfn "not connected"

  if not <| (res = "quit") then loop sckt
  

[<EntryPoint>]
let main argv = 
  printfn "%A" argv

  let server = new WebSocketServer "ws://0.0.0.0:8080"

  let sckt = ref Option<IWebSocketConnection>.None

  server.Start(fun socket ->
    socket.OnOpen <- (fun () -> sckt := Some(socket))
    socket.OnClose <- (fun () -> printfn "Close!")
    socket.OnMessage <- (fun msg -> socket.Send("connected!") |> ignore))

  loop sckt

  0 // return an integer exit code
