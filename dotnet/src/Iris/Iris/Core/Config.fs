namespace Iris.Core


open System


//  ____        __ _    ____             __ _
// |  _ \ __ _ / _| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _` | |_| __| |   / _ \| '_ \| |_| |/ _` |
// |  _ < (_| |  _| |_| |__| (_) | | | |  _| | (_| |
// |_| \_\__,_|_|  \__|\____\___/|_| |_|_| |_|\__, |
//                                            |___/

type RaftConfig =
  { RequestTimeout : uint32
  ; TempDir : string
  }
  with
    static member Default =
      { RequestTimeout = 1000u
      ; TempDir        = "ohai"
      }
// __     __                     ____             __ _
// \ \   / /_   ____   ____   __/ ___|___  _ __  / _(_) __ _
//  \ \ / /\ \ / /\ \ / /\ \ / / |   / _ \| '_ \| |_| |/ _` |
//   \ V /  \ V /  \ V /  \ V /| |__| (_) | | | |  _| | (_| |
//    \_/    \_/    \_/    \_/  \____\___/|_| |_|_| |_|\__, |
//                                                     |___/

type VvvvConfig =
  { Executables : VvvvExe list
  ; Plugins     : VvvvPlugin list
  }
  with
    static member Default =
      { Executables = List.empty
      ; Plugins     = List.empty
      }

//  ____            _    ____             __ _
// |  _ \ ___  _ __| |_ / ___|___  _ __  / _(_) __ _
// | |_) / _ \| '__| __| |   / _ \| '_ \| |_| |/ _` |
// |  __/ (_) | |  | |_| |__| (_) | | | |  _| | (_| |
// |_|   \___/|_|   \__|\____\___/|_| |_|_| |_|\__, |
//                                             |___/

type PortConfig =
  { WebSocket : uint32
  ; UDPCue    : uint32
  ; Iris      : uint32
  }
  with
    static member Default =
      { WebSocket = 8080u
      ; UDPCue    = 8075u
      ; Iris      = 9090u
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
  ; Servers   : IP list
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

//  _   _           _       ____             __ _
// | \ | | ___   __| | ___ / ___|___  _ __  / _(_) __ _
// |  \| |/ _ \ / _` |/ _ \ |   / _ \| '_ \| |_| |/ _` |
// | |\  | (_) | (_| |  __/ |__| (_) | | | |  _| | (_| |
// |_| \_|\___/ \__,_|\___|\____\___/|_| |_|_| |_|\__, |
//                                                |___/

type NodeConfig =
  { Id       : Id
  ; HostName : Name
  ; Ip       : IP
  ; Task     : Id
  }
  with
    override self.ToString() =
      sprintf "NodeConfig:
                Id: %A
                HostName: %A
                Ip: %A
                Task: %A"
              self.Id
              self.HostName
              self.Ip
              self.Task

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
              (List.fold (fun m s -> m + " " + s) "" self.Members)

//   ____ _           _
//  / ___| |_   _ ___| |_ ___ _ __
// | |   | | | | / __| __/ _ \ '__|
// | |___| | |_| \__ \ ||  __/ |
//  \____|_|\__,_|___/\__\___|_|

type Cluster =
  { Name   : Name
  ; Nodes  : NodeConfig list
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

type Config () =

  [<DefaultValue>] val mutable AudioConfig    : AudioConfig   
  [<DefaultValue>] val mutable VvvvConfig     : VvvvConfig    
  [<DefaultValue>] val mutable RaftConfig     : RaftConfig    
  [<DefaultValue>] val mutable TimingConfig   : TimingConfig  
  [<DefaultValue>] val mutable PortConfig     : PortConfig    
  [<DefaultValue>] val mutable ClusterConfig  : Cluster       
  [<DefaultValue>] val mutable ViewPorts      : ViewPort list 
  [<DefaultValue>] val mutable Displays       : Display  list 
  [<DefaultValue>] val mutable Tasks          : Task     list 
   
  //  _   _      _
  // | | | | ___| |_ __   ___ _ __ ___
  // | |_| |/ _ \ | '_ \ / _ \ '__/ __|
  // |  _  |  __/ | |_) |  __/ |  \__ \
  // |_| |_|\___|_| .__/ \___|_|  |___/
  //              |_|

  static member ParseTuple (s : string) : (int * int) =
    let nonEmpty (s : string) : bool = s.Length > 0
    let parsed =
      s.Split([| '('; ','; ' '; ')' |])
      |> Array.filter nonEmpty
    (int parsed.[0], int parsed.[1])

  static member ParseRect (str : string) : Rect =
    Config.ParseTuple str |> Rect

  static member ParseCoordinate (str : string) : Coordinate =
    Config.ParseTuple str |> Coordinate

  static member ParseStringProp (str : string) : string option =
    if str.Length > 0 then Some(str) else None

  //      _             _ _
  //     / \  _   _  __| (_) ___
  //    / _ \| | | |/ _` | |/ _ \
  //   / ___ \ |_| | (_| | | (_) |
  //  /_/   \_\__,_|\__,_|_|\___/
  //
  /// Parse Audio Configuration Section
  static member ParseAudio (cfg : ConfigFile)  : AudioConfig =
    { SampleRate = uint32 cfg.Project.Audio.SampleRate }

  //  __     __
  //  \ \   / /_   ____   ____   __
  //   \ \ / /\ \ / /\ \ / /\ \ / /
  //    \ V /  \ V /  \ V /  \ V /
  //     \_/    \_/    \_/    \_/
  //
  /// Parse VVvV configuration section
  static member ParseVvvv (cfg : ConfigFile) : VvvvConfig =
    let ctoe (i : ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type) =
      { Executable = i.Path
      ; Version    = i.Version
      ; Required   = i.Required
      }

    let ctop (i : ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type) =
      { Name = i.Name
      ; Path = i.Path
      }

    let exes  : VvvvExe list ref = ref List.empty
    let plugs : VvvvPlugin list ref = ref List.empty

    for exe in cfg.Project.VVVV.Executables do
      exes := ((ctoe exe) :: !exes)

    for plg in cfg.Project.VVVV.Plugins do
      plugs := ((ctop plg) :: !plugs)

    { Executables = List.reverse !exes
    ; Plugins     = List.reverse !plugs }

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// Parse Vsync configuration section
  static member ParseRaft (cfg : ConfigFile) : RaftConfig =
    // let eng = cfg.Project.Engine

    let ifaces : string list option ref = ref None
    let hosts  : string list option ref = ref None

    for host in cfg.Project.Engine.Hosts do
      if host.Length > 0 
      then
        let hosts' = !hosts
        match hosts' with
          | Some(list) -> hosts := Some(host :: list)
          | _          -> hosts := Some([ host ])

    match !hosts with
      | Some(list) -> hosts := Some(List.reverse list)
      | None       -> hosts := None

    for iface in cfg.Project.Engine.NetworkInterfaces do
      let ifaces' = !ifaces
      match ifaces' with
        | Some(list) -> ifaces := Some(iface :: list)
        | _          -> ifaces := Some([ iface ])

    match !ifaces with
      | Some(list) -> ifaces := Some(List.reverse list)
      | None       -> ifaces := None

    { RequestTimeout = 0u
    ; TempDir = "hahhah" }

  //   _____ _           _
  //  |_   _(_)_ __ ___ (_)_ __   __ _
  //    | | | | '_ ` _ \| | '_ \ / _` |
  //    | | | | | | | | | | | | | (_| |
  //    |_| |_|_| |_| |_|_|_| |_|\__, |
  //                             |___/
  //
  /// Parse Timing Configuration Section
  static member ParseTiming (cnf : ConfigFile) : TimingConfig =
    let servers : string list ref = ref List.empty

    for server in cnf.Project.Timing.Servers do
      servers := (server :: !servers)

    { Framebase = uint32 cnf.Project.Timing.Framebase
    ; Input     = cnf.Project.Timing.Input
    ; Servers   = List.reverse !servers
    ; UDPPort   = uint32 cnf.Project.Timing.UDPPort
    ; TCPPort   = uint32 cnf.Project.Timing.TCPPort }

  //   ____            _
  //  |  _ \ ___  _ __| |_
  //  | |_) / _ \| '__| __|
  //  |  __/ (_) | |  | |_
  //  |_|   \___/|_|   \__|
  //
  /// Parse Port Configuration Section
  static member ParsePort (cnf : ConfigFile) : PortConfig =
    { WebSocket = uint32 cnf.Project.Ports.WebSocket
    ; UDPCue    = uint32 cnf.Project.Ports.UDPCues
    ; Iris      = uint32 cnf.Project.Ports.IrisService }

  //  __     ___               ____            _
  //  \ \   / (_) _____      _|  _ \ ___  _ __| |_
  //   \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
  //    \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
  //     \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|
  //
  /// Parse ViewPort Configuration Section
  static member ParseViewports (cnf : ConfigFile) : ViewPort list =
    let vports : ViewPort list ref = ref List.empty

    for vp in cnf.Project.ViewPorts do
      let viewport' =
        { Id             = vp.Id
        ; Name           = vp.Name
        ; Position       = Config.ParseCoordinate vp.Position
        ; Size           = Config.ParseRect       vp.Size
        ; OutputPosition = Config.ParseCoordinate vp.OutputPosition
        ; OutputSize     = Config.ParseRect       vp.OutputSize
        ; Overlap        = Config.ParseRect       vp.Overlap
        ; Description    = vp.Description }

      vports := (viewport' :: !vports)

    List.reverse !vports

  //   ____  _           _
  //  |  _ \(_)___ _ __ | | __ _ _   _ ___
  //  | | | | / __| '_ \| |/ _` | | | / __|
  //  | |_| | \__ \ |_) | | (_| | |_| \__ \
  //  |____/|_|___/ .__/|_|\__,_|\__, |___/
  //              |_|            |___/
  /// Parse Displays Configuration Section
  static member ParseDisplays (cnf : ConfigFile) : Display list =
    let displays : Display list ref = ref List.empty

    for display in cnf.Project.Displays do

      /// scrape all signal defs out of the config
      let signals : Signal list ref = ref List.empty
      for signal in display.Signals do
        let signal' : Signal =
          { Size     = Config.ParseRect signal.Size
          ; Position = Config.ParseCoordinate signal.Position }
        signals := (signal' :: !signals)

      let regions : Region list ref = ref List.empty

      for region in display.RegionMap.Regions do
        let region' =
          { Id             = region.Id
          ; Name           = region.Name
          ; SrcPosition    = Config.ParseCoordinate region.SrcPosition
          ; SrcSize        = Config.ParseRect region.SrcSize
          ; OutputPosition = Config.ParseCoordinate region.OutputPosition
          ; OutputSize     = Config.ParseRect region.OutputSize
          }
        regions := (region' :: !regions)

      let display' =
        { Id        = display.Id
        ; Name      = display.Name
        ; Size      = Config.ParseRect display.Size
        ; Signals   = List.reverse !signals
        ; RegionMap =
          { SrcViewportId = display.RegionMap.SrcViewportId
          ; Regions       = List.reverse !regions }
        }
      displays := (display' :: !displays)

    List.reverse !displays

  //   _____         _
  //  |_   _|_ _ ___| | _____
  //    | |/ _` / __| |/ / __|
  //    | | (_| \__ \   <\__ \
  //    |_|\__,_|___/_|\_\___/
  //
  /// Parse Task Configuration Section
  static member ParseTasks (cfg : ConfigFile) : Task list =
    let tasks : Task list ref = ref List.empty

    for task in cfg.Project.Tasks do
      let arguments : Argument list ref = ref List.empty

      for argument in task.Arguments do
        if (argument.Key.Length > 0) && (argument.Value.Length > 0)
        then arguments := ((argument.Key, argument.Value) :: !arguments)

      let task' =
        { Id          = task.Id
        ; Description = task.Description
        ; DisplayId   = task.DisplayId
        ; AudioStream = task.AudioStream
        ; Arguments   = !arguments
        }
      tasks := (task' :: !tasks)

    List.reverse !tasks

  //    ____ _           _
  //   / ___| |_   _ ___| |_ ___ _ __
  //  | |   | | | | / __| __/ _ \ '__|
  //  | |___| | |_| \__ \ ||  __/ |
  //   \____|_|\__,_|___/\__\___|_|
  //
  /// Parse Cluster Configuration Section
  static member ParseCluster (cfg : ConfigFile) : Cluster =
    let nodes  : NodeConfig list ref = ref List.empty
    let groups : HostGroup list ref = ref List.empty

    for node in cfg.Project.Cluster.Nodes do
      let node' =
        { Id       = node.Id
        ; HostName = node.HostName
        ; Ip       = node.Ip
        ; Task     = node.Task
        }
      nodes := (node' :: !nodes)

    for group in cfg.Project.Cluster.Groups do
      if group.Name.Length > 0
      then 
        let ids : Id list ref = ref List.empty

        for mid in group.Members do
          if mid.Length > 0
          then ids := (mid :: !ids)

        let group' =
          { Name    = group.Name
          ; Members = !ids
          }
        groups := (group' :: !groups)

    { Name   = cfg.Project.Cluster.Name
    ; Nodes  = List.reverse !nodes
    ; Groups = List.reverse !groups
    }

  static member FromFile (file: ConfigFile) =
    let cfg = new Config()
    cfg.VvvvConfig     <- Config.ParseVvvv(file)
    cfg.AudioConfig    <- Config.ParseAudio(file)
    cfg.RaftConfig     <- Config.ParseRaft(file)
    cfg.TimingConfig   <- Config.ParseTiming(file)
    cfg.PortConfig     <- Config.ParsePort(file)
    cfg.ViewPorts      <- Config.ParseViewports(file)
    cfg.Displays       <- Config.ParseDisplays(file)
    cfg.Tasks          <- Config.ParseTasks(file)
    cfg.ClusterConfig  <- Config.ParseCluster(file)
    cfg


  static member Create (name: string) =
    let cfg = new Config()
    cfg.VvvvConfig     <- VvvvConfig.Default
    cfg.AudioConfig    <- AudioConfig.Default
    cfg.RaftConfig     <- RaftConfig.Default
    cfg.TimingConfig   <- TimingConfig.Default
    cfg.PortConfig     <- PortConfig.Default
    cfg.ViewPorts      <- List.empty
    cfg.Displays       <- List.empty
    cfg.Tasks          <- List.empty
    cfg.ClusterConfig  <- { Name   = name + " cluster"
                          ; Nodes  = List.empty                     
                          ; Groups = List.empty}
    cfg
