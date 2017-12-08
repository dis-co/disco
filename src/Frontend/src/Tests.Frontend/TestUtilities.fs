namespace Disco.Web.Tests

[<AutoOpen>]
module TestUtilities =

  open Fable.Core
  open Fable.Import
  open Fable.Import.Browser

  let elById (id : string) : HTMLElement = document.getElementById id

  let mkContent () : HTMLElement =
    let el = elById "content"
    let body = document.getElementsByTagName_body().[0]
    body.appendChild el |> ignore
    el

  let cleanup (el : HTMLElement) : unit =
    el.remove()

  let withContent (wrapper : HTMLElement -> unit) : unit =
    let content = mkContent ()
    wrapper content
