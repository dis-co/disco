namespace Iris.Web

open FunScript
open FunScript.Compiler
open Iris.Web
open System.IO
open System.Diagnostics

[<FunScript.JS>]
module Build = 
  (* _____         _       
    |_   _|__  ___| |_ ___ 
      | |/ _ \/ __| __/ __|
      | |  __/\__ \ |_\__ \
      |_|\___||___/\__|___/
   *)
  let compileTests () = 
    let source = Compiler.Compile(<@ Tests.main() @>, noReturn = true)
    sprintf "$(document).ready(function () {\n%s\n});" source

  (*  ____        _ _     _ 
     | __ ) _   _(_) | __| |
     |  _ \| | | | | |/ _` |
     | |_) | |_| | | | (_| |
     |____/ \__,_|_|_|\__,_| javascript & output to bin/$TARGET/assets/js
   *)
  let compileJSString () = 
    let source = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    sprintf "$(document).ready(function () {\n%s\n});" source

  [<EntryPoint>]
  let compileJSFile argv = 
    let exePath = Process.GetCurrentProcess().MainModule.FileName
    let destPath = Path.Combine(Path.GetDirectoryName(exePath), "assets", "js", "iris.js")
    let source = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    let sourceWrapped = sprintf "$(document).ready(function () {\n%s\n});" source
    File.Delete destPath
    File.WriteAllText(destPath, sourceWrapped)
    0

