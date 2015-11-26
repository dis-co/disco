namespace Iris.Web.Views

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
[<RequireQualifiedAccess>]
module Patches =

  open Iris.Web.Core
  open Iris.Core.Types

  type Root () =
    let mutable plugins = new Plugins ()

    let header = h1 <|> text "All Patches"

    let footer = div <@> class' "foot" <|> hr

    let ioboxView (context : ClientContext) (iobox : IOBox) : Html =
      if not (plugins.has iobox)
      then plugins.add iobox

      let container = li <|> (strong <|> text (iobox.Name))

      match plugins.get iobox with
        | Some(instance) -> container <|> Raw(instance.Render iobox)
        |  _             -> container

    let patchView (context : ClientContext) (patch : Patch) : Html =
      let container =
        div <@> id' patch.Id <@> class' "patch" <||>
          [| h3 <|> text "Patch:"
           ; p  <|> text (patch.Name)
           |]

      container <||> Array.map (ioboxView context) patch.IOBoxes

    let patchList (context : ClientContext) (patches : Patch array) =
      let container = div <@> id' "patches"
      container <||> Array.map (patchView context) patches

    let mainView (content : Html) : Html =
      div <@> id' "main" <||> [| header ; content ; footer |]

    (*-------------------- RENDERER --------------------*)

    interface IWidget<State,ClientContext> with
      member self.Dispose () = ()

      member self.Render (state : State) (context : ClientContext) =
        state.Patches
        |> (fun patches ->
              if Array.length patches = 0
              then p <|> text "Empty"
              else patchList context patches)
        |> mainView
        |> renderHtml
