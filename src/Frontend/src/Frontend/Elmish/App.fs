module Iris.Web.App

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Iris.Web.Core
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
        | Widgets.Players -> PlayerListView.createWidget(id)
        | Widgets.CuePlayer -> Cues.CuePlayerView.createWidget(id)
        | Widgets.CueLists -> CueListView.createWidget(id)
        | Widgets.Cues -> CuesView.createWidget(id)
        | Widgets.ProjectView -> ProjectView.createWidget(id)
        | Widgets.Cluster -> ClusterView.createWidget(id)
        | Widgets.Clients -> ClientsView.createWidget(id)
        | Widgets.Sessions -> SessionsView.createWidget(id)
        | Widgets.PinMapping -> PinMappingView.createWidget(id)
        | Widgets.Test1 -> createTestWidget1(id, name)
        | Widgets.Test2 -> createTestWidget2(id, name)
        | Widgets.Test3 -> createTestWidget3(id, name)
        | _ -> failwithf "Widget %s is not currently supported" name
  }

module Values =
  let [<Literal>] gridLayoutColumns = 20
  let [<Literal>] gridLayoutWidth = 1600
  let [<Literal>] gridLayoutRowHeight = 30

  let inspectorPanelSize = 350

module TabsView =
  /// this callback receives the list of widgets (untyped) from the grid layout
  /// and saves it back to local storage
  let private updateLayout dispatch model widgets =
    Layout.setWidgets widgets model.layout
    |> UpdateLayout
    |> dispatch

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
          "layout" => model.layout.Widgets
          "onLayoutChange" => updateLayout dispatch model
        ] [
          for KeyValue(id, widget) in model.widgets do
            if id <> widget.Id then
              printfn "DIFFERENT: %O %O" id widget.Id
            yield div [Key (string widget.Id)] [widget.Render(dispatch, model)]
        ]
      ]
      model.modal |> Option.map (Modal.show dispatch) |> opt
    ]

let view dispatch (model: Model) =
  let mutable i = 0
  div [Id "app"] [
    com<Navbar.View,_,_> { Dispatch = dispatch; Model = model } []
    div [Id "app-content"] [
      div [Id "ui-layout-container"] [
        div [Class "ui-layout-center"] [
          TabsView.root dispatch model
        ]
        div [Class "ui-layout-east"] [
          InspectorView.render dispatch model
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

let initializeLayout model dispatch () =
  let onOpen name =
    if name = "east" then
      InspectorAction.Open
      |> UpdateInspector
      |> dispatch

  let onClose name =
    if name = "east" then
      InspectorAction.Close
      |> UpdateInspector
      |> dispatch

  let onResize name _ state =
    if name = "east" then
      !!state?outerWidth
      |> InspectorAction.Resize
      |> UpdateInspector
      |> dispatch

  let options =
    %[ "east__size"       ==> model.layout.Inspector.Size
       "east__initClosed" ==> not model.layout.Inspector.IsOpen
       "onopen"           ==> onOpen
       "onclose"          ==> onClose
       "onresize_end"     ==> onResize ]

  /// initialize the north-east-south-west layout
  !!jQuery("#ui-layout-container")?layout(options)

let root model dispatch =
  hookViewWith
    equalsRef
    (initializeLayout model dispatch)
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
