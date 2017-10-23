module Iris.Web.PlayerListView

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

let private viewButton dispatch (player:CuePlayer) =
  button [
    Class "iris-button iris-icon"
    OnClick (fun ev ->
      // Don't stop propagation to allow the item to be selected
      let guid = player.Id.Guid
      let widget = getWidgetFactory().CreateWidget(Some guid, Widgets.CuePlayer)
      AddWidget(guid, widget) |> dispatch
    )
  ] [
    i [
      Class "fa fa-eye fa-lg"
      Style [
        FontSize "1.33333333em"
      ]
    ] []
  ]

let private deleteButton dispatch (player:CuePlayer) =
  button [
    Class "iris-button iris-icon icon-close"
    OnClick (fun ev ->
      // Don't stop propagation to allow the item to be selected
      /// let guid = player.Id.Guid
      /// let widget = getWidgetFactory().CreateWidget(Some guid, Widgets.CuePlayer)
      /// AddWidget(guid, widget) |> dispatch
      player
      |> RemoveCuePlayer
      |> ClientContext.Singleton.Post
      player.Id.Guid
      |> RemoveWidget
      |> dispatch
    )
  ] []

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let config = state.Project.Config
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Name"]
          th [Class "width-20"; padding5()] [str "Cue List"]
          th [Class "width-15"; padding5()] [str "Locked"]
          th [Class "width-15"; padding5()] [str "Active"]
          th [Class "width-25"] []
        ]
      ]
      tbody [] (
        state.CuePlayers
        |> Seq.map (function
          KeyValue(id,player) ->
            let cueList =
              match player.CueListId with
              | Some cueList ->
                model.state
                |> Option.map State.cueLists
                |> Option.bind (Map.tryFind cueList)
                |> Option.map (CueList.name >> string)
                |> Option.defaultValue (cueList |> Id.prefix |> String.format "{0} (orphaned)")
              | None -> "--"
            tr [Key (string id)] [
              td [Class "width-20"; padding5AndTopBorder()] [
                str (string player.Name)
              ]
              td [Class "width-20"; padding5AndTopBorder()] [
                str cueList
              ]
              td [Class "width-15"; padding5AndTopBorder()] [
                str (string player.Locked)
              ]
              td [Class "width-15"; padding5AndTopBorder()] [
                str (string true)
              ]
              td [Class "width-25"; padding5() ] [
                viewButton dispatch player
                deleteButton dispatch player
              ]
            ])
        |> Seq.toList
      )
    ]

let titleBar dispatch model =
  div [] [
    button [
      Class "iris-button"
      OnClick (fun _ ->
        None
        |> CuePlayer.create (name "Player")
        |> AddCuePlayer
        |> ClientContext.Singleton.Post
      )
    ] [str "Add Player"]
  ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Players
    member __.InitialLayout =
      { i = id; ``static`` = false
        x = 0; y = 0
        w = 8; h = 5
        minW = 4; maxW = 10
        minH = 1; maxH = 10 }
    member this.Render(dispatch, model) =
      lazyViewWith
        (fun m1 m2 ->
          match m1.state, m2.state with
          | Some s1, Some s2 ->
            equalsRef s1.CuePlayers s2.CuePlayers
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
