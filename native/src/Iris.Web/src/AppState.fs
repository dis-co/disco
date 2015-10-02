[<FunScript.JS>]
module Iris.Web.AppState

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript

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

  member x.RootNode
    with get ()   = state.RootNode
    and  set node = state <- { state with RootNode = node }

  member x.ViewState
    with get ()   = state.ViewTree
    and  set tree = state <- { state with ViewTree = tree }

  member x.AddIOBox (iobox : IOBox) =
    // iobox :: state.IOBoxes
    // |> updatePins
    notify { Kind = AddPin; Data = EmptyD } |> ignore

  member x.UpdateIOBox (iobox : IOBox) =
    // iobox :: state.IOBoxes
    // |> updatePins
    notify { Kind = AddPin; Data = EmptyD } |> ignore
    
  member x.RemoveIOBox (iobox : IOBox) =
    // iobox :: state.IOBoxes
    // |> updatePins
    notify { Kind = AddPin; Data = EmptyD } |> ignore

  member x.AddPatch (patch : Patch) =
    state <- { state with Patches = patch :: state.Patches }
    notify { Kind = AddPatch; Data = EmptyD } |> ignore

  member x.UpdatePatch (patch : Patch) =
    notify { Kind = UpdatePatch; Data = EmptyD } |> ignore

  member x.RemovePatch (patch : Patch) =
    notify { Kind = RemovePatch; Data = EmptyD } |> ignore

  member x.AddListener (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member x.ClearListeners (listener : AppEvent -> unit) =
    listeners <- []

  member x.Patches
    with get () = state.Patches 
