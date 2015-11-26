namespace Iris.Web.Core

open WebSharper

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
      
      | _ -> printfn "unknown event" ;state
