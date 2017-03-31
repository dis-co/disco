namespace Iris.Core

// * Imports

open System
open System.IO
open System.Linq
open System.Net
open System.Text
open System.Reflection
open System.Collections.Generic
open Iris.Core.Utils
open Iris.Raft

#if FABLE_COMPILER

open Fable.Core
open Iris.Core.FlatBuffers
open Iris.Web.Core.FlatBufferTypes

#else

open FSharpx.Functional
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

open FSharp.Configuration
open LibGit2Sharp
open SharpYaml.Serialization

#endif

// * RaftConfig

//  ____        __ _    ____             __ _
// |  _ \ __ _ / _| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _` | |_| __| |   / _ \| '_ \| |_| |/ _` |
// |  _ < (_| |  _| |_| |__| (_) | | | |  _| | (_| |
// |_| \_\__,_|_|  \__|\____\___/|_| |_|_| |_|\__, |
//                                            |___/

/// ## RaftConfig
///
/// Configuration for Raft-specific, user-facing values.
///
type RaftConfig =
  { RequestTimeout:   Long
    ElectionTimeout:  Long
    MaxLogDepth:      Long
    LogLevel:         Iris.Core.LogLevel
    DataDir:          FilePath
    MaxRetries:       uint8
    PeriodicInterval: uint8 }

  static member Default =
    { RequestTimeout   = 500u
      ElectionTimeout  = 6000u
      MaxLogDepth      = 20u
      MaxRetries       = 10uy
      PeriodicInterval = 50uy
      LogLevel         = LogLevel.Err
      DataDir          = "" }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let lvl = builder.CreateString (string self.LogLevel)
    let dir = builder.CreateString self.DataDir

    RaftConfigFB.StartRaftConfigFB(builder)
    RaftConfigFB.AddRequestTimeout(builder, self.RequestTimeout)
    RaftConfigFB.AddElectionTimeout(builder, self.ElectionTimeout)
    RaftConfigFB.AddMaxLogDepth(builder, self.MaxLogDepth)
    RaftConfigFB.AddLogLevel(builder, lvl)
    RaftConfigFB.AddDataDir(builder, dir)
    RaftConfigFB.AddMaxRetries(builder, uint16 self.MaxRetries)
    RaftConfigFB.AddPeriodicInterval(builder, uint16 self.PeriodicInterval)
    RaftConfigFB.EndRaftConfigFB(builder)

  static member FromFB(fb: RaftConfigFB) =
    either {
      let! level = Iris.Core.LogLevel.TryParse fb.LogLevel
      return
        { RequestTimeout   = fb.RequestTimeout
          ElectionTimeout  = fb.ElectionTimeout
          MaxLogDepth      = fb.MaxLogDepth
          LogLevel         = level
          DataDir          = fb.DataDir
          MaxRetries       = uint8 fb.MaxRetries
          PeriodicInterval = uint8 fb.PeriodicInterval }
    }

// * VvvvConfig

// __     __                     ____             __ _
// \ \   / /_   ____   ____   __/ ___|___  _ __  / _(_) __ _
//  \ \ / /\ \ / /\ \ / /\ \ / / |   / _ \| '_ \| |_| |/ _` |
//   \ V /  \ V /  \ V /  \ V /| |__| (_) | | | |  _| | (_| |
//    \_/    \_/    \_/    \_/  \____\___/|_| |_|_| |_|\__, |
//                                                     |___/

type VvvvConfig =
  { Executables : VvvvExe array
    Plugins     : VvvvPlugin array }

  static member Default =
    { Executables = [| |]
      Plugins     = [| |] }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let exes =
      Array.map (Binary.toOffset builder) self.Executables
      |> fun offsets -> VvvvConfigFB.CreateExecutablesVector(builder,offsets)

    let plugins =
      Array.map (Binary.toOffset builder) self.Plugins
      |> fun offsets -> VvvvConfigFB.CreatePluginsVector(builder,offsets)

    VvvvConfigFB.StartVvvvConfigFB(builder)
    VvvvConfigFB.AddExecutables(builder, exes)
    VvvvConfigFB.AddPlugins(builder, plugins)
    VvvvConfigFB.EndVvvvConfigFB(builder)

  static member FromFB(fb: VvvvConfigFB) =
    either {
      let! (_,exes) =
        let arr =
          fb.ExecutablesLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * VvvvExe array>) _ ->
            either {
              let! (idx, exes) = m

              let! exe =
                #if FABLE_COMPILER
                fb.Executables(idx)
                |> VvvvExe.FromFB
                #else
                let exeish = fb.Executables(idx)
                if exeish.HasValue then
                  let value = exeish.Value
                  VvvvExe.FromFB value
                else
                  "Could not parse empty VvvvExeFB"
                  |> Error.asParseError "VvvvConfig.FromFB"
                  |> Either.fail
                #endif

              exes.[idx] <- exe
              return (idx + 1, exes)
            })
          (Right(0, arr))
          arr

      let! (_,plugins) =
        let arr =
          fb.PluginsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * VvvvPlugin array>) _ ->
            either {
              let! (idx, plugins) = m

              let! plugin =
                #if FABLE_COMPILER
                fb.Plugins(idx)
                |> VvvvPlugin.FromFB
                #else
                let plugish = fb.Plugins(idx)
                if plugish.HasValue then
                  let value = plugish.Value
                  VvvvPlugin.FromFB value
                else
                  "Could not parse empty VvvvPluginFB"
                  |> Error.asParseError "VvvvConfig.FromFB"
                  |> Either.fail
                #endif

              plugins.[idx] <- plugin
              return (idx + 1, plugins)
            })
          (Right(0, arr))
          arr

      return
        { Executables = exes
          Plugins = plugins }
    }

// * TimingConfig

//  _____ _           _              ____             __ _
// |_   _(_)_ __ ___ (_)_ __   __ _ / ___|___  _ __  / _(_) __ _
//   | | | | '_ ` _ \| | '_ \ / _` | |   / _ \| '_ \| |_| |/ _` |
//   | | | | | | | | | | | | | (_| | |__| (_) | | | |  _| | (_| |
//   |_| |_|_| |_| |_|_|_| |_|\__, |\____\___/|_| |_|_| |_|\__, |
//                            |___/                        |___/

type TimingConfig =
  { Framebase : uint32
    Input     : string
    Servers   : IpAddress array
    UDPPort   : uint32
    TCPPort   : uint32 }

  static member Default =
    { Framebase = 50u
      Input     = "Iris Freerun"
      Servers   = [| |]
      UDPPort   = 8071u
      TCPPort   = 8072u }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let input = builder.CreateString self.Input
    let servers =
      Array.map (string >> builder.CreateString) self.Servers
      |> fun offsets -> TimingConfigFB.CreateServersVector(builder,offsets)

    TimingConfigFB.StartTimingConfigFB(builder)
    TimingConfigFB.AddFramebase(builder,self.Framebase)
    TimingConfigFB.AddInput(builder, input)
    TimingConfigFB.AddServers(builder, servers)
    TimingConfigFB.AddUDPPort(builder, self.UDPPort)
    TimingConfigFB.AddTCPPort(builder, self.TCPPort)
    TimingConfigFB.EndTimingConfigFB(builder)

  static member FromFB(fb: TimingConfigFB) =
    either {
      let! (_,servers) =
        let arr =
          fb.ServersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * IpAddress array>) _ ->
            either {
              let! (idx,servers) = m
              let! server =
                fb.Servers(idx)
                |> IpAddress.TryParse
              servers.[idx] <- server
              return (idx + 1, servers)
            })
          (Right(0, arr))
          arr

      return
        { Framebase = fb.Framebase
          Input     = fb.Input
          Servers   = servers
          UDPPort   = fb.UDPPort
          TCPPort   = fb.TCPPort }
    }

// * AudioConfig

//     _             _ _        ____             __ _
//    / \  _   _  __| (_) ___  / ___|___  _ __  / _(_) __ _
//   / _ \| | | |/ _` | |/ _ \| |   / _ \| '_ \| |_| |/ _` |
//  / ___ \ |_| | (_| | | (_) | |__| (_) | | | |  _| | (_| |
// /_/   \_\__,_|\__,_|_|\___/ \____\___/|_| |_|_| |_|\__, |
//                                                    |___/

type AudioConfig =
  { SampleRate : uint32 }

  static member Default =
    { SampleRate = 48000u }

  member self.ToOffset(builder: FlatBufferBuilder) =
    AudioConfigFB.StartAudioConfigFB(builder)
    AudioConfigFB.AddSampleRate(builder, self.SampleRate)
    AudioConfigFB.EndAudioConfigFB(builder)

  static member FromFB(fb: AudioConfigFB) =
    either {
      return { SampleRate = fb.SampleRate }
    }

// * HostGroup

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

type HostGroup =
  { Name    : Name
    Members : Id array }

  override self.ToString() =
    sprintf "HostGroup:
              Name: %A
              Members: %A"
            self.Name
            (Array.fold (fun m s -> m + " " + string s) "" self.Members)

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = builder.CreateString self.Name

    let members =
      Array.map (string >> builder.CreateString) self.Members
      |> fun offsets -> HostGroupFB.CreateMembersVector(builder, offsets)

    HostGroupFB.StartHostGroupFB(builder)
    HostGroupFB.AddName(builder,name)
    HostGroupFB.AddMembers(builder,members)
    HostGroupFB.EndHostGroupFB(builder)

  static member FromFB(fb: HostGroupFB) =
    either {
      let! (_,members) =
        let arr =
          fb.MembersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Id array>) _ ->
            either {
              let! (idx, ids) = m
              let id = Id (fb.Members(idx))
              ids.[idx] <- id
              return (idx + 1, ids)
            })
          (Right(0, arr))
          arr

      return
        { Name    = fb.Name
          Members = members }
    }

// * Cluster

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type ClusterConfig =
  { Id: Id
    Name: Name
    Members: Map<MemberId,RaftMember>
    Groups: HostGroup array }

  // ** Default

  static member Default
    with get () =
      { Id      = Id.Create()
        Name    = Constants.EMPTY
        Members = Map.empty
        Groups  = [| |] }

  override self.ToString() =
    sprintf "Cluster [Id: %s Name: %A Members: %d Groups: %d]"
      (string self.Id)
      self.Name
      (Map.fold (fun m _ _ -> m + 1) 0 self.Members)
      (Array.length self.Groups)

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name

    let members =
      self.Members
      |> Map.toArray
      |> Array.map (snd >> Binary.toOffset builder)
      |> fun offsets -> ClusterConfigFB.CreateMembersVector(builder, offsets)

    let groups =
      Array.map (Binary.toOffset builder) self.Groups
      |> fun offsets -> ClusterConfigFB.CreateGroupsVector(builder, offsets)

    ClusterConfigFB.StartClusterConfigFB(builder)
    ClusterConfigFB.AddId(builder, id)
    ClusterConfigFB.AddName(builder, name)
    ClusterConfigFB.AddMembers(builder, members)
    ClusterConfigFB.AddGroups(builder, groups)
    ClusterConfigFB.EndClusterConfigFB(builder)

  static member FromFB(fb: ClusterConfigFB) =
    either {
      let! (_,members) =
        let arr =
          fb.MembersLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Map<MemberId,RaftMember>>) _ ->
            either {
              let! (idx,members) = m

              let! mem =
                #if FABLE_COMPILER
                fb.Members(idx)
                |> RaftMember.FromFB
                #else
                let memish = fb.Members(idx)
                if memish.HasValue then
                  let value = memish.Value
                  RaftMember.FromFB value
                else
                  "Could not parse empty RaftMemberFB"
                  |> Error.asParseError "Cluster.FromFB"
                  |> Either.fail
                #endif

              return (idx + 1, Map.add mem.Id mem members)
            })
          (Right(0, Map.empty))
          arr

      let! (_,groups) =
        let arr =
          fb.GroupsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * HostGroup array>) _ ->
            either {
              let! (idx,groups) = m

              let! group =
                #if FABLE_COMPILER
                fb.Groups(idx)
                |> HostGroup.FromFB
                #else
                let groupish = fb.Groups(idx)
                if groupish.HasValue then
                  let value = groupish.Value
                  HostGroup.FromFB value
                else
                  "Could not parse empty HostGroupFB"
                  |> Error.asParseError "Cluster.FromFB"
                  |> Either.fail
                #endif

              groups.[idx] <- group
              return (idx + 1, groups)
            })
          (Right(0, arr))
          arr

      return
        { Id      = Id fb.Id
          Name    = fb.Name
          Members = members
          Groups  = groups }
    }

// * IrisConfig

//  ___      _      ____             __ _
// |_ _|_ __(_)___ / ___|___  _ __  / _(_) __ _
//  | || '__| / __| |   / _ \| '_ \| |_| |/ _` |
//  | || |  | \__ \ |__| (_) | | | |  _| | (_| |
// |___|_|  |_|___/\____\___/|_| |_|_| |_|\__, |
//                                        |___/

type IrisConfig =
  { MachineId:  Id
    ActiveSite: Id option
    Version:    string
    Audio:      AudioConfig
    Vvvv:       VvvvConfig
    Raft:       RaftConfig
    Timing:     TimingConfig
    Sites:      ClusterConfig array
    ViewPorts:  ViewPort array
    Displays:   Display  array
    Tasks:      Task     array }

  // ** Default
  static member Default
    with get () =
      { MachineId = Id Constants.EMPTY
        ActiveSite = None
        #if FABLE_COMPILER
        Version   = "0.0.0"
        #else
        Version   = System.Version(0,0,0).ToString()
        #endif
        Audio     = AudioConfig.Default
        Vvvv      = VvvvConfig.Default
        Raft      = RaftConfig.Default
        Timing    = TimingConfig.Default
        Sites     = [| |]
        ViewPorts = [| |]
        Displays  = [| |]
        Tasks     = [| |] }

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let version = builder.CreateString self.Version
    let machine = builder.CreateString (string self.MachineId)
    let audio = Binary.toOffset builder self.Audio
    let vvvv = Binary.toOffset builder self.Vvvv
    let raft = Binary.toOffset builder self.Raft
    let timing = Binary.toOffset builder self.Timing

    let site =
      match self.ActiveSite with
      | Some id -> id |> string |> builder.CreateString
      | None -> "" |> builder.CreateString

    let sites =
      Array.map (Binary.toOffset builder) self.Sites
      |> fun sites -> ConfigFB.CreateSitesVector(builder, sites)

    let viewports =
      Array.map (Binary.toOffset builder) self.ViewPorts
      |> fun vps -> ConfigFB.CreateViewPortsVector(builder, vps)

    let displays =
      Array.map (Binary.toOffset builder) self.Displays
      |> fun disps -> ConfigFB.CreateDisplaysVector(builder, disps)

    let tasks =
      Array.map (Binary.toOffset builder) self.Tasks
      |> fun tasks -> ConfigFB.CreateTasksVector(builder, tasks)

    ConfigFB.StartConfigFB(builder)
    ConfigFB.AddVersion(builder, version)
    ConfigFB.AddMachineId(builder, machine)
    ConfigFB.AddActiveSite(builder, site)
    ConfigFB.AddAudioConfig(builder, audio)
    ConfigFB.AddVvvvConfig(builder, vvvv)
    ConfigFB.AddRaftConfig(builder, raft)
    ConfigFB.AddTimingConfig(builder, timing)
    ConfigFB.AddSites(builder, sites)
    ConfigFB.AddViewPorts(builder, viewports)
    ConfigFB.AddDisplays(builder, displays)
    ConfigFB.AddTasks(builder, tasks)
    ConfigFB.EndConfigFB(builder)

  static member FromFB(fb: ConfigFB) =
    either {
      let machineId = Id fb.MachineId
      let version = fb.Version

      let site =
        if isNull fb.ActiveSite || fb.ActiveSite = "" then
          None
        else
          Some (Id fb.ActiveSite)

      let! audio =
        #if FABLE_COMPILER
        AudioConfig.FromFB fb.AudioConfig
        #else
        let audioish = fb.AudioConfig
        if audioish.HasValue then
          let value = audioish.Value
          AudioConfig.FromFB value
        else
          "Could not parse empty AudioConfigFB"
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

      let! vvvv =
        #if FABLE_COMPILER
        VvvvConfig.FromFB fb.VvvvConfig
        #else
        let vvvvish = fb.VvvvConfig
        if vvvvish.HasValue then
          let value = vvvvish.Value
          VvvvConfig.FromFB value
        else
          "Could not parse empty VvvvConfigFB"
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

      let! raft =
        #if FABLE_COMPILER
        RaftConfig.FromFB fb.RaftConfig
        #else
        let raftish = fb.RaftConfig
        if raftish.HasValue then
          let value = raftish.Value
          RaftConfig.FromFB value
        else
          "Could not parse empty RaftConfigFB"
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

      let! timing =
        #if FABLE_COMPILER
        TimingConfig.FromFB fb.TimingConfig
        #else
        let timingish = fb.TimingConfig
        if timingish.HasValue then
          let value = timingish.Value
          TimingConfig.FromFB value
        else
          "Could not parse empty TimingConfigFB"
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

      let! (_, sites) =
        let arr =
          fb.SitesLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * ClusterConfig array>) _ ->
            either {
              let! (idx, sites) = m
              let! site =
                #if FABLE_COMPILER
                fb.Sites(idx)
                |> ClusterConfig.FromFB
                #else
                let clusterish = fb.Sites(idx)
                if clusterish.HasValue then
                  let value = clusterish.Value
                  ClusterConfig.FromFB value
                else
                  "Could not parse empty ClusterConfigFB"
                  |> Error.asParseError "IrisConfig.FromFB"
                  |> Either.fail
                #endif
              sites.[idx] <- site
              return (idx + 1, sites)
            })
            (Right(0, arr))
            arr

      let! (_,viewports) =
        let arr =
          fb.ViewPortsLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * ViewPort array>) _ ->
            either {
              let! (idx, viewports) = m
              let! viewport =
                #if FABLE_COMPILER
                fb.ViewPorts(idx)
                |> ViewPort.FromFB
                #else
                let vpish = fb.ViewPorts(idx)
                if vpish.HasValue then
                  let value = vpish.Value
                  ViewPort.FromFB value
                else
                  "Could not parse empty ViewPortFB"
                  |> Error.asParseError "IrisConfig.FromFB"
                  |> Either.fail
                #endif
              viewports.[idx] <- viewport
              return (idx + 1, viewports)
            })
          (Right(0, arr))
          arr

      let! (_,displays) =
        let arr =
          fb.DisplaysLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Display array>) _ ->
            either {
              let! (idx, displays) = m
              let! display =
                #if FABLE_COMPILER
                fb.Displays(idx)
                |> Display.FromFB
                #else
                let dispish = fb.Displays(idx)
                if dispish.HasValue then
                  let value = dispish.Value
                  Display.FromFB value
                else
                  "Could not parse empty DisplayFB"
                  |> Error.asParseError "IrisConfig.FromFB"
                  |> Either.fail
                #endif
              displays.[idx] <- display
              return (idx + 1, displays)
            })
          (Right(0, arr))
          arr

      let! (_,tasks) =
        let arr =
          fb.TasksLength
          |> Array.zeroCreate
        Array.fold
          (fun (m: Either<IrisError, int * Task array>) _ ->
            either {
              let! (idx, tasks) = m
              let! task =
                #if FABLE_COMPILER
                fb.Tasks(idx)
                |> Task.FromFB
                #else
                let taskish = fb.Tasks(idx)
                if taskish.HasValue then
                  let value = taskish.Value
                  Task.FromFB value
                else
                  "Could not parse empty TaskFB"
                  |> Error.asParseError "IrisConfig.FromFB"
                  |> Either.fail
                #endif
              tasks.[idx] <- task
              return (idx + 1, tasks)
            })
          (Right(0, arr))
          arr

      return
        { MachineId = machineId
          ActiveSite = site
          Version   = version
          Audio     = audio
          Vvvv      = vvvv
          Raft      = raft
          Timing    = timing
          Sites     = sites
          ViewPorts = viewports
          Displays  = displays
          Tasks     = tasks }
    }

// * ProjectYaml

#if !FABLE_COMPILER && !IRIS_NODES

[<RequireQualifiedAccess>]
module ProjectYaml =

  [<Literal>]
  let private template = """
Project:
  Version:

  Metadata:
    Id:
    CreatedOn:
    Copyright:
    Author:
    Name:
    LastSaved:

  VVVV:
    Executables:
      - Path:
        Version:
        Required: true
    Plugins:
      - Name:
        Path:

  Engine:
    LogLevel:
    DataDir:
    BindAddress:
    RequestTimeout:   -1
    ElectionTimeout:  -1
    MaxLogDepth:      -1
    MaxRetries:       -1
    PeriodicInterval: -1

  Timing:
    Framebase: 50
    Input:
    Servers:
      -
    UDPPort: 8090
    TCPPort: 8091

  Audio:
    SampleRate: 48000

  ViewPorts:
    - Id:
      Name:
      Position:
      Size:
      OutputPosition:
      OutputSize:
      Overlap:
      Description:

  Displays:
    - Id:
      Name:
      Size:
      Signals:
        - Size:
          Position:
      RegionMap:
        SrcViewportId:
        Regions:
          - Id:
            Name:
            SrcPosition:
            SrcSize:
            OutputPosition:
            OutputSize:
  Tasks:
    - Id:
      Description:
      DisplayId:
      AudioStream:
      Arguments:
        - Key:
          Value:

  ActiveSite:

  Sites:
    - Id:
      Name:
      Members:
        - Id:
          HostName:
          Ip:
          Port:    -1
          WsPort:  -1
          GitPort: -1
          ApiPort: -1
          State:
      Groups:
        - Name:
          Members:
            -
"""

  type Config = YamlConfig<"",false,template>

  type internal DisplayYaml   = Config.Project_Type.Displays_Item_Type
  type internal ViewPortYaml  = Config.Project_Type.ViewPorts_Item_Type
  type internal TaskYaml      = Config.Project_Type.Tasks_Item_Type
  type internal ArgumentYaml  = Config.Project_Type.Tasks_Item_Type.Arguments_Item_Type
  type internal ClusterYaml   = Config.Project_Type.Sites_Item_Type
  type internal MemberYaml    = Config.Project_Type.Sites_Item_Type.Members_Item_Type
  type internal GroupYaml     = Config.Project_Type.Sites_Item_Type.Groups_Item_Type
  type internal AudioYaml     = Config.Project_Type.Audio_Type
  type internal EngineYaml    = Config.Project_Type.Engine_Type
  type internal MetadatYaml   = Config.Project_Type.Metadata_Type
  type internal TimingYaml    = Config.Project_Type.Timing_Type
  type internal VvvvYaml      = Config.Project_Type.VVVV_Type
  type internal ExeYaml       = Config.Project_Type.VVVV_Type.Executables_Item_Type
  type internal PluginYaml    = Config.Project_Type.VVVV_Type.Plugins_Item_Type
  type internal SignalYaml    = Config.Project_Type.Displays_Item_Type.Signals_Item_Type
  type internal RegionMapYaml = Config.Project_Type.Displays_Item_Type.RegionMap_Type
  type internal RegionYaml    = Config.Project_Type.Displays_Item_Type.RegionMap_Type.Regions_Item_Type

  // ** parseTuple

  let internal parseTuple (input: string) : Either<IrisError,int * int> =
    input.Split [| '('; ','; ' '; ')' |]       // split the string according to the specified chars
    |> Array.filter (String.length >> ((<) 0)) // filter out elements that have zero length
    |> fun parsed ->
      try
        match parsed with
        | [| x; y |] -> Right (int x, int y)
        | _ ->
          sprintf "Cannot parse %A as (int * int) tuple" input
          |> Error.asParseError "Config.parseTuple"
          |> Either.fail
      with
        | exn ->
          sprintf "Cannot parse %A as (int * int) tuple: %s" input exn.Message
          |> Error.asParseError "Config.parseTuple"
          |> Either.fail

  // ** parseRect

  let internal parseRect (str : string) : Either<IrisError,Rect> =
    parseTuple str
    |> Either.map Rect

  // ** parseCoordinate

  let internal parseCoordinate (str : string) : Either<IrisError,Coordinate> =
    parseTuple str
    |> Either.map Coordinate

  // ** parseStringProp

  let internal parseStringProp (str : string) : string option =
    if str.Length > 0 then Some(str) else None

  // ** parseAudio

  /// ### Parse the Audio configuration section
  ///
  /// Parses the Audio configuration section of the passed-in configuration file.
  ///
  /// # Returns: AudioConfig
  let internal parseAudio (config: Config) : Either<IrisError, AudioConfig> =
    Either.tryWith (Error.asParseError "Config.parseAudio") <| fun _ ->
      { SampleRate = uint32 config.Project.Audio.SampleRate }

  // ** saveAudio

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let internal saveAudio (file: Config, config: IrisConfig) =
    file.Project.Audio.SampleRate <- int (config.Audio.SampleRate)
    (file, config)

  // ** parseExe

  let internal parseExe (exe: ExeYaml) : Either<IrisError, VvvvExe> =
    Right { Executable = exe.Path
            Version    = exe.Version
            Required   = exe.Required }

  // ** parseExes

  let internal parseExes exes : Either<IrisError, VvvvExe array> =
    either {
      let arr =
        exes
        |> Seq.length
        |> Array.zeroCreate

      let! (_,exes) =
        Seq.fold
          (fun (m: Either<IrisError,int * VvvvExe array>) exe -> either {
            let! (idx, exes) = m
            let! exe = parseExe exe
            exes.[idx] <- exe
            return (idx + 1, exes)
          })
          (Right(0, arr))
          exes

      return exes
    }

  // ** parsePlugin

  let internal parsePlugin (plugin: PluginYaml) : Either<IrisError, VvvvPlugin> =
    Right { Name = plugin.Name
            Path = plugin.Path }

  // ** parsePlugins

  let internal parsePlugins plugins : Either<IrisError, VvvvPlugin array> =
    either {
      let arr =
        plugins
        |> Seq.length
        |> Array.zeroCreate

      let! (_,plugins) =
        Seq.fold
          (fun (m: Either<IrisError,int * VvvvPlugin array>) plugin -> either {
            let! (idx, plugins) = m
            let! plugin = parsePlugin plugin
            plugins.[idx] <- plugin
            return (idx + 1, plugins)
          })
          (Right(0, arr))
          plugins

      return plugins
    }

  // ** parseVvvv

  /// ### Parses the VVVV configuration
  ///
  /// Constructs the VVVV configuration values from the handed config file value.
  ///
  /// # Returns: VvvvConfig
  let internal parseVvvv (config: Config) : Either<IrisError, VvvvConfig> =
    either {
      let vvvv = config.Project.VVVV
      let! exes = parseExes vvvv.Executables
      let! plugins = parsePlugins vvvv.Plugins
      return { Executables = exes
               Plugins     = plugins }
    }

  // ** saveVvvv

  /// ### Save the VVVV configuration
  ///
  /// Translate the values from Config into the passed in configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveVvvv (file: Config, config: IrisConfig) =
    file.Project.VVVV.Executables.Clear() //

    for exe in config.Vvvv.Executables do
      let entry = new ExeYaml()
      entry.Path <- exe.Executable;
      entry.Version <- exe.Version;
      entry.Required <- exe.Required
      file.Project.VVVV.Executables.Add(entry)

    file.Project.VVVV.Plugins.Clear()

    for plug in config.Vvvv.Plugins do
      let entry = new PluginYaml()
      entry.Name <- plug.Name
      entry.Path <- plug.Path
      file.Project.VVVV.Plugins.Add(entry)

    (file, config)

  // ** parseRaft

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// ## Parses Raft-related values in passed configuration
  ///
  /// Parses the passed-in configuration file contents and returns a `RaftConfig` value.
  ///
  /// Returns: RaftConfig
  let internal parseRaft (config: Config) : Either<IrisError, RaftConfig> =
    either {
      let engine = config.Project.Engine

      let! loglevel = Iris.Core.LogLevel.TryParse engine.LogLevel

      try
        return
          { RequestTimeout   = uint32 engine.RequestTimeout
            ElectionTimeout  = uint32 engine.ElectionTimeout
            MaxLogDepth      = uint32 engine.MaxLogDepth
            LogLevel         = loglevel
            DataDir          = engine.DataDir
            MaxRetries       = uint8 engine.MaxRetries
            PeriodicInterval = uint8 engine.PeriodicInterval }
      with
        | exn ->
          return!
            sprintf "Could not parse Engine config: %s" exn.Message
            |> Error.asParseError "Config.parseRaft"
            |> Either.fail
    }

  // ** saveRaft

  /// ### Save the passed RaftConfig to the configuration file
  ///
  /// Save Raft algorithm specific configuration options to the configuration file object.
  ///
  /// # Returns: ConfigFile
  let internal saveRaft (file: Config, config: IrisConfig) =
    file.Project.Engine.RequestTimeout   <- int config.Raft.RequestTimeout
    file.Project.Engine.ElectionTimeout  <- int config.Raft.ElectionTimeout
    file.Project.Engine.MaxLogDepth      <- int config.Raft.MaxLogDepth
    file.Project.Engine.LogLevel         <- string config.Raft.LogLevel
    file.Project.Engine.DataDir          <- config.Raft.DataDir
    file.Project.Engine.MaxRetries       <- int config.Raft.MaxRetries
    file.Project.Engine.PeriodicInterval <- int config.Raft.PeriodicInterval
    (file, config)

  // ** parseTiming

  //   _____ _           _
  //  |_   _(_)_ __ ___ (_)_ __   __ _
  //    | | | | '_ ` _ \| | '_ \ / _` |
  //    | | | | | | | | | | | | | (_| |
  //    |_| |_|_| |_| |_|_|_| |_|\__, |
  //                             |___/

  /// ### Parse the timing related configuration options
  ///
  /// Parse TimingConfig related values into a TimingConfig value and return it.
  ///
  /// # Returns: TimingConfig
  let internal parseTiming (config: Config) : Either<IrisError,TimingConfig> =
    either {
      let timing = config.Project.Timing
      let arr =
        timing.Servers
        |> Seq.length
        |> Array.zeroCreate

      let! (_,servers) =
        Seq.fold
          (fun (m: Either<IrisError, int * IpAddress array>) thing -> either {
            let! (idx, lst) = m
            let! server = IpAddress.TryParse thing
            lst.[idx] <- server
            return (idx + 1, lst)
          })
          (Right(0, arr))
          timing.Servers

      try
        return
          { Framebase = uint32 timing.Framebase
            Input     = timing.Input
            Servers   = servers
            UDPPort   = uint32 timing.UDPPort
            TCPPort   = uint32 timing.TCPPort }
      with
        | exn ->
          return!
            sprintf "Could not parse Timing config: %s" exn.Message
            |> Error.asParseError "Config.parseTiming"
            |> Either.fail
    }

  // ** saveTiming

  /// ### Transfer the TimingConfig options to the passed configuration file
  ///
  ///
  ///
  /// # Returns: ConfigFile
  let internal saveTiming (file: Config, config: IrisConfig) =
    file.Project.Timing.Framebase <- int (config.Timing.Framebase)
    file.Project.Timing.Input     <- config.Timing.Input

    file.Project.Timing.Servers.Clear()
    for srv in config.Timing.Servers do
      file.Project.Timing.Servers.Add(string srv)

    file.Project.Timing.TCPPort <- int (config.Timing.TCPPort)
    file.Project.Timing.UDPPort <- int (config.Timing.UDPPort)

    (file, config)

  // ** parseViewPort

  //  __     ___               ____            _
  //  \ \   / (_) _____      _|  _ \ ___  _ __| |_
  //   \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
  //    \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
  //     \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

  let internal parseViewPort (viewport: ViewPortYaml) =
    either {
      let! pos     = parseCoordinate viewport.Position
      let! size    = parseRect       viewport.Size
      let! outpos  = parseCoordinate viewport.OutputPosition
      let! outsize = parseRect       viewport.OutputSize
      let! overlap = parseRect       viewport.Overlap

      return { Id             = Id viewport.Id
               Name           = viewport.Name
               Position       = pos
               Size           = size
               OutputPosition = outpos
               OutputSize     = outsize
               Overlap        = overlap
               Description    = viewport.Description }
    }

  // ** parseViewPorts

  /// ### Parse all Viewport configs listed in a config file
  ///
  /// Parses the ViewPort config section and returns an array of `ViewPort` values.
  ///
  /// # Returns: ViewPort array
  let internal parseViewPorts (config: Config) : Either<IrisError,ViewPort array> =
    either {
      let arr =
        config.Project.ViewPorts
        |> Seq.length
        |> Array.zeroCreate

      let! (_,viewports) =
        Seq.fold
          (fun (m: Either<IrisError, int * ViewPort array>) vp -> either {
            let! (idx, viewports) = m
            let! viewport = parseViewPort vp
            viewports.[idx] <- viewport
            return (idx + 1, viewports)
          })
          (Right(0, arr))
          config.Project.ViewPorts

      return viewports
    }

  // ** saveViewPorts

  /// ### Transfers the passed array of ViewPort values
  ///
  /// Adds a config section for each ViewPort value in the passed in Config to the configuration
  /// file.
  ///
  /// # Returns: ConfigFile
  let internal saveViewPorts (file: Config, config: IrisConfig) =
    file.Project.ViewPorts.Clear()
    for vp in config.ViewPorts do
      let item = new ViewPortYaml()
      item.Id             <- string vp.Id
      item.Name           <- vp.Name
      item.Size           <- string vp.Size
      item.Position       <- string vp.Position
      item.Overlap        <- string vp.Overlap
      item.OutputPosition <- string vp.OutputPosition
      item.OutputSize     <- string vp.OutputSize
      item.Description    <- vp.Description
      file.Project.ViewPorts.Add(item)
    (file, config)

  // ** parseSignal

  //  ____  _                   _
  // / ___|(_) __ _ _ __   __ _| |___
  // \___ \| |/ _` | '_ \ / _` | / __|
  //  ___) | | (_| | | | | (_| | \__ \
  // |____/|_|\__, |_| |_|\__,_|_|___/
  //          |___/

  /// ## Parse a Signal definition
  ///
  /// Parse a signal definition. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - signal: SignalYaml
  ///
  /// Returns: Either<IrisError, Signal>
  let internal parseSignal (signal: SignalYaml) : Either<IrisError, Signal> =
    either {
      let! size = parseRect signal.Size
      let! pos = parseCoordinate signal.Position

      return { Size     = size
               Position = pos }
    }

  // ** parseSignals

  /// ## Parse an array of signals
  ///
  /// Parse an array of signals stored in the ConfigFile. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - signals: SignalYaml collection
  ///
  /// Returns: Either<IrisError, Signal array>
  let internal parseSignals signals =
    either {
      let arr =
        signals
        |> Seq.length
        |> Array.zeroCreate

      let! (_,parsed) =
        Seq.fold
          (fun (m: Either<IrisError,int * Signal array>) signal -> either {
            let! (idx, signals) = m
            let! signal = parseSignal signal
            signals.[idx] <- signal
            return (idx + 1, signals)
          })
          (Right(0, arr))
          signals

      return parsed
    }

  // ** parseRegion

  //  ____            _
  // |  _ \ ___  __ _(_) ___  _ __  ___
  // | |_) / _ \/ _` | |/ _ \| '_ \/ __|
  // |  _ <  __/ (_| | | (_) | | | \__ \
  // |_| \_\___|\__, |_|\___/|_| |_|___/
  //            |___/

  /// ## Parse a Region definition
  ///
  /// Parse a single Region definition. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - region: Region
  ///
  /// Returns: Either<IrisError,Region>
  let internal parseRegion (region: RegionYaml) : Either<IrisError, Region> =
    either {
      let! srcpos  = parseCoordinate region.SrcPosition
      let! srcsize = parseRect       region.SrcSize
      let! outpos  = parseCoordinate region.OutputPosition
      let! outsize = parseRect       region.OutputSize

      return
        { Id             = Id region.Id
          Name           = region.Name
          SrcPosition    = srcpos
          SrcSize        = srcsize
          OutputPosition = outpos
          OutputSize     = outsize }
    }

  // ** parseRegions

  /// ## Parse an array of Region definitions
  ///
  /// Parse an array of Region definitions. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - regions: RegionYaml collection
  ///
  /// Returns: Either<IrisError,Region array>
  let internal parseRegions regions : Either<IrisError, Region array> =
    either {
      let arr =
        regions
        |> Seq.length
        |> Array.zeroCreate

      let! (_,parsed) =
        Seq.fold
          (fun (m: Either<IrisError, int * Region array>) region -> either {
            let! (idx, regions) = m
            let! region = parseRegion region
            regions.[idx] <- region
            return (idx + 1, regions)
          })
          (Right(0, arr))
          regions

      return parsed
    }

  // ** parseDisplay

  //   ____  _           _
  //  |  _ \(_)___ _ __ | | __ _ _   _ ___
  //  | | | | / __| '_ \| |/ _` | | | / __|
  //  | |_| | \__ \ |_) | | (_| | |_| \__ \
  //  |____/|_|___/ .__/|_|\__,_|\__, |___/
  //              |_|            |___/

  /// ## Parse a Display definition
  ///
  /// Parse a Display definition. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - display: DisplayYaml
  ///
  /// Returns: Either<IrisError,Display>
  let internal parseDisplay (display: DisplayYaml) : Either<IrisError, Display> =
    either {
      let! size = parseRect display.Size
      let! signals = parseSignals display.Signals
      let! regions = parseRegions display.RegionMap.Regions

      let regionmap =
        { SrcViewportId = Id display.RegionMap.SrcViewportId
          Regions       = regions }

      return { Id        = Id display.Id
               Name      = display.Name
               Size      = size
               Signals   = signals
               RegionMap = regionmap }
    }

  // ** parseDisplays

  /// ## Parse an array of Display definitionsg
  ///
  /// Parses an array of Display definitions. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - displays: DisplayYaml collection
  ///
  /// Returns: Either<IrisError,Display array>
  let internal parseDisplays (config: Config) : Either<IrisError, Display array> =
    either {
      let arr =
        config.Project.Displays
        |> Seq.length
        |> Array.zeroCreate

      let! (_,displays) =
        Seq.fold
          (fun (m: Either<IrisError, int * Display array>) display -> either {
            let! (idx, displays) = m
            let! display = parseDisplay display
            displays.[idx] <- display
            return (idx + 1, displays)
          })
          (Right(0, arr))
          config.Project.Displays

      return displays
    }

  // ** saveDisplays

  /// ### Transfer the Display config to a configuration file
  ///
  /// Save all `Display` values in `Config` to the passed configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveDisplays (file: Config, config: IrisConfig) =
    file.Project.Displays.Clear()
    for dp in config.Displays do
      let item = new DisplayYaml()
      item.Id <- string dp.Id
      item.Name <- dp.Name
      item.Size <- dp.Size.ToString()

      item.RegionMap.SrcViewportId <- string dp.RegionMap.SrcViewportId
      item.RegionMap.Regions.Clear()

      for region in dp.RegionMap.Regions do
        let r = new RegionYaml()
        r.Id <- string region.Id
        r.Name <- region.Name
        r.OutputPosition <- region.OutputPosition.ToString()
        r.OutputSize <- region.OutputSize.ToString()
        r.SrcPosition <- region.SrcPosition.ToString()
        r.SrcSize <- region.SrcSize.ToString()
        item.RegionMap.Regions.Add(r)

      item.Signals.Clear()

      for signal in dp.Signals do
        let s = new SignalYaml()
        s.Position <- signal.Position.ToString()
        s.Size <- signal.Size.ToString()
        item.Signals.Add(s)

      file.Project.Displays.Add(item)
    (file, config)

  // ** parseArgument

  //     _                                         _
  //    / \   _ __ __ _ _   _ _ __ ___   ___ _ __ | |_
  //   / _ \ | '__/ _` | | | | '_ ` _ \ / _ \ '_ \| __|
  //  / ___ \| | | (_| | |_| | | | | | |  __/ | | | |_
  // /_/   \_\_|  \__, |\__,_|_| |_| |_|\___|_| |_|\__|
  //              |___/

  /// ## Parse a single Argument key/value pair
  ///
  /// Parse a single Argument key/value pair
  ///
  /// ### Signature:
  /// - argument: ArgumentYaml
  ///
  /// Returns: Either<IrisError, string * string>
  let internal parseArgument (argument: ArgumentYaml) =
    either {
      if (argument.Key.Length > 0) && (argument.Value.Length > 0) then
        return (argument.Key, argument.Value)
      else
        return!
          sprintf "Could not parse Argument: %A" argument
          |> Error.asParseError "Config.parseArgument"
          |> Either.fail
    }

  // ** parseArguments

  /// ## Parse an array of ArgumentYamls
  ///
  /// Parse an array of ArgumentYamls
  ///
  /// ### Signature:
  /// - arguments: ArgumentYaml collection
  ///
  /// Returns: Either<IrisError, (string * string) array>
  let internal parseArguments arguments =
    either {
      let arr =
        arguments
        |> Seq.length
        |> Array.zeroCreate

      let! (_,arguments) =
        Seq.fold
          (fun (m: Either<IrisError, int * Argument array>) thing -> either {
            let! (idx, arguments) = m
            let! argument = parseArgument thing
            arguments.[idx] <- argument
            return (idx + 1, arguments)
          })
          (Right(0, arr))
          arguments

      return arguments
    }

  // ** parseTask

  //   _____         _
  //  |_   _|_ _ ___| | _____
  //    | |/ _` / __| |/ / __|
  //    | | (_| \__ \   <\__ \
  //    |_|\__,_|___/_|\_\___/
  //

  /// ## Parse a Task definition
  ///
  /// Parse a single Task definition. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - task: TaskYaml
  ///
  /// Returns: Either<IrisError, Task>
  let internal parseTask (task: TaskYaml) : Either<IrisError, Task> =
    either {
      let! arguments = parseArguments task.Arguments
      return { Id          = Id task.Id
               Description = task.Description
               DisplayId   = Id task.DisplayId
               AudioStream = task.AudioStream
               Arguments   = arguments }
    }

  // ** parseTasks

  /// ### Parse Task configuration section
  ///
  /// Create `Task` values for each entry in the Task config section.
  ///
  /// # Returns: Task array
  let internal parseTasks (config: Config) : Either<IrisError,Task array> =
    either {
      let arr =
        config.Project.Tasks
        |> Seq.length
        |> Array.zeroCreate

      let! (_,tasks) =
        Seq.fold
          (fun (m: Either<IrisError, int * Task array>) task -> either {
            let! (idx, tasks) = m
            let! task = parseTask task
            tasks.[idx] <- task
            return (idx + 1, tasks)
          })
          (Right(0, arr))
          config.Project.Tasks

      return tasks
    }

  // ** saveTasks

  /// ### Save the Tasks to a config file
  ///
  /// Transfers all `Task` values into the configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveTasks (file: Config, config: IrisConfig) =
    file.Project.Tasks.Clear()
    for task in config.Tasks do
      let t = new TaskYaml()
      t.Id <- string task.Id
      t.AudioStream <- task.AudioStream
      t.Description <- task.Description
      t.DisplayId   <- string task.DisplayId

      t.Arguments.Clear()

      for arg in task.Arguments do
        let a = new ArgumentYaml()
        a.Key <- fst arg
        a.Value <- snd arg
        t.Arguments.Add(a)

      file.Project.Tasks.Add(t)
    (file, config)

  // ** parseMember

  //    ____ _           _
  //   / ___| |_   _ ___| |_ ___ _ __
  //  | |   | | | | / __| __/ _ \ '__|
  //  | |___| | |_| \__ \ ||  __/ |
  //   \____|_|\__,_|___/\__\___|_|
  //

  /// ## Parse a single Member definition
  ///
  /// Parse a single Member definition. Returns a ParseError on failiure.
  ///
  /// ### Signature:
  /// - mem: MemberYaml
  ///
  /// Returns: Either<IrisError, RaftMember>
  let internal parseMember (mem: MemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! ip = IpAddress.TryParse mem.Ip
      let! state = RaftMemberState.TryParse mem.State

      try
        return { Id         = Id mem.Id
                 HostName   = mem.HostName
                 IpAddr     = ip
                 Port       = uint16 mem.Port
                 WsPort     = uint16 mem.WsPort
                 GitPort    = uint16 mem.GitPort
                 ApiPort    = uint16 mem.ApiPort
                 State      = state
                 Voting     = true
                 VotedForMe = false
                 NextIndex  = 1u
                 MatchIndex = 0u }
      with
        | exn ->
          return!
            sprintf "Could not parse Member definition: %s" exn.Message
            |> Error.asParseError "Config.parseMember"
            |> Either.fail
    }

  // ** parseMember

  /// ## Parse a collectio of Member definitions
  ///
  /// Parse an array of Member definitions. Returns a ParseError on failure.
  ///
  /// ### Signature:
  /// - mems: MemberYaml collection
  ///
  /// Returns: Either<IrisError, RaftMember array>
  let internal parseMembers mems : Either<IrisError, Map<MemberId,RaftMember>> =
    either {
      let! (_,mems) =
        Seq.fold
          (fun (m: Either<IrisError, int * Map<MemberId,RaftMember>>) mem -> either {
            let! (idx, mems) = m
            let! mem = parseMember mem
            return (idx + 1, Map.add mem.Id mem mems)
          })
          (Right(0, Map.empty))
          mems

      return mems
    }

  // ** parseGroup

  let internal parseGroup (group: GroupYaml) : Either<IrisError, HostGroup> =
    either {
      if group.Name.Length > 0 then
        let ids = Seq.map Id group.Members |> Seq.toArray

        return { Name    = group.Name
                 Members = ids }
      else
        return!
          "Invalid HostGroup setting (Name must be given)"
          |> Error.asParseError "Config.parseGroup"
          |> Either.fail
    }

  // ** parseGroups

  let internal parseGroups groups : Either<IrisError, HostGroup array> =
    either {
      let arr =
        groups
        |> Seq.length
        |> Array.zeroCreate

      let! (_, groups) =
        Seq.fold
          (fun (m: Either<IrisError, int * HostGroup array>) group -> either {
            let! (idx, groups) = m
            let! group = parseGroup group
            groups.[idx] <- group
            return (idx + 1, groups)
          })
          (Right(0,arr))
          groups

      return groups
    }

  // ** parseCluster

  /// ### Parse the Cluster configuration section
  ///
  /// Parse the cluster configuration section of a given configuration file into a `Cluster` value.
  ///
  /// # Returns: Cluster

  let internal parseCluster (cluster: ClusterYaml) : Either<IrisError, ClusterConfig> =
    either {
      let! groups = parseGroups cluster.Groups
      let! mems = parseMembers cluster.Members

      return { Id = Id cluster.Id
               Name = cluster.Name
               Members = mems
               Groups = groups }
    }

  // ** parseSites

  let internal parseSites (config: Config) : Either<IrisError, ClusterConfig array> =
    either {
      let arr =
        config.Project.Sites
        |> Seq.length
        |> Array.zeroCreate

      let! (_, sites) =
        Seq.fold
          (fun (m: Either<IrisError, int * ClusterConfig array>) cfg ->
            either {
              let! (idx, sites) = m
              let! site = parseCluster cfg
              sites.[idx] <- site
              return (idx + 1, sites)
            })
          (Right(0, arr))
          config.Project.Sites

      return sites
    }

  // ** saveSites

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let internal saveSites (file: Config, config: IrisConfig) =
    file.Project.Sites.Clear()

    match config.ActiveSite with
    | Some id -> file.Project.ActiveSite <- string id
    | None -> file.Project.ActiveSite <- null

    for cluster in config.Sites do
      let cfg = new ClusterYaml()
      cfg.Members.Clear()
      cfg.Groups.Clear()

      cfg.Id <- string cluster.Id
      cfg.Name <- cluster.Name

      for KeyValue(memId,mem) in cluster.Members do
        let n = new MemberYaml()
        n.Id       <- string memId
        n.Ip       <- string mem.IpAddr
        n.HostName <- mem.HostName
        n.Port     <- int mem.Port
        n.WsPort   <- int mem.WsPort
        n.GitPort  <- int mem.GitPort
        n.ApiPort  <- int mem.ApiPort
        n.State    <- string mem.State
        cfg.Members.Add(n)

      for group in cluster.Groups do
        let g = new GroupYaml()
        g.Name <- group.Name
        g.Members.Clear()

        for mem in group.Members do
          g.Members.Add(string mem)

        cfg.Groups.Add(g)
      file.Project.Sites.Add(cfg)
    (file, config)

  // ** parseLastSaved (private)

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let internal parseLastSaved (config: Config) =
    let meta = config.Project.Metadata
    if meta.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(meta.LastSaved))
      with
        | _ -> None
    else None

  // ** parseCreatedOn (private)

  /// ### Parse the CreatedOn property
  ///
  /// Parse the CreatedOn property in a given ConfigFile. If the field is empty or DateTime.Parse
  /// fails to read it, the date returned will be the begin of the epoch.
  ///
  /// # Returns: DateTime
  let internal parseCreatedOn (config: Config) =
    let meta = config.Project.Metadata
    if meta.CreatedOn.Length > 0
    then
      try
        DateTime.Parse(meta.CreatedOn)
      with
        | _ -> DateTime.FromFileTimeUtc(int64 0)
    else DateTime.FromFileTimeUtc(int64 0)

  let internal parse (str: string) =
    try
      let config = new Config()
      config.LoadText str
      Either.succeed config
    with
      | exn ->
        exn.Message
        |> Error.asParseError "ProjectYaml.parse"
        |> Either.fail

#endif

// * Config Module

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<RequireQualifiedAccess>]
module Config =

  // ** fromFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let fromFile (file: ProjectYaml.Config) (machine: IrisMachine) : Either<IrisError, IrisConfig> =
    either {
      let  version   = file.Project.Version
      let! raftcfg   = ProjectYaml.parseRaft      file
      let! timing    = ProjectYaml.parseTiming    file
      let! vvvv      = ProjectYaml.parseVvvv      file
      let! audio     = ProjectYaml.parseAudio     file
      let! viewports = ProjectYaml.parseViewPorts file
      let! displays  = ProjectYaml.parseDisplays  file
      let! tasks     = ProjectYaml.parseTasks     file
      let! sites     = ProjectYaml.parseSites     file

      let site =
        if isNull file.Project.ActiveSite || file.Project.ActiveSite = "" then
          None
        else
          Some (Id file.Project.ActiveSite)

      return { MachineId = machine.MachineId
               ActiveSite = site
               Version   = version
               Vvvv      = vvvv
               Audio     = audio
               Raft      = raftcfg
               Timing    = timing
               ViewPorts = viewports
               Displays  = displays
               Tasks     = tasks
               Sites     = sites }
    }

  #endif

  // ** toFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let toFile (config: IrisConfig) (file: ProjectYaml.Config) =
    file.Project.Version <- string config.Version
    (file, config)
    |> ProjectYaml.saveVvvv
    |> ProjectYaml.saveAudio
    |> ProjectYaml.saveRaft
    |> ProjectYaml.saveTiming
    |> ProjectYaml.saveViewPorts
    |> ProjectYaml.saveDisplays
    |> ProjectYaml.saveTasks
    |> ProjectYaml.saveSites
    |> ignore

  #endif

  // ** create

  let create (name: string) (machine: IrisMachine) =
    { MachineId = machine.MachineId
      ActiveSite = None
      #if FABLE_COMPILER
      Version   = "0.0.0"
      #else
      Version   = Assembly.GetExecutingAssembly().GetName().Version.ToString()
      #endif
      Vvvv      = VvvvConfig.Default
      Audio     = AudioConfig.Default
      Raft      = RaftConfig.Default
      Timing    = TimingConfig.Default
      ViewPorts = [| |]
      Displays  = [| |]
      Tasks     = [| |]
      Sites     = [| |] }

  // ** updateMachine

  let updateMachine (machine: IrisMachine) (config: IrisConfig) =
    { config with MachineId = machine.MachineId }

  // ** updateVvvv

  let updateVvvv (vvvv: VvvvConfig) (config: IrisConfig) =
    { config with Vvvv = vvvv }

  // ** updateAudio

  let updateAudio (audio: AudioConfig) (config: IrisConfig) =
    { config with Audio = audio }

  // ** updateEngine

  let updateEngine (engine: RaftConfig) (config: IrisConfig) =
    { config with Raft = engine }

  // ** updateTiming

  let updateTiming (timing: TimingConfig) (config: IrisConfig) =
    { config with Timing = timing }

  // ** updateViewPorts

  let updateViewPorts (viewports: ViewPort array) (config: IrisConfig) =
    { config with ViewPorts = viewports }

  // ** updateDisplays

  let updateDisplays (displays: Display array) (config: IrisConfig) =
    { config with Displays = displays }

  // ** updateTasks

  let updateTasks (tasks: Task array) (config: IrisConfig) =
    { config with Tasks = tasks }

  // ** updateCluster

  let updateCluster (cluster: ClusterConfig) (config: IrisConfig) =
    let sites = Array.map (fun site -> if cluster.Id = site.Id then cluster else site) config.Sites
    { config with Sites = sites }

  // ** updateSites

  let updateSites (sites: ClusterConfig array) (config: IrisConfig) =
    { config with Sites = sites }

  // ** findMember

  let findMember (config: IrisConfig) (id: Id) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun clst -> clst.Id = active) config.Sites with
      | Some cluster ->
        match Map.tryFind id cluster.Members with
        | Some mem -> Either.succeed mem
        | _ ->
          ErrorMessages.PROJECT_MISSING_MEMBER + ": " + (string id)
          |> Error.asProjectError "Config.findMember"
          |> Either.fail
      | _ ->
        ErrorMessages.PROJECT_MISSING_CLUSTER + ": " + (string active)
        |> Error.asProjectError "Config.findMember"
        |> Either.fail
    | None ->
      ErrorMessages.PROJECT_NO_ACTIVE_CONFIG
      |> Error.asProjectError "Config.findMember"
      |> Either.fail

  // ** getMembers

  let getMembers (config: IrisConfig) : Either<IrisError,Map<MemberId,RaftMember>> =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun clst -> clst.Id = active) config.Sites with
      | Some site -> site.Members |> Either.succeed
      | None ->
        ErrorMessages.PROJECT_MISSING_CLUSTER + ": " + (string active)
        |> Error.asProjectError "Config.getMembers"
        |> Either.fail
    | None ->
      ErrorMessages.PROJECT_NO_ACTIVE_CONFIG
      |> Error.asProjectError "Config.getMembers"
      |> Either.fail

  // ** setActiveSite

  let setActiveSite (id: Id) (config: IrisConfig) =
    if config.Sites |> Array.exists (fun x -> x.Id = id)
    then Right { config with ActiveSite = Some id }
    else
        ErrorMessages.PROJECT_MISSING_MEMBER + ": " + (string id)
        |> Error.asProjectError "Config.setActiveSite"
        |> Either.fail

  // ** getActiveSite

  let getActiveSite (config: IrisConfig) =
    config.ActiveSite

  // ** setMembers

  let setMembers (mems: Map<MemberId,RaftMember>) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun clst -> clst.Id = active) config.Sites with
      | Some site ->
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** selfMember

  let selfMember (options: IrisConfig) =
    findMember options options.MachineId

  // ** addSite

  let private addSitePrivate (site: ClusterConfig) setActive (config: IrisConfig) =
    let i = config.Sites |> Array.tryFindIndex (fun s -> s.Id = site.Id)
    let copy = Array.zeroCreate (config.Sites.Length + (if Option.isSome i then 0 else 1))
    Array.iteri (fun i site -> copy.[i] <- site) config.Sites
    copy.[match i with Some i -> i | None -> config.Sites.Length] <- site
    if setActive
    then { config with ActiveSite = Some site.Id; Sites = copy }
    else { config with Sites = copy }

  /// Adds or replaces a site with same Id
  let addSite (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site false config

  /// Adds or replaces a site with same Id and sets it as the active site
  let addSiteAndSetActive (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site false config

  // ** removeSite

  let removeSite (id: Id) (config: IrisConfig) =
    let sites = Array.filter (fun site -> site.Id <> id) config.Sites
    { config with Sites = sites }

  // ** siteByMember

  let siteByMember (memid: Id) (config: IrisConfig) =
    Array.fold
      (fun (m: ClusterConfig option) site ->
        match m with
        | Some _ -> m
        | None ->
          if Map.containsKey memid site.Members then
            Some site
          else None)
      None
      config.Sites

  // ** findSite

  let findSite (id: Id) (config: IrisConfig) =
    Array.tryFind (fun site -> site.Id = id) config.Sites

  // ** addMember

  let addMember (mem: RaftMember) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun clst -> clst.Id = active) config.Sites with
      | Some site ->
        let mems = Map.add mem.Id mem site.Members
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** removeMember

  let removeMember (id: Id) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun clst -> clst.Id = active) config.Sites with
      | Some site ->
        let mems = Map.remove id site.Members
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** logLevel

  let logLevel (config: IrisConfig) =
    config.Raft.LogLevel

  // ** setLogLevel

  let setLogLevel (level: Iris.Core.LogLevel) (config: IrisConfig) =
    { config with Raft = { config.Raft with LogLevel = level } }

  // ** metadataPath

  let metadataPath (config: IrisConfig) =
    config.Raft.DataDir </> RAFT_METADATA_FILENAME + ASSET_EXTENSION

  // ** logDataPath

  let logDataPath (config: IrisConfig) =
    config.Raft.DataDir </> RAFT_LOGDATA_PATH

// * IrisProject

//  ___      _     ____            _           _
// |_ _|_ __(_)___|  _ \ _ __ ___ (_) ___  ___| |_
//  | || '__| / __| |_) | '__/ _ \| |/ _ \/ __| __|
//  | || |  | \__ \  __/| | | (_) | |  __/ (__| |_
// |___|_|  |_|___/_|   |_|  \___// |\___|\___|\__|
//                              |__/

type IrisProject =
  { Id        : Id
  ; Name      : Name
  ; Path      : FilePath                // project path should always be the path containing '.git'
  ; CreatedOn : TimeStamp
  ; LastSaved : TimeStamp option
  ; Copyright : string    option
  ; Author    : string    option
  ; Config    : IrisConfig }

  override project.ToString() =
    sprintf @"
Id:        %s
Name:      %s
Path:      %s
Created:   %s
LastSaved: %A
Copyright: %A
Author:    %A
Config: %A
"
      (string project.Id)
      project.Name
      project.Path
      project.CreatedOn
      project.LastSaved
      project.Copyright
      project.Author
      project.Config

  // ** Empty

  static member Empty
    with get () =
      { Id        = Id Constants.EMPTY
        Name      = Constants.EMPTY
        Path      = ""
        CreatedOn = ""
        LastSaved = None
        Copyright = None
        Author    = None
        Config    = IrisConfig.Default }

  // ** AssetPath

  //     _                 _   ____       _   _
  //    / \   ___ ___  ___| |_|  _ \ __ _| |_| |__
  //   / _ \ / __/ __|/ _ \ __| |_) / _` | __| '_ \
  //  / ___ \\__ \__ \  __/ |_|  __/ (_| | |_| | | |
  // /_/   \_\___/___/\___|\__|_|   \__,_|\__|_| |_|

  member project.AssetPath
    with get () = PROJECT_FILENAME + ASSET_EXTENSION

  // ** Save

  #if !FABLE_COMPILER && !IRIS_NODES

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  static member Load (basepath: FilePath, machine: IrisMachine) =
    either {
      let filename = PROJECT_FILENAME + ASSET_EXTENSION

      let normalizedPath =
        let withRoot =
          if Path.IsPathRooted basepath then
            basepath
          else
            Path.GetFullPath basepath
        if withRoot.EndsWith filename then
          withRoot
        else
          withRoot </> filename

      if not (File.Exists normalizedPath) then
        return!
          sprintf "Project Not Found: %s" normalizedPath
          |> Error.asProjectError "Project.load"
          |> Either.fail
      else
        let! str = Asset.read normalizedPath
        let! project = Yaml.decode str

        return
          { project with
              Path   = Path.GetDirectoryName normalizedPath
              Config = Config.updateMachine machine project.Config }
    }

  //  ____
  // / ___|  __ ___   _____
  // \___ \ / _` \ \ / / _ \
  //  ___) | (_| |\ V /  __/
  // |____/ \__,_| \_/ \___|

  member project.Save (basepath: FilePath) =
    either {
      let path = basepath </> Asset.path project
      let data = Yaml.encode project
      let! _ = Asset.write path (Payload data)
      return ()
    }

  #endif

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name
    let path = builder.CreateString self.Path
    let created = builder.CreateString (string self.CreatedOn)
    let lastsaved = Option.map builder.CreateString self.LastSaved
    let copyright = Option.map builder.CreateString self.Copyright
    let author = Option.map builder.CreateString self.Author
    let config = Binary.toOffset builder self.Config

    ProjectFB.StartProjectFB(builder)
    ProjectFB.AddId(builder, id)
    ProjectFB.AddName(builder, name)
    ProjectFB.AddPath(builder, path)
    ProjectFB.AddCreatedOn(builder, created)

    match lastsaved with
    | Some offset -> ProjectFB.AddLastSaved(builder,offset)
    | _ -> ()

    match copyright with
    | Some offset -> ProjectFB.AddCopyright(builder,offset)
    | _ -> ()

    match author with
    | Some offset -> ProjectFB.AddAuthor(builder,offset)
    | _ -> ()

    ProjectFB.AddConfig(builder, config)
    ProjectFB.EndProjectFB(builder)

  member self.ToBytes () =
    Binary.buildBuffer self

  static member FromBytes(bytes: Binary.Buffer) =
    Binary.createBuffer bytes
    |> ProjectFB.GetRootAsProjectFB
    |> IrisProject.FromFB

  static member FromFB(fb: ProjectFB) =
    either {
      let! lastsaved =
        match fb.LastSaved with
        | null    -> Right None
        | date -> Right (Some date)

      let! copyright =
        match fb.Copyright with
        | null   -> Right None
        | str -> Right (Some str)

      let! author =
        match fb.Author with
        | null   -> Right None
        | str -> Right (Some str)

      let! config =
        #if FABLE_COMPILER
        IrisConfig.FromFB fb.Config
        #else
        let configish = fb.Config
        if configish.HasValue then
          let value = configish.Value
          IrisConfig.FromFB value
        else
          "Could not parse empty ConfigFB"
          |> Error.asParseError "IrisProject.FromFB"
          |> Either.fail
        #endif

      return
        { Id        = Id fb.Id
          Name      = fb.Name
          Path      = fb.Path
          CreatedOn = fb.CreatedOn
          LastSaved = lastsaved
          Copyright = copyright
          Author    = author
          Config    = config }
    }

  #if !FABLE_COMPILER && !IRIS_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml(_: Serializer) =
    let config = new ProjectYaml.Config()

    Config.toFile self.Config config

    // Project metadata
    config.Project.Metadata.Id        <- string self.Id
    config.Project.Metadata.Name      <- self.Name
    config.Project.Metadata.CreatedOn <- self.CreatedOn

    Option.map
      (fun author -> config.Project.Metadata.Author <- author)
      self.Author
    |> ignore

    Option.map
      (fun copyright -> config.Project.Metadata.Copyright <- copyright)
      self.Copyright
    |> ignore

    Option.map
      (fun saved -> config.Project.Metadata.LastSaved <- saved)
      self.LastSaved
    |> ignore

    config.ToString()

  static member FromYaml(str: string) =
    either {
      let! config = ProjectYaml.parse str

      let meta = config.Project.Metadata
      let lastSaved =
        match meta.LastSaved with
          | null | "" -> None
          | str ->
            try
              DateTime.Parse str |> ignore
              Some str
            with
              | _ -> None

      let dummy = MachineConfig.create ()

      let! config = Config.fromFile config dummy

      return { Id        = Id meta.Id
               Name      = meta.Name
               Path      = Path.GetFullPath(".")
               CreatedOn = meta.CreatedOn
               LastSaved = lastSaved
               Copyright = ProjectYaml.parseStringProp meta.Copyright
               Author    = ProjectYaml.parseStringProp meta.Author
               Config    = config }
    }

  #endif

// * Project module

[<RequireQualifiedAccess>]
module Project =

  // ** repository

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.
  ///
  /// # Returns: Repository option
  let repository (project: IrisProject) =
    Git.Repo.repository project.Path

  #endif

  // ** currentBranch

  #if !FABLE_COMPILER && !IRIS_NODES

  let currentBranch (project: IrisProject) =
    either {
      let! repo = repository project
      return Git.Branch.current repo
    }

  #endif

  // ** checkoutBranch

  #if !FABLE_COMPILER && !IRIS_NODES

  let checkoutBranch (name: string) (project: IrisProject) =
    either {
      let! repo = repository project
      return! Git.Repo.checkout name repo
    }

  #endif

  //  ____       _   _
  // |  _ \ __ _| |_| |__  ___
  // | |_) / _` | __| '_ \/ __|
  // |  __/ (_| | |_| | | \__ \
  // |_|   \__,_|\__|_| |_|___/

  let checkPath (machine: IrisMachine) projectName =
    let path = machine.WorkSpace </> projectName </> PROJECT_FILENAME + ASSET_EXTENSION
    if File.Exists path |> not then
      sprintf "Project Not Found: %s" projectName
      |> Error.asProjectError "Project.checkPath"
      |> Either.fail
    else
      Either.succeed path

  // ** filePath

  let filePath (project: IrisProject) : FilePath =
    project.Path </> PROJECT_FILENAME + ASSET_EXTENSION

  // ** userDir

  let userDir (project: IrisProject) : FilePath =
    project.Path </> USER_DIR

  // ** cueDir

  let cueDir (project: IrisProject) : FilePath =
    project.Path </> CUE_DIR

  // ** cuelistDir

  let cuelistDir (project: IrisProject) : FilePath =
    project.Path </> CUELIST_DIR

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // ** writeDaemonExportFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeDaemonExportFile (repo: Repository) =
    either {
      let path = repo.Info.Path </> "git-daemon-export-ok"
      let! _ = Asset.write path (Payload "")
      return ()
    }

  #endif

  // ** writeGitIgnoreFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeGitIgnoreFile (repo: Repository) =
    either {
      let parent = Git.Repo.parentPath repo
      let path = parent </> ".gitignore"
      let! _ = Asset.write path (Payload GITIGNORE)
      do! Git.Repo.stage repo path
    }

  #endif

  // ** createAssetDir (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private createAssetDir (repo: Repository) (dir: FilePath) =
    either {
      let parent = Git.Repo.parentPath repo
      let target = parent </> dir
      do! FileSystem.mkDir target
      let gitkeep = target </> ".gitkeep"
      let! _ = Asset.write gitkeep (Payload "")
      do! Git.Repo.stage repo gitkeep
    }

  #endif

  // ** commitPath (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## commitPath
  ///
  /// commit a file at given path to git
  ///
  /// ### Signature:
  /// - committer : Signature of committer
  /// - msg       : commit msg
  /// - filepath  : path to file being committed
  /// - project   : IrisProject
  ///
  /// Returns: (Commit * IrisProject) option
  let private commitPath (filepath: FilePath)
                         (committer: Signature)
                         (msg : string)
                         (project: IrisProject) :
                         Either<IrisError,(Commit * IrisProject)> =
    either {
      let! repo = repository project
      let abspath =
        if Path.IsPathRooted filepath then
          filepath
        else
          project.Path </> filepath
      do! Git.Repo.stage repo abspath
      let! commit = Git.Repo.commit repo msg committer
      return commit, project
    }

  #endif

  // ** saveFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let saveFile (path: FilePath)
               (contents: string)
               (committer: Signature)
               (msg : string)
               (project: IrisProject) :
               Either<IrisError,(Commit * IrisProject)> =

    either {
      let info = FileInfo path
      do! FileSystem.mkDir info.Directory.FullName
      let! _ = Asset.write path (Payload contents)
      return! commitPath path committer msg project
    }

  #endif

  // ** deleteFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let deleteFile (path: FilePath)
                 (committer: Signature)
                 (msg : string)
                 (project: IrisProject) :
                 Either<IrisError,(Commit * IrisProject)> =
    either {
      let! _ = Asset.delete path
      return! commitPath path committer msg project
    }

  #endif

  // ** saveAsset

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## saveAsset
  ///
  /// Attempt to save the passed thing, and, if succesful, return its
  /// FileInfo object.
  ///
  /// ### Signature:
  /// - thing: ^t the thing to save. Must implement certain methods/getters
  /// - committer: User the thing to save. Must implement certain methods/getters
  /// - project: Project to save file into
  ///
  /// Returns: Either<IrisError,Commit * Project>
  let inline saveAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let payload = thing |> Yaml.encode
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = sprintf "%s save %A" committer.UserName (Path.GetFileName filepath)
    saveFile filepath payload signature msg project

  #endif

  // ** deleteAsset

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ## deleteAsset
  ///
  /// Delete a file path from disk and commit the change to git.
  ///
  /// ### Signature:
  /// - thing: ^t thing to delete
  /// - committer: User committing the change
  /// - msg: User committing the change
  /// - project: IrisProject to work on
  ///
  /// Returns: Either<IrisError, FileInfo * Commit * Project>
  let inline deleteAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = sprintf "%s deleted %A" committer.UserName filepath
    deleteFile filepath signature msg project

  let private needsInit (project: IrisProject) =
    let projdir = Directory.Exists project.Path
    let git = Directory.Exists (project.Path </> ".git")
    let cues = Directory.Exists (project.Path </> CUE_DIR)
    let cuelists = Directory.Exists (project.Path </> CUELIST_DIR)
    let users = Directory.Exists (project.Path </> USER_DIR)

    (not git)      ||
    (not cues)     ||
    (not cuelists) ||
    (not users)    ||
    (not projdir)

  #endif

  // ** initRepo (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let private initRepo (project: IrisProject) : Either<IrisError,unit> =
    either {
      let! repo = Git.Repo.init project.Path
      do! writeDaemonExportFile repo
      do! writeGitIgnoreFile repo
      do! createAssetDir repo CUE_DIR
      do! createAssetDir repo USER_DIR
      do! createAssetDir repo CUELIST_DIR
      do! createAssetDir repo PINGROUP_DIR
      let relPath = Asset.path User.Admin
      let absPath = project.Path </> relPath
      let! _ =
        User.Admin
        |> Yaml.encode
        |> Payload
        |> Asset.write absPath
      do! Git.Repo.add repo relPath
      do! Git.Repo.stage repo absPath
    }

  #endif

  // ** create

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: IrisProject
  let create (path: FilePath) (name : string) (machine: IrisMachine) : Either<IrisError,IrisProject> =
    either {
      let project =
        { Id        = Id.Create()
        ; Name      = name
        ; Path      = path
        ; CreatedOn = Time.createTimestamp()
        ; LastSaved = Some (Time.createTimestamp ())
        ; Copyright = None
        ; Author    = None
        ; Config    = Config.create name machine  }

      do! initRepo project
      let! _ = Asset.saveWithCommit path User.Admin.Signature project
      return project
    }

  #endif

  // ** clone

  #if !FABLE_COMPILER && !IRIS_NODES

  let clone (host : string) (name : string) (destination: FilePath) : FilePath option =
    let url = sprintf "git://%s/%s/.git" host name
    try
      Repository.Clone(url, Path.Combine(destination, name))
      |> ignore
      Some(destination </> name)
    with
      | _ -> None

  #endif

  // ** config

  let config (project: IrisProject) : IrisConfig = project.Config

  // ** updatePath

  let updatePath (path: FilePath) (project: IrisProject) : IrisProject =
    { project with Path = path }

  // ** updateConfig

  let updateConfig (config: IrisConfig) (project: IrisProject) : IrisProject =
    { project with Config = config }

  // ** updateDataDir

  let updateDataDir (raftDir: FilePath) (project: IrisProject) : IrisProject =
    { project.Config.Raft with DataDir = raftDir }
    |> flip Config.updateEngine project.Config
    |> flip updateConfig project

  // ** addMember

  let addMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.addMember mem
    |> flip updateConfig project

  // ** updateMember

  let updateMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    addMember mem project

  // ** removeMember

  let removeMember (mem: MemberId) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.removeMember mem
    |> flip updateConfig project

  // ** addMembers

  let addMembers (mems: RaftMember list) (project: IrisProject) : IrisProject =
    List.fold
      (fun config (mem: RaftMember) ->
        Config.addMember mem config)
      project.Config
      mems
    |> flip updateConfig project

  // ** updateMachine

  let updateMachine (machine: IrisMachine) (project: IrisProject) : IrisProject =
    { project with Config = Config.updateMachine machine project.Config }
