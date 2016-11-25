namespace Iris.Service

// * Imports

open System
open System.IO
open Iris.Raft
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Persistence
open LibGit2Sharp
open ZeroMQ

// * IrisService

//  ___      _     ____                  _
// |_ _|_ __(_)___/ ___|  ___ _ ____   _(_) ___ ___
//  | || '__| / __\___ \ / _ \ '__\ \ / / |/ __/ _ \
//  | || |  | \__ \___) |  __/ |   \ V /| | (_|  __/
// |___|_|  |_|___/____/ \___|_|    \_/ |_|\___\___|
//

type IrisService(project: IrisProject ref) =
  let signature = new Signature("Karsten Gebbert", "k@ioctl.it", new DateTimeOffset(DateTime.Now))

  let store : Store = new Store(State.Empty)

  let gitserver  = new GitServer(!project)
  let raftserver = new RaftServer((!project).Config)
  let wsserver   = new WsServer((!project).Config, raftserver)
  let httpserver = new AssetServer((!project).Config)

  //  _                      _
  // | |    ___   __ _  __ _(_)_ __   __ _
  // | |   / _ \ / _` |/ _` | | '_ \ / _` |
  // | |__| (_) | (_| | (_| | | | | | (_| |
  // |_____\___/ \__, |\__, |_|_| |_|\__, |
  //             |___/ |___/         |___/

  let logger =
    Observable.subscribe
      (fun log ->
        wsserver.Broadcast (LogMsg log)
        Logger.stdout log)
      Logger.listener

  let tag = "IrisService"

  let nodeid =
    Config.getNodeId()
    |> Error.orExit id

  // ** setup

  let setup _ =
    // __        __   _    ____             _        _
    // \ \      / /__| |__/ ___|  ___   ___| | _____| |_
    //  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
    //   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_
    //    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|
    //
    //      _   _                 _ _
    //     | | | | __ _ _ __   __| | | ___ _ __ ___
    //     | |_| |/ _` | '_ \ / _` | |/ _ \ '__/ __|
    //     |  _  | (_| | | | | (_| | |  __/ |  \__ \
    //     |_| |_|\__,_|_| |_|\__,_|_|\___|_|  |___/

    /// ## OnOpen
    ///
    /// Register a callback with the WebSocket server that is run when new browser session has
    /// contacted this IrisSerivce. First, we send a `DataSnapshot` to the client to initialize it
    /// with the current state. Then, we append the newly created Session value to the Raft log to
    /// replicate it throughout the cluster.
    wsserver.OnOpen <- fun (sessionId: Id) ->
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
    wsserver.OnClose <- fun sessionid ->
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
    wsserver.OnError <- fun sessionid ->
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
    wsserver.OnMessage <- fun sessionid command ->
      match raftserver.Append(command) with
      | Right entry ->
        sprintf "Entry added to Raft log with id: %A" entry.Id
        |> Logger.debug nodeid tag
      | Left error  ->
        Logger.err nodeid tag (string error)

    //  ____        __ _     _   _                 _ _
    // |  _ \ __ _ / _| |_  | | | | __ _ _ __   __| | | ___ _ __ ___
    // | |_) / _` | |_| __| | |_| |/ _` | '_ \ / _` | |/ _ \ '__/ __|
    // |  _ < (_| |  _| |_  |  _  | (_| | | | | (_| | |  __/ |  \__ \
    // |_| \_\__,_|_|  \__| |_| |_|\__,_|_| |_|\__,_|_|\___|_|  |___/

    /// ## OnConfigured
    ///
    /// Register a callback to run when a new cluster configuration has been committed, and the
    /// joint-consensus mode has been concluded.
    raftserver.OnConfigured <-
      Array.map (Node.getId >> string)
      >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      >> Logger.debug nodeid tag

    /// ## OnNodeAdded
    ///
    /// Register a callback to be run when the user has added a new node to the `Raft` cluster. This
    /// commences the joint-consensus mode until the new node has been caught up and is ready be a
    /// full member of the cluster.

    raftserver.OnNodeAdded <- fun node ->
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

    raftserver.OnNodeUpdated <- fun node ->
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

    raftserver.OnNodeRemoved <- fun node ->
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

    raftserver.OnApplyLog <- fun sm ->
      store.Dispatch sm

      wsserver.Broadcast sm

      if raftserver.IsLeader then
        persistEntry !project sm |> ignore
      else
        updateRepo !project

      Logger.debug nodeid tag "Raft applied a new log"

  do setup ()

  member self.Raft
    with get () : RaftServer = raftserver

  // ** Dispose

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

  // ** Start

  member self.Start(web: bool) =
    try
      if web then
        httpserver.Start()
      else
        Logger.debug nodeid tag "not starting HTTP server"

      gitserver.Start()
      wsserver.Start()
      raftserver.Start()
    with
      | exn ->
        sprintf "Exception occurred trying to start IrisService: %s" exn.Message
        |> Logger.err nodeid tag

  member self.Start() =
    try
      gitserver.Start()
      httpserver.Start()
      wsserver.Start()
      raftserver.Start()
    with
      | exn ->
        sprintf "Exception occurred trying to start IrisService: %s" exn.Message
        |> Logger.err nodeid tag

  // ** Stop

  member self.Stop() =
    try
      dispose raftserver
      dispose wsserver
      dispose httpserver
      dispose logger
    with
      | exn ->
        sprintf "Exception occurred trying to dispose IrisService: %s" exn.Message
        |> Logger.err nodeid tag
