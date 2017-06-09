namespace Iris.Service

// * Imports

#if !IRIS_NODES

open System
open Iris.Raft
open Iris.Core
open Iris.Service
open LibGit2Sharp

// * Persistence

module Persistence =

  let private tag (str: string) = String.Format("Persistence.{0}", str)

  // ** createRaft

  /// ## Create a new Raft state
  ///
  /// Create a new initial Raft state value with default values from
  /// the passed-in options.
  ///
  /// ### Signature:
  /// - options: RaftOptions
  ///
  /// Returns: Either<IrisError,Raft>
  let createRaft (options: IrisConfig) =
    either {
      let! mem = Config.selfMember options
      let! mems = Config.getMembers options
      let state =
        mem
        |> Raft.create
        |> Raft.addMembers mems
        |> Raft.setMaxLogDepth options.Raft.MaxLogDepth
        |> Raft.setRequestTimeout options.Raft.RequestTimeout
        |> Raft.setElectionTimeout options.Raft.ElectionTimeout
      return state
    }

  // ** loadRaft

  /// ## Load a raft state from disk
  ///
  /// Load a Raft state value from disk. This includes parsing the
  /// project file to set up the cluster mems, as well as loading the
  /// saved Raft metadata from the local (hidden) directory
  /// `RaftDataDir` value in the project configuration.
  ///
  /// ### Signature:
  /// - options: Project Config
  ///
  /// Returns: Either<IrisError,Raft>
  let loadRaft (options: IrisConfig) : Either<IrisError,RaftValue> =
    either {
      let! mem  = Config.selfMember options
      let! mems = Config.getMembers options
      let count = Map.fold (fun m _ _ -> m + 1) 0 mems
      let! data =
        options
        |> Config.metadataPath
        |> Asset.read
      let! state = Yaml.decode data
      return
        { state with
            Member          = mem
            NumMembers      = count
            Peers           = mems
            MaxLogDepth     = options.Raft.MaxLogDepth
            RequestTimeout  = options.Raft.RequestTimeout
            ElectionTimeout = options.Raft.ElectionTimeout }
    }

  // ** getRaft

  /// ## Get Raft state value from config
  ///
  /// Either load the last Raft state from disk pointed to by the
  /// passed configuration, or create a new Raft state from scratch.
  ///
  /// ### Signature:
  /// - options: Project Config
  ///
  /// Returns: Either<IrisError,Raft>
  let getRaft (options: IrisConfig) =
    match loadRaft options with
      | Right raft -> Either.succeed raft
      | _          -> createRaft options

  // ** saveRaft

  /// ## saveRaftMetadata to disk
  ///
  /// Attempts to save Raft metadata to disk at the location
  /// configured in RaftConfig.DataDir.
  ///
  /// ### Signature:
  /// - config: IrisConfig
  /// - raft: Raft state value
  ///
  /// Returns: Either<IrisError,FileInfo>
  let saveRaft (config: IrisConfig) (raft: RaftValue) =
    try
      raft
      |> Yaml.encode
      |> Payload
      |> Asset.write (Config.metadataPath config)
      |> Either.succeed
    with
      | exn ->
        sprintf "Project Save Error: %s" exn.Message
        |> Error.asProjectError "Persistence.saveRaft"
        |> Either.fail

  // ** ensureDirectory

  let private ensureDirectory (path: FilePath) =
    path
    |> Path.getDirectoryName
    |> Directory.createDirectory
    |> ignore

  // ** persistEntry

  /// ## persistEntry
  ///
  /// Persist a StateMachine command to disk.
  ///
  /// ### Signature:
  /// - project: IrisProject to work on
  /// - sm: StateMachine command
  ///
  /// Returns: Either<IrisError, FileInfo * IrisProject>
  let persistEntry (state: State) (sm: StateMachine) =
    let signature = User.Admin.Signature
    let basePath = state.Project.Path
    let inline save t = Asset.save basePath t
    let inline delete t = t |> Asset.path |> Path.concat basePath |> Asset.delete
    match sm with
    //   ____
    //  / ___|   _  ___
    // | |  | | | |/ _ \
    // | |__| |_| |  __/
    //  \____\__,_|\___|

    | AddCue    cue
    | UpdateCue cue -> save cue
    | RemoveCue cue -> delete cue

    //   ____           _     _     _
    //  / ___|   _  ___| |   (_)___| |_
    // | |  | | | |/ _ \ |   | / __| __|
    // | |__| |_| |  __/ |___| \__ \ |_
    //  \____\__,_|\___|_____|_|___/\__|

    | AddCueList    cuelist
    | UpdateCueList cuelist -> save cuelist
    | RemoveCueList cuelist -> delete cuelist

    //   ____           ____  _
    //  / ___|   _  ___|  _ \| | __ _ _   _  ___ _ __
    // | |  | | | |/ _ \ |_) | |/ _` | | | |/ _ \ '__|
    // | |__| |_| |  __/  __/| | (_| | |_| |  __/ |
    //  \____\__,_|\___|_|   |_|\__,_|\__, |\___|_|
    //                                |___/

    | AddCuePlayer    player
    | UpdateCuePlayer player -> save player
    | RemoveCuePlayer player -> delete player

    //  ____  _        ____
    // |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
    // | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
    // |  __/| | | | | |_| | | | (_) | |_| | |_) |
    // |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
    //                                     |_|

    | AddPinGroup    group
    | UpdatePinGroup group -> save group
    | RemovePinGroup group -> delete group

    //  _   _
    // | | | |___  ___ _ __
    // | | | / __|/ _ \ '__|
    // | |_| \__ \  __/ |
    //  \___/|___/\___|_|

    | AddUser    user
    | UpdateUser user -> save user
    | RemoveUser user -> delete user

    //  __  __                _
    // |  \/  | ___ _ __ ___ | |__   ___ _ __
    // | |\/| |/ _ \ '_ ` _ \| '_ \ / _ \ '__|
    // | |  | |  __/ | | | | | |_) |  __/ |
    // |_|  |_|\___|_| |_| |_|_.__/ \___|_|

    | AddMember     _
    | RemoveMember  _
    | UpdateProject _ -> save state.Project

    //  ____  _
    // |  _ \(_)_ __
    // | |_) | | '_ \
    // |  __/| | | | |
    // |_|   |_|_| |_|

    | AddPin    pin
    | UpdatePin pin -> either {
        let! group =
          state
          |> State.tryFindPinGroup pin.PinGroup
          |> Either.ofOption (Error.asOther (tag "persistEntry") "PinGroup not found")
        let path = Asset.path group
        ensureDirectory path
        return! save group
      }
    | RemovePin pin -> either {
        let! group =
          state
          |> State.tryFindPinGroup pin.PinGroup
          |> Either.ofOption (Error.asOther (tag "persistEntry") "PinGroup not found")
        let path = Asset.path group
        ensureDirectory path
        return! save group
      }

    //   ___  _   _
    //  / _ \| |_| |__   ___ _ __
    // | | | | __| '_ \ / _ \ '__|
    // | |_| | |_| | | |  __/ |
    //  \___/ \__|_| |_|\___|_|

    | _ -> Either.nothing

  // ** commitChanges

  /// ## commitChanges
  ///
  /// Commit all changes to disk
  ///
  /// ### Signature:
  /// - project: IrisProject to work on
  /// - sm: StateMachine command
  ///
  /// Returns: Either<IrisError, Commit>
  let commitChanges (state: State) =
    either {
      let signature = User.Admin.Signature
      let! repo = state.Project |> Project.repository
      do! Git.Repo.stageAll repo
      return! Git.Repo.commit repo "Project changes committed." signature
    }

  let persistSnapshot (state: State) (log: RaftLogEntry) =
    either {
      let path = state.Project.Path
      do! state.Save(path)
      use! repo = Project.repository state.Project
      do! Git.Repo.stageAll repo

      Git.Repo.commit repo "[Snapshot] Log Compaction" User.Admin.Signature
      |> ignore

      do! Asset.save path log
    }

  // ** getRemote

  let getRemote (project: IrisProject) (repo: Repository) (leader: RaftMember) =
    let uri = Uri.gitUri project.Name leader
    match Git.Config.tryFindRemote repo (string leader.Id) with
    | None ->
      leader.Id
      |> string
      |> sprintf "Adding %A to list of remotes"
      |> Logger.debug (tag "getRemote")
      Git.Config.addRemote repo (string leader.Id) uri

    | Some remote when remote.Url <> unwrap uri ->
      leader.Id
      |> string
      |> sprintf "Updating remote section for %A to point to %A" uri
      |> Logger.debug (tag "getRemote")
      Git.Config.updateRemote repo remote uri

    | Some remote ->
      Either.succeed remote

  // ** ensureTracking

  let ensureTracking (repo: Repository) (branch: Branch) (remote: Remote) =
    if not (Git.Branch.isTracking branch) then
      Git.Branch.setTracked repo branch remote
    else
      Either.nothing

  // ** updateRepo

  /// ## updateRepo
  ///
  /// Pull changes from the leader's git repository
  ///
  /// ### Signature:
  /// - project: IrisProject
  /// - leader: RaftMember who is currently leader of the cluster
  ///
  /// Returns: Either<IrisError, unit>
  let updateRepo (project: IrisProject) (leader: RaftMember) =
    either {
      let! repo = Project.repository project
      let! remote = getRemote project repo leader

      let branch = Git.Branch.current repo
      do! ensureTracking repo branch remote
      let result = Git.Repo.pull repo remote User.Admin.Signature
      match result with
      | Right merge ->
        return!
          match merge.Status with
          | MergeStatus.Conflicts ->
            (GitMergeStatus.Conflicts, None)
            |> Either.succeed
          | MergeStatus.UpToDate  ->
            (GitMergeStatus.UpToDate, None)
            |> Either.succeed
          | MergeStatus.FastForward ->
            (GitMergeStatus.FastForward, merge.Commit |> Option.ofNull (fun c -> Some c.Sha))
            |> Either.succeed
          | MergeStatus.NonFastForward as status ->
            (GitMergeStatus.NonFastForward, merge.Commit |> Option.ofNull (fun c -> Some c.Sha))
            |> Either.succeed
          | other ->
            String.format "unknown merge status: %A" other
            |> Error.asGitError (tag "updateRepo") |> Either.fail
      | Left error -> return! Either.fail error
    }

#endif
