namespace Iris.Core

#if JAVASCRIPT
#else

open FlatBuffers
open Iris.Serialization.Raft
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

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


#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<Patch>()

  //    _   _ _____ _____
  //   | \ | | ____|_   _|
  //   |  \| |  _|   | |
  //  _| |\  | |___  | |
  // (_)_| \_|_____| |_| serialization

  static member FromFB (fb: PatchFB) =
    let ioboxes = Array.zeroCreate fb.IOBoxesLength

    for i in 0 .. (fb.IOBoxesLength - 1) do
      fb.GetIOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
      |> ignore

    try
      { Id = Id fb.Id
      ; Name = fb.Name
      ; IOBoxes = ioboxes
      } |> Some
    with
      | _ -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PatchFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let ioboxoffsets = Array.map (fun (iobox: IOBox) -> iobox.ToOffset(builder)) self.IOBoxes
    let ioboxes = PatchFB.CreateIOBoxesVector(builder, ioboxoffsets)
    PatchFB.StartPatchFB(builder)
    PatchFB.AddId(builder, id)
    PatchFB.AddName(builder, name)
    PatchFB.AddIOBoxes(builder, ioboxes)
    PatchFB.EndPatchFB(builder)

  member self.ToBytes() : byte array = Binary.buildBuffer self

  static member FromBytes (bytes: byte array) : Patch option =
    let msg = PatchFB.GetRootAsPatchFB(new ByteBuffer(bytes))
    Patch.FromFB(msg)

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|
  member self.ToJToken() =
    new JObject()
    |> addString "Id"     (string self.Id)
    |> addString "Name"    self.Name
    |> addArray  "IOBoxes" self.IOBoxes

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : Patch option =
    try
      let ioboxes =
        let jarr = token.["IOBoxes"] :?> JArray
        let arr = Array.zeroCreate jarr.Count

        for i in 0 .. (jarr.Count - 1) do
          Json.parse jarr.[i]
          |> Option.map (fun iobox -> arr.[i] <- iobox)
          |> ignore

        arr

      { Id = Id (string token.["Id"])
      ; Name = string token.["Name"]
      ; IOBoxes = ioboxes
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize patch json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : Patch option =
    JObject.Parse(str) |> Patch.FromJToken

#endif
