module Iris.Web.PanelLeftView

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Elmish.React
open Helpers
open State
open System
open Types

let onClick dispatch name _ =
  let widget = getWidgetFactory().CreateWidget(None, name)
  AddWidget(widget.Id, widget) |> dispatch

let card dispatch name letter text =
  div [
    Key name
    Class "iris-panel-left-child"
    OnClick (onClick dispatch name)
  ] [
    div [] [str letter]
    div [] [
      p [] [strong [] [str name]]
      p [] [str text]
    ]
  ]

let render dispatch (model:Model) =
  div [Class "iris-panel-left"] [
    InspectorView.render dispatch model
  ]

let root dispatch (model:Model) =
  lazyViewWith
    (fun m1 m2 -> m1.history = m2.history)
    (render dispatch)
    model
