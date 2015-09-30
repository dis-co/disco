[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.TypeScript
open System

//open Iris.Core.Types.IOBox
open Iris.Web.Html
open Iris.Web.VirtualDom
open Iris.Web.Plugins

[<JSEmit("""return JSON.stringify({0});""")>]
let toString (i : obj) = ""

let console = Globals.console
let document = Globals.document
let setInterval = Globals.setInterval

let JSON = Globals.JSON

type IOBox = { Name : string }

type EventType =
  | AddPin
  | RemovePin
  | UpdatePin

type AppEvent = { Kind : EventType; }

type AppStore () =
  let mutable pins : IOBox list = []
  let mutable listeners : (AppEvent -> unit) list = []

  member this.Pins with get () = pins

  member this.Add (pin : IOBox) =
    pins <- pin :: pins
    List.map (fun l -> l { Kind = AddPin }) listeners

  member this.Update (pin : IOBox) =
    pins <- List.map (fun p -> if p = pin then pin else p) pins
    List.map (fun l -> l { Kind = RemovePin }) listeners

  member this.Remove (pin : IOBox) =
    pins <- List.filter (fun p -> p <> pin) pins
    List.map (fun l -> l { Kind = RemovePin }) listeners

  member this.AddListener (listener : AppEvent -> unit) =
    listeners <- listener :: listeners
 
  member this.ClearListeners (listener : AppEvent -> unit) =
    listeners <- []

let onMsg (store : AppStore) (str : string) =
  let parsed = str.Split(':') 
  match parsed.[0] with
    | "add"    -> store.Add { Name = parsed.[1] }    |> ignore 
    | "remove" -> store.Remove { Name = parsed.[1] } |> ignore
    | _        -> console.log("unknown command")
  
let onClose _ = console.log("closing")
  
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

  // (fun str -> 
  //      msg := str
  //      let newtree = htmlToVTree <| render !msg
  //      let patches = diff tree newtree
  //      rootNode := patch !rootNode patches
  //      tree := newtree),

  let store = new AppStore()

  store.AddListener (fun e ->
                     let res = match e.Kind with
                                | AddPin -> "pin was added"
                                | RemovePin -> "pin was removed"
                                | UpdatePin -> "pin was updated"
                     console.log("Event occurred: " + res)
                     console.log(List.map (fun p -> p.Name) store.Pins |> List.toArray))

  async {
    let! websocket = Transport.create("ws://localhost:8080", onMsg store, onClose)
    websocket.send("start")
  } |> Async.StartImmediate

  let ps = getPlugins ()
  Globals.console.log(ps.[0]);

