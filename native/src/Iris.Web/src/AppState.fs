[<FunScript.JS>]
module Iris.Web.AppState

open FunScript
open FunScript.TypeScript

open Iris.Web.VirtualDom
open Iris.Core.Types.Patch
open Iris.Core.Types.IOBox

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

  let updatePins pins =
    state <- { state with Pins = pins }

  member o.Pins with get () = state.Pins

  member o.Add (pin : IOBox) =
    pin :: state.Pins
    |> updatePins

    notify { Kind = AddPin; Data = IOBoxD pin }

  member o.Update (pin : IOBox) =
    List.map (fun p -> if p = pin then pin else p) state.Pins
    |> updatePins

    notify { Kind = UpdatePin; Data = IOBoxD pin }

  member o.Remove (pin : IOBox) =
    List.filter (fun p -> p <> pin) state.Pins
    |> updatePins

    notify { Kind = RemovePin; Data = IOBoxD pin }

  member o.AddListener (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member o.ClearListeners (listener : AppEvent -> unit) =
    listeners <- []
