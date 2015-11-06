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
      let props = emptyProps
      let tree =
        new VTree("div", props,
                  [| new VTree("p", props,
                               [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      check_cc (tree.Children("p").Length = 1) "result should have a p" cb

    (*------------------------------------------------------------------------*)
    test "should render class attribute" <| fun cb ->
      let props = new VProps()
      props.className <- "container"

      let tree =
        new VTree("div", props,
                  [| new VTree("p", emptyProps,
                               [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      check_cc (tree.HasClass("container")) "result should have class container" cb

    (*------------------------------------------------------------------------*)
    test "should render id attribute" <| fun cb -> 
      let props = new VProps()
      props.id <- "main"

      let tree =
        new VTree("div", props,
                  [| new VTree("p", emptyProps,
                               [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      check_cc (tree.Attr("id") = "main") "result should have id main" cb

    (*------------------------------------------------------------------------*)
    test "should render style attribute" <| fun cb ->
      let props = new VProps()
      let styles = new Styles()
      styles.margin <- "40px"
      props.style <- styles

      let tree =
        new VTree("div", props,
                  [| new VTree("p", emptyProps,
                               [| new VTree("p content") |]) |])
        |> createElement
        |> JQuery.Of 

      Console.Log(props)
      Console.Log(tree)
      check_cc (tree.Css("margin") = "40px") "element should have style correctly" cb

    (*------------------------------------------------------------------------*)
    test "should patch updates in dom tree correctly" <| fun cb ->
      let props = emptyProps

      let main = new VProps()
      main.className <- "main"

      let tree =
        new VTree("div", main,
                  [| new VTree("p", emptyProps, Array.empty) |])
      let root = createElement tree

      JQuery.Of("body").Append(root) |> ignore

      let newtree =
        new VTree("div", main,
                  [| new VTree("p", emptyProps, Array.empty)
                  ;  new VTree("p", emptyProps, Array.empty) |])

      let p1 = diff tree newtree
      let newroot = patch root p1

      check (JQuery.Of(".main").Children("p").Length = 2) "should have 2 p's now"

      let anothertree =
        new VTree("div", main,
                  [| new VTree("p", emptyProps, Array.empty)
                  ;  new VTree("p", emptyProps, Array.empty)
                  ;  new VTree("p", emptyProps, Array.empty) |])

      let p2 = diff newtree anothertree
      let lastroot = patch newroot p2

      check_cc (JQuery.Of(".main").Children("p").Length = 3) "should have 2 p's now" cb
      


      
