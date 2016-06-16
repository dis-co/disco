namespace Iris.Web.Core

[<AutoOpen>]
module Client =

  open Fable.Core
  open Fable.Import
  open Fable.Import.JS
  open Fable.Import.Browser
  open Iris.Core

  type ClientContext() =
    let resource = "Iris.Web.Worker.js"

    let mutable session : Session option = None
    let mutable ctrl : ViewController<State,ClientContext> option = None 
    let mutable worker = new SharedWorker(resource)

    let close _ =
      match session with
        | Some session' -> 
          let msg = ClientMessage.Close(session')
           in worker.port.postMessage(msg, [||])
        | _ -> ()

    do window.onunload <- fun ev ->
      close ()
      failwith "Oh no"

    member self.Session
      with get () = session

    member self.Controller
      with set c  = ctrl <- Some(c)

    member self.Start() =
      worker.onerror <- (fun e -> printfn "SharedWorker Error: %s" <| JSON.stringify(e))
      worker.port.onmessage <- (fun msg ->
        self.HandleMsg (msg.data :?> ClientMessage<State>)
        failwith "oops")
      worker.port.start()

    member self.Trigger(msg : ClientMessage<State>) =
      worker.port.postMessage(msg, [||]) //
      
    member self.HandleMsg (msg : ClientMessage<State>) : unit =
      match ctrl with
        | Some(ctrl') ->
          match msg with
            // initialize this clients session variable
            | ClientMessage.Initialized(session') ->
              session <- Some(session')
              printfn "Initialized with Session: %A" session'

            // initialize this clients session variable
            | ClientMessage.Closed(session') ->
              printfn "A client closed its session: %A" session'

            | ClientMessage.Stopped ->
              printfn "Worker stopped, restarting..."
              worker <- new SharedWorker(resource)
              self.Start()

            // Re-render the current view tree with a new state
            | ClientMessage.Render(state) ->
              ctrl'.Render state self

            // Log a message from Worker on this client
            | ClientMessage.Log(thing) ->
              printfn "SharedWorker %A" thing

            | ClientMessage.Connected ->
              printfn "CONNECTED!"

            | ClientMessage.Disconnected ->
              printfn "DISCONNECTED!"

            // initialize this clients session variable
            | ClientMessage.Error(reason) ->
              printfn "SharedWorker Error: %A" reason

            | _ -> printfn "Unknown Event: %A" msg

        | _ -> printfn "no controller set"

