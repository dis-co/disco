namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module State =

  open Iris.Core.Types

  (*   ____  _        _
      / ___|| |_ __ _| |_ ___
      \___ \| __/ _` | __/ _ \
       ___) | || (_| | ||  __/
      |____/ \__\__,_|\__\___|

      Record type containing all the actual data that gets passed around in our
      application.
  *)

  type State =
    { Patches  : Patch array }
    static member Empty = { Patches = Array.empty }

  let addPatch (state : State) (patch : Patch) =
    let exists = Array.exists (fun (p : Patch) -> p.Id = patch.Id) state.Patches
    if exists
    then state
    else
      let patches' = Array.map id state.Patches // copy the array
      { state with Patches = Array.append patches' [| patch |]  }

  let updatePatch (state : State) (patch : Patch) =
    { state with
        Patches = let mapper (oldpatch : Patch) =
                      if patch.Id = oldpatch.Id
                      then patch
                      else oldpatch
                   in Array.map mapper state.Patches }

  let removePatch (state : State) (patch : Patch) =
    let pred (patch' : Patch) = patch.Id <> patch'.Id
    { state with Patches = Array.filter pred state.Patches }


  let addIOBox (state : State) (iobox : IOBox) =
    let updater (patch : Patch) =
      if iobox.Patch = patch.Id
      then addIOBox patch iobox
      else patch
    { state with Patches = Array.map updater state.Patches }


  let updateIOBox (state : State) (iobox : IOBox) =
    let mapper (patch : Patch) = updateIOBox patch iobox
    { state with Patches = Array.map mapper state.Patches }


  let removeIOBox (state : State) (iobox : IOBox) =
    let updater (patch : Patch) =
      if iobox.Patch = patch.Id
      then removeIOBox patch iobox
      else patch
    { state with Patches = Array.map updater state.Patches }
