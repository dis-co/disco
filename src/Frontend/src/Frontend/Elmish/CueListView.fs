module Iris.Web.CueListView

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

let private updateName (cueList:CueList) (value:string) =
  cueList
  |> CueList.setName (name value)
  |> UpdateCueList
  |> ClientContext.Singleton.Post

let private deleteButton dispatch (cueList:CueList) =
  button [
    Class "iris-button iris-icon icon-close"
    OnClick (fun ev ->
      // Don't stop propagation to allow the item to be selected
      cueList
      |> RemoveCueList
      |> ClientContext.Singleton.Post)
  ] []

let titleBar _ _ =
  button [
    Class "iris-button"
    OnClick(fun _ ->
      CueList.create "Untitled" Array.empty
      |> AddCueList
      |> ClientContext.Singleton.Post)
    ] [str "Add CueList"]

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let cueLists = state.CueLists |> Map.toList |> List.map snd
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Id"]
          th [Class "width-15"] [str "Name"]
          th [Class "width-5"] []
        ]
      ]
      tbody [] (
        cueLists |> Seq.map (fun cueList ->
          tr [Key (string cueList.Id)] [
            td [Class "width-20";padding5AndTopBorder()] [
              str (Id.prefix cueList.Id)
            ]
            td [Class "width-15"; topBorder()] [
                /// provide inline editing capabilities for the CuePlayer Name field
                Editable.string (string cueList.Name) (updateName cueList)
            ]
            td [Class "width-5"; padding5()] [
              deleteButton dispatch cueList
            ]
          ]
        ) |> Seq.toList
      )
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.CueLists
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
            equalsRef s1.CueLists s2.CueLists
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
