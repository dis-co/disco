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

  // ** tag

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
        |> IrisData.read
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
      |> IrisData.write (Config.metadataPath config)
      |> Either.succeed
    with
      | exn ->
        sprintf "Project Save Error: %s" exn.Message
        |> Error.asProjectError "Persistence.saveRaft"
        |> Either.fail

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
    let basePath = state.Project.Path
    let inline save t = Asset.save basePath t
    let inline delete t = Asset.delete basePath t
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

    //  __  __                   _
    // |  \/  | __ _ _ __  _ __ (_)_ __   __ _
    // | |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
    // | |  | | (_| | |_) | |_) | | | | | (_| |
    // |_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
    //              |_|   |_|            |___/

    | AddPinMapping    mapping
    | UpdatePinMapping mapping -> save mapping
    | RemovePinMapping mapping -> delete mapping

    // __        ___     _            _
    // \ \      / (_) __| | __ _  ___| |_
    //  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
    //   \ V  V / | | (_| | (_| |  __/ |_
    //    \_/\_/  |_|\__,_|\__, |\___|\__|
    //                     |___/

    | AddPinWidget    widget
    | UpdatePinWidget widget -> save widget
    | RemovePinWidget widget -> delete widget

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
    | UpdatePin pin ->
      match State.tryFindPinGroup pin.PinGroup state with
      | Some group when group.Persisted -> save group
      | Some group -> delete group
      | None -> Either.nothing

    | RemovePin pin ->
      match State.tryFindPinGroup pin.PinGroup state with
      | Some group when group.Persisted -> save group
      | Some group -> delete group
      | None -> Either.nothing

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
      let! commit = Git.Repo.commit repo "Project changes committed." signature
      return repo, commit
    }

  // ** pushChanges

  let pushChanges (repo: Repository) =
    repo
    |> Git.Config.remotes
    |> Map.map    (konst (Git.Repo.push repo))
    |> Map.filter (konst (Either.isFail))
    |> Map.map    (konst (Either.error))

  // ** persistSnapshot

  let persistSnapshot (state: State) (log: RaftLogEntry) =
    either {
      let path = Project.toFilePath state.Project.Path
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

  // ** ensureRemote

  let ensureRemote (project: IrisProject) (repo: Repository) (peer: RaftMember) =
    let uri = Uri.gitUri project.Name peer
    match Git.Config.tryFindRemote repo (string peer.Id) with
    | None ->
      peer.Id
      |> string
      |> sprintf "Adding %A to list of remotes"
      |> Logger.debug (tag "getRemote")
      Git.Config.addRemote repo (string peer.Id) uri

    | Some remote when remote.Url <> unwrap uri ->
      peer.Id
      |> string
      |> sprintf "Updating remote section for %A to point to %A" uri
      |> Logger.debug (tag "getRemote")
      Git.Config.updateRemote repo remote uri

    | Some remote ->
      Either.succeed remote

  // ** ensureRemotes

  let ensureRemotes (leader: Id)
                    (project: IrisProject)
                    (peers: Map<Id,RaftMember>)
                    (repo: Repository) =
    peers
    |> Map.toArray
    |> Array.filter (fst >> (<>) leader)
    |> Array.iter (snd >> ensureRemote project repo >> ignore)
    repo

  // ** ensureTracking

  let ensureTracking (repo: Repository) (branch: Branch) (remote: Remote) =
    if not (Git.Branch.isTracking branch) then
      Git.Branch.setTracked repo branch remote
    else
      Either.nothing

#endif
