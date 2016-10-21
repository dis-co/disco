namespace Iris.Service

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service
open LibGit2Sharp
open ZeroMQ
open FSharpx.Functional
open SharpYaml.Serialization

module Persistence =

  /// ## saveAsset
  ///
  /// save a thing (string) to a file and returns its FileInfo. Might
  /// crash, so catch it.
  ///
  /// ### Signature:
  /// - location: FilePath to save payload to
  /// - payload: string payload to save
  ///
  /// Returns: FileInfo
  let saveAsset (location: FilePath) (payload: string) : FileInfo =
   let info = IO.FileInfo location
   if not (IO.Directory.Exists info.Directory.FullName) then
     IO.Directory.CreateDirectory info.Directory.FullName
     |> ignore
   File.WriteAllText(location, payload)
   info.Refresh()
   info

  /// ## deleteAsset
  ///
  /// Delete a file from disk
  ///
  /// ### Signature:
  /// - location: path of file to delete
  ///
  /// Returns: bool
  let deleteAsset (location: FilePath) : FileInfo =
    if IO.File.Exists location then
      try
        IO.File.Delete location
      with
        | exn -> ()
    IO.FileInfo location


  /// ## Create a new Raft state
  ///
  /// Create a new initial Raft state value with default values from
  /// the passed-in options.
  ///
  /// ### Signature:
  /// - options: RaftOptions
  ///
  /// Returns: Either<IrisError,Raft>
  let createRaft (options: IrisConfig) : Either<IrisError, Raft> =
    Config.getNodeId ()
    |> Either.bind (Config.findNode options)
    |> Either.map mkRaft
    |> Either.map (options.ClusterConfig.Nodes |> Array.ofList |> addNodes)

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
  let loadRaft (options: IrisConfig) =

    /// loadRaft self (Array.map (fun (n: RaftNode) -> n.Id, n) nodes |> Map.ofArray)

    let node =
      Config.getNodeId ()
      |> Either.bind (Config.findNode options)

    let nodes =
      options.ClusterConfig.Nodes
      |> List.map (fun node -> node.Id, node)
      |> Map.ofList

    match node with
    | Right node -> failwith "FIXME: loadRaft"
    | Left error -> Either.fail error

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
  let saveRaft (config: IrisConfig) (raft: Raft) =
    try
      raft
      |> Yaml.encode
      |> saveAsset (config.RaftConfig.DataDir </> RAFT_DIRECTORY)
      |> Either.succeed
    with
      | exn ->
        ProjectSaveError exn.Message
        |> Either.fail

  /// ## saveWithCommit
  ///
  /// Attempt to save the passed thing, and, if succesful, return its
  /// FileInfo object.
  ///
  /// ### Signature:
  /// - project: Project to save file into
  /// - thing: the thing to save. Must implement certain methods/getters
  ///
  /// Returns: FileInfo option
  let inline saveWithCommit< ^t when
                         ^t : (member ToYaml : Serializer -> string) and
                         ^t : (member CanonicalName : string)       and
                         ^t : (member DirName : string)>
                         (project: IrisProject) (thing: ^t) =
    match project.Path with
    | Some path ->
      let name = (^t : (member CanonicalName : string) thing)
      let relPath = (^t : (member DirName : string) thing) </> (name + ".yaml")
      let destPath = path </> relPath
      try
        // FIXME: should later be the person who issued command (session + user)
        let committer =
          let hostname = Net.Dns.GetHostName()
          new Signature("Iris", "iris@" + hostname, new DateTimeOffset(DateTime.Now))

        let msg = sprintf "Saved %s " name

        let fileinfo =
          thing
          |> Yaml.encode
          |> saveAsset destPath

        match Project.saveFile committer msg relPath project with
        | Right (commit, saved) -> Right(fileinfo, commit, saved)
        | Left   error          -> Left error

      with
        | exn ->
          exn.Message
          |> AssetSaveError
          |> Either.fail

    | _ -> ProjectPathError |> Either.fail

  let inline deleteWithCommit< ^t when
                               ^t : (member CanonicalName : string) and
                               ^t : (member DirName : string)>
                               (project: IrisProject) (thing: ^t) =
    match project.Path with
    | Some path ->
      let name = (^t : (member CanonicalName : string) thing)
      let relPath = (^t : (member DirName : string) thing) </> (name + ".yaml")
      let destPath = path </> relPath
      try
        let fileinfo = deleteAsset destPath

        let committer =
          let hostname = Net.Dns.GetHostName()
          new Signature("Iris", "iris@" + hostname, new DateTimeOffset(DateTime.Now))

        let msg = sprintf "Saved %s " name

        match Project.saveFile committer msg relPath project with
        | Right (commit, saved) -> Right(fileinfo, commit, saved)
        | Left error            -> Left error
      with
        | exn ->
          exn.Message
          |> AssetDeleteError
          |> Either.fail
    | _ -> Either.fail ProjectPathError

  let inline persistEntry (project: IrisProject) (sm: StateMachine) =
    match sm with
    | AddCue        cue     -> saveWithCommit     project cue
    | UpdateCue     cue     -> saveWithCommit     project cue
    | RemoveCue     cue     -> deleteWithCommit project cue
    | AddCueList    cuelist -> saveWithCommit     project cuelist
    | UpdateCueList cuelist -> saveWithCommit     project cuelist
    | RemoveCueList cuelist -> deleteWithCommit project cuelist
    | AddUser       user    -> saveWithCommit     project user
    | UpdateUser    user    -> saveWithCommit     project user
    | RemoveUser    user    -> deleteWithCommit project user
    | _                     -> Left (Other "this is ok. relax")

  let updateRepo (project: IrisProject) =
    printfn "should pull shit now"
