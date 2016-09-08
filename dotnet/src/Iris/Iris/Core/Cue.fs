namespace Iris.Core

#if JAVASCRIPT
#else
open FlatBuffers
open Iris.Serialization.Raft
#endif

type Cue =
  { Id:      Id
  ; Name:    string
  ; IOBoxes: IOBox array
  }
#if JAVASCRIPT
#else
  with
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

#endif
