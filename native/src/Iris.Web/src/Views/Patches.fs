[<FunScript.JS>]
module Iris.Web.Views.Patches

open FSharp.Html
open FunScript
open FunScript.TypeScript
open FunScript.VirtualDom

open Iris.Web.Types
open Iris.Web.Dom
open Iris.Web.Util
open Iris.Web.Plugins

open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch
open Iris.Web.Types.View
open Iris.Web.Types.Store
open Iris.Web.Types.Plugin

//   console.log(plug.set Array.empty)
//   console.log(plug.get ())
//   console.log(plug.render ())
//   console.log(plug.dispose ())// 

type PatchView () =
  let mutable plugins = new Plugins ()

  let header : CompositeDom = h1 <|> text "All Patches" |> Pure
  
  let footer : CompositeDom =
    div <@> class' "foot" <||>
      [ hr
      ; span <|> text "yep"
      ]
    |> Pure
  
  let mainView content =
    NestedP(div <@> id' "main",
            List.map compToVTree
              [ header
              ; content 
              ; footer
              ])
  
  let sliceView (slice : Slice) =
    li <|> text (sprintf "Index: %d Value: %s" slice.idx slice.value)
  
  let ioboxView (iobox : IOBox) : CompositeDom =
    NestedP(li <|> (strong <|> text (iobox.name))
               <|> (p  <|> text "Values:"),
            [ plugins.tree iobox ])
  
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

  (* RENDERER *)

  interface IWidget with
    member self.render (store : Store) =
      let patches = store.state.Patches

      plugins.updateAll patches |> ignore
      plugins.render () |> ignore

      let content =
        if List.length patches = 0
        then p <|> text "Empty" |> Pure
        else patchList patches

      mainView content |> compToVTree
