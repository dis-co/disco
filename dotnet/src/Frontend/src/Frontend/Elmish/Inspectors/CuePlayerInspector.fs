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

module CuePlayerInspector =

  let private renderItem dispatch (model: Model) = function
    | CuePlayerItem.Headline headline ->
      li [] [
        str "Headline:"
        strong [] [str headline]
      ]
    | CuePlayerItem.CueList id ->
      match model.state with
      | None ->
        li [] [
          str "Cue List:"
          str (string id + " (orphaned)")
        ]
      | Some state ->
        match Map.tryFind id state.CueLists with
        | None -> li [] [ str (string id + " (orphaned)") ]
        | Some cuelist ->
          li [] [
            str "Cue List:"
            Common.link
              (string cuelist.Name)
              (fun () -> Select.cuelist dispatch cuelist)
          ]

  let private renderItems tag dispatch (model: Model) (player: CuePlayer) =
    player.Items
    |> Array.map (renderItem dispatch model)
    |> List.ofArray
    |> fun items -> Common.row tag [ ul [] items ]

  let render dispatch (model: Model) (player: PlayerId) =
    match model.state with
    | None ->
      Common.render dispatch model "Player" [
        str (string player + " (orphaned)")
      ]
    | Some state ->
      match Map.tryFind player state.CuePlayers with
      | None ->
        Common.render dispatch model "Player" [
          str (string player + " (orphaned)")
        ]
      | Some player ->
        Common.render dispatch model "Player" [
          Common.stringRow "Id"            (string player.Id)
          Common.stringRow "Name"          (string player.Name)
          Common.stringRow "Locked"        (string player.Locked)
          Common.stringRow "Selected"      (string player.Selected)
          Common.stringRow "RemainingWait" (string player.RemainingWait)
          Common.stringRow "Call"          (string player.CallId)
          Common.stringRow "Next"          (string player.NextId)
          Common.stringRow "Previous"      (string player.PreviousId)
          Common.stringRow "Last Called"   (string player.LastCalledId)
          Common.stringRow "Last Caller"   (string player.LastCallerId)
          renderItems      "Items"          dispatch model player
        ]
