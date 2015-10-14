[<FunScript.JS>]
module Iris.Web.Util

#nowarn "1182"

open FunScript
open FunScript.TypeScript

open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch
open Iris.Web.Types.Socket

let console = Globals.console
let document = Globals.document
let setInterval = Globals.setInterval
let JSON = Globals.JSON
let Math = Globals.Math

[<JSEmit("""return JSON.stringify({0});""")>]
let toString (i : obj) : string = failwith "never"

[<JSEmit("""
         return {0}.payload
         """)>]
let parsePatch (msg : Message) : Patch = failwith "never"

[<JSEmit("""
         return {0}.payload
         """)>]
let parseIOBox (msg : Message) : IOBox = failwith "never"
