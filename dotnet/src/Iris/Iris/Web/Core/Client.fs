[<AutoOpen>]
module Iris.Web.Core.Client

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Iris.Core

let inline getHostname(): string = Browser.window.location.hostname

let inline getHostPort(): int = int Browser.window.location.port

let makeObservable (ctrls: Dictionary<Guid, 'T IObserver>) =
  { new IObservable<'T> with
    member __.Subscribe(obs) =
      let guid = Guid.NewGuid()
      ctrls.Add(guid, obs)
      { new IDisposable with
          member __.Dispose() = ctrls.Remove(guid) |> ignore } }

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
type [<Pojo; NoComparison>] StateInfo =
  { context: ClientContext; session: Session; state: State }

and ClientContext private (worker: SharedWorker<string>) =
  let mutable session : Id option = None
  let ctrls = Dictionary<Guid, IObserver<StateInfo>>()
  let logCtrls = Dictionary<Guid, IObserver<ClientLog>>()

  static member Start() =
    let host = getHostname ()
    let port = getHostPort ()
    let address = sprintf "ws://%s:%d" host (port + Constants.SOCKET_SERVER_PORT_DIFF)
    let me = new SharedWorker<string>(Constants.WEB_WORKER_SCRIPT)
    let client = new ClientContext(me)
    me.OnError <- fun e -> printfn "%A" e.Message
    me.Port.OnMessage <- client.MsgHandler
    me.Port.PostMessage (ClientMessage.Connect address |> toJson)
    client

  member self.Session =
    match session with
    | Some token -> token
    | None -> failwith "Client not initialized"

  member self.Trigger(msg: ClientMessage<StateMachine>) =
    worker.Port.PostMessage(toJson msg)

  member self.Post(ev: StateMachine) =
    printfn "Will send message %A" ev
    ClientMessage.Event(self.Session, ev)
    |> toJson
    |> worker.Port.PostMessage

  member self.MsgHandler (msg : MessageEvent<string>) : unit =
    match ofJson<ClientMessage<State>> msg.Data with
    // initialize this clients session variable
    | ClientMessage.Initialized(Id id as token) ->
      session <- Some(token)
      printfn "Initialized with Session: %s" id

    // initialize this clients session variable
    | ClientMessage.Closed(token) ->
      printfn "A client closed its session: %A" token

    | ClientMessage.Stopped ->
      printfn "Worker stopped. TODO: Needs to be restarted"
      // TODO: Restart worker
      //self.Start()

    // Re-render the current view tree with a new state
    | ClientMessage.Render(state) ->
      match Map.tryFind self.Session state.Sessions with
      | Some session ->
        for ctrl in ctrls.Values do
          ctrl.OnNext {context = self; session = session; state = state}
      | None ->
        printfn "Cannot find current session"

    | ClientMessage.Connected ->
      Session.Empty self.Session |> AddSession |> self.Post
      printfn "CONNECTED!"

    | ClientMessage.Disconnected ->
      printfn "DISCONNECTED!"

    | ClientMessage.ClientLog log ->
      //printfn "%s" log
      for logCtrl in logCtrls.Values do
        logCtrl.OnNext(log)

    // initialize this clients session variable
    | ClientMessage.Error(reason) ->
      printfn "SharedWorker Error: %A" reason

    | _ -> printfn "Unknown Event: %A" msg.Data

  member val OnRender = makeObservable ctrls

  member val OnClientLog = makeObservable logCtrls

  interface IDisposable with
    member self.Dispose() =
      ClientMessage.Close(self.Session)
      |> toJson
      |> worker.Port.PostMessage

