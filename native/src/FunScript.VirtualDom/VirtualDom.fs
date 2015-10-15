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
let createElement (tree : VTree) : HTMLElement =
  failwith "never"

[<JSEmit("""return virtualDom.diff({0}, {1})""")>]
let diff (tree : VTree) (newtree : VTree) : VPatch =
  failwith "never"

[<JSEmit("""return virtualDom.patch({0}, {1})""")>]
let patch (root : HTMLElement) (patch : VPatch) : HTMLElement =
  failwith "never"

[<JSEmit("""return new Object()""")>]
let mkProperties () : VProperties =
  failwith "never"

(*
  addAttr (props : VProperties) (name : string) (value : string) : VProperties
  --------------------------------------------------

  Create a deep copy of a VTree and add the supplied property to it.
*)
[<JSEmit("""
         var copy = jQuery.extend(true, {}, {0});
         if({1} === "class")
           copy.className = {2};
         else
           copy[{1}] = {2};
         return copy;
         """)>]
let addAttr (o : VProperties) (n : string) (v : string) : VProperties =
  failwith "never"

(*
  addChild (old : VTree) ) (new : VTree) : VTree
  --------------------------------------------------

  Creates a deep copy of a VTree and adds the handed VTree to its children.
*)
[<JSEmit("""
         var children = jQuery.extend(true, [], {0}.children);
         children.push({1});
         return jQuery.extend(true, { children: children }, {0});
         """)>]
let addChild (o : VTree) (n : VTree) : VTree =
  failwith "never"

(*
  addChilden (old : VTree) ) (new : VTree array) : VTree
  --------------------------------------------------

  Creates a deep copy of a VTree and conatenates the handed VTree array with its children.
*)
[<JSEmit("""
         var children = jQuery.extend(true, [], {0}.children);
         children = children.concat({1});
         return jQuery.extend(true, { children: children }, {0});
         """)>]
let addChildren (o : VTree) (n : VTree array) : VTree =
  failwith "never"
