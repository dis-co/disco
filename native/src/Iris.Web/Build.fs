namespace Iris.Web

open FunScript
open FunScript.Compiler
open Iris.Web
open System.IO
open System.Diagnostics

[<FunScript.JS>]
module Build = 
  (*  ____        _ _     _ 
     | __ ) _   _(_) | __| |
     |  _ \| | | | | |/ _` |
     | |_) | |_| | | | (_| |
     |____/ \__,_|_|_|\__,_| javascript & output to bin/$TARGET/assets/js
   *)
  [<EntryPoint>]
  let compileJS argv = 
    let exePath = Process.GetCurrentProcess().MainModule.FileName
    let destPath = Path.Combine(Path.GetDirectoryName(exePath), "assets", "js", "iris.js")
    let js = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    File.WriteAllText(destPath, js)
    0

