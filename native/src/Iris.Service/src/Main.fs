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

    let pid = "patch-1"

    let iob1 =
      { IOBox.ValueBox("value-box-id", "Value Box Example", pid)
          with Slices = [| { Idx = 0; Value = 666 } |] }

    let iob2 =
      { IOBox.StringBox("string-box-id", "String Box Example", pid)
          with Slices = [| { Idx = 0; Value = "my example string value" } |] }

    let iob3 =
      { IOBox.ColorBox("color-box-id", "Color Box Example", pid)
          with Slices = [| { Idx = 0; Value = "#0f23ea" } |] }

    let iob4 =
      { IOBox.EnumBox("enum-box-id", "Enum Box Example", pid)
          with Slices = [| { Idx = 0; Value = [| "value 1"; "value 2"; "value 3"; |] }
                        |] }
    let iob5 =
      { IOBox.EnumBox("node-box-id", "Node Box Example", pid)
          with Slices = [| { Idx = 0; Value = "Not supported" } |] }

    let patch = { Id       = pid
                ; Name     = "asdfas"
                ; IOBoxes  =  [| iob1; iob2; iob3; iob4; iob5; |]
                }

    let msg : ApiMessage = { Type = AddPatch; Payload = patch }

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
