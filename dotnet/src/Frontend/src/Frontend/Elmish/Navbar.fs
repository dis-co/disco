module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Import
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core
open Helpers
open Types

type IProjectInfo =
  abstract name: Name
  abstract username: UserName
  abstract password: Password

let CreateProjectModal: React.ComponentClass<ModalProps<obj, string>> =
  importDefault "../../js/modals/CreateProject"

let LoadProjectModal: React.ComponentClass<ModalProps<obj, IProjectInfo>> =
  importDefault "../../js/modals/LoadProject"

let ProjectConfigModal: React.ComponentClass<ModalProps<string[], string>> =
  importDefault "../../js/modals/ProjectConfig"

module Options =
  let [<Literal>] createProject = "Create Project"
  let [<Literal>] loadProject = "Load Project"
  let [<Literal>] saveProject = "Save Project"
  let [<Literal>] unloadProject = "Unload Project"
  let [<Literal>] shutdown = "Shutdown"

let onClick dispatch id _ =
  let start f msg =
    f() |> Promise.iter (fun () -> printfn "%s" msg)
  match id with
  | Options.createProject ->
    makeModal dispatch Modals.CreateProject CreateProjectModal None
    |> Promise.iter Lib.createProject
  | Options.loadProject ->
    promise {
      let! info = makeModal dispatch Modals.LoadProject LoadProjectModal None
      let! err = Lib.loadProject(info.name, info.username, info.password, None, None)
      match err with
      | Some err ->
        // Get project sites and machine config
        let! sites = Lib.getProjectSites(info.name, info.username, info.password)
        // Ask user to create or select a new config
        let! site = makeModal dispatch Modals.ProjectConfig ProjectConfigModal (Some sites)
        // Try loading the project again with the site config
        let! err2 = Lib.loadProject(info.name, info.username, info.password, Some (Id site), None)
        err2 |> Option.iter (printfn "Error when loading site %s: %s" site)
      | None -> ()
    } |> Promise.start
  | Options.saveProject ->
    start Lib.saveProject "Project has been saved"
  | Options.unloadProject ->
    start Lib.unloadProject "Project has been unloaded"
  | Options.shutdown ->
    start Lib.shutdown "Iris has been shut down"
  | other ->
    failwithf "Unknow navbar option: %s" other

open Fable.Helpers.React.Props

let dropdown dispatch (model: Model) =
  let navbarItem opt =
    a [Class "navbar-item"; OnClick (onClick dispatch opt)] [str opt]
  div [Class "navbar-item has-dropdown is-hoverable"] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
    ] [str "Iris Menu"]
    div [Class "navbar-dropdown"] [
      navbarItem Options.createProject
      navbarItem Options.loadProject
      navbarItem Options.saveProject
      navbarItem Options.unloadProject
      navbarItem Options.shutdown
      a [
        Class "navbar-item"
        OnClick (fun _ ->
          { model.userConfig with useRightClick = not model.userConfig.useRightClick }
          |> UpdateUserConfig |> dispatch)
      ] [str ("Use right click: " + (string model.userConfig.useRightClick))]
    ]
  ]

type [<Pojo>] ViewProps =
  { Dispatch: Msg->unit
    Model: Model }

type [<Pojo>] ViewState =
  { IsMenuOpen: bool }

type View(props) =
  inherit React.Component<ViewProps, ViewState>(props)
  do base.setInitState({ IsMenuOpen = false })
  member this.render() =
    let version, buildNumber =
      match this.props.Model.state with
      | Some state ->
        try
          let info = Iris.Web.Core.Client.ClientContext.Singleton.ServiceInfo
          info.version, info.buildNumber
        with ex ->
          // printfn "Cannot read ServiceInfo from ClientContext"
          "0.0.0", "123"
      | None -> "0.0.0", "123"
    div [] [
      nav [Id "app-header"; Class "navbar"] [
        div [Class "navbar-brand"] [
          div [Class "navbar-item"] [
            img [Src "lib/img/nsynk.png"]
          ]
          div [
            classList ["navbar-burger", true; "is-active", this.state.IsMenuOpen]
            OnClick (fun _ -> this.setState({ IsMenuOpen = not this.state.IsMenuOpen }))
          ] [
            span [] []; span [] []; span [] []
          ]
        ]
        div [classList ["navbar-menu", true; "is-active", this.state.IsMenuOpen]] [
          div [Class "navbar-start"] [
            dropdown this.props.Dispatch this.props.Model
          ]
          div [Class "navbar-end"] [
            div [Class "navbar-item"] [
              str(sprintf "Iris v%s - build %s" version buildNumber)
            ]
          ]
        ]
      ]
    ]
