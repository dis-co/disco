[<ReflectedDefinition>]
module FunScript.VirtualDom

open FunScript
open FunScript.TypeScript

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
let createElement tree : Node = failwith "never"

[<JSEmit("""return virtualDom.diff({0}, {1});""")>]
let diff tree newtree = failwith "never"

[<JSEmit("""return virtualDom.patch({0}, {1});""")>]
let patch tree patches : Node = failwith "never"

(* Html -> VTree *)
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

[<JSEmit("""{0}.children.push({1});""")>]
let addChild (o : VTree) (n : VTree) = failwith "never"

[<JSEmit("""{0}.children = {0}.children.concat({1});""")>]
let addChildren (o : VTree) (n : VTree array) = failwith "never"
