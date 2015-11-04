namespace Iris.Web.Core

open WebSharper

[<JavaScript>]
module Reducer =

  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Events
  open Iris.Web.Core.Store

  (*   ____          _
      |  _ \ ___  __| |_   _  ___ ___ _ __
      | |_) / _ \/ _` | | | |/ __/ _ \ '__|
      |  _ <  __/ (_| | |_| | (_|  __/ |
      |_| \_\___|\__,_|\__,_|\___\___|_|
  *)

  let reducer ev state =
    let addPatch'    = addPatch state
    let updatePatch' = updatePatch state
    let removePatch' = removePatch state

    let addIOBox'    = addIOBox state
    let updateIOBox' = updateIOBox state
    let removeIOBox' = removeIOBox state

    match ev with
      | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch'    patch
      | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch' patch
      | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch' patch

      | { Kind = AddIOBox;    Payload = IOBoxD(box) } -> addIOBox'    box
      | { Kind = UpdateIOBox; Payload = IOBoxD(box) } -> updateIOBox' box
      | { Kind = RemoveIOBox; Payload = IOBoxD(box) } -> removeIOBox' box
      | _                                             -> state
