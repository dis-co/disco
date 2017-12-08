namespace rec Disco.Core

// * Imports

open System
open System.Collections.Generic

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Disco.Core.FlatBuffers
open Disco.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Disco.Serialization

#endif

// * PinMapping Yaml

#if !FABLE_COMPILER && !DISCO_NODES

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
    either {
      let! id = DiscoId.TryParse yml.Id
      let! source = DiscoId.TryParse yml.Source
      return {
        Id     = id
        Source = source
        Sinks  = Array.map DiscoId.Parse yml.Sinks |> Set
      }
    }

#endif

// * PinMapping

type PinMapping =
  { Id: PinMappingId
    Source: PinId
    Sinks: Set<PinId> }

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !DISCO_NODES

  member mapping.ToYaml () = PinMappingYaml.From(mapping)

  // ** FromYaml

  static member FromYaml (yml: PinMappingYaml) = yml.ToPinMapping()

  #endif

  // ** FromFB

  static member FromFB (fb: PinMappingFB) =
    either {
      try
        let! id = Id.decodeId fb
        let! source = Id.decodeSource fb
        let sinks =
          [| 0 .. fb.SinksLength - 1 |]
          |> Array.map (fb.Sinks >> DiscoId.Parse)
          |> Set
        return {
          Id = id
          Source = source
          Sinks = sinks
        }
      with exn ->
        return!
          exn.Message
          |> Error.asParseError "PinMapping.FromFB"
          |> Either.fail
    }

  // ** ToOffset

  member mapping.ToOffset(builder: FlatBufferBuilder) =
    let id = PinMappingFB.CreateIdVector(builder,mapping.Id.ToByteArray())
    let source = PinMappingFB.CreateSourceVector(builder,mapping.Source.ToByteArray())
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

  static member FromBytes (bytes: byte[]) : Either<DiscoError,PinMapping> =
    Binary.createBuffer bytes
    |> PinMappingFB.GetRootAsPinMappingFB
    |> PinMapping.FromFB

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER && !DISCO_NODES

  static member Load(path: FilePath) : Either<DiscoError, PinMapping> =
    DiscoData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<DiscoError, PinMapping array> =
    basePath </> filepath Constants.PINMAPPING_DIR
    |> DiscoData.loadAll

  // ** Save

  member mapping.Save (basePath: FilePath) =
    PinMapping.save basePath mapping

  // ** Delete

  member mapping.Delete (basePath: FilePath) =
    DiscoData.delete basePath mapping

  // ** HasParent

  /// Mappings don't live in nested directories, hence false
  member mapping.HasParent with get () = false

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

  #if !FABLE_COMPILER && !DISCO_NODES

  let save basePath (mapping: PinMapping) =
    DiscoData.save basePath mapping

  #endif

  // ** assetPath

  let assetPath (mapping: PinMapping) =
    let fn = (string mapping.Id |> String.sanitize) + ASSET_EXTENSION
    filepath PINMAPPING_DIR </> filepath fn
