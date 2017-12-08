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

module CuePlayerInspector =

  let orphanedCueList tag id =
    Common.row tag [ str (string id + " (orphaned)") ]

  let renderLink tag dispatch (model:Model) (cuelistId: CueListId) =
    model.state
    |> Option.bind (fun state -> Map.tryFind cuelistId state.CueLists)
    |> function
    | None -> orphanedCueList tag id
    | Some cuelist ->
      Common.row tag [
        Common.link
          (string cuelist.Name)
          (fun () -> Select.cuelist dispatch cuelist)
      ]

  let renderCueList tag dispatch model = function
    | Some cuelistId -> renderLink tag dispatch model cuelistId
    | None -> Common.row tag []

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
          renderCueList    "Cue List"      dispatch model player.CueListId
        ]
