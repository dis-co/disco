[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Core.Types.IOBox
open Iris.Web.Html
open Iris.Web.VirtualDom

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)
let main() =
  // Routes.start ()

  // let box = ValueBox { name      = "hello"
  //                    ; tag       = None
  //                    ; valType   = Bool
  //                    ; behavior  = Toggle
  //                    ; vecSize   = 1
  //                    ; min       = 0
  //                    ; max       = 1
  //                    ; unit      = None
  //                    ; precision = None
  //                    ; slices    = []
  //                    }

  // async {
  //   let! websocket = Transport.create("ws://localhost:8080",
  //                      (fun str -> Globals.console.log(str)),
  //                      (fun _   -> Globals.console.log("closed..")))
  //   websocket.send("hell not")
  // } |> Async.StartImmediate

  // let nod1 = mkVNode "div#hell" Array.empty
  // let nod2 = mkVNode "div#heaven" [| nod1 |]

  let mylist = ul <||>
               [ li <|> text "hello"
               ; li <|> text "bye"
               ]

  Globals.console.log("plain")
  Globals.console.log(renderHtml mylist)

  Globals.console.log("virtual-dom")
  Globals.console.log(htmlToVTree mylist |> createElement)
