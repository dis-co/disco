module Iris.Web.PanelLeft

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core.JsInterop
open Elmish.React
open Helpers

let onClick id _ =
    printfn "PanelLeft clicked %i" id

let card key letter title text =
    div [
        Key (string key)
        Class "iris-panel-left-child"
        OnClick (onClick key)
    ] [
        div [] [str letter]
        div [] [
            p [] [strong [] [str title]]
            p [] [str text]
        ]
    ]

let render () =
    div [Class "iris-panel-left"] [
        card 0 "L" "LOG" "Cluster Settings"
        card 1 "G" "Graph View" "Cluster Settings"
        card 2 "C" "Cue Player" "Cluster Settings"
        card 3 "P" "Project View" "Cluster Settings"
        card 4 "T" "Test Widget" "Cluster Settings"
        card 5 "R" "Cluster" "Cluster Settings"
        card 6 "D" "Discovery" "Cluster Settings"
        card 7 "H" "Unassigned Hosts" "Cluster Settings"
        card 8 "R" "Remotter" "Cluster Settings"
        card 9 "S" "Project Settings" "Cluster Settings"
        card 10 "L" "Library" "Graph View"
        card 11 "P" "Project Overview (Big)" "Cluster Settings"
    ]

let view () =
  lazyViewWith
    (fun x y -> obj.ReferenceEquals(x, y))
    (fun () -> render())
    ()
