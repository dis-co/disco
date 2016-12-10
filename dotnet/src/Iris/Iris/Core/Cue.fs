namespace Iris.Core

#if FABLE_COMPILER

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

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

#if FABLE_COMPILER
type Cue =
#else
and Cue =
#endif
  { Id:   Id
  ; Name: string
  ; Pins: Pin array }

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
                  |> ParseError
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

#if FABLE_COMPILER
#else
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

  member self.AssetPath
    with get () =
      let filepath =
        sprintf "%s_%s%s"
          (String.sanitize self.Name)
          (string self.Id)
          ASSET_EXTENSION
      CUE_DIR </> filepath

#endif
