module Iris.Web.Test.Main

open Microsoft.FSharp.Quotations
open System.IO
open System.Reflection
open System.Text.RegularExpressions
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

let compileTests () =
  let regex = new Regex("^Test.Units")
  let path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
  let assembly = Assembly.LoadFrom path
  assembly.GetExportedTypes ()
  |> Array.filter
    (fun m -> regex.IsMatch <| m.ToString ()) 
  |> Array.map
      (fun m ->
       let meths = m.GetMethods ()
       let info : MethodInfo option = Array.tryFind (fun mi -> mi.Name = "main") meths
       match info with
         | Some(mi) -> (m.ToString (), Compiler.Compile(Expr.Call(mi, []), noReturn = true))
         | _ -> (m.ToString (), ""))
  |> Array.toList
  
let test name str = script <@> class' name <|> text str

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
  let tops = 
    [ h1     <|> text "Iris Tests"
    ; div <@> id' "content"
    ; div <@> id' "mocha"

    ; script <@> src' "dependencies/virtual-dom/dist/virtual-dom.js"
    ; script <@> src' "dependencies/rxjs/dist/rx.all.js"
    ; script <@> src' "dependencies/jquery/dist/jquery.js"
    ; script <@> src' "dependencies/routie/dist/routie.js"
    ; script <@> src' "dependencies/fabric.js/dist/fabric.js"

    ; script <@> src' "https://cdn.rawgit.com/Automattic/expect.js/0.3.1/index.js"
    ; script <@> src' "https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.js"
  
    (* setup *)
    ; script <|> text "mocha.setup('qunit')"
    ]

  let tests = List.map (fun (name,code) -> test name code) <| compileTests ()
  let run = script <|> text "mocha.run()"

  body <||> List.fold (fun memo t -> List.append memo t) [] [ tops; tests; [run] ]

let page =
  [ doctype;
    html
    <|> header
    <|> content
  ]
  
let testsPage () = List.fold (fun m e -> m + renderHtml e) "" <| page
