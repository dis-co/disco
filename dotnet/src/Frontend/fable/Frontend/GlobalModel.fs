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
type IWidget = interface end
type ITab = interface end

[<Literal>]
let private LOG_MAX = 100

type IGlobalState =
  abstract logs: IEnumerable<string>
  abstract tabs: IDictionary<int,ITab>
  abstract widgets: IDictionary<int,IWidget>
  abstract clock: int
  abstract useRightClick: bool
  abstract serviceInfo: ServiceInfo
  abstract project: IrisProject option
  abstract pinGroups: Map<Id,PinGroup>
  abstract cues: Map<Id,Cue>
  abstract cueLists: Map<Id,CueList>
  abstract cuePlayers: Map<Id,CuePlayer>


type GlobalState(readState: unit->State option) =
  let projectOrEmpty (project: State -> Map<Id,'T>) =
      match readState() with
      | Some state -> project state
      | None -> Map.empty
  member val logsM = ResizeArray()
  member val tabsM = Dictionary()
  member val widgetsM = Dictionary()
  member val clockM = 0 with get, set
  member val useRightClickM = false with get, set
  member val serviceInfoM =
      { webSocket = "0"
        version = "0.0.0"
        buildNumber = "0"  } with get, set
  interface IGlobalState with

    member this.logs = upcast this.logsM
    member this.tabs = upcast this.tabsM
    member this.widgets = upcast this.widgetsM
    member this.clock = this.clockM
    member this.useRightClick = this.useRightClickM
    member this.serviceInfo = this.serviceInfoM
    member this.project = readState() |> Option.map (fun s -> s.Project)
    member this.pinGroups = projectOrEmpty (fun s -> s.PinGroups)
    member this.cues = projectOrEmpty (fun s -> s.Cues)
    member this.cueLists = projectOrEmpty (fun s -> s.CueLists)
    member this.cuePlayers = projectOrEmpty (fun s -> s.CuePlayers)


/// To prevent duplication, this is the model all other views have access to.
/// It manages the information coming from backend/shared worker.
type GlobalModel() =
  // Private fields
  let context = ClientContext.Singleton
  let stateM: GlobalState = GlobalState(fun () ->
    context.Store |> Option.map (fun x -> x.State))
  let stateI: IGlobalState = upcast stateM
  let subscribers = Dictionary<string, Dictionary<int, ISubscriber>>()
  let eventSubscribers = Dictionary<string, Dictionary<int, ISubscriber>>()

  // Private methods
  let newId =
    let mutable counter = 0
    fun () -> counter <- counter + 1; counter

  let notify key (newValue: obj) =
    match subscribers.TryGetValue(key) with
    | true, keySubscribers -> for s in keySubscribers.Values do s(newValue)
    | false, _ -> ()

  let notifyAll () =
    for KeyValue(key, keySubscribers) in subscribers do
      let value = stateM?(key)
      for subscriber in keySubscribers.Values do
        subscriber(value)

  let addLogPrivate (log: string) =
    let length = stateM.logsM.Count
    if length > LOG_MAX then
      let diff = LOG_MAX / 10
      stateM.logsM.RemoveRange(length - diff, diff)
    stateM.logsM.Insert(0, log)
    notify (nameof(stateI.logs)) stateI.logs

  // Constructor
  do context.Start()
  |> Promise.iter (fun () ->
    context.OnMessage
    |> Observable.add (function
      | ClientMessage.Initialized _ ->
        notifyAll()
      | ClientMessage.Event(_, ev) ->
        match ev with
        | DataSnapshot _ -> notifyAll()
        | StateMachine.UnloadProject -> notifyAll()
        | UpdateProject _ ->
          notify (nameof(stateI.project)) stateI.project
        | AddPinGroup _
        | UpdatePinGroup _
        | RemovePinGroup _
        | AddPin _
        | UpdatePin _
        | RemovePin _
        | UpdateSlices _ ->
          notify (nameof(stateI.pinGroups)) stateI.pinGroups
        | AddCue _
        | UpdateCue _
        | RemoveCue _
        | CallCue _ ->
          notify (nameof(stateI.cues)) stateI.cues
        | AddCueList _
        | UpdateCueList _
        | RemoveCueList _ ->
          notify (nameof(stateI.cueLists)) stateI.cueLists
        | AddCuePlayer    _
        | UpdateCuePlayer _
        | RemoveCuePlayer _ ->
          notify (nameof(stateI.cuePlayers)) stateI.cuePlayers
        // Add members to global state for cluster widget
        // | AddMember _
        // | UpdateMember _
        // | RemoveMember _
        | _ -> ()
      | _ -> ())
  )

  // Public methods
  member this.state: IGlobalState = stateI

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
    stateM.useRightClickM <- value
    notify (nameof(this.state.useRightClick)) value

  member this.addWidget(widget: IWidget, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    stateM.widgetsM.Add(id, widget)
    notify (nameof(this.state.widgets)) this.state.widgets
    id

  member this.removeWidget(id: int) =
    stateM.widgetsM.Remove(id) |> ignore
    notify (nameof(this.state.widgets)) this.state.widgets

  member this.addTab(tab: ITab, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    stateM.tabsM.Add(id, tab)
    notify (nameof(this.state.tabs)) this.state.tabs
    id

  member this.removeTab(id: int) =
    stateM.tabsM.Remove(id) |> ignore
    notify (nameof(this.state.tabs)) this.state.tabs

  member this.addLog(log: string) =
    addLogPrivate log

  member this.triggerEvent(event: string, data: obj) =
    match eventSubscribers.TryGetValue(event) with
    | true, subscribers -> for s in subscribers.Values do s(data)
    | false, _ -> ()
