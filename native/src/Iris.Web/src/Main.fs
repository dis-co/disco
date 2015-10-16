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

open Iris.Web.Core.Patch
open Iris.Web.Core.IOBox
open Iris.Web.Core.Socket
open Iris.Web.Core.View
open Iris.Web.Core.Store
open Iris.Web.Core.Events
open Iris.Web.Core.Reducer
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
    let! websocket = createSocket("ws://localhost:8080", onMsg, onClose)
    websocket.send("start")
  } |> Async.StartImmediate
