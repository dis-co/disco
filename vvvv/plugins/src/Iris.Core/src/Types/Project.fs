namespace Iris.Core.Types

open System
open System.IO
open System.Linq
open System.Net
open System.Collections.Generic
open Iris.Core.Utils
open Iris.Core.Config
open LibGit2Sharp

[<AutoOpen>]
module Project =
  //  ____            _           _   ____        _
  // |  _ \ _ __ ___ (_) ___  ___| |_|  _ \  __ _| |_ __ _
  // | |_) | '__/ _ \| |/ _ \/ __| __| | | |/ _` | __/ _` |
  // |  __/| | | (_) | |  __/ (__| |_| |_| | (_| | || (_| |
  // |_|   |_|  \___// |\___|\___|\__|____/ \__,_|\__\__,_|
  //               |__/
  [<ReflectedDefinition>]
  type ProjectData() =
    [<DefaultValue>] val mutable  Name      : string
    [<DefaultValue>] val mutable  Path      : FilePath option
    [<DefaultValue>] val mutable  LastSaved : DateTime option
    [<DefaultValue>] val mutable  Copyright : string   option
    [<DefaultValue>] val mutable  Author    : string   option
    [<DefaultValue>] val mutable  Year      : int
    [<DefaultValue>] val mutable  Config    : Config

    override self.GetHashCode() =
      hash self

    override self.Equals(other) =
      match other with
        | :? ProjectData as p ->
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

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/
  type Project(repo, data) =
    let mutable repo : Repository option = repo
    let mutable data : ProjectData = data

    //   ____                          _ _   _
    //  / ___|___  _ __ ___  _ __ ___ (_) |_| |_ ___ _ __
    // | |   / _ \| '_ ` _ \| '_ ` _ \| | __| __/ _ \ '__|
    // | |__| (_) | | | | | | | | | | | | |_| ||  __/ |
    //  \____\___/|_| |_| |_|_| |_| |_|_|\__|\__\___|_| is Iris
    let Committer =
      let hostname = Dns.GetHostName()
      new Signature("Iris", "iris@" + hostname, new DateTimeOffset(DateTime.Now))

    new (data : ProjectData) = new Project(None, data)

    //  ____                            _
    // |  _ \ _ __ ___  _ __   ___ _ __(_) ___  ___
    // | |_) | '__/ _ \| '_ \ / _ \ '__| |/ _ \/ __|
    // |  __/| | | (_) | |_) |  __/ |  | |  __/\__ \
    // |_|   |_|  \___/| .__/ \___|_|  |_|\___||___/
    //                 |_|
    member self.Name
      with get()     = data.Name
      and  set(name) = data.Name <- name

    member self.Path
      with get()     = data.Path
      and  set(path) = data.Path <- path

    member self.Repo
      with get()      = repo
      and  set(repo') = repo <- repo'

    member self.LastSaved
      with get() = data.LastSaved
      and  set(date) = data.LastSaved <- date

    member self.Copyright
      with get()          = data.Copyright
      and  set(copyright) = data.Copyright <- copyright

    member self.Author
      with get()       = data.Author
      and  set(author) = data.Author <- author
      
    member self.Year
      with get()     = data.Year
      and  set(year) = data.Year <- year

    member self.Config
      with get()     = data.Config
      and  set(conf) = data.Config <- conf

    member self.Data
      with get()      = data
      and  set(data') = data <- data'

    // Static:
    //    ____                _
    //   / ___|_ __ ___  __ _| |_ ___
    //  | |   | '__/ _ \/ _` | __/ _ \
    //  | |___| | |  __/ (_| | ||  __/
    //   \____|_|  \___|\__,_|\__\___|
    //
    static member Create(name : string) =
      let data = new ProjectData()
      data.Name      <- name
      data.Path      <- None
      data.Copyright <- None
      data.LastSaved <- None
      data.Year      <- System.DateTime.Now.Year
      data.Config    <-
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
      new Project(data)

    //   _                    _
    //  | |    ___   __ _  __| |
    //  | |   / _ \ / _` |/ _` |
    //  | |__| (_) | (_| | (_| |
    //  |_____\___/ \__,_|\__,_|
    //
    /// Load a Project from Disk
    static member Load(path : FilePath) : Project option =
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

        let project = Project.Create Meta.Name
        let basedir = Path.GetDirectoryName(path)

        project.Repo      <- Some(new Repository(Path.Combine(basedir, ".git")))
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
    member self.Save(sign : Signature, msg : string) =
      if Option.isSome self.Path
      then
        Directory.CreateDirectory (Option.get self.Path) |> ignore

        if Option.isNone self.Repo
        then
          let path = Repository.Init(Option.get self.Path)
          File.WriteAllText(Path.Combine(path, "git-daemon-export-ok"), "")
          self.Repo <- Some(new Repository(path))

        // Project metadata
        IrisConfig.Project.Metadata.Name <- self.Name

        if Option.isSome self.Author
        then IrisConfig.Project.Metadata.Author <- Option.get self.Author

        if Option.isSome self.Copyright
        then IrisConfig.Project.Metadata.Copyright <- Option.get self.Copyright

        IrisConfig.Project.Metadata.Year <- self.Year

        self.LastSaved <- Some(DateTime.Now)
        IrisConfig.Project.Metadata.LastSaved <- DateTime.Now.ToString()

        // VVVV related information
        IrisConfig.Project.VVVV.Executables.Clear()
        for exe in self.Config.Vvvv.Executables do
          let entry = new ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type()
          entry.Path <- exe.Executable;
          entry.Version <- exe.Version;
          entry.Required <- exe.Required
          IrisConfig.Project.VVVV.Executables.Add(entry)

        IrisConfig.Project.VVVV.Plugins.Clear()
        for plug in self.Config.Vvvv.Plugins do
          let entry = new ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type ()
          entry.Name <- plug.Name
          entry.Path <- plug.Path
          IrisConfig.Project.VVVV.Plugins.Add(entry)

        // Engine related configuration
        if Option.isSome self.Config.Engine.AesKey
        then IrisConfig.Project.Engine.AesKey <- Option.get self.Config.Engine.AesKey

        if Option.isSome self.Config.Engine.DefaultTimeout
        then
          let value = string (Option.get self.Config.Engine.DefaultTimeout)
          IrisConfig.Project.Engine.DefaultTimeout <- value

        if Option.isSome self.Config.Engine.DontCompress
        then
          let value = Option.get self.Config.Engine.DontCompress
          IrisConfig.Project.Engine.DontCompress <- value

        if Option.isSome self.Config.Engine.FastEthernet
        then
          let value = Option.get self.Config.Engine.FastEthernet
          IrisConfig.Project.Engine.FastEthernet <- value

        if Option.isSome self.Config.Engine.GracefulShutdown
        then
          let value = Option.get self.Config.Engine.GracefulShutdown
          IrisConfig.Project.Engine.GracefulShutdown <- value

        if Option.isSome self.Config.Engine.Hosts
        then
          IrisConfig.Project.Engine.Hosts.Clear()
          for host in Option.get self.Config.Engine.Hosts do
            IrisConfig.Project.Engine.Hosts.Add(host)

        if Option.isSome self.Config.Engine.IgnorePartitions
        then
          let value = Option.get self.Config.Engine.IgnorePartitions
          IrisConfig.Project.Engine.IgnorePartitions <- value

        if Option.isSome self.Config.Engine.IgnoreSmallPartitions
        then
          let value = Option.get self.Config.Engine.IgnoreSmallPartitions
          IrisConfig.Project.Engine.IgnoreSmallPartitions <- value

        if Option.isSome self.Config.Engine.InfiniBand
        then
          let value = Option.get self.Config.Engine.InfiniBand
          IrisConfig.Project.Engine.InfiniBand <- value

        if Option.isSome self.Config.Engine.Large
        then
          let value = Option.get self.Config.Engine.Large
          IrisConfig.Project.Engine.Large <- value

        if Option.isSome self.Config.Engine.LogDir
        then
          let value = Option.get self.Config.Engine.LogDir
          IrisConfig.Project.Engine.LogDir <- value

        if Option.isSome self.Config.Engine.Logged
        then
          let value = Option.get self.Config.Engine.Logged
          IrisConfig.Project.Engine.Logged <- value

        if Option.isSome self.Config.Engine.MCMDReportRate
        then
          let value = int (Option.get self.Config.Engine.MCMDReportRate)
          IrisConfig.Project.Engine.MCMDReportRate <- value

        if Option.isSome self.Config.Engine.MCRangeHigh
        then
          let value = Option.get self.Config.Engine.MCRangeHigh
          IrisConfig.Project.Engine.MCRangeHigh <- value

        if Option.isSome self.Config.Engine.MCRangeLow
        then
          let value = Option.get self.Config.Engine.MCRangeLow
          IrisConfig.Project.Engine.MCRangeLow <- value

        if Option.isSome self.Config.Engine.MaxAsyncMTotal
        then
          let value = int (Option.get self.Config.Engine.MaxAsyncMTotal)
          IrisConfig.Project.Engine.MaxAsyncMTotal <- value

        if Option.isSome self.Config.Engine.MaxIPMCAddrs
        then
          let value = int (Option.get self.Config.Engine.MaxIPMCAddrs)
          IrisConfig.Project.Engine.MaxIPMCAddrs <- value

        if Option.isSome self.Config.Engine.MaxMsgLen
        then
          let value = int (Option.get self.Config.Engine.MaxMsgLen)
          IrisConfig.Project.Engine.MaxMsgLen <- value

        if Option.isSome self.Config.Engine.Mute
        then
          let value = Option.get self.Config.Engine.Mute
          IrisConfig.Project.Engine.Mute <- value

        if Option.isSome self.Config.Engine.Netmask
        then
          let value = Option.get self.Config.Engine.Netmask
          IrisConfig.Project.Engine.Netmask <- value

        IrisConfig.Project.Engine.NetworkInterfaces.Clear()
        if Option.isSome self.Config.Engine.NetworkInterfaces
        then
          for iface in Option.get self.Config.Engine.NetworkInterfaces do
            IrisConfig.Project.Engine.NetworkInterfaces.Add(iface)

        if Option.isSome self.Config.Engine.OOBViaTCP
        then
          let value = Option.get self.Config.Engine.OOBViaTCP
          IrisConfig.Project.Engine.OOBViaTCP <- value

        if Option.isSome self.Config.Engine.Port
        then
          let value = int (Option.get self.Config.Engine.Port)
          IrisConfig.Project.Engine.Port <- value

        if Option.isSome self.Config.Engine.PortP2P
        then
          let value = int (Option.get self.Config.Engine.PortP2P)
          IrisConfig.Project.Engine.PortP2P <- value

        if Option.isSome self.Config.Engine.RateLim
        then
          let value = int (Option.get self.Config.Engine.RateLim)
          IrisConfig.Project.Engine.RateLim <- value

        if Option.isSome self.Config.Engine.Sigs
        then
          let value = Option.get self.Config.Engine.Sigs
          IrisConfig.Project.Engine.Sigs <- value

        if Option.isSome self.Config.Engine.SkipFirstInterface
        then
          let value = Option.get self.Config.Engine.SkipFirstInterface
          IrisConfig.Project.Engine.SkipFirstInterface <- value

        if Option.isSome self.Config.Engine.Subnet
        then
          let value = Option.get self.Config.Engine.Subnet
          IrisConfig.Project.Engine.Subnet <- value

        if Option.isSome self.Config.Engine.TTL
        then
          let value = int (Option.get self.Config.Engine.TTL)
          IrisConfig.Project.Engine.TTL <- value

        if Option.isSome self.Config.Engine.TokenDelay
        then
          let value = int (Option.get self.Config.Engine.TokenDelay)
          IrisConfig.Project.Engine.TokenDelay <- value

        if Option.isSome self.Config.Engine.UDPChkSum
        then
          let value = Option.get self.Config.Engine.UDPChkSum
          IrisConfig.Project.Engine.UDPChkSum <- value

        if Option.isSome self.Config.Engine.UnicastOnly
        then
          let value = Option.get self.Config.Engine.UnicastOnly
          IrisConfig.Project.Engine.UnicastOnly <- value

        if Option.isSome self.Config.Engine.UseIPv4
        then
          let value = Option.get self.Config.Engine.UseIPv4
          IrisConfig.Project.Engine.UseIPv4 <- value

        if Option.isSome self.Config.Engine.UseIPv6
        then
          let value = Option.get self.Config.Engine.UseIPv6
          IrisConfig.Project.Engine.UseIPv6 <- value

        if Option.isSome self.Config.Engine.UserDMA
        then
          let value = Option.get self.Config.Engine.UserDMA
          IrisConfig.Project.Engine.UserDMA <- value

        // Timing
        IrisConfig.Project.Timing.Framebase <- int (self.Config.Timing.Framebase)
        IrisConfig.Project.Timing.Input <- self.Config.Timing.Input

        IrisConfig.Project.Timing.Servers.Clear()
        for srv in self.Config.Timing.Servers do
          IrisConfig.Project.Timing.Servers.Add(srv)

        IrisConfig.Project.Timing.TCPPort <- int (self.Config.Timing.TCPPort)
        IrisConfig.Project.Timing.UDPPort <- int (self.Config.Timing.UDPPort)

        // Ports
        IrisConfig.Project.Ports.IrisService <- int (self.Config.Port.Iris)
        IrisConfig.Project.Ports.UDPCues <- int (self.Config.Port.UDPCue)
        IrisConfig.Project.Ports.WebSocket <- int (self.Config.Port.WebSocket)

        // Audio
        IrisConfig.Project.Audio.SampleRate <- int (self.Config.Audio.SampleRate)

        // ViewPorts
        IrisConfig.Project.ViewPorts.Clear()
        for vp in self.Config.ViewPorts do
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
        for dp in self.Config.Displays do
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
        for task in self.Config.Tasks do
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
        IrisConfig.Project.Cluster.Name <- self.Config.Cluster.Name

        for node in self.Config.Cluster.Nodes do
          let n = new ConfigFile.Project_Type.Cluster_Type.Nodes_Item_Type()
          n.Id       <- node.Id
          n.Ip       <- node.Ip
          n.HostName <- node.HostName
          n.Task     <- node.Task
          IrisConfig.Project.Cluster.Nodes.Add(n)

        for group in self.Config.Cluster.Groups do
          let g = new ConfigFile.Project_Type.Cluster_Type.Groups_Item_Type()
          g.Name <- group.Name

          for mem in group.Members do
            g.Members.Add(mem)

          IrisConfig.Project.Cluster.Groups.Add(g)

        // save everything!
        let destPath = Path.Combine(Option.get self.Path, self.Name + IrisExt)
        IrisConfig.Save(destPath)

        // commit project to git.
        match self.Repo with
          | Some(repo') ->
            repo'.Stage(destPath)
            repo'.Commit(msg, sign, Committer)
            |> ignore
          | None -> failwith "Saving without repository is unsupported. Aborting"


    //   ____ _
    //  / ___| | ___  _ __   ___
    // | |   | |/ _ \| '_ \ / _ \
    // | |___| | (_) | | | |  __/
    //  \____|_|\___/|_| |_|\___|
    // clone a project from a different host
    static member Clone(host : string, name : string) : FilePath option =
      let url = sprintf "git://%s/%s/.git" host name
      try
        let res = Repository.Clone(url, Path.Combine(Workspace(), name))
        logger "cloneProject" <| sprintf "clone result: %s" res
        Some(Path.Combine(Workspace(), name))
      with
        | _ -> None

    //  _   _           _       _
    // | | | |_ __   __| | __ _| |_ ___
    // | | | | '_ \ / _` |/ _` | __/ _ \
    // | |_| | |_) | (_| | (_| | ||  __/
    //  \___/| .__/ \__,_|\__,_|\__\___|
    //       |_|

    member self.Update(project : Project) =
      self.Data <- project.Data

    member self.Update(data : ProjectData) =
      self.Data <- data
