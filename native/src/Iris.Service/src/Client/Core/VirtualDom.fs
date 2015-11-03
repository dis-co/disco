[<ReflectedDefinition>]
module FunScript.VirtualDom

#nowarn "1182"

open FunScript
open FunScript.TypeScript
(*
  addChild (old : VTree) ) (new : VTree) : VTree
  --------------------------------------------------

  Creates a deep copy of a VTree and adds the handed VTree to its children.
*)
// [<JSEmit("""
//          var children = jQuery.extend(true, [], {0}.children);
//          children.push({1});
//          return jQuery.extend(true, { children: children }, {0});
//          """)>]
// let addChild (o : VTree) (n : VTree) : VTree =
//   failwith "never"

(*
  addChilden (old : VTree) ) (new : VTree array) : VTree
  --------------------------------------------------

  Creates a deep copy of a VTree and conatenates the handed VTree array with its children.
*)
// [<JSEmit("""
//          var children = jQuery.extend(true, [], {0}.children);
//          children = children.concat({1});
//          return jQuery.extend(true, { children: children }, {0});
//          """)>]
// let addChildren (o : VTree) (n : VTree array) : VTree =
//   failwith "never"
