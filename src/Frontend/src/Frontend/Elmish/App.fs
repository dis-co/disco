module Iris.Web.App

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Iris.Web.State
open Iris.Web.Notifications
open System
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers
open Types

importSideEffects "react-grid-layout/css/styles.css"

let ReactGridLayout: obj -> ReactElement = importDefault "react-grid-layout"
let createTestWidget1(id: Guid, name: string): IWidget = importDefault "../../js/widgets/TestWidget1"
let createTestWidget2(id: Guid, name: string): IWidget = importDefault "../../js/widgets/TestWidget2"
let createTestWidget3(id: Guid, name: string): IWidget = importDefault "../../js/widgets/TestWidget3"

initWidgetFactory
  { new IWidgetFactory with
      member __.CreateWidget(id, name) =
        let id = Option.defaultWith (fun () -> Guid.NewGuid()) id
        match name with
        | Widgets.Log -> LogView.createWidget(id)
        | Widgets.GraphView -> GraphView.createWidget(id)
        | Widgets.CuePlayer -> Cues.CuePlayerView.createWidget(id)
        | Widgets.ProjectView -> ProjectView.createWidget(id)
        | Widgets.Cluster -> ClusterView.createWidget(id)
        | Widgets.Clients -> ClientsView.createWidget(id)
        | Widgets.Sessions -> SessionsView.createWidget(id)
        | Widgets.PinMapping -> PinMappingView.createWidget(id)
        | Widgets.InspectorView -> InspectorView.createWidget(id)
        | Widgets.Test1 -> createTestWidget1(id, name)
        | Widgets.Test2 -> createTestWidget2(id, name)
        | Widgets.Test3 -> createTestWidget3(id, name)
        | _ -> failwithf "Widget %s is not currently supported" name
  }

let [<Literal>] private mdir = "../../js/modals/"

let makeModal dispatch (modal: IModal): React.ReactElement =
  let data, com =
    match modal with
    | :? Modal.AddMember              -> None,                 importDefault (mdir+"AddMember")
    | :? Modal.CreateProject          -> None,                 importDefault (mdir+"CreateProject")
    | :? Modal.LoadProject            -> None,                 importDefault (mdir+"LoadProject")
    | :? Modal.Login as m             -> Some(box m.Project),  importDefault (mdir+"Login")
    | :? Modal.ProjectConfig as m     -> Some(box m.Sites),    importDefault (mdir+"ProjectConfig")
    | :? Modal.AvailableProjects as m -> Some(box m.Projects), importDefault (mdir+"AvailableProjects")
    | _ -> failwithf "Cannot render unknown modal %A" modal
  let props =
    createObj ["data" ==> data
               "onSubmit" ==> fun res ->
                CloseModal(modal, Choice1Of2 res) |> dispatch ]
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

module Values =
  let [<Literal>] gridLayoutColumns = 20
  let [<Literal>] gridLayoutWidth = 1600
  let [<Literal>] gridLayoutRowHeight = 30
  let [<Literal>] jqueryLayoutWestSize = 200

module TabsView =
  let root dispatch (model: Model) =
    div [ Class "iris-tab-container" ] [
      div [Class "tabs is-boxed"] [
        ul [] [
          li [Class "is-active"] [a [] [str "Workspace"]]
        ]
      ]
      div [Class "iris-tab-body"] [
        fn ReactGridLayout %[
          "className" => "iris-workspace"
          "cols" => Values.gridLayoutColumns
          "rowHeight" => Values.gridLayoutRowHeight
          "width" => Values.gridLayoutWidth
          "verticalCompact" => false
          "draggableHandle" => ".iris-draggable-handle"
          "layout" => model.layout
          "onLayoutChange" => (UpdateLayout >> dispatch)
        ] [
          for KeyValue(id, widget) in model.widgets do
            if id <> widget.Id then
              printfn "DIFFERENT: %O %O" id widget.Id
            yield div [Key (string widget.Id)] [widget.Render(dispatch, model)]
        ]
      ]
      model.modal |> Option.map (makeModal dispatch) |> opt
    ]

let view dispatch (model: Model) =
  let mutable i = 0
  div [Id "app"] [
    com<Navbar.View,_,_> { Dispatch = dispatch; Model = model } []
    div [Id "app-content"] [
      div [Id "ui-layout-container"] [
        div [Class "ui-layout-west"] [
          PanelLeftView.root dispatch ()
        ]
        div [Class "ui-layout-center"] [
          TabsView.root dispatch model
        ]
      ]
    ]
    footer [Id "app-footer"] [
      div [Class "container"] [
        p [] [
          str "© 2017 - "
          a [Href "http://nsynk.de/"] [str "NSYNK Gesellschaft für Kunst und Technik GmbH"]
        ]
      ]
    ]

    Notifications.root
  ]

let root model dispatch =
  hookViewWith
    equalsRef
    (fun () ->
      !!jQuery("#ui-layout-container")
        ?layout(%["west__size" ==> Values.jqueryLayoutWestSize]))
    (fun () -> printfn "App unmounted!")
    (view dispatch)
    model

open Elmish.React
open Elmish.Debug

let init() =
  Program.mkProgram init update root
  // |> Program.toNavigable (parseHash pageParser) urlUpdate
  |> Program.withReact "app-container"
  //#if DEBUG
  //|> Program.withDebugger
  // #endif
  |> Program.run
