module Iris.Web.State

open Elmish
open Elmish.Browser.Navigation
open Elmish.Browser.UrlParser
open Fable.Import.Browser

type Msg =
  | Msg

type Model = {
    name: string
  }

let init() =
    { name = "World" }, []

let update msg model =
  match msg with
  | Msg -> model, []
