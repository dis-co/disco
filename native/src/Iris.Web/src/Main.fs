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

let document = Globals.document
let setInterval = Globals.setInterval

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)
let main() =
  let render content =
    div <@> id' "main" <||>
      [ h1 <|> text "Content:"
      ; p  <|> text content
      ; hr
      ]

  let msg = ref "not connteced"
  let tree = ref (htmlToVTree (render !msg)) 
  let rootNode = ref (createElement !tree)

  document.body.appendChild(!rootNode) |> ignore

  async {
    let! websocket = Transport.create("ws://localhost:8080",
                       (fun str -> 
                            msg := str
                            let newtree = htmlToVTree <| render !msg
                            let patches = diff tree newtree
                            rootNode := patch !rootNode patches
                            tree := newtree),
                       (fun _   -> Globals.console.log("closed..")))
    websocket.send("hello")
  } |> Async.StartImmediate

