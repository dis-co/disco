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

let render dispatch () =
  div [Class "iris-panel-left"] [
    card dispatch Widgets.Log           "L" "Log View"
    card dispatch Widgets.InspectorView "I" "Inspector"
    card dispatch Widgets.GraphView     "G" "Graph View"
    card dispatch Widgets.Players       "P" "Players"
    card dispatch Widgets.Cues          "C" "Cues"
    card dispatch Widgets.CueLists      "C" "Cue Lists"
    card dispatch Widgets.PinMapping    "M" "Pin Mappings"
    card dispatch Widgets.ProjectView   "P" "Project Overview"
    card dispatch Widgets.Cluster       "R" "Cluster Settings"
    card dispatch Widgets.Clients       "A" "Clients"
    card dispatch Widgets.Sessions      "S" "Sessions"
    card dispatch Widgets.Test1         "T" "Test Widget 1"
    card dispatch Widgets.Test2         "T" "Test Widget 2"
    card dispatch Widgets.Test3         "T" "Test Widget 3"
  ]

let root dispatch () =
  lazyViewWith
    (fun x y -> obj.ReferenceEquals(x, y))
    (fun () -> render dispatch ())
    ()
