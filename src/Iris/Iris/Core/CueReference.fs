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
  [<DefaultValue>] val mutable AutoFollow: bool
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
    AutoFollow: bool
    Duration:   int
    Prewait:    int
   //Trigger:  Event option
  }

  // ** optics

  static member Id_ =
    (fun (ref:CueReference) -> ref.Id),
    (fun id (ref:CueReference) -> { ref with Id = id })

  static member CueId_ =
    (fun (ref:CueReference) -> ref.CueId),
    (fun cueId (ref:CueReference) -> { ref with CueId = cueId })

  static member AutoFollow_ =
    (fun (ref:CueReference) -> ref.AutoFollow),
    (fun autoFollow (ref:CueReference) -> { ref with AutoFollow = autoFollow })

  static member Duration_ =
    (fun (ref:CueReference) -> ref.Duration),
    (fun duration (ref:CueReference) -> { ref with Duration = duration })

  static member Prewait_ =
    (fun (ref:CueReference) -> ref.Prewait),
    (fun prewait (ref:CueReference) -> { ref with Prewait = prewait })

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

[<RequireQualifiedAccess>]
module CueReference =

  open Aether

  // ** create

  let create (cueId: CueId) =
    { Id = IrisId.Create()
      CueId = cueId
      AutoFollow = false
      Duration = -1
      Prewait = -1 }

  // ** ofCue

  let ofCue (cue: Cue) = create cue.Id

  // ** getters

  let id = Optic.get CueReference.Id_
  let cueId = Optic.get CueReference.CueId_
  let autoFollow = Optic.get CueReference.AutoFollow_
  let duration = Optic.get CueReference.Duration_
  let prewait = Optic.get CueReference.Prewait_

  // ** setters

  let setId = Optic.set CueReference.Id_
  let setCueId = Optic.set CueReference.CueId_
  let setAutoFollow = Optic.set CueReference.AutoFollow_
  let setDuration = Optic.set CueReference.Duration_
  let setPrewait = Optic.set CueReference.Prewait_
