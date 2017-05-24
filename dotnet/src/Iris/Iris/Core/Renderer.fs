namespace Iris.Core

// * Imports

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FlatBuffers
open Iris.Serialization

#endif

// * Signal

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

  static member FromFB(fb: SignalFB) =
    either {
      return
        { Size     = Rect(fb.SizeX, fb.SizeY)
          Position = Coordinate(fb.PositionX, fb.PositionY) }
    }

// * Region

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

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString

    RegionFB.StartRegionFB(builder)
    RegionFB.AddId(builder, id)
    Option.iter (fun value -> RegionFB.AddName(builder,value)) name
    RegionFB.AddSrcPositionX(builder, self.SrcPosition.X)
    RegionFB.AddSrcPositionY(builder, self.SrcPosition.Y)
    RegionFB.AddSrcSizeX(builder, self.SrcSize.X)
    RegionFB.AddSrcSizeY(builder, self.SrcSize.Y)
    RegionFB.AddOutputPositionX(builder, self.OutputPosition.X)
    RegionFB.AddOutputPositionY(builder, self.OutputPosition.Y)
    RegionFB.AddOutputSizeX(builder, self.OutputSize.X)
    RegionFB.AddOutputSizeY(builder, self.OutputSize.Y)
    RegionFB.EndRegionFB(builder)

  // ** FromFB

  static member FromFB(fb: RegionFB) =
    either {
      return
        { Id             = Id fb.Id
          Name           = name fb.Name
          SrcSize        = Rect(fb.SrcSizeX,fb.SrcSizeY)
          SrcPosition    = Coordinate(fb.SrcPositionX,fb.SrcPositionY)
          OutputSize     = Rect(fb.OutputSizeX,fb.OutputSizeY)
          OutputPosition = Coordinate(fb.OutputPositionX,fb.OutputPositionY) }
    }

// * RegionMap

//  ____            _             __  __
// |  _ \ ___  __ _(_) ___  _ __ |  \/  | __ _ _ __
// | |_) / _ \/ _` | |/ _ \| '_ \| |\/| |/ _` | '_ \
// |  _ <  __/ (_| | | (_) | | | | |  | | (_| | |_) |
// |_| \_\___|\__, |_|\___/|_| |_|_|  |_|\__,_| .__/
//            |___/                           |_|

type RegionMap =
  { SrcViewportId : Id
    Regions       : Region array }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.SrcViewportId)
    let regions =
      Array.map (Binary.toOffset builder) self.Regions
      |> fun offsets -> RegionMapFB.CreateRegionsVector(builder, offsets)

    RegionMapFB.StartRegionMapFB(builder)
    RegionMapFB.AddSrcViewportId(builder,id)
    RegionMapFB.AddRegions(builder,regions)
    RegionMapFB.EndRegionMapFB(builder)

  // ** FromFB

  static member FromFB(fb: RegionMapFB) =
    either {
      let! (_,regions) =
        let arr =
          fb.RegionsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Region array>) _ ->
            either {
              let! (idx,regions) = m

              let! region =
                #if FABLE_COMPILER
                fb.Regions(idx)
                |> Region.FromFB
                #else
                let regionish = fb.Regions(idx)
                if regionish.HasValue then
                  let value = regionish.Value
                  Region.FromFB value
                else
                  "Could not parse empty RegionFB"
                  |> Error.asParseError "RegionMap.FromFB"
                  |> Either.fail
                #endif

              regions.[idx] <- region
              return (idx + 1, regions)
            })
          (Right(0, arr))
          arr
      return
        { SrcViewportId = Id fb.SrcViewportId
          Regions       = regions }
    }

// * Display

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

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let signals =
      Array.map (Binary.toOffset builder) self.Signals
      |> fun offsets -> DisplayFB.CreateSignalsVector(builder, offsets)
    let map = Binary.toOffset builder self.RegionMap

    DisplayFB.StartDisplayFB(builder)
    DisplayFB.AddId(builder,id)
    Option.iter (fun value -> DisplayFB.AddName(builder,value)) name
    DisplayFB.AddSizeX(builder,self.Size.X)
    DisplayFB.AddSizeY(builder,self.Size.Y)
    DisplayFB.AddSignals(builder,signals)
    DisplayFB.AddRegionMap(builder,map)
    DisplayFB.EndDisplayFB(builder)

  // ** FromFB

  static member FromFB(fb: DisplayFB) =
    either {
      let! (_,signals) =
        let arr =
          fb.SignalsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Signal array>) _ ->
            either {
              let! (idx, signals) = m

              let! signal =
                #if FABLE_COMPILER
                fb.Signals(idx)
                |> Signal.FromFB
                #else
                let signalish = fb.Signals(idx)
                if signalish.HasValue then
                  let value = signalish.Value
                  Signal.FromFB value
                else
                  "Could not parse empty SignalFB"
                  |> Error.asParseError "Display.FromFB"
                  |> Either.fail
                #endif

              signals.[idx] <- signal
              return (idx + 1, signals)
            })
          (Right(0, arr))
          arr

      let! regionmap =
        #if FABLE_COMPILER
        RegionMap.FromFB fb.RegionMap
        #else
        let mapish = fb.RegionMap
        if mapish.HasValue then
          let value = mapish.Value
          RegionMap.FromFB value
        else
          "Could not parse empty RegionMap"
          |> Error.asParseError "Display.FromFB"
          |> Either.fail
        #endif

      return
        { Id        = Id fb.Id
          Name      = name fb.Name
          Size      = Rect(fb.SizeX, fb.SizeY)
          Signals   = signals
          RegionMap = regionmap }
    }

// * ViewPort

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

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let desc = Option.mapNull builder.CreateString self.Description

    ViewPortFB.StartViewPortFB(builder)
    ViewPortFB.AddId(builder, id)
    Option.iter (fun value -> ViewPortFB.AddName(builder,value)) name
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
    Option.iter (fun value -> ViewPortFB.AddDescription(builder,value)) desc
    ViewPortFB.EndViewPortFB(builder)

  // ** FromFB

  static member FromFB(fb: ViewPortFB) =
    either {
      return
        { Id = Id fb.Id
          Name = name fb.Name
          Description = fb.Description
          Size = Rect(fb.SizeX, fb.SizeY)
          Position = Coordinate(fb.PositionX, fb.PositionY)
          OutputSize = Rect(fb.OutputSizeX, fb.OutputSizeY)
          OutputPosition = Coordinate(fb.OutputPositionX, fb.OutputPositionY)
          Overlap = Rect(fb.OverlapX, fb.OverlapY) }
    }

// * Task

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

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let desc = Option.mapNull builder.CreateString self.Description
    let disp = builder.CreateString (string self.DisplayId)
    let audio = Option.mapNull builder.CreateString self.AudioStream
    let args =
      self.Arguments
      |> Array.map (fun (x,y) ->
                    match x, y with
                    | null, null -> builder.CreateString(sprintf "%A;%A" x y)
                    | null, _ -> builder.CreateString(sprintf "%A;%s" x y)
                    | _, null -> builder.CreateString(sprintf "%s;%A" x y)
                    | _, _ -> builder.CreateString(sprintf "%s;%s" x y))
      |> fun offsets -> TaskFB.CreateArgumentsVector(builder,offsets)

    TaskFB.StartTaskFB(builder)
    TaskFB.AddId(builder,id)
    Option.iter (fun value -> TaskFB.AddDescription(builder,value)) desc
    TaskFB.AddDisplayId(builder,disp)
    Option.iter (fun value -> TaskFB.AddAudioStream(builder,value)) audio
    TaskFB.AddArguments(builder,args)
    TaskFB.EndTaskFB(builder)

  // ** FromFB

  static member FromFB(fb: TaskFB) =
    either {
      let! (_,arguments) =
        let arr =
          fb.ArgumentsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Argument array>) _ ->
            either {
              let! (idx,args) = m
              let! arg =
                let str = fb.Arguments(idx)
                let nstr = sprintf "%A" null
                match String.split [| ';' |] str with
                | [| x; y; |] ->
                  match x, y with
                  | _,_ when x = nstr && y = nstr -> Right (null,null)
                  | _,y when x = nstr -> Right (null,y)
                  | x,_ when y = nstr -> Right (x,null)
                  | x,y -> Right (x,y)
                | _ ->
                  sprintf "Argument has wrong format: %s" str
                  |> Error.asParseError "Task.FromFB"
                  |> Either.fail
              args.[idx] <- arg
              return (idx + 1, args)
            })
          (Right(0,arr))
          arr

      return
        { Id          = Id fb.Id
          Description = fb.Description
          DisplayId   = Id fb.DisplayId
          AudioStream = fb.AudioStream
          Arguments   = arguments }
    }

// * VvvvExe

// __     __                    _____
// \ \   / /_   ____   ____   _| ____|_  _____
//  \ \ / /\ \ / /\ \ / /\ \ / /  _| \ \/ / _ \
//   \ V /  \ V /  \ V /  \ V /| |___ >  <  __/
//    \_/    \_/    \_/    \_/ |_____/_/\_\___|

type VvvvExe =
  { Executable : FilePath
    Version    : Version
    Required   : bool }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let path = self.Executable |> unwrap |> Option.mapNull builder.CreateString
    let version = self.Version |> unwrap |> Option.mapNull builder.CreateString

    VvvvExeFB.StartVvvvExeFB(builder)
    Option.iter (fun value -> VvvvExeFB.AddExecutable(builder,value)) path
    Option.iter (fun value -> VvvvExeFB.AddVersion(builder,value)) version
    VvvvExeFB.AddRequired(builder, self.Required)
    VvvvExeFB.EndVvvvExeFB(builder)

  // ** FromFB

  static member FromFB(fb: VvvvExeFB) =
    either {
      return
        { Executable = filepath fb.Executable
          Version    = version fb.Version
          Required   = fb.Required }
    }

// * VvvvPlugin

// __     __                    ____  _             _
// \ \   / /_   ____   ____   _|  _ \| |_   _  __ _(_)_ __
//  \ \ / /\ \ / /\ \ / /\ \ / / |_) | | | | |/ _` | | '_ \
//   \ V /  \ V /  \ V /  \ V /|  __/| | |_| | (_| | | | | |
//    \_/    \_/    \_/    \_/ |_|   |_|\__,_|\__, |_|_| |_|
//                                            |___/

type VvvvPlugin =
  { Name : Name
    Path : FilePath }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let path = self.Path |> unwrap |> Option.mapNull builder.CreateString

    VvvvPluginFB.StartVvvvPluginFB(builder)
    Option.iter (fun value -> VvvvPluginFB.AddName(builder,value)) name
    Option.iter (fun value -> VvvvPluginFB.AddPath(builder,value)) path
    VvvvPluginFB.EndVvvvPluginFB(builder)

  // ** FromFB

  static member FromFB(fb: VvvvPluginFB) =
    either {
      return
        { Name = name fb.Name
          Path = filepath fb.Path }
    }
