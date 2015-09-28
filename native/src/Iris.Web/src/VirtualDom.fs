[<FunScript.JS>]
module Iris.Web.VirtualDom

open FunScript
open FunScript.TypeScript

open Iris.Web.Html


(* Shim class type to please type checker and not have obj() everywhere *)
type VProperties () = class end

type VTree =
  | VNode of VNodeD
  | VText of VTextD

and VNodeD (tag : string, attrs : VProperties, chdrn : VTree array) =
  let mutable tagName = tag
  let mutable attributes = attrs
  let mutable children = chdrn 

and VTextD (content : string) =
  let mutable text = content


[<JSEmit("""
         vnode = new virtualDom.VNode({0});
         vnode.properties = {1};
         vnode.children = {2};
         return vnode;
         """)>]
let mkVNode tag attrs chdrn = VNode <| new VNodeD(tag, attrs, chdrn)

[<JSEmit("""return new virtualDom.VText({0});""")>]
let mkVText txt = VText <| new VTextD(txt)

[<JSEmit("""return virtualDom.create({0});""")>]
let createElement tree = failwith "never"

(*
   Html -> VTree
*)

[<JSEmit("""return new Object();""")>]
let mkProperties () = new VProperties ()

[<JSEmit("""
         if({1} === "class")
           {0}.className = {2};
         else
           {0}[{1}] = {2};
         return {0};
         """)>]
let addAttr (o : VProperties) (n : string) (v : string) = failwith "never"

let parseAttrs attrs =
  List.fold (fun (outp : VProperties) (attr : Attribute) -> 
    match attr with
      | Pair(n, v) -> addAttr outp n v 
      | Single(n)  -> addAttr outp n n) (mkProperties ()) attrs

let rec htmlToVTree (html : Html) =
  match html with
    | Parent(n, a, ch) -> mkVNode n (parseAttrs a) (List.map htmlToVTree ch |> List.toArray)
    | Leaf(n, a)       -> mkVNode n (parseAttrs a) Array.empty
    | Literal(t)       -> mkVText t
