namespace Iris.Core

#if JAVASCRIPT
#else

open FlatBuffers
open Iris.Serialization.Raft
open Newtonsoft.Json
open Newtonsoft.Json.Linq

#endif

type CueList =
  { Id   : Id
  ; Name : Name
  ; Cues : Cue array }

#if JAVASCRIPT
#else

  static member Type
    with get () = Serialization.GetTypeName<CueList> ()

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = self.Id |> string |> builder.CreateString
    let name = self.Name |> builder.CreateString
    let cueoffsets = Array.map (fun (cue: Cue)  -> cue.ToOffset(builder)) self.Cues
    let cuesvec = CueListFB.CreateCuesVector(builder, cueoffsets)
    CueListFB.StartCueListFB(builder)
    CueListFB.AddId(builder, id)
    CueListFB.AddName(builder, name)
    CueListFB.AddCues(builder, cuesvec)
    CueListFB.EndCueListFB(builder)

  member self.ToBytes() = Binary.buildBuffer self

  static member FromFB(fb: CueListFB) : CueList option =
    let cues = Array.zeroCreate fb.CuesLength

    for i in 0 .. (fb.CuesLength - 1) do
      fb.GetCues(i)
      |> Cue.FromFB
      |> Option.map (fun cue -> cues.[i] <- cue)
      |> ignore

    try
      { Id = Id fb.Id
      ; Name = fb.Name
      ; Cues = cues }
      |> Some
    with
      | exn ->
        printfn "Could not seserialize CueList: %s" exn.Message
        None

  static member FromBytes (bytes: byte array) =
    CueListFB.GetRootAsCueListFB(new ByteBuffer(bytes))
    |> CueList.FromFB

  //      _
  //     | |___  ___  _ __
  //  _  | / __|/ _ \| '_ \
  // | |_| \__ \ (_) | | | |
  //  \___/|___/\___/|_| |_|

  member self.ToJToken() =
    new JObject()
    |> addString "Id"   (string self.Id)
    |> addString "Name" self.Name
    |> addArray  "Cues" self.Cues

  member self.ToJson() =
    self.ToJToken() |> string

  static member FromJToken(token: JToken) : CueList option =
    try
      let cues =
        let jarr = token.["Cues"] :?> JArray
        let arr = Array.zeroCreate jarr.Count

        for i in 0 .. (jarr.Count - 1) do
          Json.parse jarr.[i]
          |> Option.map (fun cue -> arr.[i] <- cue)
          |> ignore

        arr

      { Id = Id (string token.["Id"])
      ; Name = (string token.["Name"])
      ; Cues = cues }
      |> Some
    with
      | exn ->
        printfn "Could not deserialize cue json: "
        printfn "    Message: %s"  exn.Message
        printfn "    json:    %s" (string token)
        None

  static member FromJson(str: string) : CueList option =
    JToken.Parse(str) |> CueList.FromJToken

#endif
