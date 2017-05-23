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
      let! cues =
        let arr = Array.zeroCreate yaml.CueRefs.Length
        Array.fold
          (fun (m: Either<IrisError,int * CueReference array>) cueish -> either {
            let! (i, arr) = m
            let! (cue: CueReference) = Yaml.fromYaml cueish
            arr.[i] <- cue
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.CueRefs
        |> Either.map snd

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
        let arr = Array.zeroCreate fb.CueRefsLength
        Array.fold
          (fun (m: Either<IrisError,int * CueReference array>) _ -> either {
            let! (i, cues) = m

            #if FABLE_COMPILER

            let! cue =
              fb.CueRefs(i)
              |> CueReference.FromFB
            #else

            let! cue =
              let value = fb.CueRefs(i)
              if value.HasValue then
                value.Value
                |> CueReference.FromFB
              else
                "Could not parse empty CueReferenceFB"
                |> Error.asParseError "CueGroup.FromFB"
                |> Either.fail

            #endif

            cues.[i] <- cue
            return (i + 1, cues)
          })
          (Right (0, arr))
          arr
        |> Either.map snd

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

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member self.AssetPath
    with get () =
      let path =
        sprintf "%s_%s%s"
          (self.Name |> unwrap |> String.sanitize)
          (string self.Id)
          ASSET_EXTENSION
      CUEGROUP_DIR <.> path

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, CueGroup> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, CueGroup array> =
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
