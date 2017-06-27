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

type ISubscriber = obj -> IDictionary<string,obj> -> unit
type ISubscriber<'T> = 'T -> IDictionary<string,obj> -> unit
type IWidget = interface end
type ITab = interface end

let [<Literal>] private LOG_MAX = 100
let [<Literal>] private LOG_DIFF = 10

// Polyfill, Fable doesn't support RemoveRange yet
[<Emit("$2.splice($0,$1)")>]
let private removeRange (index: int) (count: int) (ar: ResizeArray<'T>): unit = jsNative

// INTERFACES --------------------------------------------------

// As these interfaces are exposed to JS, we start the members
// with lower case to follow JS conventions

type IDisposableJS =
  abstract dispose: unit->unit

type IGlobalState =
  abstract logs: IEnumerable<string>
  abstract tabs: IDictionary<Guid,ITab>
  abstract widgets: IDictionary<Guid,IWidget>
  abstract clock: uint32
  abstract useRightClick: bool
  abstract serviceInfo: ServiceInfo
  abstract project: IrisProject option
  abstract pinGroups: Map<Id,PinGroup>
  abstract cues: Map<Id,Cue>
  abstract cueLists: Map<Id,CueList>
  abstract cuePlayers: Map<Id,CuePlayer>

type IGlobalModel =
  abstract state: IGlobalState
  abstract subscribe: keys: U2<string, string[]> * subscriber: ISubscriber -> IDisposableJS
  abstract subscribeToEvent: event: string * subscriber: ISubscriber<'T> -> IDisposableJS
  abstract useRightClick: value: bool -> unit
  abstract addWidget: widget: IWidget * ?id: Guid -> Guid
  abstract removeWidget: id: Guid -> unit
  abstract addTab: tab: ITab * ?id: Guid -> Guid
  abstract removeTab: id: Guid -> unit
  abstract addLog: log: string -> unit
  abstract triggerEvent: event: string * data: obj -> unit

// IMPLEMENTATIONS --------------------------------------------------

type private Disposable(f: unit->unit) =
  interface IDisposableJS with
    member __.dispose() = f()
  interface System.IDisposable with
    member __.Dispose() = f()

type private GlobalStateMutable(readState: unit->State option) =
  let projectOrEmpty (project: State -> Map<Id,'T>) =
      match readState() with
      | Some state -> project state
      | None -> Map.empty
  member val Logs = ResizeArray()
  member val Tabs = Dictionary()
  member val Widgets = Dictionary()
  member val Clock = 0ul with get, set
  member val UseRightClick = false with get, set
  member val ServiceInfo =
      { webSocket = "0"
        version = "0.0.0"
        buildNumber = "0"  } with get, set
  interface IGlobalState with
    member this.logs = upcast this.Logs
    member this.tabs = upcast this.Tabs
    member this.widgets = upcast this.Widgets
    member this.clock = this.Clock
    member this.useRightClick = this.UseRightClick
    member this.serviceInfo = this.ServiceInfo
    member this.pinGroups = projectOrEmpty (fun s -> s.PinGroups)
    member this.cues = projectOrEmpty (fun s -> s.Cues)
    member this.cueLists = projectOrEmpty (fun s -> s.CueLists)
    member this.cuePlayers = projectOrEmpty (fun s -> s.CuePlayers)
    member this.project =
      #if DESIGN
      MockData.project |> Some
      #else
      readState() |> Option.map (fun s -> s.Project)
      #endif

/// To prevent duplication, this is the model all other views have access to.
/// It manages the information coming from backend/shared worker.
type GlobalModel() as this =
  // Private fields
  let context = ClientContext.Singleton
  let stateMutable = GlobalStateMutable(fun () ->
    context.Store |> Option.map (fun x -> x.State))
  let stateImmutable: IGlobalState = upcast stateMutable
  let subscribers = Dictionary<string, Dictionary<Guid, ISubscriber>>()
  let eventSubscribers = Dictionary<string, Dictionary<Guid, ISubscriber>>()

  // Constructor
  do context.Start()
  |> Promise.iter (fun () ->
    context.OnMessage
    |> Observable.add (function
      | ClientMessage.Initialized _ ->
        this.NotifyAll()
      | ClientMessage.Event(_, ev) ->
        // match ev with
        // | LogMsg _ | UpdateClock _ -> ()
        // | ev -> printfn "GlobalModel received event %A" ev
        match ev with
        | DataSnapshot _ -> this.NotifyAll()
        // The UnloadProject event is not actually being returned to frontend
        // | StateMachine.UnloadProject -> this.NotifyAll()
        | UpdateProject _ | AddMember _ | UpdateMember _ | RemoveMember _ ->
          this.Notify(nameof(stateImmutable.project), stateImmutable.project, [])
        | AddPinGroup _
        | UpdatePinGroup _
        | RemovePinGroup _
        | AddPin _
        | UpdatePin _
        | RemovePin _
        | UpdateSlices _ ->
          this.Notify(nameof(stateImmutable.pinGroups), stateImmutable.pinGroups, [])
        | AddCue cue
        | UpdateCue cue
        | RemoveCue cue
        | CallCue cue ->
          this.Notify(nameof(stateImmutable.cues), stateImmutable.cues, [nameof cue.Id, box cue.Id])
        | AddCueList cueList
        | UpdateCueList cueList
        | RemoveCueList cueList ->
          this.Notify(nameof(stateImmutable.cueLists), stateImmutable.cueLists, [(nameof cueList.Id), box cueList.Id])
        | AddCuePlayer    _
        | UpdateCuePlayer _
        | RemoveCuePlayer _ ->
          this.Notify(nameof(stateImmutable.cuePlayers), stateImmutable.cuePlayers, [])
        | LogMsg log -> this.AddLog(log.Message)
        | UpdateClock frames ->
          stateMutable.Clock <- frames
          this.Notify(nameof(stateImmutable.clock), stateImmutable.clock, [])
        | _ -> ()
      | _ -> ())
  )

  static let mutable singleton: GlobalModel option = None

  static member Singleton =
    match singleton with
    | Some singleton -> singleton
    | None ->
      let globalModel = GlobalModel()
      singleton <- Some globalModel
      globalModel

  // Private methods
  member private this.Notify(key, newValue: obj, keyValuePairs) =
    match subscribers.TryGetValue(key) with
    | true, keySubscribers ->
      let dic = dict keyValuePairs
      for s in keySubscribers.Values do s newValue dic
    | false, _ -> ()

  member this.NotifyAll() =
    let dic = dict []
    for KeyValue(key, keySubscribers) in subscribers do
      let value = stateMutable?(key)
      keySubscribers.Values |> Seq.iteri (fun i subscriber ->
        // printfn "Inform subscriber %i: %s" i key
        subscriber value dic)

  // Public methods
  member this.State: IGlobalState = stateImmutable

  member this.Subscribe(keys: U2<string, string[]>, subscriber: ISubscriber): IDisposable =
    let keys =
      match keys with
      | U2.Case1 key -> [|key|]
      | U2.Case2 keys -> keys
    let disposables = ResizeArray<IDisposable>()
    for key in keys do
      let id = Guid.NewGuid()
      if subscribers.ContainsKey(key) |> not then
        subscribers.Add(key, Dictionary())
      subscribers.[key].Add(id, subscriber)
      new Disposable(fun () -> subscribers.[key].Remove(id) |> ignore)
      |> disposables.Add
    upcast new Disposable(fun () -> for d in disposables do d.Dispose())

  member this.SubscribeToEvent(event: string, subscriber: ISubscriber<'T>): IDisposable =
    let id = Guid.NewGuid()
    if eventSubscribers.ContainsKey(event) |> not then
      eventSubscribers.Add(event, Dictionary())
    eventSubscribers.[event].Add(id, !!subscriber)
    printfn "Subscription to event %s" event
    upcast new Disposable(fun () -> eventSubscribers.[event].Remove(id) |> ignore)

  member this.UseRightClick(value: bool) =
    stateMutable.UseRightClick <- value
    this.Notify(nameof(this.State.useRightClick), value, [])

  member this.AddWidget(widget: IWidget, ?id: Guid) =
    let id = match id with Some id -> id | None -> Guid.NewGuid()
    stateMutable.Widgets.Add(id, widget)
    this.Notify(nameof(this.State.widgets), this.State.widgets, [])
    id

  member this.RemoveWidget(id: Guid) =
    stateMutable.Widgets.Remove(id) |> ignore
    this.Notify(nameof(this.State.widgets), this.State.widgets, [])

  member this.AddTab(tab: ITab, ?id: Guid) =
    let id = match id with Some id -> id | None -> Guid.NewGuid()
    stateMutable.Tabs.Add(id, tab)
    this.Notify(nameof(this.State.tabs), this.State.tabs, [])
    id

  member this.RemoveTab(id: Guid) =
    stateMutable.Tabs.Remove(id) |> ignore
    this.Notify(nameof(this.State.tabs), this.State.tabs, [])

  member this.AddLog(log: string) =
    let length = stateMutable.Logs.Count
    if length > LOG_MAX then
      removeRange (length - LOG_DIFF) LOG_DIFF stateMutable.Logs
    stateMutable.Logs.Insert(0, log)
    this.Notify(nameof(stateImmutable.logs), stateImmutable.logs, [])

  member this.TriggerEvent(event: string, data: obj) =
    match eventSubscribers.TryGetValue(event) with
    | true, subscribers ->
      let dic = dict []
      for s in subscribers.Values do s data dic
    | false, _ -> ()

  interface IGlobalModel with
    member this.state: IGlobalState = this.State
    member this.subscribe(keys, subscriber) = this.Subscribe(keys, subscriber) :?> IDisposableJS
    member this.subscribeToEvent(event, subscriber) = this.SubscribeToEvent(event, subscriber) :?> IDisposableJS
    member this.useRightClick(value) = this.UseRightClick(value)
    member this.addWidget(widget, ?id) = this.AddWidget(widget, ?id=id)
    member this.removeWidget(id) = this.RemoveWidget(id)
    member this.addTab(tab, ?id) = this.AddTab(tab, ?id=id)
    member this.removeTab(id) = this.RemoveTab(id)
    member this.addLog(log) = this.AddLog(log)
    member this.triggerEvent(event, data) = this.TriggerEvent(event, data)
