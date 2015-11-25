namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Worker =

  open Iris.Core.Types
  open Iris.Web.Core

  (*---------------------------------------------------------------------------*
       _        __         _             
      \ \      / /__  _ __| | _____ _ __ 
       \ \ /\ / / _ \| '__| |/ / _ \ '__|
        \ V  V / (_) | |  |   <  __/ |   
         \_/\_/ \___/|_|  |_|\_\___|_|   

  *----------------------------------------------------------------------------*)
  [<Stub>]
  type SharedWorker =
      [<DefaultValue>]
      val mutable onerror : (obj -> unit)

      [<DefaultValue>]
      val mutable port : MessagePort

      [<Inline "new SharedWorker($url)">]
      new(url : string) = {}


  type WorkerEvent = { ports : MessagePort array }

  (*---------------------------------------------------------------------------*
       ____ _       _           _  ____            _            _   
      / ___| | ___ | |__   __ _| |/ ___|___  _ __ | |_ _____  _| |_ 
     | |  _| |/ _ \| '_ \ / _` | | |   / _ \| '_ \| __/ _ \ \/ / __|
     | |_| | | (_) | |_) | (_| | | |__| (_) | | | | ||  __/>  <| |_ 
      \____|_|\___/|_.__/ \__,_|_|\____\___/|_| |_|\__\___/_/\_\\__|

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

  *----------------------------------------------------------------------------*)
  
  type GlobalContext() =
    let mutable connections : MessagePort array = Array.empty
    let mutable store = new Store<State>(reducer, State.Empty)
    let mutable socket = Option<WebSocket>.None

    let send (ev : ClientMessage) (port : MessagePort) : unit =
      port.PostMessage(ev, Array.empty)

    let broadcast (ev : ClientMessage) : unit =
      Array.iter (send ev) connections

    let notify (action : ClientAction) : ClientMessage =
      { Type = action; Payload = None }

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
      let msg = JSON.Parse(ev.Data :?> string) :?> ApiMessage
      let parsed =
        match msg.Type with
          | ApiAction.AddPatch    -> PatchEvent (AddPatch,    msg.Payload :?> Patch)
          | ApiAction.UpdatePatch -> PatchEvent (UpdatePatch, msg.Payload :?> Patch)
          | ApiAction.RemovePatch -> PatchEvent (RemovePatch, msg.Payload :?> Patch)

          | ApiAction.AddIOBox    -> IOBoxEvent (AddIOBox,    msg.Payload :?> IOBox)
          | ApiAction.UpdateIOBox -> IOBoxEvent (UpdateIOBox, msg.Payload :?> IOBox)
          | ApiAction.RemoveIOBox -> IOBoxEvent (RemoveIOBox, msg.Payload :?> IOBox)

      in store.Dispatch parsed
      broadcast { Type = Render; Payload = Some(store.State :> obj) }

    (*                      _                   _             
         ___ ___  _ __  ___| |_ _ __ _   _  ___| |_ ___  _ __ 
        / __/ _ \| '_ \/ __| __| '__| | | |/ __| __/ _ \| '__|
       | (_| (_) | | | \__ \ |_| |  | |_| | (__| || (_) | |   
        \___\___/|_| |_|___/\__|_|   \__,_|\___|\__\___/|_|   
    *)
    do
      let s = new WebSocket("ws://localhost:8080")
      s.Onopen  <- (fun _ -> broadcast <| notify Connected)
      s.Onclose <- (fun _ -> broadcast <| notify Disconnected)
      s.Onerror <- (fun _ -> broadcast <| notify ConnectionError)
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
      let parsed = msg.Data :?> ClientMessage
      match parsed.Type with
        | Add    ->
          let data = Option.get(parsed.Payload) :?> int
          broadcast { Type = Render; Payload = Some(data :> obj) }

        | Update ->
          let data = Option.get(parsed.Payload) :?> string
          broadcast { Type = Render; Payload = Some(data :> obj) }

        | Remove ->
          let data = Option.get(parsed.Payload) :?> string
          broadcast { Type = Render; Payload = Some(data :> obj) }

        | _ -> __.Log(parsed)

    member __.Add (port : MessagePort) : unit =
      port.Onmessage <- __.OnClientMsg
      Array.append connections [| port |]
      |> ignore

    member __.Store with get () = store
    member __.Socket with get () = socket

    member __.Broadcast (msg : ClientMessage) : unit =
      broadcast msg

    member __.Send (msg : ClientMessage)  : unit =
      match socket with
        | Some(thing) -> thing.Send(JSON.Stringify(msg))
        | None -> __.Log("Not connected")

    member __.Log (thing : obj) : unit =
      broadcast { Type = Log; Payload = Some(thing) }
