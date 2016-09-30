namespace Iris.Web.Core

open Iris.Core
open Iris.Web.Core

open Fable.Core
open Fable.Core.JsInterop
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

  [<Emit("$0.binaryType = $1")>]
  member __.BinaryType
    with set (str: string) = failwith "ONLY JS"

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
    with set (cb: MessageEvent<ArrayBuffer> -> unit) = failwith "ONLY JS"

  [<Emit("$0.close()")>]
  member self.Close() = failwith "ONLY JS"

  [<Emit("$0.send($1)")>]
  member self.Send(stuff: Binary.Buffer) = failwith "ONLY JS"

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
  let onConnect (_: WorkerEvent<string> -> unit) = failwith "ONLY JS"

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
    | IRIS SERVICE | StateMachine  | SHARED WORKER | ClientAction | BROWSER WINDOW |
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

type ClientMessagePort = MessagePort<string>
type PortMap = Map<Id,ClientMessagePort>

type GlobalContext() =
  let mutable count = 0
  let mutable store = new Store(State.Empty)
  let mutable socket : (string * WebSocket) option = None

  let ports : PortMap = Map.Create<Id,ClientMessagePort>()

  member self.ConnectServer(addr) =
    let init _ =
      let sock = new WebSocket(addr)

      sock.BinaryType <- "arraybuffer"

      sock.OnError <- sprintf "Error: %A" >> self.Log

      sock.OnOpen <- fun _ ->
        self.Broadcast ClientMessage.Connected

      sock.OnClose <- fun _ ->
        self.Broadcast ClientMessage.Disconnected

      sock.OnMessage <- fun (ev: MessageEvent<ArrayBuffer>) ->
        match Binary.decode ev.Data with
        | Some sm -> self.OnSocketMessage sm
        | _       -> self.Log "Unable to parse received message. Ignoring."

      socket <- Some (addr, sock)

    match socket with
    | Some (current, sock) ->
      if addr <> current then
        sock.Close()
        init()
    | _  -> init ()

  [<Emit("$0.close()")>]
  member self.Close () = failwith "JS Only"

  (*-------------------------------------------------------------------------*
       ____             _        _
      / ___|  ___   ___| | _____| |_
      \___ \ / _ \ / __| |/ / _ \ __|
       ___) | (_) | (__|   <  __/ |_
      |____/ \___/ \___|_|\_\___|\__| Message Handler

   *-------------------------------------------------------------------------*)

  member self.OnSocketMessage(ev: StateMachine) : unit =
    match ev with
    | LogMsg (level,str) -> self.Log (sprintf "[%A] %s" level str)
    | _ ->
      match ev with
      | DataSnapshot state ->
        store <- new Store(state)
        self.Broadcast <| ClientMessage.Render(store.State)
      | _ ->
        try
          store.Dispatch ev
        with
          | exn -> self.Log (sprintf "Crash: %s" exn.Message)
        self.Broadcast <| ClientMessage.Render(store.State)

  (*-------------------------------------------------------------------------*
       ____ _ _            _
      / ___| (_) ___ _ __ | |_
     | |   | | |/ _ \ '_ \| __|
     | |___| | |  __/ | | | |_
      \____|_|_|\___|_| |_|\__| Message Handler

   *------------------------------------------------------------------------*)

  member self.OnClientMessage(msg : MessageEvent<string>) : unit =
    match ofJson<ClientMessage<State>> msg.Data with
    | ClientMessage.Close(session) -> self.UnRegister(session)

    | ClientMessage.Stop ->
      self.Broadcast <| ClientMessage.Stopped
      self.Close ()

    | ClientMessage.Connect(address) ->
      self.Log (sprintf "connecting to %s" address)
      self.ConnectServer(address)

    | ClientMessage.Event(_, ev) -> self.SendServer(ev)

    | _ -> self.Log "clients-only message ignored"


  member self.Register (port : MessagePort<string>) =
    count <- count + 1                     // increase the connection count
    let session = Id.Create()             // create a session id
    port.OnMessage <- self.OnClientMessage   // register handler for client messages
    ports.set(session, port)              // remember the port in our map
    |> ignore

    ClientMessage.Initialized(session)    // tell client all is good
    |> self.SendClient port

    ClientMessage.Render(store.State)     // ask client to render
    |> self.SendClient port

  member self.UnRegister (session: Id) =
    count <- count - 1
    if ports.delete(session) then
      self.Broadcast(ClientMessage.Closed(session))

  (* -------------------------------------------------------------------------

                +-------------+                  +-------------+
                |             |                  |             |
                |  SHARED     | ---------------> | BROWSER     |
                |  WORKER     | <--------------- | WINDOW      |
                |             |                  |             |
                +-------------+                  +-------------+

  ------------------------------------------------------------------------- *)

  member self.Store  with get () = store
  member self.Socket with get () = socket

  member self.SendServer (msg: StateMachine) =
    let buffer = Binary.encode msg
    match socket with
    | Some (_, server) -> server.Send(buffer)
    | _                -> self.Log "Cannot update server: no connection."

  member self.SendClient (port: ClientMessagePort) (msg: ClientMessage<State>) =
    port.PostMessage(toJson msg)

  member self.Broadcast (msg : ClientMessage<State>) : unit =
    let handler port _ _ = self.SendClient port msg
    let func = new System.Func<ClientMessagePort,Id,PortMap,unit> (handler)
    ports.forEach(func)

  member self.Multicast (session: Id, msg: ClientMessage<State>) : unit =
    let handler port token _ =
      if session <> token then
        self.SendClient port msg
    let func = new System.Func<ClientMessagePort,Id,PortMap,unit> (handler)
    ports.forEach(func)

  member self.Log (thing : ClientLog) : unit =
    self.Broadcast <| ClientMessage.ClientLog(thing)
