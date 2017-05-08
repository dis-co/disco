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

// TODO: Unify this with ClientContext?

/// To prevent duplication, this is the model all other views have access to.
/// It manages the information coming from backend/shared worker.
type GlobalModel() =

  // Private fields
  let state = Dictionary<string, obj>()
  let subscribers = Dictionary<string, Dictionary<int, ISubscriber>>()
  let eventSubscribers = Dictionary<string, Dictionary<int, ISubscriber>>()

  // Private methods
  let newId =
    let mutable counter = 0
    fun () -> counter <- counter + 1; counter
  let updateState(key: string, defaultValue: Lazy<'T>, updater: 'T->'T) =
    let newValue =
      match state.TryGetValue(key) with
      | true, value ->
        let newValue = value :?> 'T |> updater
        state.[key] <- newValue
        newValue
      | false, _ ->
        let newValue = updater defaultValue.Value
        state.Add(key, newValue)
        newValue
    // Notify update
    match subscribers.TryGetValue(key) with
    | true, subscribers -> for s in subscribers.Values do s(newValue)
    | false, _ -> ()

  // Public methods
  member this.Subscribe(keys: U2<string, string[]>, subscriber: ISubscriber) =
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

  member this.SubscribeToEvent(event: string, subscriber: ISubscriber) =
    let id = newId()
    if eventSubscribers.ContainsKey(event) |> not then
      eventSubscribers.Add(event, Dictionary())
    eventSubscribers.[event].Add(id, subscriber)
    printfn "Subscription to event %s" event
    { new IDisposable with
        member __.Dispose() = eventSubscribers.[event].Remove(id) |> ignore }

  member this.UseRightClick(value: bool) =
    updateState("useRightClick", lazy false, fun _ -> value)

  member this.AddWidget(widget: IWidget, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    updateState("widgets", lazy Map.empty, Map.add id widget)
    id

  member this.RemoveWidget(id: int) =
    updateState("widgets", lazy Map.empty, Map.remove id)

  member this.AddTab(tab: ITab, ?id: int) =
    let id = match id with Some id -> id | None -> newId()
    updateState("tabs", lazy Map.empty, Map.add id tab)
    id

  member this.RemoveTab(id: int) =
    updateState("tabs", lazy Map.empty, Map.remove id)

  member this.AddLog(log: string) =
    updateState("logs", lazy [], fun logs ->
      let logs =
        let length = List.length logs
        if length > LOG_MAX then
          let diff = LOG_MAX / 100
          List.take (length - diff) logs
        else
          logs
      (newId(), log)::logs)

  member this.TriggerEvent(event: string, data: obj) =
    match eventSubscribers.TryGetValue(event) with
    | true, subscribers -> for s in subscribers.Values do s(data)
    | false, _ -> ()

  // JS API
  member this.subscribe = this.Subscribe
  member this.subscribeToEvent = this.SubscribeToEvent
  member this.useRightClick = this.UseRightClick
  member this.addWidget = this.AddWidget
  member this.removeWidget = this.RemoveWidget
  member this.addTab = this.AddTab
  member this.removeTab = this.RemoveTab
  member this.addLog = this.AddLog
  member this.triggerEvent = this.TriggerEvent
