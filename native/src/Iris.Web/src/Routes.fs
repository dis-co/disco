namespace Iris.Web

open FunScript
open FunScript.TypeScript

[<FunScript.JS>]
module Routes =
  let goto (route : string) = 
    Globals.routie(route)

  let hello () =
    Globals.Dollar.Invoke("main")
      .append("<strong>hello</strong><br>")
      .append("<strong>hello</strong><br>")
      .append("<strong>hello</strong><br>")
      .append("<strong>hello</strong><br>")
      .append("<strong>hello</strong><br>") |> ignore

  let bye () =
    Globals.Dollar.Invoke("main")
      .append("<strong>bye</strong><br>")
      .append("<strong>bye</strong><br>")
      .append("<strong>bye</strong><br>")
      .append("<strong>bye</strong><br>")
      .append("<strong>bye</strong><br>") |> ignore

  let home () = goto "hello"

  let start () =
    Globals.routie("", unbox<Function>(home))
    Globals.routie("hello", unbox<Function>(hello))
    Globals.routie("bye", unbox<Function>(bye))
