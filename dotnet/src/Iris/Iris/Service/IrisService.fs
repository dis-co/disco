namespace Iris.Service.Core

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service
open Iris.Service.Types
open Iris.Service.Core
open Iris.Service.Raft.Server
open LibGit2Sharp
open ZeroMQ
open FSharpx.Functional
open SharpYaml.Serialization

[<AutoOpen>]
module Hooks =
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

        match project.SaveFile(committer, msg, relPath) with
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

        match project.SaveFile(committer, msg, relPath) with
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

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

type IrisService(project: Project ref) =
  let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  let store : Store = new Store(State.Empty)

  let kontext = new ZContext()

  let raftserver = new RaftServer((!project).Config, kontext)
  let wsserver   = new WsServer((!project).Config, raftserver)
  let httpserver = new AssetServer((!project).Config)

  let setup _ =
    // WEBSOCKET
    wsserver.OnOpen <- fun (session: Session) ->
      wsserver.Send session.Id (DataSnapshot store.State)
      let msg =
        match raftserver.Append(AddSession session) with
        | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Added session to Raft log with id: %A" entry.Id)
        | _          -> Iris.Core.LogLevel.Err, "Could not add new session log."
      wsserver.Broadcast (LogMsg msg)

    wsserver.OnClose <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
          | _          -> Iris.Core.LogLevel.Err, "Could not remove session log."
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Iris.Core.LogLevel.Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    wsserver.OnError <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Remove session (due to Error) added to Raft log with id: %A" entry.Id)
          | _          -> Iris.Core.LogLevel.Err, "Could not remove session log."
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Iris.Core.LogLevel.Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    wsserver.OnMessage <- fun sessionid command ->
      let msg =
        match raftserver.Append(command) with
        | Some entry -> Iris.Core.LogLevel.Debug, (sprintf "Entry added to Raft log with id: %A" entry.Id)
        | _          -> Iris.Core.LogLevel.Err, "Could not add new entry to Raft log :("
      wsserver.Broadcast (LogMsg msg)

    // RAFTSERVER
    raftserver.OnConfigured <-
      Array.map (fun (node: RaftNode) -> string node.Id)
      >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      >> (fun str -> LogMsg(Iris.Core.LogLevel.Debug, str))
      >> wsserver.Broadcast

    raftserver.OnLogMsg <- fun _ msg ->
      wsserver.Broadcast(LogMsg(Iris.Core.LogLevel.Debug, msg))

    raftserver.OnNodeAdded   <- AddNode    >> wsserver.Broadcast
    raftserver.OnNodeUpdated <- UpdateNode >> wsserver.Broadcast
    raftserver.OnNodeRemoved <- RemoveNode >> wsserver.Broadcast

    raftserver.OnApplyLog <- fun sm ->
      store.Dispatch sm
      wsserver.Broadcast sm
      if raftserver.IsLeader then
        persistEntry !project sm |> ignore
      else
        updateRepo !project

  do setup ()

  member self.Raft
    with get () : RaftServer = raftserver

  //  ___       _             __
  // |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___
  //  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __|
  //  | || | | | ||  __/ |  |  _| (_| | (_|  __/\__ \
  // |___|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/
  //
  interface IDisposable with
    member self.Dispose() =
      self.Stop()

  //  _     _  __       ____           _
  // | |   (_)/ _| ___ / ___|   _  ___| | ___
  // | |   | | |_ / _ \ |  | | | |/ __| |/ _ \
  // | |___| |  _|  __/ |__| |_| | (__| |  __/
  // |_____|_|_|  \___|\____\__, |\___|_|\___|
  //                        |___/
  member self.Start() =
    printfn "Starting Http Server on %d" (!project).Config.PortConfig.Http
    httpserver.Start()

    printfn "Starting WebSocket Server on %d" (!project).Config.PortConfig.WebSocket
    wsserver.Start()

    printfn "Starting Raft Server %d" (!project).Config.PortConfig.Raft
    raftserver.Start()

  member self.Stop() =
    dispose raftserver
    dispose wsserver
    dispose httpserver
    dispose kontext

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  // member self.SaveProject(id, msg) =
  //   match saveProject id signature msg !state with
  //     | Success (commit, newstate) ->
  //       state := newstate
  //       Either.succeed commit
  //     | Fail err -> Either.fail err

  //   ____                _
  //  / ___|_ __ ___  __ _| |_ ___
  // | |   | '__/ _ \/ _` | __/ _ \
  // | |___| | |  __/ (_| | ||  __/
  //  \____|_|  \___|\__,_|\__\___|

  // member self.CreateProject(name, path) =
  //   createProject name path signature !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         project.Name
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> state := state'
  //                        self.Ctrl.Load(project.Id, project.Name)
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "hm"

  //   ____ _
  //  / ___| | ___  ___  ___
  // | |   | |/ _ \/ __|/ _ \
  // | |___| | (_) \__ \  __/
  //  \____|_|\___/|___/\___|

  // member self.CloseProject(pid) =
  //   findProject pid !state
  //     >>= fun project ->
  //       combine project (closeProject pid !state)
  //     >>= fun (project, state') ->
  //       // remove and stop process groups
  //       let result = removeProcess project.Id >>= stopProcess
  //       match result with
  //         | Success _ -> state := state'                    // save global state
  //                        self.Ctrl.Close(pid, project.Name) // notify everybody
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err

  //   printfn "fm"

  //  _                    _
  // | |    ___   __ _  __| |
  // | |   / _ \ / _` |/ _` |
  // | |__| (_) | (_| | (_| |
  // |_____\___/ \__,_|\__,_|

  // member self.LoadProject(path : FilePath) =
  //   loadProject path !state
  //     >>= fun (project, state') ->
  //       // add and start the process groups for this project
  //       let result =
  //         ProjectProcess.Create project
  //           >>= startProcess
  //           >>= (addProcess project.Id >> succeed)

  //       match result with
  //         | Success _ -> self.Ctrl.Load(project.Id, project.Name)
  //                        state := state'
  //                        succeed project
  //         | Fail err  -> logger tag err
  //                        fail err
  //   printfn "oh"
