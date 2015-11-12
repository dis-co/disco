namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Store =

  open Iris.Web.Core.Html
  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Events

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

  (* Reducers are take a state, an action, acts and finally return the new state *)
  type Reducer<'a> = (AppEvent -> 'a -> 'a)

  (*
       ____  _
      / ___|| |_ ___  _ __ ___
      \___ \| __/ _ \| '__/ _ \
       ___) | || (_) | | |  __/
      |____/ \__\___/|_|  \___|

      The store centrally manages all state changes and notifies interested
      parties of changes to the carried state (e.g. views, socket transport).

  *)

  type Store<'a> =
    { Reducer   : Reducer<'a>
    ; State     : 'a
    ; Listeners : Listener<'a> list }

  and Listener<'a> = (Store<'a> -> AppEvent -> unit)

  let private notify (store : Store<'a>) (ev : AppEvent) =
    List.map (fun l -> l store ev) store.Listeners

  let dispatch (store : Store<'a>) (ev : AppEvent) : Store<'a> =
    let newstate = store.Reducer ev store.State
    let newstore = { store with State = newstate }
    notify newstore ev |> ignore
    newstore

  let subscribe (store : Store<'a>) (listener : Listener<'a>) =
    { store with Listeners = listener :: store.Listeners }

  let mkStore (reducer : Reducer<'a>) (state : 'a) =
    { Reducer = reducer
    ; State   = state
    ; Listeners = []
    }
