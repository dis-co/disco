module rec Iris.Web.State

open System
open Iris.Core
open Iris.Core.Commands
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Fable.Import
open Elmish
open Types
open Helpers

let loadProject dispatch site (info: IProjectInfo) =
  Lib.loadProject(info.name, info.username, info.password, site, None)
  |> Promise.bind (function
    | Some err ->
      // Get project sites and machine config
      Lib.getProjectSites(info.name, info.username, info.password)
      |> Promise.map (fun sites ->
        // Ask user to create or select a new config
        Modal.ProjectConfig(sites, info) :> IModal |> OpenModal |> dispatch)
    | None ->
      dispatch
      |> displayAvailableProjectsModal
      |> Promise.lift)

let handleModalResult (modal: IModal) dispatch =
  match modal with
  | :? Modal.AddMember as m ->
    m.Result |> Lib.addMember
  | :? Modal.CreateProject as m ->
    m.Result
    |> Lib.createProject
    |> Promise.iter (function
      | Some name -> Modal.Login(name) :> IModal |> OpenModal |> dispatch
      | None -> ())
  | :? Modal.LoadProject as m ->
    m.Result |> loadProject dispatch None |> Promise.start
  | :? Modal.AvailableProjects as m ->
    match m.Result with
    | Some n -> Modal.Login(n) :> IModal |> OpenModal |> dispatch
    | None -> Modal.CreateProject() :> IModal |> OpenModal |> dispatch
  | :? Modal.EditSettings as m ->
    { m.UserConfig with useRightClick = m.Result }
    |> UpdateUserConfig
    |> dispatch
  | :? Modal.CreateCue as m ->
    match m.Result with
    | null -> ()
    | str -> Lib.createCue str m.Pins
  | :? Modal.InsertCues as m ->
    match m.Result with
    | [| |] -> ()
    | ids -> Lib.groupAddCues ids m.Cues m.CueList m.SelectedCueGroupIndex m.SelectedCueIndex
  | :? Modal.UpdateCues as m ->
    match m.Result with
    | [| |] -> ()
    | selected -> Lib.updateCues selected m.Pins m.Cues
  | :? Modal.Login as m ->
    match m.Result with
    | Some projInfo ->
      loadProject dispatch None projInfo |> Promise.start
    | None -> Modal.CreateProject() :> IModal |> OpenModal |> dispatch
  | :? Modal.ProjectConfig as m ->
    // Try loading the project again with the site config
    loadProject dispatch (Some m.Result) m.Info |> Promise.start
  | _ -> failwithf "Cannot handle unknown modal %A" modal

let private hideModal modal dispatch =
  CloseModal(modal, Choice2Of2 ()) |> dispatch

let private displayAvailableProjectsModal dispatch =
  promise {
    #if DESIGN
    let projects = [|name "foo"; name "bar"|]
    #else
    let! projects = Lib.listProjects()
    #endif
    if projects.Length > 0
    then Modal.AvailableProjects(projects) :> IModal
    else Modal.CreateProject() :> IModal
    |> OpenModal |> dispatch
  } |> Promise.start

/// Unfortunately this is necessary to hide the resizer of
/// the jQuery plugin ui-layout
let private toggleUILayoutResizer (visible: bool) =
  let setVisibility selector visibility =
    let results = Browser.document.querySelectorAll(selector)
    for i = 0 to (!!results.length - 1) do
      results.[i]?style?visibility <- visibility
  let visibility =
    if visible then "visible" else "hidden"
  setVisibility ".ui-layout-resizer" visibility
  setVisibility ".ui-layout-toggler" visibility

let delay ms (f:'T->unit) =
  fun x ->
    Promise.sleep ms
    |> Promise.iter (fun () -> f x)

let getKeyBindings (dispatch: Dispatch<Msg>): KeyBinding array =
  let postCmd cmd =
    fun () -> StateMachine.Command cmd |> ClientContext.Singleton.Post
  //  ctrl, shift, key, action
  [| true,  false, Codes.z,         Lib.undo
     true,  true,  Codes.z,         Lib.redo
     true,  false, Codes.s,         Lib.saveProject
     true,  false, Codes.i,         Lib.toggleInspector
     false, false, Codes.delete, fun () -> dispatch RemoveSelectedDragItems
  |]

/// Initialization function for Elmish state
let init() =
  let startContext dispatch =
    let context = ClientContext.Singleton
    context.Start()
    |> Promise.iter (fun () ->
      getKeyBindings dispatch
      |> Keyboard.registerKeyHandlers
      context.OnMessage
      |> Observable.add (function
        | ClientMessage.Event(_, LogMsg log) ->
          AddLog log |> dispatch
        // TODO: Add clock to Elmish state?
        | ClientMessage.Event(_, UpdateClock _) -> ()
        // For all other cases, just update the state
        | _ ->
          let state = context.Store |> Option.map (fun s -> s.State)
          UpdateState state |> dispatch)
      )

  let layout = Layout.load ()
  let widgets = Layout.createWidgets layout

  let initModel =
    { widgets = widgets
      layout = layout
      state = None
      modal = None
      #if DESIGN // Mockup data
      logs = List.init 50 (fun _ -> Core.MockData.genLog())
      #else
      logs = []
      #endif
      history = { index = 0; selected = InspectorSelection.Nothing; previous = [] }
      selectedDragItems = DragItems.Pins []
      userConfig = UserConfig.Create() }
  // Delay the display of the modal dialog to let
  // other plugins (like jQuery ui-layout) load
  initModel, [startContext; delay 500 displayAvailableProjectsModal]

let private saveWidgetsAndLayout (widgets: Map<Guid,IWidget>) (layout: Layout) =
  Layout.save layout

let [<Literal>] maxLength = 4
let chop (list: 'a list) =
  match list with
  | list when list.Length > maxLength ->
    list |> List.rev |> List.tail |> List.rev
  | _ -> list

/// Update function for Elmish state
let update msg model: Model*Cmd<Msg> =
  match msg with
  ///  _____     _
  /// |_   _|_ _| |__
  ///   | |/ _` | '_ \
  ///   | | (_| | |_) |
  ///   |_|\__,_|_.__/

  | UpdateTabs TabAction.AddTab ->
    let name = sprintf "Untitled %d" (model.layout |> Layout.tabs |> Array.length)
    let tab = Tab.create name
    let layout = tab |> flip Layout.addTab model.layout |> Layout.setSelected tab.Id
    let widgets = Layout.createWidgets layout
    { model with widgets = widgets; layout = layout },[]

  | UpdateTabs (TabAction.SelectTab id) ->
    let layout = Layout.setSelected id model.layout
    let widgets = Layout.createWidgets layout
    { model with widgets = widgets; layout = layout },[]

  | UpdateTabs (TabAction.RemoveTab id) ->
    printfn "RemoveTab"
    model,[]

  /// __        ___     _            _
  /// \ \      / (_) __| | __ _  ___| |_
  ///  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
  ///   \ V  V / | | (_| | (_| |  __/ |_
  ///    \_/\_/  |_|\__,_|\__, |\___|\__|
  ///                     |___/

  | AddWidget(id, widget) ->
    let widgets = Map.add widget.Id widget model.widgets
    let layout = Layout.addWidget widget model.layout
    Layout.save layout
    { model with widgets = widgets; layout = layout }, []

  | RemoveWidget id ->
    let widgets = Map.remove id model.widgets
    let layout = Layout.removeWidget id model.layout
    Layout.save layout
    { model with widgets = widgets; layout = layout }, []

  ///  ____                     _              _ ____
  /// |  _ \ _ __ __ _  __ _   / \   _ __   __| |  _ \ _ __ ___  _ __
  /// | | | | '__/ _` |/ _` | / _ \ | '_ \ / _` | | | | '__/ _ \| '_ \
  /// | |_| | | | (_| | (_| |/ ___ \| | | | (_| | |_| | | | (_) | |_) |
  /// |____/|_|  \__,_|\__, /_/   \_\_| |_|\__,_|____/|_|  \___/| .__/
  ///                  |___/                                    |_|

  | RemoveSelectedDragItems ->
    match model.state, model.selectedDragItems with
    | Some state, DragItems.CueAtoms ids ->
      // Group id tuples by CueId (first one)
      ([], Seq.groupBy fst ids) ||> Seq.fold (fun cmds (cueId, ids) ->
        let cue = Lib.findCue cueId state
        if Lib.mayAlterCue state cue then
          Lib.removeSlicesFromCue cue (Seq.map snd ids)
          |> cons cmds
        else cmds)
      |> Lib.postStateCommands
    | _ -> ()
    model, []

  | SelectDragItems(newItems, multi) ->
    { model with selectedDragItems =
                  if multi
                  then model.selectedDragItems.Append(newItems)
                  else newItems
    }, []

  | SelectElement selected ->
    let history = selected :: model.history.previous |> chop
    { model with
        history = { model.history with
                     selected = selected
                     index = 0
                     previous = history } }, []

  ///  ___                           _
  /// |_ _|_ __  ___ _ __   ___  ___| |_ ___  _ __
  ///  | || '_ \/ __| '_ \ / _ \/ __| __/ _ \| '__|
  ///  | || | | \__ \ |_) |  __/ (__| || (_) | |
  /// |___|_| |_|___/ .__/ \___|\___|\__\___/|_|
  ///               |_|

  | Navigate cmd when not (List.isEmpty model.history.previous) ->
    let history =
      try
        let index = cmd |> function
          | InspectorNavigate.Previous -> model.history.index + 1
          | InspectorNavigate.Next     -> model.history.index - 1
          | InspectorNavigate.Set idx  -> idx
        { model.history with
            index = index
            selected = model.history.previous.[index] }
      with _ -> model.history
    { model with history = history }, []

  | Navigate _ -> model, []

  | UpdateInspector InspectorAction.Open ->
    let layout = Layout.setInspectorOpen true model.layout
    Layout.save layout
    { model with layout = layout }, []

  | UpdateInspector InspectorAction.Close ->
    let layout = Layout.setInspectorOpen false model.layout
    Layout.save layout
    { model with layout = layout }, []

  | UpdateInspector (InspectorAction.Resize width) ->
    let layout = Layout.setInspectorSize width model.layout
    Layout.save layout
    { model with layout = layout }, []

  ///  _                            _
  /// | |    __ _ _   _  ___  _   _| |_
  /// | |   / _` | | | |/ _ \| | | | __|
  /// | |__| (_| | |_| | (_) | |_| | |_
  /// |_____\__,_|\__, |\___/ \__,_|\__|
  ///             |___/

  | UpdateLayout widgets ->
    let layout = Layout.setWidgets widgets model.layout
    Layout.save layout
    { model with layout = layout }, []

  ///   ____             __ _
  ///  / ___|___  _ __  / _(_) __ _
  /// | |   / _ \| '_ \| |_| |/ _` |
  /// | |__| (_) | | | |  _| | (_| |
  ///  \____\___/|_| |_|_| |_|\__, |
  ///                         |___/

  | UpdateUserConfig cfg ->
    { model with userConfig = cfg }, []

  ///  ____  _        _
  /// / ___|| |_ __ _| |_ ___
  /// \___ \| __/ _` | __/ _ \
  ///  ___) | || (_| | ||  __/
  /// |____/ \__\__,_|\__\___|

  | UpdateState state ->
    let cmd =
      match model.state, state, model.modal with
      // If a project is loaded (model.state from None to Some), hide modals
      | None, Some _, Some modal -> [hideModal modal]
      // If a project is unloaded (model.state from None to Some), display AvailableProjects modal
      | Some _, None, None -> [displayAvailableProjectsModal]
      | _ -> []
    { model with state = state }, cmd

  ///  __  __           _       _
  /// |  \/  | ___   __| | __ _| |
  /// | |\/| |/ _ \ / _` |/ _` | |
  /// | |  | | (_) | (_| | (_| | |
  /// |_|  |_|\___/ \__,_|\__,_|_|

  | OpenModal modal ->
    toggleUILayoutResizer false
    match model.modal with
    | None -> ()
    | Some modal -> printfn "Modal to be opened before closing %A" modal
    match modal, model.state with
    // If there's already a loaded project (state.IsSome),
    // ignore AvailableProjects modal
    | :? Modal.AvailableProjects, Some _ ->
      toggleUILayoutResizer true
      { model with modal = None }, []
    | _ ->
      { model with modal = Some modal }, []

  | CloseModal(modal, result) ->
    toggleUILayoutResizer true
    let cmd =
      match model.modal with
      | None ->
        printfn "Modal is not open: %A (%A)" modal result
        []
      | Some currentModal ->
        if obj.ReferenceEquals(modal, currentModal) |> not then
          printfn "Modal to be closed, %A (%A), different from open modal %A"
            modal result currentModal
          []
        else
          match result with
          | Choice1Of2 result ->
            modal.SetResult(result)
            [handleModalResult modal]
          | Choice2Of2 () -> [] // For now, just ignore cancelled modals
    { model with modal = None }, cmd

  ///  _
  /// | |    ___   __ _
  /// | |   / _ \ / _` |
  /// | |__| (_) | (_| |
  /// |_____\___/ \__, |
  ///             |___/

  | AddLog log ->
    { model with logs = log::model.logs }, []
