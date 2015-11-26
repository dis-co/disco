namespace Iris.Web

open WebSharper
open WebSharper.JQuery
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type ClientContext() =
    let mutable session = Option<Session>.None
    let mutable ctrl = Option<ViewController<State>>.None 

    let worker = new SharedWorker("Iris.Web.Worker.js")

    member __.Controller
      with set c  = ctrl <- Some(c)

    member __.Start() =
      worker.onerror <- (fun e -> Console.Log("error: ", e))
      worker.port.Onmessage <- (fun msg -> __.HandleMsg (msg.Data :?> ClientMessage<State>))
      worker.port.Start()

      JS.Window.Onunload <- (fun _ -> __.Close())

    member __.Close() =
      if (Option.isSome session)
      then let msg = ClientMessage.Close(Option.get session)
            in worker.port.PostMessage(msg, Array.empty)

    member __.HandleMsg (msg : ClientMessage<State>) : unit =
      match ctrl with
        | Some(ctrl') ->
          match msg with
            // initialize this clients session variable
            | ClientMessage.Initialized(session') ->
              session <- Some(session')
              Console.Log("Initialized with Session: ", session')

            // initialize this clients session variable
            | ClientMessage.Closed(session') ->
              Console.Log("A client closed its session: ", session')

            // Re-render the current view tree with a new state
            | ClientMessage.Render(state) ->
              ctrl'.Render state

            // Log a message from Worker on this client
            | ClientMessage.Log(thing) ->
              Console.Log("SharedWorker", thing)

            | ClientMessage.Connected -> Console.Log("CONNECTED!")
            | ClientMessage.Disconnected -> Console.Log("DISCONNECTED!")

            // initialize this clients session variable
            | ClientMessage.Error(reason) ->
              Console.Log("SharedWorker Error: ", reason)

            | _ -> Console.Log("Unknown Event:", msg)

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
