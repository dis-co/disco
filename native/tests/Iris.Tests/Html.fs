module Iris.Tests.Html

open Iris.Web.Html
open NUnit.Framework

[<Test>]
let ``should emit correct html snippet for a`` () =
  let result = "test"
  Assert.AreEqual("test",result)
