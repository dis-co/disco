namespace Iris.Web.Core

open Iris.Core
open Iris.Web.Core

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.JS
open System.Collections.Generic

//  __  __                                _____                 _
// |  \/  | ___  ___ ___  __ _  __ _  ___| ____|_   _____ _ __ | |_
// | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \  _| \ \ / / _ \ '_ \| __|
// | |  | |  __/\__ \__ \ (_| | (_| |  __/ |___ \ V /  __/ | | | |_
// |_|  |_|\___||___/___/\__,_|\__, |\___|_____| \_/ \___|_| |_|\__|
//                             |___/

[<Global>]
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

[<Global>]
type MessagePort<'data>() =

  [<Emit("$0.onmessage = $1")>]
  member __.OnMessage
    with set (_: MessageEvent<'data> -> unit) = failwith "ONLY JS"

  [<Emit("$0.postMessage($1)")>]
  member __.PostMessage(_: 'data): unit = failwith "ONLY JS"

  [<Emit("$0.start()")>]
  member __.Start(): unit = failwith "ONLY JS"

  [<Emit("$0.close()")>]
  member __.Close(): unit = failwith "ONLY JS"


// __        __         _             _____                 _
// \ \      / /__  _ __| | _____ _ __| ____|_   _____ _ __ | |_
//  \ \ /\ / / _ \| '__| |/ / _ \ '__|  _| \ \ / / _ \ '_ \| __|
//   \ V  V / (_) | |  |   <  __/ |  | |___ \ V /  __/ | | | |_
//    \_/\_/ \___/|_|  |_|\_\___|_|  |_____| \_/ \___|_| |_|\__|

[<Global>]
type WorkerEvent<'data>() =

  [<Emit("$0.ports")>]
  member __.Ports
    with get () : MessagePort<'data> array = failwith "ONLY JS"

// __        __   _    ____             _        _
// \ \      / /__| |__/ ___|  ___   ___| | _____| |_
//  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
//   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_
//    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|

[<Global>]
type WebSocket(_url: string)  =

  [<Emit("$0.binaryType = $1")>]
  member __.BinaryType
    with set (_: string) = failwith "ONLY JS"

  [<Emit("$0.onerror = $1")>]
  member __.OnError
    with set (_: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onopen = $1")>]
  member __.OnOpen
    with set (_: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onclose = $1")>]
  member __.OnClose
    with set (_: unit -> unit) = failwith "ONLY JS"

  [<Emit("$0.onmessage = $1")>]
  member __.OnMessage
    with set (_: MessageEvent<ArrayBuffer> -> unit) = failwith "ONLY JS"

  [<Emit("$0.close()")>]
  member self.Close() = failwith "ONLY JS"

  [<Emit("$0.send($1)")>]
  member self.Send(_: ArrayBuffer) = failwith "ONLY JS"

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
    |              |   AddPin      |               |    Render    |                |
    |              | ------------> | update Store  | -----------> | re-render DOM  |
    |              |  UpdatePin    |               |    Render    |                |
    |              | ------------> | update Store  | -----------> | re-render DOM  |
    |              |  RemovePin    |               |    Render    |                |
    |              | ------------> | update Store  | -----------> | re-render DOM  |
    |              |               |               |              |                |
    |              |  UpdatePin    |               | UpdatePin    |                |
 <--|  relays msg  | <------------ | update Store  | <----------- |  edit Pin      |
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
type PortMap = Dictionary<SessionId,ClientMessagePort>

type WorkerContext() =
  let id = IrisId.Create()
  let mutable count = 0
  let mutable socket : WebSocket option = None

  let ports : PortMap = PortMap()

  let toBytes(buffer: ArrayBuffer): byte[] =
    !!JS.Uint8Array.Create(buffer)

  member self.ConnectServer(addr) =
    let init _ =
      let sock = WebSocket(addr)

      sock.BinaryType <- "arraybuffer"

      sock.OnError <- sprintf "Error: %A" >> (self.Log LogLevel.Err)

      sock.OnOpen <- fun _ ->
        self.Broadcast ClientMessage.Connected

      sock.OnClose <- fun _ ->
        self.Broadcast ClientMessage.Disconnected

      sock.OnMessage <- fun (ev: MessageEvent<ArrayBuffer>) ->
        match toBytes ev.Data |> Binary.decode with
        | Right sm   -> self.OnSocketMessage sm
        | Left error ->
          sprintf "Unable to parse received message. %A" error
          |> self.Log LogLevel.Err

      socket <- Some sock

    match socket with
    | Some sock ->
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
    ClientMessage.Event(id, ev)
    |> self.Broadcast

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
      self.Log LogLevel.Info (sprintf "connecting to %s" address)
      self.ConnectServer(address)

    | ClientMessage.Event(_, ev) -> self.SendServer(ev)

    | msg ->
      sprintf "Client-only message ignored: %A" msg
      |> self.Log LogLevel.Debug


  member self.Register (port : MessagePort<string>) =
    count <- count + 1                     // increase the connection count
    let session = IrisId.Create()         // create a session id
    port.OnMessage <- self.OnClientMessage   // register handler for client messages
    ports.Add(session, port)              // remember the port in our map
    |> ignore

    ClientMessage.Initialized(session)    // tell client all is good
    |> self.SendClient port

  member self.UnRegister (session: IrisId) =
    count <- count - 1
    if ports.Remove(session) then
      self.Broadcast(ClientMessage.Closed(session))

  (* -------------------------------------------------------------------------

                +-------------+                  +-------------+
                |             |                  |             |
                |  SHARED     | ---------------> | BROWSER     |
                |  WORKER     | <--------------- | WINDOW      |
                |             |                  |             |
                +-------------+                  +-------------+

  ------------------------------------------------------------------------- *)

  member self.Socket with get () = socket

  member self.SendServer (msg: StateMachine) =
    let bytes = Binary.encode msg
    match socket with
    | Some server -> server.Send(!!bytes?buffer)
    | _           -> self.Log LogLevel.Err "Cannot update server: no connection."

  member self.SendClient (port: ClientMessagePort) (msg: ClientMessage<State>) =
    port.PostMessage(toJson msg)

  member self.Broadcast (msg : ClientMessage<State>) : unit =
    for KeyValue(_, port) in ports do
      self.SendClient port msg

  member self.Multicast (session: IrisId, msg: ClientMessage<State>) : unit =
    for KeyValue(token, port) in ports do
      if session <> token then
        self.SendClient port msg

  member self.Log (logLevel: LogLevel) (message : string) : unit =
    let log = Logger.create logLevel "worker" message
    ClientMessage.Event(id, LogMsg log)
    |> self.Broadcast
