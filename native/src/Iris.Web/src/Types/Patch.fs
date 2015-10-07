[<ReflectedDefinition>]
module Iris.Web.Types.Patch

open FunScript
open FunScript.TypeScript
open Iris.Web.Types.IOBox

(*   ____       _       _     
    |  _ \ __ _| |_ ___| |__  
    | |_) / _` | __/ __| '_ \ 
    |  __/ (_| | || (__| | | |
    |_|   \__,_|\__\___|_| |_|
*)

[<NoEquality; NoComparison>]
type Patch =
  { id       : string
  ; name     : string
  ; ioboxes  : IOBox array
  }
