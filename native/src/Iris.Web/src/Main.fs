[<FunScript.JS>]
module Iris.Web.Main

#nowarn "1182"

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript
open System

open FSharp.Html

open Iris.Web.Util
open Iris.Web.Dom
open Iris.Web.Types

open Iris.Web.Types.Patch
open Iris.Web.Types.IOBox
open Iris.Web.Types.Socket
open Iris.Web.Types.View
open Iris.Web.Types.Store
open Iris.Web.Types.Events

open Iris.Web.Views.Patches

(* FIXME: need to factor this out into a nice abstraction *)
let handler (store : Store) (msg : Message) : Store =
  let ev, thing = 
    match msg.Type with
      | "iris.patch.add"    -> (AddPatch,     PatchD(parsePatch msg))
      | "iris.patch.update" -> (UpdatePatch,  PatchD(parsePatch msg))
      | "iris.patch.remove" -> (RemovePatch,  PatchD(parsePatch msg))
      | "iris.iobox.add"    -> (AddIOBox,     IOBoxD(parseIOBox msg))
      | "iris.iobox.update" -> (UpdateIOBox,  IOBoxD(parseIOBox msg))
      | "iris.iobox.remove" -> (RemoveIOBox,  IOBoxD(parseIOBox msg))
      | _                   -> (UnknownEvent, EmptyD)
  in dispatch store { Kind = ev; Payload = thing }

let onClose _ = console.log("closing")

(*   ____          _                     
    |  _ \ ___  __| |_   _  ___ ___ _ __ 
    | |_) / _ \/ _` | | | |/ __/ _ \ '__|
    |  _ <  __/ (_| | |_| | (_|  __/ |   
    |_| \_\___|\__,_|\__,_|\___\___|_|   
*)
let reducer ev state =
  let addPatch'    = addPatch state
  let updatePatch' = updatePatch state
  let removePatch' = removePatch state

  let addIOBox'    = addIOBox state
  let updateIOBox' = updateIOBox state
  let removeIOBox' = removeIOBox state

  match ev with
    | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch'    patch
    | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch' patch
    | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch' patch

    | { Kind = AddIOBox;    Payload = IOBoxD(box) } -> addIOBox'    box
    | { Kind = UpdateIOBox; Payload = IOBoxD(box) } -> updateIOBox' box
    | { Kind = RemoveIOBox; Payload = IOBoxD(box) } -> removeIOBox' box
    | _                                             -> state

(*   __  __       _       
    |  \/  | __ _(_)_ __  
    | |\/| |/ _` | | '_ \ 
    | |  | | (_| | | | | |
    |_|  |_|\__,_|_|_| |_| entry point.
*)

let main () : unit =
  let store  = ref <| mkStore reducer
  let widget = new PatchView ()
  let ctrl   = new ViewController (widget)

  // initialize 
  ctrl.render !store

  // register view controller with store for updates
  store := subscribe !store (fun s e -> ctrl.render s)

  let onMsg (msg : Message) =
    store := handler !store msg

  async {
    let! websocket = Socket.create("ws://localhost:8080", onMsg, onClose)
    websocket.send("start")
  } |> Async.StartImmediate
