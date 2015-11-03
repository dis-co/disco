[<ReflectedDefinition>]
module Iris.Web.Core.ViewController

open System

open FunScript
open FunScript.TypeScript

open Iris.Web.Core.Store
open Iris.Web.Core.Html

(*  
    __        ___     _            _   
    \ \      / (_) __| | __ _  ___| |_ 
     \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
      \ V  V / | | (_| | (_| |  __/ |_ 
       \_/\_/  |_|\__,_|\__, |\___|\__|
                        |___/          
*)
type IWidget =
  inherit IDisposable
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
  let mutable view : IWidget      = widget 
  let mutable tree : VTree option = None
  let mutable root : HTMLElement  = Globals.document.createElement "div"

  member self.Container
    with get () = root
  
  member self.init tree = 
    let rootNode = createElement tree
    Globals.document.body.appendChild(rootNode) |> ignore
    root <- rootNode

  (* render and patch the DOM *)
  member self.render (store : Store) : unit =
    let newtree = view.render store

    match tree with
      | Some(oldtree) -> 
        let update = diff oldtree newtree
        let newroot = patch root update
        root <- newroot
      | _ -> self.init newtree

    tree <- Some(newtree)

  interface IDisposable with
    member self.Dispose () =
      widget.Dispose ()
      root.parentNode.removeChild root |> ignore
