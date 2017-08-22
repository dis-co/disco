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
  importDefault "../../../src/modals/CreateProject"

let LoadProjectModal: React.ComponentClass<ModalProps<obj, IProjectInfo>> =
  importDefault "../../../src/modals/LoadProject"

let ProjectConfigModal: React.ComponentClass<ModalProps<string[], string>> =
  importDefault "../../../src/modals/ProjectConfig"

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
    makeModal dispatch CreateProjectModal None
    |> Promise.iter Lib.createProject
  | Options.loadProject ->
    promise {
      let! info = makeModal dispatch LoadProjectModal None
      let! err = Lib.loadProject(info.name, info.username, info.password, None, None)
      match err with
      | Some err ->
        // Get project sites and machine config
        let! sites = Lib.getProjectSites(info.name, info.username, info.password)
        // Ask user to create or select a new config
        let! site = makeModal dispatch ProjectConfigModal (Some sites)
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

let view dispatch (model: Model) =
  let version, buildNumber =
    match model.state with
    | Some state ->
      try
        let info = Iris.Web.Core.Client.ClientContext.Singleton.ServiceInfo
        info.version, info.buildNumber
      with ex ->
        // printfn "Cannot read ServiceInfo from ClientContext"
        "0.0.0", "123"
    | None -> "0.0.0", "123"
  div [] [
    nav [Id "app-header"; Class "navbar "] [
      div [Class "navbar-brand"] [
        a [Class "navbar-item"; Href "http://nsynk.de"] [
          img [Src "lib/img/nsynk.png"]
        ]
      ]
      div [Class "navbar-menu is-active"] [
        div [Class "navbar-start"] [
          dropdown dispatch model
        ]
        div [Class "navbar-end"] [
          div [Class "navbar-item"] [
            str(sprintf "Iris v%s - build %s" version buildNumber)
          ]
        ]
      ]
    ]
  ]
