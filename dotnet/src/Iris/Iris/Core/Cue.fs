namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open System.IO
open SharpYaml
open SharpYaml.Serialization
open FlatBuffers
open Iris.Serialization.Raft

type CueYaml(id, name, pins) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable Pins : PinYaml array

  new () = new CueYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.Pins <- pins

#endif

type Cue =
  { Id:   Id
    Name: string
    Pins: Pin array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueFB) : Either<IrisError,Cue> =
    either {
      let! pins =
        let arr = Array.zeroCreate fb.PinsLength
        Array.fold
          (fun (m: Either<IrisError,int * Pin array>) _ -> either {
              let! (i, pins) = m

              #if FABLE_COMPILER

              let! pin = i |> fb.Pins |> Pin.FromFB

              #else

              let! pin =
                let nullable = fb.Pins(i)
                if nullable.HasValue then
                  nullable.Value
                  |> Pin.FromFB
                else
                  "Could not parse empty PinFB"
                  |> Error.asParseError "Cue.FromFB"
                  |> Either.fail

              #endif

              pins.[i] <- pin
              return (i + 1, pins)
            })
          (Right (0, arr))
          arr
        |> Either.map snd

      return { Id = Id fb.Id
               Name = fb.Name
               Pins = pins }
    }

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let pinoffsets = Array.map (fun (pin: Pin) -> pin.ToOffset(builder)) self.Pins
    let pins = CueFB.CreatePinsVector(builder, pinoffsets)
    CueFB.StartCueFB(builder)
    CueFB.AddId(builder, id)
    CueFB.AddName(builder, name)
    CueFB.AddPins(builder, pins)
    CueFB.EndCueFB(builder)

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Cue> =
    CueFB.GetRootAsCueFB(Binary.createBuffer bytes)
    |> Cue.FromFB

  member self.ToBytes() = Binary.buildBuffer self

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  #if !FABLE_COMPILER

  member self.ToYamlObject() =
    let pins = Array.map Yaml.toYaml self.Pins
    new CueYaml(string self.Id, self.Name, pins)

  static member FromYamlObject(yaml: CueYaml) : Either<IrisError,Cue> =
    either {
      let! pins =
        let arr = Array.zeroCreate yaml.Pins.Length
        Array.fold
          (fun (m: Either<IrisError,int * Pin array>) box -> either {
            let! (i, arr) = m
            let! (pin : Pin) = Yaml.fromYaml box
            arr.[i] <- pin
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.Pins
        |> Either.map snd

      return { Id = Id yaml.Id
               Name = yaml.Name
               Pins = pins }
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

  #endif

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  #if !FABLE_COMPILER

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
      let! info = Asset.write path (Payload data)
      return ()
    }

  #endif
