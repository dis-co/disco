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

module CueListInspector =

  let private renderCue dispatch (model: Model) (ref: CueReference) =
    match model.state with
    | None -> li [] []
    | Some state ->
      match Map.tryFind ref.CueId state.Cues with
      | None ->
        li [] [
          ul [] [
            li [] [ str (string ref.CueId + " (orphaned)") ]
            li [] [
              table [ Class "iris-table" ] [
                tbody [] [
                  tr [] [
                    td [] [ str "Duration" ]
                    td [] [ str (string ref.Duration) ]
                  ]
                  tr [] [
                    td [] [ str "AutoFollow" ]
                    td [] [ str (string ref.AutoFollow) ]
                  ]
                  tr [] [
                    td [] [ str "Prewait" ]
                    td [] [ str (string ref.Prewait) ]
                  ]
                ]
              ]
            ]
          ]
        ]
      | Some cue ->
        li [] [
          ul [] [
            li [] [
              strong [] [ str "Cue:" ]
              Common.link (string cue.Name) (fun () -> Select.cue dispatch cue)
            ]
            li [] [
              table [ Class "iris-table" ] [
                tbody [] [
                  tr [] [
                    td [] [ str "Duration" ]
                    td [] [ str (string ref.Duration) ]
                  ]
                  tr [] [
                    td [] [ str "AutoFollow" ]
                    td [] [ str (string ref.AutoFollow) ]
                  ]
                  tr [] [
                    td [] [ str "Prewait" ]
                    td [] [ str (string ref.Prewait) ]
                  ]
                ]
              ]
            ]
          ]
        ]

  let private renderCues dispatch (model: Model) (cuegroup: CueGroup) =
    cuegroup.CueRefs
    |> Array.map (renderCue dispatch model)
    |> List.ofArray
    |> ul []

  let private renderGroup dispatch (model: Model) (cuegroup: CueGroup) =
    ul [] [
      li [] [ strong [] [ str (string cuegroup.Name) ] ]
      li [] [ renderCues dispatch model cuegroup ]
    ]

  let private renderItems tag dispatch model (cuelist: CueList) =
    cuelist.Items
    |> Array.map (renderGroup dispatch model)
    |> List.ofArray
    |> Common.row tag

  let private renderPlayer dispatch model (player: CuePlayer) =
    li [] [
      Common.link
        (string player.Name)
        (fun () -> Select.player dispatch player)
    ]

  let private renderPlayers tag dispatch (model: Model) (cuelist: CueList) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.CuePlayers
      |> Map.filter (fun _ -> CuePlayer.contains cuelist.Id)
      |> Map.toList
      |> List.map (snd >> renderPlayer dispatch model)
      |> fun players -> Common.row tag [ ul [] players ]

  let render dispatch (model: Model) (cuelist: CueListId) =
    match model.state with
    | None ->
      Common.render dispatch model "Cue List" [
        str (string cuelist + " (orphaned)")
      ]
    | Some state ->
      match Map.tryFind cuelist state.CueLists with
      | None ->
        Common.render dispatch model "Cue List" [
          str (string cuelist + " (orphaned)")
        ]
      | Some cuelist ->
        Common.render dispatch model "Cue List" [
          Common.stringRow "Id"     (string cuelist.Id)
          Common.stringRow "Name"   (string cuelist.Name)
          renderItems      "Items"   dispatch model cuelist
          renderPlayers    "Players" dispatch model cuelist
        ]
