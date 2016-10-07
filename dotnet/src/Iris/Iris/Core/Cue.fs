namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open SharpYaml
open SharpYaml.Serialization
open FlatBuffers
open Iris.Serialization.Raft

type CueYaml(id, name, ioboxes) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable IOBoxes : IOBoxYaml array

  new () = new CueYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.IOBoxes <- ioboxes

#endif

#if JAVASCRIPT
type Cue =
#else
and Cue =
#endif
  { Id:      Id
  ; Name:    string
  ; IOBoxes: IOBox array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueFB) : Cue option =
#if JAVASCRIPT
    let ioboxes = [| |]
#else
    let ioboxes = Array.zeroCreate fb.IOBoxesLength
#endif

    for i in 0 .. (fb.IOBoxesLength - 1) do
#if JAVASCRIPT
      fb.IOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
      |> ignore
#else
      let iobox = fb.IOBoxes(i)
      if iobox.HasValue then
        iobox.Value
        |> IOBox.FromFB
        |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
        |> ignore
#endif

    try
      { Id = Id fb.Id
      ; Name = fb.Name
      ; IOBoxes = ioboxes
      } |> Some
    with
      | exn -> None

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let ioboxoffsets = Array.map (fun (iobox: IOBox) -> iobox.ToOffset(builder)) self.IOBoxes
    let ioboxes = CueFB.CreateIOBoxesVector(builder, ioboxoffsets)
    CueFB.StartCueFB(builder)
    CueFB.AddId(builder, id)
    CueFB.AddName(builder, name)
    CueFB.AddIOBoxes(builder, ioboxes)
    CueFB.EndCueFB(builder)

  static member FromBytes(bytes: Binary.Buffer) : Cue option =
    CueFB.GetRootAsCueFB(Binary.createBuffer bytes)
    |> Cue.FromFB

  member self.ToBytes() = Binary.buildBuffer self

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    let ioboxes = Array.map Yaml.toYaml self.IOBoxes
    new CueYaml(string self.Id, self.Name, ioboxes)

  static member FromYamlObject(yaml: CueYaml) : Cue option =
    let ioboxes =
      Array.fold
        (fun m box ->
           match Yaml.fromYaml box with
           | Some iobox -> Array.append m [| iobox |]
           | _          -> m)
        [| |]
        yaml.IOBoxes
    try
      { Id = Id yaml.Id
      ; Name = yaml.Name
      ; IOBoxes = ioboxes
      } |> Some
    with
      | exn ->
        printfn "Could not deserialize Cue: %s" exn.Message
        None

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  static member FromYaml(str: string) : Cue option =
    let serializer = new Serializer()
    serializer.Deserialize<CueYaml>(str)
    |> Yaml.fromYaml

  member self.DirName
    with get () = "cues"

  member self.CanonicalName
    with get () =
      sanitizeName self.Name
      |> sprintf "%s-%s" (string self.Id)
#endif
