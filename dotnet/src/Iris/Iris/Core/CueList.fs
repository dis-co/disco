namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open SharpYaml.Serialization
// * CueList Yaml

type CueListYaml(id, name, cues) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable Cues : CueYaml array

  new () = new CueListYaml(null, null, null)

  do
    self.Id   <- id
    self.Name <- name
    self.Cues <- cues

#endif

// * CueList

type CueList =
  { Id   : Id
    Name : Name
    Cues : Cue array }

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = self.Id |> string |> builder.CreateString
    let name = self.Name |> unwrap |> builder.CreateString
    let cueoffsets = Array.map (fun (cue: Cue)  -> cue.ToOffset(builder)) self.Cues
    let cuesvec = CueListFB.CreateCuesVector(builder, cueoffsets)
    CueListFB.StartCueListFB(builder)
    CueListFB.AddId(builder, id)
    CueListFB.AddName(builder, name)
    CueListFB.AddCues(builder, cuesvec)
    CueListFB.EndCueListFB(builder)

  // ** ToBytes

  member self.ToBytes() = Binary.buildBuffer self

  // ** FromFB

  static member FromFB(fb: CueListFB) : Either<IrisError, CueList> =
    either {
      let! cues =
        let arr = Array.zeroCreate fb.CuesLength
        Array.fold
          (fun (m: Either<IrisError,int * Cue array>) _ -> either {
            let! (i, cues) = m

            #if FABLE_COMPILER

            let! cue =
              fb.Cues(i)
              |> Cue.FromFB
            #else

            let! cue =
              let value = fb.Cues(i)
              if value.HasValue then
                value.Value
                |> Cue.FromFB
              else
                "Could not parse empty CueFB"
                |> Error.asParseError "CueList.FromFB"
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
               Cues = cues }
    }

  // ** FromBytes

  static member FromBytes (bytes: byte[]) =
    Binary.createBuffer bytes
    |> CueListFB.GetRootAsCueListFB
    |> CueList.FromFB

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER && !IRIS_NODES

  // ** ToYamlObject

  member self.ToYamlObject() =
    new CueListYaml(
      string self.Id,
      unwrap self.Name,
      Array.map Yaml.toYaml self.Cues)

  // ** FromYamlObject

  static member FromYamlObject(yml: CueListYaml) : Either<IrisError,CueList> =
    either {
      let! cues =
        let arr = Array.zeroCreate yml.Cues.Length
        Array.fold
          (fun (m: Either<IrisError,int * Cue array>) cueish -> either {
            let! (i, arr) = m
            let! (cue: Cue) = Yaml.fromYaml cueish
            arr.[i] <- cue
            return (i + 1, arr)
          })
          (Right (0, arr))
          yml.Cues
        |> Either.map snd

      return { Id = Id yml.Id
               Name = name yml.Name
               Cues = cues }
    }

  // ** ToYaml

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  // ** FromYaml

  static member FromYaml(str: string) : Either<IrisError, CueList> =
    let serializer = new Serializer()
    serializer.Deserialize<CueListYaml>(str)
    |> Yaml.fromYaml

  // ** AssetPath

  member self.AssetPath
    with get () =
      let filepath =
        sprintf "%s_%s%s"
          (self.Name |> unwrap |> String.sanitize)
          (string self.Id)
          ASSET_EXTENSION
      CUELIST_DIR </> filepath

  // ** Load

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load(path: FilePath) : Either<IrisError, CueList> =
    either {
      let! data = Asset.read path
      let! cuelist = Yaml.decode data
      return cuelist
    }

  static member LoadAll(basePath: FilePath) : Either<IrisError, CueList array> =
    either {
      try
        let dir = basePath </> CUELIST_DIR
        let files = Directory.GetFiles(dir, sprintf "*%s" ASSET_EXTENSION)

        let! (_,cuelists) =
          let arr =
            files
            |> Array.length
            |> Array.zeroCreate
          Array.fold
            (fun (m: Either<IrisError, int * CueList array>) path ->
              either {
                let! (idx,cuelists) = m
                let! cuelist = CueList.Load path
                cuelists.[idx] <- cuelist
                return (idx + 1, cuelists)
              })
            (Right(0, arr))
            files

        return cuelists
      with
        | exn ->
          return!
            exn.Message
            |> Error.asAssetError "CueList.LoadAll"
            |> Either.fail
    }

  // ** Save

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member cuelist.Save (basePath: FilePath) =
    either {
      let path = basePath </> Asset.path cuelist
      let data = Yaml.encode cuelist
      let! _ = Asset.write path (Payload data)
      return ()
    }

  #endif
