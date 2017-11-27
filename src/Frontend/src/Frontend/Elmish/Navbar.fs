module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Import
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Helpers.React.Props
open Iris.Core
open Helpers
open Types
open State

let private withDelay f =
  async {
    do! Async.Sleep(20)
    do f()
  }
  |> Async.StartImmediate

let private navbarItem cb opt key =
  let elem payload = div [ Class "column" ] [ str payload ]
  div [ Class "columns navbar-item"; OnClick (cb opt) ] [
    div [ Class "column is-two-thirds" ] [ str opt ]
    div [ Class "column shortcut" ] [
      Option.map str key |> Option.defaultValue (str "")
    ]
  ]

type [<Pojo>] ViewProps =
  { Dispatch: Msg->unit
    Model: Model }

type [<Pojo>] ViewState =
  { ProjectMenuOpen: bool
    EditMenuOpen: bool
    ConfigMenuOpen: bool
    WindowsMenuOpen: bool
    BurgerMenuOpen: bool }

module ViewState =

  let defaultState =
    { ProjectMenuOpen = false
      EditMenuOpen = false
      ConfigMenuOpen = false
      WindowsMenuOpen = false
      BurgerMenuOpen = false }

  let toggleProject state =
    { defaultState with ProjectMenuOpen = not state.ProjectMenuOpen }

  let toggleEdit state =
    { defaultState with EditMenuOpen = not state.EditMenuOpen }

  let toggleConfig state =
    { defaultState with ConfigMenuOpen = not state.ConfigMenuOpen }

  let toggleWindows state =
    { defaultState with WindowsMenuOpen = not state.WindowsMenuOpen }

  let toggleBurger state =
    { defaultState with BurgerMenuOpen = not state.BurgerMenuOpen }

///  ____            _           _   __  __
/// |  _ \ _ __ ___ (_) ___  ___| |_|  \/  | ___ _ __  _   _
/// | |_) | '__/ _ \| |/ _ \/ __| __| |\/| |/ _ \ '_ \| | | |
/// |  __/| | | (_) | |  __/ (__| |_| |  | |  __/ | | | |_| |
/// |_|   |_|  \___// |\___|\___|\__|_|  |_|\___|_| |_|\__,_|
///               |__/

module private ProjectMenu =
  let [<Literal>] create = "Create"
  let [<Literal>] load = "Load"
  let [<Literal>] save = "Save"
  let [<Literal>] unload = "Unload"
  let [<Literal>] shutdown = "Shutdown"

let private projectMenu onOpen (state:ViewState) (props:ViewProps) =
  let onClick dispatch id _ =
    match id with
    | ProjectMenu.create   -> Modal.CreateProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.load     -> Modal.LoadProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.save     -> Lib.saveProject()
    | ProjectMenu.unload   -> Lib.unloadProject()
    | ProjectMenu.shutdown -> Lib.shutdown()
    | other                -> failwithf "Unknow navbar option: %s" other
    withDelay onOpen
  div [
    classList [
      "navbar-item has-dropdown", true
      "is-active", state.ProjectMenuOpen
    ]
  ] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
      OnClick (fun _ -> onOpen())
    ] [str "Project"]
    div [Class "navbar-dropdown"] [
      navbarItem (onClick props.Dispatch) ProjectMenu.create   None
      navbarItem (onClick props.Dispatch) ProjectMenu.load     None
      navbarItem (onClick props.Dispatch) ProjectMenu.save     (Some "Ctrl-s")
      navbarItem (onClick props.Dispatch) ProjectMenu.unload   None
      navbarItem (onClick props.Dispatch) ProjectMenu.shutdown None
    ]
  ]

///  _____    _ _ _   __  __
/// | ____|__| (_) |_|  \/  | ___ _ __  _   _
/// |  _| / _` | | __| |\/| |/ _ \ '_ \| | | |
/// | |__| (_| | | |_| |  | |  __/ | | | |_| |
/// |_____\__,_|_|\__|_|  |_|\___|_| |_|\__,_|

module private EditMenu =
  let [<Literal>] undo = "Undo"
  let [<Literal>] redo = "Redo"
  let [<Literal>] resetDirty = "Reset Dirty"
  let [<Literal>] filechooser = "Choose File"
  let [<Literal>] settings = "Settings"

let private editMenu onOpen (state:ViewState) (props:ViewProps) =
  let onClick dispatch id _ =
    let start f msg =
      f() |> Promise.iter (fun () -> printfn "%s" msg)
    match id with
    | EditMenu.resetDirty -> Option.iter Lib.resetDirty props.Model.state
    | EditMenu.undo -> Lib.undo()
    | EditMenu.redo -> Lib.redo()
    | EditMenu.filechooser -> Modal.showFileChooser props.Model props.Dispatch
    | EditMenu.settings -> Modal.showSettings props.Model props.Dispatch
    | _ -> ()
    withDelay onOpen
  div [
    classList [
      "navbar-item has-dropdown", true
      "is-active", state.EditMenuOpen
    ]
  ] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
      OnClick (fun _ -> onOpen())
    ] [str "Edit"]
    div [Class "navbar-dropdown"] [
      navbarItem (onClick props.Dispatch) EditMenu.undo (Some "Ctrl-z")
      navbarItem (onClick props.Dispatch) EditMenu.redo (Some "Ctrl-Z")
      navbarItem (onClick props.Dispatch) EditMenu.resetDirty None
      div [ Class "navbar-divider" ] []
      navbarItem (onClick props.Dispatch) EditMenu.filechooser None
      navbarItem (onClick props.Dispatch) EditMenu.settings None
    ]
  ]

/// __        ___           _                   __  __
/// \ \      / (_)_ __   __| | _____      _____|  \/  | ___ _ __  _   _
///  \ \ /\ / /| | '_ \ / _` |/ _ \ \ /\ / / __| |\/| |/ _ \ '_ \| | | |
///   \ V  V / | | | | | (_| | (_) \ V  V /\__ \ |  | |  __/ | | | |_| |
///    \_/\_/  |_|_| |_|\__,_|\___/ \_/\_/ |___/_|  |_|\___|_| |_|\__,_|

module private WindowsMenu =
  let [<Literal>] log = "Logs"
  let [<Literal>] inspector = "Inspector"
  let [<Literal>] fileBrowser = "File Browser"
  let [<Literal>] graph = "Graph"
  let [<Literal>] players = "Players"
  let [<Literal>] cues = "Cues"
  let [<Literal>] cueLists = "Cue Lists"
  let [<Literal>] pinMappings = "Pin Mappings"
  let [<Literal>] project = "Project Overview"
  let [<Literal>] clusterSettings = "Cluster Settings"
  let [<Literal>] clients = "Clients"
  let [<Literal>] sessions = "Sessions"
  let [<Literal>] testWidget1 = "Test Widget 1"
  let [<Literal>] testWidget2 = "Test Widget 2"
  let [<Literal>] testWidget3 = "Test Widget 3"

let private windowsMenu onOpen (state:ViewState) (props:ViewProps) =
  let show name =
    let widget = getWidgetFactory().CreateWidget(None, name)
    AddWidget(widget.Id, widget) |> props.Dispatch
  let onClick id _ =
    match id with
    | WindowsMenu.log             -> show Widgets.Log
    | WindowsMenu.inspector       -> Lib.toggleInspector()
    | WindowsMenu.fileBrowser     -> show Widgets.AssetBrowser
    | WindowsMenu.graph           -> show Widgets.GraphView
    | WindowsMenu.players         -> show Widgets.Players
    | WindowsMenu.cues            -> show Widgets.Cues
    | WindowsMenu.cueLists        -> show Widgets.CueLists
    | WindowsMenu.pinMappings     -> show Widgets.PinMapping
    | WindowsMenu.project         -> show Widgets.ProjectView
    | WindowsMenu.clusterSettings -> show Widgets.Cluster
    | WindowsMenu.clients         -> show Widgets.Clients
    | WindowsMenu.sessions        -> show Widgets.Sessions
    | WindowsMenu.testWidget1     -> show Widgets.Test1
    | WindowsMenu.testWidget2     -> show Widgets.Test2
    | WindowsMenu.testWidget3     -> show Widgets.Test3
    | _ -> ()
    withDelay onOpen
  div [
    classList [
      "navbar-item has-dropdown", true
      "is-active", state.WindowsMenuOpen
    ]
  ] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
      OnClick (fun _ -> onOpen())
    ] [str "Windows"]
    div [Class "navbar-dropdown"]
      ([ WindowsMenu.log,             None
         WindowsMenu.inspector,       Some ("Ctrl-i")
         WindowsMenu.fileBrowser,     Some ("Ctrl-b")
         WindowsMenu.graph,           None
         WindowsMenu.players,         None
         WindowsMenu.cues,            None
         WindowsMenu.cueLists,        None
         WindowsMenu.pinMappings,     None
         WindowsMenu.project,         Some ("Ctrl-p")
         WindowsMenu.clusterSettings, None
         WindowsMenu.clients,         None
         WindowsMenu.sessions,        None
         WindowsMenu.testWidget1,     None
         WindowsMenu.testWidget2,     None
         WindowsMenu.testWidget3,     None ]
       |> List.map (fun (tipe, key) -> navbarItem onClick tipe key))
  ]

type View(props) =
  inherit React.Component<ViewProps, ViewState>(props)
  do base.setInitState(ViewState.defaultState)

  member this.toggleProject() =
    this.state
    |> ViewState.toggleProject
    |> this.setState

  member this.toggleEdit() =
    this.state
    |> ViewState.toggleEdit
    |> this.setState

  member this.toggleConfig() =
    this.state
    |> ViewState.toggleConfig
    |> this.setState

  member this.toggleWindows() =
    this.state
    |> ViewState.toggleWindows
    |> this.setState

  member this.render() =
    let version, buildNumber =
      match this.props.Model.state with
      | Some state ->
        try
          let info = Iris.Web.Core.Client.ClientContext.Singleton.ServiceInfo
          info.version, info.buildNumber
        with ex -> "0.0.0", "123"
      | None -> "0.0.0", "123"
    div [] [
      nav [Id "app-header"; Class "navbar"] [
        div [Id "app-logo"; Class "navbar-brand"] [
          div [Class "navbar-item"] [
            img [Src "lib/img/nsynk.png"]
          ]
          div [
            classList ["navbar-burger", true; "is-active", this.state.BurgerMenuOpen]
            OnClick (fun _ -> this.setState(ViewState.toggleBurger this.state))
          ] [
            span [] []; span [] []; span [] []
          ]
        ]
        div [classList ["navbar-menu", true; "is-active", this.state.BurgerMenuOpen]] [
          div [Class "navbar-start"] [
            projectMenu this.toggleProject this.state this.props
            editMenu    this.toggleEdit    this.state this.props
            windowsMenu this.toggleWindows this.state this.props
          ]
          div [Class "navbar-end"] [
            div [Class "navbar-item"] [
              str(sprintf "Iris v%s - build %s" version buildNumber)
            ]
          ]
        ]
      ]
    ]
