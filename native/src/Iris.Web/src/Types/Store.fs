[<ReflectedDefinition>]
module Iris.Web.Types.Store

open FSharp.Html

open FunScript.TypeScript
open FunScript.VirtualDom

open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch
open Iris.Web.Types.Events

(* Listener callback. *)
type Listener = (AppEvent -> unit)

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
  static member empty = { Patches  = [] }

(*
     ____  _                 
    / ___|| |_ ___  _ __ ___ 
    \___ \| __/ _ \| '__/ _ \
     ___) | || (_) | | |  __/
    |____/ \__\___/|_|  \___|

    The store centrally manages all state changes and notifies interested
    parties of changes to the carried state (e.g. views, socket transport).

*)

type Store () =
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

  member self.Dispatch (ev : AppEvent) =
    match ev with
      | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch    patch
      | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch patch
      | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch patch
      | { Kind = AddIOBox;    Payload = IOBoxD(patch) } -> addIOBox    patch
      | { Kind = UpdateIOBox; Payload = IOBoxD(patch) } -> updateIOBox patch
      | { Kind = RemoveIOBox; Payload = IOBoxD(patch) } -> removeIOBox patch
      | _ -> Globals.console.log("unhandled event detected")

    notify ev |> ignore

  member self.Subscribe (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member self.GetState
    with get () = state

