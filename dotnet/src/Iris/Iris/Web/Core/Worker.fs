namespace Iris.Web.Core

open Iris.Core
open Iris.Web.Core

open Fable.Core
open Fable.Import
open Fable.Import.JS

//  __  __                                _____                 _
// |  \/  | ___  ___ ___  __ _  __ _  ___| ____|_   _____ _ __ | |_
// | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \  _| \ \ / / _ \ '_ \| __|
// | |  | |  __/\__ \__ \ (_| | (_| |  __/ |___ \ V /  __/ | | | |_
// |_|  |_|\___||___/___/\__,_|\__, |\___|_____| \_/ \___|_| |_|\__|
//                             |___/

[<Emit("new MessageEvent()")>]
type MessageEvent<'data> =

  [<Emit("$0.data")>]
  member __.Data
    with get () : 'data = failwith "ONLY JS"

//  __  __                                ____            _
// |  \/  | ___  ___ ___  __ _  __ _  ___|  _ \ ___  _ __| |_
// | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \ |_) / _ \| '__| __|
// | |  | |  __/\__ \__ \ (_| | (_| |  __/  __/ (_) | |  | |_
// |_|  |_|\___||___/___/\__,_|\__, |\___|_|   \___/|_|   \__|
//                             |___/

[<Emit("new MessagePort()")>]
type MessagePort<'data>() =

  [<Emit("$0.onmessage = $1")>]
  member __.OnMessage
    with set (cb: MessageEvent<'data> -> unit) = failwith "ONLY JS"

  [<Emit("$0.postMessage($1)")>]
  member __.PostMessage(_: 'data) = failwith "ONLY JS"

  [<Emit("$0.start()")>]
  member __.Start() = failwith "ONLY JS"

  [<Emit("$0.close()")>]
  member __.Close() = failwith "ONLY JS"


// __        __         _             _____                 _
// \ \      / /__  _ __| | _____ _ __| ____|_   _____ _ __ | |_
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|  _| \ \ / / _ \ '_ \| __|
//   \ V  V / (_) | |  |   <  __/ |  | |___ \ V /  __/ | | | |_
//    \_/\_/ \___/|_|  |_|\_\___|_|  |_____| \_/ \___|_| |_|\__|

[<Emit("new WorkerEvent()")>]
type WorkerEvent<'data>() =

  [<Emit("$0.ports")>]
  member __.Ports
    with get () : MessagePort<'data> array = failwith "ONLY JS"

// __        __   _    ____             _        _
// \ \      / /__| |__/ ___|  ___   ___| | _____| |_
//  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
//   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_
//    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|

[<Emit("new WebSocket($0)")>]
type WebSocket(url: string)  =

  [<Emit("$0.onerror = $1")>]
  member __.OnError
    with set (cb: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onopen = $1")>]
  member __.OnOpen
    with set (cb: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onclose = $1")>]
  member __.OnClose
    with set (cb: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onmessage = $1")>]
  member __.OnMessage
    with set (cb: MessageEvent<string> -> unit) = failwith "ONLY JS"

  [<Emit("$0.close()")>]
  member self.Close() = failwith "ONLY JS"

  [<Emit("$0.send($1)")>]
  member self.Send(stuff: string) = failwith "ONLY JS"

// __        __         _
// \ \      / /__  _ __| | _____ _ __
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|
//   \ V  V / (_) | |  |   <  __/ |
//    \_/\_/ \___/|_|  |_|\_\___|_|

[<AutoOpen>]
module Worker =

  [<Emit "importScripts ? importScripts($0) : null">]
  let importScript (_: string) : unit = failwith "JS ONLY"

  [<Emit("onconnect = $0")>]
  let onConnect (_: WorkerEvent<ClientMessage<State>> -> unit) = failwith "ONLY JS"

  [<Emit("JSON.stringify($0)")>]
  let inline stringify (thing: ^a) : string = failwith "ONLY JS"

  [<Emit("JSON.parse($0)")>]
  let inline parse (thing: string) : ^a = failwith "ONLY JS"


(* ///////////////////////////////////////////////////////////////////////////////
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

/////////////////////////////////////////////////////////////////////////////// *)

type ClientMessagePort = MessagePort<ClientMessage<State>>
type PortMap = Map<Session,ClientMessagePort>

type GlobalContext() =
  let mutable count = 0
  let mutable store = new Store<State>(Reducer, State.Empty)
  let mutable socket : (string * WebSocket) option = None

  let ports : PortMap = Map.Create<Session,ClientMessagePort>()

  member __.Connect(addr) =
    let init _ =
      let sock = new WebSocket(addr)

      sock.OnOpen <- fun _ ->
        __.Broadcast ClientMessage.Connected

      sock.OnClose <- fun _ ->
        __.Broadcast ClientMessage.Disconnected

      sock.OnMessage <- fun (ev: MessageEvent<string>) ->
        __.Log ev.Data

      socket <- Some (addr, sock)

    match socket with
    | Some (current, sock) ->
      if addr <> current then
        sock.Close()
        init()
    | _  -> init ()

  [<Emit "$0.close()">]
  member __.Close () = failwith "JS Only"

  (*-------------------------------------------------------------------------*
       ____             _        _
      / ___|  ___   ___| | _____| |_
      \___ \ / _ \ / __| |/ / _ \ __|
       ___) | (_) | (__|   <  __/ |_
      |____/ \___/ \___|_|\_\___|\__| Message Handler

    *-------------------------------------------------------------------------*)

  // member __.OnSocketMessage(ev : MessageEvent<string>) : unit =
  //   let msg : ApiAction = parse ev.Data

  //   let handleRender msg =
  //     store.Dispatch msg
  //     __.Broadcast <| ClientMessage.Render(store.State)

  //   match msg with
  //     | AddPatch    patch -> PatchEvent(Create, patch) |> handleRender
  //     | UpdatePatch patch -> PatchEvent(Update, patch) |> handleRender
  //     | RemovePatch patch -> PatchEvent(Delete, patch) |> handleRender

  //     | AddIOBox    iobox -> IOBoxEvent(Create, iobox) |> handleRender
  //     | UpdateIOBox iobox -> IOBoxEvent(Update, iobox) |> handleRender
  //     | RemoveIOBox iobox -> IOBoxEvent(Delete, iobox) |> handleRender

  //     | LogStr str -> this.Log str

  (*-------------------------------------------------------------------------*
       ____ _ _            _
      / ___| (_) ___ _ __ | |_
     | |   | | |/ _ \ '_ \| __|
     | |___| | |  __/ | | | |_
      \____|_|_|\___|_| |_|\__| Message Handler

   *------------------------------------------------------------------------*)

  member __.OnClientMessage(msg : MessageEvent<ClientMessage<State>>) : unit =
    let handleAppEvent (session: Session) (appevent: AppEvent) =
      match appevent with
      | IOBoxEvent (Create,iobox) as ev -> __.SendServer(AddIOBox iobox)
      | IOBoxEvent (Read,iobox)   as ev -> __.SendServer(AddIOBox iobox)
      | IOBoxEvent (Update,iobox) as ev -> __.SendServer(UpdateIOBox iobox)
      | IOBoxEvent (Delete,iobox) as ev -> __.SendServer(RemoveIOBox iobox)

      | PatchEvent (Create,patch) as ev -> __.SendServer(AddPatch patch)
      | PatchEvent (Read,patch)   as ev -> __.SendServer(AddPatch patch)
      | PatchEvent (Update,patch) as ev -> __.SendServer(UpdatePatch patch)
      | PatchEvent (Delete,patch) as ev -> __.SendServer(RemovePatch patch)

      | CueEvent (Create,cue) as ev -> __.SendServer(AddCue cue)
      | CueEvent (Read,cue)   as ev -> __.SendServer(AddCue cue)
      | CueEvent (Update,cue) as ev -> __.SendServer(UpdateCue cue)
      | CueEvent (Delete,cue) as ev -> __.SendServer(RemoveCue cue)

      | _ -> __.Log "other are currently not supported in-worker"

      store.Dispatch appevent
      __.Multicast(session, ClientMessage.Render(store.State))

    match msg.Data with
    | ClientMessage.Close(session) -> __.UnRegister(session)

    | ClientMessage.Undo ->
      store.Undo()
      __.Broadcast <| ClientMessage.Render(store.State)
      __.SendServer (LogStr "Undo!")

    | ClientMessage.Redo ->
      store.Redo()
      __.Broadcast <| ClientMessage.Render(store.State)
      __.SendServer (LogStr "Redo!")

    | ClientMessage.Stop ->
      __.Broadcast <| ClientMessage.Stopped
      __.Close ()

    | ClientMessage.Connect(address) ->
      __.Log (sprintf "connecting to %s" address)
      __.Connect(address)

    | ClientMessage.Event(session, ev) -> handleAppEvent session ev

    | _ -> __.Log "clients-only message ignored"

  member __.Register (port : MessagePort<ClientMessage<State>>) =
    count <- count + 1                     // increase the connection count
    let session = mkGuid ()               // create a session id
    port.OnMessage <- __.OnClientMessage   // register handler for client messages
    ports.set(session, port)              // remember the port in our map
    |> ignore

    ClientMessage.Initialized(session)    // tell client all is good
    |> __.SendClient port

    ClientMessage.Render(store.State)     // ask client to render
    |> __.SendClient port

  member __.UnRegister (session: Session) =
    count <- count - 1
    if ports.delete(session) then
      __.Broadcast(ClientMessage.Closed(session))

  (* -------------------------------------------------------------------------

                +-------------+                  +-------------+
                |             |                  |             |
                |  SHARED     | ---------------> | BROWSER     |
                |  WORKER     | <--------------- | WINDOW      |
                |             |                  |             |
                +-------------+                  +-------------+

  ------------------------------------------------------------------------- *)

  member __.Store  with get () = store
  member __.Socket with get () = socket

  member __.SendServer (msg: ApiAction) =
    match socket with
    | Some (_, server) -> server.Send(stringify msg)
    | _                -> __.Log "Cannot update server: no connection."

  member __.SendClient (port: ClientMessagePort) (msg: ClientMessage<State>) =
    port.PostMessage(msg)

  member __.Broadcast (msg : ClientMessage<State>) : unit =
    let handler port _ _ = __.SendClient port msg
    let func = new System.Func<ClientMessagePort,Session,PortMap,unit> (handler)
    ports.forEach(func)

  member __.Multicast (session: Session, msg: ClientMessage<State>) : unit =
    let handler port token _ =
      if session <> token then
        __.SendClient port msg
    let func = new System.Func<ClientMessagePort,Session,PortMap,unit> (handler)
    ports.forEach(func)

  member __.Log (thing : ClientLog) : unit =
    __.Broadcast <| ClientMessage.Log(thing)
