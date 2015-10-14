module Iris.Web.Test.Main

open System.IO
open FSharp.Html
open FunScript
open FunScript.Compiler
open FunScript.TypeScript

(*

  <html>
  <head>
    <meta charset="utf-8">
    <title>Mocha Tests</title>
    <link href="https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.css" rel="stylesheet" />
  </head>
  <body>
    <div id="mocha"></div>
  
    <script src="https://cdn.rawgit.com/jquery/jquery/2.1.4/dist/jquery.min.js"></script>
    <script src="https://cdn.rawgit.com/Automattic/expect.js/0.3.1/index.js"></script>
    <script src="https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.js"></script>
  
    <script>mocha.setup('bdd')</script>
  
    <script src="test.array.js"></script>
    <script src="test.object.js"></script>
    <script src="test.xhr.js"></script>
  
    <script>
      mocha.checkLeaks();
      mocha.globals(['jQuery']);
      mocha.run();
    </script>
  </body>
  </html>

*)

let test1 = Compiler.Compile(<@ Test.Units.VirtualDom.main() @>, noReturn = true)
let test2 = Compiler.Compile(<@ Test.Units.Store.main() @>, noReturn = true)

let doctype = Literal("<!doctype html>")
let charset = meta <@> charset' "utf-8" 

let header =
  head <||> 
    [ title <|> text "Iris Tests"
    ; charset
    ; link <@> rel'  "stylesheet"
           <@> href' "https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.css"
    ]

let content =
  body <||>
    [ h1     <|> text "Iris Tests"

    ; script <@> src' "dependencies/virtual-dom/dist/virtual-dom.js"
    ; script <@> src' "dependencies/rxjs/dist/rx.all.js"
    ; script <@> src' "dependencies/jquery/dist/jquery.js"
    ; script <@> src' "dependencies/routie/dist/routie.js"
    ; script <@> src' "dependencies/fabric.js/dist/fabric.js"

    ; script <@> src' "https://cdn.rawgit.com/Automattic/expect.js/0.3.1/index.js"
    ; script <@> src' "https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.js"
  
    ; script <|> text "mocha.setup('bdd')"
    ; script <|> text test1
    ; script <|> text test2

    ]

let page =
  [ doctype;
    html
    <|> header
    <|> content
  ]
  
let testsPage () = List.fold (fun m e -> m + renderHtml e) "" <| page

// let compileTests () = 
//   let source = Compiler.Compile(<@ Iris.Web.Test.Main.main() @>, noReturn = true)
//    sprintf "$(document).ready(function () {\n%s\n});" source
