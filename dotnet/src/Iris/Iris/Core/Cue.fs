namespace Iris.Core

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

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml
open SharpYaml.Serialization

type CueYaml(id, name, slices) as self =
  [<DefaultValue>] val mutable Id: string
  [<DefaultValue>] val mutable Name: string
  [<DefaultValue>] val mutable Slices: SlicesYaml array

  new () = new CueYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.Slices <- slices

#endif

[<StructuralEquality; StructuralComparison>]
type Cue =
  { Id:     Id
    Name:   string
    Slices: Slices array }

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

  static member FromBytes(bytes: byte[]) : Either<IrisError,Cue> =
    bytes
    |> Binary.createBuffer
    |> CueFB.GetRootAsCueFB
    |> Cue.FromFB

  member self.ToBytes() = Binary.buildBuffer self

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  member self.ToYamlObject() =
    let slices = Array.map Yaml.toYaml self.Slices
    new CueYaml(string self.Id, self.Name, slices)

  static member FromYamlObject(yaml: CueYaml) : Either<IrisError,Cue> =
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

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  static member FromYaml(str: string) : Either<IrisError,Cue> =
    let serializer = new Serializer()
    serializer.Deserialize<CueYaml>(str)
    |> Yaml.fromYaml

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member self.AssetPath
    with get () =
      let filepath =
        sprintf "%s_%s%s"
          (String.sanitize self.Name)
          (string self.Id)
          ASSET_EXTENSION
      CUE_DIR </> filepath

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, Cue> =
    either {
      let! data = Asset.read path
      let! cue = Yaml.decode data
      return cue
    }

  static member LoadAll(basePath: FilePath) : Either<IrisError, Cue array> =
    either {
      try
        let dir = basePath </> CUE_DIR
        let files = Directory.GetFiles(dir, sprintf "*%s" ASSET_EXTENSION)

        let! (_,cues) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate
          Array.fold
            (fun (m: Either<IrisError, int * Cue array>) path ->
              either {
                let! (idx,cues) = m
                let! cue = Cue.Load path
                cues.[idx] <- cue
                return (idx + 1, cues)
              })
            (Right(0, arr))
            files

        return cues
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "Cue.LoadAll"
            |> Either.fail
    }

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member cue.Save (basePath: FilePath) =
    either {
      let path = basePath </> Asset.path cue
      let data = Yaml.encode cue
      let! _ = Asset.write path (Payload data)
      return ()
    }

  #endif
