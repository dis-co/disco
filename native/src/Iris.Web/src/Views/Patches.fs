[<FunScript.JS>]
module Iris.Web.Views.Patches

open FSharp.Html
open FunScript
open FunScript.TypeScript

open Iris.Web.Types
open Iris.Web.Dom
open Iris.Web.Util
open Iris.Web.Plugins

open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch
open Iris.Web.Types.View
open Iris.Web.Types.Store

//   let ps = viewPlugins ()
//   let plug = ps.[0].Create ()// 

//   console.log(plug.set Array.empty)
//   console.log(plug.get ())
//   console.log(plug.render ())
//   console.log(plug.dispose ())// 

type PatchView () =
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

  let ioboxView (iobox : IOBox) : Html =
    li <|> text (iobox.name)

  let patchView (patch : Patch) : Html =

    div <@> class' "patch" <||>
      [ h3 <|> text "Patch:"
      ; p  <|> text (patch.name)
      ; ul <||> (Array.toList patch.ioboxes |> List.map ioboxView)
      ]

  let patchList (patches : Patch list) =
    div <@> id' "patches" <||> List.map patchView patches

  interface IWidget with
    member self.render (store : Store) =
      let patches = store.GetState.Patches

      let content =
        if List.length patches = 0
        then p <|> text "Empty"
        else patchList patches

      mainView content |> htmlToVTree
