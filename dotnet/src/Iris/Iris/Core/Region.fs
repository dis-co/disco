namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

//  ____            _
// |  _ \ ___  __ _(_) ___  _ __
// | |_) / _ \/ _` | |/ _ \| '_ \
// |  _ <  __/ (_| | | (_) | | | |
// |_| \_\___|\__, |_|\___/|_| |_|
//            |___/

type Region =
  { Id             : Id
    Name           : Name
    SrcPosition    : Coordinate
    SrcSize        : Rect
    OutputPosition : Coordinate
    OutputSize     : Rect }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name

    RegionFB.StartRegionFB(builder)
    RegionFB.AddId(builder, id)
    RegionFB.AddName(builder, name)
    RegionFB.AddSrcPositionX(builder, self.SrcPosition.X)
    RegionFB.AddSrcPositionY(builder, self.SrcPosition.Y)
    RegionFB.AddSrcSizeX(builder, self.SrcSize.X)
    RegionFB.AddSrcSizeY(builder, self.SrcSize.Y)
    RegionFB.AddOutputPositionX(builder, self.OutputPosition.X)
    RegionFB.AddOutputPositionY(builder, self.OutputPosition.Y)
    RegionFB.AddOutputSizeX(builder, self.OutputSize.X)
    RegionFB.AddOutputSizeY(builder, self.OutputSize.Y)
    RegionFB.EndRegionFB(builder)

//  ____            _             __  __
// |  _ \ ___  __ _(_) ___  _ __ |  \/  | __ _ _ __
// | |_) / _ \/ _` | |/ _ \| '_ \| |\/| |/ _` | '_ \
// |  _ <  __/ (_| | | (_) | | | | |  | | (_| | |_) |
// |_| \_\___|\__, |_|\___/|_| |_|_|  |_|\__,_| .__/
//            |___/                           |_|

type RegionMap =
  { SrcViewportId : Id
    Regions       : Region array }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.SrcViewportId)
    let regions =
      Array.map (Binary.toOffset builder) self.Regions
      |> fun offsets -> RegionMapFB.CreateRegionsVector(builder, offsets)

    RegionMapFB.StartRegionMapFB(builder)
    RegionMapFB.AddSrcViewportId(builder,id)
    RegionMapFB.AddRegions(builder,regions)
    RegionMapFB.EndRegionMapFB(builder)
