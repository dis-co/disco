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
      | None -> Promise.lift ())

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

[<PassGenerics>]
let private loadFromLocalStorage<'T> (key: string) =
  let g = Fable.Import.Browser.window
  match g.localStorage.getItem(key) with
  | null -> None
  | value -> ofJson<'T> !!value |> Some

let private saveToLocalStorage (key: string) (value: obj) =
  let g = Fable.Import.Browser.window
  g.localStorage.setItem(key, toJson value)

let delay ms (f:'T->unit) =
  fun x ->
    Promise.sleep ms
    |> Promise.iter (fun () -> f x)

/// Initialization function for Elmish state
let init() =
  let startContext dispatch =
    let context = ClientContext.Singleton
    context.Start()
    |> Promise.iter (fun () ->
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
  let widgets =
    let factory = Types.getWidgetFactory()
    loadFromLocalStorage<WidgetRef[]> StorageKeys.widgets
    |> Option.defaultValue [||]
    |> Array.map (fun (id, name) ->
      let widget = factory.CreateWidget(Some id, name)
      id, widget)
    |> Map
  let layout =
    loadFromLocalStorage<Layout[]> StorageKeys.layout
    |> Option.defaultValue [||]
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
      selectedPins = Set.empty
      userConfig = UserConfig.Create() }
  // Delay the display of the modal dialog to let
  // other plugins (like jQuery ui-layout) load
  initModel, [startContext; delay 500 displayAvailableProjectsModal]

let private saveWidgetsAndLayout (widgets: Map<Guid,IWidget>) (layout: Layout[]) =
    widgets
    |> Seq.map (fun kv -> kv.Key, kv.Value.Name)
    |> Seq.toArray |> saveToLocalStorage StorageKeys.widgets
    layout |> saveToLocalStorage StorageKeys.layout

let private addCue (cueList:CueList) (cueGroupIndex:int) (cueIndex:int) =
  // TODO: Select the cue list from the widget
  if cueList.Groups.Length = 0 then
    failwith "A Cue Group must be added first"
  // Create new Cue and CueReference
  let newCue = { Id = IrisId.Create(); Name = name "Untitled"; Slices = [||] }
  let newCueRef = { Id = IrisId.Create(); CueId = newCue.Id; AutoFollow = -1; Duration = -1; Prewait = -1 }
  // Insert new CueRef in the selected CueGroup after the selected cue
  let cueGroup = cueList.Groups.[max cueGroupIndex 0]
  let idx = if cueIndex < 0 then cueGroup.CueRefs.Length - 1 else cueIndex
  let newCueGroup = { cueGroup with CueRefs = Array.insertAfter idx newCueRef cueGroup.CueRefs }
  // Update the CueList
  let newCueList = { cueList with Groups = Array.replaceById newCueGroup cueList.Groups }
  // Send messages to backend
  AddCue newCue |> ClientContext.Singleton.Post
  UpdateCueList newCueList |> ClientContext.Singleton.Post

let [<Literal>] maxLength = 4
let chop (list: 'a list) =
  match list with
  | list when list.Length > maxLength ->
    list |> List.rev |> List.tail |> List.rev
  | _ -> list

/// Update function for Elmish state
let update msg model: Model*Cmd<Msg> =
  match msg with
  | AddWidget(id, widget) ->
    let widgets = Map.add id widget model.widgets
    let layout = Array.append model.layout [|widget.InitialLayout|]
    saveWidgetsAndLayout widgets layout
    { model with widgets = widgets; layout = layout }, []
  | RemoveWidget id ->
    let widgets = Map.remove id model.widgets
    let layout = model.layout |> Array.filter (fun x -> x.i <> id)
    saveWidgetsAndLayout widgets layout
    { model with widgets = widgets; layout = layout }, []
  // | AddTab -> // Add tab and remove widget
  // | RemoveTab -> // Optional, add widget

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

  | SelectElement selected ->
    let history = selected :: model.history.previous |> chop
    { model with
        selectedPins =
          match selected with
          | InspectorSelection.Pin(_,_,pinId,multi) ->
            if multi then Set.add pinId model.selectedPins else set [pinId]
          | _ -> model.selectedPins
        history = { model.history with
                     selected = selected
                     index = 0
                     previous = history } }, []
  | AddLog log ->
    { model with logs = log::model.logs }, []
  | AddCueUI(cueList, cueGroupIndex, cueIndex) ->
    addCue cueList cueGroupIndex cueIndex
    model, []
  | UpdateLayout layout ->
    saveToLocalStorage StorageKeys.layout layout
    { model with layout = layout }, []
  | UpdateUserConfig cfg ->
    { model with userConfig = cfg }, []
  | UpdateState state ->
    let cmd =
      match model.state, state, model.modal with
      // If a project is loaded (model.state from None to Some), hide modals
      | None, Some _, Some modal -> [hideModal modal]
      // If a project is unloaded (model.state from None to Some), display AvailableProjects modal
      | Some _, None, None -> [displayAvailableProjectsModal]
      | _ -> []
    { model with state = state }, cmd
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
