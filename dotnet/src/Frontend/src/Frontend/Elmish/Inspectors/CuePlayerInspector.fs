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

  let render dispatch (model: Model) (player: CuePlayer) =
    Common.render dispatch model "Player" [
      Common.stringRow "Id"            (string player.Id)
      Common.stringRow "Name"          (string player.Name)
      Common.stringRow "Locked"        (string player.Locked)
      Common.stringRow "Selected"      (string player.Selected)
      Common.stringRow "RemainingWait" (string player.RemainingWait)
      Common.stringRow "Call"          (string player.CallId)
      Common.stringRow "Next"          (string player.NextId)
      Common.stringRow "Previous"      (string player.PreviousId)
      Common.stringRow "Previous"      (string player.PreviousId)
      Common.stringRow "Last Called"   (string player.LastCalledId)
      Common.stringRow "Last Caller"   (string player.LastCallerId)
      Common.stringRow "Items"         (string player.Items)
    ]
