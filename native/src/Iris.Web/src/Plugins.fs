[<FunScript.JS>]
module Iris.Web.Plugins

open FunScript
open FunScript.TypeScript

open Iris.Web.Types

[<JSEmit("""
         window.IrisPlugins = window.IrisPlugins || [];
         return window.IrisPlugins;
         """)>]
let getPlugins () : ViewPlugin array = failwith "never"
