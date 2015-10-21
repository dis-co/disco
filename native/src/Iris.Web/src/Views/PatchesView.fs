[<FunScript.JS>]
module Iris.Web.Views.PatchesView

#nowarn "1182"

open FunScript
open FunScript.TypeScript

open Iris.Web.Util

open Iris.Web.Core.Html
open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch
open Iris.Web.Core.ViewController
open Iris.Web.Core.Store
open Iris.Web.Core.Plugin

type PatchesView () =
  let mutable plugins = new Plugins ()

  let header = h1 <|> text "All Patches"

  let footer = div <@> class' "foot" <|> hr

  let ioboxView (iobox : IOBox) : Html =
    if not (plugins.has iobox)
    then plugins.add iobox

    let container = li <|> (strong <|> text (iobox.name))

    match plugins.get iobox with
      | Some(instance) -> container <|> Raw(instance.render iobox)
      |  _             -> container 

  let patchView (patch : Patch) : Html =
    let container = 
      div <@> id' patch.id <@> class' "patch" <||>
        [| h3 <|> text "Patch:"
         ; p  <|> text (patch.name)
         |]

    container <||> Array.map ioboxView patch.ioboxes

  let patchList (patches : Patch array) =
    let container = div <@> id' "patches"
    container <||> Array.map patchView patches

  let mainView (content : Html) : Html =
    div <@> id' "main" <||> [| header ; content ; footer |]

  (* RENDERER *)

  interface IWidget with
    member self.Dispose () = ()
      
    member self.render (store : Store) =
      store.state.Patches
      |> List.toArray
      |> (fun patches ->
            if Array.length patches = 0
            then p <|> text "Empty"
            else patchList patches)
      |> mainView
      |> renderHtml

(*

let mainView content =
  NestedP(div <@> id' "main",
          List.map compToVTree
            [ header
            ; content
            ; footer
            ])

let ioboxView (iobox : IOBox) : CompositeDom =
  Globals.console.log("ioboxView function; value: " + iobox.slices.[0].value)
  NestedP(li <|> (strong <|> text (iobox.name))
             <|> (p  <|> text "Values:"),
          [ sliceView iobox |> htmlToVTree ])
          //[ plugins.render iobox ])

let patchView (patch : Patch) : CompositeDom =
  for iobox in patch.ioboxes do
    if not (plugins.has iobox)
    then plugins.add iobox
  let b = div <@> class' "patch" <||>
            [ h3 <|> text "Patch:"
            ; p  <|> text (patch.name)
            ]
  let lst = addChildren (htmlToVTree ul) (Array.map (ioboxView >> compToVTree) patch.ioboxes)
  NestedP(b, [ lst ])

let patchList (patches : Patch list) =
  NestedP(div <@> id' "patches", List.map (patchView >> compToVTree) patches)

(* in rendering finally do *)
let content =
  if List.length patches = 0
  then p <|> text "Empty" |> Pure
  else patchList patches
mainView content |> compToVTree

*)
