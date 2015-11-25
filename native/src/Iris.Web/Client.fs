namespace Iris.Web

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  [<Stub>]
  type SharedWorker =
      [<DefaultValue>]
      val mutable onerror : (obj -> unit)

      [<DefaultValue>]
      val mutable port : MessagePort

      [<Inline "new SharedWorker($url)">]
      new(url : string) = {}

  let HandleMsg (msg : ClientEvent) : unit =
    match msg with
      | Render    -> Console.Log("RENDER")
      | Connected -> Console.Log("CONNECTED")
      | _ as a    -> Console.Log("Event", a)

  let Main : unit =
    let widget = new Patches.Root()
    let ctrl = new ViewController<State> (widget)

    let worker = new SharedWorker("Iris.Web.Worker.js")

    worker.onerror <- (fun e -> Console.Log("error: ", e))
    worker.port.Onmessage <- (fun msg -> Console.Log("onmessage!", msg.Data))
    worker.port.Start()

    // ctrl.Render store
    // store.Subscribe (fun store' _ -> ctrl.Render store')

    
