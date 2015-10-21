[<ReflectedDefinition>]
module Test.Units.Html

#nowarn "1182"

open FunScript
open FunScript.TypeScript
open FunScript.Mocha
open FunScript.VirtualDom

open Iris.Web.Test.Util
open Iris.Web.Core.Html

let main () =
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Html - basic combinators" 
  (*--------------------------------------------------------------------------*)

  test "innerText should be specified with `text'` combinator" <|
    (fun cb ->
     let content = "hello there"
     let comb = h1 <|> text content
     let elm = renderHtml comb |> createElement
     check_cc (elm.innerText = content) "content mismatch" cb)

  (*--------------------------------------------------------------------------*)
  test "class should should be specified with `class'` combinator" <|
    (fun cb ->
     let klass = "there"
     let comb = h1 <@> class' klass
     let elm = renderHtml comb |> createElement
     check_cc (elm.className = klass) "class mismatch" cb)

  (*--------------------------------------------------------------------------*)
  test "id should should be specified with `id'` combinator" <|
    (fun cb ->
     let eidee = "thou"
     let comb = h1 <@> id' eidee
     let elm = renderHtml comb |> createElement
     check_cc (elm.id = eidee) "id mismatch" cb)

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Html - composite Html * VTree" 
  (*--------------------------------------------------------------------------*)

  test "nested VTree in Html should be rendered as such"
    (fun cb ->
     let t  = div <@> class' "thing" <|> text "hello"
     let t' = renderHtml <| (h1 <|> text "hallo")

     let elm =
       renderHtml (t <|> Raw t')
       |> createElement

     check (elm.className = "thing") "should be a thing but isn't"

     elm.getElementsByTagName "h1"
     |> (fun t'' -> check_cc (t''.length = 1.0) "should have a h1 but hasn't'" cb))

  (*--------------------------------------------------------------------------*)
  test "pure html in should be rendered as such" <| 
    (fun cb ->
     let t  = div <@> class' "thing" <|> text "hello"

     let elm = renderHtml t |> createElement

     check_cc (elm.className = "thing") "should be a thing but isn't" cb)

  (*--------------------------------------------------------------------------*)
  test "VTree should be rendered as expected" <| fun cb ->
    let elm =
      Raw((div <@> id' "hello") |> renderHtml)
      |> renderHtml
      |> createElement

    check_cc (elm.id = "hello") "should have an element with id" cb
    

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.VirtualDom - basic operations"
  (*--------------------------------------------------------------------------*)

  test "should add new element to list on diff/patch" <| fun cb ->
    withContent <| fun content ->
       let litem = li <|> text "an item"
       let comb  = ul <||> [| litem |]

       let tree = renderHtml comb
       let root = createElement tree

       content.appendChild root |> ignore

       check (root.children.length = 1.) "ul item count does not match (expected 1)"

       let newtree = renderHtml <| (comb <|> litem)
       let newroot = patch root <| diff tree newtree

       check_cc (newroot.children.length = 2.) "ul item count does not match (expected 2)" cb

       cleanup content

  test "patching should update only relevant bits of the dom" <| fun cb ->
    withContent <| fun content ->
       let firstContent = "first item in the list"
       let secondContent = "second item in the list"

       let list content =
         ul <||>
            [| li <@> id' "first"  <|> text content
             ; li <@> id' "second" <|> text secondContent
             |]

       let mutable tree = list firstContent |> renderHtml
       let mutable root = createElement tree

       content.appendChild root |> ignore

       let newtree = list "harrrr i got cha" |> renderHtml
       let newroot = patch root <| diff tree newtree

       tree <- newtree
       root <- newroot

       let fst = Globals.document.getElementById "first"
       let snd = Globals.document.getElementById "second"

       check (fst.innerText <> firstContent) "the content of the first element should different but isn't"
       check_cc (snd.innerText = secondContent) "the content of the second element should be the same but isn't" cb

       let list' = list firstContent <|> (li <|> text "hmm")
       root <- patch root <| diff tree (renderHtml list')

       check (fst.innerText = firstContent) "the content of the first element should be the same but isn't"
       check (root.children.length = 3.) "the list should have 3 elements now"
       check_cc (snd.innerText = secondContent) "the content of the second element should be the same but isn't" cb

       cleanup content
