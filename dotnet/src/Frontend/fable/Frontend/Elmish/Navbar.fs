module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Import
open Fable.Core.JsInterop
open Helpers
open Types

type IOnSubmit<'T> =
  abstract onSubmit: 'T -> unit

let CreateProjectModal: React.ComponentClass<IOnSubmit<string>> =
  importDefault "../../../src/modals/CreateProject"

module Options =
  let [<Literal>] createProject = "Create Project"
  let [<Literal>] loadProject = "Load Project"
  let [<Literal>] saveProject = "Save Project"
  let [<Literal>] unloadProject = "Unload Project"
  let [<Literal>] shutdown = "Shutdown"

let onClick dispatch id _ =
  match id with
  | Options.createProject ->
    (fun d m ->
      let props =
        { new IOnSubmit<string> with
          member __.onSubmit(name) =
            Lib.createProject name
            UpdateModal None |> dispatch }
      from CreateProjectModal props [])
    |> Some |> UpdateModal |> dispatch
  | Options.loadProject -> ()
  | Options.saveProject -> ()
  | Options.unloadProject -> ()
  | Options.shutdown -> ()
  | o -> failwithf "Unknow navbar option: %s" o

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
        printfn "Cannot read ServiceInfo from ClientContext"
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
