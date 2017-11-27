[<AutoOpen>]
module Iris.Web.Modal

// * Imports

open System
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Elmish.React
open Iris.Core
open Iris.Core.Commands
open Types
open Iris.Web.AssetBrowserView

// * Modal module

/// Modal dialogs
[<RequireQualifiedAccess>]
module Modal =

  // ** types

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

  type CreateCue(pins: Pin list) =
    let mutable res:string = null
    member __.Pins = pins
    member __.Result: string = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type InsertCues(cues: Cue[], cueList: CueList, groupIdx:int, cueIdx: int) =
    let mutable res:CueId array = Array.empty
    member __.Cues = cues
    member __.CueList = cueList
    member __.SelectedCueGroupIndex = groupIdx
    member __.SelectedCueIndex = cueIdx
    member __.Result: CueId[] = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type UpdateCues(cues: Cue[], pins: Pin list) =
    let mutable res:CueId array = Array.empty
    member __.Cues = cues
    member __.Pins = pins
    member __.Result: CueId array = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type ProjectConfig(sites: NameAndId[], info: IProjectInfo) =
    let mutable res = None
    member __.Sites = sites
    member __.Info = info
    member __.Result: NameAndId = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

  type EditSettings(config:UserConfig) =
    let mutable res = config.useRightClick
    member __.Result = res
    member __.UserConfig = config
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type FileChooser(state:State) =
    let mutable res: FsPath List = List.empty
    member __.Result = res
    member __.State = state
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  // ** mdir

  let [<Literal>] private mdir = "../../js/modals/"

  // ** importModal

  let private importModal (modal:IModal) =
    match modal with
    | :? AddMember         -> importDefault (mdir+"AddMember")
    | :? CreateProject     -> importDefault (mdir+"CreateProject")
    | :? LoadProject       -> importDefault (mdir+"LoadProject")
    | :? EditSettings      -> importDefault (mdir+"EditSettings")
    | :? CreateCue         -> importDefault (mdir+"CreateCue")
    | :? InsertCues        -> importDefault (mdir+"SelectCues")
    | :? UpdateCues        -> importDefault (mdir+"SelectCues")
    | :? Login             -> importDefault (mdir+"Login")
    | :? ProjectConfig     -> importDefault (mdir+"ProjectConfig")
    | :? AvailableProjects -> importDefault (mdir+"AvailableProjects")
    | _ -> failwithf "Cannot render unknown modal %A" modal

  // ** renderJsModal

  let private renderJsModal (modal: IModal) (data: obj option) dispatch =
    let props =
      createObj [
        "data" ==> data
        "onSubmit" ==> fun res -> CloseModal(modal, Choice1Of2 res) |> dispatch
      ]
    let view = importModal modal
    from view props []

  // ** renderFileChooser

  let private renderFileChooser model (modal:IModal) selectable data dispatch =
    let closeModal _ =
      let fileChooser = modal :?> FileChooser
      CloseModal(modal, Choice1Of2 (unbox fileChooser.Result)) |> dispatch
    div [] [
      p [ ClassName "title has-text-centered" ] [ str "Choose File" ]
      com<AssetBrowserView,_,_>
        { Id = Guid.NewGuid()
          Model = model
          Selectable = selectable
          OnSelect = Some modal.SetResult
          Dispatch = dispatch }
        []
      div [ ClassName "field is-grouped" ] [
        p [ ClassName "control" ] [
          button [
            ClassName "button is-primary"
            OnClick closeModal
          ] [ str "Submit" ]
        ]
      ]
    ]

  // ** renderModal

  let private renderModal model (modal:IModal) dispatch =
    match modal with
    | :? AddMember              -> renderJsModal     modal None                     dispatch
    | :? CreateProject          -> renderJsModal     modal None                     dispatch
    | :? LoadProject            -> renderJsModal     modal None                     dispatch
    | :? EditSettings      as m -> renderJsModal     modal (Some(box m.UserConfig)) dispatch
    | :? CreateCue         as m -> renderJsModal     modal (Some(box m.Pins))       dispatch
    | :? InsertCues        as m -> renderJsModal     modal (Some(box m.Cues))       dispatch
    | :? UpdateCues        as m -> renderJsModal     modal (Some(box m.Cues))       dispatch
    | :? Login             as m -> renderJsModal     modal (Some(box m.Project))    dispatch
    | :? ProjectConfig     as m -> renderJsModal     modal (Some(box m.Sites))      dispatch
    | :? AvailableProjects as m -> renderJsModal     modal (Some(box m.Projects))   dispatch
    | :? FileChooser       as m -> renderFileChooser model modal Selectable.Files (Some(box m)) dispatch
    | _ -> failwithf "Unknown modal type: %A" modal

  // ** show

  let show model dispatch (modal: IModal): React.ReactElement =
    div [ClassName "modal is-active"] [
      div [
        ClassName "modal-background"
        OnClick (fun ev ->
          ev.stopPropagation()
          CloseModal(modal, Choice2Of2 ()) |> dispatch )
      ] []
      div [ClassName "modal-content"] [
        div [ClassName "box"] [
          renderModal model modal dispatch
        ]
      ]
    ]

  // ** asIModal

  let private asIModal (t: 't when 't :> IModal) = t :> IModal

  // ** showFileChooser

  let showFileChooser (model:Model) dispatch =
    Option.iter (FileChooser >> asIModal >> OpenModal >> dispatch) model.state

  // ** showSettings

  let showSettings (model:Model) dispatch =
      model.userConfig |> EditSettings |> asIModal |> OpenModal |> dispatch
