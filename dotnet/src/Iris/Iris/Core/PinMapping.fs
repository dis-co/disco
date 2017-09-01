namespace rec Iris.Core

// * Imports

open System
open System.Collections.Generic

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

// * PinMapping Yaml

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization

type PinMappingYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Source: string
  [<DefaultValue>] val mutable Sinks: string array

  static member From (mapping: PinMapping) =
    let yml: PinMappingYaml = PinMappingYaml()
    yml.Id     <- string mapping.Id
    yml.Source <- string mapping.Source
    yml.Sinks  <- (Set.map string mapping.Sinks |> Array.ofSeq)
    yml

  member yml.ToPinMapping() =
    { Id     = Id yml.Id
      Source = Id yml.Source
      Sinks  = Array.map Id yml.Sinks |> Set }
    |> Either.succeed

#endif

// * PinMapping

type PinMapping =
  { Id: Id
    Source: Id
    Sinks: Set<Id> }

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member mapping.ToYamlObject () = PinMappingYaml.From(mapping)

  // ** ToYaml

  member self.ToYaml (serializer: Serializer) =
    self
    |> Yaml.toYaml
    |> serializer.Serialize

  // ** FromYamlObject

  static member FromYamlObject (yml: PinMappingYaml) = yml.ToPinMapping()

  // ** FromYaml

  static member FromYaml (str: string) : Either<IrisError,PinMapping> =
    let serializer = Serializer()
    let yml = serializer.Deserialize<PinMappingYaml>(str)
    Yaml.fromYaml yml

  #endif


  // ** FromFB

  static member FromFB (fb: PinMappingFB) =
    either {
      let sinks =
        [| 0 .. fb.SinksLength - 1 |]
        |> Array.map (fb.Sinks >> Id)
        |> Set
      return { Id = Id fb.Id
               Source = Id fb.Source
               Sinks = sinks }
    }

  // ** ToOffset

  member mapping.ToOffset(builder: FlatBufferBuilder) =
    let id = mapping.Id |> string |> builder.CreateString
    let source = mapping.Source |> string |> builder.CreateString
    let sinks =
      mapping.Sinks
      |> Array.ofSeq
      |> Array.map (string >> builder.CreateString)
      |> fun arr -> PinMappingFB.CreateSinksVector(builder, arr)
    PinMappingFB.StartPinMappingFB(builder)
    PinMappingFB.AddId(builder, id)
    PinMappingFB.AddSource(builder, source)
    PinMappingFB.AddSinks(builder, sinks)
    PinMappingFB.EndPinMappingFB(builder)

  // ** ToBytes

  member mapping.ToBytes() : byte[] = Binary.buildBuffer mapping

  // ** FromBytes

  static member FromBytes (bytes: byte[]) : Either<IrisError,PinMapping> =
    Binary.createBuffer bytes
    |> PinMappingFB.GetRootAsPinMappingFB
    |> PinMapping.FromFB

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !IRIS_NODES

  static member Load(path: FilePath) : Either<IrisError, PinMapping> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, PinMapping array> =
    basePath </> filepath Constants.PINMAPPING_DIR
    |> IrisData.loadAll

  // ** Save

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member mapping.Save (basePath: FilePath) =
    PinMapping.save basePath mapping

  // ** IsSaved

  member mapping.Exists (basePath: FilePath) =
    basePath </> PinMapping.assetPath mapping
    |> File.exists

  #endif

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member mapping.AssetPath
    with get () = PinMapping.assetPath mapping


// * PinMapping module

module PinMapping =

  // ** source

  let source { Source = source; Sinks = _ } = source

  // ** sinks

  let sinks { Source = _; Sinks = sinks } = sinks

  // ** save

  #if !FABLE_COMPILER && !IRIS_NODES

  let save basePath (mapping: PinMapping) =
    IrisData.save basePath mapping

  #endif

  // ** assetPath

  let assetPath (mapping: PinMapping) =
    let fn = (string mapping.Id |> String.sanitize) + ASSET_EXTENSION
    filepath PINMAPPING_DIR </> filepath fn
