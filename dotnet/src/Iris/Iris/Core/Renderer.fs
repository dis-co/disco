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

//  _____         _
// |_   _|_ _ ___| | __
//   | |/ _` / __| |/ /
//   | | (_| \__ \   <
//   |_|\__,_|___/_|\_\

type Argument = (string * string)

type Task =
  { Id             : Id
    Description    : string
    DisplayId      : Id
    AudioStream    : string
    Arguments      : Argument array }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let desc = builder.CreateString self.Description
    let disp = builder.CreateString (string self.DisplayId)
    let audio = builder.CreateString self.AudioStream
    let args =
      self.Arguments
      |> Array.map (fun (x,y) -> builder.CreateString(sprintf "%s;%s" x y))
      |> fun offsets -> TaskFB.CreateArgumentsVector(builder,offsets)

    TaskFB.StartTaskFB(builder)
    TaskFB.AddId(builder,id)
    TaskFB.AddDescription(builder,desc)
    TaskFB.AddDisplayId(builder,disp)
    TaskFB.AddAudioStream(builder,audio)
    TaskFB.AddArguments(builder,args)
    TaskFB.EndTaskFB(builder)

// __     __
// \ \   / /_   ____   ____   __
//  \ \ / /\ \ / /\ \ / /\ \ / /
//   \ V /  \ V /  \ V /  \ V /
//    \_/    \_/    \_/    \_/

type VvvvExe =
  { Executable : FilePath
    Version    : Version
    Required   : bool }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let path = builder.CreateString self.Executable
    let version = builder.CreateString self.Version

    VvvvExeFB.StartVvvvExeFB(builder)
    VvvvExeFB.AddExecutable(builder, path)
    VvvvExeFB.AddVersion(builder,version)
    VvvvExeFB.AddRequired(builder, self.Required)
    VvvvExeFB.EndVvvvExeFB(builder)

type VvvvPlugin =
  { Name : Name
    Path : FilePath }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = builder.CreateString self.Name
    let path = builder.CreateString self.Path

    VvvvPluginFB.StartVvvvPluginFB(builder)
    VvvvPluginFB.AddName(builder,name)
    VvvvPluginFB.AddPath(builder,path)
    VvvvPluginFB.EndVvvvPluginFB(builder)
