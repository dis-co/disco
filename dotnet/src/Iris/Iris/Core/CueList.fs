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
      let cue = fb.Cues(i)
      if cue.HasValue then
        cue.Value
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
#endif
