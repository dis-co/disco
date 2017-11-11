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

module TabsView =
  /// this callback receives the list of widgets (untyped) from the grid layout
  /// and saves it back to local storage
  let private updateLayout dispatch widgets =
    widgets
    |> UpdateLayout
    |> dispatch

  let private contextMenu dispatch selected tab =
    button [
      classList [
        "iris-button", true
        "inactive", tab.Id <> selected
      ]
      Style [ Visibility (if tab.Removable then "visible" else "hidden") ]
      OnClick
        (fun e ->
          e.stopPropagation()
          tab.Id
          |> TabAction.RemoveTab
          |> UpdateTabs
                |> dispatch)
    ] [
      i [ Class "fa fa-times" ] []
    ]

  let private addTab dispatch =
    li [] [
      button [
        Class "button"
        Title "Add new Tab"
        OnClick (fun _ -> TabAction.AddTab |> UpdateTabs |> dispatch)
      ] [
        i [ Class "fa fa-plus" ] []
      ]
    ]

  let private renderTab dispatch selected tab =
    li [
      classList [
        "is-active", selected = tab.Id
      ]
      OnClick (fun _ -> tab.Id |> TabAction.SelectTab |> UpdateTabs |> dispatch)
    ] [
      a [] [
        str tab.Name
        contextMenu dispatch selected tab
      ]
    ]

  let private renderTabs dispatch model =
    let tabs =
      model.layout
      |> Layout.tabs
      |> List.ofArray
      |> List.map (renderTab dispatch model.layout.Selected)
    ul [] (addTab dispatch :: tabs)

  let root dispatch (model: Model) =
    div [ Class "iris-tab-container" ] [
      div [Class "tabs is-boxed"] [
        renderTabs dispatch model
      ]
      div [Class "iris-tab-body"] [
        fn ReactGridLayout %[
          "className" => "iris-workspace"
          "cols" => Layout.gridColumns
          "rowHeight" => Layout.gridRowHeight
          "width" => Layout.gridWidth()
          "verticalCompact" => false
          "draggableHandle" => ".iris-draggable-handle"
          "layout" => Layout.widgetLayouts model.layout
          "onLayoutChange" => updateLayout dispatch
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
