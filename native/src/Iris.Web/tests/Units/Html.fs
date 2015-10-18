[<ReflectedDefinition>]
module Test.Units.Html

#nowarn "1182"

open FunScript
open FunScript.TypeScript
open FunScript.Mocha
open FunScript.VirtualDom

open FSharp.Html
open Iris.Web.Dom

let main () =
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Html - basic combinators" 
  (*--------------------------------------------------------------------------*)

  test "innerText should be specified with `text'` combinator" <|
    (fun cb ->
     let content = "hello there"
     let comb = h1 <|> text content
     let elm = htmlToVTree comb |> createElement
     check_cc (elm.innerText = content) "content mismatch" cb)

  (*--------------------------------------------------------------------------*)
  test "class should should be specified with `class'` combinator" <|
    (fun cb ->
     let klass = "there"
     let comb = h1 <@> class' klass
     let elm = htmlToVTree comb |> createElement
     check_cc (elm.className = klass) "class mismatch" cb)

  (*--------------------------------------------------------------------------*)
  test "id should should be specified with `id'` combinator" <|
    (fun cb ->
     let eidee = "thou"
     let comb = h1 <@> id' eidee
     let elm = htmlToVTree comb |> createElement
     check_cc (elm.id = eidee) "id mismatch" cb)

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Html - composite Html * VTree" 
  (*--------------------------------------------------------------------------*)

  test "nested VTree in Html should be rendered as such"
    (fun cb ->
     let t  = div <@> class' "thing" <|> text "hello"
     let t' = htmlToVTree <| (h1 <|> text "hallo")

     let elm =
       NestedP(t, t' :: [])
       |> compToVTree 
       |> createElement

     check (elm.className = "thing") "should be a thing but isn't"

     elm.getElementsByTagName "h1"
     |> (fun t'' -> check_cc (t''.length = 1.0) "should have a h1 but hasn't'" cb))

  (*--------------------------------------------------------------------------*)
  test "pure html in should be rendered as such" <| 
    (fun cb ->
     let t  = div <@> class' "thing" <|> text "hello"

     let elm =
       Pure(t)
       |> compToVTree 
       |> createElement

     check_cc (elm.className = "thing") "should be a thing but isn't" cb)

  (*--------------------------------------------------------------------------*)
  test "nested Html in VTree parent should be rendered as such" <|
    (fun cb ->
     let t = (div <@> class' "thing") |> htmlToVTree
     let t' = h1 <|> text "hi"

     let elm =
       NestedR(t, t' :: [])
       |> compToVTree
       |> createElement

     elm.getElementsByTagName "h1"
     |> (fun els -> check_cc (els.length = 1.0) "should have exactly one h1" cb))

  (*--------------------------------------------------------------------------*)
  test "nested CompositeDom in Html parent should be rendered as such" <|
    (fun cb ->
     let thing1 =
       NestedR((div <@> class' "thing") |> htmlToVTree, [ h1 <|> text "Hello " ])

     let thing2 =
       NestedP(div <@> class' "thing", [ (h1 <|> text "Bye") |> htmlToVTree ])

     let elm =
       NestedC(div <@> id' "super", [ thing1; thing2 ])
       |> compToVTree
       |> createElement
     
     check (elm.id = "super") "elms id should be `super`"

     elm.getElementsByClassName "thing"
     |> (fun els -> check (els.length = 2.0) "should have 2 `things`")

     elm.getElementsByTagName "h1"
     |> (fun els -> check_cc (els.length = 2.0) "should have 2 `h1` tags" cb))

  (*--------------------------------------------------------------------------*)
  test "VTree should be rendered as expected" <|
    (fun cb ->
     let elm =
       Rendered((div <@> id' "hello") |> htmlToVTree)
       |> compToVTree
       |> createElement

     check_cc (elm.id = "hello") "should have an element with id" cb)
    
