[<FunScript.JS>]
module Iris.Web.DOM

open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.virtualDom

let hello () =
  let tree = virtualDom.Globals.h("div#hello", Array.empty)
  virtualDom.Globals.create tree
