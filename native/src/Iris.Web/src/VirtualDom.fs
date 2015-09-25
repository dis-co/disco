[<FunScript.JS>]
module Iris.Web.VirtualDom

open FunScript
open FunScript.TypeScript

open Iris.Web.Html

type VTree =
  | VNode of VNodeD
  | VText of VTextD

and VNodeD (tag : string, chdrn : VTree array) =
  let mutable tagName  = tag
  let mutable children = chdrn 

and VTextD (content : string) =
  let mutable text = content

[<JSEmit("""
         vnode = new virtualDom.VNode({0});
         vnode.children = arr;
         return vnode;
         """)>]
let mkVNode tag arr = VNode <| new VNodeD(tag, arr)

[<JSEmit("""return new virtualDom.VText({0});""")>]
let mkVText txt = VText <| new VTextD(txt)

[<JSEmit("""return virtualDom.create({0});""")>]
let createElement tree = failwith "never"

(*
   Html -> VTree
*)
let rec htmlToVTree (html : Html) =
  match html with
    | Parent(n, a, ch) -> mkVNode n (List.map htmlToVTree ch |> List.toArray)
    | Leaf(n, a)       -> mkVNode n Array.empty
    | Literal(t)       -> mkVText t
