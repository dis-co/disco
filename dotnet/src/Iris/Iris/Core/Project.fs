namespace Iris.Core

open System
open System.IO
open System.Linq
open System.Net
open System.Collections.Generic
open LibGit2Sharp
open Iris.Core.Utils
open FSharpx.Functional
open Iris.Raft

//  ____            _           _
// |  _ \ _ __ ___ (_) ___  ___| |_
// | |_) | '__/ _ \| |/ _ \/ __| __|
// |  __/| | | (_) | |  __/ (__| |_
// |_|   |_|  \___// |\___|\___|\__|
//               |__/

// [<NoComparison;NoEquality>]
type Project =
  { Id        : Id
  ; Name      : Name
  ; Path      : FilePath  option        // Project path should always be the path containing '.git'
  ; CreatedOn : TimeStamp
  ; LastSaved : TimeStamp option
  ; Copyright : string    option
  ; Author    : string    option
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
      | Some path -> Git.Repo.repository path
      | _         -> ProjectPathError |> Either.fail

  let currentBranch (project: Project) =
    repository project
    |> Either.map Git.Branch.current

  let checkoutBranch (name: string) (project: Project) =
    repository project
    |> Either.bind (Git.Repo.checkout name)

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: Project
  let createProject (name : string) : Project =
    { Id        = Id.Create()
    ; Name      = name
    ; Path      = None
    ; CreatedOn = createTimestamp()
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
  let loadProject (path : FilePath) : Either<IrisError,Project> =
    if not (File.Exists path) then
      ProjectNotFound path |> Either.fail
    else
      IrisConfig.Load(path)

      let meta = IrisConfig.Project.Metadata
      let lastSaved =
        match meta.LastSaved with
          | null | "" -> None
          | str ->
            try
              DateTime.Parse str |> ignore
              Some str
            with
              | _ -> None

      { Id        = Id.Parse meta.Id
      ; Name      = meta.Name
      ; Path      = Some <| Path.GetDirectoryName(path)
      ; CreatedOn = meta.CreatedOn
      ; LastSaved = lastSaved
      ; Copyright = parseStringProp meta.Copyright
      ; Author    = parseStringProp meta.Author
      ; Config    = Config.FromFile(IrisConfig) }
      |> Either.succeed

  let writeDaemonExportFile (repo: Repository) =
    File.WriteAllText(Path.Combine(repo.Info.Path, "git-daemon-export-ok"), "")

  let writeGitIgnoreFile (repo: Repository) =
    let gitignore = @"
/.raft
    "
    let parent = Git.Repo.parentPath repo
    let path = parent </> ".gitignore"

    File.WriteAllText(path, gitignore)
    Git.Repo.stage repo path

  let createAssetDir (repo: Repository) (dir: FilePath) =
    let parent = Git.Repo.parentPath repo
    let target = parent </> dir
    if not (Directory.Exists target) && not (File.Exists target) then
      Directory.CreateDirectory target |> ignore
      let gitkeep = target </> ".gitkeep"
      File.WriteAllText(gitkeep, "")
      Git.Repo.stage repo gitkeep

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let initRepo (project: Project) : Either<IrisError,Repository> =
    let initRepoImpl (repo: Repository) =
      writeDaemonExportFile repo
      writeGitIgnoreFile repo
      createAssetDir repo "cues"
      createAssetDir repo "cuelists"
      createAssetDir repo "users"
      repo
    match project.Path with
    | Some path ->
      path
      |> Git.Repo.init
      |> Either.map initRepoImpl
    | _ ->
      ProjectPathError
      |> Either.fail

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

    config.Project.Metadata.CreatedOn <- project.CreatedOn

    let ts = createTimestamp()
    config.Project.Metadata.LastSaved <- ts

    { project with LastSaved = Some ts }

  /// ## commitPath
  ///
  /// commit a file at given path to git
  ///
  /// ### Signature:
  /// - committer : Signature of committer
  /// - msg       : commit msg
  /// - filepath  : path to file being committed
  /// - project   : Project
  ///
  /// Returns: (Commit * Project) option
  let commitPath (committer: Signature) (msg : string) (filepath: FilePath) (project: Project) : Either<IrisError,(Commit * Project)> =
    let doCommit repo =
      try
        let parent = Git.Repo.parentPath repo
        let abspath =  parent </> filepath

        // FIXME: need to do some checks on repository before...
        // create git commit
        Git.Repo.stage repo abspath
        Git.Repo.commit repo msg committer
        |> fun commit -> Either.succeed (commit, project)
      with
        | exn ->
          exn.Message
          |> CommitError
          |> Either.fail

    match repository project with
    | Left _ ->
      match initRepo project with
      | Right repo -> doCommit repo
      | Left error -> Left error
    | Right repo -> doCommit repo

  let saveFile (committer: Signature) (msg : string) (path: FilePath) (project: Project) : Either<IrisError,(Commit * Project)> =
    commitPath committer msg path project

  let saveProject (committer: Signature) (msg : string) (project: Project) : Either<IrisError,(Commit * Project)> =
    match project.Path with
    | Some path ->
      if not (Directory.Exists path) then
        Directory.CreateDirectory path |> ignore

      let project =
        IrisConfig
        |> toFile project.Config
        |> saveMetadata project

      // save everything!
      let destPath = Path.Combine(path, PROJECT_FILENAME)

      try
        IrisConfig.Save(destPath)
        commitPath committer msg destPath project
      with
        | exn ->
          exn.Message
          |> ProjectSaveError
          |> Either.fail
    | _ ->
      ProjectPathError
      |> Either.fail

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

  let getConfig (project: Project) : Config = project.Config

  let updatePath (path: FilePath) (project: Project) : Project =
    { project with Path = Some path }

  let updateConfig (config: Config) (project: Project) : Project =
    { project with Config = config }

  let updateDataDir (raftDir: FilePath) (project: Project) : Project =
    { project.Config.RaftConfig with DataDir = raftDir }
    |> flip updateEngine project.Config
    |> flip updateConfig project

  let addMember (node: RaftNode) (project: Project) : Project =
    project.Config
    |> addNodeConfig node
    |> flip updateConfig project

  let addMembers (nodes: RaftNode list) (project: Project) : Project =
    List.fold
      (fun config (node: RaftNode) ->
        addNodeConfig node config)
      project.Config
      nodes
    |> flip updateConfig project
