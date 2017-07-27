module Iris.Web.App

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Iris.Web.State

open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Helpers

let ReactGridLayout: obj -> ReactElement = importDefault "react-grid-layout"

module Values =
  let [<Literal>] gridLayoutColumns = 20
  let [<Literal>] gridLayoutWidth = 1600
  let [<Literal>] gridLayoutRowHeight = 30
  let [<Literal>] jqueryLayoutWestSize = 200

module Tabs =
  let view dispatch (model: Model) =
    div [Class "iris-tab-container"] [
      div [Class "tabs is-boxed"] [
        ul [] [
          li [Class "is-active"] [a [] [str "Workspace"]]
          // li [] [a [] [str "Foo"]]
          // li [] [a [] [str "Bar"]]
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
          "layout" => (
            model.widgets
            |> Seq.map (fun (KeyValue(_,widget)) -> widget.InitialLayout)
            |> Seq.toArray
          )
          "onLayoutChange" => fun layout ->
            // printfn "Layout Change: %A" layout
            ()
        ] [
          for KeyValue(id,widget) in model.widgets do
            yield widget.Render(id, dispatch, model)
        ]
      ]
    ]

let view dispatch (model: Model) =
  div [Id "app"] [
    Navbar.view()
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

// App
Program.mkProgram init update root
// |> Program.toNavigable (parseHash pageParser) urlUpdate
|> Program.withReact "app-container"
// #if DEBUG
// |> Program.withDebugger
// #endif
|> Program.run
