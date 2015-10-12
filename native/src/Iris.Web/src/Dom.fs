[<ReflectedDefinition>]
module Iris.Web.Dom

open FunScript.VirtualDom
open FSharp.Html

type CompositeDom =
  | Pure     of Html
  | NestedP  of Html * VTree list // Html parent with VTree children
  | NestedR  of VTree * Html list // VTree parent with 
  | NestedC  of Html * CompositeDom
  | Rendered of VTree

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

let rec compToVTree (tree : CompositeDom) : VTree =
  match tree with
    | Pure(html)         -> htmlToVTree html
    | NestedP(html, vts) -> addChildren (htmlToVTree html) (List.toArray vts)
    | NestedR(vt, html)  -> addChildren vt (List.map htmlToVTree html |> List.toArray)
    | NestedC(html, cts) -> addChildren (htmlToVTree html) [| compToVTree cts |]
    | Rendered(t)        -> t


// let render (state : AppState) =
//   let newtree =  |> htmlToVTree
//   let patches = diff view.tree newtree
//   rootNode := patch !rootNode patches
//   tree := newtree

// let msg = ref "not connteced"
// let tree = ref (htmlToVTree (render !msg)) 
// let rootNode = ref (createElement !tree)
