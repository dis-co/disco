namespace Test.Units

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.Mocha
open Iris.Web.Core.Html

[<JavaScript>]
[<RequireQualifiedAccess>]
module VirtualDom =

  let main () =

    (*------------------------------------------------------------------------*)
    suite "Test.Units.VirtualDom"
    (*------------------------------------------------------------------------*)

    test "should render dom elements correctly" <| fun cb ->
      let tree =
        new VTree("div", emptyProps,
                  [| new VTree("My beautiful tree")
                  ;  new VTree("p", emptyProps, [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 
      check_cc (tree.Children("p").Length = 1) "result should have a p" cb

    test "should render class attribute" <| fun cb ->
      let props = new VProps()
      props.id <- "main"
      props.className <- "container"

      let tree =
        new VTree("div", props,
                  [| new VTree("My beautiful tree")
                  ;  new VTree("p", emptyProps, [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      check_cc (tree.HasClass("container")) "result should have class container" cb

    test "should render id attribute" <| fun cb -> 
      let props = new VProps()
      props.id <- "main"
      props.className <- "container"

      let tree =
        new VTree("div", props,
                  [| new VTree("My beautiful tree")
                  ;  new VTree("p", emptyProps, [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      check_cc (tree.HasClass("container")) "result should have class container" cb

    pending "should render style attribute"
    pending "should patch updates in dom tree correctly"
