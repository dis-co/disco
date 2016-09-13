namespace Iris.Core

[<AutoOpen>]
module Reducer =

#if JAVASCRIPT
  open Fable.Core
  open Fable.Import
  open Fable.Import.JS
#endif

  (*   ____          _
      |  _ \ ___  __| |_   _  ___ ___ _ __
      | |_) / _ \/ _` | | | |/ __/ _ \ '__|
      |  _ <  __/ (_| | |_| | (_|  __/ |
      |_| \_\___|\__,_|\__,_|\___\___|_|
  *)

  let Reducer (ev : StateMachine) (state : State) =
    match ev with
    | AddCue                cue -> state.AddCue        cue
    | UpdateCue             cue -> state.UpdateCue     cue
    | RemoveCue             cue -> state.RemoveCue     cue

    | AddCueList        cuelist -> state.AddCueList    cuelist
    | UpdateCueList     cuelist -> state.UpdateCueList cuelist
    | RemoveCueList     cuelist -> state.RemoveCueList cuelist

    | AddPatch            patch -> state.AddPatch      patch
    | UpdatePatch         patch -> state.UpdatePatch   patch
    | RemovePatch         patch -> state.RemovePatch   patch

    | AddIOBox            iobox -> state.AddIOBox      iobox
    | UpdateIOBox         iobox -> state.UpdateIOBox   iobox
    | RemoveIOBox         iobox -> state.RemoveIOBox   iobox

    | AddNode              node -> state.AddNode       node
    | UpdateNode           node -> state.UpdateNode    node
    | RemoveNode           node -> state.RemoveNode    node

    | AddSession        session -> state.AddSession    session
    | UpdateSession     session -> state.UpdateSession session
    | RemoveSession     session -> state.RemoveSession session

    | AddUser              user -> state.AddUser       user
    | UpdateUser           user -> state.UpdateUser    user
    | RemoveUser           user -> state.RemoveUser    user

    | _                 -> state
