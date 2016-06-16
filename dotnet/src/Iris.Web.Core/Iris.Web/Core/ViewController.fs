namespace Iris.Web.Core

[<AutoOpen>]
module ViewController =

  open System
  open Fable.Core
  open Fable.Import
  open Fable.Import.JS
  open Fable.Import.Browser

  (* __        ___     _            _
     \ \      / (_) __| | __ _  ___| |_
      \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
       \ V  V / | | (_| | (_| |  __/ |_
        \_/\_/  |_|\__,_|\__, |\___|\__|
                         |___/           *)
  type IWidget<'store,'context> =
    abstract Render  : 'store -> 'context -> VTree
    abstract Dispose : unit -> unit

  (*  __     ___                ____ _        _
      \ \   / (_) _____      __/ ___| |_ _ __| |
       \ \ / /| |/ _ \ \ /\ / / |   | __| '__| |
        \ V / | |  __/\ V  V /| |___| |_| |  | |
         \_/  |_|\___| \_/\_/  \____|\__|_|  |_|RRrrr..

      ViewController orchestrates the rendering of state changes and wraps up
      both the widget tree and the rendering context needed for virtual-dom.  *)

  type ViewController<'store,'context> (widget : IWidget<'store,'context>) =
    let mutable view : IWidget<'store,'context>  = widget
    let mutable tree : VTree option = None

    let mutable root : HTMLElement = window.document.createElement "div"

    let initWith tree =
      let rootNode = createElement tree
      let body = document.getElementsByTagName_body().[0]
      body.appendChild rootNode |> ignore
      root <- rootNode

    member self.Container
      with get () = root

    (* render and patch the DOM *)
    member self.Render (store : 'store) (context : 'context) : unit =
      let newtree = view.Render store context

      match tree with
        | Some(oldtree) ->
          let update = diff oldtree newtree
          let newroot = patch root update
          root <- newroot
        | _ -> initWith newtree

      tree <- Some(newtree)

    interface IDisposable with
      member self.Dispose () =
        view.Dispose ()
        root.remove ()
