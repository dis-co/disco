namespace Iris.Web.Core

#nowarn "1182"

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
                                                     |     BROWSER     |
     +---------------+      +-----------------+      |     WINDOW      |
     |               |      |                 |<---->|                 |
     |     IRIS      |----->|     SHARED      |      +-----------------+
     |    SERVICE    +<-----+     WORKER      |      +-----------------+
     |               |      |                 |<---->|                 |
     +---------------+      +-----------------+      |     BROWSER     |
                                                     |     WINDOW      |
                                                     |                 |
                                                     +-----------------+


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

  *----------------------------------------------------------------------------*)

  let flip f b a = f a b

  let mkSession () =
    let time = (new Date()).GetTime()
    let fac = Math.Random()
    JSON.Stringify(Math.Floor(float(time) * fac))

  type Ports [<Inline "{}">]() = class end
  
  type GlobalContext() =
    let mutable count = 0
    let mutable ports = new Ports()
    let mutable store = new Store<State>(Reducer, State.Empty)
    let mutable socket = Option<WebSocket>.None

    [<Direct "$ports[$id] = $port">]
    let addImpl (ports : Ports) id port : unit = X

    [<Direct "delete $ports[$id]">]
    let rmImpl (ports : Ports) id : unit = X

    [<Direct "Object.keys($ports)">]
    let allKeysImpl (ports : Ports) : string array = X

    [<Direct "$ports[$key]">]
    let getImpl (ports : Ports) (key : string) : MessagePort = X

    [<Inline "void(self.close())">]
    let close () = X

    let send (msg : ClientMessage<State>) (port : MessagePort) : unit =
      port.PostMessage(msg, Array.empty)

    let broadcast (msg : ClientMessage<State>) : unit =
      Array.map (getImpl ports) (allKeysImpl ports)
      |> Array.iter (send msg)

    let multicast (id : Session) (msg : ClientMessage<State>) : unit =
      allKeysImpl ports
      |> Array.filter (fun str -> id <> str)
      |> Array.map (getImpl ports)
      |> Array.iter (send msg)

    let remove (id : Session) =
      count <- count - 1
      rmImpl ports id
      broadcast <| ClientMessage.Closed(id)

    let log (o : obj) =
      Console.Log(o)
      broadcast <| ClientMessage.Log(o)

    (*-------------------------------------------------------------------------*
        ____             _        _   
       / ___|  ___   ___| | _____| |_ 
       \___ \ / _ \ / __| |/ / _ \ __|
        ___) | (_) | (__|   <  __/ |_ 
       |____/ \___/ \___|_|\_\___|\__| Message Handler

     *-------------------------------------------------------------------------*)

    let onSocketMessage (ev : MessageEvent) : unit = 
      let msg = JSON.Parse(ev.Data :?> string) :?> ApiMessage
      let parsed =
        match msg.Type with
          | ApiAction.AddPatch    -> PatchEvent(Create, msg.Payload :?> Patch)
          | ApiAction.UpdatePatch -> PatchEvent(Update, msg.Payload :?> Patch)
          | ApiAction.RemovePatch -> PatchEvent(Delete, msg.Payload :?> Patch)

          | ApiAction.AddIOBox    -> IOBoxEvent(Create, msg.Payload :?> IOBox)
          | ApiAction.UpdateIOBox -> IOBoxEvent(Update, msg.Payload :?> IOBox)
          | ApiAction.RemoveIOBox -> IOBoxEvent(Delete, msg.Payload :?> IOBox)

      in store.Dispatch parsed
      broadcast <| ClientMessage.Render(store.State)

    (*-------------------------------------------------------------------------*
        ____ _ _            _   
       / ___| (_) ___ _ __ | |_ 
      | |   | | |/ _ \ '_ \| __|
      | |___| | |  __/ | | | |_ 
       \____|_|_|\___|_| |_|\__| Message Handler

     *------------------------------------------------------------------------*)

    let onClientMessage (msg : MessageEvent) : unit =
      let parsed = msg.Data :?> ClientMessage<State>
      match parsed with
        | ClientMessage.Close(session) -> remove(session)

        | ClientMessage.Stop ->
          broadcast <| ClientMessage.Stopped
          close ()

        | ClientMessage.Event(session, event') ->
          match event' with
            | IOBoxEvent(_,_) as ev -> store.Dispatch ev
            | PatchEvent(_,_) as ev -> store.Dispatch ev
            | _ -> log "other are not supported in-worker" 
          multicast session <| ClientMessage.Render(store.State)

        | _ -> log "clients-only message ignored" 

    let add (port : MessagePort) =
      count <- count + 1                    // increase the connection count
      let id = mkSession()                  // create a session id
      port.Onmessage <- onClientMessage     // register callback on port
      addImpl ports id port                 // add port to ports object

      [ ClientMessage.Initialized(id)       // tell client all is good
      ; ClientMessage.Render(store.State) ] // tell client to render
      |> List.map (flip send port)
      |> ignore

    (*                      _                   _             
         ___ ___  _ __  ___| |_ _ __ _   _  ___| |_ ___  _ __ 
        / __/ _ \| '_ \/ __| __| '__| | | |/ __| __/ _ \| '__|
       | (_| (_) | | | \__ \ |_| |  | |_| | (__| || (_) | |   
        \___\___/|_| |_|___/\__|_|   \__,_|\___|\__\___/|_|   
    *)
    do
      let s = new WebSocket("ws://localhost:8080")
      s.Onopen  <- (fun _ -> broadcast <| ClientMessage.Connected)
      s.Onclose <- (fun _ -> broadcast <| ClientMessage.Disconnected)
      s.Onerror <- (fun e -> broadcast <| ClientMessage.Error(JSON.Stringify(e)))
      s.Onmessage <- (fun msg -> onSocketMessage msg)
      socket <- Some(s)

    (*--------------------------------------------------------------------------

                 +-------------+                  +-------------+
                 |             |                  |             |
                 |  SHARED     | ---------------> | BROWSER     |
                 |  WORKER     | <--------------- | WINDOW      |
                 |             |                  |             |
                 +-------------+                  +-------------+

    ---------------------------------------------------------------------------*)

    member __.Add (port : MessagePort) = add port
    member __.Store  with get () = store
    member __.Socket with get () = socket

    member __.Send (msg : ClientMessage<State>)  : unit =
      match socket with
        | Some(thing) -> thing.Send(JSON.Stringify(msg))
        | None -> __.Log("Not connected")

    member __.Log (thing : obj) : unit =
      broadcast <| ClientMessage.Log(thing)
