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

  test "class should should be specified with `class'` combinator" <|
    (fun cb ->
     let klass = "there"
     let comb = h1 <@> class' klass
     let elm = htmlToVTree comb |> createElement
     check_cc (elm.className = klass) "class mismatch" cb)

  test "id should should be specified with `id'` combinator" <|
    (fun cb ->
     let eidee = "thou"
     let comb = h1 <@> id' eidee
     let elm = htmlToVTree comb |> createElement
     check_cc (elm.id = eidee) "id mismatch" cb)

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Html - interleaved Html & VTree" 
  (*--------------------------------------------------------------------------*)

  pending "pure html in should be rendered as such"
  pending "nested VTree in Html should be rendered as such"
  pending "nested Html in VTree parent should be rendered as such"
  pending "nested CompositeDom in Html parent should be rendered as such"
  pending "VTree should be rendered as expected"

