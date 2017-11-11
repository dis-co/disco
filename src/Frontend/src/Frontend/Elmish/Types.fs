module Iris.Web.Types

open System
open Fable.Core
open Fable.Import
open Iris.Raft
open Iris.Core
open Iris.Core.Commands
open Iris.Web.Core

/// Keys for Browser localStorage
module StorageKeys =
  let [<Literal>] layout = "iris-layout"
  let [<Literal>] widgets = "iris-widgets"

/// Widget names
module Widgets =
  let [<Literal>] Log           = "LOG"
  let [<Literal>] GraphView     = "Graph View"
  let [<Literal>] Players       = "Cue Players"
  let [<Literal>] CueLists      = "Cue Lists"
  let [<Literal>] Cues          = "Cues"
  let [<Literal>] CuePlayer     = "Cue Player"
  let [<Literal>] ProjectView   = "Project View"
  let [<Literal>] Cluster       = "Cluster"
  let [<Literal>] Clients       = "Clients"
  let [<Literal>] Sessions      = "Sessions"
  let [<Literal>] PinMapping    = "Pin Mappings"
  let [<Literal>] Test1         = "Test 1"
  let [<Literal>] Test2         = "Test 2"
  let [<Literal>] Test3         = "Test 3"

type IProjectInfo =
  abstract name: Name
  abstract username: UserName
  abstract password: Password

/// Interface that must be implemented by all widgets
type IWidget =
  abstract Id: Guid
  abstract Name: string
  abstract InitialLayout: WidgetLayout
  abstract Render: Elmish.Dispatch<Msg> * Model -> React.ReactElement

// Modal Dialog interfac
and IModal =
  abstract SetResult: obj -> unit

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

and [<RequireQualifiedAccess>] InspectorAction =
  | Open
  | Close
  | Resize of int

and [<RequireQualifiedAccess>] TabAction =
  | AddTab
  | UpdateTab of id:Guid
  | RemoveTab of id:Guid

/// Messages that can be dispatched to Elmish
and Msg =
  | AddWidget of Guid * IWidget
  | RemoveWidget of Guid
  | UpdateTabs of TabAction
  | AddLog of LogEvent
  | UpdateLayout of WidgetLayout[]
  | UpdateUserConfig of UserConfig
  | UpdateState of State option
  | UpdateInspector of InspectorAction
  | OpenModal of IModal
  | CloseModal of IModal * result: Choice<obj,unit>
  | RemoveSelectedDragItems
  | SelectDragItems of DragItems * multiple: bool
  | SelectElement of InspectorSelection
  | Navigate of InspectorNavigate

and InspectorSelection =
  | Pin      of Name * ClientId * PinId
  | PinGroup of Name * ClientId * PinGroupId
  | Client   of Name * ClientId
  | Member   of Name * MemberId
  | Cue      of Name * CueId
  | CueList  of Name * CueListId
  | Player   of Name * PlayerId
  | Mapping  of PinMappingId
  | Session  of SessionId
  | User     of Name * UserId
  | Nothing

and InspectorHistory =
  { index: int
    selected: InspectorSelection
    previous: InspectorSelection list }

and InspectorNavigate =
  | Previous
  | Next
  | Set of int

and [<RequireQualifiedAccess>] DragItems =
  | Pins of PinId list
  | CueAtoms of (CueId * PinId) list
  /// Merge selected items if they have the same case
  /// Otherwise, it just returns the new items
  member oldItems.Append(newItems: DragItems) =
    let appendDistinct x y =
      List.append x y |> List.distinct
    match oldItems, newItems with
    | Pins x, Pins y -> appendDistinct x y |> Pins
    | CueAtoms x, CueAtoms y -> appendDistinct x y |> CueAtoms
    | _ -> newItems

/// Elmish state model
and Model =
  { widgets: Map<Guid,IWidget>
    layout: Layout
    modal: IModal option
    state: State option
    logs: LogEvent list
    history: InspectorHistory
    selectedDragItems: DragItems
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
and [<Pojo>] WidgetLayout =
  { i: Guid; ``static``: bool
    x: int; y: int
    w: int; h: int
    minW: int; maxW: int
    minH: int; maxH: int }

and [<Pojo>] InspectorLayout =
  { IsOpen: bool
    Size: int }

and [<Pojo>] Tab =
  { Id: Guid
    Name: string
    Removable: bool
    WidgetRefs: WidgetRef[]
    WidgetLayouts: WidgetLayout[] }

and [<Pojo>] Layout =
  { Tabs: Tab[]
    Selected: Guid
    Inspector: InspectorLayout }

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

///  ___                           _             _                            _
/// |_ _|_ __  ___ _ __   ___  ___| |_ ___  _ __| |    __ _ _   _  ___  _   _| |_
///  | || '_ \/ __| '_ \ / _ \/ __| __/ _ \| '__| |   / _` | | | |/ _ \| | | | __|
///  | || | | \__ \ |_) |  __/ (__| || (_) | |  | |__| (_| | |_| | (_) | |_| | |_
/// |___|_| |_|___/ .__/ \___|\___|\__\___/|_|  |_____\__,_|\__, |\___/ \__,_|\__|
///               |_|                                       |___/

module InspectorLayout =

  let defaultLayout =
    { IsOpen = false
      Size = 350 }                      /// in pixels

  let isOpen { IsOpen = isOpen } = isOpen
  let size { Size = size } = size

  let toggle inspector = { inspector with IsOpen = not inspector.IsOpen }
  let setSize size inspector = { inspector with Size = size }
  let setOpen isOpen inspector = { inspector with IsOpen = isOpen }

///  _____     _
/// |_   _|_ _| |__
///   | |/ _` | '_ \
///   | | (_| | |_) |
///   |_|\__,_|_.__/

module Tab =
  let private workspaceId = Guid.Parse "5b4f88b5-7a84-474d-8c5c-e689e7e48091"

  let workspace =
    { Id = workspaceId
      Name = "Workspace"
      Removable = false
      WidgetRefs = Array.empty
      WidgetLayouts = Array.empty }

  let id ({ Id = id }:Tab) = id
  let name ({ Name = name }:Tab) = name
  let setName name (tab:Tab) = { tab with Name = name }
  let removable ({ Removable = removable }:Tab) = removable
  let widgetRefs ({ WidgetRefs = widgets }:Tab) = widgets
  let widgetLayouts ({ WidgetLayouts = layouts }:Tab) = layouts
  let setWidgetRefs widgets (tab:Tab) = { tab with WidgetRefs = widgets }
  let setWidgetLayouts layouts (tab:Tab) = { tab with WidgetLayouts = layouts }

  let createWidgets (tab:Tab) =
    let factory = getWidgetFactory()
    Array.fold
      (fun map (id, name) ->
        let widget = factory.CreateWidget(Some id, name)
        Map.add id widget map)
      Map.empty
      tab.WidgetRefs

  let addWidget (widget:IWidget) (tab:Tab) =
    let layouts =
      tab
      |> widgetLayouts
      |> flip Array.append [| widget.InitialLayout |]
    let widgetRefs =
      tab
      |> widgetRefs
      |> flip Array.append [| widget.Id, widget.Name |]
    { tab with WidgetRefs = widgetRefs; WidgetLayouts = layouts }

  let removeWidget id (tab:Tab) =
    { tab with
        WidgetLayouts = Array.filter (fun { i = widget } -> widget <> id) tab.WidgetLayouts
        WidgetRefs = Array.filter (fun (wid,_) -> wid <> id) tab.WidgetRefs }

///  _                            _
/// | |    __ _ _   _  ___  _   _| |_
/// | |   / _` | | | |/ _ \| | | | __|
/// | |__| (_| | |_| | (_) | |_| | |_
/// |_____\__,_|\__, |\___/ \__,_|\__|
///             |___/

module Layout =

  let defaultLayout =
    { Tabs = [| Tab.workspace |]
      Selected = Tab.workspace.Id
      Inspector = InspectorLayout.defaultLayout }

  let tabs { Tabs = tabs } = tabs
  let setTabs tabs layout = { layout with Tabs = tabs }

  let tab id { Tabs = tabs } =
    match Array.tryFind (fun tab -> tab.Id = id) tabs with
    | Some tab -> tab
    | None -> Tab.workspace

  let currentTab layout =
    tab layout.Selected layout

  let updateTab tab layout =
    { layout with
        Tabs = Array.map (fun other -> if other.Id = tab.Id then tab else other) layout.Tabs }

  let widgetLayouts = currentTab >> Tab.widgetLayouts
  let widgetRefs = currentTab >> Tab.widgetRefs

  let inspector { Inspector = inspector } = inspector
  let setInspector inspector (layout:Layout) =
    { layout with Inspector = inspector }

  let setInspectorSize width (layout:Layout) =
    layout |> inspector |> InspectorLayout.setSize width |> flip setInspector layout

  let setInspectorOpen isOpen (layout:Layout) =
    layout |> inspector |> InspectorLayout.setOpen isOpen |> flip setInspector layout

  let addWidget widget (layout:Layout) =
    layout
    |> currentTab
    |> Tab.addWidget widget
    |> flip updateTab layout

  let removeWidget id (layout:Layout) =
    layout
    |> currentTab
    |> Tab.removeWidget id
    |> flip updateTab layout

  let setWidgets (widgets:WidgetLayout[]) (layout:Layout) =
    layout
    |> currentTab
    |> Tab.setWidgetLayouts widgets
    |> flip updateTab layout

  let save (layout:Layout) =
    Storage.save StorageKeys.layout layout

  let createWidgets (layout:Layout) =
    layout
    |> currentTab
    |> Tab.createWidgets

  let load () =
    StorageKeys.layout
    |> Storage.load<Layout>
    |> Option.defaultValue defaultLayout
