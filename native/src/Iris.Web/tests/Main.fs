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
    ____                      _ _      
   / ___|___  _ __ ___  _ __ (_) | ___ 
  | |   / _ \| '_ ` _ \| '_ \| | |/ _ \
  | |__| (_) | | | | | | |_) | | |  __/
   \____\___/|_| |_| |_| .__/|_|_|\___| tests
                       |_|             

  Uses reflection to get and compile all modules in this assembly whose
  namespace matches `^Test.Units.*`. The return type is a tuple module name and
  the compiled javascript code as a regular string.
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
    [ h2 <@> style' @"margin: 50px 65px;"
         <|> (a <@> href' "/tests" <|> text "Iris Tests")
    ; div    <@> id' "content"
    ; div    <@> id' "mocha"
    ; script <@> src' "dependencies/virtual-dom/dist/virtual-dom.js"
    ; script <@> src' "dependencies/rxjs/dist/rx.all.js"
    ; script <@> src' "dependencies/jquery/dist/jquery.js"
    ; script <@> src' "dependencies/routie/dist/routie.js"
    ; script <@> src' "dependencies/fabric.js/dist/fabric.js"
    ; script <@> src' "https://cdn.rawgit.com/Automattic/expect.js/0.3.1/index.js"
    ; script <@> src' "https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.js"
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
  
(*
   _____         _   ____                  
  |_   _|__  ___| |_|  _ \ __ _  __ _  ___ 
    | |/ _ \/ __| __| |_) / _` |/ _` |/ _ \
    | |  __/\__ \ |_|  __/ (_| | (_| |  __/
    |_|\___||___/\__|_|   \__,_|\__, |\___|
                                |___/      

    Function to compile the tests page. 
*)

let testsPage () = List.fold (fun m e -> m + renderHtml e) "" <| page
