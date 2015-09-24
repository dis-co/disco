open Fleck

[<EntryPoint>]
let main argv = 
  printfn "%A" argv

  let server = new WebSocketServer "ws://0.0.0.0:8080"

  server.Start(fun socket ->
    socket.OnOpen <- (fun () -> printfn "OPEN!")
    socket.OnClose <- (fun () -> printfn "Close!")
    socket.OnMessage <- (fun msg -> printfn "message: %s" msg))

  System.Console.ReadLine()

  0 // return an integer exit code

