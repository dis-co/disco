namespace Iris.Web

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type ClientContext() =
    let mutable ctrl = Option<ViewController<State>>.None 

    let worker = new SharedWorker("Iris.Web.Worker.js")

    member __.Controller
     with set c  = ctrl <- Some(c)

    member __.Start() =
      worker.onerror <- (fun e -> Console.Log("error: ", e))
      worker.port.Onmessage <- (fun msg -> __.HandleMsg (msg.Data :?> ClientMessage))
      worker.port.Start()
    
    member __.HandleMsg (msg : ClientMessage) : unit =
      match ctrl with
        | Some(ctrl') ->
          match msg.Type with
            | Log       -> Console.Log("SharedWorker", msg.Payload)
            | Render    -> ctrl'.Render (Option.get(msg.Payload) :?> State)
            | Connected -> Console.Log("CONNECTED")
            | _         -> Console.Log("Unknown Event:", msg)
        | _ -> Console.Log("no controller set")

  (*   __  __       _
      |  \/  | __ _(_)_ __
      | |\/| |/ _` | | '_ \
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  let Main : unit =
    let widget = new Patches.Root()
    let ctrl = new ViewController<State> (widget)

    let context = new ClientContext()

    context.Controller <- ctrl
    context.Start()

    // ctrl.Render store
    // store.Subscribe (fun store' _ -> ctrl.Render store')
