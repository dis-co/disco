[<RequireQualifiedAccess>]
module Iris.Web.ContextMenu

open System
open Iris.Core
open Iris.Web.Core
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Iris.Web
open Types
open Helpers

type MenuCommand = unit -> unit

let private withDelay (f) =
  async {
    do! Async.Sleep(20)
    do f()
  }
  |> Async.StartImmediate

let private toItem close (name:string, command: MenuCommand) =
  a [
    Href "#"
    Class "dropdown-item"
    OnClick (fun _ -> command(); withDelay close)
  ] [ str name ]

let private toItems onOpen options =
  div [ Class "dropdown-content" ]
    (List.map (toItem onOpen) options)

let create active (onOpen:MenuCommand) (options: (string * MenuCommand) list) =
  div [
    classList [
      "pull-right dropdown", true
      "is-active", active
    ]
  ] [
    div [ Class "dropdown-trigger" ] [
      button [ Class "iris-button"; OnClick (fun _ -> onOpen()) ] [
        span [ Class "icon is-small" ] [
          i [ Class "fa fa-cog" ] []
        ]
      ]
    ]
    div [ Class "dropdown-menu"; Role "menu" ] [
      toItems onOpen options
    ]
  ]
