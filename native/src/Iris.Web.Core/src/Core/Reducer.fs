namespace Iris.Web.Core

open WebSharper

[<AutoOpen>]
[<JavaScript>]
module Reducer =

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
      | PatchEvent (Kind = AddPatch;    Patch = patch) -> addPatch'    patch
      | PatchEvent (Kind = UpdatePatch; Patch = patch) -> updatePatch' patch
      | PatchEvent (Kind = RemovePatch; Patch = patch) -> removePatch' patch

      | IOBoxEvent (Kind = AddIOBox;    IOBox = iobox ) -> addIOBox'    iobox
      | IOBoxEvent (Kind = UpdateIOBox; IOBox = iobox ) -> updateIOBox' iobox
      | IOBoxEvent (Kind = RemoveIOBox; IOBox = iobox ) -> removeIOBox' iobox
      
      | _ -> printfn "unknown event" ;state
