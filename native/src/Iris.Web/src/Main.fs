namespace Iris.Web

open FunScript
open FunScript.TypeScript
open System

(* __  __       _       
  |  \/  | __ _(_)_ __  
  | |\/| |/ _` | | '_ \ 
  | |  | | (_| | | | | |
  |_|  |_|\__,_|_|_| |_| entry point.
*)

[<FunScript.JS>]
module Main =
  let main () = Routes.start ()
