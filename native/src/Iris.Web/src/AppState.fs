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

  member x.AddPatch    (patch : Patch) = ()
  member x.UpdatePatch (patch : Patch) = ()
  member x.RemovePatch (patch : Patch) = () 

  member x.AddListener (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member x.ClearListeners (listener : AppEvent -> unit) =
    listeners <- []
