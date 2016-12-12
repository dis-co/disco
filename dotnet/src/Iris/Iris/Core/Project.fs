namespace Iris.Core

// * Imports

open System
open System.IO
open System.Linq
open System.Net
open System.Text
open System.Collections.Generic
open LibGit2Sharp
open Iris.Core.Utils
open FSharpx.Functional
open Iris.Raft

#if FABLE_COMPILER

#else

open FlatBuffers
open Iris.Serialization.Raft

#endif

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

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = builder.CreateString (string self.Id)
    let name = builder.CreateString self.Name
    let path = builder.CreateString self.Path
    let created = builder.CreateString (string self.CreatedOn)
    let lastsaved = Option.map builder.CreateString self.LastSaved
    let copyright = Option.map builder.CreateString self.Copyright
    let author = Option.map builder.CreateString self.Author
    let config = Binary.toOffset builder self.Config

    ProjectFB.StartProjectFB(builder)
    ProjectFB.AddId(builder, id)
    ProjectFB.AddName(builder, name)
    ProjectFB.AddPath(builder, path)
    ProjectFB.AddCreatedOn(builder, created)

    match lastsaved with
    | Some offset -> ProjectFB.AddLastSaved(builder,offset)
    | _ -> ()

    match copyright with
    | Some offset -> ProjectFB.AddCopyright(builder,offset)
    | _ -> ()

    match author with
    | Some offset -> ProjectFB.AddAuthor(builder,offset)
    | _ -> ()

    ProjectFB.AddConfig(builder, config)
    ProjectFB.EndProjectFB(builder)

  static member FromFB(fb: ProjectFB) =
    failwith "Project.FromFB"

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
    either {
      let! repo = repository project
      return Git.Branch.current repo
    }

  // ** checkoutBranch

  let checkoutBranch (name: string) (project: IrisProject) =
    either {
      let! repo = repository project
      return! Git.Repo.checkout name repo
    }

  // ** create

  /// ### Create a new project with the given name
  ///
  /// Create a new project with the given name. The default configuration will apply.
  ///
  /// # Returns: IrisProject
  let create (name : string) (machine: IrisMachine) : IrisProject =
    { Id        = Id.Create()
    ; Name      = name
    ; Path      = Environment.CurrentDirectory </> name
    ; CreatedOn = Time.createTimestamp()
    ; LastSaved = None
    ; Copyright = None
    ; Author    = None
    ; Config    = Config.create name machine  }

  // ** parseLastSaved (private)

  /// ### Parses the LastSaved property.
  ///
  /// Attempt to parse the LastSaved proptery from the passed `ConfigFile`.
  ///
  /// # Returns: DateTime option
  let private parseLastSaved (config: ProjectYaml) =
    let meta = config.Project.Metadata
    if meta.LastSaved.Length > 0
    then
      try
        Some(DateTime.Parse(meta.LastSaved))
      with
        | _ -> None
    else None

  // ** parseCreatedOn (private)

  /// ### Parse the CreatedOn property
  ///
  /// Parse the CreatedOn property in a given ConfigFile. If the field is empty or DateTime.Parse
  /// fails to read it, the date returned will be the begin of the epoch.
  ///
  /// # Returns: DateTime
  let private parseCreatedOn (config: ProjectYaml) =
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
  let load (path : FilePath) (machine: IrisMachine) : Either<IrisError,IrisProject> =
    either {
      if not (File.Exists path) then
        return!
          sprintf "Project Not Found: %s" path
          |> Error.asProjectError "Project.load"
          |> Either.fail
      else
        try
          let config = ProjectYaml()
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

          let! config = Config.fromFile config machine

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
              |> Error.asProjectError "Project.load"
              |> Either.fail
    }

  //  ____       _   _
  // |  _ \ __ _| |_| |__  ___
  // | |_) / _` | __| '_ \/ __|
  // |  __/ (_| | |_| | | \__ \
  // |_|   \__,_|\__|_| |_|___/

  // ** filePath

  let filePath (project: IrisProject) : FilePath =
    project.Path </> PROJECT_FILENAME + ASSET_EXTENSION

  // ** userDir

  let userDir (project: IrisProject) : FilePath =
    project.Path </> USER_DIR

  // ** cueDir

  let cueDir (project: IrisProject) : FilePath =
    project.Path </> CUE_DIR

  // ** cuelistDir

  let cuelistDir (project: IrisProject) : FilePath =
    project.Path </> CUELIST_DIR

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // ** writeDaemonExportFile (private)

  let private writeDaemonExportFile (repo: Repository) =
    either {
      let path = repo.Info.Path </> "git-daemon-export-ok"
      let! _ = Asset.save path ""
      return ()
    }

  // ** writeGitIgnoreFile (private)

  let private writeGitIgnoreFile (repo: Repository) =
    either {
      let parent = Git.Repo.parentPath repo
      let path = parent </> ".gitignore"
      let! _ = Asset.save path GITIGNORE
      do! Git.Repo.stage repo path
    }

  // ** createAssetDir (private)

  let private createAssetDir (repo: Repository) (dir: FilePath) =
    either {
      let parent = Git.Repo.parentPath repo
      let target = parent </> dir
      do! FileSystem.mkDir target
      let gitkeep = target </> ".gitkeep"
      let! _ = Asset.save gitkeep ""
      do! Git.Repo.stage repo gitkeep
    }

  // ** saveMetadata (private)

  /// ### Save metadata portion of project
  ///
  /// Save the metadata portion of the handed project value by *implicitly* mutating the handed
  /// config file object. As we want to keep track of the last moment a project was saved, we update
  /// the project value with the new time stamp.
  ///
  /// # Returns: IrisProject
  let private toFile (project: IrisProject) (config: ProjectYaml)  =
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

  // ** commitPath (private)

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
  let private commitPath (filepath: FilePath)
                         (committer: Signature)
                         (msg : string)
                         (project: IrisProject) :
                         Either<IrisError,(Commit * IrisProject)> =
    either {
      let! repo = repository project
      let abspath =
        if Path.IsPathRooted filepath then
          filepath
        else
          project.Path </> filepath
      do! Git.Repo.stage repo abspath
      let! commit = Git.Repo.commit repo msg committer
      return commit, project
    }

  // ** saveFile

  let saveFile (path: FilePath)
               (contents: string)
               (committer: Signature)
               (msg : string)
               (project: IrisProject) :
               Either<IrisError,(Commit * IrisProject)> =

    either {
      let info = FileInfo path
      do! FileSystem.mkDir info.Directory.FullName
      let! info = Asset.save path contents
      return! commitPath path committer msg project
    }

  // ** deleteFile

  let deleteFile (path: FilePath)
                 (committer: Signature)
                 (msg : string)
                 (project: IrisProject) :
                 Either<IrisError,(Commit * IrisProject)> =
    either {
      let info = FileInfo path
      let! result = Asset.delete path
      return! commitPath path committer msg project
    }

  // ** saveAsset

  /// ## saveAsset
  ///
  /// Attempt to save the passed thing, and, if succesful, return its
  /// FileInfo object.
  ///
  /// ### Signature:
  /// - thing: ^t the thing to save. Must implement certain methods/getters
  /// - committer: User the thing to save. Must implement certain methods/getters
  /// - project: Project to save file into
  ///
  /// Returns: Either<IrisError,Commit * Project>
  let inline saveAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let payload = thing |> Yaml.encode
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = sprintf "%s save %A" committer.UserName filepath
    saveFile filepath payload signature msg project

  // ** deleteAsset

  /// ## deleteAsset
  ///
  /// Delete a file path from disk and commit the change to git.
  ///
  /// ### Signature:
  /// - thing: ^t thing to delete
  /// - committer: User committing the change
  /// - msg: User committing the change
  /// - project: IrisProject to work on
  ///
  /// Returns: Either<IrisError, FileInfo * Commit * Project>
  let inline deleteAsset (thing: ^t) (committer: User) (project: IrisProject) =
    let filepath = project.Path </> Asset.path thing
    let signature = committer.Signature
    let msg = sprintf "%s deleted %A" committer.UserName filepath
    deleteFile filepath signature msg project


  let private needsInit (project: IrisProject) =
    let projdir = Directory.Exists project.Path
    let git = Directory.Exists (project.Path </> ".git")
    let cues = Directory.Exists (project.Path </> CUE_DIR)
    let cuelists = Directory.Exists (project.Path </> CUELIST_DIR)
    let users = Directory.Exists (project.Path </> USER_DIR)

    (not git)      ||
    (not cues)     ||
    (not cuelists) ||
    (not users)    ||
    (not projdir)

  // ** initRepo (private)

  /// ### Initialize the project git repository
  ///
  /// Given a project value, attempt to work out whether a git repository at that location already
  /// exists, otherwise creating it.
  ///
  /// # Returns: Repository
  let private initRepo (project: IrisProject) : Either<IrisError,unit> =
    either {
      let! repo = Git.Repo.init project.Path
      do! writeDaemonExportFile repo
      do! writeGitIgnoreFile repo
      do! createAssetDir repo CUE_DIR
      do! createAssetDir repo USER_DIR
      do! createAssetDir repo CUELIST_DIR
      do! createAssetDir repo PATCHES_DIR
      let adminPath = project.Path </> Asset.path User.Admin
      let! _ =
        User.Admin
        |> Yaml.encode
        |> Asset.save adminPath
      do! Git.Repo.stage repo adminPath
      return ()
    }

  // ** saveProject

  let saveProject (user: User) (project: IrisProject) : Either<IrisError,(Commit * IrisProject)> =
    either {
      do! if needsInit project then
            initRepo project
          else
            Right ()

      let msg = sprintf "%s saved the project" user.UserName
      let config = ProjectYaml()

      let project =
        config
        |> Config.toFile project.Config
        |> toFile project

      // save everything!
      let destPath = project.Path </> PROJECT_FILENAME + ASSET_EXTENSION

      try
        config.Save(destPath)
        return! commitPath destPath user.Signature msg project
      with
        | exn ->
          return!
            exn.Message
            |> Error.asProjectError "Project.saveProject"
            |> Either.fail
    }

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

  // ** updateConfig

  let updateConfig (config: IrisConfig) (project: IrisProject) : IrisProject =
    { project with Config = config }

  // ** updateDataDir

  let updateDataDir (raftDir: FilePath) (project: IrisProject) : IrisProject =
    { project.Config.RaftConfig with DataDir = raftDir }
    |> flip Config.updateEngine project.Config
    |> flip updateConfig project

  // ** addMember

  let addMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.addMember mem
    |> flip updateConfig project

  // ** updateMember

  let updateMember (mem: RaftMember) (project: IrisProject) : IrisProject =
    addMember mem project

  // ** removeMember

  let removeMember (mem: MemberId) (project: IrisProject) : IrisProject =
    project.Config
    |> Config.removeMember mem
    |> flip updateConfig project

  // ** addMembers

  let addMembers (mems: RaftMember list) (project: IrisProject) : IrisProject =
    List.fold
      (fun config (mem: RaftMember) ->
        Config.addMember mem config)
      project.Config
      mems
    |> flip updateConfig project
