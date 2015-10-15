[<ReflectedDefinition>]
module Iris.Web.Test.Util

open FunScript
open FunScript.TypeScript

let elById (id : string) : HTMLElement = Globals.document.getElementById id

let mkContent () : HTMLElement =
  let el = Globals.document.createElement "div"
  el.id <- "content"
  Globals.document.body.appendChild el |> ignore
  el
  
let cleanup (el : HTMLElement) : unit =
  Globals.document.body.removeChild el |> ignore

let withContent (wrapper : HTMLElement -> unit) : unit =
  let content = mkContent () 
  wrapper content
