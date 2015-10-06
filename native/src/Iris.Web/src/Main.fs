[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript
open System

open FSharp.Html

open Iris.Web.Util
open Iris.Web.Dom
open Iris.Web.Types
open Iris.Web.Plugins

open Iris.Web.Types.Patch
open Iris.Web.Types.IOBox
open Iris.Web.Types.Socket
open Iris.Web.Types.View
open Iris.Web.Types.Store
open Iris.Web.Types.Events

open Iris.Web.Views.Patches

(* FIXME: need to factor this out into a nice abstraction *)
let onMsg (store : Store) (msg : Message) =
  let ev, thing = 
    match msg.Type with
      | "iris.patch.add"    -> (AddPatch,     PatchD(parsePatch msg))
      | "iris.patch.update" -> (UpdatePatch,  PatchD(parsePatch msg))
      | "iris.patch.remove" -> (RemovePatch,  PatchD(parsePatch msg))
      | "iris.iobox.add"    -> (AddIOBox,     IOBoxD(parseIOBox msg))
      | "iris.iobox.update" -> (UpdateIOBox,  IOBoxD(parseIOBox msg))
      | "iris.iobox.remove" -> (RemoveIOBox,  IOBoxD(parseIOBox msg))
      | _                   -> (UnknownEvent, EmptyD)
  in store.Dispatch { Kind = ev; Payload = thing }

let onClose _ = console.log("closing")

let reducer ev state =
  let addPatch (patch : Patch) = 
    { state with Patches = patch :: state.Patches }

  let updatePatch (patch : Patch) = state
  let removePatch (patch : Patch) = state
  let addIOBox    (iobox : IOBox) = state
  let updateIOBox (iobox : IOBox) = state
  let removeIOBox (iobox : IOBox) = state

  match ev with
    | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch    patch
    | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch patch
    | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch patch
    | { Kind = AddIOBox;    Payload = IOBoxD(patch) } -> addIOBox    patch
    | { Kind = UpdateIOBox; Payload = IOBoxD(patch) } -> updateIOBox patch
    | { Kind = RemoveIOBox; Payload = IOBoxD(patch) } -> removeIOBox patch
    | _                                               -> state


(*   __  __       _       
    |  \/  | __ _(_)_ __  
    | |\/| |/ _` | | '_ \ 
    | |  | | (_| | | | | |
    |_|  |_|\__,_|_|_| |_| entry point.
*)

let main () : unit =
  let store  = new Store (reducer)
  let widget = new PatchView ()
  let ctrl   = new ViewController (widget)

  // initialize 
  ctrl.render store

  // register view controller with store for updates
  store.Subscribe (fun e -> ctrl.render store)

  async {
    let! websocket = Socket.create("ws://localhost:8080", onMsg store, onClose)
    websocket.send("start")
  } |> Async.StartImmediate
