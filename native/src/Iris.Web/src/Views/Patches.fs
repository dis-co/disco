[<FunScript.JS>]
module Iris.Web.Views.Patches

open FSharp.Html
open FunScript
open FunScript.TypeScript

open Iris.Web.Types
open Iris.Web.Dom
open Iris.Web.Util


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

  let patchView (patch : Patch) : Html =
    console.log(patch.Name)
    div <@> class' "patch" <||>
      [ h3 <|> text "Patch:"
      ; p  <|> text (patch.Name)
      ]

  let patchList (patches : Patch list) =
    div <@> id' "patches" <||> List.map patchView patches

  interface IWidget with
    member self.render (state : State) =
      let content =
        if List.length state.Patches = 0
        then p <|> text "Empty"
        else patchList state.Patches
      mainView content |> htmlToVTree
