namespace Iris.Web.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
module Util = 
  
  let elById (id : string) : Dom.Element = JS.Document.GetElementById id
  
  let mkContent () : JQuery =
    let el = JQuery.Of("div#content")
    JQuery.Of("body").Append el |> ignore
    el
    
  let cleanup (el : Dom.Element) : unit =
    failwith "cleanup needs implementing"
  
  let withContent (wrapper : JQuery -> unit) : unit =
    let content = mkContent () 
    wrapper content
