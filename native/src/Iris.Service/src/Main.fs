namespace Iris.Service

module Main =

  open Fleck
  open System.Diagnostics
  open System.IO
  open Iris.Core.Types
  open WebSharper

  open Iris.Service.Server.AssetServer

  let rec loop (sckt : IWebSocketConnection option ref) =
    let res = System.Console.ReadLine()

    match !sckt with
      | Some(s) -> s.Send(res) |> ignore
      | _ -> printfn "not connected"

    if not <| (res = "quit") then loop sckt
  
  let toString = System.Text.Encoding.ASCII.GetString

  let serialize (value : 'U) : string =
    let JsonProvider = Core.Json.Provider.Create()
    let encoder = JsonProvider.GetEncoder<'U>()
    use writer = new StringWriter()

    value
    |> encoder.Encode
    |> JsonProvider.Pack
    |> WebSharper.Core.Json.Write writer

    writer.ToString()

  [<EntryPoint>]
  let main argv =
    let socketServer = new WebSocketServer "ws://0.0.0.0:8080"
    // let assetServer = new AssetServer("0.0.0.0", 3000)
    // assetServer.Start ()

    let iob1 =
      { IOBox.StringBox("0xb33f", "hi", "0xb4d1d34")
          with Slices = [| { Idx = 0; Value = "swell" } |] }

    let patch = { Id       = "hello"
                ; Name     = "asdfas"
                ; IOBoxes  =  [| iob1 |]
                }

    let msg = { Type = AddPatch; Payload = patch }

    let sckt = ref Option<IWebSocketConnection>.None

    socketServer.Start(fun socket ->
      socket.OnOpen    <- (fun () -> sckt := Some(socket)
                                     serialize msg
                                     |> socket.Send
                                     |> ignore)
      socket.OnClose   <- (fun () -> printfn "Close!")
      socket.OnMessage <- (fun msg -> printfn "message: %s" msg))

    loop sckt

    0 // return an integer exit code
