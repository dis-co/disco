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

    test "innerText should be specified with `text'` combinator" <|
      (fun cb ->
       let content = "hello there"
       let comb = H1 [] [| Text content |]
       let elm = renderHtml comb |> createElement
       check_cc (elm.innerHTML = content) "content mismatch" cb)

    (* -------------------------------------------------------------------------- *)
    test "class should shouLD BE SPECIFIED with `class'` combinator" <|
      (fun cb ->
       let klass = "there"
       let comb = H1 [ Class klass ] Array.empty
       let elm = renderHtml comb |> createElement
       check_cc (elm.className = klass) "class mismatch" cb)

    (* -------------------------------------------------------------------------- *)
    test "id should should be specified with `id'` combinator" <|
      (fun cb ->
       let eidee = "thou"
       let comb = H1 [ ElmId eidee ] Array.empty
       let elm = renderHtml comb |> createElement 
       check_cc (elm.getAttribute("id") = eidee) "id mismatch" cb)

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.Html - composite Html * VTree"
    (* -------------------------------------------------------------------------- *)

    test "nested VTree in Html should be rendered as such"
      (fun cb ->
       let t  = Div [ Class "thing" ] [| Text "hello" |]
       let t' = renderHtml <| H1 [] [| Text "hallo" |]

       let elm = renderHtml (t <+ Raw t') |> createElement
         
       check (elm.getAttribute("class") = "thing") "should be a thing but isn't"

       elm.getElementsByTagName "h1"
       |> (fun t'' -> check_cc (t''.length = 1.0) "should have a h1 but hasn't'" cb))

    (* -------------------------------------------------------------------------- *)
    test "pure html in should be rendered as such" <|
      (fun cb ->
       let t  = Div [ Class "thing" ] [| Text "hello" |]

       let elm = renderHtml t |> createElement 

       check_cc (elm.getAttribute("class") = "thing") "should be a thing but isn't" cb)

    (* -------------------------------------------------------------------------- *)
    test "VTree should be rendered as expected" <| fun cb ->
      let elm =
        Raw((Div [ ElmId "hello" ] Array.empty) |> renderHtml)
        |> renderHtml
        |> createElement
        

      check_cc (elm.getAttribute("id") = "hello") "should have an element with id" cb

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.Html - event callbacks"
    (* -------------------------------------------------------------------------- *)

    test "callback function should be rendered and called" <| fun cb ->
      let elm =
        Div [ OnClick (fun ev -> check_cc true "should have been called" cb) ] Array.empty
        |> renderHtml
        |> createElement
        
        
      elm.click () |> ignore

