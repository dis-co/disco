module Iris.Web.Test.Main

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

// let getTestModules () : System.Type array =
//   let regex = new Regex("^Test.Units")
//   let path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
//   let assembly = Assembly.LoadFrom path
//   let types = assembly.GetExportedTypes ()
//   types
//   // |> Array.filter
//   //   (fun t -> CompilationMappingAttribute.GetCustomAttributes t
//   //             |> Seq.exists (fun attr ->
//   //                              let a = attr :?> CompilationMappingAttribute
//   //                              a.SourceConstructFlags = SourceConstructFlags.Module))
//   |> Array.filter
//     (fun m -> not (regex.IsMatch <| m.ToString ())) 

// let compileTests () =
//   getTestModules ()
//   |> Array.map (fun m -> Compiler.Compile(<@ m.InvokeMember("main", BindingFlags.InvokeMethod, null, m, Array.empty) @>, noReturn = true))
  
let test str = script <|> text str

let vdomT  = test <| Compiler.Compile(<@ Test.Units.VirtualDom.main() @>, noReturn = true)
let htmlT  = test <| Compiler.Compile(<@ Test.Units.Html.main() @>, noReturn = true)
let storeT = test <| Compiler.Compile(<@ Test.Units.Store.main() @>, noReturn = true)

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

    (* the actual tests *)
    ; vdomT
    ; htmlT
    ; storeT

    (* the actual tests *)
    ; script <|> text "mocha.run()"
    ]

let page =
  [ doctype;
    html
    <|> header
    <|> content
  ]
  
let testsPage () = List.fold (fun m e -> m + renderHtml e) "" <| page
