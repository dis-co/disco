namespace rec Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

// * CueGroupYaml

open System
open Path

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml
open SharpYaml.Serialization

type CueGroupYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable CueRefs: CueReferenceYaml array

  // ** From

  static member From(cueGroup: CueGroup) =
    let yaml = CueGroupYaml()
    yaml.Id <- string cueGroup.Id
    yaml.Name <- unwrap cueGroup.Name
    yaml.CueRefs <- Array.map Yaml.toYaml cueGroup.CueRefs
    yaml

  // ** ToCueGroup

  member yaml.ToCueGroup() =
    either {
      let! cues = Either.bindArray Yaml.fromYaml yaml.CueRefs
      return { Id = Id yaml.Id
               Name = name yaml.Name
               CueRefs = cues }
    }

#endif

// * CueGroup

[<StructuralEquality; StructuralComparison>]
type CueGroup =
  { Id:         Id
    Name:       Name
    CueRefs:    CueReference array }

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueGroupFB) : Either<IrisError,CueGroup> =
    either {
      let! cues =
        EitherExt.bindGeneratorToArray "CueGroup.FromFB" fb.CueRefsLength fb.CueRefs CueReference.FromFB
      return { Id = Id fb.Id
               Name = name fb.Name
               CueRefs = cues }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueGroupFB> =
    let id = self.Id |> string |> builder.CreateString
    let name = self.Name |> unwrap |> builder.CreateString
    let cueoffsets = Array.map (fun (cue: CueReference)  -> cue.ToOffset(builder)) self.CueRefs
    let cuesvec = CueGroupFB.CreateCueRefsVector(builder, cueoffsets)
    CueGroupFB.StartCueGroupFB(builder)
    CueGroupFB.AddId(builder, id)
    CueGroupFB.AddName(builder, name)
    CueGroupFB.AddCueRefs(builder, cuesvec)
    CueGroupFB.EndCueGroupFB(builder)

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,CueGroup> =
    bytes
    |> Binary.createBuffer
    |> CueGroupFB.GetRootAsCueGroupFB
    |> CueGroup.FromFB

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYamlObject() = CueGroupYaml.From(cue)

  // ** FromYamlObject

  static member FromYamlObject(yaml: CueGroupYaml) : Either<IrisError,CueGroup> =
    yaml.ToCueGroup()

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) : Either<IrisError,CueGroup> =
    let serializer = Serializer()
    serializer.Deserialize<CueGroupYaml>(str)
    |> Yaml.fromYaml

  #endif
