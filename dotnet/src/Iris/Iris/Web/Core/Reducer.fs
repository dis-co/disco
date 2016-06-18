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
                let input = JSON.stringify(Date.now() * Math.random())
                let cue : Cue  = { Id = sha1sum input
                                 ; Name = "Cue-" + input
                                 ; IOBoxes = [||] }
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
