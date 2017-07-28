module rec Iris.Web.State

open System
open System.Text.RegularExpressions
open Iris.Core
open Fable.Core
open Fable.Import
open Elmish

type Msg =
  | AddWidget of Guid * IWidget
  | RemoveWidget of Guid
  // | AddTab
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

type LogConfig =
  { filter: string option
    logLevel: LogLevel option
    setLogLevel: LogLevel
    sorting: Sorting option
    columns: Map<string, bool>
    viewLogs: LogEvent array
  }
  static member Create(logs: LogEvent list) =
    { filter = None
      logLevel = None
      // TODO: This should be read from backend
      setLogLevel = LogLevel.Debug
      sorting = None
      columns =
        Map["LogLevel", true
            "Time", true
            "Tag", true
            "Tier", true]
      viewLogs = Array.ofList logs }

type [<Pojo>] Layout =
  { i: Guid; ``static``: bool
    x: int; y: int
    w: int; h: int
    minW: int; maxW: int
    minH: int; maxH: int }

type IWidget =
  abstract InitialLayout: Layout
  abstract Render: Guid * Dispatch<Msg> * Model -> React.ReactElement

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
    // | AddTab -> // Add tab and remove widget
    // | RemoveTab -> // Optional, add widget
    | AddLog log ->
      { model with logs = log::model.logs }
    | UpdateLogConfig cfg ->
      let cfg = updateViewLogs model cfg
      { model with logConfig = cfg }
  newModel, []
