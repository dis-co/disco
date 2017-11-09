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

let private navbarItem cb opt =
  a [Class "navbar-item"; OnClick (cb opt)] [str opt]

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
    { state with
        ProjectMenuOpen = not state.ProjectMenuOpen
        EditMenuOpen = false
        ConfigMenuOpen = false
        WindowsMenuOpen = false
        BurgerMenuOpen = false }

  let toggleEdit state =
    { state with
        EditMenuOpen = not state.EditMenuOpen
        ProjectMenuOpen = false
        ConfigMenuOpen = false
        WindowsMenuOpen = false
        BurgerMenuOpen = false }

  let toggleConfig state =
    { state with
        ConfigMenuOpen = not state.ConfigMenuOpen
        EditMenuOpen = false
        ProjectMenuOpen = false
        WindowsMenuOpen = false
        BurgerMenuOpen = false }

  let toggleWindows state =
    { state with
        WindowsMenuOpen = not state.WindowsMenuOpen
        ConfigMenuOpen = false
        EditMenuOpen = false
        ProjectMenuOpen = false
        BurgerMenuOpen = false }

  let toggleBurger state =
    { state with
        BurgerMenuOpen = not state.BurgerMenuOpen
        WindowsMenuOpen = false
        ConfigMenuOpen = false
        EditMenuOpen = false
        ProjectMenuOpen = false }

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
    let start f msg =
      f() |> Promise.iter (fun () -> printfn "%s" msg)
    match id with
    | ProjectMenu.create   -> Modal.CreateProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.load     -> Modal.LoadProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.save     -> start Lib.saveProject "Project has been saved"
    | ProjectMenu.unload   -> start Lib.unloadProject "Project has been unloaded"
    | ProjectMenu.shutdown -> start Lib.shutdown "Iris has been shut down"
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
      navbarItem (onClick props.Dispatch) ProjectMenu.create
      navbarItem (onClick props.Dispatch) ProjectMenu.load
      navbarItem (onClick props.Dispatch) ProjectMenu.save
      navbarItem (onClick props.Dispatch) ProjectMenu.unload
      navbarItem (onClick props.Dispatch) ProjectMenu.shutdown
    ]
  ]

///  _____    _ _ _   __  __
/// | ____|__| (_) |_|  \/  | ___ _ __  _   _
/// |  _| / _` | | __| |\/| |/ _ \ '_ \| | | |
/// | |__| (_| | | |_| |  | |  __/ | | | |_| |
/// |_____\__,_|_|\__|_|  |_|\___|_| |_|\__,_|

module private EditMenu =
  let [<Literal>] resetDirty = "Reset Dirty"

let private editMenu onOpen (state:ViewState) (props:ViewProps) =
  let onClick dispatch id _ =
    let start f msg =
      f() |> Promise.iter (fun () -> printfn "%s" msg)
    match id with
    | EditMenu.resetDirty -> Option.iter Lib.resetDirty props.Model.state
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
      div [ Class "navbar-item"] [
        navbarItem (onClick props.Dispatch) EditMenu.resetDirty
      ]
    ]
  ]

///   ____             __ _       __  __
///  / ___|___  _ __  / _(_) __ _|  \/  | ___ _ __  _   _
/// | |   / _ \| '_ \| |_| |/ _` | |\/| |/ _ \ '_ \| | | |
/// | |__| (_) | | | |  _| | (_| | |  | |  __/ | | | |_| |
///  \____\___/|_| |_|_| |_|\__, |_|  |_|\___|_| |_|\__,_|
///                         |___/

module private ConfigMenu =
  let [<Literal>] rightClick = "Use right click"

let private configMenu onOpen (state:ViewState) (props:ViewProps) =
  div [
    classList [
      "navbar-item has-dropdown", true
      "is-active", state.ConfigMenuOpen
    ]
  ] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
      OnClick (fun _ -> onOpen())
    ] [str "Config"]
    div [Class "navbar-dropdown"] [
      div [
        Class "navbar-item field"
      ] [
        div [ Class "control"; Style [ MarginRight "5px" ] ] [
          input [
            Type "checkbox"
            Checked props.Model.userConfig.useRightClick
            OnClick (fun _ ->
              { props.Model.userConfig
                  with useRightClick = not props.Model.userConfig.useRightClick }
              |> UpdateUserConfig
              |> props.Dispatch
              withDelay onOpen)
          ]
        ]
        label [
          Class "label"
          Style [ FontSize "12px"; FontWeight "normal" ]
        ] [
          str ConfigMenu.rightClick
        ]
      ]
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
      ([ WindowsMenu.log
         WindowsMenu.inspector
         WindowsMenu.graph
         WindowsMenu.players
         WindowsMenu.cues
         WindowsMenu.cueLists
         WindowsMenu.pinMappings
         WindowsMenu.project
         WindowsMenu.clusterSettings
         WindowsMenu.clients
         WindowsMenu.sessions
         WindowsMenu.testWidget1
         WindowsMenu.testWidget2
         WindowsMenu.testWidget3 ]
       |> List.map (navbarItem onClick))
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
            configMenu  this.toggleConfig  this.state this.props
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
