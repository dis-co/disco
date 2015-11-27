namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Reducer =

  open Iris.Core.Types

  (*   ____          _
      |  _ \ ___  __| |_   _  ___ ___ _ __
      | |_) / _ \/ _` | | | |/ __/ _ \ '__|
      |  _ <  __/ (_| | |_| | (_|  __/ |
      |_| \_\___|\__,_|\__,_|\___\___|_|
  *)

  let Reducer (ev : AppEvent) (state : State) =
    match ev with
      | PatchEvent(action, patch) ->
          match action with
              | Create -> state.Add patch
              | Update -> state.Update patch
              | Delete -> state.Remove patch
              | _ -> state

      | IOBoxEvent(action, iobox) ->
          match action with
              | Create -> state.Add iobox
              | Update -> state.Update iobox
              | Delete -> state.Remove iobox
              | _ -> state
      
      | CueEvent(action, cueish) ->
          match action with
              | Create ->
                let input = JSON.Stringify(float((new Date()).GetTime()) * Math.Random())
                let cue = { Id = sha1sum input
                          ; Name = "Cue-" + input
                          ; IOBoxes = Array.fold (fun m (ps : Patch) -> Array.append m ps.IOBoxes)
                                      Array.empty state.Patches }
                in state.Add cue
              | Update ->
                if Option.isSome cueish
                then state.Update (Option.get cueish)
                else state
              | Delete ->
                if Option.isSome cueish
                then state.Remove (Option.get cueish)
                else state
              | _ -> state
      
      | _ -> printfn "unknown event" ;state
