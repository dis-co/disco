namespace Iris.Service

// * Imports

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Persistence
open Iris.Service.Git
open Iris.Service.WebSockets
open Iris.Service.Raft
open LibGit2Sharp
open ZeroMQ

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

module Iris =

  // ** tag

  [<Literal>]
  let private tag = "IrisServer"

  let private signature =
    new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  // ** IIrisServer

  type IIrisServer =
    inherit IDisposable

    abstract Start : unit -> Either<IrisError,unit>

  // __        __   _    ____             _        _
  // \ \      / /__| |__/ ___|  ___   ___| | _____| |_ ___
  //  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __/ __|
  //   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_\__ \
  //    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|___/

  /// ## OnOpen
  ///
  /// Register a callback with the WebSocket server that is run when new browser session has
  /// contacted this IrisSerivce. First, we send a `DataSnapshot` to the client to initialize it
  /// with the current state. Then, we append the newly created Session value to the Raft log to
  /// replicate it throughout the cluster.

  let private onOpen (wsserver: IWebSocketServer) (sessionId: Id) =
    wsserver.Send sessionId (DataSnapshot store.State)
    // TODO: Session shouldn't be added until getting ID from client
    //match raftserver.Append(AddSession session) with
    //| Right entry ->
    //  sprintf "Added session to Raft log with id: %A" entry.Id
    //  |> Logger.debug nodeid tag
    //| Left  error ->
    //  Logger.err nodeid tag (string error)

  /// ## OnClose
  ///
  /// Register a callback to be run when a browser as exited a session in an orderly fashion. The
  /// session is removed from the global state by appending a `RemoveSession`
  let private onClose (raftserver: IRaftServer) (sessionid: Id) =
    match Map.tryFind sessionid store.State.Sessions with
    | Some session ->
      match raftserver.Append(RemoveSession session) with
      | Right entry ->
        sprintf "Remove session added to Raft log with id: %A" entry.Id
        |> Logger.debug nodeid tag
      | Left error  ->
        Logger.err nodeid tag (string error)
    | _ ->
      Logger.err nodeid tag "Session not found. Something spooky is going on"

  /// ## OnError
  ///
  /// Register a callback to be run if the client connection unexpectectly fails. In that case the
  /// Session is retrieved and removed from global state.
  let private onError (raftserver: IRaftServer) (sessionid: Id) =
    match Map.tryFind sessionid store.State.Sessions with
    | Some session ->
      match raftserver.Append(RemoveSession session) with
      | Right entry ->
        sprintf "Remove session added to Raft log with id: %A" entry.Id
        |> Logger.debug nodeid tag
      | Left error  ->
        Logger.err nodeid tag (string error)
    | _ ->
      Logger.err nodeid tag "Session not found. Something spooky is going on"

  /// ## OnMessage
  ///
  /// Register a handler to process messages coming from the browser client. The current handling
  /// mechanism is that incoming message get appended to the `Raft` log immediately, and a log
  /// message is sent back to the client. Once the new command has been replicated throughout the
  /// system, it will be applied to the server-side global state, then pushed over the socket to
  /// be applied to all client-side global state atoms.
  let private onMessage (raftserver: IRaftServer) sessionid command =
    match raftserver.Append(command) with
    | Right entry ->
      sprintf "Entry added to Raft log with id: %A" entry.Id
      |> Logger.debug nodeid tag
    | Left error  ->
      Logger.err nodeid tag (string error)

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  /// ## OnConfigured
  ///
  /// Register a callback to run when a new cluster configuration has been committed, and the
  /// joint-consensus mode has been concluded.
  let private onConfigured =
    Array.map (Node.getId >> string)
    >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
    >> Logger.debug nodeid tag

  /// ## OnNodeAdded
  ///
  /// Register a callback to be run when the user has added a new node to the `Raft` cluster. This
  /// commences the joint-consensus mode until the new node has been caught up and is ready be a
  /// full member of the cluster.

  let private onNodeAdded (wsserver: IWebSocketServer) (node: RaftNode) =
    node.Id
    |> string
    |> sprintf "Node added to cluster: %s"
    |> Logger.debug nodeid tag

    AddNode node
    |> wsserver.Broadcast

  /// ## OnNodeUpdated
  ///
  /// Register a callback to be called when a cluster node's properties such as e.g. its node
  /// state.

  let private onNodeUpdated (wsserver: IWebSocketServer) (node: RaftNode) =
    node.Id
    |> string
    |> sprintf "Node updated: %s"
    |> Logger.debug nodeid tag

    UpdateNode node
    |> wsserver.Broadcast

  /// ## OnNodeRemoved
  ///
  /// Register a callback to be run when a node was removed from the cluster, resulting into
  /// the cluster entering into joint-consensus mode until the node was successfully removed.

  let private onNodeRemoved (wsserver: IWebSocketServer) (node: RaftNode) =
    node.Id
    |> string
    |> sprintf "Node removed from cluster: %s"
    |> Logger.debug nodeid tag

    RemoveNode node
    |> wsserver.Broadcast

  /// ## OnApplyLog
  ///
  /// Register a callback to be run when an appended entry is considered safely appended to a
  /// majority of servers logs. The entry then is regarded as applied.
  ///
  /// In this callback implementation we essentially do 3 things:
  ///
  ///   - the state machine command is applied to the store, potentially altering its state
  ///   - the state machine command is broadcast to all clients
  ///   - the state machine command is persisted to disk (potentially recorded in a git commit)

  let private onApplyLog (wsserver: IWebSocketServer) (raftserver: IRaftServer) (sm: StateMachine) =
    store.Dispatch sm

    wsserver.Broadcast sm

    if raftserver.IsLeader then
      persistEntry !project sm |> ignore
    else
      updateRepo !project

    Logger.debug nodeid tag "Raft applied a new log"

  //   ____ _ _
  //  / ___(_) |_
  // | |  _| | __|
  // | |_| | | |_
  //  \____|_|\__|

  let private onGitEvent (wsserver: IWebSocketServer) (msg: GitEvent) =
    msg
    |> string
    |> Logger.debug nodeid tag

  // ** IrisService

  [<RequireQualifiedAccess>]
  module IrisService =

    let create (project: IrisProject) =
      either {
        match project.Path with
        | None ->
          return!
            ProjectPathError
            |> Either.fail

        | Some path ->
          let store = new Store(State.Empty)

          let httpserver  = new AssetServer(project.Config)

          let! raftserver = RaftServer.create project.Config
          let! wsserver   = IrisSocketServer.create raftserver.Node
          let! gitserver  = GitServer.create raftserver.Node path

          let logger =
            Observable.subscribe
              (fun log ->
                wsserver.Broadcast (LogMsg log) |> ignore
                Logger.stdout log)
              Logger.listener

          return
            { new IIrisServer with
                member self.Start() =
                  either {
                    do! httpserver.Start()
                    do! wsserver.Start()
                    do! GitServer.Start()
                    do! raftserver.Start()
                  }

                member self.Dispose() =
                  dispose logger
                  dispose raftserver
                  dispose wsserver
                  dispose gitserver
                  dispose httpserver
              }
      }
