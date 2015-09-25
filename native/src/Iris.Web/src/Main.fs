[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Core.Types.IOBox
open Iris.Web.Transport

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)

let main() =
  // let conn = Transport.connect "ws://localhost:9500"
  // conn._open ()
  // let hello = DOM.hello ()
  // Routes.start ()

  // let s = new Store.DataStore ()

  let box = ValueBox { name      = "hello"
                     ; tag       = None
                     ; valType   = Bool
                     ; behavior  = Toggle
                     ; vecSize   = 1
                     ; min       = 0
                     ; max       = 1
                     ; unit      = None
                     ; precision = None
                     ; slices    = []
                     }

  async {
    let! websocket = Transport.create("ws://localhost:8080",
                       (fun str -> Globals.console.log(str)),
                       (fun _   -> Globals.console.log("closed..")))
    websocket.send("hell not")
  } |> Async.StartImmediate
