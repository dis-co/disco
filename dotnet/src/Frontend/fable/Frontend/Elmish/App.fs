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

module PanelLeft =
  let view() = div [] []

module Tabs =
  let view() = div [] []

let view (model: Model) dispatch =
  div [Id "app"] [
    Navbar.view()
    div [Id "app-content"] [
      div [Class "ui-layout-west"] [
        PanelLeft.view()
      ]
      div [Class "ui-layout-center"] [
        Tabs.view()
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
    (fun x y -> obj.ReferenceEquals(x, y))
    (fun () -> printfn "Mounted!")
    (fun () -> printfn "Unmounted!")
    (fun model -> view model dispatch)
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
