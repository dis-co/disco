namespace Iris.Service.Client.Views

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module PatchView =

  open Iris.Service.Client.Core.Html
  open Iris.Service.Client.Core.IOBox
  open Iris.Service.Client.Core.Patch
  open Iris.Service.Client.Core.ViewController
  open Iris.Service.Client.Core.Store
  open Iris.Service.Client.Core.Plugin

  type PatchView () =
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
