[<FunScript.JS>]
module Iris.Web.VirtualDom

open FunScript
open FunScript.TypeScript

open Iris.Web.Html

type VProperties = {
  propList : (string * string) array
  }

type VTree =
  | VText of string
  | VNode of
    tag        : string      *
    properties : VProperties *
    children   : VTree array
  | Thunk of vnode : VTree


[<JSEmit("""
         vnode = new virtualDom.VNode({0}) 
         vnode.children = {2};
         return vnode;
         """)>]
let mkVNode node props children = VNode(node, props, children)
