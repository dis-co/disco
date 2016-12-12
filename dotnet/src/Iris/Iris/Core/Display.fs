namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

//  ____  _           _
// |  _ \(_)___ _ __ | | __ _ _   _
// | | | | / __| '_ \| |/ _` | | | |
// | |_| | \__ \ |_) | | (_| | |_| |
// |____/|_|___/ .__/|_|\__,_|\__, |
//             |_|            |___/

type Display =
  { Id        : Id
    Name      : Name
    Size      : Rect
    Signals   : Signal array
    RegionMap : RegionMap }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name
    let signals =
      Array.map (Binary.toOffset builder) self.Signals
      |> fun offsets -> DisplayFB.CreateSignalsVector(builder, offsets)
    let map = Binary.toOffset builder self.RegionMap

    DisplayFB.StartDisplayFB(builder)
    DisplayFB.AddId(builder,id)
    DisplayFB.AddName(builder,name)
    DisplayFB.AddSizeX(builder,self.Size.X)
    DisplayFB.AddSizeY(builder,self.Size.Y)
    DisplayFB.AddSignals(builder,signals)
    DisplayFB.AddRegionMap(builder,map)
    DisplayFB.EndDisplayFB(builder)
