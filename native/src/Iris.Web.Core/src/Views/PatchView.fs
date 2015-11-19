namespace Iris.Web.Views

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
[<RequireQualifiedAccess>]
module Patches =

  open Iris.Web.Core.Html
  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.ViewController
  open Iris.Web.Core.Store
  open Iris.Web.Core.State
  open Iris.Web.Core.Plugin

  type Root () =
    let mutable plugins = new Plugins ()

    let header = h1 <|> text "All Patches"

    let footer = div <@> class' "foot" <|> hr

    let ioboxView (iobox : IOBox) : Html =
      if not (plugins.has iobox)
      then plugins.add iobox

      let container = li <|> (strong <|> text (iobox.name))

      match plugins.get iobox with
        | Some(instance) -> container <|> Raw(instance.Render iobox)
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

    (*-------------------- RENDERER --------------------*)

    interface IWidget<State> with
      member self.Dispose () = ()

      member self.Render (store : Store<State>) =
        store.State.Patches
        |> List.toArray
        |> (fun patches ->
              if Array.length patches = 0
              then p <|> text "Empty"
              else patchList patches)
        |> mainView
        |> renderHtml
