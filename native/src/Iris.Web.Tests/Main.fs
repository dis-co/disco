namespace Iris.Web.Tests

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery

open Microsoft.FSharp.Quotations
open System.IO
open System.Reflection
open System.Text.RegularExpressions

[<JavaScript>]
module Client =
  open Test.Units

  let apply f = f ()

  let Main =
    [ Html.main
    ; Store.main
    ; VirtualDom.main
    ; PatchesView.main
    ; ViewController.main 
    ] |> List.iter apply
    
(*
open Microsoft.FSharp.Quotations
open System.IO
open System.Reflection
open System.Text.RegularExpressions

    ____                      _ _      
   / ___|___  _ __ ___  _ __ (_) | ___ 
  | |   / _ \| '_ ` _ \| '_ \| | |/ _ \
  | |__| (_) | | | | | | |_) | | |  __/
   \____\___/|_| |_| |_| .__/|_|_|\___| tests
                       |_|             

  Uses reflection to get and compile all modules in this assembly whose
  namespace matches `^Test.Units.*`. The return type is a tuple module name and
  the compiled javascript code as a regular string.

let compileTests () = failwith "not implemented" 
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

*)
