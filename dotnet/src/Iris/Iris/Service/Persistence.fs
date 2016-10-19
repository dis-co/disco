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

  /// ## saveToDisk
  ///
  /// Attempt to save the passed thing, and, if succesful, return its
  /// FileInfo object.
  ///
  /// ### Signature:
  /// - project: Project to save file into
  /// - thing: the thing to save. Must implement certain methods/getters
  ///
  /// Returns: FileInfo option
  let inline saveToDisk< ^t when
                         ^t : (member ToYaml : Serializer -> string) and
                         ^t : (member CanonicalName : string)       and
                         ^t : (member DirName : string)>
                         (project: Project) (thing: ^t) =
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

        match saveFile committer msg relPath project with
        | Right (commit, saved) -> Right(fileinfo, commit, saved)
        | Left   error          -> Left error

      with
        | exn ->
          exn.Message
          |> AssetSaveError
          |> Either.fail

    | _ -> ProjectPathError |> Either.fail

  let inline deleteFromDisk< ^t when
                             ^t : (member CanonicalName : string) and
                             ^t : (member DirName : string)>
                             (project: Project) (thing: ^t) =
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

        match saveFile committer msg relPath project with
        | Right (commit, saved) -> Right(fileinfo, commit, saved)
        | Left error            -> Left error
      with
        | exn ->
          exn.Message
          |> AssetDeleteError
          |> Either.fail
    | _ -> Either.fail ProjectPathError

  let inline persistEntry (project: Project) (sm: StateMachine) =
    match sm with
    | AddCue        cue     -> saveToDisk     project cue
    | UpdateCue     cue     -> saveToDisk     project cue
    | RemoveCue     cue     -> deleteFromDisk project cue
    | AddCueList    cuelist -> saveToDisk     project cuelist
    | UpdateCueList cuelist -> saveToDisk     project cuelist
    | RemoveCueList cuelist -> deleteFromDisk project cuelist
    | AddUser       user    -> saveToDisk     project user
    | UpdateUser    user    -> saveToDisk     project user
    | RemoveUser    user    -> deleteFromDisk project user
    | _                     -> Left (Other "this is ok. relax")

  let updateRepo (project: Project) =
    printfn "should pull shit now"


  /// ## Create a new Raft state
  ///
  /// Create a new initial Raft state value from the passed-in options.
  ///
  /// ### Signature:
  /// - options: RaftOptions
  ///
  /// Returns: Raft<StateMachine,IrisNode>
  let createRaft (options: Config) : Either<IrisError, Raft> =
    getNodeId ()
    |> Either.bind (tryFindNode options)
    |> Either.map mkRaft
    |> Either.map (options.ClusterConfig.Nodes |> Array.ofList |> addNodes)

  let loadRaft (options: Config) =
    let db = failwith "FIXME: loadRaft"

    let node =
      getNodeId ()
      |> Either.bind (tryFindNode options)

    let nodes =
      options.ClusterConfig.Nodes
      |> List.map (fun node -> node.Id, node)
      |> Map.ofList

    match db, node with
    | Right database, Right node -> failwith "FIXME: loadRaft"
    | Left error,     _          -> Either.fail error
    | _,              Left error -> Either.fail error

  let getRaft (options: Config) =
    match loadRaft options with
      | Right raft -> Either.succeed raft
      | _          -> createRaft options

  let saveRaft _ =
    failwith "implement saveRaft"

  let saveRaftMetadata _ =
    failwith "implement saveRaftMetadata"
