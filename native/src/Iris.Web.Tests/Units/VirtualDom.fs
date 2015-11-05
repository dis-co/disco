namespace Test.Units

open WebSharper
open WebSharper.JavaScript
open WebSharper.Mocha
open Iris.Web.Core.Html

[<JavaScript>]
module VirtualDom =

  let main () =
    // let props = new VProperties()
    // props.id <- "main"
    // props.className <- "horrific"
    // 
    // Console.Log(new VTree ("h1", props, [| new VTree("Oh wtf"); new VTree("p", new VProperties(), [| new VTree("p content") |]) |]) |> createElement)

    (*------------------------------------------------------------------------*)
    suite "Test.Units.VirtualDom"
    (*------------------------------------------------------------------------*)

    pending "should render dom elements correctly"
    pending "should render updates to a dom tree correctly"
    pending "should render id attribute"
    pending "should render style attribute"
    pending "should render class attribute"

