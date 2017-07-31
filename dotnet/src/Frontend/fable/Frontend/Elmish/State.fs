module rec Iris.Web.State

open System
open System.Text.RegularExpressions
open Iris.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Elmish
open Types

[<PassGenerics>]
let getFromLocalStorage<'T> (key: string) =
  let g = Fable.Import.Browser.window
  match g.localStorage.getItem(key) with
  | null -> Unchecked.defaultof<'T>
  | value -> ofJson<'T> !!value

let saveToLocalStorage (key: string) (value: obj) =
  let g = Fable.Import.Browser.window
  g.localStorage.setItem(key, toJson value)

module Widgets =
    let [<Literal>] Log = "LOG"

let updateViewLogs (model: Model) (cfg: LogConfig) =
  let readLog (col: string) (log: LogEvent) =
    match col with
    | "LogLevel" -> string log.LogLevel
    | "Time" -> string log.Time
    | "Tag" -> log.Tag
    | "Tier" -> string log.Tier
    | "Message" -> log.Message
    | col -> failwithf "Unrecognized log column: %s" col
  let viewLogs = model.logs |> List.toArray
  let viewLogs =
    match cfg.filter with
    | Some filter ->
      try
        let reg = Regex(filter, RegexOptions.IgnoreCase);
        viewLogs |> Array.filter (fun log -> reg.IsMatch(log.Message))
      with _ -> viewLogs  // Do nothing if the RegExp is not well formed
    | None -> viewLogs
  let viewLogs =
    match cfg.logLevel with
    | Some lv -> viewLogs |> Array.filter (fun log -> log.LogLevel = lv)
    | None -> viewLogs
  let viewLogs =
    match cfg.sorting with
    | Some sort ->
      viewLogs |> Array.sortWith (fun log1 log2 ->
        let col1 = readLog sort.column log1
        let col2 = readLog sort.column log2
        let res = compare col1 col2
        match sort.direction with
        | Direction.Ascending -> res
        | Direction.Descending -> res * -1
      )
    | None -> viewLogs
  { cfg with viewLogs = viewLogs}

let init() =
  let logs = List.init 50 (fun _ -> Core.MockData.genLog())
  let initModel =
    { widgets = Map.empty
      logs = logs
      logConfig = LogConfig.Create(logs)
      layout = obj()
      //   widgets
      //   |> Seq.map (fun (KeyValue(_,widget)) -> widget.InitialLayout)
      //   |> Seq.toArray
    }
  initModel, []

let update msg model =
  let newModel =
    match msg with
    | AddWidget(id, widget) ->
      { model with widgets = Map.add id widget model.widgets }
    | RemoveWidget id ->
      { model with widgets = Map.remove id model.widgets }
    // | AddTab -> // Add tab and remove widget
    // | RemoveTab -> // Optional, add widget
    | AddLog log ->
      { model with logs = log::model.logs }
    | UpdateLogConfig cfg ->
      let cfg = updateViewLogs model cfg
      { model with logConfig = cfg }
  newModel, []
