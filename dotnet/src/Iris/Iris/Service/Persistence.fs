namespace Iris.Service

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
      let! mem = Config.selfMember options
      let! mems = Config.getMembers options
      let! data =
        options
        |> Config.metadataPath
        |> Asset.read
      return! Yaml.decode data
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
    let basepath = state.Project.Path
    match sm with
    | AddCue        cue     -> Asset.saveWithCommit   cue           basepath signature
    | UpdateCue     cue     -> Asset.saveWithCommit   cue           basepath signature
    | RemoveCue     cue     -> Asset.deleteWithCommit cue           basepath signature
    | AddCueList    cuelist -> Asset.saveWithCommit   cuelist       basepath signature
    | UpdateCueList cuelist -> Asset.saveWithCommit   cuelist       basepath signature
    | RemoveCueList cuelist -> Asset.deleteWithCommit cuelist       basepath signature
    | AddUser       user    -> Asset.saveWithCommit   user          basepath signature
    | UpdateUser    user    -> Asset.saveWithCommit   user          basepath signature
    | RemoveUser    user    -> Asset.deleteWithCommit user          basepath signature
    | AddMember     _       -> Asset.saveWithCommit   state.Project basepath signature
    | UpdateMember  _       -> Asset.saveWithCommit   state.Project basepath signature
    | RemoveMember  _       -> Asset.deleteWithCommit state.Project basepath signature
    | UpdateProject project -> Asset.saveWithCommit   project       basepath signature
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
  let updateRepo (project: IrisProject) (leader: RaftMember) : Either<IrisError,unit> = //
    printfn "should pull shit now"
    |> Either.succeed
