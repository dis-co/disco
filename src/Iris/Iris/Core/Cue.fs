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
    yaml.Name <- unwrap cue.Name
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

      let! id = IrisId.TryParse yaml.Id

      return {
        Id = id
        Name = name yaml.Name
        Slices = slices
      }
    }

#endif

// * Cue

[<StructuralEquality; StructuralComparison>]
type Cue =
  { Id:     CueId
    Name:   Name
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

            let! slice =
              try
                #if FABLE_COMPILER
                i |> fb.Slices |> Slices.FromFB
                #else
                let nullable = fb.Slices(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Slices.FromFB
                else
                  "Could not parse empty SlicesFB"
                  |> Error.asParseError "Cue.FromFB"
                  |> Either.fail
                #endif
              with
                | exn ->
                  exn.Message
                  |> Error.asParseError "Cue.FromtFB"
                  |> Either.fail

            slices.[i] <- slice
            return (i + 1, slices) })
          (Right (0, arr))
          arr
        |> Either.map snd

      let! id = Id.decodeId fb

      return {
        Id = id
        Name = name fb.Name
        Slices = slices
      }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
    let id = CueFB.CreateIdVector(builder,self.Id.ToByteArray())
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let sliceoffsets = Array.map (Binary.toOffset builder) self.Slices
    let slices = CueFB.CreateSlicesVector(builder, sliceoffsets)
    CueFB.StartCueFB(builder)
    CueFB.AddId(builder, id)
    Option.iter (fun value -> CueFB.AddName(builder, value)) name
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

  // ** ToYaml

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member cue.ToYaml() = CueYaml.From(cue)

  // ** FromYaml

  static member FromYaml(yaml: CueYaml) : Either<IrisError,Cue> =
    yaml.ToCue()

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member self.AssetPath
    with get () =
      CUE_DIR <.> sprintf "%s%s" (string self.Id) ASSET_EXTENSION

  // ** HasParent

  /// Cues don't live in nested directories, hence false
  member cue.HasParent with get () = false

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

  member cue.Save (basePath: FilePath) =
    IrisData.save basePath cue

  // ** Delete

  member cue.Delete (basePath: FilePath) =
    IrisData.delete basePath cue

  #endif
