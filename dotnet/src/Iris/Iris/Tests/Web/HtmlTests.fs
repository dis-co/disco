namespace Test.Units

[<RequireQualifiedAccess>]
module Html =

  open Fable.Core
  open Fable.Import
  open Fable.Import.Browser

  open Iris.Web.Tests
  open Iris.Web.Core.Html

  let main () =
    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.Html - basic combinators"
    (* -------------------------------------------------------------------------- *)

    test "innerText should be specified with `text'` combinator" <| fun finish ->
       let content = "hello there"
       let comb = H1 [] [| Text content |]
       let elm = renderHtml comb |> createElement
       equals content elm.innerHTML
       finish()

    (* -------------------------------------------------------------------------- *)
    test "class should shouLD be specified with `class'` combinator" <| fun finish ->
       let klass = "there"
       let comb = H1 [ Class klass ] Array.empty
       let elm = renderHtml comb |> createElement
       equals klass elm.className
       finish()

    (* -------------------------------------------------------------------------- *)
    test "id should should be specified with `id'` combinator" <| fun finish ->
       let eidee = "thou"
       let comb = H1 [ ElmId eidee ] Array.empty
       let elm = renderHtml comb |> createElement
       equals eidee <| elm.getAttribute("id")
       finish ()

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.Html - composite Html * VTree"
    (* -------------------------------------------------------------------------- *)

    test "nested VTree in Html should be rendered as such" <| fun finish ->
       let t  = Div [ Class "thing" ] [| Text "hello" |]
       let t' = renderHtml <| H1 [] [| Text "hallo" |]
       let elm = renderHtml (t <+ Raw t') |> createElement
       equals "thing" <| elm.getAttribute("class")
       equals 1.0 (elm.getElementsByTagName "h1" |> fun el -> el.length)
       finish()

    (* -------------------------------------------------------------------------- *)
    test "pure html in should be rendered as such" <| fun finish ->
       let t  = Div [ Class "thing" ] [| Text "hello" |]
       let elm = renderHtml t |> createElement
       equals "thing" <| elm.getAttribute("class")
       finish()

    (* -------------------------------------------------------------------------- *)
    test "VTree should be rendered as expected" <| fun finish ->
      let elm =
        Raw((Div [ ElmId "hello" ] Array.empty) |> renderHtml)
        |> renderHtml
        |> createElement

      equals "hello" <| elm.getAttribute("id")
      finish()

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.Html - event callbacks"
    (* -------------------------------------------------------------------------- *)

    test "function should be rendered and called" <| fun finish ->
      let elm =
        Div [ OnClick (fun ev -> finish()) ] Array.empty
        |> renderHtml
        |> createElement

      elm.click() |> ignore
