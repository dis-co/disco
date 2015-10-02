[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript
open System

open FSharp.Html
open Iris.Web.Util
open Iris.Web.Types
open Iris.Web.AppState
open Iris.Web.Plugins

let parsePatchMsg (msg : MsgPayload) = PatchP <| new Patch ()
let parseIOBoxMsg (msg : MsgPayload) = IOBoxP <| new IOBox ()

let onMsg (state : AppState) (msg : Message) =
  match msg.Type with
    | "iris.patch.add" ->
      match parsePatchMsg msg.Payload with
        | PatchP(p) -> state.AddPatch p
        | _         -> console.log("could not add patch")

    | "iris.patch.update" ->
      match parsePatchMsg msg.Payload with
        | PatchP(p) -> state.UpdatePatch p
        | _         -> console.log("could not update patch: ")

    | "iris.patch.remove" ->
      match parsePatchMsg msg.Payload with
        | PatchP(p) -> state.RemovePatch p
        | _         -> console.log("could not remove patch: ")

    | "iris.iobox.add" -> 
      match parseIOBoxMsg msg.Payload with
        | IOBoxP(p) -> state.AddIOBox p
        | _         -> console.log("could not update pin: ")

    | "iris.iobox.update" -> 
      match parseIOBoxMsg msg.Payload with
        | IOBoxP(p) -> state.UpdateIOBox p
        | _         -> console.log("could not update pin: ")

    | "iris.iobox.remove" -> 
      match parseIOBoxMsg msg.Payload with
        | IOBoxP(p) -> state.RemoveIOBox p
        | _         -> console.log("could not update pin: ")

    | a -> console.log("Invalid message: " + a)

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

// let msg = ref "not connteced"
// let tree = ref (htmlToVTree (render !msg)) 
// let rootNode = ref (createElement !tree)

// document.body.appendChild(!rootNode) |> ignore

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)
let main() =
  let state = new AppState()
  state.AddListener (fun e -> console.log(e))

  async {
    let! websocket = Transport.create("ws://localhost:8080", onMsg state, onClose)
    websocket.send("start")
  } |> Async.StartImmediate

  let ps = viewPlugins ()
  let plug = ps.[0].Create ()

  console.log(plug.set Array.empty)
  console.log(plug.get ())
  console.log(plug.render ())
  console.log(plug.dispose ())
