namespace Iris.Web

open FunScript
open FunScript.Compiler
open Iris.Web.Main
open System.IO

[<FunScript.JS>]
module Build = 

  [<EntryPoint>]
  let main argv = 
    let path =  Array.get argv 0
    let js = Compiler.Compile(<@ Main.main() @>, noReturn = true)
    File.WriteAllText(path, js)
    0

