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
  [<DefaultValue>] val mutable AutoFollow: Nullable<int>
  [<DefaultValue>] val mutable Duration: Nullable<int>
  [<DefaultValue>] val mutable Prewait: Nullable<int>

  // ** From

  static member From(cue: CueReference) =
    let yaml = CueReferenceYaml()
    yaml.Id <- string cue.Id
    yaml.CueId <- string cue.CueId
    yaml.AutoFollow <- Option.toNullable cue.AutoFollow
    yaml.Duration <- Option.toNullable cue.Duration
    yaml.Prewait <- Option.toNullable cue.Prewait
    yaml

  // ** ToCueReference

  member yaml.ToCueReference() =
    { Id          = Id yaml.Id
      CueId       = Id yaml.CueId    
      AutoFollow  = Option.ofNullable yaml.AutoFollow
      Duration    = Option.ofNullable yaml.Duration
      Prewait     = Option.ofNullable yaml.Prewait }
    |> Right

#endif

// * CueReference

[<StructuralEquality; StructuralComparison>]
type CueReference =
  { Id:         Id
    CueId:      Id
    AutoFollow: int option
    Duration:   int option
    Prewait:    int option
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
      AutoFollow  = Option.ofNullable fb.AutoFollow
      Duration    = Option.ofNullable fb.Duration
      Prewait     = Option.ofNullable fb.Prewait }
    |> Right

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueReferenceFB> =
    let id = string self.Id |> builder.CreateString
    let cueId = string self.CueId |> builder.CreateString
    CueReferenceFB.StartCueReferenceFB(builder)
    CueReferenceFB.AddId(builder, id)
    CueReferenceFB.AddCueId(builder, cueId)
    self.AutoFollow |> Option.iter (fun x -> CueReferenceFB.AddAutoFollow(builder, x))
    self.Duration |> Option.iter (fun x -> CueReferenceFB.AddDuration(builder, x))
    self.Prewait |> Option.iter (fun x -> CueReferenceFB.AddPrewait(builder, x))
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

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member self.AssetPath
    with get () =
      let path = (string self.Id) + ASSET_EXTENSION
      CUE_DIR <.> path

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, CueReference> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, CueReference array> =
    basePath </> filepath CUE_DIR
    |> IrisData.loadAll

  // ** Save

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member cue.Save (basePath: FilePath) =
    IrisData.save basePath cue

  #endif
