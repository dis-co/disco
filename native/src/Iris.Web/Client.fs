namespace Iris.Web

open System
open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =
  
  open Iris.Web.Core.Html
  open Iris.Web.Core.Patch
  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Socket
  open Iris.Web.Core.ViewController
  open Iris.Web.Core.Store
  open Iris.Web.Core.Events
  open Iris.Web.Core.Reducer
  open Iris.Web.Views.PatchView

  (* FIXME: need to factor this out into a nice abstraction *)
  let handler (store : Store<State>) (msg : Message) : Store<State> =
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
  
  let onClose _ = Console.Log("closing")
  
  (*   __  __       _       
      |  \/  | __ _(_)_ __  
      | |\/| |/ _` | | '_ \ 
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  
  let Main =
    let store  = ref <| mkStore reducer State.Empty
    let widget = new PatchView ()
    let ctrl   = new ViewController<State> (widget)
  
    // initialize 
    ctrl.Render !store
  
    // register view controller with store for updates
    store := subscribe !store (fun s _ -> ctrl.Render s)
  
    // let onMsg (msg : Message) =
    //   store := handler !store msg

    Console.Log("STARTINMG!!")
  
    // async {
    //   let! websocket = createSocket("ws://localhost:8080", onMsg, onClose)
    //   websocket.send("start")
    // } |> Async.StartImmediate
