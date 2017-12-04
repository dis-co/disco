module Iris.Web.SessionsView

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
open Helpers
open State
open Types

let inline padding5() =
  Style [PaddingLeft "5px"]

let inline topBorder() =
  Style [BorderTop "1px solid lightgray"]

let inline padding5AndTopBorder() =
  Style [PaddingLeft "5px"; BorderTop "1px solid lightgray"]

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let config = state.Project.Config
    div [ Class "iris-sessions" ] [
      table [Class "iris-table"] [
        thead [] [
          tr [] [
            th [Class "width-20"; padding5()] [str "User"]
            th [Class "width-20"; padding5()] [str "Id"]
            th [Class "width-15"] [str "IP"]
            th [Class "width-25"] [str "User Agent"]
          ]
        ]
        tbody [] (
          state.Sessions
          |> Seq.map (function
            KeyValue(id,session) ->
              tr [Key (string id)] [
                td [Class "width-20"; padding5AndTopBorder()] [
                  span [Class "iris-output iris-icon icon-host"] [
                    str "Admin"
                    span [Class "iris-icon icon-bull iris-status-on"] []
                  ]
                ]
                td [Class "width-20"; topBorder()] [
                  str (session.Id.Prefix())
                ]
                td [Class "width-15"; topBorder()] [str (string session.IpAddress)]
                td [Class "width-25"; topBorder()] [str (string session.UserAgent)]
              ])
          |> Seq.toList
        )
      ]
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Sessions
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4
        minH = 1 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.Sessions s2.Sessions
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
