namespace Iris.Core

#if JAVASCRIPT

open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open SharpYaml.Serialization
open FlatBuffers
open Iris.Serialization.Raft

#endif

type CueList =
  { Id   : Id
  ; Name : Name
  ; Cues : Cue array }

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
#if JAVASCRIPT
      fb.Cues(i)
      |> Cue.FromFB
      |> Option.map (fun cue -> cues.[i] <- cue)
      |> ignore
#else
      let cue = fb.Cues(i)
      if cue.HasValue then
        cue.Value
        |> Cue.FromFB
        |> Option.map (fun cue -> cues.[i] <- cue)
        |> ignore
#endif

    try
      { Id = Id fb.Id
      ; Name = fb.Name
      ; Cues = cues }
      |> Some
    with
      | exn ->
        printfn "Could not seserialize CueList: %s" exn.Message
        None

  static member FromBytes (bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> CueListFB.GetRootAsCueListFB
    |> CueList.FromFB

#if JAVASCRIPT
#else
  member self.ToYaml(serializer: Serializer) =
    serializer.Serialize(self)

  static member FromYaml(str: string) =
    failwith "in a minute"

  member self.DirName
    with get () = "cuelists"

  member self.CanonicalName
    with get () =
      sanitizeName self.Name
      |> sprintf "%s-%s" (string self.Id)

#endif
