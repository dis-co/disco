namespace Iris.Core

open System
open System.IO
open System.Linq
open System.Net
open System.Collections.Generic
open Iris.Core.Utils
open LibGit2Sharp

(***************************************************************************************************

  //   ____                          _ _   _
  //  / ___|___  _ __ ___  _ __ ___ (_) |_| |_ ___ _ __
  // | |   / _ \| '_ ` _ \| '_ ` _ \| | __| __/ _ \ '__|
  // | |__| (_) | | | | | | | | | | | | |_| ||  __/ |
  //  \____\___/|_| |_| |_|_| |_| |_|_|\__|\__\___|_| is Iris
  let committer =
    let hostname = Dns.GetHostName()
    new Signature("Iris", "iris@" + hostname, new DateTimeOffset(DateTime.Now))

***************************************************************************************************)

//  ____            _           _
// |  _ \ _ __ ___ (_) ___  ___| |_
// | |_) | '__/ _ \| |/ _ \/ __| __|
// |  __/| | | (_) | |  __/ (__| |_
// |_|   |_|  \___// |\___|\___|\__|
//               |__/

type Project =
  { Id        : ProjectId
  ; Name      : Name
  ; Path      : FilePath option
  ; CreatedOn : DateTime
  ; LastSaved : DateTime option
  ; Copyright : string   option
  ; Author    : string   option
  ; Config    : Config }

[<AutoOpen>]
module ProjectHelper =

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.
  ///
  /// # Returns: Repository option
  let repository (project: Project) =
    match project.Path with
      | Some path ->
        let basedir = Path.GetDirectoryName(path)
        match Directory.Exists basedir with
          | true ->
            new Repository(Path.Combine(basedir, ".git")) |> Some
          | _ -> None
      | _ -> None

  let branch (repo: Repository) = repo.Head

  let currentBranch (project: Project) =
    repository project
    |> Option.map branch

  let checkout (name: string) (repo: Repository) =
    try
      repo.Branches.First(fun b -> b.CanonicalName = name)
      |> repo.Checkout
      |> Some
    with
      | _ -> None

  let checkoutBranch (name: string) (project: Project) =
    repository project
    |> Option.bind (checkout name)

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: Project
  let create (name : string) : Project =
    let now = System.DateTime.Now
    { Id        = Id.Create()
    ; Name      = name
    ; Path      = None
    ; CreatedOn = now
    ; LastSaved = None
    ; Copyright = None
    ; Author    = None
    ; Config    = Config.Create(name) }

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let parseLastSaved (config: ConfigFile) =
    let meta = IrisConfig.Project.Metadata
    if meta.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(meta.LastSaved))
      with
        | _ -> None
    else None

  /// ### Parse the CreatedOn property
  ///
  /// Parse the CreatedOn property in a given ConfigFile. If the field is empty or DateTime.Parse
  /// fails to read it, the date returned will be the begin of the epoch.
  ///
  /// # Returns: DateTime
  let parseCreatedOn (config: ConfigFile) =
    let meta = IrisConfig.Project.Metadata
    if meta.CreatedOn.Length > 0
    then
      try
        DateTime.Parse(meta.CreatedOn)
      with
        | _ -> DateTime.FromFileTimeUtc(int64 0)
    else DateTime.FromFileTimeUtc(int64 0)

  /// ### Load a project from disk
  ///
  /// Attempts to load a serializad project file from the specified location.
  ///
  /// # Returns: Project option
  let load (path : FilePath) : Project option =
    if not <| File.Exists(path) then
      None
    else
      IrisConfig.Load(path)

      let meta = IrisConfig.Project.Metadata
      let lastSaved = parseLastSaved IrisConfig
      let createdOn = parseCreatedOn IrisConfig

      { Id        = Id.Parse meta.Id
      ; Name      = meta.Name
      ; Path      = Some <| Path.GetDirectoryName(path)
      ; CreatedOn = createdOn
      ; LastSaved = lastSaved
      ; Copyright = parseStringProp meta.Copyright
      ; Author    = parseStringProp meta.Author
      ; Config    = Config.FromFile(IrisConfig) }
      |> Some

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let initRepo (project: Project) : Repository option =
    match project.Path with
      | Some path ->
        try
          Repository.Init path
          |> fun rpath ->
            File.WriteAllText(Path.Combine(rpath, "git-daemon-export-ok"), "")
            new Repository(rpath)
            |> Some
        with
          | _ -> None
      | _ -> None

  //   ____             __ _                       _   _
  //  / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
  // | |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
  // | |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
  //  \____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
  //                         |___/

  /// ### Save metadata portion of project
  ///
  /// Save the metadata portion of the handed project value by *implicitly* mutating the handed
  /// config file object. As we want to keep track of the last moment a project was saved, we update
  /// the project value with the new time stamp.
  ///
  /// # Returns: Project
  let private saveMetadata (project: Project) (config: ConfigFile)  =
    // Project metadata
    config.Project.Metadata.Id   <- string project.Id
    config.Project.Metadata.Name <- project.Name

    if Option.isSome project.Author then
      config.Project.Metadata.Author <- Option.get project.Author

    if Option.isSome project.Copyright then
      config.Project.Metadata.Copyright <- Option.get project.Copyright

    config.Project.Metadata.CreatedOn <- project.CreatedOn.ToLongTimeString()

    let now = DateTime.Now
    config.Project.Metadata.LastSaved <- string now

    { project with LastSaved = Some now }

  //   ____
  //  / ___|  __ ___   _____
  //  \___ \ / _` \ \ / / _ \
  //   ___) | (_| |\ V /  __/
  //  |____/ \__,_| \_/ \___|
  //
  /// Save a Project to Disk
  let save (committer: Signature) (msg : string) (project: Project) : (Commit * Project) option =
    match project.Path with
      | Some path ->
        let repo =
          match repository project with
            | Some repo -> repo
            | _ ->
              match initRepo project with
                | Some repo -> repo
                | _ ->
                  printfn "Unable to get/initialize git repository."
                  exit 1

        let project =
          IrisConfig
          |> toFile project.Config
          |> saveMetadata project

        // save everything!
        let destPath = Path.Combine(path, project.Name + IrisExt)

        try
          IrisConfig.Save(destPath)
          // create git commit
          Commands.Stage(repo, destPath)
          repo.Commit(msg, committer, committer)
          |> fun commit -> Some (commit, project)
        with
          | exn -> None
      | _ -> None


  //   ____ _
  //  / ___| | ___  _ __   ___
  // | |   | |/ _ \| '_ \ / _ \
  // | |___| | (_) | | | |  __/
  //  \____|_|\___/|_| |_|\___|
  // clone a project from a different host
  let clone (host : string) (name : string) (destination: FilePath) : FilePath option =
    let url = sprintf "git://%s/%s/.git" host name
    try
      let res = Repository.Clone(url, Path.Combine(destination, name))
      logger "cloneProject" <| sprintf "clone result: %s" res
      Some(Path.Combine(destination, name))
    with
      | _ -> None


  /// ## Retrieve current repository status object
  ///
  /// Retrieve status information on the current repository
  ///
  /// ### Signature:
  /// - repo: Repository to fetch status for
  ///
  /// Returns: RepositoryStatus
  let retrieveStatus (repo: Repository) : RepositoryStatus =
    repo.RetrieveStatus()


  /// ## Check if repository is currently dirty
  ///
  /// Check if the current repository is dirty or nor.
  ///
  /// ### Signature:
  /// - repo: Repository to check
  ///
  /// Returns: boolean
  let isDirty (repo: Repository) : bool =
    retrieveStatus repo |> fun status -> status.IsDirty


  /// ## Shorthand to work with the commit log of a repository
  ///
  /// Get the list of commits for a repository.
  ///
  /// ### Signature:
  /// - repo: Repository to get commits for
  ///
  /// Returns: IQueryableCommitLog
  let commits (repo: Repository) : IQueryableCommitLog =
    repo.Commits

  let elementAt (idx: int) (t: IQueryableCommitLog) : Commit =
    t.ElementAt(idx)

  let commitCount (repo: Repository) =
    commits repo |> fun lst -> lst.Count()

  //  _____      _                 _
  // | ____|_  _| |_ ___ _ __  ___(_) ___  _ __  ___
  // |  _| \ \/ / __/ _ \ '_ \/ __| |/ _ \| '_ \/ __|
  // | |___ >  <| ||  __/ | | \__ \ | (_) | | | \__ \
  // |_____/_/\_\\__\___|_| |_|___/_|\___/|_| |_|___/

  type Project with

    static member Create (name: string) = create name

    static member Load (path: FilePath) = load path

    member self.Save (committer: Signature, msg : string) : (Commit * Project) option =
      save committer msg self
