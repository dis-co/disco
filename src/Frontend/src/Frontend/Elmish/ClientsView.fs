module Disco.Web.ClientsView

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
open Disco.Core
open Disco.Web.Core
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
  | None -> table [Class "disco-table"] []
  | Some state ->
    let config = state.Project.Config
    div [ Class "disco-clients" ] [
      table [Class "disco-table"] [
        thead [] [
          tr [] [
            th [Class "width-20"; padding5()] [str "Name"]
            th [Class "width-15"] [str "IP"]
            th [Class "width-25"] [str "Port"]
            th [Class "width-15"] [str "Role"]
            th [Class "width-15"] [str "Status"]
          ]
        ]
        tbody [] (
          state.Clients
          |> Seq.map (function
            KeyValue(id,client) ->
              tr [Key (string id)] [
                td [
                  Class "width-20"
                  padding5AndTopBorder()
                  OnClick (fun _ -> Select.client dispatch client)
                  Style [ Cursor "pointer" ]
                ] [
                  span [Class "disco-output disco-icon icon-host"] [
                    str (unwrap client.Name)
                    span [Class "disco-icon icon-bull disco-status-on"] []
                  ]
                ]
                td [Class "width-15"; topBorder()] [str (string client.IpAddress)]
                td [Class "width-25"; topBorder()] [str (string client.Port)]
                td [Class "width-15"; topBorder()] [str (string client.Role)]
                td [Class "width-15"; topBorder()] [str (string client.Status)]
              ])
          |> Seq.toList
        )
      ]
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Clients
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
            equalsRef s1.Clients s2.Clients
          | None, None -> true
          | _ -> false)
        (widget id this.Name None body dispatch)
        model
  }
