module Iris.Web.Test.Main

open Microsoft.FSharp.Quotations
open System.IO
open System.Reflection
open System.Text.RegularExpressions

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
  
let test name str = sprintf "<script id=\"%s\">%s</script>" name str
  
(*
   _____         _   ____                  
  |_   _|__  ___| |_|  _ \ __ _  __ _  ___ 
    | |/ _ \/ __| __| |_) / _` |/ _` |/ _ \
    | |  __/\__ \ |_|  __/ (_| | (_| |  __/
    |_|\___||___/\__|_|   \__,_|\__, |\___|
                                |___/      

    Function to compile the tests page. 
*)

let testsPage () = 
  List.fold (fun m (name,code) -> m + (test name code)) "" <| compileTests ()
  |> sprintf 
     @"
     <!doctype html>
     <html>  
       <head>
         <title>Iris Browser Tests</title>
         <meta charset=""utf-8"">
         <link rel=""stylesheet"" href=""https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.css"">
       </head>
       <body>
         <h2 style=""margin: 50px 65px;"">
           <a href=""/tests"">Iris Tests</a>
         </h2>

         <div id=""content""></div>
         <div id=""mocha""></div>

         <script src=""dependencies/virtual-dom/dist/virtual-dom.js""></script>
         <script src=""dependencies/rxjs/dist/rx.all.js""></script>
         <script src=""dependencies/jquery/dist/jquery.js""></script>
         <script src=""dependencies/routie/dist/routie.js""></script>
         <script src=""dependencies/fabric.js/dist/fabric.js""></script>
         <script src=""https://cdn.rawgit.com/Automattic/expect.js/0.3.1/index.js""></script>
         <script src=""https://cdn.rawgit.com/mochajs/mocha/2.2.5/mocha.js""></script>
         <script>mocha.setup('qunit')</script>

         %s

         <script>mocha.run()</script>
       </body>
     </html>
     "
