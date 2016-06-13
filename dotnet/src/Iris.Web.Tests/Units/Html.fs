namespace Test.Units

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.Mocha

[<JavaScript>]
[<RequireQualifiedAccess>]
module Html =

  open Iris.Web.Tests.Util
  open Iris.Web.Core.Html

  let main () =
    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Html - basic combinators"
    (*--------------------------------------------------------------------------*)

    test "innerText should be specified with `text'` combinator" <|
      (fun cb ->
       let content = "hello there"
       let comb = h1 <|> text content
       let elm = renderHtml comb |> createElement |> JQuery.Of
       check_cc (elm.Html() = content) "content mismatch" cb)

    (*--------------------------------------------------------------------------*)
    test "class should should be specified with `class'` combinator" <|
      (fun cb ->
       let klass = "there"
       let comb = h1 <@> class' klass
       let elm = renderHtml comb |> createElement |> JQuery.Of
       check_cc (elm.Attr("class") = klass) "class mismatch" cb)

    (*--------------------------------------------------------------------------*)
    test "id should should be specified with `id'` combinator" <|
      (fun cb ->
       let eidee = "thou"
       let comb = h1 <@> id' eidee
       let elm = renderHtml comb |> createElement |> JQuery.Of
       check_cc (elm.Attr("id") = eidee) "id mismatch" cb)

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
         |> JQuery.Of

       check (elm.Attr("class") = "thing") "should be a thing but isn't"

       elm.Children("h1")
       |> (fun t'' -> check_cc (t''.Length = 1) "should have a h1 but hasn't'" cb))

    (*--------------------------------------------------------------------------*)
    test "pure html in should be rendered as such" <|
      (fun cb ->
       let t  = div <@> class' "thing" <|> text "hello"

       let elm = renderHtml t |> createElement |> JQuery.Of

       check_cc (elm.Attr("class") = "thing") "should be a thing but isn't" cb)

    (*--------------------------------------------------------------------------*)
    test "VTree should be rendered as expected" <| fun cb ->
      let elm =
        Raw((div <@> id' "hello") |> renderHtml)
        |> renderHtml
        |> createElement
        |> JQuery.Of

      check_cc (elm.Attr("id") = "hello") "should have an element with id" cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Html - event callbacks"
    (*--------------------------------------------------------------------------*)

    test "callback function should be rendered and called" <| fun cb ->
      let elm =
        div <@> onClick (fun ev -> check_cc true "should have been called" cb)
        |> renderHtml
        |> createElement
        |> JQuery.Of
        
      elm.Click () |> ignore

