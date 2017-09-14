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

let loadProject dispatch (info: IProjectInfo) = promise {
    let! err = Lib.loadProject(info.name, info.username, info.password, None, None)
    match err with
    | Some err ->
      // Get project sites and machine config
      let! sites = Lib.getProjectSites(info.name, info.username, info.password)

      Array.iter (fun (x:NameAndId) -> printfn "name: %A id %A" x.Name x.Id) sites

      // Ask user to create or select a new config
      let! site = makeModal<IrisId> dispatch (Modal.ProjectConfig sites)

      printfn "site: %A" site

      // Try loading the project again with the site config
      let! err2 = Lib.loadProject(info.name, info.username, info.password, Some site, None)
      err2 |> Option.iter (printfn "Error when loading site %A: %A" site)
    | None -> ()
  }

let private displayNoProjectModal dispatch =
  promise {
    #if DESIGN
    let projects = [|name "foo"; name "bar"|]
    #else
    let! projects = Lib.listProjects()
    #endif
    if projects.Length > 0 then
      let! projectInfo = makeModal dispatch (Modal.NoProject projects)
      do!
        match projectInfo with
        | Some projectInfo ->
          loadProject dispatch projectInfo
        | None ->
          makeModal dispatch Modal.CreateProject
          |> Promise.bind Lib.createProject
    else
      do! makeModal dispatch Modal.CreateProject
          |> Promise.bind Lib.createProject
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
      userConfig = UserConfig.Create() }
  // Delay the display of the modal dialog to let
  // other plugins (like jQuery ui-layout) load
  initModel, [startContext; delay 500 displayNoProjectModal]

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
    match state, model.modal with
    // When project is loaded, hide NoProject modal if displayed
    | Some _, Some { modal = Modal .NoProject _ } ->
      toggleUILayoutResizer true
      { model with state = state; modal = None }, []
    | Some _, _
    | None, Some { modal = Modal .NoProject _ } ->
      { model with state = state }, []
    | None, _ ->
      { model with state = state }, [displayNoProjectModal]
  | UpdateModal modal ->
    let cmd =
      match modal, model.state with
      // If no modal and no state, display no project modal
      | None, None -> [displayNoProjectModal]
      | None, Some _ -> toggleUILayoutResizer true; []
      | Some _, _ -> toggleUILayoutResizer false; []
    { model with modal = modal }, cmd
