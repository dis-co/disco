[<AutoOpen>]
module Iris.Web.Core.Global

open System
open System.Collections.Generic
open Fable.Core
open Fable.Import
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core
open Iris.Core.Commands

type ISubscriber = obj -> unit
type IServiceInfo = interface end
type IWidget = interface end
type ITab = interface end

[<Literal>]
let private LOG_MAX = 100

// TODO: Deal with all messages in single pattern matching
let private subscribeToLogs(f:string->unit): IDisposable =
    ClientContext.Singleton.OnMessage.Subscribe (function
      | ClientMessage.Event(_, LogMsg log) -> f log.Message
      | _ -> ())

let private subscribeToClock(f:uint32->unit): IDisposable =
    ClientContext.Singleton.OnMessage.Subscribe (function
      | ClientMessage.Event(_, UpdateClock frames) -> f frames
      | _ -> ())

let private startContext f =
  let context = ClientContext.Singleton
  let notify = function
    | Some state ->
      match Map.tryFind context.Session state.Sessions with
      | Some session ->
        Some { session = session; state = state } |> f
      | None -> ()
    | None ->
        f None
  context.Start()
  |> Promise.iter (fun () ->
    context.OnMessage
    |> Observable.add (function
      | ClientMessage.Initialized _ ->
        // TODO: Store should be initialized, throw error if not?
        context.Store |> Option.map (fun x -> x.State) |> notify
      | ClientMessage.Event(_, ev) ->
        match ev with
        | StateMachine.UnloadProject -> notify None
        | DataSnapshot state -> notify (Some state)
        | ev ->
          match context.Store with
          // TODO: Reduce the number of notifications to widget suscriptors
          | Some store -> notify None
          | None -> () // This case should be handled in ClientContext
      | _ -> ())
  )

[<Pojo>]
type GlobalState =
  { logs: (int*string) list
    tabs: Map<int,ITab>
    widgets: Map<int,IWidget>
    pinGroups: Map<Id,PinGroup>
    cues: Map<Id,Cue>
    cueLists: Map<Id,CueList>
    cuePlayers: Map<Id,CuePlayer>
    useRightClick: bool
    serviceInfo: ServiceInfo
    clock: int
    project: IrisProject option }

// TODO: Unify this with ClientContext?

/// To prevent duplication, this is the model all other views have access to.
/// It manages the information coming from backend/shared worker.
type GlobalModel() =
  // Private fields
  let logSubscription = ref None
  let clockSubscription = ref None
  let subscribers = Dictionary<string, Dictionary<int, ISubscriber>>()
  let eventSubscribers = Dictionary<string, Dictionary<int, ISubscriber>>()
  let state =
    { logs = []
      tabs = Map.empty
      widgets = Map.empty
      pinGroups = Map.empty
      cues = Map.empty
      cueLists = Map.empty
      cuePlayers = Map.empty
      useRightClick = false
      serviceInfo =
        { webSocket = "0"
          version = "0.0.0"
          buildNumber = "0"  }
      clock = 0
      project  = None }

  // Private methods
  let newId =
    let mutable counter = 0
    fun () -> counter <- counter + 1; counter

  let updateStateAndNotify (key: string) (updater: 'T->'T) =
    let newValue = !!state?(key) |> updater
    state?(key) <- newValue
    // Notify update
    match subscribers.TryGetValue(key) with
    | true, subscribers -> for s in subscribers.Values do s(newValue)
    | false, _ -> ()

  let setStateAndNotify key (value:'T) =
    updateStateAndNotify key (fun _ -> value)

  let addLogPrivate (log: string) =
    updateStateAndNotify (nameof(state.logs)) <| fun logs ->
      let logs =
        let length = List.length logs
        if length > LOG_MAX then
          let diff = LOG_MAX / 100
          List.take (length - diff) logs
        else
          logs
      KeyValuePair(newId, log)::logs

  // Constructor
  do startContext(fun info ->
    // Init logs and clocks upon receiving first message
    if Option.isNone !logSubscription then
      logSubscription := addLogPrivate |> subscribeToLogs |> Some

    if Option.isNone !clockSubscription then
      clockSubscription :=
        nameof(state.clock)
        |> setStateAndNotify
        |> subscribeToClock
        |> Some

    if state.serviceInfo.version = "0.0.0" then
      ClientContext.Singleton.ServiceInfo
      |> setStateAndNotify (nameof(state.serviceInfo))

    match info with
    | Some i ->
      setStateAndNotify (nameof(state.project)) (Some i.state.Project)
      setStateAndNotify (nameof(state.pinGroups)) i.state.PinGroups
      setStateAndNotify (nameof(state.cues)) i.state.Cues
      setStateAndNotify (nameof(state.cueLists)) i.state.CueLists
      setStateAndNotify (nameof(state.cuePlayers)) i.state.CuePlayers
    | None ->
      setStateAndNotify (nameof(state.project)) None
      setStateAndNotify (nameof(state.pinGroups)) Map.empty
      setStateAndNotify (nameof(state.cues)) Map.empty
      setStateAndNotify (nameof(state.cueLists)) Map.empty
      setStateAndNotify (nameof(state.cuePlayers)) Map.empty
  )

  // Public methods
  member this.subscribe(keys: U2<string, string[]>, subscriber: ISubscriber) =
    let keys =
      match keys with
      | U2.Case1 key -> [|key|]
      | U2.Case2 keys -> keys
    let disposables = ResizeArray<IDisposable>()
    for key in keys do
      let id = newId()
      if subscribers.ContainsKey(key) |> not then
        subscribers.Add(key, Dictionary())
      subscribers.[key].Add(id, subscriber)
      { new IDisposable with
          member __.Dispose() = subscribers.[key].Remove(id) |> ignore }
      |> disposables.Add
    { new IDisposable with
        member __.Dispose() = for d in disposables do d.Dispose() }

  member this.subscribeToEvent(event: string, subscriber: ISubscriber) =
    let id = newId()
    if eventSubscribers.ContainsKey(event) |> not then
      eventSubscribers.Add(event, Dictionary())
    eventSubscribers.[event].Add(id, subscriber)
    printfn "Subscription to event %s" event
    { new IDisposable with
        member __.Dispose() = eventSubscribers.[event].Remove(id) |> ignore }

  member this.useRightClick(value: bool) =
    setStateAndNotify (nameof(state.useRightClick)) value

  member this.addWidget(widget: IWidget, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    updateStateAndNotify (nameof(state.widgets)) (Map.add id widget)
    id

  member this.removeWidget(id: int) =
    updateStateAndNotify (nameof(state.widgets)) (Map.remove id)

  member this.addTab(tab: ITab, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    updateStateAndNotify (nameof(state.tabs)) (Map.add id tab)
    id

  member this.removeTab(id: int) =
    updateStateAndNotify (nameof(state.tabs)) (Map.remove id)

  member this.addLog(log: string) =
    addLogPrivate log

  member this.triggerEvent(event: string, data: obj) =
    match eventSubscribers.TryGetValue(event) with
    | true, subscribers -> for s in subscribers.Values do s(data)
    | false, _ -> ()

