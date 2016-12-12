namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

// __     ___               ____            _
// \ \   / (_) _____      _|  _ \ ___  _ __| |_
//  \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
//   \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
//    \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

type ViewPort =
  { Id             : Id
    Name           : Name
    Position       : Coordinate
    Size           : Rect
    OutputPosition : Coordinate
    OutputSize     : Rect
    Overlap        : Rect
    Description    : string }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name
    let desc = builder.CreateString self.Description

    ViewPortFB.StartViewPortFB(builder)
    ViewPortFB.AddId(builder, id)
    ViewPortFB.AddName(builder, name)
    ViewPortFB.AddPositionX(builder, self.Position.X)
    ViewPortFB.AddPositionY(builder, self.Position.Y)
    ViewPortFB.AddSizeX(builder, self.Size.X)
    ViewPortFB.AddSizeY(builder, self.Size.Y)
    ViewPortFB.AddOutputPositionX(builder, self.OutputPosition.X)
    ViewPortFB.AddOutputPositionY(builder, self.OutputPosition.Y)
    ViewPortFB.AddOutputSizeX(builder, self.OutputSize.X)
    ViewPortFB.AddOutputSizeY(builder, self.OutputSize.Y)
    ViewPortFB.AddOverlapX(builder, self.Overlap.X)
    ViewPortFB.AddOverlapY(builder, self.Overlap.Y)
    ViewPortFB.AddDescription(builder, desc)
    ViewPortFB.EndViewPortFB(builder)
