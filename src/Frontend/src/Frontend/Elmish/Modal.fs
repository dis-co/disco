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

  // ** mdir

  let [<Literal>] private mdir = "../../js/modals/"

  // ** show

  let show dispatch (modal: IModal): React.ReactElement =
    let data, com =
      match modal with
      | :? AddMember              -> None,                   importDefault (mdir+"AddMember")
      | :? CreateProject          -> None,                   importDefault (mdir+"CreateProject")
      | :? LoadProject            -> None,                   importDefault (mdir+"LoadProject")
      | :? EditSettings as m      -> Some(box m.UserConfig), importDefault (mdir+"EditSettings")
      | :? CreateCue as m         -> Some(box m.Pins),       importDefault (mdir+"CreateCue")
      | :? InsertCues as m        -> Some(box m.Cues),       importDefault (mdir+"SelectCues")
      | :? UpdateCues as m        -> Some(box m.Cues),       importDefault (mdir+"SelectCues")
      | :? Login as m             -> Some(box m.Project),    importDefault (mdir+"Login")
      | :? ProjectConfig as m     -> Some(box m.Sites),      importDefault (mdir+"ProjectConfig")
      | :? AvailableProjects as m -> Some(box m.Projects),   importDefault (mdir+"AvailableProjects")
      | _ -> failwithf "Cannot render unknown modal %A" modal
    let props =
      createObj [
        "data" ==> data
        "onSubmit" ==> fun res -> CloseModal(modal, Choice1Of2 res) |> dispatch
      ]
    div [ClassName "modal is-active"] [
      div [
        ClassName "modal-background"
        OnClick (fun ev ->
          ev.stopPropagation()
          CloseModal(modal, Choice2Of2 ()) |> dispatch )
      ] []
      div [ClassName "modal-content"] [
        div [ClassName "box"] [from com props []]
      ]
    ]
