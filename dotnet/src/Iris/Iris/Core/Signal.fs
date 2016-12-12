namespace Iris.Core

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

//  ____  _                   _
// / ___|(_) __ _ _ __   __ _| |
// \___ \| |/ _` | '_ \ / _` | |
//  ___) | | (_| | | | | (_| | |
// |____/|_|\__, |_| |_|\__,_|_|
//          |___/

type Signal =
  { Size     : Rect
    Position : Coordinate }

  member self.ToOffset(builder: FlatBufferBuilder) =
    SignalFB.StartSignalFB(builder)
    SignalFB.AddSizeX(builder, self.Size.X)
    SignalFB.AddSizeY(builder, self.Size.Y)
    SignalFB.AddPositionX(builder, self.Position.X)
    SignalFB.AddPositionY(builder, self.Position.Y)
    SignalFB.EndSignalFB(builder)
