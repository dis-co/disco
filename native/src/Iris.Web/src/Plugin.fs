[<FunScript.JS>]
module Iris.Web.Plugin

open FunScript
open FunScript.TypeScript

type ViewPlugin (name : string ) =
  let mutable name = name

[<JSEmit("""
         window.IrisPlugins = window.IrisPlugins || [];
         return window.IrisPlugins;
         """)>]
let getPlugins () : ViewPlugin list = failwith "never"
