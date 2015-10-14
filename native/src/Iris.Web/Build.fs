namespace Iris.Web

open FunScript
open FunScript.Compiler
open System.IO
open System.Diagnostics

open Iris.Web
open Test.Main

[<FunScript.JS>]
module Build = 
  (* _____         _       
    |_   _|__  ___| |_ ___ 
      | |/ _ \/ __| __/ __|
      | |  __/\__ \ |_\__ \
      |_|\___||___/\__|___/
   *)
  let compileTestPage () = testsPage ()

  (*  ____        _ _     _ 
     | __ ) _   _(_) | __| |
     |  _ \| | | | | |/ _` |
     | |_) | |_| | | | (_| |
     |____/ \__,_|_|_|\__,_| javascript & output to bin/$TARGET/assets/js
   *)
  let compileJSString () = 
    let source = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    sprintf "$(document).ready(function () {\n%s\n});" source
