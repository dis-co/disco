namespace Iris.Service.Client

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Client =
  open System
  
  open Iris.Service.Client.Core.Html
  open Iris.Service.Client.Core.Patch
  open Iris.Service.Client.Core.IOBox
  open Iris.Service.Client.Core.Socket
  open Iris.Service.Client.Core.ViewController
  open Iris.Service.Client.Core.Store
  open Iris.Service.Client.Core.Events
  open Iris.Service.Client.Core.Reducer

  open Iris.Service.Client.Views.PatchView

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
  
  let onClose _ = Console.Log("closing")
  
  (*   __  __       _       
      |  \/  | __ _(_)_ __  
      | |\/| |/ _` | | '_ \ 
      | |  | | (_| | | | | |
      |_|  |_|\__,_|_|_| |_| entry point.
  *)
  
  let Main =
    let store  = ref <| mkStore reducer
    let widget = new PatchView ()
    let ctrl   = new ViewController (widget)
  
    // initialize 
    ctrl.render !store
  
    // register view controller with store for updates
    store := subscribe !store (fun s e -> ctrl.render s)
  
    let onMsg (msg : Message) =
      store := handler !store msg

    Console.Log("STARTINMG!!")
  
    // async {
    //   let! websocket = createSocket("ws://localhost:8080", onMsg, onClose)
    //   websocket.send("start")
    // } |> Async.StartImmediate
