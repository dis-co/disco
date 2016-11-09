namespace Iris.Core

#if JAVASCRIPT

open Fable.Core
open Fable.Import
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open SharpYaml
open SharpYaml.Serialization
open FlatBuffers
open Iris.Serialization.Raft

type CueYaml(id, name, ioboxes) as self =
  [<DefaultValue>] val mutable Id   : string
  [<DefaultValue>] val mutable Name : string
  [<DefaultValue>] val mutable IOBoxes : IOBoxYaml array

  new () = new CueYaml(null, null, null)

  do
    self.Id <- id
    self.Name <- name
    self.IOBoxes <- ioboxes

#endif

#if JAVASCRIPT
type Cue =
#else
and Cue =
#endif
  { Id:      Id
  ; Name:    string
  ; IOBoxes: IOBox array }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueFB) : Either<IrisError,Cue> =
    either {
      let! ioboxes =
        let arr = Array.zeroCreate fb.IOBoxesLength
        Array.fold
          (fun (m: Either<IrisError,int * IOBox array>) _ -> either {
              let! (i, ioboxes) = m

              #if JAVASCRIPT

              let! iobox = i |> fb.IOBoxes |> IOBox.FromFB

              #else

              let! iobox =
                let nullable = fb.IOBoxes(i)
                if nullable.HasValue then
                  nullable.Value
                  |> IOBox.FromFB
                else
                  "Could not parse empty IOBoxFB"
                  |> ParseError
                  |> Either.fail

              #endif

              ioboxes.[i] <- iobox
              return (i + 1, ioboxes)
            })
          (Right (0, arr))
          arr
        |> Either.map snd

      return { Id = Id fb.Id
               Name = fb.Name
               IOBoxes = ioboxes }
    }

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
    let id = string self.Id |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let ioboxoffsets = Array.map (fun (iobox: IOBox) -> iobox.ToOffset(builder)) self.IOBoxes
    let ioboxes = CueFB.CreateIOBoxesVector(builder, ioboxoffsets)
    CueFB.StartCueFB(builder)
    CueFB.AddId(builder, id)
    CueFB.AddName(builder, name)
    CueFB.AddIOBoxes(builder, ioboxes)
    CueFB.EndCueFB(builder)

  static member FromBytes(bytes: Binary.Buffer) : Either<IrisError,Cue> =
    CueFB.GetRootAsCueFB(Binary.createBuffer bytes)
    |> Cue.FromFB

  member self.ToBytes() = Binary.buildBuffer self

#if JAVASCRIPT
#else
  member self.ToYamlObject() =
    let ioboxes = Array.map Yaml.toYaml self.IOBoxes
    new CueYaml(string self.Id, self.Name, ioboxes)

  static member FromYamlObject(yaml: CueYaml) : Either<IrisError,Cue> =
    either {
      let! ioboxes =
        let arr = Array.zeroCreate yaml.IOBoxes.Length
        Array.fold
          (fun (m: Either<IrisError,int * IOBox array>) box -> either {
            let! (i, arr) = m
            let! (iobox : IOBox) = Yaml.fromYaml box
            arr.[i] <- iobox
            return (i + 1, arr)
          })
          (Right (0, arr))
          yaml.IOBoxes
        |> Either.map snd

      return { Id = Id yaml.Id
               Name = yaml.Name
               IOBoxes = ioboxes }
    }

  member self.ToYaml(serializer: Serializer) =
    Yaml.toYaml self |> serializer.Serialize

  static member FromYaml(str: string) : Either<IrisError,Cue> =
    let serializer = new Serializer()
    serializer.Deserialize<CueYaml>(str)
    |> Yaml.fromYaml

  member self.DirName
    with get () = "cues"

  member self.CanonicalName
    with get () =
      sanitizeName self.Name
      |> sprintf "%s-%s" (string self.Id)
#endif
