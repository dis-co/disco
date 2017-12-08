namespace Disco.Web.Inspectors

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
open Disco.Web.Helpers
open Disco.Web.Types
open State

module CueListInspector =

  let private renderCue dispatch (model: Model) (ref: CueReference) =
    match model.state with
    | None -> li [] []
    | Some state ->
      match Map.tryFind ref.CueId state.Cues with
      | None ->
        div [ Class "columns" ] [
          div [ Class "column is-one-quarter" ] [ str (string ref.CueId + " (orphaned)") ]
          div [ Class "column" ] [
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "Duration" ]
              div [ Class "column" ] [ str (string ref.Duration) ]
            ]
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "AutoFollow" ]
              div [ Class "column" ] [ str (string ref.AutoFollow) ]
            ]
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "Prewait" ]
              div [ Class "column" ] [ str (string ref.Prewait) ]
            ]
          ]
        ]
      | Some cue ->
        div [ Class "columns" ] [
          div [ Class "column is-one-quarter" ] [
            Common.link (string cue.Name) (fun () -> Select.cue dispatch cue)
          ]
          div [ Class "column" ] [
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "Duration" ]
              div [ Class "column" ] [ str (string ref.Duration) ]
            ]
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "AutoFollow" ]
              div [ Class "column" ] [ str (string ref.AutoFollow) ]
            ]
            div [ Class "columns" ] [
              div [ Class "column" ] [ str "Prewait" ]
              div [ Class "column" ] [ str (string ref.Prewait) ]
            ]
          ]
        ]

  let private renderCues dispatch (model: Model) (cuegroup: CueGroup) =
    cuegroup.CueRefs
    |> Array.map (renderCue dispatch model)
    |> List.ofArray

  let private renderGroup dispatch (model: Model) (cuegroup: CueGroup) =
    let headline =
      div [ Class "headline" ] [
        strong [] [ str ("Group: " + string cuegroup.Name) ]
      ]
    let cues = renderCues dispatch model cuegroup
    div [] (headline :: cues)

  let private renderItems tag dispatch model (cuelist: CueList) =
    cuelist.Items
    |> Array.map (renderGroup dispatch model)
    |> List.ofArray
    |> Common.row tag

  let private renderPlayer dispatch model (player: CuePlayer) =
    Common.link
      (string player.Name)
      (fun () -> Select.player dispatch player)

  let private renderPlayers tag dispatch (model: Model) (cuelist: CueList) =
    match model.state with
    | None -> Common.row tag []
    | Some state ->
      state.CuePlayers
      |> Map.filter (fun _ -> CuePlayer.contains cuelist.Id)
      |> Map.toList
      |> List.map (snd >> renderPlayer dispatch model)
      |> Common.row tag

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
