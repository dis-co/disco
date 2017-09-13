module Iris.Web.Types

open System
open Fable.Core
open Fable.Import
open Iris.Core
open Iris.Core.Commands

/// Keys for Browser localStorage
module StorageKeys =
  let [<Literal>] layout = "iris-layout"
  let [<Literal>] widgets = "iris-widgets"

/// Widget names
module Widgets =
  let [<Literal>] Log = "LOG"
  let [<Literal>] GraphView = "Graph View"
  let [<Literal>] CuePlayer = "Cue Player"
  let [<Literal>] ProjectView = "Project View"
  let [<Literal>] Cluster = "Cluster"
  let [<Literal>] Test = "Test"

/// Modal dialogs
[<RequireQualifiedAccess>]
type Modal =
  | AddMember
  | CreateProject
  | LoadProject
  | NoProject     of projects:Name[]
  | ProjectConfig of sites:NameAndId[]

type ModalView =
  { modal: Modal; view: React.ReactElement }

type IProjectInfo =
  abstract name: Name
  abstract username: UserName
  abstract password: Password

/// Interface that must be implemented by all widgets
type IWidget =
  abstract Id: Guid
  abstract Name: string
  abstract InitialLayout: Layout
  abstract Render: Elmish.Dispatch<Msg> * Model -> React.ReactElement

/// Widget data that will be stored in Browser localStorage
/// (layout is saved separately)
and WidgetRef = Guid * string

/// Direction of column sorting
and Direction =
  | Ascending
  | Descending
  member this.Reverse =
    match this with
    | Ascending -> Descending
    | Descending -> Ascending

/// Column sorting (e.g. in Log wdiget)
and Sorting =
  { column: string
    direction: Direction
  }

/// Messages that can be dispatched to Elmish
and Msg =
  | AddWidget of Guid * IWidget
  | RemoveWidget of Guid
  // | AddTab | RemoveTab
  | AddLog of LogEvent
  | AddCueUI of cueList:CueList * cueGroupIndex:int * cueIndex:int
  | UpdateLayout of Layout[]
  | UpdateUserConfig of UserConfig
  | UpdateState of State option
  | UpdateModal of ModalView option

/// Elmish state model
and Model =
  { widgets: Map<Guid,IWidget>
    layout: Layout[]
    modal: ModalView option
    state: State option
    logs: LogEvent list
    userConfig: UserConfig
  }

/// User frontend configuration
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

/// Widget layout as understood by react-grid-layout
and [<Pojo>] Layout =
  { i: Guid; ``static``: bool
    x: int; y: int
    w: int; h: int
    minW: int; maxW: int
    minH: int; maxH: int }

and IWidgetFactory =
  abstract CreateWidget: id: Guid option * name: string -> IWidget

let mutable private singletonWidgetFactory = None

let getWidgetFactory() =
  match singletonWidgetFactory with
  | Some x -> x
  | None -> failwith "Factory hasn't been initialized yet"

/// This function should only be called by App.fs
/// at the start of the program
let initWidgetFactory(factory: IWidgetFactory) =
  singletonWidgetFactory <- Some factory
