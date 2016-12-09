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
      let! node = Config.selfNode options
      let! nodes = Config.getNodes options
      let state =
        node
        |> Raft.mkRaft
        |> Raft.addNodes nodes
      return state
    }

  // ** loadRaft

  /// ## Load a raft state from disk
  ///
  /// Load a Raft state value from disk. This includes parsing the
  /// project file to set up the cluster nodes, as well as loading the
  /// saved Raft metadata from the local (hidden) directory
  /// `RaftDataDir` value in the project configuration.
  ///
  /// ### Signature:
  /// - options: Project Config
  ///
  /// Returns: Either<IrisError,Raft>
  let loadRaft (options: IrisConfig) : Either<IrisError,RaftValue> =
    either {
      let! node = Config.selfNode options
      let! nodes = Config.getNodes options
      let! data =
        options
        |> Config.metadataPath
        |> Asset.load
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
      |> Asset.save (Config.metadataPath config)
      |> Either.succeed
    with
      | exn ->
        ProjectSaveError exn.Message
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
  let inline persistEntry (project: IrisProject) (sm: StateMachine) =
    match sm with
    | AddCue        cue     -> Project.saveAsset   cue     User.Admin project
    | UpdateCue     cue     -> Project.saveAsset   cue     User.Admin project
    | RemoveCue     cue     -> Project.deleteAsset cue     User.Admin project
    | AddCueList    cuelist -> Project.saveAsset   cuelist User.Admin project
    | UpdateCueList cuelist -> Project.saveAsset   cuelist User.Admin project
    | RemoveCueList cuelist -> Project.deleteAsset cuelist User.Admin project
    | AddUser       user    -> Project.saveAsset   user    User.Admin project
    | UpdateUser    user    -> Project.saveAsset   user    User.Admin project
    | RemoveUser    user    -> Project.deleteAsset user    User.Admin project
    | _                     -> Left (Other "this is ok. relax")

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
  let updateRepo (project: IrisProject) (leader: RaftNode) : Either<IrisError,unit> =
    printfn "should pull shit now"
    |> Either.succeed
