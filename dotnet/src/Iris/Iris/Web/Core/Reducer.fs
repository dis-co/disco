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

  let Reducer (ev : ApplicationEvent) (state : State) =
    match ev with
    | AddCue cue        -> state.AddCue      cue
    | UpdateCue     cue -> state.UpdateCue   cue
    | RemoveCue     cue -> state.RemoveCue   cue
    | AddPatch    patch -> state.AddPatch    patch
    | UpdatePatch patch -> state.UpdatePatch patch
    | RemovePatch patch -> state.RemovePatch patch
    | AddIOBox    iobox -> state.AddIOBox    iobox
    | UpdateIOBox iobox -> state.UpdateIOBox iobox
    | RemoveIOBox iobox -> state.RemoveIOBox iobox
    | AddNode      node -> state.AddNode    node
    | UpdateNode   node -> state.UpdateNode node
    | RemoveNode   node -> state.RemoveNode node
    | _                 -> state
