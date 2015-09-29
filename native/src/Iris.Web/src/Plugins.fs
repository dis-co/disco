[<FunScript.JS>]
module Iris.Web.Plugins

open FunScript
open FunScript.TypeScript

type ViewPlugin (name : string ) =
  let mutable name = name

  member this.Name with get () = name

[<JSEmit("""
         window.IrisPlugins = window.IrisPlugins || [];
         return window.IrisPlugins;
         """)>]
let getPlugins () : ViewPlugin array = failwith "never"
