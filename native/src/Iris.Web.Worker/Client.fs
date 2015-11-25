namespace Iris.Web.Worker

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  let initialize (context : GlobalContext) ev =
    let port = ev.ports.[0]
    port.Onmessage <- context.OnClientMsg
    context.Clients.Push(port) |> ignore

  [<Direct "void (onconnect = $handler)">]
  let onConnect (handler: WorkerEvent -> unit) = ()

  (*  __  __       _
     |  \/  | __ _(_)_ __
     | |\/| |/ _` | | '_ \
     | |  | | (_| | | | | |
     |_|  |_|\__,_|_|_| |_| entry point. *)

  let Main : unit =
    let context = new GlobalContext()
    onConnect (initialize context)
