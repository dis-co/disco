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
      try
        { Id = Id fb.Id
        ; Name = fb.Name
        ; IOBoxes = [| |]
        } |> Some
      with
        | exn -> None

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<CueFB> =
      let id = string self.Id |> builder.CreateString
      let name = self.Name |> builder.CreateString
      CueFB.StartCueFB(builder)
      CueFB.AddId(builder, id)
      CueFB.AddName(builder, name)
      CueFB.EndCueFB(builder)

    member self.ToBytes () =
      let builder = new FlatBufferBuilder(1)
      let offset = self.ToOffset(builder)
      builder.Finish(offset.Value)
      builder.SizedByteArray()

    static member FromBytes (bytes: byte array) : Cue option =
      let msg = CueFB.GetRootAsCueFB(new ByteBuffer(bytes))
      Cue.FromFB(msg)

#endif
