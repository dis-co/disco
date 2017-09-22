module Iris.Web.Types

open System
open Fable.Core
open Fable.Import
open Iris.Raft
open Iris.Core
open Iris.Core.Commands

/// Keys for Browser localStorage
module StorageKeys =
  let [<Literal>] layout = "iris-layout"
  let [<Literal>] widgets = "iris-widgets"

/// Widget names
module Widgets =
  let [<Literal>] Log           = "LOG"
  let [<Literal>] GraphView     = "Graph View"
  let [<Literal>] CuePlayer     = "Cue Player"
  let [<Literal>] ProjectView   = "Project View"
  let [<Literal>] Cluster       = "Cluster"
  let [<Literal>] Clients       = "Clients"
  let [<Literal>] Sessions      = "Sessions"
  let [<Literal>] PinMapping    = "Pin Mappings"
  let [<Literal>] InspectorView = "Inspector View"
  let [<Literal>] Test          = "Test"

type IProjectInfo =
  abstract name: Name
  abstract username: UserName
  abstract password: Password

type IModal =
  abstract SetResult: obj -> unit

/// Modal dialogs
[<RequireQualifiedAccess>]
module Modal =
  type AddMember() =
    let mutable res = None
    member __.Result: string * uint16 = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

  type CreateProject() =
    let mutable res = None
    member __.Result: string = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

  type LoadProject() =
    let mutable res = None
    member __.Result: IProjectInfo = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

  type AvailableProjects(projects: Name[]) =
    let mutable res = Unchecked.defaultof<_>
    member __.Projects = projects
    member __.Result: Name option = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type Login(project: Name) =
    let mutable res = Unchecked.defaultof<_>
    member __.Project = project
    member __.Result: IProjectInfo option = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type ProjectConfig(sites: NameAndId[], info: IProjectInfo) =
    let mutable res = None
    member __.Sites = sites
    member __.Info = info
    member __.Result: NameAndId = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

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
  | OpenModal of IModal
  | CloseModal of IModal * result: Choice<obj,unit>
  | SelectElement of Selected
  | Navigate of Browse

and Selected =
  | Pin      of Pin
  | PinGroup of PinGroup
  | Client   of IrisClient
  | Member   of RaftMember
  | Cue      of Cue
  | CueList  of CueList
  | Player   of CuePlayer
  | Mapping  of PinMapping
  | Session  of Session
  | User     of User
  | Nothing

and BrowseHistory =
  { index: int
    selected: Selected
    previous: Selected list }

and Browse =
  | Previous
  | Next

/// Elmish state model
and Model =
  { widgets: Map<Guid,IWidget>
    layout: Layout[]
    modal: IModal option
    state: State option
    logs: LogEvent list
    history: BrowseHistory
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

and IUpdater =
  abstract Update: dragging:bool * index:int * value:obj -> unit

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
