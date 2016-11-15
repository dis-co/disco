[<AutoOpen>]
module Iris.Web.Core.Client

open Fable.Core
open Fable.Import
open Fable.Import.JS
open Fable.Import.Browser
open Fable.Core.JsInterop
open Iris.Core

let inline getHostname(): string = window.location.hostname

let inline getHostPort(): int = int window.location.port

//  ____  _                        ___        __         _
// / ___|| |__   __ _ _ __ ___  __| \ \      / /__  _ __| | _____ _ __
// \___ \| '_ \ / _` | '__/ _ \/ _` |\ \ /\ / / _ \| '__| |/ / _ \ '__|
//  ___) | | | | (_| | | |  __/ (_| | \ V  V / (_) | |  |   <  __/ |
// |____/|_| |_|\__,_|_|  \___|\__,_|  \_/\_/ \___/|_|  |_|\_\___|_|

type ErrorMsg() =
  [<Emit("$0.message")>]
  member __.Message
    with get () : string = failwith "ONLY JS"

[<Global>]
type SharedWorker<'data>(url: string) =
  [<Emit("$0.onerror = $1")>]
  member __.OnError
    with set (_: ErrorMsg -> unit) = failwith "ONLY JS"

  [<Emit("$0.port")>]
  member __.Port
    with get () : MessagePort<'data> = failwith "ONLY JS"

//   ____ _ _            _
//  / ___| (_) ___ _ __ | |_
// | |   | | |/ _ \ '_ \| __|
// | |___| | |  __/ | | | |_
//  \____|_|_|\___|_| |_|\__|

type ClientContext() =
  let mutable session : Id option = None
  let mutable worker  : SharedWorker<string> option = None
  let mutable ctrl    : (ClientContext -> State -> unit) option = None

  member self.Session
    with get () = session

  member self.Subscribe(c) =
    ctrl <- Some(c)

  member self.Start() =
    let host = getHostname ()
    let port = getHostPort ()
    let address = sprintf "ws://%s:%d" host (port + Constants.SOCKET_SERVER_PORT_DIFF)
    let me = new SharedWorker<string>(Constants.WEB_WORKER_SCRIPT)
    me.OnError <- fun e -> printfn "%A" e.Message
    me.Port.OnMessage <- self.MsgHandler
    worker <- Some me
    me.Port.PostMessage (ClientMessage.Connect address |> toJson)

  member self.Trigger(msg: ClientMessage<State>) =
    match worker with
    | Some me -> msg |> toJson |> me.Port.PostMessage
    | _       -> printfn "oops no workr??"

  member self.Close() =
    match session, worker with
    | Some token, Some me ->
      ClientMessage.Close(token)
      |> toJson
      |> me.Port.PostMessage
    | _ -> printfn "could not close it??"

  member self.MsgHandler (msg : MessageEvent<string>) : unit =
    match ofJson<ClientMessage<State>> msg.Data with
    // initialize this clients session variable
    | ClientMessage.Initialized(token) ->
      session <- Some(token)
      printfn "Initialized with Session: %A" token

    // initialize this clients session variable
    | ClientMessage.Closed(token) ->
      printfn "A client closed its session: %A" token

    | ClientMessage.Stopped ->
      printfn "Worker stopped, restarting..."
      self.Start()

    // Re-render the current view tree with a new state
    | ClientMessage.Render(state) ->
      match ctrl with
      | Some(controller) -> controller self state
      | _                -> printfn "no controller defined"


    | ClientMessage.Connected ->
      printfn "CONNECTED!"

    | ClientMessage.Disconnected ->
      printfn "DISCONNECTED!"

    | ClientMessage.ClientLog log ->
      printfn "%s" log

    // initialize this clients session variable
    | ClientMessage.Error(reason) ->
      printfn "SharedWorker Error: %A" reason

    | _ -> printfn "Unknown Event: %A" msg.Data
