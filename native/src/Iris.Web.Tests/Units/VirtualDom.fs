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

    (**)
    suite "Test.Units.VirtualDom"
