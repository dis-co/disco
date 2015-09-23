[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Core.Types.IOBox

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

  let box = ValueBox( name = "hello"
                    , tag = None
                    , valType = Bool
                    , behavior = Toggle
                    , vecSize = 1
                    , min = 0
                    , max = 1
                    , unit = None
                    , precision = None
                    , slices = []
                    )

  Globals.console.log (box)
  
  Globals.console.log (getName box)

  let box' = setName box "bye"

  Globals.console.log (getName box')
