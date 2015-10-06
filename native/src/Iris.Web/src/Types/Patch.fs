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

type Patch () =
  let   id : string = ""
  let name : string = ""
  let mutable ioboxes : IOBoxes = new IOBoxes ()
  
  member self.Id
    with get () = id

  member self.Name
    with get () = name

  member self.IOBoxes
    with get () = ioboxes

  member self.add (iobox : IOBox) =
    ioboxes.add (iobox.Id) iobox

  member self.remove id =
    ioboxes.remove id

  member self.update (iobox : IOBox) =
    ioboxes.add (iobox.Id) iobox
