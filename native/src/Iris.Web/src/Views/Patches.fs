[<FunScript.JS>]
module Iris.Web.Views.Patches

#nowarn "1182"

open FSharp.Html
open FunScript
open FunScript.TypeScript
open FunScript.VirtualDom

open Iris.Web.Dom
open Iris.Web.Util

open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch
open Iris.Web.Core.View
open Iris.Web.Core.Store
open Iris.Web.Core.Plugin

//   console.log(plug.set Array.empty)
//   console.log(plug.get ())
//   console.log(plug.render ())
//   console.log(plug.dispose ())//

type PatchView () =
  let mutable plugins = new Plugins ()

  let header = h1 <|> text "All Patches"

  let footer =
    div <@> class' "foot" <||>
      [ hr
      ; span <|> text "yep"
      ]

  let sliceView (iobox : IOBox) =
    div <@> id' iobox.id
        <|> (p <@> class' "slice" <|> text (iobox.slices.[0].value))

  let ioboxView1 (iobox : IOBox) : Html =
    li <|> (strong <|> text (iobox.name))
       <|> (p  <|> text "Values:")
       <|> sliceView iobox

  let patchView1 (patch : Patch) : Html =
    div <@> class' "patch" <||>
      [ h3 <|> text "Patch:"
      ; p  <|> text (patch.name)
      ; ul <||> (Array.map ioboxView1 patch.ioboxes |> Array.toList)
      ]

  let patchList1 (patches : Patch list) =
    div <@> id' "patches" <||> List.map patchView1 patches

  let mainView1 content =
    div <@> id' "main" <||> [ header ; content ; footer ]

  (* RENDERER *)

  interface IWidget with
    member self.render (store : Store) =
      let patches = store.state.Patches

      let content =
        if List.length patches = 0
        then p <|> text "Empty"
        else patchList1 patches

      mainView1 content |> htmlToVTree

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
