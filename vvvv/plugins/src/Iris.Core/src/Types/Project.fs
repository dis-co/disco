namespace Iris.Core.Types

open System
open System.IO
open System.Linq
open System.Collections.Generic
open Iris.Core.Types.Config

[<AutoOpen>]
[<ReflectedDefinition>]
module Project =

  (* ---------- Project ---------- *)
  type Project() =
    [<DefaultValue>] val mutable  Name      : string
    [<DefaultValue>] val mutable  Path      : FilePath option
    [<DefaultValue>] val mutable  LastSaved : DateTime option
    [<DefaultValue>] val mutable  Copyright : string   option
    [<DefaultValue>] val mutable  Author    : string   option
    [<DefaultValue>] val mutable  Year      : int
    [<DefaultValue>] val mutable  Audio     : AudioConfig
    [<DefaultValue>] val mutable  Vvvv      : VvvvConfig
    [<DefaultValue>] val mutable  Engine    : VsyncConfig
    [<DefaultValue>] val mutable  Timing    : TimingConfig
    [<DefaultValue>] val mutable  Port      : PortConfig
    [<DefaultValue>] val mutable  ViewPorts : ViewPort list
    [<DefaultValue>] val mutable  Displays  : Display list
    [<DefaultValue>] val mutable  Tasks     : Task list
    [<DefaultValue>] val mutable  Cluster   : Cluster


[<AutoOpen>]
/// utility functions only needed in native code
module ProjectUtil =
  /// Parse Audio configuration section
  let private parseAudioCfg (cfg : ConfigFile)  : AudioConfig =
    { SampleRate = uint32 cfg.Project.Audio.SampleRate }

  /// Parse VVvV configuration section
  let private parseVvvvCfg (cfg : ConfigFile) : VvvvConfig =
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

  /// Parse Vsync configuration section
  let private parseVsyncCfg (cfg : ConfigFile) : VsyncConfig =
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

  let private parseTimingCnf (cnf : ConfigFile) : TimingConfig =
    let servers : string list ref = ref List.empty

    for server in cnf.Project.Timing.Servers do
      servers := (server :: !servers)

    { Framebase = uint32 cnf.Project.Timing.Framebase
    ; Input     = cnf.Project.Timing.Input
    ; Servers   = !servers
    ; UDPPort   = uint32 cnf.Project.Timing.UDPPort
    ; TCPPort   = uint32 cnf.Project.Timing.TCPPort
    }

  let private parsePortCnf (cnf : ConfigFile) : PortConfig =
    { WebSocket = uint32 cnf.Project.Ports.WebSocket
    ; UDPCue    = uint32 cnf.Project.Ports.UDPCues
    ; Iris      = uint32 cnf.Project.Ports.IrisService
    }

  let private parseTuple (s : string) : (int * int) =
    let nonEmpty (s : string) : bool = s.Length > 0
    let parsed =
      s.Split([| '('; ','; ' '; ')' |])
      |> Array.filter nonEmpty
    (int parsed.[0], int parsed.[1])
      
  let private parseRect (str : string) : Rect = parseTuple str
  let private parseCoordinate (str : string) : Coordinate = parseTuple str

  let private parseViewports (cnf : ConfigFile) : ViewPort list =
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

  let private parseDisplays (cnf : ConfigFile) : Display list =
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
        
 
  let private parseTasks (cfg : ConfigFile) : Task list =
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

  let private parseCluster (cfg : ConfigFile) : Cluster =
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
    


  let createProject(name : string) : Project =
    let project = new Project()
    project.Name      <- name
    project.Path      <- None
    project.Copyright <- None
    project.LastSaved <- None
    project.Year      <- System.DateTime.Now.Year
    project.Audio     <- AudioConfig.Default
    project.Vvvv      <- VvvvConfig.Default
    project.Engine    <- VsyncConfig.Default
    project.Timing    <- TimingConfig.Default
    project.Port      <- PortConfig.Default
    project.ViewPorts <- List.empty
    project.Displays  <- List.empty
    project.Tasks     <- List.empty
    project.Cluster   <- {
      Name   = name + " cluster";
      Nodes  = List.empty;
      Groups = List.empty;
      }
    project

  let loadProject (path : FilePath) : Project option =
    if not <| File.Exists(path)
    then None
    else
      IrisConfig.Load(path)

      let Meta = IrisConfig.Project.Metadata

      let project = createProject Meta.Name
      project.Path <- Some(path)
      project.LastSaved <- Some(DateTime.Parse(Meta.LastSaved))
      project.Copyright <- Some(Meta.Copyright)
      project.Author    <- Some(Meta.Author)
      project.Year      <- Meta.Year
      project.Audio     <- parseAudioCfg  IrisConfig
      project.Vvvv      <- parseVvvvCfg   IrisConfig
      project.Engine    <- parseVsyncCfg  IrisConfig
      project.Timing    <- parseTimingCnf IrisConfig
      project.Port      <- parsePortCnf   IrisConfig
      project.ViewPorts <- parseViewports IrisConfig
      project.Displays  <- parseDisplays  IrisConfig
      project.Tasks     <- parseTasks     IrisConfig
      project.Cluster   <- parseCluster   IrisConfig

      Some(project)

  let saveProject (project : Project) =
    if Option.isSome project.Path
    then
      // Project metadata
      IrisConfig.Project.Metadata.Name <- project.Name

      if Option.isSome project.Author
      then IrisConfig.Project.Metadata.Author <- Option.get project.Author

      if Option.isSome project.Copyright
      then IrisConfig.Project.Metadata.Copyright <- Option.get project.Copyright

      IrisConfig.Project.Metadata.Year <- project.Year

      project.LastSaved <- Some(DateTime.Now)
      IrisConfig.Project.Metadata.LastSaved <- DateTime.Now.ToString()

      // VVVV related information
      IrisConfig.Project.VVVV.Executables.Clear()
      for exe in project.Vvvv.Executables do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type()
        entry.Path <- exe.Executable;
        entry.Version <- exe.Version;
        entry.Required <- exe.Required
        IrisConfig.Project.VVVV.Executables.Add(entry)

      IrisConfig.Project.VVVV.Plugins.Clear()
      for plug in project.Vvvv.Plugins do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type ()
        entry.Name <- plug.Name
        entry.Path <- plug.Path
        IrisConfig.Project.VVVV.Plugins.Add(entry)

      // Engine related configuration
      if Option.isSome project.Engine.AesKey
      then IrisConfig.Project.Engine.AesKey <- Option.get project.Engine.AesKey

      if Option.isSome project.Engine.DefaultTimeout
      then IrisConfig.Project.Engine.DefaultTimeout <- string (Option.get project.Engine.DefaultTimeout)

      if Option.isSome project.Engine.DontCompress
      then IrisConfig.Project.Engine.DontCompress <- Option.get project.Engine.DontCompress

      if Option.isSome project.Engine.FastEthernet
      then IrisConfig.Project.Engine.FastEthernet <- Option.get project.Engine.FastEthernet

      if Option.isSome project.Engine.GracefulShutdown
      then IrisConfig.Project.Engine.GracefulShutdown <- Option.get project.Engine.GracefulShutdown

      if Option.isSome project.Engine.Hosts
      then
        IrisConfig.Project.Engine.Hosts.Clear()
        for host in Option.get project.Engine.Hosts do
          IrisConfig.Project.Engine.Hosts.Add(host)

      if Option.isSome project.Engine.IgnorePartitions
      then IrisConfig.Project.Engine.IgnorePartitions <- Option.get project.Engine.IgnorePartitions

      if Option.isSome project.Engine.IgnoreSmallPartitions
      then IrisConfig.Project.Engine.IgnoreSmallPartitions <- Option.get project.Engine.IgnoreSmallPartitions

      if Option.isSome project.Engine.InfiniBand
      then IrisConfig.Project.Engine.InfiniBand <- Option.get project.Engine.InfiniBand

      if Option.isSome project.Engine.Large
      then IrisConfig.Project.Engine.Large <- Option.get project.Engine.Large

      if Option.isSome project.Engine.LogDir
      then IrisConfig.Project.Engine.LogDir <- Option.get project.Engine.LogDir

      if Option.isSome project.Engine.Logged
      then IrisConfig.Project.Engine.Logged <- Option.get project.Engine.Logged

      if Option.isSome project.Engine.MCMDReportRate
      then IrisConfig.Project.Engine.MCMDReportRate <- int (Option.get project.Engine.MCMDReportRate)

      if Option.isSome project.Engine.MCRangeHigh
      then IrisConfig.Project.Engine.MCRangeHigh <- Option.get project.Engine.MCRangeHigh

      if Option.isSome project.Engine.MCRangeLow
      then IrisConfig.Project.Engine.MCRangeLow <- Option.get project.Engine.MCRangeLow

      if Option.isSome project.Engine.MaxAsyncMTotal
      then IrisConfig.Project.Engine.MaxAsyncMTotal <- int (Option.get project.Engine.MaxAsyncMTotal)

      if Option.isSome project.Engine.MaxIPMCAddrs
      then IrisConfig.Project.Engine.MaxIPMCAddrs <- int (Option.get project.Engine.MaxIPMCAddrs)

      if Option.isSome project.Engine.MaxMsgLen
      then IrisConfig.Project.Engine.MaxMsgLen <- int (Option.get project.Engine.MaxMsgLen)

      if Option.isSome project.Engine.Mute
      then IrisConfig.Project.Engine.Mute <- Option.get project.Engine.Mute

      if Option.isSome project.Engine.Netmask
      then IrisConfig.Project.Engine.Netmask <- Option.get project.Engine.Netmask

      IrisConfig.Project.Engine.NetworkInterfaces.Clear()
      if Option.isSome project.Engine.NetworkInterfaces
      then
        for iface in Option.get project.Engine.NetworkInterfaces do
          IrisConfig.Project.Engine.NetworkInterfaces.Add(iface)

      if Option.isSome project.Engine.OOBViaTCP
      then IrisConfig.Project.Engine.OOBViaTCP <- Option.get project.Engine.OOBViaTCP

      if Option.isSome project.Engine.Port
      then IrisConfig.Project.Engine.Port <- int (Option.get project.Engine.Port)

      if Option.isSome project.Engine.PortP2P
      then IrisConfig.Project.Engine.PortP2P <- int (Option.get project.Engine.PortP2P)

      if Option.isSome project.Engine.RateLim
      then IrisConfig.Project.Engine.RateLim <- int (Option.get project.Engine.RateLim)

      if Option.isSome project.Engine.Sigs
      then IrisConfig.Project.Engine.Sigs <- Option.get project.Engine.Sigs

      if Option.isSome project.Engine.SkipFirstInterface
      then IrisConfig.Project.Engine.SkipFirstInterface <- Option.get project.Engine.SkipFirstInterface

      if Option.isSome project.Engine.Subnet
      then IrisConfig.Project.Engine.Subnet <- Option.get project.Engine.Subnet

      if Option.isSome project.Engine.TTL
      then IrisConfig.Project.Engine.TTL <- int (Option.get project.Engine.TTL)

      if Option.isSome project.Engine.TokenDelay
      then IrisConfig.Project.Engine.TokenDelay <- int (Option.get project.Engine.TokenDelay)

      if Option.isSome project.Engine.UDPChkSum
      then IrisConfig.Project.Engine.UDPChkSum <- Option.get project.Engine.UDPChkSum

      if Option.isSome project.Engine.UnicastOnly
      then IrisConfig.Project.Engine.UnicastOnly <- Option.get project.Engine.UnicastOnly

      if Option.isSome project.Engine.UseIPv4
      then IrisConfig.Project.Engine.UseIPv4 <- Option.get project.Engine.UseIPv4

      if Option.isSome project.Engine.UseIPv6
      then IrisConfig.Project.Engine.UseIPv6 <- Option.get project.Engine.UseIPv6

      if Option.isSome project.Engine.UserDMA
      then IrisConfig.Project.Engine.UserDMA <- Option.get project.Engine.UserDMA

      // Timing
      IrisConfig.Project.Timing.Framebase <- int (project.Timing.Framebase)
      IrisConfig.Project.Timing.Input <- project.Timing.Input

      IrisConfig.Project.Timing.Servers.Clear()
      for srv in project.Timing.Servers do
        IrisConfig.Project.Timing.Servers.Add(srv)

      IrisConfig.Project.Timing.TCPPort <- int (project.Timing.TCPPort)
      IrisConfig.Project.Timing.UDPPort <- int (project.Timing.UDPPort)

      // Ports
      IrisConfig.Project.Ports.IrisService <- int (project.Port.Iris)
      IrisConfig.Project.Ports.UDPCues <- int (project.Port.UDPCue)
      IrisConfig.Project.Ports.WebSocket <- int (project.Port.WebSocket)

      // Audio
      IrisConfig.Project.Audio.SampleRate <- int (project.Audio.SampleRate)

      // ViewPorts
      IrisConfig.Project.ViewPorts.Clear()
      for vp in project.ViewPorts do
        let item = new ConfigFile.Project_Type.ViewPorts_Item_Type()
        item.Id <- vp.Id
        item.Name <- vp.Name
        item.Size <- vp.Size.ToString()
        item.Position <- vp.Position.ToString()
        item.Overlap <- vp.Overlap.ToString()
        item.OutputPosition <- vp.OutputPosition.ToString()
        item.OutputSize <- vp.OutputSize.ToString()
        item.Description <- vp.Description
        IrisConfig.Project.ViewPorts.Add(item)

      // Displays
      IrisConfig.Project.Displays.Clear()
      for dp in project.Displays do
        let item = new ConfigFile.Project_Type.Displays_Item_Type()
        item.Id <- dp.Id
        item.Name <- dp.Name
        item.Size <- dp.Size.ToString()

        item.RegionMap.SrcViewportId <- dp.RegionMap.SrcViewportId
        item.RegionMap.Regions.Clear()
        for region in dp.RegionMap.Regions do
          let r = new ConfigFile.Project_Type.Displays_Item_Type.RegionMap_Type.Regions_Item_Type()
          r.Id <- region.Id
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

        IrisConfig.Project.Displays.Add(item)

      // Tasks
      IrisConfig.Project.Tasks.Clear()
      for task in project.Tasks do
        let t = new ConfigFile.Project_Type.Tasks_Item_Type()
        t.Id <- task.Id
        t.AudioStream <- task.AudioStream
        t.Description <- task.Description
        t.DisplayId   <- task.DisplayId
        for arg in task.Arguments do
          let a = new ConfigFile.Project_Type.Tasks_Item_Type.Arguments_Item_Type()
          a.Key <- fst arg
          a.Value <- snd arg
          t.Arguments.Add(a)
        IrisConfig.Project.Tasks.Add(t)

      // Cluster
      IrisConfig.Project.Cluster.Nodes.Clear()
      IrisConfig.Project.Cluster.Groups.Clear()
      IrisConfig.Project.Cluster.Name <- project.Cluster.Name

      for node in project.Cluster.Nodes do
        let n = new ConfigFile.Project_Type.Cluster_Type.Nodes_Item_Type()
        n.Id       <- node.Id
        n.Ip       <- node.Ip
        n.HostName <- node.HostName
        n.Task     <- node.Task
        IrisConfig.Project.Cluster.Nodes.Add(n)

      for group in project.Cluster.Groups do
        let g = new ConfigFile.Project_Type.Cluster_Type.Groups_Item_Type()
        g.Name <- group.Name

        for mem in group.Members do
          g.Members.Add(mem)

        IrisConfig.Project.Cluster.Groups.Add(g)

      // finally save everything!
      IrisConfig.Save(Option.get project.Path)
