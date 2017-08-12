module Iris.Web.Types

open System
open Fable.Core
open Fable.Import
open Iris.Core

module StorageKeys =
  let [<Literal>] layout = "iris-layout"
  let [<Literal>] widgets = "iris-widgets"

module Widgets =
    let [<Literal>] Log = "LOG"
    let [<Literal>] GraphView = "Graph View"
    let [<Literal>] CuePlayer = "Cue Player"
    let [<Literal>] ProjectView = "Project View"

type IWidget =
  abstract Id: Guid
  abstract Name: string
  abstract InitialLayout: Layout
  abstract Render: Elmish.Dispatch<Msg> * Model -> React.ReactElement

and WidgetRef = Guid * string

and Direction =
  | Ascending
  | Descending
  member this.Reverse =
    match this with
    | Ascending -> Descending
    | Descending -> Ascending

and Sorting =
  { column: string
    direction: Direction
  }

and Msg =
  | AddWidget of Guid * IWidget
  | RemoveWidget of Guid
  // | AddTab | RemoveTab
  | AddLog of LogEvent
  | AddCueUI of cueList:CueList * cueGroupIndex:int * cueIndex:int
  | UpdateLayout of Layout[]
  | UpdateUserConfig of UserConfig
  | UpdateState of State option

and Model =
  { widgets: Map<Guid,IWidget>
    layout: Layout[]
    state: State option
    logs: LogEvent list
    userConfig: UserConfig
  }

and UserConfig =
  { logTextFilter: string option
    logLevelFilter: LogLevel option
    setLogLevel: LogLevel
    logSorting: Sorting option
    logColumns: Map<string, bool>
    useRightClick: bool
  }
  static member Create() =
    { logTextFilter = None
      logLevelFilter = None
      // TODO: This should be read from backend
      setLogLevel = LogLevel.Debug
      logSorting = None
      logColumns =
        Map["LogLevel", true
            "Time", true
            "Tag", true
            "Tier", true]
      useRightClick = false }

and [<Pojo>] Layout =
  { i: Guid; ``static``: bool
    x: int; y: int
    w: int; h: int
    minW: int; maxW: int
    minH: int; maxH: int }

and IFactory =
  abstract CreateWidget: id: Guid option * name: string -> IWidget

let mutable private singletonFactory = None

let getFactory() =
  match singletonFactory with
  | Some x -> x
  | None -> failwith "Factory hasn't been initialized yet"

let initFactory(factory: IFactory) =
  singletonFactory <- Some factory
