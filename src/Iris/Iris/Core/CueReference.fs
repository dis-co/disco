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
     either {
      let! id = IrisId.TryParse yaml.Id
      let! cueId = IrisId.TryParse yaml.CueId
      return {
        Id          = id
        CueId       = cueId
        AutoFollow  = yaml.AutoFollow
        Duration    = yaml.Duration
        Prewait     = yaml.Prewait
      }
    }

#endif

// * CueReference

[<StructuralEquality; StructuralComparison>]
type CueReference =
  { Id:         CueRefId
    CueId:      CueId
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
    either {
      let! id = Id.decodeId fb
      let! cueId = Id.decodeCueId fb
      return {
        Id          = id
        CueId       = cueId
        AutoFollow  = fb.AutoFollow
        Duration    = fb.Duration
        Prewait     = fb.Prewait
      }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueReferenceFB> =
    let id = CueReferenceFB.CreateIdVector(builder,self.Id.ToByteArray())
    let cueId = CueReferenceFB.CreateCueIdVector(builder,self.CueId.ToByteArray())
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

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYaml() = CueReferenceYaml.From(cue)

  // ** FromYaml

  static member FromYaml(yaml: CueReferenceYaml) : Either<IrisError,CueReference> =
    yaml.ToCueReference()

  #endif

// * CueReference module

module CueReference =

  // ** create

  let create (cue: Cue) =
    { Id = IrisId.Create()
      CueId = cue.Id
      AutoFollow = -1
      Duration = -1
      Prewait = -1 }
