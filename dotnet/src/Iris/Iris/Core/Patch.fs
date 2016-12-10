namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open SharpYaml.Serialization
open Iris.Serialization.Raft

#endif

#if !FABLE_COMPILER

type PatchYaml(id, name, pins) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable Pins : PinYaml array

  new () = new PatchYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.Pins <- pins

#endif

// #if FABLE_COMPILER
// [<CustomEquality>]
// [<CustomComparison>]
// #endif
type Patch =
  { Id   : Id
    Name : Name
    Pins : Map<Id,Pin> }

  //  _   _           ____  _
  // | | | | __ _ ___|  _ \(_)_ __
  // | |_| |/ _` / __| |_) | | '_ \
  // |  _  | (_| \__ \  __/| | | | |
  // |_| |_|\__,_|___/_|   |_|_| |_|

  static member HasPin (patch : Patch) (id: Id) : bool =
    Map.containsKey id patch.Pins

  //  _____ _           _ ____  _
  // |  ___(_)_ __   __| |  _ \(_)_ __
  // | |_  | | '_ \ / _` | |_) | | '_ \
  // |  _| | | | | | (_| |  __/| | | | |
  // |_|   |_|_| |_|\__,_|_|   |_|_| |_|

  static member FindPin (patches : Map<Id, Patch>) (id : Id) : Pin option =
    let folder (m : Pin option) _ (patch: Patch) =
      match m with
        | Some _ as res -> res
        |      _        -> Map.tryFind id patch.Pins
    Map.fold folder None patches

  //   ____            _        _           ____  _
  //  / ___|___  _ __ | |_ __ _(_)_ __  ___|  _ \(_)_ __
  // | |   / _ \| '_ \| __/ _` | | '_ \/ __| |_) | | '_ \
  // | |__| (_) | | | | || (_| | | | | \__ \  __/| | | | |
  //  \____\___/|_| |_|\__\__,_|_|_| |_|___/_|   |_|_| |_|

  static member ContainsPin (patches : Map<Id,Patch>) (id: Id) : bool =
    let folder m _ p =
      if m then m else Patch.HasPin p id || m
    Map.fold folder false patches

  //     _       _     _ ____  _
  //    / \   __| | __| |  _ \(_)_ __
  //   / _ \ / _` |/ _` | |_) | | '_ \
  //  / ___ \ (_| | (_| |  __/| | | | |
  // /_/   \_\__,_|\__,_|_|   |_|_| |_|

  static member AddPin (patch : Patch) (pin : Pin) : Patch=
    if Patch.HasPin patch pin.Id then
      patch
    else
      { patch with Pins = Map.add pin.Id pin patch.Pins }

  //  _   _           _       _       ____  _
  // | | | |_ __   __| | __ _| |_ ___|  _ \(_)_ __
  // | | | | '_ \ / _` |/ _` | __/ _ \ |_) | | '_ \
  // | |_| | |_) | (_| | (_| | ||  __/  __/| | | | |
  //  \___/| .__/ \__,_|\__,_|\__\___|_|   |_|_| |_|
  //       |_|

  static member UpdatePin (patch : Patch) (pin : Pin) : Patch =
    if Patch.HasPin patch pin.Id then
      let mapper _ (other: Pin) =
        if other.Id = pin.Id then pin else other
      { patch with Pins = Map.map mapper patch.Pins }
    else
      patch

  //  ____                               ____  _
  // |  _ \ ___ _ __ ___   _____   _____|  _ \(_)_ __
  // | |_) / _ \ '_ ` _ \ / _ \ \ / / _ \ |_) | | '_ \
  // |  _ <  __/ | | | | | (_) \ V /  __/  __/| | | | |
  // |_| \_\___|_| |_| |_|\___/ \_/ \___|_|   |_|_| |_|

  static member RemovePin (patch : Patch) (pin : Pin) : Patch =
    { patch with Pins = Map.remove pin.Id patch.Pins }

#if !FABLE_COMPILER

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYamlObject () =
    let yaml = new PatchYaml()
    yaml.Id <- string self.Id
    yaml.Name <- self.Name
    yaml.Pins <- self.Pins
                   |> Map.toArray
                   |> Array.map (snd >> Yaml.toYaml)
    yaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  static member FromYamlObject (yml: PatchYaml) =
    either {
      let! pins =
        Array.fold
          (fun (m: Either<IrisError,Map<Id,Pin>>) pinyml -> either {
            let! pins = m
            let! (pin : Pin) = Yaml.fromYaml pinyml
            return Map.add pin.Id pin pins
          })
          (Right Map.empty)
          yml.Pins

      return { Id = Id yml.Id
               Name = yml.Name
               Pins = pins }
    }

  static member FromYaml (str: string) : Either<IrisError,Patch> =
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
    either {
      let! pins =
        let arr = Array.zeroCreate fb.PinsLength
        Array.fold
          (fun (m: Either<IrisError,int * Map<Id,Pin>>) _ -> either {
              let! (i, pins) = m

  #if FABLE_COMPILER
              let! pin = i |> fb.Pins |> Pin.FromFB
  #else
              let! pin =
                let nullable = fb.Pins(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Pin.FromFB
                else
                  "Could not parse empty PinFB"
                  |> ParseError
                  |> Either.fail
  #endif

              return (i + 1, Map.add pin.Id pin pins)
            })
          (Right (0, Map.empty))
          arr
        |> Either.map snd

      return { Id = Id fb.Id
               Name = fb.Name
               Pins = pins }
    }

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<PatchFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let pinoffsets =
      self.Pins
      |> Map.toArray
      |> Array.map (fun (_,pin: Pin) -> pin.ToOffset(builder))

    let pins = PatchFB.CreatePinsVector(builder, pinoffsets)
    PatchFB.StartPatchFB(builder)
    PatchFB.AddId(builder, id)
    PatchFB.AddName(builder, name)
    PatchFB.AddPins(builder, pins)
    PatchFB.EndPatchFB(builder)

  member self.ToBytes() : Binary.Buffer = Binary.buildBuffer self

  static member FromBytes (bytes: Binary.Buffer) : Either<IrisError,Patch> =
    Binary.createBuffer bytes
    |> PatchFB.GetRootAsPatchFB
    |> Patch.FromFB
