// __        __         _               __  __       _
// \ \      / /__  _ __| | _____ _ __  |  \/  | __ _(_)_ __
//  \ \ /\ / / _ \| '__| |/ / _ \ '__| | |\/| |/ _` | | '_ \
//   \ V  V / (_) | |  |   <  __/ |    | |  | | (_| | | | | |
//    \_/\_/ \___/|_|  |_|\_\___|_|    |_|  |_|\__,_|_|_| |_|

open Iris.Core
open Iris.Web.Core
open Fable.Core.JsInterop

let context = new GlobalContext()

onConnect <| fun ev ->
  let port = ev.Ports.[0]

  port.OnMessage <- fun msg ->
    ClientMessage.ClientLog (sprintf "hello echo %A" msg.Data)
    |> toJson
    |> port.PostMessage

  context.Register(port)
