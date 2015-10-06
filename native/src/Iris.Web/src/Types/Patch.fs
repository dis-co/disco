[<ReflectedDefinition>]
module Iris.Web.Types.Patch

open Iris.Web.Types.IOBox

(*   ____       _       _     
    |  _ \ __ _| |_ ___| |__  
    | |_) / _` | __/ __| '_ \ 
    |  __/ (_| | || (__| | | |
    |_|   \__,_|\__\___|_| |_|
*)

type Patch () =
  let   id : string = ""
  let name : string = ""
  let mutable ioboxes : IOBox array = Array.empty

  member x.Name
    with get () = name

  member x.IOBoxes
    with get () = ioboxes
    and  set bx = ioboxes <- bx


