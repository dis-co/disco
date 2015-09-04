namespace Iris.Web

open System
open FunScript
open FunScript.TypeScript

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)

[<FunScript.JS>]
module Main =
  let main () = Routes.start ()
