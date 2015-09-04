namespace Iris.Web

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
  let main () =
    Globals.Dollar.Invoke("main").append("hello")
    Globals.alert("What is the answer?")
    Globals.console.log("..the answer is: 42.")
