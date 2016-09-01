[<AutoOpen>]
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

  [<Emit "importScripts ? importScripts($0) : null">]
  let importScript (_: string) : unit = failwith "JS ONLY"

  [<Emit "void(onconnect = $0)">]
  let onConnect (_: WorkerEvent -> unit) = failwith "JS ONLY"

  [<Emit("throw JSON.stringify({ data: $0 })")>]
  let throw (str: string) = failwith "JS ONLY"


onConnect <| fun ev ->
  printfn "hello"
