namespace Iris.Core

type Patch =
  { Id      : Id
  ; Name    : Name
  ; IOBoxes : IOBox array
  }

  static member HasIOBox (patch : Patch) (iobox : IOBox) : bool =
    let pred (iob : IOBox) = iob.Id = iobox.Id
    match Array.findIndex pred patch.IOBoxes with
      | i when i > -1 -> true
      | _             -> false
  
  static member FindIOBox (patches : Patch array) (id : Id) : IOBox option =
    let folder (m : IOBox option) (p : Patch) =
      match m with
        | Some(iobox) -> Some(iobox)
        | None -> Array.tryFind (fun iob -> iob.Id = id) p.IOBoxes
    Array.fold folder None patches
  
  static member ContainsIOBox (patches : Patch array) (iobox : IOBox) : bool =
    let folder m p = Patch.HasIOBox p iobox || m
    Array.fold folder false patches
  
  static member AddIOBox (patch : Patch) (iobox : IOBox) : Patch=
    if Patch.HasIOBox patch iobox
    then patch 
    else { patch with IOBoxes = Array.append patch.IOBoxes [| iobox |] }
  
  static member UpdateIOBox (patch : Patch) (iobox : IOBox) : Patch =
    let mapper (ibx : IOBox) = 
      if ibx.Id = iobox.Id
      then iobox
      else ibx
    if Patch.HasIOBox patch iobox
    then { patch with IOBoxes = Array.map mapper patch.IOBoxes }
    else patch
  
  static member RemoveIOBox (patch : Patch) (iobox : IOBox) : Patch =
    { patch with
        IOBoxes = Array.filter (fun box -> box.Id <> iobox.Id) patch.IOBoxes }
  
  static member HasPatch (patches : Patch array) (patch : Patch) : bool =
    Array.exists (fun p -> p.Id = patch.Id) patches
