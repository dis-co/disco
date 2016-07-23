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

    test "should render dom elements correctly" <| fun finish ->
      let tree =
        VNode "div" [] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      equals 1.0 (childrenByTag "p" tree |> fun els -> els.length)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "should render class attribute" <| fun finish ->
      let tree =
        VNode "div" [ Class "container" ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      equals true (hasClass "container" tree)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "should render id attribute" <| fun finish ->
      let tree =
        VNode "div" [ ElmId "main" ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      equals "main" tree.id
      finish()

    (* ------------------------------------------------------------------------ *)
    test "should render style attribute" <| fun finish ->
      let tree =
        VNode "div" [ Style [ Margin "40px"] ] [|
          VNode "p" [] [|
            VText "p content"
          |]
        |]
        |> createElement

      equals (Some "40px") (getStyle "margin" tree)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "should patch updates in dom tree correctly" <| fun finish ->
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

      equals 2.0 (getByClass "main" |> nthElement 0 |> childrenByTag "p" |> fun els -> els.length)

      let anothertree =
        VNode "div" [ Class "main"]
              [| VNode "p" [] Array.empty
                 VNode "p" [] Array.empty
                 VNode "p" [] Array.empty |]

      let p2 = diff newtree anothertree
      let lastroot = patch newroot p2

      equals 3.0 (getByClass "main"|> nthElement 0 |> childrenByTag "p" |> fun els -> els.length)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "should add new element to list on diff/patch" <| fun finish ->
      let litem = Li [] [| Text "an item" |]
      let comb  = Ul [] [| litem |]

      let tree = renderHtml comb
      let root = createElement tree

      getByTag "body"
      |> nthElement 0
      |> appendChild root
      |> ignore

      equals 1.0 root.children.length

      let newtree = renderHtml <| (comb <+ litem )
      let newroot = patch root <| diff tree newtree

      equals 2.0 newroot.children.length
      newroot.remove() |> ignore
      finish()

    (* ------------------------------------------------------------------------ *)
    test "patching should update only relevant bits of the dom" <| fun finish ->
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

      equals true (fst.innerHTML <> firstContent)
      equals true (snd.innerHTML = secondContent)

      let list' = list firstContent <+ Li [] [| Text "hmm" |]
      root <- patch root <| diff tree (renderHtml list')

      equals firstContent  fst.innerHTML
      equals 3.0           root.children.length
      equals secondContent snd.innerHTML
      root.remove()
      finish()
