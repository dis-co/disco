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

(*   ____          _                     
    |  _ \ ___  __| |_   _  ___ ___ _ __ 
    | |_) / _ \/ _` | | | |/ __/ _ \ '__|
    |  _ <  __/ (_| | |_| | (_|  __/ |   
    |_| \_\___|\__,_|\__,_|\___\___|_|   
*)
let reducer ev state =
  let addPatch (patch : Patch) = 
    { state with Patches = patch :: state.Patches }

  let updatePatch (patch : Patch) =
    { state with
        Patches = let mapper (oldpatch : Patch) =
                      if patch.id = oldpatch.id
                      then patch
                      else oldpatch
                   in List.map mapper state.Patches }
     
  let removePatch (patch : Patch) = 
    let pred (patch' : Patch) = patch.id <> patch'.id
    { state with Patches = List.filter pred state.Patches }

  let addIOBox (iobox : IOBox) =
    let updater (patch : Patch) =
      let idx =
        try Some(Array.findIndex (fun iob -> iob.id = iobox.id) patch.ioboxes)
        with
          | _ -> None

      match idx with
        | Some(place) -> patch // already added
        | None ->
          { patch with
              ioboxes = Array.append patch.ioboxes [|iobox|] }

    let patches = List.map updater state.Patches
    let out = { state with Patches = patches }
    Globals.console.log(out)
    out 

  let updateIOBox (iobox : IOBox) =
    let updater (patch : Patch) =
      { patch with
          ioboxes = Array.map (fun ibx -> 
                                 if ibx.id = iobox.id
                                 then iobox
                                 else ibx) patch.ioboxes }
    { state with Patches = List.map updater state.Patches }

  let removeIOBox (iobox : IOBox) =
    let updater (patch : Patch) =
      if iobox.patch = patch.id
      then { patch with
               ioboxes = Array.filter (fun box -> box.id <> iobox.id) patch.ioboxes }
      else patch
    { state with Patches = List.map updater state.Patches }

  match ev with
    | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch    patch
    | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch patch
    | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch patch

    | { Kind = AddIOBox;    Payload = IOBoxD(box) } -> addIOBox    box
    | { Kind = UpdateIOBox; Payload = IOBoxD(box) } -> updateIOBox box
    | { Kind = RemoveIOBox; Payload = IOBoxD(box) } -> removeIOBox box
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
