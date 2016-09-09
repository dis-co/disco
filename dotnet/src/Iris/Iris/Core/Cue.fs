namespace Iris.Core

#if JAVASCRIPT
#else

open FlatBuffers
open Iris.Serialization.Raft
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

type Cue =
  { Id:      Id
  ; Name:    string
  ; IOBoxes: IOBox array
  }

  static member Type
    with get () = "Iris.Core.Cue"

#if JAVASCRIPT
#else
  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  static member FromFB(fb: CueFB) : Cue option =
    let ioboxes = Array.zeroCreate fb.IOBoxesLength

    for i in 0 .. (fb.IOBoxesLength - 1) do
      fb.GetIOBoxes(i)
      |> IOBox.FromFB
      |> Option.map (fun iobox -> ioboxes.[i] <- iobox)
      |> ignore

    try
      { Id = Id fb.Id
      ; Name = fb.Name
      ; IOBoxes = ioboxes
      } |> Some
    with
      | exn -> None

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

  member self.ToBytes () = Binary.buildBuffer self

  static member FromBytes (bytes: byte array) : Cue option =
    let msg = CueFB.GetRootAsCueFB(new ByteBuffer(bytes))
    Cue.FromFB(msg)

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() =
    let json = new JObject()
    json.Add("$type", new JValue(Cue.Type))
    json.Add("Id", new JValue(string self.Id))
    json.Add("Name", new JValue(self.Name))
    json.Add("IOBoxes", new JArray(Array.map Json.tokenize self.IOBoxes))
    json :> JToken

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : Cue option =
    try
      let tag = string token.["$type"]
      if tag = Cue.Type then
        let ioboxes =
          let jarr = token.["IOBoxes"] :?> JArray
          let arr = Array.zeroCreate jarr.Count

          for i in 0 .. (jarr.Count - 1) do
            Json.parse jarr.[i]
            |> Option.map (fun iobox -> arr.[i] <- iobox; iobox)
            |> ignore

          arr

        { Id = Id (string token.["Id"])
        ; Name = string token.["Name"]
        ; IOBoxes = ioboxes
        }
        |> Some
      else
        failwithf "$type not correct or missing: %s" Cue.Type
    with
      | exn ->
        printfn "Could not deserialize cue json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : Cue option =
    JToken.Parse(str) |> Cue.FromJToken

#endif
