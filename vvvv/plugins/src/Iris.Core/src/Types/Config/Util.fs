namespace Iris.Core.Types.Config

open System
open Iris.Core.Types

[<AutoOpen>]
module Util =
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
    if t.Length >= 0
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

    { Executables = !exes
    ; Plugins     = !plugs
    }

  //  __     __
  //  \ \   / /__ _   _ _ __   ___
  //   \ \ / / __| | | | '_ \ / __|
  //    \ V /\__ \ |_| | | | | (__
  //     \_/ |___/\__, |_| |_|\___|
  //              |___/
  //
  /// Parse Vsync configuration section
  let parseVsyncCfg (cfg : ConfigFile) : VsyncConfig =
    let eng = cfg.Project.Engine

    let ifaces : string list option ref = ref None
    let hosts  : string list option ref = ref None

    for host in cfg.Project.Engine.Hosts do
      let hosts' = !hosts
      match hosts' with
        | Some(list) -> hosts := Some(host :: list)
        | _ -> hosts := Some([ host ])

    for iface in cfg.Project.Engine.NetworkInterfaces do
      let ifaces' = !ifaces
      match ifaces' with
        | Some(list) -> ifaces := Some(iface :: list)
        | _ -> ifaces := Some([ iface ])

    { AesKey                = getOptStr eng.AesKey
    ; DefaultTimeout        = getOptUInt eng.DefaultTimeout
    ; DontCompress          = Some(eng.DontCompress)
    ; FastEthernet          = Some(eng.FastEthernet)
    ; GracefulShutdown      = Some(eng.GracefulShutdown)
    ; Hosts                 = !hosts
    ; IgnorePartitions      = Some(eng.IgnorePartitions)
    ; IgnoreSmallPartitions = Some(eng.IgnoreSmallPartitions)
    ; InfiniBand            = Some(eng.InfiniBand)
    ; Large                 = Some(eng.Large)
    ; LogDir                = getOptStr eng.LogDir
    ; Logged                = Some(eng.Logged)
    ; MCMDReportRate        = getOptInt eng.MCMDReportRate
    ; MCRangeHigh           = getOptStr eng.MCRangeHigh
    ; MCRangeLow            = getOptStr eng.MCRangeLow
    ; MaxAsyncMTotal        = getOptInt eng.MaxAsyncMTotal
    ; MaxIPMCAddrs          = getOptInt eng.MaxIPMCAddrs
    ; MaxMsgLen             = getOptInt eng.MaxMsgLen
    ; Mute                  = Some(eng.Mute)
    ; Netmask               = getOptStr eng.Netmask
    ; NetworkInterfaces     = !ifaces
    ; OOBViaTCP             = Some(eng.OOBViaTCP)
    ; Port                  = getOptInt eng.Port
    ; PortP2P               = getOptInt eng.PortP2P
    ; RateLim               = getOptInt eng.RateLim
    ; Sigs                  = Some(eng.Sigs)
    ; SkipFirstInterface    = Some(eng.SkipFirstInterface)
    ; Subnet                = getOptStr eng.Subnet
    ; TTL                   = getOptInt eng.TTL
    ; TokenDelay            = getOptInt eng.TokenDelay
    ; UDPChkSum             = Some(eng.UDPChkSum)
    ; UnicastOnly           = Some(eng.UnicastOnly)
    ; UseIPv4               = Some(eng.UseIPv4)
    ; UseIPv6               = Some(eng.UseIPv6)
    ; UserDMA               = Some(eng.UserDMA)
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
    ; Servers   = !servers
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

    !vports

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
        ; Signals   = !signals
        ; RegionMap =
          { SrcViewportId = display.RegionMap.SrcViewportId
          ; Regions       = !regions
          }
        }
      displays := (display' :: !displays)

    !displays

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
        arguments := ((argument.Key, argument.Value) :: !arguments)

      let task' =
        { Id          = task.Id
        ; Description = task.Description
        ; DisplayId   = task.DisplayId
        ; AudioStream = task.AudioStream
        ; Arguments   = !arguments
        }
      tasks := (task' :: !tasks)

    !tasks

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
      let ids : Id list ref = ref List.empty

      for id in group.Members do
        ids := (id :: !ids)

      let group' =
        { Name    = group.Name
        ; Members = !ids
        }

      groups := (group' :: !groups)

    { Name   = cfg.Project.Cluster.Name
    ; Nodes  = !nodes
    ; Groups = !groups
    }
