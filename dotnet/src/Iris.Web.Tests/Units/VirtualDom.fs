namespace Test.Units

[<RequireQualifiedAccess>]
module VirtualDom =

  open Iris.Web.Tests
  open Iris.Web.Core.Html
  open Fable.Core
  open Fable.Import

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
      
    (*------------------------------------------------------------------------*)
    test "should add new element to list on diff/patch" <| fun cb ->
      let litem = li <|> text "an item"
      let comb  = ul <||> [| litem |]

      let tree = renderHtml comb
      let root = createElement tree

      JQuery.Of("body").Append(root) |> ignore

      check (JQuery.Of(root).Children().Length = 1) "ul item count does not match (expected 1)"

      let newtree = renderHtml <| (comb <|> litem)
      let newroot = patch root <| diff tree newtree

      check_cc (JQuery.Of(newroot).Children().Length = 2) "ul item count does not match (expected 2)" cb

      JQuery.Of(newroot).Remove() |> ignore

    (*------------------------------------------------------------------------*)
    test "patching should update only relevant bits of the dom" <| fun cb ->
      let firstContent = "first item in the list"
      let secondContent = "second item in the list"

      let list content =
        ul <||>
          [| li <@> id' "first"  <|> text content
           ; li <@> id' "second" <|> text secondContent
           |]

      let mutable tree = list firstContent |> renderHtml
      let mutable root = createElement tree

      JQuery.Of("body").Append(root) |> ignore

      let newtree = list "harrrr i got cha" |> renderHtml
      let newroot = patch root <| diff tree newtree

      tree <- newtree
      root <- newroot

      let fst = JQuery.Of("#first")
      let snd = JQuery.Of("#second")

      check (fst.Html() <> firstContent) "the content of the first element should different but isn't"
      check (snd.Html() = secondContent) "the content of the second element should be the same but isn't"

      let list' = list firstContent <|> (li <|> text "hmm")
      root <- patch root <| diff tree (renderHtml list')

      check (fst.Html() = firstContent) "the content of the first element should be the same but isn't"
      check (JQuery.Of(root).Children().Length = 3) "the list should have 3 elements now"
      check_cc (snd.Html() = secondContent) "the content of the second element should be the same but isn't" cb

      JQuery.Of(root).Remove() |> ignore
