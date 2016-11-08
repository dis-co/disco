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

  let kontext = new ZContext()

  let gitserver  = new Git.Daemon(project)
  let raftserver = new RaftServer((!project).Config, kontext)
  let wsserver   = new WsServer((!project).Config, raftserver)
  let httpserver = new AssetServer((!project).Config)

  // ** setup

  let setup _ =
    ////////////////////////////////////////////////////////
    // __        __   _    ____             _        _    //
    // \ \      / /__| |__/ ___|  ___   ___| | _____| |_  //
    //  \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __| //
    //   \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_  //
    //    \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__| //
    //                                                    //
    //  _   _                 _ _                         //
    // | | | | __ _ _ __   __| | | ___ _ __ ___           //
    // | |_| |/ _` | '_ \ / _` | |/ _ \ '__/ __|          //
    // |  _  | (_| | | | | (_| | |  __/ |  \__ \          //
    // |_| |_|\__,_|_| |_|\__,_|_|\___|_|  |___/          //
    ////////////////////////////////////////////////////////


    /// ## OnOpen
    ///
    /// Register a callback with the WebSocket server that is run when new browser session has
    /// contacted this IrisSerivce. First, we send a `DataSnapshot` to the client to initialize it
    /// with the current state. Then, we append the newly created Session value to the Raft log to
    /// replicate it throughout the cluster.

    wsserver.OnOpen <- fun (session: Session) ->
      wsserver.Send session.Id (DataSnapshot store.State)
      let msg =
        match raftserver.Append(AddSession session) with
        | Right entry -> Debug, (sprintf "Added session to Raft log with id: %A" entry.Id)
        | Left  error -> Err, string error
      wsserver.Broadcast (LogMsg msg)

    /// ## OnClose
    ///
    /// Register a callback to be run when a browser as exited a session in an orderly fashion. The
    /// session is removed from the global state by appending a `RemoveSession`

    wsserver.OnClose <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Right entry -> Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
          | Left error  -> Err, string error
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    /// ## OnError
    ///
    /// Register a callback to be run if the client connection unexpectectly fails. In that case the
    /// Session is retrieved and removed from global state.

    wsserver.OnError <- fun sessionid ->
      match Map.tryFind sessionid store.State.Sessions with
      | Some session ->
        let msg =
          match raftserver.Append(RemoveSession session) with
          | Right entry -> Debug, (sprintf "Remove session added to Raft log with id: %A" entry.Id)
          | Left error  -> Err, string error
        wsserver.Broadcast (LogMsg msg)
      | _ ->
        let msg = Err, "Session not found. Something spooky is going on"
        wsserver.Broadcast (LogMsg msg)

    /// ## OnMessage
    ///
    /// Register a handler to process messages coming from the browser client. The current handling
    /// mechanism is that incoming message get appended to the `Raft` log immediately, and a log
    /// message is sent back to the client. Once the new command has been replicated throughout the
    /// system, it will be applied to the server-side global state, then pushed over the socket to
    /// be applied to all client-side global state atoms.

    wsserver.OnMessage <- fun sessionid command ->
      let msg =
        match raftserver.Append(command) with
        | Right entry -> Debug, (sprintf "Entry added to Raft log with id: %A" entry.Id)
        | Left error  -> Err, string error
      wsserver.Broadcast (LogMsg msg)

    ////////////////////////////////////////////////////////////////////
    //  ____        __ _     _   _                 _ _                //
    // |  _ \ __ _ / _| |_  | | | | __ _ _ __   __| | | ___ _ __ ___  //
    // | |_) / _` | |_| __| | |_| |/ _` | '_ \ / _` | |/ _ \ '__/ __| //
    // |  _ < (_| |  _| |_  |  _  | (_| | | | | (_| | |  __/ |  \__ \ //
    // |_| \_\__,_|_|  \__| |_| |_|\__,_|_| |_|\__,_|_|\___|_|  |___/ //
    ////////////////////////////////////////////////////////////////////

    /// ## OnConfigured
    ///
    /// Register a callback to run when a new cluster configuration has been committed, and the
    /// joint-consensus mode has been concluded.

    raftserver.OnConfigured <-
      Array.map (Node.getId >> string)
      >> Array.fold (fun s id -> sprintf "%s %s" s  id) "New Configuration with: "
      >> (fun str -> LogMsg(Iris.Core.LogLevel.Debug, str))
      >> wsserver.Broadcast

    /// ## OnLogMsg
    ///
    /// Register a callback to be called when some `Raft`-interal logging has occurred.

    raftserver.OnLogMsg <- fun _ msg ->
      wsserver.Broadcast(LogMsg(Iris.Core.LogLevel.Debug, msg))

    /// ## OnNodeAdded
    ///
    /// Register a callback to be run when the user has added a new node to the `Raft` cluster. This
    /// commences the joint-consensus mode until the new node has been caught up and is ready be a
    /// full member of the cluster.

    raftserver.OnNodeAdded   <- fun node ->
      warn "NODE NEEDS TO BE ADDED TO PROJECT NOW"
      AddNode node
      |> wsserver.Broadcast

    /// ## OnNodeUpdated
    ///
    /// Register a callback to be called when a cluster node's properties such as e.g. its node
    /// state.

    raftserver.OnNodeUpdated <- fun node ->
      warn "NODE NEEDS TO BE UPDATED IN PROJECT NOW"
      UpdateNode node
      |> wsserver.Broadcast

    /// ## OnNodeRemoved
    ///
    /// Register a callback to be run when a node was removed from the cluster, resulting into
    /// the cluster entering into joint-consensus mode until the node was successfully removed.

    raftserver.OnNodeRemoved <- fun node ->
      warn "NODE NEEDS TO BE REMOVED FROM PROJECT NOW"
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
      gitserver.Start()
      wsserver.Start()
      raftserver.Start()
    with
      | exn ->
        printfn "Exception occurred trying to start IrisService: %s" exn.Message

  member self.Start() =
    try
      gitserver.Start()
      httpserver.Start()
      wsserver.Start()
      raftserver.Start()
    with
      | exn ->
        printfn "Exception occurred trying to start IrisService: %s" exn.Message

  // ** Stop

  // ** Stop

  member self.Stop() =
    try
      dispose raftserver
      dispose wsserver
      dispose httpserver
      dispose kontext
    with
      | exn ->
        printfn "Exception occurred trying to dispose IrisService: %s" exn.Message

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
