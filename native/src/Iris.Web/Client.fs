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
  let HandleMsg (ctrl : ViewController<State>) (msg : ClientMessage) : unit =
    Console.Log(msg)
    match msg.Type with
      | Render    -> ctrl.Render (Option.get(msg.Payload) :?> State)
      | Connected -> Console.Log("CONNECTED")
      | _ as a    -> Console.Log("Event", a)

  let Main : unit =
    let widget = new Patches.Root()
    let ctrl = new ViewController<State> (widget)

    let worker = new SharedWorker("Iris.Web.Worker.js")

    worker.onerror <- (fun e -> Console.Log("error: ", e))
    worker.port.Onmessage <- (fun msg -> HandleMsg ctrl (msg.Data :?> ClientMessage))
    worker.port.Start()

    // ctrl.Render store
    // store.Subscribe (fun store' _ -> ctrl.Render store')

    
