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

// * CueReferenceYaml

open System
open Path

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml
open SharpYaml.Serialization

type CueReferenceYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable CueId: string
  [<DefaultValue>] val mutable AutoFollow: int
  [<DefaultValue>] val mutable Duration: int
  [<DefaultValue>] val mutable Prewait: int

  // ** From

  static member From(cue: CueReference) =
    let yaml = CueReferenceYaml()
    yaml.Id <- string cue.Id
    yaml.CueId <- string cue.CueId
    yaml.AutoFollow <- cue.AutoFollow
    yaml.Duration   <- cue.Duration
    yaml.Prewait    <- cue.Prewait
    yaml

  // ** ToCueReference

  member yaml.ToCueReference() =
    { Id          = Id yaml.Id
      CueId       = Id yaml.CueId    
      AutoFollow  = yaml.AutoFollow
      Duration    = yaml.Duration
      Prewait     = yaml.Prewait }
    |> Right

#endif

// * CueReference

[<StructuralEquality; StructuralComparison>]
type CueReference =
  { Id:         Id
    CueId:      Id
    AutoFollow: int
    Duration:   int
    Prewait:    int
    //Trigger:  Event option
  }

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueReferenceFB) : Either<IrisError,CueReference> =
    { Id          = Id fb.Id
      CueId       = Id fb.CueId    
      AutoFollow  = fb.AutoFollow
      Duration    = fb.Duration
      Prewait     = fb.Prewait }
    |> Right

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueReferenceFB> =
    let id = string self.Id |> builder.CreateString
    let cueId = string self.CueId |> builder.CreateString
    CueReferenceFB.StartCueReferenceFB(builder)
    CueReferenceFB.AddId(builder, id)
    CueReferenceFB.AddCueId(builder, cueId)
    CueReferenceFB.AddAutoFollow(builder, self.AutoFollow)
    CueReferenceFB.AddDuration(builder, self.Duration)
    CueReferenceFB.AddPrewait(builder, self.Prewait)
    CueReferenceFB.EndCueReferenceFB(builder)

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,CueReference> =
    bytes
    |> Binary.createBuffer
    |> CueReferenceFB.GetRootAsCueReferenceFB
    |> CueReference.FromFB

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYamlObject() = CueReferenceYaml.From(cue)

  // ** FromYamlObject

  static member FromYamlObject(yaml: CueReferenceYaml) : Either<IrisError,CueReference> =
    yaml.ToCueReference()

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) : Either<IrisError,CueReference> =
    let serializer = Serializer()
    serializer.Deserialize<CueReferenceYaml>(str)
    |> Yaml.fromYaml

  #endif
