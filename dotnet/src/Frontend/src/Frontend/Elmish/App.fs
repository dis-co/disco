module Iris.Web.App

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Iris.Web.State
open System
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers
open Types

importSideEffects "react-grid-layout/css/styles.css"
let ReactGridLayout: obj -> ReactElement = importDefault "react-grid-layout"
let createTestWidget: Guid -> IWidget = importDefault "../../js/widgets/TestWidget"

initWidgetFactory
  { new IWidgetFactory with
      member __.CreateWidget(id, name) =
        let id = Option.defaultWith (fun () -> Guid.NewGuid()) id
        match name with
        | Widgets.Log -> Log.createWidget(id)
        | Widgets.GraphView -> GraphView.createWidget(id)
        | Widgets.CuePlayer -> CuePlayer.createWidget(id)
        | Widgets.ProjectView -> ProjectView.createWidget(id)
        | Widgets.Cluster -> Cluster.createWidget(id)
        | Widgets.Test -> createTestWidget(id)
        | _ -> failwithf "Widget %s is not currently supported" name
  }

module Values =
  let [<Literal>] gridLayoutColumns = 20
  let [<Literal>] gridLayoutWidth = 1600
  let [<Literal>] gridLayoutRowHeight = 30
  let [<Literal>] jqueryLayoutWestSize = 200

module Tabs =
  let modalView dispatch content =
    div [ClassName "modal is-active"] [
      div [
        ClassName "modal-background"
        OnClick (fun _ -> dispatch (UpdateModal None))
      ] []
      div [ClassName "modal-content"] [
        div [ClassName "box"] [content]
      ]
    ]

  let view dispatch (model: Model) =
    div [Class "iris-tab-container"] [
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
      model.modal |> Option.map (snd >> modalView dispatch) |> opt
    ]

let view dispatch (model: Model) =
  div [Id "app"] [
    com<Navbar.View,_,_> { Dispatch = dispatch; Model = model } []
    div [Id "app-content"] [
      div [Id "ui-layout-container"] [
        div [Class "ui-layout-west"] [
          PanelLeft.view dispatch ()
        ]
        div [Class "ui-layout-center"] [
          Tabs.view dispatch model
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
  // #if DEBUG
  // |> Program.withDebugger
  // #endif
  |> Program.run
