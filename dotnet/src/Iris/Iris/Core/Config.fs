namespace Iris.Core

// * Imports

open System
open System.IO
open System.Reflection
open Iris.Raft
open SharpYaml
open SharpYaml.Serialization

#if FABLE_COMPILER

#else

open Iris.Serialization.Raft
open FlatBuffers

#endif

// * IrisMachine

type IrisMachine =
  { MachineId : Id
    HostName  : string
    WorkSpace : FilePath }

  override self.ToString() =
    sprintf "MachineId: %s" (string self.MachineId)

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.MachineId)
    let hn = builder.CreateString self.HostName
    let wsp = builder.CreateString self.WorkSpace

    MachineConfigFB.StartMachineConfigFB(builder)
    MachineConfigFB.AddMachineId(builder,id)
    MachineConfigFB.AddHostName(builder, hn)
    MachineConfigFB.AddWorkSpace(builder,wsp)
    MachineConfigFB.EndMachineConfigFB(builder)

// * MachineConfig module

[<RequireQualifiedAccess>]
module MachineConfig =

  let private tag (str: string) = sprintf "MachineConfig.%s" str

  // ** MachineConfigYaml (private)

  type MachineConfigYaml () =
    [<DefaultValue>] val mutable MachineId : string
    [<DefaultValue>] val mutable WorkSpace : string

    static member Create (cfg: IrisMachine) =
      let yml = new MachineConfigYaml()
      yml.MachineId <- string cfg.MachineId
      yml.WorkSpace <- cfg.WorkSpace
      yml

  // ** parse (private)

  let private parse (yml: MachineConfigYaml) : Either<IrisError,IrisMachine> =
    let hostname = Network.getHostName ()
    { MachineId = Id yml.MachineId
      HostName  = hostname
      WorkSpace = yml.WorkSpace }
    |> Either.succeed

  // ** ensureExists (private)

  let private ensureExists (path: FilePath) =
    try
      if not (Directory.Exists path) then
        Directory.CreateDirectory path
        |> ignore
    with
      | _ -> ()

  // ** defaultPath

  let defaultPath =
    let dir =
      Assembly.GetExecutingAssembly().Location
      |> Path.GetDirectoryName
    dir </> MACHINECONFIG_DEFAULT_PATH </> MACHINECONFIG_NAME + ASSET_EXTENSION

  // ** create

  let create () : IrisMachine =
    let hostname = Network.getHostName()
    let workspace =
      if Platform.isUnix then
        let home = Environment.GetEnvironmentVariable "HOME"
        home </> "iris"
      else
        @"C:\Iris"

    { MachineId = Id.Create()
      HostName  = hostname
      WorkSpace = workspace }

  // ** save

  let save (path: FilePath option) (cfg: IrisMachine) : Either<IrisError,unit> =
    let serializer = new Serializer()

    try
      let location =
        match path with
        | Some location -> location
        | None -> defaultPath

      let payload=
        cfg
        |> MachineConfigYaml.Create
        |> serializer.Serialize

      location
      |> Path.GetDirectoryName
      |> ensureExists

      File.WriteAllText(location, payload)
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "save")
        |> Either.fail

  // ** load

  let load (path: FilePath option) : Either<IrisError,IrisMachine> =
    let serializer = new Serializer()
    try
      let location =
        match path with
        | Some location -> location
        | None -> defaultPath

      let raw = File.ReadAllText location
      serializer.Deserialize<MachineConfigYaml>(raw)
      |> parse
    with
      | exn ->
        exn.Message
        |> Error.asIOError (tag "load")
        |> Either.fail

// * Aliases

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type DisplayYaml    = ProjectYaml.Project_Type.Displays_Item_Type
type ViewPortYaml   = ProjectYaml.Project_Type.ViewPorts_Item_Type
type TaskYaml       = ProjectYaml.Project_Type.Tasks_Item_Type
type ArgumentYaml   = TaskYaml.Arguments_Item_Type
type ClusterYaml    = ProjectYaml.Project_Type.Cluster_Type
type MemberYaml     = ProjectYaml.Project_Type.Cluster_Type.Members_Item_Type
type GroupYaml      = ProjectYaml.Project_Type.Cluster_Type.Groups_Item_Type
type AudioYaml      = ProjectYaml.Project_Type.Audio_Type
type EngineYaml     = ProjectYaml.Project_Type.Engine_Type
type MetadatYaml    = ProjectYaml.Project_Type.Metadata_Type
type TimingYaml     = ProjectYaml.Project_Type.Timing_Type
type VvvvYaml       = ProjectYaml.Project_Type.VVVV_Type
type ExeYaml        = ProjectYaml.Project_Type.VVVV_Type.Executables_Item_Type
type PluginYaml     = ProjectYaml.Project_Type.VVVV_Type.Plugins_Item_Type
type SignalYaml     = DisplayYaml.Signals_Item_Type
type RegionMapYaml  = DisplayYaml.RegionMap_Type
type RegionYaml     = RegionMapYaml.Regions_Item_Type

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
    LogLevel:         LogLevel
    DataDir:          FilePath
    MaxRetries:       uint8
    PeriodicInterval: uint8 }

  static member Default =
    let guid = Guid.NewGuid()
    let path = Path.GetTempPath() </> guid.ToString() </> RAFT_DIRECTORY
    { RequestTimeout   = 500u
      ElectionTimeout  = 6000u
      MaxLogDepth      = 20u
      MaxRetries       = 10uy
      PeriodicInterval = 50uy
      LogLevel         = Err
      DataDir          = path }

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
    { Executables = Array.empty
      Plugins     = Array.empty }

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
      Servers   = Array.empty
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
    AudioConfigFB.CreateAudioConfigFB(builder, self.SampleRate)

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

// * Cluster

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type Cluster =
  { Name    : Name
    Members : RaftMember array
    Groups  : HostGroup  array }

  override self.ToString() =
    sprintf "Cluster:
              Name: %A
              Members: %A
              Groups: %A"
            self.Name
            self.Members
            self.Groups

  member self.ToOffset(builder: FlatBufferBuilder) =
    let name = builder.CreateString self.Name

    let members =
      Array.map (Binary.toOffset builder) self.Members
      |> fun offsets -> ClusterConfigFB.CreateMembersVector(builder, offsets)

    let groups =
      Array.map (Binary.toOffset builder) self.Groups
      |> fun offsets -> ClusterConfigFB.CreateGroupsVector(builder, offsets)

    ClusterConfigFB.StartClusterConfigFB(builder)
    ClusterConfigFB.AddName(builder, name)
    ClusterConfigFB.AddMembers(builder, members)
    ClusterConfigFB.AddGroups(builder, groups)
    ClusterConfigFB.EndClusterConfigFB(builder)

// * IrisConfig

//  ___      _      ____             __ _
// |_ _|_ __(_)___ / ___|___  _ __  / _(_) __ _
//  | || '__| / __| |   / _ \| '_ \| |_| |/ _` |
//  | || |  | \__ \ |__| (_) | | | |  _| | (_| |
// |___|_|  |_|___/\____\___/|_| |_|_| |_|\__, |
//                                        |___/

type IrisConfig =
  { MachineConfig  : IrisMachine
    AudioConfig    : AudioConfig
    VvvvConfig     : VvvvConfig
    RaftConfig     : RaftConfig
    TimingConfig   : TimingConfig
    ClusterConfig  : Cluster
    ViewPorts      : ViewPort array
    Displays       : Display  array
    Tasks          : Task     array }

  member self.ToOffset(builder: FlatBufferBuilder) =
    let machine = Binary.toOffset builder self.MachineConfig
    let audio = Binary.toOffset builder self.AudioConfig
    let vvvv = Binary.toOffset builder self.VvvvConfig
    let raft = Binary.toOffset builder self.RaftConfig
    let timing = Binary.toOffset builder self.TimingConfig
    let cluster = Binary.toOffset builder self.ClusterConfig

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
    ConfigFB.AddMachineConfig(builder, machine)
    ConfigFB.AddAudioConfig(builder, audio)
    ConfigFB.AddVvvvConfig(builder, vvvv)
    ConfigFB.AddRaftConfig(builder, raft)
    ConfigFB.AddTimingConfig(builder, timing)
    ConfigFB.AddClusterConfig(builder, cluster)
    ConfigFB.AddViewPorts(builder, viewports)
    ConfigFB.AddDisplays(builder, displays)
    ConfigFB.AddTasks(builder, tasks)
    ConfigFB.EndConfigFB(builder)


// * Config Module

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

[<RequireQualifiedAccess>]
module Config =

  // ** parseTuple

  let private parseTuple (input: string) : Either<IrisError,int * int> =
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

  let private parseRect (str : string) : Either<IrisError,Rect> =
    parseTuple str
    |> Either.map Rect

  // ** parseCoordinate

  let private parseCoordinate (str : string) : Either<IrisError,Coordinate> =
    parseTuple str
    |> Either.map Coordinate

  // ** parseStringProp

  let parseStringProp (str : string) : string option =
    if str.Length > 0 then Some(str) else None

  // ** parseAudio

  //      _             _ _
  //     / \  _   _  __| (_) ___
  //    / _ \| | | |/ _` | |/ _ \
  //   / ___ \ |_| | (_| | | (_) |
  //  /_/   \_\__,_|\__,_|_|\___/

  /// ### Parse the Audio configuration section
  ///
  /// Parses the Audio configuration section of the passed-in configuration file.
  ///
  /// # Returns: AudioConfig
  let private parseAudio (config: ProjectYaml) : Either<IrisError, AudioConfig> =
    Either.tryWith (Error.asParseError "Config.parseAudio") <| fun _ ->
      { SampleRate = uint32 config.Project.Audio.SampleRate }

  // ** saveAudio

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let private saveAudio (file: ProjectYaml, config: IrisConfig) =
    file.Project.Audio.SampleRate <- int (config.AudioConfig.SampleRate)
    (file, config)

  // ** parseExe

  //  __     __
  //  \ \   / /_   ____   ____   __
  //   \ \ / /\ \ / /\ \ / /\ \ / /
  //    \ V /  \ V /  \ V /  \ V /
  //     \_/    \_/    \_/    \_/
  //

  let parseExe (exe: ExeYaml) : Either<IrisError, VvvvExe> =
    Right { Executable = exe.Path
            Version    = exe.Version
            Required   = exe.Required }

  // ** parseExes

  let parseExes exes : Either<IrisError, VvvvExe array> =
    either {
      let arr =
        exes
        |> Seq.length
        |> Array.zeroCreate

      let! exes =
        Seq.fold
          (fun (m: Either<IrisError,int * VvvvExe array>) exe -> either {
            let! (idx, exes) = m
            let! exe = parseExe exe
            exes.[idx] <- exe
            return (idx + 1, exes)
          })
          (Right(0, arr))
          exes
      return arr
    }

  // ** parsePlugin

  let parsePlugin (plugin: PluginYaml) : Either<IrisError, VvvvPlugin> =
    Right { Name = plugin.Name
            Path = plugin.Path }

  // ** parsePlugins

  let parsePlugins plugins : Either<IrisError, VvvvPlugin array> =
    either {
      let arr =
        plugins
        |> Seq.length
        |> Array.zeroCreate

      let! plugins =
        Seq.fold
          (fun (m: Either<IrisError,int * VvvvPlugin array>) plugin -> either {
            let! (idx, plugins) = m
            let! plugin = parsePlugin plugin
            plugins.[idx] <- plugin
            return (idx + 1, plugins)
          })
          (Right(0, arr))
          plugins
      return arr
    }

  // ** parseVvvv

  /// ### Parses the VVVV configuration
  ///
  /// Constructs the VVVV configuration values from the handed config file value.
  ///
  /// # Returns: VvvvConfig
  let private parseVvvv (config: ProjectYaml) : Either<IrisError, VvvvConfig> =
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
  let private saveVvvv (file: ProjectYaml, config: IrisConfig) =
    file.Project.VVVV.Executables.Clear() //

    for exe in config.VvvvConfig.Executables do
      let entry = new ExeYaml()
      entry.Path <- exe.Executable;
      entry.Version <- exe.Version;
      entry.Required <- exe.Required
      file.Project.VVVV.Executables.Add(entry)

    file.Project.VVVV.Plugins.Clear()

    for plug in config.VvvvConfig.Plugins do
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
  let private parseRaft (config: ProjectYaml) : Either<IrisError, RaftConfig> =
    either {
      let engine = config.Project.Engine

      let! loglevel = LogLevel.TryParse engine.LogLevel

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
  let private saveRaft (file: ProjectYaml, config: IrisConfig) =
    file.Project.Engine.RequestTimeout   <- int config.RaftConfig.RequestTimeout
    file.Project.Engine.ElectionTimeout  <- int config.RaftConfig.ElectionTimeout
    file.Project.Engine.MaxLogDepth      <- int config.RaftConfig.MaxLogDepth
    file.Project.Engine.LogLevel         <- string config.RaftConfig.LogLevel
    file.Project.Engine.DataDir          <- config.RaftConfig.DataDir
    file.Project.Engine.MaxRetries       <- int config.RaftConfig.MaxRetries
    file.Project.Engine.PeriodicInterval <- int config.RaftConfig.PeriodicInterval
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
  let private parseTiming (config: ProjectYaml) : Either<IrisError,TimingConfig> =
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
  let private saveTiming (file: ProjectYaml, config: IrisConfig) =
    file.Project.Timing.Framebase <- int (config.TimingConfig.Framebase)
    file.Project.Timing.Input     <- config.TimingConfig.Input

    file.Project.Timing.Servers.Clear()
    for srv in config.TimingConfig.Servers do
      file.Project.Timing.Servers.Add(string srv)

    file.Project.Timing.TCPPort <- int (config.TimingConfig.TCPPort)
    file.Project.Timing.UDPPort <- int (config.TimingConfig.UDPPort)

    (file, config)

  // ** parseViewPort

  //  __     ___               ____            _
  //  \ \   / (_) _____      _|  _ \ ___  _ __| |_
  //   \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
  //    \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
  //     \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

  let parseViewPort (viewport: ViewPortYaml) =
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
  let private parseViewPorts (config: ProjectYaml) : Either<IrisError,ViewPort array> =
    either {
      let arr =
        config.Project.ViewPorts
        |> Seq.length
        |> Array.zeroCreate

      let! viewports =
        Seq.fold
          (fun (m: Either<IrisError, int * ViewPort array>) vp -> either {
            let! (idx, viewports) = m
            let! viewport = parseViewPort vp
            viewports.[idx] <- viewport
            return (idx + 1, viewports)
          })
          (Right(0, arr))
          config.Project.ViewPorts

      return arr
    }

  // ** saveViewPorts

  /// ### Transfers the passed array of ViewPort values
  ///
  /// Adds a config section for each ViewPort value in the passed in Config to the configuration
  /// file.
  ///
  /// # Returns: ConfigFile
  let private saveViewPorts (file: ProjectYaml, config: IrisConfig) =
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
  let parseSignal (signal: SignalYaml) : Either<IrisError, Signal> =
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
  let parseSignals signals =
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
  let parseRegion (region: RegionYaml) : Either<IrisError, Region> =
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
  let parseRegions regions : Either<IrisError, Region array> =
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
  let parseDisplay (display: DisplayYaml) : Either<IrisError, Display> =
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
  let private parseDisplays (config: ProjectYaml) : Either<IrisError, Display array> =
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
  let private saveDisplays (file: ProjectYaml, config: IrisConfig) =
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
  let private parseArgument (argument: ArgumentYaml) =
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
  let parseArguments arguments =
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
  let parseTask (task: TaskYaml) : Either<IrisError, Task> =
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
  let private parseTasks (config: ProjectYaml) : Either<IrisError,Task array> =
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
  let private saveTasks (file: ProjectYaml, config: IrisConfig) =
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
  let private parseMember (mem: MemberYaml) : Either<IrisError, RaftMember> =
    either {
      let! ip = IpAddress.TryParse mem.Ip
      let! state = RaftMemberState.TryParse mem.State

      try
        return { Id         = Id mem.Id
                 HostName   = mem.HostName
                 IpAddr     = ip
                 Port       = uint16 mem.Port
                 WebPort    = uint16 mem.WebPort
                 WsPort     = uint16 mem.WsPort
                 GitPort    = uint16 mem.GitPort
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
  let parseMembers mems : Either<IrisError, RaftMember array> =
    either {
      let arr =
        mems
        |> Seq.length
        |> Array.zeroCreate

      let! (_,mems) =
        Seq.fold
          (fun (m: Either<IrisError, int * RaftMember array>) mem -> either {
            let! (idx, mems) = m
            let! mem = parseMember mem
            mems.[idx] <- mem
            return (idx + 1, mems)
          })
          (Right(0, arr))
          mems

      return mems
    }

  // ** parseGroup

  let private parseGroup (group: GroupYaml) : Either<IrisError, HostGroup> =
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

  let parseGroups groups : Either<IrisError, HostGroup array> =
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

  let private parseCluster (config: ProjectYaml) : Either<IrisError, Cluster> =
    either {
      let cluster = config.Project.Cluster

      let! groups = parseGroups cluster.Groups
      let! mems = parseMembers cluster.Members

      return { Name    = cluster.Name
               Members = mems
               Groups  = groups }
    }

  // ** saveCluster

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let saveCluster (file: ProjectYaml, config: IrisConfig) =
    file.Project.Cluster.Members.Clear()
    file.Project.Cluster.Groups.Clear()
    file.Project.Cluster.Name <- config.ClusterConfig.Name

    for mem in config.ClusterConfig.Members do
      let n = new MemberYaml()
      n.Id       <- string mem.Id
      n.Ip       <- string mem.IpAddr
      n.HostName <- mem.HostName
      n.Port     <- int mem.Port
      n.WebPort  <- int mem.WebPort
      n.WsPort   <- int mem.WsPort
      n.GitPort  <- int mem.GitPort
      n.State    <- string mem.State
      file.Project.Cluster.Members.Add(n)

    for group in config.ClusterConfig.Groups do
      let g = new GroupYaml()
      g.Name <- group.Name

      g.Members.Clear()

      for mem in group.Members do
        g.Members.Add(string mem)

      file.Project.Cluster.Groups.Add(g)
    (file, config)

  // ** fromFile

  let fromFile (file: ProjectYaml) (machine: IrisMachine) : Either<IrisError, IrisConfig> =
    either {
      let! raftcfg   = parseRaft      file
      let! timing    = parseTiming    file
      let! vvvv      = parseVvvv      file
      let! audio     = parseAudio     file
      let! viewports = parseViewPorts file
      let! displays  = parseDisplays  file
      let! tasks     = parseTasks     file
      let! cluster   = parseCluster   file

      return { MachineConfig = machine
               VvvvConfig    = vvvv
               AudioConfig   = audio
               RaftConfig    = raftcfg
               TimingConfig  = timing
               ViewPorts     = viewports
               Displays      = displays
               Tasks         = tasks
               ClusterConfig = cluster }
    }

  // ** toFile

  let toFile (config: IrisConfig) (file: ProjectYaml) =
    (file, config)
    |> saveVvvv
    |> saveAudio
    |> saveRaft
    |> saveTiming
    |> saveViewPorts
    |> saveDisplays
    |> saveTasks
    |> saveCluster
    |> fst

  // ** create

  let create (name: string) (machine: IrisMachine) =
    { MachineConfig  = machine
    ; VvvvConfig     = VvvvConfig.Default
    ; AudioConfig    = AudioConfig.Default
    ; RaftConfig     = RaftConfig.Default
    ; TimingConfig   = TimingConfig.Default
    ; ViewPorts      = [| |]
    ; Displays       = [| |]
    ; Tasks          = [| |]
    ; ClusterConfig  = { Name   = name + " cluster"
                       ; Members = [| |]
                       ; Groups  = [| |] } }

  // ** updateVvvv

  let updateVvvv (vvvv: VvvvConfig) (config: IrisConfig) =
    { config with VvvvConfig = vvvv }

  // ** updateAudio

  let updateAudio (audio: AudioConfig) (config: IrisConfig) =
    { config with AudioConfig = audio }

  // ** updateEngine

  let updateEngine (engine: RaftConfig) (config: IrisConfig) =
    { config with RaftConfig = engine }

  // ** updateTiming

  let updateTiming (timing: TimingConfig) (config: IrisConfig) =
    { config with TimingConfig = timing }

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

  let updateCluster (cluster: Cluster) (config: IrisConfig) =
    { config with ClusterConfig = cluster }

  // ** findMember

  let findMember (config: IrisConfig) (id: Id) =
    let result =
      Array.tryFind
        (fun (mem: RaftMember) -> mem.Id = id)
        config.ClusterConfig.Members

    match result with
    | Some mem -> Either.succeed mem
    | _ ->
      sprintf "Missing Node: %s" (string id)
      |> Error.asProjectError "Config.findMember"
      |> Either.fail

  // ** getMembers

  let getMembers (config: IrisConfig) : Either<IrisError,RaftMember array> =
    config.ClusterConfig.Members
    |> Either.succeed

  // ** setMembers

  let setMembers (mems: RaftMember array) (config: IrisConfig) =
    { config with
        ClusterConfig =
          { config.ClusterConfig with Members = mems } }

  // ** selfMember

  let selfMember (options: IrisConfig) =
    findMember options options.MachineConfig.MachineId

  // ** addMember

  let addMember (mem: RaftMember) (config: IrisConfig) =
    { config with
        ClusterConfig =
          { config.ClusterConfig with
              Members = Array.append [| mem |] config.ClusterConfig.Members
            } }

  // ** removeMember

  let removeMember (id: Id) (config: IrisConfig) =
    { config with
        ClusterConfig =
          { config.ClusterConfig with
              Members = Array.filter
                          (fun (mem: RaftMember) -> mem.Id = id)
                          config.ClusterConfig.Members } }

  // ** logLevel

  let logLevel (config: IrisConfig) =
    config.RaftConfig.LogLevel

  // ** setLogLevel

  let setLogLevel (level: LogLevel) (config: IrisConfig) =
    { config with
        RaftConfig =
          { config.RaftConfig with
              LogLevel = level } }

  // ** metadataPath

  let metadataPath (config: IrisConfig) =
    config.RaftConfig.DataDir </> RAFT_METADATA_FILENAME + ASSET_EXTENSION

  // ** logDataPath

  let logDataPath (config: IrisConfig) =
    config.RaftConfig.DataDir </> RAFT_LOGDATA_PATH
