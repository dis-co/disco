[<FunScript.JS>]
module Iris.Web.AppState

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript

open Iris.Web.Util
open Iris.Web.Types
// open Iris.Core.Types.Patch
// open Iris.Core.Types.IOBox

(*
   --------------------------
   - Global state container -
   --------------------------
*)

type AppState () =
  let mutable state     : State = State.empty
  let mutable listeners : Listener list = []

  let notify ev =
    List.map (fun l -> l ev) listeners

  // let updatePins pins =
  //   state <- { state with IOBoxes = pins }

  let addPatch (patch : Patch) = 
    state <- { state with Patches = patch :: state.Patches }

  let updatePatch (patch : Patch) = ()
  let removePatch (patch : Patch) = ()

  let addIOBox    (iobox : IOBox) = ()
  let updateIOBox (iobox : IOBox) = ()
  let removeIOBox (iobox : IOBox) = ()

  member x.Dispatch (ev : AppEvent) =
    match ev with
      | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch    patch
      | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch patch
      | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch patch
      | { Kind = AddIOBox;    Payload = IOBoxD(patch) } -> addIOBox    patch
      | { Kind = UpdateIOBox; Payload = IOBoxD(patch) } -> updateIOBox patch
      | { Kind = RemoveIOBox; Payload = IOBoxD(patch) } -> removeIOBox patch
      | _ -> console.log("unhandled event detected")

    notify ev |> ignore

  member x.RootNode
    with get ()   = state.RootNode
    and  set node = state <- { state with RootNode = node }

  member x.ViewState
    with get ()   = state.ViewTree
    and  set tree = state <- { state with ViewTree = tree }

  member x.AddListener (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member x.ClearListeners (listener : AppEvent -> unit) =
    listeners <- []

  member x.Patches
    with get () = state.Patches 
