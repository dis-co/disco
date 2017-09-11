module Iris.Web.PinMappingView

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

let titleBar dispatch (model: Model) =
  button [
    Class "iris-button"
    OnClick(fun _ ->
      printfn "TODO: Add PinMapping")
  ] [str "Add"]

let body dispatch (model: Model) =
  match model.state with
  | None -> table [Class "iris-table"] []
  | Some state ->
    let config = state.Project.Config
    let members =
      config.ActiveSite |> Option.bind (fun activeSite ->
        config.Sites |> Seq.tryFind (fun site -> site.Id = activeSite))
      |> Option.map (fun site -> site.Members)
      |> Option.defaultValue Map.empty
    table [Class "iris-table"] [
      thead [] [
        tr [] [
          th [Class "width-20"; padding5()] [str "Source"]
          th [Class "width-75"] [str "Sinks"]
          th [Class "width-5"] []
        ]
      ]
      tbody [] (
        members |> Seq.map (fun kv ->
          let node = kv.Value
          tr [Key (string kv.Key)] [
            td [Class "width-20"; padding5AndTopBorder()] [
              // Source pin
            ]
            td [Class "width-75"; topBorder()] [
              // Sink pins
            ]
            td [Class "width-5"; topBorder()] [
              button [
                Class "iris-button iris-icon icon-close"
                OnClick (fun ev ->
                  ev.stopPropagation()
                  printfn "TODO: Remove PinMapping")
              ] []
            ]
          ]
        ) |> Seq.toList
      )
    ]

let createWidget(id: System.Guid) =
  { new IWidget with
    member __.Id = id
    member __.Name = Types.Widgets.PinMappings
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
            equalsRef s1.Project s2.Project
          | None, None -> true
          | _ -> false)
        (widget id this.Name (Some titleBar) body dispatch)
        model
  }
