[<ReflectedDefinition>]
module FunScript.VirtualDom

open FunScript
open FunScript.TypeScript

(* Shim class type to please type checker and not have obj() everywhere *)
type VProperties () = class end
type VTree       () = class end
type VPatch      () = class end

[<JSEmit("""return new virtualDom.VNode({0}, {1}, {2})""")>]
let mkVNode (tag : string) (props : VProperties) (chdrn : VTree array) : VTree =
  failwith "never"

[<JSEmit("""return new virtualDom.VText({0})""")>]
let mkVText (txt : string) : VTree =
  failwith "never"

[<JSEmit("""return virtualDom.create({0})""")>]
let createElement (tree : VTree) : Node =
  failwith "never"

[<JSEmit("""return virtualDom.diff({0}, {1})""")>]
let diff (tree : VTree) (newtree : VTree) : VPatch =
  failwith "never"

[<JSEmit("""return virtualDom.patch({0}, {1})""")>]
let patch (root : Node) (patch : VPatch) : Node =
  failwith "never"

[<JSEmit("""return new Object()""")>]
let mkProperties () : VProperties =
  failwith "never"

[<JSEmit("""
         if({1} === "class")
           {0}.className = {2};
         else
           {0}[{1}] = {2};
         return {0};
         """)>]
let addAttr (o : VProperties) (n : string) (v : string) : VProperties =
  failwith "never"

[<JSEmit("""
         {0}.children.push({1});
         return {0}
         """)>]
let addChild (o : VTree) (n : VTree) : VTree =
  failwith "never"

[<JSEmit("""
         {0}.children = {0}.children.concat({1});
         return {0}
         """)>]
let addChildren (o : VTree) (n : VTree array) : VTree =
  failwith "never"
