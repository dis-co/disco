namespace rec Iris.Core

// * Imports

open System
open System.IO
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

open System.Linq
open System.Net
open FlatBuffers
open Iris.Serialization

#endif

#if !FABLE_COMPILER && !IRIS_NODES

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
  { RequestTimeout:   Timeout
    ElectionTimeout:  Timeout
    MaxLogDepth:      int
    LogLevel:         Iris.Core.LogLevel
    DataDir:          FilePath
    MaxRetries:       int
    PeriodicInterval: Timeout }

  // ** Default

  static member Default =
    { RequestTimeout   = Constants.RAFT_REQUEST_TIMEOUT * 1<ms>
      ElectionTimeout  = Constants.RAFT_ELECTION_TIMEOUT * 1<ms>
      PeriodicInterval = Constants.RAFT_PERIODIC_INTERVAL * 1<ms>
      MaxLogDepth      = Constants.RAFT_MAX_LOGDEPTH
      MaxRetries       = 10
      LogLevel         = LogLevel.Err
      DataDir          = filepath "" }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let lvl = self.LogLevel |> string |> builder.CreateString
    let dir = self.DataDir |> unwrap |> Option.mapNull builder.CreateString

    RaftConfigFB.StartRaftConfigFB(builder)
    RaftConfigFB.AddRequestTimeout(builder, int self.RequestTimeout)
    RaftConfigFB.AddElectionTimeout(builder, int self.ElectionTimeout)
    RaftConfigFB.AddMaxLogDepth(builder, self.MaxLogDepth)
    RaftConfigFB.AddLogLevel(builder, lvl)
    Option.iter (fun value -> RaftConfigFB.AddDataDir(builder,value)) dir
    RaftConfigFB.AddMaxRetries(builder, self.MaxRetries)
    RaftConfigFB.AddPeriodicInterval(builder, int self.PeriodicInterval)
    RaftConfigFB.EndRaftConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: RaftConfigFB) =
    either {
      let! level = Iris.Core.LogLevel.TryParse fb.LogLevel
      return
        { RequestTimeout   = fb.RequestTimeout * 1<ms>
          ElectionTimeout  = fb.ElectionTimeout * 1<ms>
          MaxLogDepth      = fb.MaxLogDepth
          LogLevel         = level
          DataDir          = filepath fb.DataDir
          MaxRetries       = fb.MaxRetries
          PeriodicInterval = fb.PeriodicInterval * 1<ms> }
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

  // ** Default

  static member Default =
    { Executables = [| |]
      Plugins     = [| |] }

  // ** ToOffset

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

  // ** FromFB

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

  // ** Default

  static member Default =
    { Framebase = 50u
      Input     = "Iris Freerun"
      Servers   = [| |]
      UDPPort   = 8071u
      TCPPort   = 8072u }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let input = Option.mapNull builder.CreateString self.Input
    let servers =
      Array.map (string >> builder.CreateString) self.Servers
      |> fun offsets -> TimingConfigFB.CreateServersVector(builder,offsets)

    TimingConfigFB.StartTimingConfigFB(builder)
    TimingConfigFB.AddFramebase(builder,self.Framebase)
    Option.iter (fun value -> TimingConfigFB.AddInput(builder,value)) input
    TimingConfigFB.AddServers(builder, servers)
    TimingConfigFB.AddUDPPort(builder, self.UDPPort)
    TimingConfigFB.AddTCPPort(builder, self.TCPPort)
    TimingConfigFB.EndTimingConfigFB(builder)

  // ** FromFB

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

  // ** Default

  static member Default =
    { SampleRate = 48000u }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    AudioConfigFB.StartAudioConfigFB(builder)
    AudioConfigFB.AddSampleRate(builder, self.SampleRate)
    AudioConfigFB.EndAudioConfigFB(builder)

  // ** FromFB

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

  // ** ToString

  override self.ToString() =
    sprintf "HostGroup:
              Name: %A
              Members: %A"
            self.Name
            (Array.fold (fun m s -> m + " " + string s) "" self.Members)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString

    let members =
      Array.map (string >> builder.CreateString) self.Members
      |> fun offsets -> HostGroupFB.CreateMembersVector(builder, offsets)

    HostGroupFB.StartHostGroupFB(builder)
    Option.iter (fun value -> HostGroupFB.AddName(builder,value)) name
    HostGroupFB.AddMembers(builder,members)
    HostGroupFB.EndHostGroupFB(builder)

  // ** FromFB

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
        { Name    = name fb.Name
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
        Name    = name Constants.DEFAULT
        Members = Map.empty
        Groups  = [| |] }

  // ** ToString

  override self.ToString() =
    sprintf "Cluster [Id: %s Name: %A Members: %d Groups: %d]"
      (string self.Id)
      self.Name
      (Map.fold (fun m _ _ -> m + 1) 0 self.Members)
      (Array.length self.Groups)

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString

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
    Option.iter (fun value -> ClusterConfigFB.AddName(builder,value)) name
    ClusterConfigFB.AddMembers(builder, members)
    ClusterConfigFB.AddGroups(builder, groups)
    ClusterConfigFB.EndClusterConfigFB(builder)

  // ** FromFB

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
          Name    = name fb.Name
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
  { Machine:    IrisMachine
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
      { Machine    = IrisMachine.Default
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

  // ** ToOffset

  //  ____  _
  // | __ )(_)_ __   __ _ _ __ _   _
  // |  _ \| | '_ \ / _` | '__| | | |
  // | |_) | | | | | (_| | |  | |_| |
  // |____/|_|_| |_|\__,_|_|   \__, |
  //                           |___/

  member self.ToOffset(builder: FlatBufferBuilder) =
    let version = Option.mapNull builder.CreateString self.Version
    let audio = Binary.toOffset builder self.Audio
    let vvvv = Binary.toOffset builder self.Vvvv
    let raft = Binary.toOffset builder self.Raft
    let timing = Binary.toOffset builder self.Timing
    let machine = Binary.toOffset builder self.Machine

    let site =
      match self.ActiveSite with
      | Some id -> id |> string |> builder.CreateString |> Some
      | None -> None

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
    Option.iter (fun value -> ConfigFB.AddVersion(builder,value)) version
    Option.iter (fun value -> ConfigFB.AddActiveSite(builder, value)) site
    ConfigFB.AddMachine(builder, machine)
    ConfigFB.AddAudioConfig(builder, audio)
    ConfigFB.AddVvvvConfig(builder, vvvv)
    ConfigFB.AddRaftConfig(builder, raft)
    ConfigFB.AddTimingConfig(builder, timing)
    ConfigFB.AddSites(builder, sites)
    ConfigFB.AddViewPorts(builder, viewports)
    ConfigFB.AddDisplays(builder, displays)
    ConfigFB.AddTasks(builder, tasks)
    ConfigFB.EndConfigFB(builder)

  // ** FromFB

  static member FromFB(fb: ConfigFB) =
    either {
      let version = fb.Version

      let site =
        if isNull fb.ActiveSite then
          None
        else
          Some (Id fb.ActiveSite)

      let! machine =
        #if FABLE_COMPILER
        IrisMachine.FromFB fb.Machine
        #else
        let nullable = fb.Machine
        if nullable.HasValue then
          let value = nullable.Value
          IrisMachine.FromFB value
        else
          "Unable to parse empty IrisMachineFB value"
          |> Error.asParseError "IrisConfig.FromFB"
          |> Either.fail
        #endif

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
        { Machine   = machine
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

  // ** VvvvExecutableYaml

  type VvvvExecutableYaml() =
    [<DefaultValue>] val mutable Path: string
    [<DefaultValue>] val mutable Version: string
    [<DefaultValue>] val mutable Required: bool

  // ** VvvvPluginYaml

  type VvvvPluginYaml() =
    [<DefaultValue>] val mutable Path: string
    [<DefaultValue>] val mutable Name: string

  // ** VvvvYaml

  type VvvvYaml() =
    [<DefaultValue>] val mutable Executables: VvvvExecutableYaml array
    [<DefaultValue>] val mutable Plugins:     VvvvPluginYaml array

  // ** TimingYaml

  type TimingYaml() =
    [<DefaultValue>] val mutable Framebase: int
    [<DefaultValue>] val mutable Input: string
    [<DefaultValue>] val mutable Servers: string array
    [<DefaultValue>] val mutable UDPPort: int
    [<DefaultValue>] val mutable TCPPort: int

  // ** AudioYaml

  type AudioYaml() =
    [<DefaultValue>] val mutable SampleRate: int

  // ** RectYaml

  type RectYaml() =
    [<DefaultValue>] val mutable X: int
    [<DefaultValue>] val mutable Y: int

  // ** CoordinateYaml

  type CoordinateYaml() =
    [<DefaultValue>] val mutable X: int
    [<DefaultValue>] val mutable Y: int

  // ** ViewPortYaml

  type ViewPortYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Position: CoordinateYaml
    [<DefaultValue>] val mutable Size: RectYaml
    [<DefaultValue>] val mutable OutputPosition: CoordinateYaml
    [<DefaultValue>] val mutable OutputSize: RectYaml
    [<DefaultValue>] val mutable Overlap: RectYaml
    [<DefaultValue>] val mutable Description: string

  // ** SignalYaml

  type SignalYaml () =
    [<DefaultValue>] val mutable Size: RectYaml
    [<DefaultValue>] val mutable Position: CoordinateYaml

  // ** RegionYaml

  type RegionYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable SrcPosition: CoordinateYaml
    [<DefaultValue>] val mutable SrcSize: RectYaml
    [<DefaultValue>] val mutable OutputPosition: CoordinateYaml
    [<DefaultValue>] val mutable OutputSize: RectYaml

  // ** RegionMapYaml

  type RegionMapYaml() =
    [<DefaultValue>] val mutable SrcViewportId: string
    [<DefaultValue>] val mutable Regions: RegionYaml array

  // ** DisplayYaml

  type DisplayYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Size: RectYaml
    [<DefaultValue>] val mutable Signals: SignalYaml array
    [<DefaultValue>] val mutable RegionMap: RegionMapYaml

  // ** EngineYaml

  type EngineYaml() =
    [<DefaultValue>] val mutable LogLevel: string
    [<DefaultValue>] val mutable DataDir: string
    [<DefaultValue>] val mutable BindAddress: string
    [<DefaultValue>] val mutable RequestTimeout: int
    [<DefaultValue>] val mutable ElectionTimeout: int
    [<DefaultValue>] val mutable PeriodicInterval: int
    [<DefaultValue>] val mutable MaxLogDepth: int
    [<DefaultValue>] val mutable MaxRetries: int

  // ** ArgumentYaml

  type ArgumentYaml() =
    [<DefaultValue>] val mutable Key: string
    [<DefaultValue>] val mutable Value: string

  // ** TaskYaml

  type TaskYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Description: string
    [<DefaultValue>] val mutable DisplayId: string
    [<DefaultValue>] val mutable AudioStream: string
    [<DefaultValue>] val mutable Arguments: ArgumentYaml array

  // ** GroupYaml

  type GroupYaml() =
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Members: string array

  // ** SiteYaml

  type SiteYaml () =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Members: RaftMemberYaml array
    [<DefaultValue>] val mutable Groups: GroupYaml array

  // ** IrisProjectYaml

  type IrisProjectYaml() =
    [<DefaultValue>] val mutable Id: string
    [<DefaultValue>] val mutable Name: string
    [<DefaultValue>] val mutable Version: string
    [<DefaultValue>] val mutable Copyright: string
    [<DefaultValue>] val mutable Author: string
    [<DefaultValue>] val mutable CreatedOn: string
    [<DefaultValue>] val mutable LastSaved: string
    [<DefaultValue>] val mutable ActiveSite: string

    [<DefaultValue>] val mutable VVVV: VvvvYaml
    [<DefaultValue>] val mutable Engine: EngineYaml
    [<DefaultValue>] val mutable Timing: TimingYaml
    [<DefaultValue>] val mutable Audio: AudioYaml

    [<DefaultValue>] val mutable ViewPorts: ViewPortYaml array
    [<DefaultValue>] val mutable Displays:  DisplayYaml array
    [<DefaultValue>] val mutable Tasks:  TaskYaml array
    [<DefaultValue>] val mutable Sites:  SiteYaml array

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

  let internal parseRect (rect: RectYaml) : Either<IrisError,Rect> =
    (rect.X, rect.Y)
    |> Rect
    |> Either.succeed

  // ** parseCoordinate

  let internal parseCoordinate (coord: CoordinateYaml) : Either<IrisError,Coordinate> =
    (coord.X, coord.Y)
    |> Coordinate
    |> Either.succeed

  // ** parseStringProp

  let internal parseStringProp (str : string) : string option =
    if str.Length > 0 then Some(str) else None

  // ** parseAudio

  /// ### Parse the Audio configuration section
  ///
  /// Parses the Audio configuration section of the passed-in configuration file.
  ///
  /// # Returns: AudioConfig
  let internal parseAudio (config: IrisProjectYaml) : Either<IrisError, AudioConfig> =
    Either.tryWith (Error.asParseError "Config.parseAudio") <| fun _ ->
      { SampleRate = uint32 config.Audio.SampleRate }

  // ** saveAudio

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let internal saveAudio (file: IrisProjectYaml, config: IrisConfig) =
    let audio = AudioYaml()
    audio.SampleRate <- int (config.Audio.SampleRate)
    file.Audio <- audio
    (file, config)

  // ** parseExe

  let internal parseExe (exe: VvvvExecutableYaml) : Either<IrisError, VvvvExe> =
    Right { Executable = filepath exe.Path
            Version    = version exe.Version
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

  let internal parsePlugin (plugin: VvvvPluginYaml) : Either<IrisError, VvvvPlugin> =
    Right { Name = name plugin.Name
            Path = filepath plugin.Path }

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
  let internal parseVvvv (config: IrisProjectYaml) : Either<IrisError, VvvvConfig> =
    either {
      let! exes = parseExes config.VVVV.Executables
      let! plugins = parsePlugins config.VVVV.Plugins
      return { Executables = exes
               Plugins     = plugins }
    }

  // ** saveVvvv

  /// ### Save the VVVV configuration
  ///
  /// Translate the values from Config into the passed in configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveVvvv (file: IrisProjectYaml, config: IrisConfig) =
    let exes = ResizeArray()
    for exe in config.Vvvv.Executables do
      let entry = VvvvExecutableYaml()
      entry.Path <- unwrap exe.Executable;
      entry.Version <- unwrap exe.Version;
      entry.Required <- exe.Required
      exes.Add(entry)

    let plugins = ResizeArray()
    for plug in config.Vvvv.Plugins do
      let entry = VvvvPluginYaml()
      entry.Name <- unwrap plug.Name
      entry.Path <- unwrap plug.Path
      plugins.Add(entry)

    let vvvv = VvvvYaml()
    vvvv.Executables <- exes.ToArray()
    vvvv.Plugins <- plugins.ToArray()

    file.VVVV <- vvvv

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
  let internal parseRaft (config: IrisProjectYaml) : Either<IrisError, RaftConfig> =
    either {
      let! loglevel = Iris.Core.LogLevel.TryParse config.Engine.LogLevel

      try
        return
          { RequestTimeout   = config.Engine.RequestTimeout * 1<ms>
            ElectionTimeout  = config.Engine.ElectionTimeout * 1<ms>
            MaxLogDepth      = config.Engine.MaxLogDepth
            LogLevel         = loglevel
            DataDir          = filepath config.Engine.DataDir
            MaxRetries       = config.Engine.MaxRetries
            PeriodicInterval = config.Engine.PeriodicInterval * 1<ms> }
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
  let internal saveRaft (file: IrisProjectYaml, config: IrisConfig) =
    let engine = EngineYaml()
    engine.RequestTimeout   <- int config.Raft.RequestTimeout
    engine.ElectionTimeout  <- int config.Raft.ElectionTimeout
    engine.MaxLogDepth      <- int config.Raft.MaxLogDepth
    engine.LogLevel         <- string config.Raft.LogLevel
    engine.DataDir          <- unwrap config.Raft.DataDir
    engine.MaxRetries       <- int config.Raft.MaxRetries
    engine.PeriodicInterval <- int config.Raft.PeriodicInterval
    file.Engine <- engine
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
  let internal parseTiming (config: IrisProjectYaml) : Either<IrisError,TimingConfig> =
    either {
      let timing = config.Timing
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
  let internal saveTiming (file: IrisProjectYaml, config: IrisConfig) =
    let timing = TimingYaml()
    timing.Framebase <- int (config.Timing.Framebase)
    timing.Input     <- config.Timing.Input

    let servers = ResizeArray()
    for srv in config.Timing.Servers do
      servers.Add(string srv)

    timing.Servers <- servers.ToArray()

    timing.TCPPort <- int (config.Timing.TCPPort)
    timing.UDPPort <- int (config.Timing.UDPPort)

    file.Timing <- timing

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
               Name           = name viewport.Name
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
  let internal parseViewPorts (config: IrisProjectYaml) : Either<IrisError,ViewPort array> =
    either {
      let arr =
        config.ViewPorts
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
          config.ViewPorts

      return viewports
    }

  // ** saveViewPorts

  /// ### Transfers the passed array of ViewPort values
  ///
  /// Adds a config section for each ViewPort value in the passed in Config to the configuration
  /// file.
  ///
  /// # Returns: ConfigFile
  let internal saveViewPorts (file: IrisProjectYaml, config: IrisConfig) =
    let viewports = ResizeArray()
    for vp in config.ViewPorts do
      let item = ViewPortYaml()
      item.Id             <- string vp.Id
      item.Name           <- unwrap vp.Name

      let size = RectYaml()
      size.X <- vp.Size.X
      size.Y <- vp.Size.Y
      item.Size <- size

      let position = CoordinateYaml()
      position.X <- vp.Position.X
      position.Y <- vp.Position.Y
      item.Position <- position

      let overlap = RectYaml()
      overlap.X <- vp.Overlap.X
      overlap.Y <- vp.Overlap.Y
      item.Overlap <- overlap

      let position = CoordinateYaml()
      position.X <- vp.OutputPosition.X
      position.Y <- vp.OutputPosition.Y
      item.OutputPosition <- position

      let size = RectYaml()
      size.X <- vp.OutputSize.X
      size.Y <- vp.OutputSize.Y
      item.OutputSize <- size

      item.Description <- vp.Description
      viewports.Add(item)

    file.ViewPorts <- viewports.ToArray()

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
          Name           = name region.Name
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
               Name      = name display.Name
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
  let internal parseDisplays (config: IrisProjectYaml) : Either<IrisError, Display array> =
    either {
      let arr =
        config.Displays
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
          config.Displays

      return displays
    }

  // ** saveDisplays

  /// ### Transfer the Display config to a configuration file
  ///
  /// Save all `Display` values in `Config` to the passed configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveDisplays (file: IrisProjectYaml, config: IrisConfig) =
    let displays = ResizeArray()
    for dp in config.Displays do
      let display = DisplayYaml()
      display.Id <- string dp.Id
      display.Name <- unwrap dp.Name

      let size = RectYaml()
      size.X <- dp.Size.X
      size.Y <- dp.Size.Y
      display.Size <- size

      let regionmap = RegionMapYaml()
      regionmap.SrcViewportId <- string dp.RegionMap.SrcViewportId

      let regions = ResizeArray()

      for region in dp.RegionMap.Regions do
        let r = RegionYaml()
        r.Id <- string region.Id
        r.Name <- unwrap region.Name

        let position = CoordinateYaml()
        position.X <- region.OutputPosition.X
        position.Y <- region.OutputPosition.Y
        r.OutputPosition <- position

        let size = RectYaml()
        size.X <- region.OutputSize.X
        size.Y <- region.OutputSize.Y
        r.OutputSize <- size

        let position = CoordinateYaml()
        position.X <- region.SrcPosition.X
        position.Y <- region.SrcPosition.Y
        r.SrcPosition <- position

        let size = RectYaml()
        size.X <- region.SrcSize.X
        size.Y <- region.SrcSize.Y
        r.SrcSize <- size

        regions.Add(r)

      regionmap.Regions <- regions.ToArray()

      display.RegionMap <- regionmap

      let signals = ResizeArray()

      for signal in dp.Signals do
        let s = SignalYaml()

        let position = CoordinateYaml()
        position.X <- signal.Position.X
        position.Y <- signal.Position.Y
        s.Position <- position

        let size = RectYaml()
        size.X <- signal.Size.X
        size.Y <- signal.Size.Y
        s.Size <- size

        signals.Add(s)

      display.Signals <- signals.ToArray()
      displays.Add display

    file.Displays <- displays.ToArray()
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
      return (argument.Key, argument.Value)
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
  let internal parseTasks (config: IrisProjectYaml) : Either<IrisError,Task array> =
    either {
      let arr =
        config.Tasks
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
          config.Tasks

      return tasks
    }

  // ** saveTasks

  /// ### Save the Tasks to a config file
  ///
  /// Transfers all `Task` values into the configuration file.
  ///
  /// # Returns: ConfigFile
  let internal saveTasks (file: IrisProjectYaml, config: IrisConfig) =
    let tasks = ResizeArray()
    for task in config.Tasks do
      let t = TaskYaml()
      t.Id <- string task.Id
      t.AudioStream <- task.AudioStream
      t.Description <- task.Description
      t.DisplayId   <- string task.DisplayId

      let args = ResizeArray()

      for arg in task.Arguments do
        let a = ArgumentYaml()
        a.Key <- fst arg
        a.Value <- snd arg
        args.Add(a)

      t.Arguments <- args.ToArray()
      tasks.Add t

    file.Tasks <- tasks.ToArray()
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
  let internal parseMember (mem: RaftMemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! ip = IpAddress.TryParse mem.IpAddr
      let! state = RaftMemberState.TryParse mem.State

      try
        return { Id         = Id mem.Id
                 HostName   = name mem.HostName
                 IpAddr     = ip
                 Port       = mem.Port    |> uint16 |> port
                 WsPort     = mem.WsPort  |> uint16 |> port
                 GitPort    = mem.GitPort |> uint16 |> port
                 ApiPort    = mem.ApiPort |> uint16 |> port
                 State      = state
                 Voting     = true
                 VotedForMe = false
                 NextIndex  = index 1
                 MatchIndex = index 0 }
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
      let ids = Seq.map (string >> Id) group.Members |> Seq.toArray
      return { Name = name group.Name
               Members = ids }
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

  let internal parseCluster (cluster: SiteYaml) : Either<IrisError, ClusterConfig> =
    either {
      let! groups = parseGroups cluster.Groups
      let! mems = parseMembers cluster.Members

      return { Id = Id cluster.Id
               Name = name cluster.Name
               Members = mems
               Groups = groups }
    }

  // ** parseSites

  let internal parseSites (config: IrisProjectYaml) : Either<IrisError, ClusterConfig array> =
    either {
      let arr =
        config.Sites
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
          config.Sites

      return sites
    }

  // ** saveSites

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let internal saveSites (file: IrisProjectYaml, config: IrisConfig) =
    let sites = ResizeArray()

    match config.ActiveSite with
    | Some id -> file.ActiveSite <- string id
    | None -> file.ActiveSite <- null

    for cluster in config.Sites do
      let cfg = SiteYaml()
      let members = ResizeArray()
      let groups = ResizeArray()

      cfg.Id <- string cluster.Id
      cfg.Name <- unwrap cluster.Name

      for KeyValue(memId,mem) in cluster.Members do
        let n = RaftMemberYaml()
        n.Id       <- string memId
        n.IpAddr   <- string mem.IpAddr
        n.HostName <- unwrap mem.HostName
        n.Port     <- unwrap mem.Port
        n.WsPort   <- unwrap mem.WsPort
        n.GitPort  <- unwrap mem.GitPort
        n.ApiPort  <- unwrap mem.ApiPort
        n.State    <- string mem.State
        members.Add(n)

      for group in cluster.Groups do
        let g = GroupYaml()
        g.Name <- unwrap group.Name
        g.Members <- Array.map string group.Members
        groups.Add(g)

      cfg.Members <- members.ToArray()
      sites.Add(cfg)

    file.Sites <- sites.ToArray()
    (file, config)

  // ** parseLastSaved (private)

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let internal parseLastSaved (config: IrisProjectYaml) =
    if config.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(config.LastSaved))
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
  let internal parseCreatedOn (config: IrisProjectYaml) =
    if config.CreatedOn.Length > 0
    then
      try
        DateTime.Parse(config.CreatedOn)
      with
        | _ -> DateTime.FromFileTimeUtc(int64 0)
    else DateTime.FromFileTimeUtc(int64 0)

  // ** parse

  let internal parse (str: string) =
    try
      let serializer = Serializer()
      let config = serializer.Deserialize<IrisProjectYaml>(str)
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

  let fromFile (file: ProjectYaml.IrisProjectYaml) (machine: IrisMachine) : Either<IrisError, IrisConfig> =
    either {
      let  version   = file.Version
      let! raftcfg   = ProjectYaml.parseRaft      file
      let! timing    = ProjectYaml.parseTiming    file
      let! vvvv      = ProjectYaml.parseVvvv      file
      let! audio     = ProjectYaml.parseAudio     file
      let! viewports = ProjectYaml.parseViewPorts file
      let! displays  = ProjectYaml.parseDisplays  file
      let! tasks     = ProjectYaml.parseTasks     file
      let! sites     = ProjectYaml.parseSites     file

      let site =
        if isNull file.ActiveSite || file.ActiveSite = "" then
          None
        else
          Some (Id file.ActiveSite)

      return { Machine    = machine
               ActiveSite = site
               Version    = version
               Vvvv       = vvvv
               Audio      = audio
               Raft       = raftcfg
               Timing     = timing
               ViewPorts  = viewports
               Displays   = displays
               Tasks      = tasks
               Sites      = sites }
    }

  #endif

  // ** toFile

  #if !FABLE_COMPILER && !IRIS_NODES

  let toFile (config: IrisConfig) (file: ProjectYaml.IrisProjectYaml) =
    file.Version <- string config.Version
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

  let create (machine: IrisMachine) =
    { Machine    = machine
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
    { config with Machine = machine }

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
    let sites =
      Array.map
        (fun (site: ClusterConfig) ->
          if cluster.Id = site.Id
          then cluster
          else site)
        config.Sites
    { config with Sites = sites }

  // ** updateSites

  let updateSites (sites: ClusterConfig array) (config: IrisConfig) =
    { config with Sites = sites }

  // ** findMember

  let findMember (config: IrisConfig) (id: Id) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
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
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
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
    match config.ActiveSite with
    | Some id -> Array.tryFind (fun (site: ClusterConfig) -> site.Id = id) config.Sites
    | None -> None

  // ** getActiveMember

  let getActiveMember (config: IrisConfig) =
    config
    |> getActiveSite
    |> Option.bind (fun (site: ClusterConfig) -> Map.tryFind config.Machine.MachineId site.Members)

  // ** setMembers

  let setMembers (mems: Map<MemberId,RaftMember>) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
      | Some site ->
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** selfMember

  let selfMember (options: IrisConfig) =
    findMember options options.Machine.MachineId

  // ** addSitePrivate

  let private addSitePrivate (site: ClusterConfig) setActive (config: IrisConfig) =
    let i = config.Sites |> Array.tryFindIndex (fun s -> s.Id = site.Id)
    let copy = Array.zeroCreate (config.Sites.Length + (if Option.isSome i then 0 else 1))
    Array.iteri (fun i s -> copy.[i] <- s) config.Sites
    copy.[match i with Some i -> i | None -> config.Sites.Length] <- site
    if setActive
    then { config with ActiveSite = Some site.Id; Sites = copy }
    else { config with Sites = copy }

  // ** addSite

  /// Adds or replaces a site with same Id
  let addSite (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site false config

  // ** addSiteAndActive

  /// Adds or replaces a site with same Id and sets it as the active site
  let addSiteAndSetActive (site: ClusterConfig) (config: IrisConfig) =
    addSitePrivate site true config

  // ** removeSite

  let removeSite (id: Id) (config: IrisConfig) =
    let sites = Array.filter (fun (site: ClusterConfig) -> site.Id <> id) config.Sites
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
    Array.tryFind (fun (site: ClusterConfig) -> site.Id = id) config.Sites

  // ** addMember

  let addMember (mem: RaftMember) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst: ClusterConfig) -> clst.Id = active) config.Sites with
      | Some site ->
        let mems = Map.add mem.Id mem site.Members
        updateCluster { site with Members = mems } config
      | None -> config
    | None -> config

  // ** removeMember

  let removeMember (id: Id) (config: IrisConfig) =
    match config.ActiveSite with
    | Some active ->
      match Array.tryFind (fun (clst:ClusterConfig) -> clst.Id = active) config.Sites with
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
    unwrap config.Raft.DataDir <.> RAFT_METADATA_FILENAME + ASSET_EXTENSION

  // ** logDataPath

  let logDataPath (config: IrisConfig) =
    unwrap config.Raft.DataDir <.> RAFT_LOGDATA_PATH

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
  ; Path      : FilePath
  ; CreatedOn : TimeStamp
  ; LastSaved : TimeStamp option
  ; Copyright : string    option
  ; Author    : string    option
  ; Config    : IrisConfig }

  // ** ToString

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
      (unwrap project.Name)
      (unwrap project.Path)
      project.CreatedOn
      project.LastSaved
      project.Copyright
      project.Author
      project.Config

  // ** Empty

  static member Empty
    with get () =
      { Id        = Id Constants.EMPTY
        Name      = name Constants.EMPTY
        Path      = filepath ""
        CreatedOn = timestamp ""
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
    with get () =
      PROJECT_FILENAME + ASSET_EXTENSION
      |> filepath

  // ** Load

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
          if Path.isPathRooted basepath then
            basepath
          else
            Path.getFullPath basepath
        if Path.endsWith filename withRoot then
          withRoot
        else
          unwrap withRoot <.> filename

      if not (File.exists normalizedPath) then
        return!
          sprintf "Project Not Found: %O" normalizedPath
          |> Error.asProjectError "Project.load"
          |> Either.fail
      else
        let! str = Asset.read normalizedPath
        let! project = Yaml.decode str

        return
          { project with
              Path   = Path.getDirectoryName normalizedPath |> unwrap |> filepath
              Config = Config.updateMachine machine project.Config }
    }

  // ** Save

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
    let strornull (str: string) =
      let nll = sprintf "%A" null
      match str with
      | null -> nll |> builder.CreateString
      | str -> str |> builder.CreateString
    let id = builder.CreateString (string self.Id)
    let name = self.Name |> unwrap |> Option.mapNull builder.CreateString
    let path = self.Path |> unwrap |> Option.mapNull builder.CreateString
    let created = Option.mapNull builder.CreateString self.CreatedOn
    let lastsaved = Option.map strornull self.LastSaved
    let copyright = Option.map strornull self.Copyright
    let author = Option.map strornull self.Author
    let config = Binary.toOffset builder self.Config

    ProjectFB.StartProjectFB(builder)
    ProjectFB.AddId(builder, id)

    Option.iter (fun value -> ProjectFB.AddPath(builder,value)) path
    Option.iter (fun value -> ProjectFB.AddName(builder,value)) name
    Option.iter (fun value -> ProjectFB.AddCreatedOn(builder,value)) created
    Option.iter (fun offset -> ProjectFB.AddLastSaved(builder,offset)) lastsaved
    Option.iter (fun offset -> ProjectFB.AddCopyright(builder,offset)) copyright
    Option.iter (fun offset -> ProjectFB.AddAuthor(builder,offset)) author

    ProjectFB.AddConfig(builder, config)
    ProjectFB.EndProjectFB(builder)

  // ** ToBytes

  member self.ToBytes () =
    Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes(bytes: byte[]) =
    Binary.createBuffer bytes
    |> ProjectFB.GetRootAsProjectFB
    |> IrisProject.FromFB

  // ** FromFB

  static member FromFB(fb: ProjectFB) =
    either {
      let nll = sprintf "%A" null

      let! lastsaved =
        match fb.LastSaved with
        | null    -> Right None
        | value when value = nll -> Right (Some null)
        | date -> Right (Some date)

      let! copyright =
        match fb.Copyright with
        | null   -> Right None
        | value when value = nll -> Right (Some null)
        | str -> Right (Some str)

      let! author =
        match fb.Author with
        | null   -> Right None
        | value when value = nll -> Right (Some null)
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
          Name      = name fb.Name
          Path      = filepath fb.Path
          CreatedOn = fb.CreatedOn
          LastSaved = lastsaved
          Copyright = copyright
          Author    = author
          Config    = config }
    }

  // ** ToYaml

  #if !FABLE_COMPILER && !IRIS_NODES

  // __   __              _
  // \ \ / /_ _ _ __ ___ | |
  //  \ V / _` | '_ ` _ \| |
  //   | | (_| | | | | | | |
  //   |_|\__,_|_| |_| |_|_|

  member self.ToYaml(_: Serializer) =
    let config = ProjectYaml.IrisProjectYaml()

    Config.toFile self.Config config

    // Project metadata
    config.Id        <- string self.Id
    config.Name      <- unwrap self.Name
    config.CreatedOn <- self.CreatedOn

    Option.map
      (fun author -> config.Author <- author)
      self.Author
    |> ignore

    Option.map
      (fun copyright -> config.Copyright <- copyright)
      self.Copyright
    |> ignore

    Option.map
      (fun saved -> config.LastSaved <- saved)
      self.LastSaved
    |> ignore

    config.ToString()

  // ** FromYaml

  static member FromYaml(str: string) =
    either {
      let! meta = ProjectYaml.parse str

      let lastSaved =
        match meta.LastSaved with
          | null | "" -> None
          | str ->
            try
              DateTime.Parse str |> ignore
              Some str
            with
              | _ -> None

      let dummy = MachineConfig.create Constants.DEFAULT_IP None

      let! config = Config.fromFile meta dummy

      return { Id        = Id meta.Id
               Name      = name meta.Name
               Path      = filepath (Path.GetFullPath ".")
               CreatedOn = timestamp meta.CreatedOn
               LastSaved = lastSaved
               Copyright = ProjectYaml.parseStringProp meta.Copyright
               Author    = ProjectYaml.parseStringProp meta.Author
               Config    = config }
    }

  #endif

// * Project module

[<RequireQualifiedAccess>]
module Project =

  // ** tag

  let private tag (str: string) = String.format "Project.{0}" str

  // ** toFilePath

  let toFilePath (path: FilePath) =
    path |> unwrap |> filepath

  // ** ofFilePath

  let ofFilePath (path: FilePath) =
    path |> unwrap |> filepath

  // ** repository

  #if !FABLE_COMPILER && !IRIS_NODES

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.
  ///
  /// # Returns: Repository option
  let repository (project: IrisProject) =
    project.Path
    |> Git.Repo.repository

  #endif

  // **  localRemote

  let localRemote (project: IrisProject) =
    project.Config
    |> Config.getActiveMember
    |> Option.map (Uri.gitUri project.Name)

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

  // ** checkPath

  //  ____       _   _
  // |  _ \ __ _| |_| |__  ___
  // | |_) / _` | __| '_ \/ __|
  // |  __/ (_| | |_| | | \__ \
  // |_|   \__,_|\__|_| |_|___/

  #if !FABLE_COMPILER && !IRIS_NODES

  let checkPath (machine: IrisMachine) (projectName: Name) =
    let file = PROJECT_FILENAME + ASSET_EXTENSION
    let path = machine.WorkSpace </> (unwrap projectName <.> file)
    if File.exists path |> not then
      sprintf "Project Not Found: %O" projectName
      |> Error.asProjectError (tag "checkPath")
      |> Either.fail
    else
      Either.succeed path

  #endif

  // ** filePath

  let filePath (project: IrisProject) : FilePath =
    unwrap project.Path <.> PROJECT_FILENAME + ASSET_EXTENSION

  // ** userDir

  let userDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> USER_DIR

  // ** cueDir

  let cueDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> CUE_DIR

  // ** cuelistDir

  let cuelistDir (project: IrisProject) : FilePath =
    unwrap project.Path <.> CUELIST_DIR

  // ** writeDaemonExportFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeDaemonExportFile (repo: Repository) =
    either {
      let path = repo.Info.Path <.> "git-daemon-export-ok"
      let! _ = Asset.write path (Payload "")
      return ()
    }

  #endif

  // ** writeGitIgnoreFile (private)

  #if !FABLE_COMPILER && !IRIS_NODES

  let private writeGitIgnoreFile (repo: Repository) =
    either {
      let parent = Git.Repo.parentPath repo
      let path = parent </> filepath ".gitignore"
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
      let gitkeep = target </> filepath ".gitkeep"
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
        if Path.isPathRooted filepath then
          filepath
        else
          toFilePath project.Path </> filepath
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
      let info = File.info path
      do! info.Directory.FullName |> filepath |> FileSystem.mkDir
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
    let msg = String.Format("{0} saved {1}", committer.UserName, Path.getFileName filepath)
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
    let msg = String.Format("{0} deleted {1}", committer.UserName, filepath)
    deleteFile filepath signature msg project

  let private needsInit (project: IrisProject) =
    let projPath = project.Path
    let projdir =  projPath |> Directory.exists
    let git = Directory.exists (projPath </> filepath ".git")
    let cues = Directory.exists (projPath </> filepath CUE_DIR)
    let cuelists = Directory.exists (projPath </> filepath CUELIST_DIR)
    let users = Directory.exists (projPath </> filepath USER_DIR)

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
      let! repo = project.Path |> Git.Repo.init
      do! writeDaemonExportFile repo
      do! Git.Repo.setReceivePackConfig repo
      do! writeGitIgnoreFile repo
      do! createAssetDir repo (filepath CUE_DIR)
      do! createAssetDir repo (filepath USER_DIR)
      do! createAssetDir repo (filepath CUELIST_DIR)
      do! createAssetDir repo (filepath PINGROUP_DIR)
      do! createAssetDir repo (filepath CUEPLAYER_DIR)
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
  let create (path: FilePath) (projectName: string) (machine: IrisMachine) =
    either {
      let project =
        { Id        = Id.Create()
          Name      = name projectName
          Path      = path
          CreatedOn = Time.createTimestamp()
          LastSaved = Some (Time.createTimestamp ())
          Copyright = None
          Author    = None
          Config    = Config.create machine  }

      do! initRepo project
      let! _ = Asset.saveWithCommit (toFilePath path) User.Admin.Signature project
      return project
    }

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

  // ** findMember

  let findMember (mem: MemberId) (project: IrisProject) =
    Config.findMember project.Config mem

  // ** selfMember

  let selfMember (project: IrisProject) =
    Config.findMember project.Config project.Config.Machine.MachineId

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

  // ** updateRemotes

  #if !FABLE_COMPILER && !IRIS_NODES

  let updateRemotes (project: IrisProject) = either {
      let! repo = repository project

      // delete all current remotes
      let current = Git.Config.remotes repo
      do! Map.fold
            (fun kontinue name _ -> either {
              do! kontinue
              do! Git.Config.delRemote repo name })
            (Right ())
            current

      let! mem = Config.selfMember project.Config

      // add remotes for all other peers
      do! match Config.getActiveSite project.Config with
          | Some cluster ->
            Map.fold
              (fun kontinue id peer -> either {
                  do! kontinue
                  if id <> mem.Id then
                    let url = Uri.gitUri project.Name peer
                    let name = string peer.Id
                    do! Git.Config.addRemote repo name url
                        |> Either.iterError (string >> Logger.err (tag "updateRemotes"))
                        |> Either.succeed
                })
              (Right ())
              cluster.Members
          | None -> Either.nothing
    }

  #endif
