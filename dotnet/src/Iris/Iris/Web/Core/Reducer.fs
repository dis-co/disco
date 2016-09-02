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

    | CueEvent(action, cue) ->
        match action with
        | Create -> state.AddCue    cue
        | Update -> state.UpdateCue cue
        | Delete -> state.RemoveCue cue
        | _      -> state

    | _ -> printfn "unknown event" ;state
