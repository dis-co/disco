[<FunScript.JS>]
module Iris.Web.Main

open FunScript
open FunScript.VirtualDom
open FunScript.TypeScript
open System

open FSharp.Html

open Iris.Web.Util
open Iris.Web.Dom
open Iris.Web.Types
open Iris.Web.AppState
open Iris.Web.Plugins

let parsePatchMsg (msg : MsgPayload) = PatchP <| new Patch ()
let parseIOBoxMsg (msg : MsgPayload) = IOBoxP <| new IOBox ()

let onMsg (state : AppState) (msg : Message) =
  match msg.Type with
    | "iris.patch.add" ->
      match parsePatchMsg msg.Payload with
        | PatchP(p) -> state.AddPatch p; console.log(p)
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

let patchView (patch : Patch) : Html =
  div <@> class' "patch" <||>
    [ hr
    ; h3 <|> text "Patch:"
    ; p  <|> text (patch.Name)
    ; hr
    ]

let patchList (state : AppState) =
  div <@> id' "patches" <||>
    List.map patchView state.Patches

let header = h1 <|> text "All Patches"

let footer =
  div <@> class' "foot" <||>
    [ hr
    ; span <|> text "yep"
    ]

let mainView content =
  div <@> id' "main" <||>
    [ header
    ; content 
    ; footer
    ]


// let render (state : AppState) =
//   let newtree =  |> htmlToVTree
//   let patches = diff view.tree newtree
//   rootNode := patch !rootNode patches
//   tree := newtree

// let msg = ref "not connteced"
// let tree = ref (htmlToVTree (render !msg)) 
// let rootNode = ref (createElement !tree)

let render (state : AppState) (content : Html) =
  let newtree = mainView content |> htmlToVTree

  let init () = 
    let rootNode = createElement newtree
    document.body.appendChild(rootNode) |> ignore
    state.RootNode  <- Some(rootNode)

  match state.ViewState with
    | Some(oldtree) -> 
      match state.RootNode with
        | Some(rootNode) -> 
          let viewPatch = diff oldtree newtree
          state.RootNode <- Some(patch rootNode viewPatch)
        | _ -> init ()
    | _ -> init ()

  state.ViewState <- Some(newtree)

(*   __  __       _       
    |  \/  | __ _(_)_ __  
    | |\/| |/ _` | | '_ \ 
    | |  | | (_| | | | | |
    |_|  |_|\__,_|_|_| |_| entry point.
*)

let main () : unit =
  let state = new AppState()

  let content = p <|> text "Empty"
  render state content

  state.AddListener (fun e -> patchList state |> render state)

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
