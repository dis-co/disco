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

let private updateName (player:CuePlayer) (value:string) =
  player
  |> CuePlayer.setName (name value)
  |> UpdateCuePlayer
  |> ClientContext.Singleton.Post

let private updateCueList (player:CuePlayer) = function
  | Some id ->
    match IrisId.TryParse id with
    | Left _ ->
      CuePlayer.unsetCueList player
      |> UpdateCuePlayer
      |> ClientContext.Singleton.Post
    | Right id ->
      CuePlayer.setCueList id player
      |> UpdateCuePlayer
      |> ClientContext.Singleton.Post
  | None ->
    CuePlayer.unsetCueList player
    |> UpdateCuePlayer
    |> ClientContext.Singleton.Post

let private boolButton value f =
  let active = if value then "pressed" else ""
  div [
    Style [
      Height "15px"
      Width "15px"
    ]
    Class ("iris-button " + active)
    OnClick (fun _ -> f (not value))
  ] []

let private renderNameInput (player:CuePlayer) =
  Editable.string (string player.Name) (updateName player)

let private renderCueListDropdown (state:State) (player:CuePlayer) =
  let cueList =
    match player.CueListId with
    | Some cueList ->
      /// try find the currently used Cue List for this player
      state
      |> State.cueLists
      |> Map.tryFind cueList
      |> Option.map (CueList.name >> string)
      |> Option.defaultValue (cueList |> Id.prefix |> String.format "{0} (orphaned)")
    | None -> "--"
  if player.Locked
  then str cueList
  else
    let cueLists =
      state.CueLists
      |> Map.toArray
      |> Array.map (fun (id,cueList) -> string cueList.Name, string id)
    Editable.dropdown
      cueList
      (Option.map string player.CueListId)
      cueLists
      (updateCueList player)

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    /// all name * id pairs of existing Cue Lists for use in the dropdown menu
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
            tr [Key (string id)] [
              td [
                Class "width-20"
                padding5AndTopBorder()
              ] [
                /// provide inline editing capabilities for the CuePlayer Name field
                renderNameInput player
              ]
              td [Class "width-20"; padding5AndTopBorder()] [
                /// provies inline selection method for the Cue List used by the player
                renderCueListDropdown state player
              ]
              td [Class "width-15"; padding5AndTopBorder()] [
                boolButton
                  player.Locked
                  (flip CuePlayer.setLocked player
                   >> UpdateCuePlayer
                   >> ClientContext.Singleton.Post)
              ]
              td [Class "width-15"; padding5AndTopBorder()] [
                boolButton
                  player.Active
                  (flip CuePlayer.setActive player
                   >> UpdateCuePlayer
                   >> ClientContext.Singleton.Post)
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
        |> CuePlayer.create "Player"
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
