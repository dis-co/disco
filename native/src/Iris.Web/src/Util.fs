namespace Iris.Web

open FunScript
open FunScript.TypeScript

[<FunScript.JS>]
module Util =

  let log str =
    Globals.console.log(str)
