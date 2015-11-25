namespace Iris.Core.Types

open WebSharper

[<AutoOpen>]
[<ReflectedDefinition>]
module Patch =

  type Patch = {
      [<Name "id">]      Id      : IrisId;
      [<Name "name">]    Name    : Name;
      [<Name "ioboxes">] IOBoxes : IOBox array;
    }

  
  let hasIOBox (patch : Patch) (iobox : IOBox) : bool =
    let pred (iob : IOBox) = iob.Id = iobox.Id
    let idx = try Some(Array.findIndex pred patch.IOBoxes)
              with
                | _ -> None
    match idx with
      | Some(_) -> true
      | _       -> false
  
  let findIOBox (patches : Patch array) (id : string) : IOBox option =
    let folder (m : IOBox option) (p : Patch) =
      match m with
        | Some(iobox) -> Some(iobox)
        | None -> Array.tryFind (fun iob -> iob.Id = id) p.IOBoxes
    Array.fold folder None patches
  
  let containsIOBox (patches : Patch array) (iobox : IOBox) : bool =
    let folder m p =
      if hasIOBox p iobox || m
      then true
      else false
    Array.fold folder false patches
  
  let addIOBox (patch : Patch) (iobox : IOBox) : Patch=
    if hasIOBox patch iobox
    then patch 
    else { patch with IOBoxes = Array.append patch.IOBoxes [| iobox |] }
  
  let updateIOBox (patch : Patch) (iobox : IOBox) : Patch =
    let mapper (ibx : IOBox) = 
      if ibx.Id = iobox.Id
      then iobox
      else ibx
    if hasIOBox patch iobox
    then { patch with IOBoxes = Array.map mapper patch.IOBoxes }
    else patch
  
  let removeIOBox (patch : Patch) (iobox : IOBox) : Patch =
    { patch with
        IOBoxes = Array.filter (fun box -> box.Id <> iobox.Id) patch.IOBoxes }
  
  let hasPatch (patches : Patch array) (patch : Patch) : bool =
    Array.exists (fun p -> p.Id = patch.Id) patches
