namespace Iris.Web.Worker

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =

  open Iris.Core.Types
  open Iris.Web.Core
  open Iris.Web.Views

  type WorkerEvent = { ports : MessagePort array }

  (*
                                                     +-----------------+
                                                     |                 |
                                                     |  BROWSER        |
     +---------------+      +-----------------+      |  WINDOW         |
     |               |      |                 |<---->|                 |
     |  IRIS         |      |  SHARED WORKER  |      +-----------------+
     |  SERVICE      +<---->+                 |
     |               |      |                 |<---->+-----------------+
     +---------------+      +-----------------+      |                 |
                                                     |  BROWSER        |
                                                     |  WINDOW         |
                                                     |                 |
                                                     +-----------------+
  *)

  type GlobalContext() =
    let mutable connections = new Array<MessagePort>()
    let mutable store = new Store<State>(reducer, State.Empty)
    let mutable socket : WebSocket = Unchecked.defaultof<_>

    let broadcast (ev : ClientEvent) =
      connections.ForEach(fun (c,_,_) -> c.PostMessage(ev, Array.empty); true)
      |> ignore

    let notify (action : ClientAction) : ClientEvent =
      { Type = action; Payload = Unchecked.defaultof<_> }


    (*

     +--------------+               +---------------+              +----------------+
     | IRIS SERVICE |   ApiAction   | SHARED WORKER | ClientAction | BROWSER WINDOW |
     |              |               |               |              |                |
     |              |   AddPatch    |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |  UpdatePatch  |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |  RemovePatch  |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |               |               |              |                |
     |              |   AddIOBox    |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |  UpdateIOBox  |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |  RemoveIOBox  |               |    Render    |                |
     |              | ------------> | update Store  | -----------> | re-render DOM  |
     |              |               |               |              |                |
     |              |  UpdateIOBox  |               | UpdateIOBox  |                |
  <--|  relays msg  | <------------ | update Store  | <----------- |  edit IOBox    |
     |              |               |               |              |                |
     |              |    AddCue     |               |    AddCue    |                |
  <--|  relays msg  | <------------ | update Store  | <----------- |  create Cue    |
     |              |  UpdateCue    |               |  UpdateCue   |                |
  <--|  relays msg  | <------------ | update Store  | <----------- |   edits Cue    |
     |              |  RemoveCue    |               |  RemoveCue   |                |
  <--|  relays msg  | <------------ | update Store  | <----------- |  remove Cue    |
     |              |               |               |              |                |
     +--------------+               +---------------+              +----------------+

    *)

    let onSocketMessage (ev : MessageEvent) : unit = 
      let msg = JSON.Parse(ev.Data :?> string) :?> Message
      let parsed =
        match msg.Type with
          | ApiAction.AddPatch    -> PatchEvent (AddPatch,    msg.Payload :?> Patch)
          | ApiAction.UpdatePatch -> PatchEvent (UpdatePatch, msg.Payload :?> Patch)
          | ApiAction.RemovePatch -> PatchEvent (RemovePatch, msg.Payload :?> Patch)

          | ApiAction.AddIOBox    -> IOBoxEvent (AddIOBox,    msg.Payload :?> IOBox)
          | ApiAction.UpdateIOBox -> IOBoxEvent (UpdateIOBox, msg.Payload :?> IOBox)
          | ApiAction.RemoveIOBox -> IOBoxEvent (RemoveIOBox, msg.Payload :?> IOBox)

      in store.Dispatch parsed
      broadcast { Type = Render; Payload = store.State }

    (*                      _                   _             
         ___ ___  _ __  ___| |_ _ __ _   _  ___| |_ ___  _ __ 
        / __/ _ \| '_ \/ __| __| '__| | | |/ __| __/ _ \| '__|
       | (_| (_) | | | \__ \ |_| |  | |_| | (__| || (_) | |   
        \___\___/|_| |_|___/\__|_|   \__,_|\___|\__\___/|_|   
    *)
    do
      let s = new WebSocket("ws://localhost:8080")
      s.Onopen  <- (fun _   -> broadcast <| notify Connected)
      s.Onclose <- (fun _   -> broadcast <| notify Disconnected)
      s.Onerror <- (fun err -> broadcast <| notify ConnectionError)
      s.Onmessage <- (fun msg -> onSocketMessage msg)

    (*--------------------------------------------------------------------------

       +-------------+                  +-------------+
       |             |   ClientAction   |             |
       |  SHARED     | ---------------> | BROWSER     |
       |  WORKER     | <--------------- | WINDOW      |
       |             |                  |             |
       +-------------+                  +-------------+

    ---------------------------------------------------------------------------*)
    
    member __.OnClientMsg (msg : MessageEvent) : unit =
      let parsed = msg.Data :?> ClientEvent
      match parsed.Type with
        | Add    ->
          let data = parsed.Payload :?> int
          broadcast { Type = Render; Payload = data }

        | Update ->
          let data = parsed.Payload :?> string
          broadcast { Type = Render; Payload = data }

        | Remove ->
          let data = parsed.Payload :?> string
          broadcast { Type = Render; Payload = data }

    member __.Clients with get () = connections
    member __.Store with get () = store
    member __.Socket with get () = socket

    member __.Broadcast (msg : ClientEvent) : unit =
      connections.ForEach(fun (port, _, _) ->
                          port.PostMessage(msg, Array.empty)
                          true) |> ignore

    member __.Send (msg : ClientEvent)  : unit = socket.Send("hllo")
    member __.Log (thing : obj) : unit = broadcast { Type = Log; Payload = thing }


  let initialize (context : GlobalContext) ev =
    let port = ev.ports.[0]
    port.Onmessage <- context.OnClientMsg
    context.Clients.Push(port) |> ignore


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
