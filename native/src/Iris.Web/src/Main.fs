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
open Iris.Web.Plugins

[<JSEmit("""return {0}.payload;""")>]
let parsePatch (msg : Message) : Patch = failwith "never"

[<JSEmit("""return {0}.payload;""")>]
let parseIOBox (msg : Message) : IOBox = failwith "never"

let onMsg (store : Store) (msg : Message) =
  let ev, thing = 
    match msg.Type with
      | "iris.patch.add"    -> (AddPatch,     PatchD(parsePatch msg))
      | "iris.patch.update" -> (UpdatePatch,  PatchD(parsePatch msg))
      | "iris.patch.remove" -> (RemovePatch,  PatchD(parsePatch msg))
      | "iris.iobox.add"    -> (AddIOBox,     IOBoxD(parseIOBox msg))
      | "iris.iobox.update" -> (UpdateIOBox,  IOBoxD(parseIOBox msg))
      | "iris.iobox.remove" -> (RemoveIOBox,  IOBoxD(parseIOBox msg))
      | _                   -> (UnknownEvent, EmptyD)
  in store.Dispatch { Kind = ev; Payload = thing }

let onClose _ = console.log("closing")

let patchView (patch : Patch) : Html =
  div <@> class' "patch" <||>
    [ hr
    ; h3 <|> text "Patch:"
    ; p  <|> text (patch.Name)
    ; hr
    ]

let patchList (store : Store) =
  div <@> id' "patches" <||>
    List.map patchView store.Patches

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

    
let render (store : Store) (content : Html) =
  let newtree = mainView content |> htmlToVTree

  let init () = 
    let rootNode = createElement newtree
    document.body.appendChild(rootNode) |> ignore
    store.RootNode  <- Some(rootNode)

  match store.ViewState with
    | Some(oldtree) -> 
      match store.RootNode with
        | Some(rootNode) -> 
          let viewPatch = diff oldtree newtree
          store.RootNode <- Some(patch rootNode viewPatch)
        | _ -> init ()
    | _ -> init ()

  store.ViewState <- Some(newtree)

(*   __  __       _       
    |  \/  | __ _(_)_ __  
    | |\/| |/ _` | | '_ \ 
    | |  | | (_| | | | | |
    |_|  |_|\__,_|_|_| |_| entry point.
*)

let main () : unit =
  let store = new Store ()

  let content = p <|> text "Empty"
  render store content

  store.AddListener (fun e -> patchList store |> render store)

  async {
    let! websocket = Transport.create("ws://localhost:8080", onMsg store, onClose)
    websocket.send("start")
  } |> Async.StartImmediate

  let ps = viewPlugins ()
  let plug = ps.[0].Create ()

  console.log(plug.set Array.empty)
  console.log(plug.get ())
  console.log(plug.render ())
  console.log(plug.dispose ())
