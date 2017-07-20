module Iris.Web.App

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Import.Browser
open Iris.Web.State

open Fable.Helpers.React
open Fable.Helpers.React.Props

let inline Class x = ClassName x

let root (model: Model) dispatch =
  div [Id "app"] [
    Navbar.view()
  ]
  // h1 [Class "title is-1"] [str (sprintf "Bye %s!" model.name)]

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
