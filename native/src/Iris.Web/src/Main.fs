[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Core.Types.IOBox
open Iris.Web.Html
open Iris.Web.VirtualDom

[<JSEmit("""return JSON.stringify({0});""")>]
let toString (i : obj) = ""

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

  let render (cnt : int) =
    ul <@> class' "nostyle" <@> id' "main" <||>
      [ li <|> text ("previous: " + (toString <| cnt - 1))
      ; li <|> text ("current: "  + (toString cnt))
      ; li <|> text ("next: "     + (toString <| cnt + 1))
      ]

  let count = ref 0
  let tree = ref (htmlToVTree (render !count)) 
  let rootNode = ref (createElement !tree)

  Globals.document.body.appendChild(!rootNode) |> ignore

  Globals.setInterval((fun _ ->
                         count := (!count + 1)
                         let newtree = htmlToVTree <| render !count
                         let patches = diff tree newtree
                         rootNode := patch !rootNode patches
                         tree := newtree
                         ), 1000)
