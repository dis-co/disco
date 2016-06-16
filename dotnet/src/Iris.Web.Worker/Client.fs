namespace Iris.Web.Worker


module Client =

  open Fable.Core
  open Fable.Import
  
  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Views
  
  (*  __  __       _
     |  \/  | __ _(_)_ __
     | |\/| |/ _` | | '_ \
     | |  | | (_| | | | | |
     |_|  |_|\__,_|_|_| |_| entry point. *)

  let initialize (ctx : GlobalContext) (ev : WorkerEvent) : unit =
    ctx.Add(ev.ports.[0])

  [<Emit "void(importScripts ? importScripts($script) : null)">]
  let importScript (script : string) : unit = failwith "oh no jS"

  [<Emit "void (onconnect = $handler)">]
  let onConnect (handler: WorkerEvent -> unit) = failwith "hohoho"

  let Main : unit =
    importScript "dependencies/asmcrypto/asmcrypto.js"

    let context = new GlobalContext()
    onConnect (initialize context)
