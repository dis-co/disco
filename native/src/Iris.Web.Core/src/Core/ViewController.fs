namespace Iris.Web.Core

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module ViewController =

  open System
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
  type IWidget<'a> =
    abstract Render  : Store<'a> -> VTree
    abstract Dispose : unit -> unit

  (*
      __     ___                ____ _        _
      \ \   / (_) _____      __/ ___| |_ _ __| |
       \ \ / /| |/ _ \ \ /\ / / |   | __| '__| |
        \ V / | |  __/\ V  V /| |___| |_| |  | |
         \_/  |_|\___| \_/\_/  \____|\__|_|  |_|RRrrr..

      ViewController orchestrates the rendering of state changes and wraps up both
      the widget tree and the rendering context needed for virtual-dom.
  *)

  type ViewController<'a> (widget : IWidget<'a>) =
    let mutable view : IWidget<'a>  = widget
    let mutable tree : VTree option = None
    let mutable root : Dom.Element  = JS.Window.Document.CreateElement "div"

    let initWith tree =
      let rootNode = createElement tree
      JQuery.Of("body").Append(rootNode) |> ignore
      root <- rootNode

    member self.Container
      with get () = root

    (* render and patch the DOM *)
    member self.Render (store : Store<'a>) : unit =
      let newtree = view.Render store

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
        JQuery.Of(root).Remove() |> ignore
