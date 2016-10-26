namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open SharpYaml.Serialization
open Iris.Serialization.Raft

#endif

type PatchYaml(id, name, ioboxes) as self =
  [<DefaultValue>] val mutable Id : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable IOBoxes : IOBoxYaml array

  new () = new PatchYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.IOBoxes <- ioboxes

// #if JAVASCRIPT
// [<CustomEquality>]
// [<CustomComparison>]
// #endif
type Patch =
  { Id      : Id
  ; Name    : Name
  ; IOBoxes : Map<Id,IOBox> }

  //  _   _           ___ ___  ____
  // | | | | __ _ ___|_ _/ _ \| __ )  _____  __
  // | |_| |/ _` / __|| | | | |  _ \ / _ \ \/ /
  // |  _  | (_| \__ \| | |_| | |_) | (_) >  <
  // |_| |_|\__,_|___/___\___/|____/ \___/_/\_\

  static member HasIOBox (patch : Patch) (id: Id) : bool =
    Map.containsKey id patch.IOBoxes

  //  _____ _           _ ___ ___  ____
  // |  ___(_)_ __   __| |_ _/ _ \| __ )  _____  __
  // | |_  | | '_ \ / _` || | | | |  _ \ / _ \ \/ /
  // |  _| | | | | | (_| || | |_| | |_) | (_) >  <
  // |_|   |_|_| |_|\__,_|___\___/|____/ \___/_/\_\

  static member FindIOBox (patches : Map<Id, Patch>) (id : Id) : IOBox option =
    let folder (m : IOBox option) _ (patch: Patch) =
      match m with
        | Some _ as res -> res
        |      _        -> Map.tryFind id patch.IOBoxes
    Map.fold folder None patches

  //   ____            _        _           ___ ___  ____
  //  / ___|___  _ __ | |_ __ _(_)_ __  ___|_ _/ _ \| __ )  _____  __
  // | |   / _ \| '_ \| __/ _` | | '_ \/ __|| | | | |  _ \ / _ \ \/ /
  // | |__| (_) | | | | || (_| | | | | \__ \| | |_| | |_) | (_) >  <
  //  \____\___/|_| |_|\__\__,_|_|_| |_|___/___\___/|____/ \___/_/\_\

  static member ContainsIOBox (patches : Map<Id,Patch>) (id: Id) : bool =
    let folder m _ p =
      if m then m else Patch.HasIOBox p id || m
    Map.fold folder false patches

  //     _       _     _ ___ ___  ____
  //    / \   __| | __| |_ _/ _ \| __ )  _____  __
  //   / _ \ / _` |/ _` || | | | |  _ \ / _ \ \/ /
  //  / ___ \ (_| | (_| || | |_| | |_) | (_) >  <
  // /_/   \_\__,_|\__,_|___\___/|____/ \___/_/\_\

  static member AddIOBox (patch : Patch) (iobox : IOBox) : Patch=
    if Patch.HasIOBox patch iobox.Id then
      patch
    else
      { patch with IOBoxes = Map.add iobox.Id iobox patch.IOBoxes }

  //  _   _           _       _       ___ ___  ____
  // | | | |_ __   __| | __ _| |_ ___|_ _/ _ \| __ )  _____  __
  // | | | | '_ \ / _` |/ _` | __/ _ \| | | | |  _ \ / _ \ \/ /
  // | |_| | |_) | (_| | (_| | ||  __/| | |_| | |_) | (_) >  <
  //  \___/| .__/ \__,_|\__,_|\__\___|___\___/|____/ \___/_/\_\
  //       |_|
  static member UpdateIOBox (patch : Patch) (iobox : IOBox) : Patch =
    if Patch.HasIOBox patch iobox.Id then
      let mapper _ (other: IOBox) =
        if other.Id = iobox.Id then iobox else other
      { patch with IOBoxes = Map.map mapper patch.IOBoxes }
    else
      patch

  //  ____                               ___ ___  ____
  // |  _ \ ___ _ __ ___   _____   _____|_ _/ _ \| __ )  _____  __
  // | |_) / _ \ '_ ` _ \ / _ \ \ / / _ \| | | | |  _ \ / _ \ \/ /
  // |  _ <  __/ | | | | | (_) \ V /  __/| | |_| | |_) | (_) >  <
  // |_| \_\___|_| |_| |_|\___/ \_/ \___|___\___/|____/ \___/_/\_\

  static member RemoveIOBox (patch : Patch) (iobox : IOBox) : Patch =
    { patch with IOBoxes = Map.remove iobox.Id patch.IOBoxes }

#if JAVASCRIPT
#else

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = new PatchYaml()
    yaml.Id <- string self.Id
    yaml.Name <- self.Name
    yaml.IOBoxes <- self.IOBoxes
                   |> Map.toArray
                   |> Array.map (snd >> Yaml.toYaml)
    yaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: PatchYaml) =
    let ioboxes =
      Array.fold
        (fun (ioboxes: Map<Id,IOBox>) (ioyml: IOBoxYaml) ->
           let parsed : IOBox option = Yaml.fromYaml ioyml
           match parsed with
           | Some iobox -> Map.add iobox.Id iobox ioboxes
           | _          -> ioboxes)
        Map.empty
        yml.IOBoxes

    { Id = Id yml.Id
      Name = yml.Name
      IOBoxes = ioboxes }
    |> Some

  static member FromYaml (str: string) : Patch option =
    let serializer = new Serializer()
    serializer.Deserialize<PatchYaml>(str)
    |> Yaml.fromYaml

#endif

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB (fb: PatchFB) =
    let mutable ioboxes = Map.empty

    for i in 0 .. (fb.IOBoxesLength - 1) do
#if JAVASCRIPT
      fb.IOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
      |> ignore
#else
      let iobox = fb.IOBoxes(i)
      if iobox.HasValue then
        iobox.Value
        |> IOBox.FromFB
        |> Option.map (fun iobox -> ioboxes <- Map.add iobox.Id iobox ioboxes)
        |> ignore
#endif

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
    let ioboxoffsets =
      self.IOBoxes
      |> Map.toArray
      |> Array.map (fun (_,iobox: IOBox) -> iobox.ToOffset(builder))

    let ioboxes = PatchFB.CreateIOBoxesVector(builder, ioboxoffsets)
    PatchFB.StartPatchFB(builder)
    PatchFB.AddId(builder, id)
    PatchFB.AddName(builder, name)
    PatchFB.AddIOBoxes(builder, ioboxes)
    PatchFB.EndPatchFB(builder)

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) : Patch option =
    Binary.createBuffer bytes
    |> PatchFB.GetRootAsPatchFB
    |> Patch.FromFB
