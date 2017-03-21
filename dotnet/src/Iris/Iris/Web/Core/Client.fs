[<AutoOpen>]
module Iris.Web.Core.Client

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core

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
  { session: Session; state: State }

and ClientContext private () =
  let mutable session : Id option = None
  let mutable worker : SharedWorker<string> option = None
  let ctrls = Dictionary<Guid, IObserver<ClientMessage<State>>>()

  static let mutable singleton: ClientContext option = None

  static member Singleton =
    match singleton with
    | Some singleton -> singleton
    | None ->
      let client = new ClientContext()
      singleton <- Some client
      client

  member self.Start() = promise {
    let me = new SharedWorker<string>(Constants.WEB_WORKER_SCRIPT)
    me.OnError <- fun e -> printfn "%A" e.Message
    me.Port.OnMessage <- self.MsgHandler
    worker <- Some me
    do! self.ConnectWithWebSocket()
  }

  member self.ConnectWithWebSocket() =
    (Commands.GetWebSocketAddress, [])
    ||> Fetch.postRecord Constants.WEP_API_COMMAND
    |> Promise.bind (fun res -> res.text())
    |> Promise.map (fun address ->
        ClientMessage.Connect address
        |> toJson |> self.Worker.Port.PostMessage)

  member self.Session =
    match session with
    | Some session -> session
    | None -> failwith "Client not initialized"

  member self.Worker: SharedWorker<string> =
    match worker with
    | Some worker -> worker
    | None -> failwith "Client not initialized"

  member self.Trigger(msg: ClientMessage<StateMachine>) =
    self.Worker.Port.PostMessage(toJson msg)

  member self.Post(ev: StateMachine) =
    printfn "Will send message %A" ev
    ClientMessage.Event(self.Session, ev)
    |> toJson
    |> self.Worker.Port.PostMessage

  member self.MsgHandler (msg : MessageEvent<string>) : unit =
    let data = ofJson<ClientMessage<State>> msg.Data
    for ctrl in ctrls.Values do
      ctrl.OnNext data

    match data with
    // initialize this clients session variable
    | ClientMessage.Initialized(Id id as token) ->
      session <- Some(token)
      printfn "Initialized with Session: %s" id

    // close this clients session variable
    | ClientMessage.Closed(token) ->
      printfn "A client closed its session: %A" token

    | ClientMessage.Stopped ->
      printfn "Worker stopped. TODO: Needs to be restarted"
      // TODO: Restart worker
      //self.Start()

    // Re-render the current view tree with a new state
    | ClientMessage.Render _ ->
      // Do nothing, delegate responsibility to controllers
      ()

    | ClientMessage.Connected ->
      Session.Empty self.Session |> AddSession |> self.Post
      printfn "CONNECTED!"

    | ClientMessage.Disconnected ->
      printfn "DISCONNECTED!"

    | ClientMessage.ClientLog _log ->
      // Logs are polluting the browser console, disable printing temporally
//      printfn "%s" log
      ()

    // initialize this clients session variable
    | ClientMessage.Error(reason) ->
      printfn "SharedWorker Error: %A" reason

    | _ -> printfn "Unknown Event: %A" msg.Data

  member __.OnMessage =
    { new IObservable<_> with
      member __.Subscribe(obs) =
        let guid = Guid.NewGuid()
        ctrls.Add(guid, obs)
        { new IDisposable with
            member __.Dispose() = ctrls.Remove(guid) |> ignore } }

  interface IDisposable with
    member self.Dispose() =
      ClientMessage.Close(self.Session)
      |> toJson
      |> self.Worker.Port.PostMessage

