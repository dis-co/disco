namespace Iris.Web.Worker

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views
  
  (*  __  __       _
     |  \/  | __ _(_)_ __
     | |\/| |/ _` | | '_ \
     | |  | | (_| | | | | |
     |_|  |_|\__,_|_|_| |_| entry point. *)

  let initialize (ctx : GlobalContext) (ev : WorkerEvent) : unit =
    ctx.Add(ev.ports.[0])

  [<Direct "void(importScripts ? importScripts($script) : null)">]
  let importScript (script : string) : unit = X

  [<Direct "void (onconnect = $handler)">]
  let onConnect (handler: WorkerEvent -> unit) = ()

  let Main : unit =
    importScript "dependencies/asmcrypto/asmcrypto.js"

    let context = new GlobalContext()
    onConnect (initialize context)
