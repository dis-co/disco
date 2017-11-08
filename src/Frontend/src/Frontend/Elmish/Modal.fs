[<AutoOpen>]
module Iris.Web.Modal

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

  type CreateCue(pins: Pin list) =
    let mutable res:string = null
    member __.Pins = pins
    member __.Result: string = res
    interface IModal with
      member this.SetResult(v) = res <- unbox v

  type ProjectConfig(sites: NameAndId[], info: IProjectInfo) =
    let mutable res = None
    member __.Sites = sites
    member __.Info = info
    member __.Result: NameAndId = res.Value
    interface IModal with
      member this.SetResult(v) = res <- Some(unbox v)

  let [<Literal>] private mdir = "../../js/modals/"

  let show dispatch (modal: IModal): React.ReactElement =
    let data, com =
      match modal with
      | :? AddMember              -> None,                 importDefault (mdir+"AddMember")
      | :? CreateProject          -> None,                 importDefault (mdir+"CreateProject")
      | :? LoadProject            -> None,                 importDefault (mdir+"LoadProject")
      | :? CreateCue as m         -> Some(box m.Pins),     importDefault (mdir+"CreateCue")
      | :? Login as m             -> Some(box m.Project),  importDefault (mdir+"Login")
      | :? ProjectConfig as m     -> Some(box m.Sites),    importDefault (mdir+"ProjectConfig")
      | :? AvailableProjects as m -> Some(box m.Projects), importDefault (mdir+"AvailableProjects")
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
