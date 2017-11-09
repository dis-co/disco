module Iris.Web.CuesView

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

let private updateName (cue:Cue) (value:string) =
  cue
  |> Cue.setName (name value)
  |> UpdateCue
  |> ClientContext.Singleton.Post

let private deleteButton dispatch (cue:Cue) =
  button [
    Class "iris-button iris-icon icon-close"
    OnClick (fun ev ->
      // Don't stop propagation to allow the item to be selected
      cue
      |> RemoveCue
      |> ClientContext.Singleton.Post)
  ] []

let titleBar _ _ =
  button [
    Class "iris-button"
    OnClick(fun _ ->
      Cue.create "Untitled" Array.empty
      |> AddCue
      |> ClientContext.Singleton.Post)
    ] [str "Add Cue"]

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let cues = state.Cues |> Map.toList |> List.map snd |> List.sortBy Cue.name
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Id"]
          th [Class "width-15"] [str "Name"]
          th [Class "width-5"] []
        ]
      ]
      tbody [] (
        cues |> Seq.map (fun cue ->
          tr [Key (string cue.Id)] [
            td [Class "width-20";padding5AndTopBorder()] [
              str (Id.prefix cue.Id)
            ]
            td [Class "width-15"; topBorder()] [
                /// provide inline editing capabilities for the CuePlayer Name field
                Editable.string (string cue.Name) (updateName cue)
            ]
            td [Class "width-5"; padding5()] [
              deleteButton dispatch cue
            ]
          ]
        ) |> Seq.toList
      )
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.Cues
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
            equalsRef s1.Cues s2.Cues
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
