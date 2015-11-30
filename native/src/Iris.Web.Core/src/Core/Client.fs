namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Client =

  open Iris.Core.Types

  type ClientContext() =
    let resource = "Iris.Web.Worker.js"

    let mutable session = Option<Session>.None
    let mutable ctrl = Option<ViewController<State,ClientContext>>.None 
    let mutable worker = new SharedWorker(resource)

    let close _ =
      if (Option.isSome session)
      then let msg = ClientMessage.Close(Option.get session)
            in worker.port.PostMessage(msg, Array.empty)

    do JS.Window.Onunload <- close

    member self.Session
      with get () = session

    member self.Controller
      with set c  = ctrl <- Some(c)

    member self.Start() =
      worker.onerror <- (fun e -> Console.Log("SharedWorker Error: " + JSON.Stringify(e)))
      worker.port.Onmessage <- (fun msg ->
        self.HandleMsg (msg.Data :?> ClientMessage<State>))
      worker.port.Start()

    member self.Trigger(msg : ClientMessage<State>) =
      worker.port.PostMessage(msg, Array.empty)
      
    member self.HandleMsg (msg : ClientMessage<State>) : unit =
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

            | ClientMessage.Stopped ->
              Console.Log("Worker stopped, restarting...")
              worker <- new SharedWorker(resource)
              self.Start()

            // Re-render the current view tree with a new state
            | ClientMessage.Render(state) ->
              ctrl'.Render state self

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

