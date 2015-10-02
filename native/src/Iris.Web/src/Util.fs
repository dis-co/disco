[<FunScript.JS>]
module Iris.Web.Util

open FunScript
open FunScript.TypeScript

[<JSEmit("""return JSON.stringify({0});""")>]
let toString (i : obj) = ""

let console = Globals.console
let document = Globals.document
let setInterval = Globals.setInterval

let JSON = Globals.JSON
