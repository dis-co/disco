module Iris.Tests.Html

open Iris.Web.Html
open NUnit.Framework

[<Test>]
let ``should emit correct html snippet for a`` () =
  let mylink = a <@> href' "http://localhost"
                 <|> text "life's good"

  let expectation = @"<a href=""http://localhost"">life's good</a>"

  Assert.AreEqual(expectation, renderHtml mylink)

[<Test>]
let ``should emit correct html snippet for div with id`` () =
  let mydiv = div <@> id' "main"
                  <@> class' "top bottom"
                  <|> text "life's good"

  let expectation = @"<div id=""main"" class=""top bottom"">life's good</div>"

  Assert.AreEqual(expectation, renderHtml mydiv)

[<Test>]
let ``should emit correct html snippet for ul with items`` () =
  let mydiv = ul <@> class' "nostyle" <||>
                [ li <@> class' "item" <|> text "life's good"
                ; li <@> class' "item" <|> text "innit?"
                ]

  let expectation =
    @"<ul class=""nostyle""><li class=""item"">life's good</li><li class=""item"">innit?</li></ul>"

  Assert.AreEqual(expectation, renderHtml mydiv)
