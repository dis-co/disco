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

// * CueYaml

open Path

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml
open SharpYaml.Serialization

type CueYaml() =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Slices: SlicesYaml array

  // ** From

  static member From(cue: Cue) =
    let yaml = CueYaml()
    yaml.Id <- string cue.Id
    yaml.Name <- cue.Name
    yaml.Slices <- Array.map Yaml.toYaml cue.Slices
    yaml

  // ** ToCue

  member yaml.ToCue() =
    either {
      let! slices =
        let arr = Array.zeroCreate yaml.Slices.Length
        Array.fold
          (fun (m: Either<IrisError,int * Slices array>) box -> either {
            let! (i, arr) = m
            let! (slice : Slices) = Yaml.fromYaml box
            arr.[i] <- slice
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.Slices
        |> Either.map snd

      return { Id = Id yaml.Id
               Name = yaml.Name
               Slices = slices }
    }

#endif

// * Cue

[<StructuralEquality; StructuralComparison>]
type Cue =
  { Id:     Id
    Name:   string
    Slices: Slices array }

  // ** FromFB

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueFB) : Either<IrisError,Cue> =
    either {
      let! slices =
        let arr = Array.zeroCreate fb.SlicesLength
        Array.fold
          (fun (m: Either<IrisError,int * Slices array>) _ -> either {
              let! (i, slices) = m

              #if FABLE_COMPILER

              let! slice = i |> fb.Slices |> Slices.FromFB

              #else

              let! slice =
                let nullable = fb.Slices(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Slices.FromFB
                else
                  "Could not parse empty SlicesFB"
                  |> Error.asParseError "Cue.FromFB"
                  |> Either.fail

              #endif

              slices.[i] <- slice
              return (i + 1, slices)
            })
          (Right (0, arr))
          arr
        |> Either.map snd

      return { Id = Id fb.Id
               Name = fb.Name
               Slices = slices }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let slices = CueFB.CreateSlicesVector(builder, sliceoffsets)
    CueFB.StartCueFB(builder)
    CueFB.AddId(builder, id)
    CueFB.AddName(builder, name)
    CueFB.AddSlices(builder, slices)
    CueFB.EndCueFB(builder)

  // ** FromBytes

  static member FromBytes(bytes: byte[]) : Either<IrisError,Cue> =
    bytes
    |> Binary.createBuffer
    |> CueFB.GetRootAsCueFB
    |> Cue.FromFB

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** ToYamlObject

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYamlObject() = CueYaml.From(cue)

  // ** FromYamlObject

  static member FromYamlObject(yaml: CueYaml) : Either<IrisError,Cue> =
    yaml.ToCue()

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) : Either<IrisError,Cue> =
    let serializer = Serializer()
    serializer.Deserialize<CueYaml>(str)
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
          (String.sanitize self.Name)
          (string self.Id)
          ASSET_EXTENSION
      CUE_DIR <.> path

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, Cue> =
    IrisData.load path

  // ** LoadAll

  static member LoadAll(basePath: FilePath) : Either<IrisError, Cue array> =
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
