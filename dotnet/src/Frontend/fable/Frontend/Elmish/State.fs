module rec Iris.Web.State

open System
open Iris.Core
open Fable.Core
open Fable.Import
open Elmish

type Msg =
  | AddWidget of Guid * IWidget
  | RemoveWidget of Guid
  | AddLog of LogEvent
  | UpdateLogConfig of LogConfig

type Direction =
  | Ascending
  | Descending
  member this.Reverse =
    match this with
    | Ascending -> Descending
    | Descending -> Ascending

type Sorting =
  { column: string
    direction: Direction
  }

type LogConfig =
  { filter: string option
    logLevel: LogLevel option
    sorting: Sorting option
    columns: Map<string, bool>
    viewLogs: LogEvent array
  }
  static member Create(logs: LogEvent list) =
    { filter = None
      logLevel = None
      sorting = None
      columns =
        Map["LogLevel", true
            "Time", true
            "Tag", true
            "Tier", true]
      viewLogs = Array.ofList logs }

type IWidget =
  abstract Render: Dispatch<Msg> * Model -> React.ReactElement

type Model =
  { widgets: Map<Guid,IWidget>
    logs: LogEvent list
    logConfig: LogConfig
  }

let init() =
  let logs = List.init 50 (fun _ -> Core.MockData.genLog())
  let initModel =
    { widgets = Map.empty
      logs = logs
      logConfig = LogConfig.Create(logs)
    }
  initModel, []

let update msg model =
  let newModel =
    match msg with
    | AddWidget(id, widget) ->
      { model with widgets = Map.add id widget model.widgets }
    | RemoveWidget id ->
      { model with widgets = Map.remove id model.widgets }
    | AddLog log ->
      { model with logs = log::model.logs }
    | UpdateLogConfig cfg ->
      { model with logConfig = cfg }
  newModel, []
