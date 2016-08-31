namespace Iris.Web.Core

[<AutoOpen>]
module Reducer =

  open Fable.Core
  open Fable.Import
  open Fable.Import.JS

  open Iris.Core

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
        | Create -> state.AddPatch patch
        | Update -> state.UpdatePatch patch
        | Delete -> state.RemovePatch patch
        | _ -> state

    | IOBoxEvent(action, iobox) ->
        match action with
        | Create -> state.AddIOBox iobox
        | Update -> state.UpdateIOBox iobox
        | Delete -> state.RemoveIOBox iobox
        | _ -> state

    | CueEvent(action, cueish) ->
        match action with
        | Create ->
          let input = JSON.stringify(Date.now() * Math.random())
          let cue : Cue  = { Id = sha1sum input
                            ; Name = "Cue-" + input
                            ; IOBoxes = [||] }
          in state.AddCue cue
        | Update ->
          if Option.isSome cueish
          then state.UpdateCue (Option.get cueish)
          else state
        | Delete ->
          if Option.isSome cueish
          then state.RemoveCue (Option.get cueish)
          else state
        | _ -> state

    | _ -> printfn "unknown event" ;state
