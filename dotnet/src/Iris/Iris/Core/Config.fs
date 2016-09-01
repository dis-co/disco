namespace Iris.Core

open System
open System.IO
open Iris.Raft

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
  ; ElectionTimeout:  Long
  ; MaxLogDepth:      Long
  ; LogLevel:         LogLevel
  ; DataDir:          FilePath
  ; MaxRetries:       uint8
  ; PeriodicInterval: uint8
  ; BindAddress:      string
  }
  with
    static member Default =
      let guid = Guid.NewGuid()
      { RequestTimeout   = 500UL
      ; ElectionTimeout  = 6000UL
      ; MaxLogDepth      = 20UL
      ; MaxRetries       = 10uy
      ; PeriodicInterval = 50uy
      ; LogLevel         = Err
      ; DataDir          = Path.Combine(Path.GetTempPath(), guid.ToString())
      ; BindAddress      = "127.0.0.1"
      }

// __     __                     ____             __ _
// \ \   / /_   ____   ____   __/ ___|___  _ __  / _(_) __ _
//  \ \ / /\ \ / /\ \ / /\ \ / / |   / _ \| '_ \| |_| |/ _` |
//   \ V /  \ V /  \ V /  \ V /| |__| (_) | | | |  _| | (_| |
//    \_/    \_/    \_/    \_/  \____\___/|_| |_|_| |_|\__, |
//                                                     |___/

type VvvvConfig =
  { Executables : VvvvExe list
  ; Plugins     : VvvvPlugin list }
  with
    static member Default =
      { Executables = List.empty
      ; Plugins     = List.empty }

//  ____            _    ____             __ _
// |  _ \ ___  _ __| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _ \| '__| __| |   / _ \| '_ \| |_| |/ _` |
// |  __/ (_) | |  | |_| |__| (_) | | | |  _| | (_| |
// |_|   \___/|_|   \__|\____\___/|_| |_|_| |_|\__, |
//                                             |___/

type PortConfig =
  { WebSocket : uint32
  ; UDPCue    : uint32
  ; Raft      : uint32
  ; Http      : uint32
  }
  with
    static member Default =
      { WebSocket = 8081u
      ; UDPCue    = 8075u
      ; Raft      = 9090u
      ; Http      = 8080u
      }

//  _____ _           _              ____             __ _
// |_   _(_)_ __ ___ (_)_ __   __ _ / ___|___  _ __  / _(_) __ _
//   | | | | '_ ` _ \| | '_ \ / _` | |   / _ \| '_ \| |_| |/ _` |
//   | | | | | | | | | | | | | (_| | |__| (_) | | | |  _| | (_| |
//   |_| |_|_| |_| |_|_|_| |_|\__, |\____\___/|_| |_|_| |_|\__, |
//                            |___/                        |___/

type TimingConfig =
  { Framebase : uint32
  ; Input     : string
  ; Servers   : IpAddress list
  ; UDPPort   : uint32
  ; TCPPort   : uint32
  }
  with
    static member Default =
      { Framebase = 50u
      ; Input     = "Iris Freerun"
      ; Servers   = List.empty
      ; UDPPort   = 8071u
      ; TCPPort   = 8072u
      }

//     _             _ _        ____             __ _
//    / \  _   _  __| (_) ___  / ___|___  _ __  / _(_) __ _
//   / _ \| | | |/ _` | |/ _ \| |   / _ \| '_ \| |_| |/ _` |
//  / ___ \ |_| | (_| | | (_) | |__| (_) | | | |  _| | (_| |
// /_/   \_\__,_|\__,_|_|\___/ \____\___/|_| |_|_| |_|\__, |
//                                                    |___/

type AudioConfig =
  { SampleRate : uint32 }
  with
    static member Default =
      { SampleRate = 48000u }

//  _   _           _    ____
// | | | | ___  ___| |_ / ___|_ __ ___  _   _ _ __
// | |_| |/ _ \/ __| __| |  _| '__/ _ \| | | | '_ \
// |  _  | (_) \__ \ |_| |_| | | | (_) | |_| | |_) |
// |_| |_|\___/|___/\__|\____|_|  \___/ \__,_| .__/
//                                           |_|

type HostGroup =
  { Name    : Name
  ; Members : Id list
  }
  with
    override self.ToString() =
      sprintf "HostGroup:
                Name: %A
                Members: %A"
              self.Name
              (List.fold (fun m s -> m + " " + string s) "" self.Members)

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type Cluster =
  { Name   : Name
  ; Nodes  : RaftNode  list
  ; Groups : HostGroup list
  }
  with
    override self.ToString() =
      sprintf "Cluster:
                Name: %A
                Nodes: %A
                Groups: %A"
              self.Name
              self.Nodes
              self.Groups

//   ____             __ _
//  / ___|___  _ __  / _(_) __ _
// | |   / _ \| '_ \| |_| |/ _` |
// | |__| (_) | | | |  _| | (_| |
//  \____\___/|_| |_|_| |_|\__, |
//                         |___/

type Config =
  { AudioConfig    : AudioConfig
  ; VvvvConfig     : VvvvConfig
  ; RaftConfig     : RaftConfig
  ; TimingConfig   : TimingConfig
  ; PortConfig     : PortConfig
  ; ClusterConfig  : Cluster
  ; ViewPorts      : ViewPort list
  ; Displays       : Display  list
  ; Tasks          : Task     list }

//  _   _      _
// | | | | ___| |_ __   ___ _ __ ___
// | |_| |/ _ \ | '_ \ / _ \ '__/ __|
// |  _  |  __/ | |_) |  __/ |  \__ \
// |_| |_|\___|_| .__/ \___|_|  |___/
//              |_|

[<AutoOpen>]
module Configuration =

  let private parseTuple (input: string) : (int * int) =
    input.Split [| '('; ','; ' '; ')' |]       // split the string according to the specified chars
    |> Array.filter (String.length >> ((<) 0)) // filter out elements that have zero length
    |> function
      | [| x; y |] -> (int x, int y)
      | _        -> failwithf "failed to parse tuple: %s" input

  let private parseRect (str : string) : Rect =
    parseTuple str |> Rect

  let private parseCoordinate (str : string) : Coordinate =
    parseTuple str |> Coordinate

  let parseStringProp (str : string) : string option =
    if str.Length > 0 then Some(str) else None

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
  let private parseAudio (cfg : ConfigFile)  : AudioConfig =
    { SampleRate = uint32 cfg.Project.Audio.SampleRate }

  /// ### Save the AudioConfig value
  ///
  /// Transfer the configuration from `AudioConfig` values to a given config file.
  ///
  /// # Returns: ConfigFile
  let private saveAudio (file: ConfigFile, config: Config) =
    file.Project.Audio.SampleRate <- int (config.AudioConfig.SampleRate)
    (file, config)

  //  __     __
  //  \ \   / /_   ____   ____   __
  //   \ \ / /\ \ / /\ \ / /\ \ / /
  //    \ V /  \ V /  \ V /  \ V /
  //     \_/    \_/    \_/    \_/
  //

  /// ### Parses the VVVV configuration
  ///
  /// Constructs the VVVV configuration values from the handed config file value.
  ///
  /// # Returns: VvvvConfig
  let private parseVvvv (cfg : ConfigFile) : VvvvConfig =
    let ctoe (i : ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type) =
      { Executable = i.Path
      ; Version    = i.Version
      ; Required   = i.Required
      }

    let ctop (i : ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type) =
      { Name = i.Name
      ; Path = i.Path
      }

    let exes  : VvvvExe list ref = ref []
    let plugs : VvvvPlugin list ref = ref []

    for exe in cfg.Project.VVVV.Executables do
      exes := ((ctoe exe) :: !exes)

    for plg in cfg.Project.VVVV.Plugins do
      plugs := ((ctop plg) :: !plugs)

    { Executables = List.reverse !exes
    ; Plugins     = List.reverse !plugs }

  /// ### Save the VVVV configuration
  ///
  /// Translate the values from Config into the passed in configuration file.
  ///
  /// # Returns: ConfigFile
  let private saveVvvv (file: ConfigFile, config: Config) =
    file.Project.VVVV.Executables.Clear()
    for exe in config.VvvvConfig.Executables do
      let entry = new ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type()
      entry.Path <- exe.Executable;
      entry.Version <- exe.Version;
      entry.Required <- exe.Required
      file.Project.VVVV.Executables.Add(entry)

    file.Project.VVVV.Plugins.Clear()
    for plug in config.VvvvConfig.Plugins do
      let entry = new ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type ()
      entry.Name <- plug.Name
      entry.Path <- plug.Path
      file.Project.VVVV.Plugins.Add(entry)

    (file, config)

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
  let private parseRaft (cfg : ConfigFile) : RaftConfig =
    // let eng = cfg.Project.Engine
    { RequestTimeout   = uint64 cfg.Project.Engine.RequestTimeout
    ; ElectionTimeout  = uint64 cfg.Project.Engine.ElectionTimeout
    ; MaxLogDepth      = uint64 cfg.Project.Engine.MaxLogDepth
    ; LogLevel         = LogLevel.Parse cfg.Project.Engine.LogLevel
    ; DataDir          = cfg.Project.Engine.DataDir
    ; MaxRetries       = uint8 cfg.Project.Engine.MaxRetries
    ; PeriodicInterval = uint8 cfg.Project.Engine.PeriodicInterval
    ; BindAddress      = cfg.Project.Engine.BindAddress
    }


  /// ### Save the passed RaftConfig to the configuration file
  ///
  /// Save Raft algorithm specific configuration options to the configuration file object.
  ///
  /// # Returns: ConfigFile
  let private saveRaft (file: ConfigFile, config: Config) =
    file.Project.Engine.RequestTimeout   <- int config.RaftConfig.RequestTimeout
    file.Project.Engine.ElectionTimeout  <- int config.RaftConfig.ElectionTimeout
    file.Project.Engine.MaxLogDepth      <- int config.RaftConfig.MaxLogDepth
    file.Project.Engine.LogLevel         <- string config.RaftConfig.LogLevel
    file.Project.Engine.DataDir          <- config.RaftConfig.DataDir
    file.Project.Engine.MaxRetries       <- int config.RaftConfig.MaxRetries
    file.Project.Engine.PeriodicInterval <- int config.RaftConfig.PeriodicInterval
    file.Project.Engine.BindAddress      <- config.RaftConfig.BindAddress
    (file, config)

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
  let private parseTiming (cnf : ConfigFile) : TimingConfig =
    let servers : IpAddress list ref = ref []

    for server in cnf.Project.Timing.Servers do
      servers := (IpAddress.Parse server :: !servers)

    { Framebase = uint32 cnf.Project.Timing.Framebase
    ; Input     = cnf.Project.Timing.Input
    ; Servers   = List.reverse !servers
    ; UDPPort   = uint32 cnf.Project.Timing.UDPPort
    ; TCPPort   = uint32 cnf.Project.Timing.TCPPort }


  /// ### Transfer the TimingConfig options to the passed configuration file
  ///
  ///
  ///
  /// # Returns: ConfigFile
  let private saveTiming (file: ConfigFile, config: Config) =
    file.Project.Timing.Framebase <- int (config.TimingConfig.Framebase)
    file.Project.Timing.Input     <- config.TimingConfig.Input

    file.Project.Timing.Servers.Clear()
    for srv in config.TimingConfig.Servers do
      file.Project.Timing.Servers.Add(string srv)

    file.Project.Timing.TCPPort <- int (config.TimingConfig.TCPPort)
    file.Project.Timing.UDPPort <- int (config.TimingConfig.UDPPort)

    (file, config)

  //   ____            _
  //  |  _ \ ___  _ __| |_
  //  | |_) / _ \| '__| __|
  //  |  __/ (_) | |  | |_
  //  |_|   \___/|_|   \__|

  /// ### Parse the Port configuration
  ///
  /// Parse the port configuration in a given config file into a `PortConfig` value.
  ///
  /// # Returns: PortConfig
  let private parsePort (cnf : ConfigFile) : PortConfig =
    { WebSocket = uint32 cnf.Project.Ports.WebSocket
    ; UDPCue    = uint32 cnf.Project.Ports.UDPCues
    ; Raft      = uint32 cnf.Project.Ports.Raft
    ; Http      = uint32 cnf.Project.Ports.Http
    }

  /// ### Transfer the PortConfig configuration
  ///
  /// Save all values in the PortConfig to the passed configuration file instance.
  ///
  /// # Returns: ConfigFile
  let private savePort (file: ConfigFile, config: Config) =
    file.Project.Ports.Raft        <- int (config.PortConfig.Raft)
    file.Project.Ports.Http        <- int (config.PortConfig.Http)
    file.Project.Ports.UDPCues     <- int (config.PortConfig.UDPCue)
    file.Project.Ports.WebSocket   <- int (config.PortConfig.WebSocket)
    (file, config)

  //  __     ___               ____            _
  //  \ \   / (_) _____      _|  _ \ ___  _ __| |_
  //   \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
  //    \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
  //     \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|

  /// ### Parse all Viewport configs listed in a config file
  ///
  /// Parses the ViewPort config section and returns a list of `ViewPort` values.
  ///
  /// # Returns: ViewPort list
  let private parseViewPorts (cnf : ConfigFile) : ViewPort list =
    let vports : ViewPort list ref = ref []

    for vp in cnf.Project.ViewPorts do
      let viewport' =
        { Id             = Id.Parse vp.Id
        ; Name           = vp.Name
        ; Position       = parseCoordinate vp.Position
        ; Size           = parseRect       vp.Size
        ; OutputPosition = parseCoordinate vp.OutputPosition
        ; OutputSize     = parseRect       vp.OutputSize
        ; Overlap        = parseRect       vp.Overlap
        ; Description    = vp.Description }

      vports := (viewport' :: !vports)

    List.reverse !vports

  /// ### Transfers the passed list of ViewPort values
  ///
  /// Adds a config section for each ViewPort value in the passed in Config to the configuration
  /// file.
  ///
  /// # Returns: ConfigFile
  let private saveViewPorts (file: ConfigFile, config: Config) =
    file.Project.ViewPorts.Clear()
    for vp in config.ViewPorts do
      let item = new ConfigFile.Project_Type.ViewPorts_Item_Type()
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

  //   ____  _           _
  //  |  _ \(_)___ _ __ | | __ _ _   _ ___
  //  | | | | / __| '_ \| |/ _` | | | / __|
  //  | |_| | \__ \ |_) | | (_| | |_| \__ \
  //  |____/|_|___/ .__/|_|\__,_|\__, |___/
  //              |_|            |___/

  /// ### Parse the Display section of a configuration file
  ///
  /// Construct a list of `Display` values from the given configuration file.
  ///
  /// # Returns: Display list
  let private parseDisplays (cnf : ConfigFile) : Display list =
    let displays : Display list ref = ref []

    for display in cnf.Project.Displays do

      /// scrape all signal defs out of the config
      let signals : Signal list ref = ref []
      for signal in display.Signals do
        let signal' : Signal =
          { Size     = parseRect       signal.Size
          ; Position = parseCoordinate signal.Position }
        signals := (signal' :: !signals)

      let regions : Region list ref = ref []
      for region in display.RegionMap.Regions do
        let region' =
          { Id             = Id.Parse region.Id
          ; Name           = region.Name
          ; SrcPosition    = parseCoordinate region.SrcPosition
          ; SrcSize        = parseRect       region.SrcSize
          ; OutputPosition = parseCoordinate region.OutputPosition
          ; OutputSize     = parseRect       region.OutputSize }
        regions := (region' :: !regions)

      let display' =
        { Id        = Id.Parse display.Id
        ; Name      = display.Name
        ; Size      = parseRect display.Size
        ; Signals   = List.reverse !signals
        ; RegionMap =
          { SrcViewportId = Id.Parse display.RegionMap.SrcViewportId
          ; Regions       = List.reverse !regions }
        }
      displays := (display' :: !displays)

    List.reverse !displays

  /// ### Transfer the Display config to a configuration file
  ///
  /// Save all `Display` values in `Config` to the passed configuration file.
  ///
  /// # Returns: ConfigFile
  let private saveDisplays (file: ConfigFile, config: Config) =
    file.Project.Displays.Clear()
    for dp in config.Displays do
      let item = new ConfigFile.Project_Type.Displays_Item_Type()
      item.Id <- string dp.Id
      item.Name <- dp.Name
      item.Size <- dp.Size.ToString()

      item.RegionMap.SrcViewportId <- string dp.RegionMap.SrcViewportId
      item.RegionMap.Regions.Clear()

      for region in dp.RegionMap.Regions do
        let r = new ConfigFile.Project_Type.Displays_Item_Type.RegionMap_Type.Regions_Item_Type()
        r.Id <- string region.Id
        r.Name <- region.Name
        r.OutputPosition <- region.OutputPosition.ToString()
        r.OutputSize <- region.OutputSize.ToString()
        r.SrcPosition <- region.SrcPosition.ToString()
        r.SrcSize <- region.SrcSize.ToString()
        item.RegionMap.Regions.Add(r)

      item.Signals.Clear()

      for signal in dp.Signals do
        let s = new ConfigFile.Project_Type.Displays_Item_Type.Signals_Item_Type()
        s.Position <- signal.Position.ToString()
        s.Size <- signal.Size.ToString()
        item.Signals.Add(s)

      file.Project.Displays.Add(item)
    (file, config)

  //   _____         _
  //  |_   _|_ _ ___| | _____
  //    | |/ _` / __| |/ / __|
  //    | | (_| \__ \   <\__ \
  //    |_|\__,_|___/_|\_\___/
  //

  /// ### Parse Task configuration section
  ///
  /// Create `Task` values for each entry in the Task config section.
  ///
  /// # Returns: Task list
  let private parseTasks (cfg : ConfigFile) : Task list =
    let tasks : Task list ref = ref []

    for task in cfg.Project.Tasks do
      let arguments : Argument list ref = ref []

      for argument in task.Arguments do
        if (argument.Key.Length > 0) && (argument.Value.Length > 0)
        then arguments := ((argument.Key, argument.Value) :: !arguments)

      let task' =
        { Id          = Id.Parse task.Id
        ; Description = task.Description
        ; DisplayId   = Id.Parse task.DisplayId
        ; AudioStream = task.AudioStream
        ; Arguments   = !arguments
        }
      tasks := (task' :: !tasks)

    List.reverse !tasks

  /// ### Save the Tasks to a config file
  ///
  /// Transfers all `Task` values into the configuration file.
  ///
  /// # Returns: ConfigFile
  let private saveTasks (file: ConfigFile, config: Config) =
    file.Project.Tasks.Clear()
    for task in config.Tasks do
      let t = new ConfigFile.Project_Type.Tasks_Item_Type()
      t.Id <- string task.Id
      t.AudioStream <- task.AudioStream
      t.Description <- task.Description
      t.DisplayId   <- string task.DisplayId

      for arg in task.Arguments do
        let a = new ConfigFile.Project_Type.Tasks_Item_Type.Arguments_Item_Type()
        a.Key <- fst arg
        a.Value <- snd arg
        t.Arguments.Add(a)

      file.Project.Tasks.Add(t)
    (file, config)

  //    ____ _           _
  //   / ___| |_   _ ___| |_ ___ _ __
  //  | |   | | | | / __| __/ _ \ '__|
  //  | |___| | |_| \__ \ ||  __/ |
  //   \____|_|\__,_|___/\__\___|_|
  //

  /// ### Parse the Cluster configuration section
  ///
  /// Parse the cluster configuration section of a given configuration file into a `Cluster` value.
  ///
  /// # Returns: Cluster
  let private parseCluster (cfg : ConfigFile) : Cluster =
    let nodes  : RaftNode  list ref = ref []
    let groups : HostGroup list ref = ref []

    for node in cfg.Project.Cluster.Nodes do
      let node' : RaftNode =
        { Id         = Id.Parse node.Id
        ; HostName   = node.HostName
        ; IpAddr     = IpAddress.Parse node.Ip
        ; Port       = uint16 node.Port
        ; State      = RaftNodeState.Parse node.State
        ; Voting     = true
        ; VotedForMe = false
        ; NextIndex  = 1UL
        ; MatchIndex = 0UL
        }
      nodes := (node' :: !nodes)

    for group in cfg.Project.Cluster.Groups do
      if group.Name.Length > 0
      then
        let ids : Id list ref = ref []

        for mid in group.Members do
          if mid.Length > 0
          then ids := (Id.Parse mid :: !ids)

        let group' =
          { Name    = group.Name
          ; Members = !ids
          }
        groups := (group' :: !groups)

    { Name   = cfg.Project.Cluster.Name
    ; Nodes  = List.reverse !nodes
    ; Groups = List.reverse !groups }

  /// ### Save a Cluster value to a configuration file
  ///
  /// Saves the passed `Cluster` value to the passed config file.
  ///
  /// # Returns: ConfigFile
  let saveCluster (file: ConfigFile, config: Config) =
    file.Project.Cluster.Nodes.Clear()
    file.Project.Cluster.Groups.Clear()
    file.Project.Cluster.Name <- config.ClusterConfig.Name

    for node in config.ClusterConfig.Nodes do
      let n = new ConfigFile.Project_Type.Cluster_Type.Nodes_Item_Type()
      n.Id       <- string node.Id
      n.Ip       <- string node.IpAddr
      n.HostName <- node.HostName
      n.Port     <- int node.Port
      n.State    <- string node.State
      file.Project.Cluster.Nodes.Add(n)

    for group in config.ClusterConfig.Groups do
      let g = new ConfigFile.Project_Type.Cluster_Type.Groups_Item_Type()
      g.Name <- group.Name

      for mem in group.Members do
        g.Members.Add(string mem)

      file.Project.Cluster.Groups.Add(g)
    (file, config)

  let fromFile (file: ConfigFile) =
    { VvvvConfig     = parseVvvv      file
    ; AudioConfig    = parseAudio     file
    ; RaftConfig     = parseRaft      file
    ; TimingConfig   = parseTiming    file
    ; PortConfig     = parsePort      file
    ; ViewPorts      = parseViewPorts file
    ; Displays       = parseDisplays  file
    ; Tasks          = parseTasks     file
    ; ClusterConfig  = parseCluster   file  }

  let toFile (config: Config) (file: ConfigFile) =
    (file, config)
    |> saveVvvv
    |> saveAudio
    |> saveRaft
    |> saveTiming
    |> savePort
    |> saveViewPorts
    |> saveDisplays
    |> saveTasks
    |> saveCluster
    |> fst

  let create (name: string) =
    { VvvvConfig     = VvvvConfig.Default
    ; AudioConfig    = AudioConfig.Default
    ; RaftConfig     = RaftConfig.Default
    ; TimingConfig   = TimingConfig.Default
    ; PortConfig     = PortConfig.Default
    ; ViewPorts      = []
    ; Displays       = []
    ; Tasks          = []
    ; ClusterConfig  = { Name   = name + " cluster"
                       ; Nodes  = []
                       ; Groups = [] } }

  let updateVvvv (vvvv: VvvvConfig) (config: Config) =
    { config with VvvvConfig = vvvv }

  let updateAudio (audio: AudioConfig) (config: Config) =
    { config with AudioConfig = audio }

  let updateEngine (engine: RaftConfig) (config: Config) =
    { config with RaftConfig = engine }

  let updateTiming (timing: TimingConfig) (config: Config) =
    { config with TimingConfig = timing }

  let updatePorts (ports: PortConfig) (config: Config)=
    { config with PortConfig = ports }

  let updateViewPorts (viewports: ViewPort list) (config: Config) =
    { config with ViewPorts = viewports }

  let updateDisplays (displays: Display list) (config: Config) =
    { config with Displays = displays }

  let updateTasks (tasks: Task list) (config: Config) =
    { config with Tasks = tasks }

  let updateCluster (cluster: Cluster) (config: Config) =
    { config with ClusterConfig = cluster }

  //  __  __                _
  // |  \/  | ___ _ __ ___ | |__   ___ _ __ ___
  // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
  // | |  | |  __/ | | | | | |_) |  __/ |  \__ \
  // |_|  |_|\___|_| |_| |_|_.__/ \___|_|  |___/

  type Config with

    static member Create (name: string) : Config = create name

    static member FromFile (file: ConfigFile) : Config = fromFile file

    static member ToFile (file: ConfigFile) (config: Config) = toFile config file
