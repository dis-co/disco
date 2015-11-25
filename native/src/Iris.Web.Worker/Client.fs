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
    context.Add(ev.ports.[0])
    context.Log(JSON.Stringify(ev.ports))

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
