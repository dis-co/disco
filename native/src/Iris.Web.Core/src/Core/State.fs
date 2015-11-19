namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module State =

  (*   ____  _        _
      / ___|| |_ __ _| |_ ___
      \___ \| __/ _` | __/ _ \
       ___) | || (_| | ||  __/
      |____/ \__\__,_|\__\___|

      Record type containing all the actual data that gets passed around in our
      application.
  *)

  type State =
    { Patches  : Patch list }
    static member Empty = { Patches = [] }

  let addPatch (state : State) (patch : Patch) =
    let exists = List.exists (fun p -> p.id = patch.id) state.Patches
    if not exists
    then { state with Patches = patch :: state.Patches }
    else state

  let updatePatch (state : State) (patch : Patch) =
    { state with
        Patches = let mapper (oldpatch : Patch) =
                      if patch.id = oldpatch.id
                      then patch
                      else oldpatch
                   in List.map mapper state.Patches }

  let removePatch (state : State) (patch : Patch) =
    let pred (patch' : Patch) = patch.id <> patch'.id
    { state with Patches = List.filter pred state.Patches }


  let addIOBox (state : State) (iobox : IOBox) =
    let updater (patch : Patch) =
      if iobox.patch = patch.id
      then addIOBox patch iobox
      else patch
    { state with Patches = List.map updater state.Patches }


  let updateIOBox (state : State) (iobox : IOBox) =
    let mapper (patch : Patch) = updateIOBox patch iobox
    { state with Patches = List.map mapper state.Patches }


  let removeIOBox (state : State) (iobox : IOBox) =
    let updater (patch : Patch) =
      if iobox.patch = patch.id
      then removeIOBox patch iobox
      else patch
    { state with Patches = List.map updater state.Patches }
