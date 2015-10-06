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

open Iris.Web.Views.Patches

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

(*   __  __       _       
    |  \/  | __ _(_)_ __  
    | |\/| |/ _` | | '_ \ 
    | |  | | (_| | | | | |
    |_|  |_|\__,_|_|_| |_| entry point.
*)

let main () : unit =
  let store  = new Store ()
  let widget = new PatchView ()
  let ctrl   = new ViewController (widget)

  // initialize 
  ctrl.render store

  // register view controller with store for updates
  store.AddListener (fun e -> ctrl.render store)

  async {
    let! websocket = Transport.create("ws://localhost:8080", onMsg store, onClose)
    websocket.send("start")
  } |> Async.StartImmediate

  let ps = viewPlugins ()
  let plug = ps.[0].Create ()

  console.log(plug.set Array.empty)
  console.log(plug.get ())
  console.log(plug.render ())
  console.log(plug.dispose ())
