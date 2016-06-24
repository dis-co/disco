namespace Test.Units

[<RequireQualifiedAccess>]
module VirtualDom =

  open Iris.Web.Tests
  open Iris.Web.Core
  open Iris.Web.Core.Html
  open Fable.Core
  open Fable.Import

  let main () =

    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.VirtualDom"
    (* ------------------------------------------------------------------------ *)

    test "should render dom elements correctly" <| fun cb ->
      let tree =
        VNode "div" [] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      check_cc (childrenByTag "p" tree |> fun els -> els.length = 1.0) "result should have a p" cb

    (* ------------------------------------------------------------------------ *)
    test "should render class attribute" <| fun cb ->
      let tree =
        VNode "div" [ Class "container" ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      check_cc (hasClass "container" tree) "result should have class container" cb

    (* ------------------------------------------------------------------------ *)
    test "should render id attribute" <| fun cb ->
      let tree =
        VNode "div" [ ElmId "main" ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      check_cc (tree.id = "main") "result should have id main" cb

    (* ------------------------------------------------------------------------ *)
    test "should render style attribute" <| fun cb ->
      let tree =
        VNode "div" [ Style [ Margin "40px"] ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      check_cc (getStyle "margin" tree = Some "40px") "element should have style correctly" cb

    (* ------------------------------------------------------------------------ *)
    test "should patch updates in dom tree correctly" <| fun cb ->
      let tree =
        VNode "div" [ Class "main" ] [|
          VNode "p" [] Array.empty
        |]

      let root = createElement tree

      getByTag "body"
      |> nthElement 0
      |> appendChild root
      |> ignore

      let newtree =
        VNode "div" [ Class "main" ]
              [| VNode "p" [] Array.empty
                 VNode "p" [] Array.empty |]

      let p1 = diff tree newtree
      let newroot = patch root p1

      check (getByClass "main" |> nthElement 0 |> childrenByTag "p" |> fun els -> els.length = 2.0) "should have 2 p's now"

      let anothertree =
        VNode "div" [ Class "main"]
              [| VNode "p" [] Array.empty
                 VNode "p" [] Array.empty
                 VNode "p" [] Array.empty |]

      let p2 = diff newtree anothertree
      let lastroot = patch newroot p2

      check_cc (getByClass "main"|> nthElement 0 |> childrenByTag "p" |> fun els -> els.length = 3.0) "should have 2 p's now" cb

    (* ------------------------------------------------------------------------ *)
    test "should add new element to list on diff/patch" <| fun cb ->
      let litem = Li [] [| Text "an item" |]
      let comb  = Ul [] [| litem |]

      let tree = renderHtml comb
      let root = createElement tree

      getByTag "body"
      |> nthElement 0
      |> appendChild root
      |> ignore

      check (root.children.length = 1.0) "ul item count does not match (expected 1)"

      let newtree = renderHtml <| (comb <+ litem )
      let newroot = patch root <| diff tree newtree

      check_cc (newroot.children.length = 2.0) "ul item count does not match (expected 2)" cb

      newroot.remove() |> ignore

    (* ------------------------------------------------------------------------ *)
    test "patching should update only relevant bits of the dom" <| fun cb ->
      let firstContent = "first item in the list"
      let secondContent = "second item in the list"

      let list content =
        Ul [] [| Li [ ElmId "first"  ] [| Text content |]
                 Li [ ElmId "second" ] [| Text secondContent |] |]

      let mutable tree = list firstContent |> renderHtml
      let mutable root = createElement tree

      getByTag "body"
      |> nthElement 0
      |> appendChild root
      |> ignore

      let newtree = list "harrrr i got cha" |> renderHtml
      let newroot = patch root <| diff tree newtree

      tree <- newtree
      root <- newroot

      let fst = getById "first" |> Option.get
      let snd = getById "second" |> Option.get

      check (fst.innerHTML <> firstContent) "the content of the first element should different but isn't"
      check (snd.innerHTML = secondContent) "the content of the second element should be the same but isn't"

      let list' = list firstContent <+ Li [] [| Text "hmm" |]
      root <- patch root <| diff tree (renderHtml list')

      check (fst.innerHTML = firstContent) "the content of the first element should be the same but isn't"
      check (root.children.length = 3.0) "the list should have 3 elements now"
      check_cc (snd.innerHTML = secondContent) "the content of the second element should be the same but isn't" cb

      root.remove()
