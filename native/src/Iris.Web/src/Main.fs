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
  let main() =
    let conn = Transport.connect "ws://localhost:9500"
    conn._open ()
    Routes.start()
