namespace Iris.Web.Core

module Storage =

  open Fable.Core
  open Fable.Core.JsInterop
  open Fable.PowerPack
  open Fable.Import

  [<PassGenerics>]
  let load<'T> (key: string) =
    let g = Fable.Import.Browser.window
    match g.localStorage.getItem(key) with
    | null -> None
    | value -> ofJson<'T> !!value |> Some

  let save (key: string) (value: obj) =
    let g = Fable.Import.Browser.window
    g.localStorage.setItem(key, toJson value)
