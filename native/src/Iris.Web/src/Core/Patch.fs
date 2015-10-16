[<ReflectedDefinition>]
module Iris.Web.Core.Patch

open FunScript
open FunScript.TypeScript
open Iris.Web.Core.IOBox

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


let hasIOBox (patch : Patch) (iobox : IOBox) : bool =
  let pred (iob : IOBox) = iob.id = iobox.id
  let idx = try Some(Array.findIndex pred patch.ioboxes)
            with
              | _ -> None
  match idx with
    | Some(_) -> true
    | _       -> false
  
let addIOBox (patch : Patch) (iobox : IOBox) : Patch=
  if hasIOBox patch iobox
  then
    patch 
  else 
    { patch with
        ioboxes = Array.append patch.ioboxes [|iobox|] }

let updateIOBox (patch : Patch) (iobox : IOBox) : Patch =
  let mapper (ibx : IOBox) = 
    if ibx.id = iobox.id
    then iobox
    else ibx
  if hasIOBox patch iobox
  then { patch with ioboxes = Array.map mapper patch.ioboxes }
  else   patch

let removeIOBox (patch : Patch) (iobox : IOBox) : Patch =
  { patch with
      ioboxes = Array.filter (fun box -> box.id <> iobox.id) patch.ioboxes }
