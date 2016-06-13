namespace Iris.Core.Config

open System
open Iris.Core.Types

[<AutoOpen>]
module Config =

  type Config =
    {  Audio     : AudioConfig
    ;  Vvvv      : VvvvConfig
    ;  Engine    : RaftConfig
    ;  Timing    : TimingConfig
    ;  Port      : PortConfig
    ;  ViewPorts : ViewPort list
    ;  Displays  : Display  list
    ;  Tasks     : Task     list
    ;  Cluster   : Cluster
    }

  //   ____       _            _
  //  |  _ \ _ __(_)_   ____ _| |_ ___
  //  | |_) | '__| \ \ / / _` | __/ _ \
  //  |  __/| |  | |\ V / (_| | ||  __/
  //  |_|   |_|  |_| \_/ \__,_|\__\___|
  //
  let private getOptStr (t : string) : string option =
    if t.Length > 0
    then Some(t)
    else None

  let private getOptUInt (t : string) : uint32 option =
    if t.Length > 0
    then Some(UInt32.Parse(t))
    else None

  let private getOptInt (t : int) : uint32 option =
    if t >= 0
    then Some(uint32 t)
    else None

  let private parseTuple (s : string) : (int * int) =
    let nonEmpty (s : string) : bool = s.Length > 0
    let parsed =
      s.Split([| '('; ','; ' '; ')' |])
      |> Array.filter nonEmpty
    (int parsed.[0], int parsed.[1])

  let private parseRect (str : string) : Rect = parseTuple str
  let private parseCoordinate (str : string) : Coordinate = parseTuple str

  let private reverse (lst : 'a list) : 'a list =
    let reverser acc elm = List.concat [[elm]; acc]
    List.fold reverser List.empty lst

  let parseStringProp (str : string) : string option =
    if str.Length > 0
    then Some(str)
    else None

  //      _             _ _
  //     / \  _   _  __| (_) ___
  //    / _ \| | | |/ _` | |/ _ \
  //   / ___ \ |_| | (_| | | (_) |
  //  /_/   \_\__,_|\__,_|_|\___/
  //
  /// Parse Audio Configuration Section
  let parseAudioCfg (cfg : ConfigFile)  : AudioConfig =
    { SampleRate = uint32 cfg.Project.Audio.SampleRate }

  //  __     __
  //  \ \   / /_   ____   ____   __
  //   \ \ / /\ \ / /\ \ / /\ \ / /
  //    \ V /  \ V /  \ V /  \ V /
  //     \_/    \_/    \_/    \_/
  //
  /// Parse VVvV configuration section
  let parseVvvvCfg (cfg : ConfigFile) : VvvvConfig =
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

    { Executables = reverse !exes
    ; Plugins     = reverse !plugs
    }

  //  __     __
  //  \ \   / /__ _   _ _ __   ___
  //   \ \ / / __| | | | '_ \ / __|
  //    \ V /\__ \ |_| | | | | (__
  //     \_/ |___/\__, |_| |_|\___|
  //              |___/
  //
  /// Parse Vsync configuration section
  let parseVsyncCfg (cfg : ConfigFile) : RaftConfig =
    let eng = cfg.Project.Engine

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
      | Some(list) -> hosts := Some(reverse list)
      | None       -> hosts := None

    for iface in cfg.Project.Engine.NetworkInterfaces do
      let ifaces' = !ifaces
      match ifaces' with
        | Some(list) -> ifaces := Some(iface :: list)
        | _          -> ifaces := Some([ iface ])

    match !ifaces with
      | Some(list) -> ifaces := Some(reverse list)
      | None       -> ifaces := None

    { RequestTimeout = 0u
    ; TempDir = "hahhah"
    }

  //   _____ _           _
  //  |_   _(_)_ __ ___ (_)_ __   __ _
  //    | | | | '_ ` _ \| | '_ \ / _` |
  //    | | | | | | | | | | | | | (_| |
  //    |_| |_|_| |_| |_|_|_| |_|\__, |
  //                             |___/
  //
  /// Parse Timing Configuration Section
  let parseTimingCnf (cnf : ConfigFile) : TimingConfig =
    let servers : string list ref = ref List.empty

    for server in cnf.Project.Timing.Servers do
      servers := (server :: !servers)

    { Framebase = uint32 cnf.Project.Timing.Framebase
    ; Input     = cnf.Project.Timing.Input
    ; Servers   = reverse !servers
    ; UDPPort   = uint32 cnf.Project.Timing.UDPPort
    ; TCPPort   = uint32 cnf.Project.Timing.TCPPort
    }

  //   ____            _
  //  |  _ \ ___  _ __| |_
  //  | |_) / _ \| '__| __|
  //  |  __/ (_) | |  | |_
  //  |_|   \___/|_|   \__|
  //
  /// Parse Port Configuration Section
  let parsePortCnf (cnf : ConfigFile) : PortConfig =
    { WebSocket = uint32 cnf.Project.Ports.WebSocket
    ; UDPCue    = uint32 cnf.Project.Ports.UDPCues
    ; Iris      = uint32 cnf.Project.Ports.IrisService
    }

  //  __     ___               ____            _
  //  \ \   / (_) _____      _|  _ \ ___  _ __| |_
  //   \ \ / /| |/ _ \ \ /\ / / |_) / _ \| '__| __|
  //    \ V / | |  __/\ V  V /|  __/ (_) | |  | |_
  //     \_/  |_|\___| \_/\_/ |_|   \___/|_|   \__|
  //
  /// Parse ViewPort Configuration Section
  let parseViewports (cnf : ConfigFile) : ViewPort list =
    let vports : ViewPort list ref = ref List.empty

    for vp in cnf.Project.ViewPorts do
      let viewport' =
        { Id             = vp.Id
        ; Name           = vp.Name
        ; Position       = parseCoordinate vp.Position
        ; Size           = parseRect       vp.Size
        ; OutputPosition = parseCoordinate vp.OutputPosition
        ; OutputSize     = parseRect       vp.OutputSize
        ; Overlap        = parseRect       vp.Overlap
        ; Description    = vp.Description
        }
      vports := (viewport' :: !vports)

    reverse !vports

  //   ____  _           _
  //  |  _ \(_)___ _ __ | | __ _ _   _ ___
  //  | | | | / __| '_ \| |/ _` | | | / __|
  //  | |_| | \__ \ |_) | | (_| | |_| \__ \
  //  |____/|_|___/ .__/|_|\__,_|\__, |___/
  //              |_|            |___/
  /// Parse Displays Configuration Section
  let parseDisplays (cnf : ConfigFile) : Display list =
    let displays : Display list ref = ref List.empty

    for display in cnf.Project.Displays do

      /// scrape all signal defs out of the config
      let signals : Signal list ref = ref List.empty
      for signal in display.Signals do
        let signal' : Signal =
          { Size     = parseRect signal.Size
          ; Position = parseCoordinate signal.Position }
        signals := (signal' :: !signals)

      let regions : Region list ref = ref List.empty

      for region in display.RegionMap.Regions do
        let region' =
          { Id             = region.Id
          ; Name           = region.Name
          ; SrcPosition    = parseCoordinate region.SrcPosition
          ; SrcSize        = parseRect region.SrcSize
          ; OutputPosition = parseCoordinate region.OutputPosition
          ; OutputSize     = parseRect region.OutputSize
          }
        regions := (region' :: !regions)

      let display' =
        { Id        = display.Id
        ; Name      = display.Name
        ; Size      = parseRect display.Size
        ; Signals   = reverse !signals
        ; RegionMap =
          { SrcViewportId = display.RegionMap.SrcViewportId
          ; Regions       = reverse !regions
          }
        }
      displays := (display' :: !displays)

    reverse !displays

  //   _____         _
  //  |_   _|_ _ ___| | _____
  //    | |/ _` / __| |/ / __|
  //    | | (_| \__ \   <\__ \
  //    |_|\__,_|___/_|\_\___/
  //
  /// Parse Task Configuration Section
  let parseTasks (cfg : ConfigFile) : Task list =
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

    reverse !tasks

  //    ____ _           _
  //   / ___| |_   _ ___| |_ ___ _ __
  //  | |   | | | | / __| __/ _ \ '__|
  //  | |___| | |_| \__ \ ||  __/ |
  //   \____|_|\__,_|___/\__\___|_|
  //
  /// Parse Cluster Configuration Section
  let parseCluster (cfg : ConfigFile) : Cluster =
    let nodes  : Node list ref = ref List.empty
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
    ; Nodes  = reverse !nodes
    ; Groups = reverse !groups
    }
