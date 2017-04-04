namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open System.IO
open System.Collections.Concurrent
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service
open LibGit2Sharp
open ZeroMQ
open FSharpx.Functional
open SharpYaml.Serialization

// * Persistence
module Persistence =

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
        |> Raft.mkRaft
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
      let count = Map.fold (fun m _ _ -> m + 1u) 0u mems
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

  // ** persistEntry

  /// ## persistEntry
  ///
  /// Persist a StateMachine command to disk.
  ///
  /// ### Signature:
  /// - project: IrisProject to work on
  /// - sm: StateMachine command
  ///
  /// Returns: Either<IrisError, FileInfo * Commit * IrisProject>
  let inline persistEntry (state: State) (sm: StateMachine) =
    let signature = User.Admin.Signature
    let path = state.Project.Path
    match sm with
    | AddCue        cue     -> Asset.saveWithCommit   path signature cue
    | UpdateCue     cue     -> Asset.saveWithCommit   path signature cue
    | RemoveCue     cue     -> Asset.deleteWithCommit path signature cue
    | AddCueList    cuelist -> Asset.saveWithCommit   path signature cuelist
    | UpdateCueList cuelist -> Asset.saveWithCommit   path signature cuelist
    | RemoveCueList cuelist -> Asset.deleteWithCommit path signature cuelist
    | AddUser       user    -> Asset.saveWithCommit   path signature user
    | UpdateUser    user    -> Asset.saveWithCommit   path signature user
    | RemoveUser    user    -> Asset.deleteWithCommit path signature user
    | AddMember     _       -> Asset.saveWithCommit   path signature state.Project
    | UpdateMember  _       -> Asset.saveWithCommit   path signature state.Project
    | RemoveMember  _       -> Asset.deleteWithCommit path signature state.Project
    | UpdateProject project -> Asset.saveWithCommit   path signature project
    | _                     -> Left OK

  // ** updateRepo

  /// ## updateRepo
  ///
  /// Description
  ///
  /// ### Signature:
  /// - arg: arg
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: Either<IrisError, about:blank>
  let updateRepo (project: IrisProject) (leader: RaftMember) : Either<IrisError,unit> =
    printfn "should pull repository now"
    |> Either.succeed

#endif
