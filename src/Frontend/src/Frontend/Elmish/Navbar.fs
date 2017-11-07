module Iris.Web.Navbar

open Fable.Helpers.React
open Fable.Import
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Iris.Core
open Helpers
open Types
open State

module private ProjectMenu =
  let [<Literal>] create = "Create"
  let [<Literal>] load = "Load"
  let [<Literal>] save = "Save"
  let [<Literal>] unload = "Unload"
  let [<Literal>] shutdown = "Shutdown"

open Fable.Helpers.React.Props

let private navbarItem cb opt =
  a [Class "navbar-item"; OnClick (cb opt)] [str opt]

let private projectMenu dispatch (model: Model) =
  let onClick dispatch id _ =
    let start f msg =
      f() |> Promise.iter (fun () -> printfn "%s" msg)
    match id with
    | ProjectMenu.create ->
      Modal.CreateProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.load ->
      Modal.LoadProject() :> IModal |> OpenModal |> dispatch
    | ProjectMenu.save ->
      start Lib.saveProject "Project has been saved"
    | ProjectMenu.unload ->
      start Lib.unloadProject "Project has been unloaded"
    | ProjectMenu.shutdown ->
      start Lib.shutdown "Iris has been shut down"
    | other ->
      failwithf "Unknow navbar option: %s" other
  div [Class "navbar-item has-dropdown is-hoverable"] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
    ] [str "Project"]
    div [Class "navbar-dropdown"] [
      navbarItem (onClick dispatch) ProjectMenu.create
      navbarItem (onClick dispatch) ProjectMenu.load
      navbarItem (onClick dispatch) ProjectMenu.save
      navbarItem (onClick dispatch) ProjectMenu.unload
      navbarItem (onClick dispatch) ProjectMenu.shutdown
    ]
  ]

module private ConfigMenu =
  let [<Literal>] rightClick = "Use right click"

let private configMenu dispatch (model:Model) =
  div [Class "navbar-item has-dropdown is-hoverable"] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
    ] [str "Config"]
    div [Class "navbar-dropdown"] [
      div [
        Class "navbar-item field"
      ] [
        div [ Class "control"; Style [ MarginRight "5px" ] ] [
          input [
            Type "checkbox"
            Checked model.userConfig.useRightClick
            OnClick (fun _ ->
              { model.userConfig with useRightClick = not model.userConfig.useRightClick }
              |> UpdateUserConfig |> dispatch)
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

module private EditMenu =
  let [<Literal>] resetDirty = "Reset Dirty"

let private editMenu dispatch (model:Model) =
  let onClick dispatch id _ =
    let start f msg =
      f() |> Promise.iter (fun () -> printfn "%s" msg)
    match id with
    | EditMenu.resetDirty -> Option.iter Lib.resetDirty model.state
    | _ -> ()
  div [Class "navbar-item has-dropdown is-hoverable"] [
    a [
      Class "navbar-link"
      Style [!!("fontSize", "14px")]
    ] [str "Edit"]
    div [Class "navbar-dropdown"] [
      div [
        Class "navbar-item"
      ] [
        navbarItem (onClick dispatch) EditMenu.resetDirty
      ]
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
            projectMenu this.props.Dispatch this.props.Model
            editMenu this.props.Dispatch this.props.Model
            configMenu this.props.Dispatch this.props.Model
          ]
          div [Class "navbar-end"] [
            div [Class "navbar-item"] [
              str(sprintf "Iris v%s - build %s" version buildNumber)
            ]
          ]
        ]
      ]
    ]
