namespace Iris.Web.Inspectors

open System
open System.Collections.Generic
open Fable.Import
open Fable.Import.React
open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.Core
open Fable.Core.JsInterop
open Fable.PowerPack
open Elmish.React
open Iris.Core
open Iris.Web.Core
open Iris.Web.Helpers
open Iris.Web.Types
open State

module Common =

  let link (content: string) f =
    div [
      Class "iris-link"
      OnClick (fun _ -> f())
    ] [ str content ]

  let activeLink (content: string) f =
    div [
      Class "iris-link iris-link-active"
      OnClick (fun _ -> f())
    ] [ str content ]

  let makeLink dispatch (history: InspectorHistory) (idx: int) (content: string) =
    let link =
      let inverted = abs (history.previous.Length - 1 - idx)
      let selected = abs (history.index - (history.previous.Length - 1))
      if idx = selected
      then
        activeLink
          content
          (fun () -> Navigate.set inverted dispatch)
      else
        link
          content
          (fun () -> Navigate.set inverted dispatch)
    if idx < history.previous.Length - 1
    then [link; str ">"]
    else [link]

  let breadcrumb dispatch (history: InspectorHistory) (idx: int) (selected: InspectorSelection) =
    let makeLink = makeLink dispatch history idx
    let content =
      match selected with
      | InspectorSelection.Pin (name,client,pin)        -> makeLink (string name)
      | InspectorSelection.PinGroup (name,client,group) -> makeLink (string name)
      | InspectorSelection.Client (name,client)         -> makeLink (string name)
      | InspectorSelection.Member (name,mem)            -> makeLink (string name)
      | InspectorSelection.Cue (name,cue)               -> makeLink (string name)
      | InspectorSelection.CueList (name,cuelist)       -> makeLink (string name)
      | InspectorSelection.Player (name,player)         -> makeLink (string name)
      | InspectorSelection.User (name,user)             -> makeLink (string name)
      | InspectorSelection.Session session              -> makeLink (session.Prefix())
      | InspectorSelection.Mapping mapping              -> makeLink (mapping.Prefix())
      | InspectorSelection.Nothing                      -> [str ""]
    li [ Style [ Display "inline-block" ] ] content

  let bar dispatch (model: Model) =
    let disabled = List.isEmpty model.history.previous
    div [ Class "bar" ] [
      span [ Class "headline" ] [ str "Inspector" ]
      div [ Class "buttons pull-right " ] [
        button [
          Disabled disabled
          OnClick (fun _ -> Navigate.back dispatch)
        ] [ str "<"]
        button [
          Disabled disabled
          OnClick (fun _ -> Navigate.forward dispatch)
        ] [ str ">"]
      ]
    ]

  let row (tag: string) children =
    tr [Key tag] [
      td [] [str tag]
      td [] children
    ]

  let stringRow (tag: string) (value: string) =
    row tag [ str value ]


  let toHeader (idx: int) (title: string) =
    match idx with
    | 0 -> th [ ] [ str title ]
    | _ -> th [ ] [ str title ]

  let tableRow (tag: string) headers children =
    row tag [
      table [Class "iris-table"] [
        thead [] [
          tr [] (List.mapi toHeader headers)
        ]
        tbody [] children
      ]
    ]

  let buttonRow (tag: string) (value: bool) (f: bool -> unit) =
    let active = if value then "pressed" else ""
    row tag [
      div [
        Class ("iris-button " + active)
        OnClick (fun _ -> f (not value))
      ] []
    ]

  let header (title: string) =
    thead [] [
      tr [] [
        th [] [
          str title
        ]
      ]
    ]

  let footer =
    tfoot [] [
      tr [] [
        td [ ] []
        td [ ] []
      ]
    ]

  let render dispatch model (title: string) children =
    div [ Class "iris-inspector" ] [
      bar dispatch model
      table [] [
        header title
        tbody [] children
        footer
      ]
    ]
