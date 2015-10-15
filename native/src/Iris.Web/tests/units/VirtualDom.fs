[<ReflectedDefinition>]
module Test.Units.VirtualDom

#nowarn "1182"

open FunScript
open FunScript.TypeScript
open FunScript.Mocha
open FunScript.VirtualDom
open FSharp.Html
open Iris.Web.Dom

let elById id = Globals.document.getElementById id

let mkContent () =
  let el = Globals.document.createElement "div"
  el.id <- "content"
  Globals.document.body.appendChild el |> ignore
  el
  
let cleanup el = Globals.document.body.removeChild el |> ignore

let main () =
  suite "Test.Units.VirtualDom" 

  let content = mkContent () 

  test "should add new element to list on diff/patch" <|
    (fun cb ->
     let litem = li <|> text "an item"
     let comb  = ul <||> [ litem ]

     let tree = htmlToVTree comb
     let root = createElement tree

     content.appendChild root |> ignore

     check (root.children.length = 1.) "ul item count does not match (expected 1)"
     
     let newtree = htmlToVTree <| (comb <|> litem)
     let newroot = patch root <| diff tree newtree
     
     check_cc (newroot.children.length = 2.) "ul item count does not match (expected 2)" cb)
  
  cleanup content

  test "addChild should add new element to VTree children array" <|
    (fun cb ->
     let litem = li <|> text "an item"
     let comb  = ul <||> [ litem ]

     let tree = htmlToVTree comb
     let root = createElement tree

     content.appendChild root |> ignore

     check (root.children.length = 1.) "ul item count does not match (expected 1)"
     
     let newtree = addChild tree <| htmlToVTree litem
     let newroot = patch root <| diff tree newtree
     
     check_cc (newroot.children.length = 2.) "ul item count does not match (expected 2)" cb)

  test "addChildren should add a list of new VTree children" <|
    (fun cb ->
     let litem = li <|> text "an item"
     let comb  = ul <||> [ litem ]

     let tree = htmlToVTree comb
     let root = createElement tree

     content.appendChild root |> ignore

     check (root.children.length = 1.) "ul item count does not match (expected 1)"
     
     let newtree = addChildren tree <| Array.map htmlToVTree [| litem; litem |]
     let newroot = patch root <| diff tree newtree
     
     check_cc (newroot.children.length = 3.) "ul item count does not match (expected 2)" cb)
