﻿[<ReflectedDefinition>]
module Iris.Web.Dom

open FunScript.VirtualDom
open FSharp.Html

let parseAttrs attrs =
  List.fold (fun (outp : VProperties) (attr : Attribute) -> 
    match attr with
      | Pair(n, v) -> addAttr outp n v 
      | Single(n)  -> addAttr outp n n) (mkProperties ()) attrs

let rec htmlToVTree (html : Html) =
  let mkNode n a ch =
    mkVNode n (parseAttrs a) (List.map htmlToVTree ch |> List.toArray)

  match html with
    | Parent(n, a, ch) -> mkNode n a ch
    | Leaf(n, a)       -> mkNode n a []
    | Literal(t)       -> mkVText t
