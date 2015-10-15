[<ReflectedDefinition>]
module Iris.Web.Types.View

open Iris.Web.Types.Store
open FunScript.VirtualDom
open FunScript.TypeScript

(*  
    __        ___     _            _   
    \ \      / (_) __| | __ _  ___| |_ 
     \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
      \ V  V / | | (_| | (_| |  __/ |_ 
       \_/\_/  |_|\__,_|\__, |\___|\__|
                        |___/          
*)
type IWidget =
  abstract render : Store -> VTree

(*
    __     ___                ____ _        _ 
    \ \   / (_) _____      __/ ___| |_ _ __| |
     \ \ / /| |/ _ \ \ /\ / / |   | __| '__| |
      \ V / | |  __/\ V  V /| |___| |_| |  | |
       \_/  |_|\___| \_/\_/  \____|\__|_|  |_|RRrrr..

    ViewController orchestrates the rendering of state changes and wraps up both
    the widget tree and the rendering context needed for virtual-dom.
*)

type ViewController (widget : IWidget) =
  let mutable view : IWidget            = widget 
  let mutable tree : VTree option       = None
  let mutable root : HTMLElement option = None 
  
  member self.init tree = 
    let rootNode = createElement tree
    Globals.document.body.appendChild(rootNode) |> ignore
    root <- Some(rootNode)

  (* render and patch the DOM *)
  member self.render (store : Store) : unit =  
    let newtree = view.render store

    match tree with
      | Some(oldtree) -> 
        match root with
          | Some(oldroot) -> 
            let update = diff oldtree newtree
            root <- Some(patch oldroot update)
          | _ -> self.init newtree
      | _ -> self.init newtree

    tree <- Some(newtree)
