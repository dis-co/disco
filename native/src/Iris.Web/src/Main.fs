[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Core.Types.Pin

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

  let s = new Store.DataStore ()
  Globals.console.log (s.Dispatch ())
