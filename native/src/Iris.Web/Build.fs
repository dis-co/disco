open FunScript
open FunScript.Compiler
open Iris.Web
open System.IO
open System.Diagnostics


[<FunScript.JS>]
module Build = 

  [<EntryPoint>]
  let main argv = 
    let exePath = Process.GetCurrentProcess().MainModule.FileName
    let destPath = Path.Combine(Path.GetDirectoryName(exePath), "assets", "js", "iris.js")
    let js = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    File.WriteAllText(destPath, js)
    0

