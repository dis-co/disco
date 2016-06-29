namespace Iris.Core

open System
open System.IO
open System.Linq
open System.Net
open System.Collections.Generic
open Iris.Core.Utils
open LibGit2Sharp

//  ____            _           _   ____        _
// |  _ \ _ __ ___ (_) ___  ___| |_|  _ \  __ _| |_ __ _
// | |_) | '__/ _ \| |/ _ \/ __| __| | | |/ _` | __/ _` |
// |  __/| | | (_) | |  __/ (__| |_| |_| | (_| | || (_| |
// |_|   |_|  \___// |\___|\___|\__|____/ \__,_|\__\__,_|
//               |__/

type ProjectData() =
  [<DefaultValue>] val mutable Id        : Guid
  [<DefaultValue>] val mutable Name      : string
  [<DefaultValue>] val mutable Path      : FilePath option
  [<DefaultValue>] val mutable LastSaved : DateTime option
  [<DefaultValue>] val mutable Copyright : string   option
  [<DefaultValue>] val mutable Author    : string   option
  [<DefaultValue>] val mutable Year      : int
  [<DefaultValue>] val mutable Config    : Config

  override self.GetHashCode() =
    hash self

  override self.Equals(other) =
    match other with
      | :? ProjectData as p ->
        (p.Id                    = self.Id)                    &&
        (p.Name                  = self.Name)                  &&
        (p.Path                  = self.Path)                  &&
        (p.LastSaved             = self.LastSaved)             &&
        (p.Copyright             = self.Copyright)             &&
        (p.Author                = self.Author)                &&
        (p.Year                  = self.Year)                  &&
        (p.Config.AudioConfig    = self.Config.AudioConfig)    &&
        (p.Config.VvvvConfig     = self.Config.VvvvConfig)     &&
        (p.Config.RaftConfig     = self.Config.RaftConfig)     &&
        (p.Config.TimingConfig   = self.Config.TimingConfig)   &&
        (p.Config.PortConfig     = self.Config.PortConfig)     &&
        (p.Config.ViewPorts      = self.Config.ViewPorts)      &&
        (p.Config.Displays       = self.Config.Displays)       &&
        (p.Config.Tasks          = self.Config.Tasks)          &&
        (p.Config.ClusterConfig  = self.Config.ClusterConfig)
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
  let committer =
    let hostname = Dns.GetHostName()
    new Signature("Iris", "iris@" + hostname, new DateTimeOffset(DateTime.Now))


  new (data : ProjectData) = new Project(None, data)

  //  ____                            _
  // |  _ \ _ __ ___  _ __   ___ _ __(_) ___  ___
  // | |_) | '__/ _ \| '_ \ / _ \ '__| |/ _ \/ __|
  // |  __/| | | (_) | |_) |  __/ |  | |  __/\__ \
  // |_|   |_|  \___/| .__/ \___|_|  |_|\___||___/
  //                 |_|
  member self.Id
    with get()    = data.Id
    and  set(id') = data.Id <- id'

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

  member self.CurrentBranch
    with get() : Branch option =
      match repo with
        | Some(repo') -> Some repo'.Head
        | _           -> None

  member self.SetBranch(name) : unit =
    match repo with
      | Some(repo') ->
        try 
          let branch = repo'.Branches.First(fun b -> b.CanonicalName = name)
          repo'.Checkout branch
          |> ignore
        with
          | _ -> ()
      | _ -> ()

  static member private Build(pid : string, name : string) : Project =
    let data = new ProjectData()
    data.Id        <- Guid.Parse(pid)
    data.Name      <- name
    data.Path      <- None
    data.Copyright <- None
    data.LastSaved <- None
    data.Year      <- System.DateTime.Now.Year
    data.Config    <- Config.Create(name)
    new Project(data)

  //  _____                  _ _ _
  // | ____|__ _ _   _  __ _| (_) |_ _   _
  // |  _| / _` | | | |/ _` | | | __| | | |
  // | |__| (_| | |_| | (_| | | | |_| |_| |
  // |_____\__, |\__,_|\__,_|_|_|\__|\__, |
  //          |_|                    |___/
  override self.GetHashCode() =
    hash self

  override self.Equals(other) =
    match other with
      | :? Project as p -> p.Data = self.Data
      | _ -> false

  // Static:
  //    ____                _
  //   / ___|_ __ ___  __ _| |_ ___
  //  | |   | '__/ _ \/ _` | __/ _ \
  //  | |___| | |  __/ (_| | ||  __/
  //   \____|_|  \___|\__,_|\__\___|
  //
  static member Create(name : string) =
    let guid = mkGuid()
    Project.Build(guid, name)

  //   _                    _
  //  | |    ___   __ _  __| |
  //  | |   / _ \ / _` |/ _` |
  //  | |__| (_) | (_| | (_| |
  //  |_____\___/ \__,_|\__,_|
  //
  /// Load a Project from Disk
  static member Load(path : FilePath) : Either<string, Project> =
    if not <| File.Exists(path) // must be a *File*
    then Fail("File not found!")
    else
      IrisConfig.Load(path)

      let meta = IrisConfig.Project.Metadata

      let date =
        if meta.LastSaved.Length > 0
        then
          try
            Some(DateTime.Parse(meta.LastSaved))
          with
            | _ -> None
        else None

      let project = Project.Build(meta.Id, meta.Name)
      let basedir = Path.GetDirectoryName(path)

      project.Repo      <- Some(new Repository(Path.Combine(basedir, ".git")))
      project.Path      <- Some(Path.GetDirectoryName(path))
      project.LastSaved <- date
      project.Copyright <- Config.ParseStringProp meta.Copyright
      project.Author    <- Config.ParseStringProp meta.Author
      project.Year      <- meta.Year
      project.Config    <- Config.FromFile(IrisConfig)
      Success(project)

  //   ____
  //  / ___|  __ ___   _____
  //  \___ \ / _` \ \ / / _ \
  //   ___) | (_| |\ V /  __/
  //  |____/ \__,_| \_/ \___|
  //
  /// Save a Project to Disk
  member self.Save(sign : Signature, msg : string) : Either<string,Commit> =
    if Option.isSome self.Path
    then
      Directory.CreateDirectory (Option.get self.Path) |> ignore

      if Option.isNone self.Repo
      then
        let path = Repository.Init(Option.get self.Path)
        File.WriteAllText(Path.Combine(path, "git-daemon-export-ok"), "")
        self.Repo <- Some(new Repository(path))

      // Project metadata
      IrisConfig.Project.Metadata.Id   <- self.Id.ToString()
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
      for exe in self.Config.VvvvConfig.Executables do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Executables_Item_Type()
        entry.Path <- exe.Executable;
        entry.Version <- exe.Version;
        entry.Required <- exe.Required
        IrisConfig.Project.VVVV.Executables.Add(entry)

      IrisConfig.Project.VVVV.Plugins.Clear()
      for plug in self.Config.VvvvConfig.Plugins do
        let entry = new ConfigFile.Project_Type.VVVV_Type.Plugins_Item_Type ()
        entry.Name <- plug.Name
        entry.Path <- plug.Path
        IrisConfig.Project.VVVV.Plugins.Add(entry)

      // Timing
      IrisConfig.Project.Timing.Framebase <- int (self.Config.TimingConfig.Framebase)
      IrisConfig.Project.Timing.Input <- self.Config.TimingConfig.Input

      IrisConfig.Project.Timing.Servers.Clear()
      for srv in self.Config.TimingConfig.Servers do
        IrisConfig.Project.Timing.Servers.Add(srv)

      IrisConfig.Project.Timing.TCPPort <- int (self.Config.TimingConfig.TCPPort)
      IrisConfig.Project.Timing.UDPPort <- int (self.Config.TimingConfig.UDPPort)

      // Ports
      IrisConfig.Project.Ports.IrisService <- int (self.Config.PortConfig.Iris)
      IrisConfig.Project.Ports.UDPCues <- int (self.Config.PortConfig.UDPCue)
      IrisConfig.Project.Ports.WebSocket <- int (self.Config.PortConfig.WebSocket)

      // Audio
      IrisConfig.Project.Audio.SampleRate <- int (self.Config.AudioConfig.SampleRate)

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
      IrisConfig.Project.Cluster.Name <- self.Config.ClusterConfig.Name

      for node in self.Config.ClusterConfig.Nodes do
        let n = new ConfigFile.Project_Type.Cluster_Type.Nodes_Item_Type()
        n.Id       <- node.Id
        n.Ip       <- node.Ip
        n.HostName <- node.HostName
        n.Task     <- node.Task
        IrisConfig.Project.Cluster.Nodes.Add(n)

      for group in self.Config.ClusterConfig.Groups do
        let g = new ConfigFile.Project_Type.Cluster_Type.Groups_Item_Type()
        g.Name <- group.Name

        for mem in group.Members do
          g.Members.Add(mem)

        IrisConfig.Project.Cluster.Groups.Add(g)

      // save everything!
      let destPath = Path.Combine(Option.get self.Path, self.Name + IrisExt)

      try 
        IrisConfig.Save(destPath)

        // commit project to git.
        match self.Repo with
          | Some(repo') ->
            Commands.Stage(repo',destPath)
            repo'.Commit(msg, sign, committer)
            |> Success
          | _ ->
            Fail "Saving without repository is unsupported. Aborting"
      with
        | exn -> Fail exn.Message
    else Fail "Cannot save without path."

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
