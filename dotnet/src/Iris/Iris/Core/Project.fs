namespace Iris.Core

// * Imports

open System
open System.IO
open System.Linq
open System.Net
open System.Collections.Generic
open LibGit2Sharp
open Iris.Core.Utils
open FSharpx.Functional
open Iris.Raft

// * IrisProject

//  ____            _           _
// |  _ \ _ __ ___ (_) ___  ___| |_
// | |_) | '__/ _ \| |/ _ \/ __| __|
// |  __/| | | (_) | |  __/ (__| |_
// |_|   |_|  \___// |\___|\___|\__|
//               |__/

type IrisProject =
  { Id        : Id
  ; Name      : Name
  ; Path      : FilePath                // project path should always be the path containing '.git'
  ; CreatedOn : TimeStamp
  ; LastSaved : TimeStamp option
  ; Copyright : string    option
  ; Author    : string    option
  ; Config    : IrisConfig }

// * Project module

[<RequireQualifiedAccess>]
module Project =

  // ** repository

  /// ### Retrieve git repository
  ///
  /// Computes the path to the passed projects' git repository from its `Path` field and checks
  /// whether it exists. If so, construct a git Repository object and return that.
  ///
  /// # Returns: Repository option
  let repository (project: IrisProject) =
      Git.Repo.repository project.Path

  // ** currentBranch

  let currentBranch (project: IrisProject) =
    repository project
    |> Either.map Git.Branch.current

  // ** checkoutBranch

  let checkoutBranch (name: string) (project: IrisProject) =
    repository project
    |> Either.bind (Git.Repo.checkout name)

  // ** create

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: IrisProject
  let create (name : string) : IrisProject =
    { Id        = Id.Create()
    ; Name      = name
    ; Path      = Path.GetFullPath(".") </> name
    ; CreatedOn = Time.createTimestamp()
    ; LastSaved = None
    ; Copyright = None
    ; Author    = None
    ; Config    = Config.create(name) }

  // ** parseLastSaved

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let parseLastSaved (config: ConfigFile) =
    let meta = config.Project.Metadata
    if meta.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(meta.LastSaved))
      with
        | _ -> None
    else None

  // ** parseCreatedOn

  /// ### Parse the CreatedOn property
  ///
  /// Parse the CreatedOn property in a given ConfigFile. If the field is empty or DateTime.Parse
  /// fails to read it, the date returned will be the begin of the epoch.
  ///
  /// # Returns: DateTime
  let parseCreatedOn (config: ConfigFile) =
    let meta = config.Project.Metadata
    if meta.CreatedOn.Length > 0
    then
      try
        DateTime.Parse(meta.CreatedOn)
      with
        | _ -> DateTime.FromFileTimeUtc(int64 0)
    else DateTime.FromFileTimeUtc(int64 0)

  // ** load

  /// ### Load a project from disk
  ///
  /// Attempts to load a serializad project file from the specified location.
  ///
  /// # Returns: IrisProject option
  let load (path : FilePath) : Either<IrisError,IrisProject> =
    either {
      if not (File.Exists path) then
        return!
          ProjectNotFound path
          |> Either.fail
      else
        try
          let config = ConfigFile()
          config.Load(path)

          let meta = config.Project.Metadata
          let lastSaved =
            match meta.LastSaved with
              | null | "" -> None
              | str ->
                try
                  DateTime.Parse str |> ignore
                  Some str
                with
                  | _ -> None

          let! config = Config.fromFile config

          let normalizedPath =
            if Path.IsPathRooted path then
              path
            else
              Path.GetFullPath path

          return { Id        = Id meta.Id
                   Name      = meta.Name
                   Path      = Path.GetDirectoryName(normalizedPath)
                   CreatedOn = meta.CreatedOn
                   LastSaved = lastSaved
                   Copyright = Config.parseStringProp meta.Copyright
                   Author    = Config.parseStringProp meta.Author
                   Config    = config }
        with
          | exn ->
            return!
              sprintf "Could not load Project: %s" exn.Message
              |> ProjectParseError
              |> Either.fail
    }

  // ** writeDaemonExportFile

  let writeDaemonExportFile (repo: Repository) =
    File.WriteAllText(repo.Info.Path </> "git-daemon-export-ok", "")

  // ** writeGitIgnoreFile

  let writeGitIgnoreFile (repo: Repository) =
    let gitignore = @"
/.raft
    "
    let parent = Git.Repo.parentPath repo
    let path = parent </> ".gitignore"

    File.WriteAllText(path, gitignore)
    Git.Repo.stage repo path

  // ** createAssetDir

  let createAssetDir (repo: Repository) (dir: FilePath) =
    let parent = Git.Repo.parentPath repo
    let target = parent </> dir
    if not (Directory.Exists target) && not (File.Exists target) then
      Directory.CreateDirectory target |> ignore
      let gitkeep = target </> ".gitkeep"
      File.WriteAllText(gitkeep, "")
      Git.Repo.stage repo gitkeep

  // ** initRepo

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let initRepo (project: IrisProject) : Either<IrisError,Repository> =
    let initRepoImpl (repo: Repository) =
      writeDaemonExportFile repo
      writeGitIgnoreFile repo
      createAssetDir repo "cues"
      createAssetDir repo "cuelists"
      createAssetDir repo "users"
      repo
    project.Path
    |> Git.Repo.init
    |> Either.map initRepoImpl

  // ** saveMetadata

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
  /// # Returns: IrisProject
  let private saveMetadata (project: IrisProject) (config: ConfigFile)  =
    // Project metadata
    config.Project.Metadata.Id   <- string project.Id
    config.Project.Metadata.Name <- project.Name

    if Option.isSome project.Author then
      config.Project.Metadata.Author <- Option.get project.Author

    if Option.isSome project.Copyright then
      config.Project.Metadata.Copyright <- Option.get project.Copyright

    config.Project.Metadata.CreatedOn <- project.CreatedOn

    let ts = Time.createTimestamp()
    config.Project.Metadata.LastSaved <- ts

    { project with LastSaved = Some ts }

  // ** commitPath

  /// ## commitPath
  ///
  /// commit a file at given path to git
  ///
  /// ### Signature:
  /// - committer : Signature of committer
  /// - msg       : commit msg
  /// - filepath  : path to file being committed
  /// - project   : IrisProject
  ///
  /// Returns: (Commit * IrisProject) option
  let commitPath (committer: Signature) (msg : string) (filepath: FilePath) (project: IrisProject) : Either<IrisError,(Commit * IrisProject)> =
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

  // ** saveFile

  let saveFile (committer: Signature) (msg : string) (path: FilePath) (project: IrisProject) : Either<IrisError,(Commit * IrisProject)> =
    commitPath committer msg path project

  // ** save

  let save (committer: Signature) (msg : string) (project: IrisProject) : Either<IrisError,(Commit * IrisProject)> =
    if not (Directory.Exists project.Path) then
      Directory.CreateDirectory project.Path |> ignore

    let config = ConfigFile()

    let project =
      config
      |> Config.toFile project.Config
      |> saveMetadata project

    // save everything!
    let destPath = project.Path </> PROJECT_FILENAME + ASSET_EXTENSION

    try
      config.Save(destPath)
      commitPath committer msg destPath project
    with
      | exn ->
        exn.Message
        |> ProjectSaveError
        |> Either.fail

  // ** clone

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
      Some(destination </> name)
    with
      | _ -> None

  // ** config

  let config (project: IrisProject) : IrisConfig = project.Config

  // ** updatePath

  let updatePath (path: FilePath) (project: IrisProject) : IrisProject =
    { project with Path = path }

  // ** filePath

  let filePath (project: IrisProject) : FilePath =
    project.Path </> PROJECT_FILENAME + ASSET_EXTENSION

  // ** updateConfig

  let updateConfig (config: IrisConfig) (project: IrisProject) : IrisProject =
    { project with Config = config }

  // ** updateDataDir

  let updateDataDir (raftDir: FilePath) (project: IrisProject) : IrisProject =
    { project.Config.RaftConfig with DataDir = raftDir }
    |> flip Config.updateEngine project.Config
    |> flip updateConfig project

  // ** addMember

  let addMember (node: RaftNode) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.addNode node
    |> flip updateConfig project

  // ** addMembers

  let addMembers (nodes: RaftNode list) (project: IrisProject) : IrisProject =
    List.fold
      (fun config (node: RaftNode) ->
        Config.addNode node config)
      project.Config
      nodes
    |> flip updateConfig project
