namespace Iris.Core.Types

open System
open System.IO
open System.Linq
open System.Collections.Generic
open Iris.Core.Types.Config
open LibGit2Sharp

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
    [<DefaultValue>] val mutable  Config    : Config

    [<DefaultValue>] val mutable  Repo      : Repository

    override self.GetHashCode() =
      hash self

    override self.Equals(other) =
      match other with
        | :? Project as p ->
          (p.Name             = self.Name)             &&
          (p.Path             = self.Path)             &&
          (p.LastSaved        = self.LastSaved)        &&
          (p.Copyright        = self.Copyright)        &&
          (p.Author           = self.Author)           &&
          (p.Year             = self.Year)             &&
          (p.Config.Audio     = self.Config.Audio)     &&
          (p.Config.Vvvv      = self.Config.Vvvv)      &&
          (p.Config.Engine    = self.Config.Engine)    &&
          (p.Config.Timing    = self.Config.Timing)    &&
          (p.Config.Port      = self.Config.Port)      &&
          (p.Config.ViewPorts = self.Config.ViewPorts) &&
          (p.Config.Displays  = self.Config.Displays)  &&
          (p.Config.Tasks     = self.Config.Tasks)     &&
          (p.Config.Cluster   = self.Config.Cluster)
        | _ -> false

[<AutoOpen>]
/// utility functions only needed in native code
module ProjectUtil =
  let private IrisExt = ".iris"

  //    ____                _
  //   / ___|_ __ ___  __ _| |_ ___
  //  | |   | '__/ _ \/ _` | __/ _ \
  //  | |___| | |  __/ (_| | ||  __/
  //   \____|_|  \___|\__,_|\__\___|
  //
  /// Create a new project
  let createProject(name : string) : Project =
    let project = new Project()
    project.Name      <- name
    project.Path      <- None
    project.Copyright <- None
    project.LastSaved <- None
    project.Year      <- System.DateTime.Now.Year
    project.Config    <-
      { Audio     =  AudioConfig.Default
      ; Vvvv      =  VvvvConfig.Default
      ; Engine    =  VsyncConfig.Default
      ; Timing    =  TimingConfig.Default
      ; Port      =  PortConfig.Default
      ; ViewPorts =  List.empty
      ; Displays  =  List.empty
      ; Tasks     =  List.empty
      ; Cluster   =
          { Name   = name + " cluster"
          ; Nodes  = List.empty
          ; Groups = List.empty
          }
      }
    project

  //   _                    _
  //  | |    ___   __ _  __| |
  //  | |   / _ \ / _` |/ _` |
  //  | |__| (_) | (_| | (_| |
  //  |_____\___/ \__,_|\__,_|
  //
  /// Load a Project from Disk
  let loadProject (path : FilePath) : Project option =
    if not <| File.Exists(path) // must be a *File*
    then None
    else
      IrisConfig.Load(path)

      let Meta = IrisConfig.Project.Metadata

      let date =
        if Meta.LastSaved.Length > 0
        then
          try
            Some(DateTime.Parse(Meta.LastSaved))
          with
            | _ -> None
        else None

      let project = createProject Meta.Name
      let basedir = Path.GetDirectoryName(path)
      
      project.Repo      <- new Repository(Path.Combine(basedir, ".git"))
      project.Path      <- Some(Path.GetDirectoryName(path))
      project.LastSaved <- date
      project.Copyright <- parseStringProp Meta.Copyright
      project.Author    <- parseStringProp Meta.Author
      project.Year      <- Meta.Year
      project.Config    <-
        { Audio     = parseAudioCfg  IrisConfig
        ; Vvvv      = parseVvvvCfg   IrisConfig
        ; Engine    = parseVsyncCfg  IrisConfig
        ; Timing    = parseTimingCnf IrisConfig
        ; Port      = parsePortCnf   IrisConfig
        ; ViewPorts = parseViewports IrisConfig
        ; Displays  = parseDisplays  IrisConfig
        ; Tasks     = parseTasks     IrisConfig
        ; Cluster   = parseCluster   IrisConfig
        }
      Some(project)

  //   ____
  //  / ___|  __ ___   _____
  //  \___ \ / _` \ \ / / _ \
  //   ___) | (_| |\ V /  __/
  //  |____/ \__,_| \_/ \___|
  //
  /// Save a Project to Disk
  let saveProject (project : Project) =
    if Option.isSome project.Path
    then
      Directory.CreateDirectory (Option.get project.Path) |> ignore

      if isNull project.Repo
      then 
        let path = Repository.Init(Option.get project.Path)
        project.Repo <- new Repository(path)

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
      for exe in project.Config.Vvvv.Executables do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type()
        entry.Path <- exe.Executable;
        entry.Version <- exe.Version;
        entry.Required <- exe.Required
        IrisConfig.Project.VVVV.Executables.Add(entry)

      IrisConfig.Project.VVVV.Plugins.Clear()
      for plug in project.Config.Vvvv.Plugins do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type ()
        entry.Name <- plug.Name
        entry.Path <- plug.Path
        IrisConfig.Project.VVVV.Plugins.Add(entry)

      // Engine related configuration
      if Option.isSome project.Config.Engine.AesKey
      then IrisConfig.Project.Engine.AesKey <- Option.get project.Config.Engine.AesKey

      if Option.isSome project.Config.Engine.DefaultTimeout
      then
        let value = string (Option.get project.Config.Engine.DefaultTimeout)
        IrisConfig.Project.Engine.DefaultTimeout <- value

      if Option.isSome project.Config.Engine.DontCompress
      then
        let value = Option.get project.Config.Engine.DontCompress
        IrisConfig.Project.Engine.DontCompress <- value

      if Option.isSome project.Config.Engine.FastEthernet
      then
        let value = Option.get project.Config.Engine.FastEthernet
        IrisConfig.Project.Engine.FastEthernet <- value

      if Option.isSome project.Config.Engine.GracefulShutdown
      then
        let value = Option.get project.Config.Engine.GracefulShutdown
        IrisConfig.Project.Engine.GracefulShutdown <- value

      if Option.isSome project.Config.Engine.Hosts
      then
        IrisConfig.Project.Engine.Hosts.Clear()
        for host in Option.get project.Config.Engine.Hosts do
          IrisConfig.Project.Engine.Hosts.Add(host)

      if Option.isSome project.Config.Engine.IgnorePartitions
      then
        let value = Option.get project.Config.Engine.IgnorePartitions
        IrisConfig.Project.Engine.IgnorePartitions <- value

      if Option.isSome project.Config.Engine.IgnoreSmallPartitions
      then
        let value = Option.get project.Config.Engine.IgnoreSmallPartitions
        IrisConfig.Project.Engine.IgnoreSmallPartitions <- value

      if Option.isSome project.Config.Engine.InfiniBand
      then
        let value = Option.get project.Config.Engine.InfiniBand
        IrisConfig.Project.Engine.InfiniBand <- value

      if Option.isSome project.Config.Engine.Large
      then
        let value = Option.get project.Config.Engine.Large
        IrisConfig.Project.Engine.Large <- value

      if Option.isSome project.Config.Engine.LogDir
      then
        let value = Option.get project.Config.Engine.LogDir
        IrisConfig.Project.Engine.LogDir <- value

      if Option.isSome project.Config.Engine.Logged
      then
        let value = Option.get project.Config.Engine.Logged
        IrisConfig.Project.Engine.Logged <- value

      if Option.isSome project.Config.Engine.MCMDReportRate
      then
        let value = int (Option.get project.Config.Engine.MCMDReportRate)
        IrisConfig.Project.Engine.MCMDReportRate <- value

      if Option.isSome project.Config.Engine.MCRangeHigh
      then
        let value = Option.get project.Config.Engine.MCRangeHigh
        IrisConfig.Project.Engine.MCRangeHigh <- value

      if Option.isSome project.Config.Engine.MCRangeLow
      then
        let value = Option.get project.Config.Engine.MCRangeLow
        IrisConfig.Project.Engine.MCRangeLow <- value

      if Option.isSome project.Config.Engine.MaxAsyncMTotal
      then
        let value = int (Option.get project.Config.Engine.MaxAsyncMTotal)
        IrisConfig.Project.Engine.MaxAsyncMTotal <- value

      if Option.isSome project.Config.Engine.MaxIPMCAddrs
      then
        let value = int (Option.get project.Config.Engine.MaxIPMCAddrs)
        IrisConfig.Project.Engine.MaxIPMCAddrs <- value

      if Option.isSome project.Config.Engine.MaxMsgLen
      then
        let value = int (Option.get project.Config.Engine.MaxMsgLen)
        IrisConfig.Project.Engine.MaxMsgLen <- value

      if Option.isSome project.Config.Engine.Mute
      then
        let value = Option.get project.Config.Engine.Mute
        IrisConfig.Project.Engine.Mute <- value

      if Option.isSome project.Config.Engine.Netmask
      then
        let value = Option.get project.Config.Engine.Netmask
        IrisConfig.Project.Engine.Netmask <- value

      IrisConfig.Project.Engine.NetworkInterfaces.Clear()
      if Option.isSome project.Config.Engine.NetworkInterfaces
      then
        for iface in Option.get project.Config.Engine.NetworkInterfaces do
          IrisConfig.Project.Engine.NetworkInterfaces.Add(iface)

      if Option.isSome project.Config.Engine.OOBViaTCP
      then
        let value = Option.get project.Config.Engine.OOBViaTCP
        IrisConfig.Project.Engine.OOBViaTCP <- value

      if Option.isSome project.Config.Engine.Port
      then
        let value = int (Option.get project.Config.Engine.Port)
        IrisConfig.Project.Engine.Port <- value

      if Option.isSome project.Config.Engine.PortP2P
      then
        let value = int (Option.get project.Config.Engine.PortP2P)
        IrisConfig.Project.Engine.PortP2P <- value

      if Option.isSome project.Config.Engine.RateLim
      then
        let value = int (Option.get project.Config.Engine.RateLim)
        IrisConfig.Project.Engine.RateLim <- value

      if Option.isSome project.Config.Engine.Sigs
      then
        let value = Option.get project.Config.Engine.Sigs
        IrisConfig.Project.Engine.Sigs <- value

      if Option.isSome project.Config.Engine.SkipFirstInterface
      then
        let value = Option.get project.Config.Engine.SkipFirstInterface
        IrisConfig.Project.Engine.SkipFirstInterface <- value

      if Option.isSome project.Config.Engine.Subnet
      then
        let value = Option.get project.Config.Engine.Subnet
        IrisConfig.Project.Engine.Subnet <- value

      if Option.isSome project.Config.Engine.TTL
      then
        let value = int (Option.get project.Config.Engine.TTL)
        IrisConfig.Project.Engine.TTL <- value

      if Option.isSome project.Config.Engine.TokenDelay
      then
        let value = int (Option.get project.Config.Engine.TokenDelay)
        IrisConfig.Project.Engine.TokenDelay <- value

      if Option.isSome project.Config.Engine.UDPChkSum
      then
        let value = Option.get project.Config.Engine.UDPChkSum
        IrisConfig.Project.Engine.UDPChkSum <- value

      if Option.isSome project.Config.Engine.UnicastOnly
      then
        let value = Option.get project.Config.Engine.UnicastOnly
        IrisConfig.Project.Engine.UnicastOnly <- value

      if Option.isSome project.Config.Engine.UseIPv4
      then
        let value = Option.get project.Config.Engine.UseIPv4
        IrisConfig.Project.Engine.UseIPv4 <- value

      if Option.isSome project.Config.Engine.UseIPv6
      then
        let value = Option.get project.Config.Engine.UseIPv6
        IrisConfig.Project.Engine.UseIPv6 <- value

      if Option.isSome project.Config.Engine.UserDMA
      then
        let value = Option.get project.Config.Engine.UserDMA
        IrisConfig.Project.Engine.UserDMA <- value

      // Timing
      IrisConfig.Project.Timing.Framebase <- int (project.Config.Timing.Framebase)
      IrisConfig.Project.Timing.Input <- project.Config.Timing.Input

      IrisConfig.Project.Timing.Servers.Clear()
      for srv in project.Config.Timing.Servers do
        IrisConfig.Project.Timing.Servers.Add(srv)

      IrisConfig.Project.Timing.TCPPort <- int (project.Config.Timing.TCPPort)
      IrisConfig.Project.Timing.UDPPort <- int (project.Config.Timing.UDPPort)

      // Ports
      IrisConfig.Project.Ports.IrisService <- int (project.Config.Port.Iris)
      IrisConfig.Project.Ports.UDPCues <- int (project.Config.Port.UDPCue)
      IrisConfig.Project.Ports.WebSocket <- int (project.Config.Port.WebSocket)

      // Audio
      IrisConfig.Project.Audio.SampleRate <- int (project.Config.Audio.SampleRate)

      // ViewPorts
      IrisConfig.Project.ViewPorts.Clear()
      for vp in project.Config.ViewPorts do
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
      for dp in project.Config.Displays do
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
      for task in project.Config.Tasks do
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
      IrisConfig.Project.Cluster.Name <- project.Config.Cluster.Name

      for node in project.Config.Cluster.Nodes do
        let n = new ConfigFile.Project_Type.Cluster_Type.Nodes_Item_Type()
        n.Id       <- node.Id
        n.Ip       <- node.Ip
        n.HostName <- node.HostName
        n.Task     <- node.Task
        IrisConfig.Project.Cluster.Nodes.Add(n)

      for group in project.Config.Cluster.Groups do
        let g = new ConfigFile.Project_Type.Cluster_Type.Groups_Item_Type()
        g.Name <- group.Name

        for mem in group.Members do
          g.Members.Add(mem)

        IrisConfig.Project.Cluster.Groups.Add(g)

      // save everything!
      let destPath = Path.Combine(Option.get project.Path, project.Name + IrisExt)
      IrisConfig.Save(destPath)

      // commit project to git.
      project.Repo
      |> (fun repo ->
          let msg  = "Project saved."
          let sign = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))
          repo.Stage(destPath)
          repo.Commit(msg, sign, sign)
          |> ignore)
