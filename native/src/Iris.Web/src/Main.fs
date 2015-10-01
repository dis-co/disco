[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

open Iris.Web.AppState
open Iris.Web.Html
open Iris.Web.VirtualDom
open Iris.Web.Plugins

[<JSEmit("""return JSON.stringify({0});""")>]
let toString (i : obj) = ""

let console = Globals.console
let document = Globals.document
let setInterval = Globals.setInterval

let JSON = Globals.JSON

type Patch = { Name : string }
type IOBox = { Name : string }

let onMsg (store : AppState) (str : string) =
  let parsed = str.Split(':') 
  match parsed.[0] with
    // | "add"    -> store.Add    { Name = parsed.[1] } 
    // | "remove" -> store.Remove { Name = parsed.[1] }
    // | "update" -> store.Update { Name = parsed.[1] }
    | _        -> console.log("unknown command"); []
  |> ignore

let onClose _ = console.log("closing")

let view content =
  div <@> id' "main" <||>
    [ h1 <|> text "Content:"
    ; p  <|> text content
    ; hr
    ]

// let render view state =
//   let newtree = htmlToVTree <| state.render !msg
//   let patches = diff view.tree newtree
//   rootNode := patch !rootNode patches
//   tree := newtree

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)
let main() =

  // let msg = ref "not connteced"
  // let tree = ref (htmlToVTree (render !msg)) 
  // let rootNode = ref (createElement !tree)

  // document.body.appendChild(!rootNode) |> ignore

  let store = new AppState()

  // store.AddListener (fun e ->
  //                    let res = match e.Kind with
  //                               | AddPin -> "pin was added"
  //                               | RemovePin -> "pin was removed"
  //                               | UpdatePin -> "pin was updated"
  //                    console.log("Event occurred: " + res)
  //                    console.log(List.map (fun p -> p.Name) store.Pins |> List.toArray))

  async {
    let! websocket = Transport.create("ws://localhost:8080", onMsg store, onClose)
    websocket.send("start")
  } |> Async.StartImmediate

  let ps = getPlugins ()
  Globals.console.log(ps.[0]);

