namespace Iris.Service

open System
open System.Threading
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Zmq
open Iris.Raft
// open FSharpx.Stm
open FSharpx.Functional
open Utilities
open Persistence
open Stm

//  ____        __ _     ____
// |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __
// | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__|
// |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |
// |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|

type RaftServer(options: IrisConfig, context: ZeroMQ.ZContext) as self =
  let locker = new Object()

  let mutable serverState = ServiceStatus.Stopped

  let server : Zmq.Rep option ref = ref None
  let periodictoken               = ref None

  let cbs = self :> IRaftCallbacks

  let appState =
    match mkContext context options with
    | Right state -> newTVar state
    | Left error  -> Error.exitWith error

  //            _ _ _                _
  //   ___ __ _| | | |__   __ _  ___| | _____
  //  / __/ _` | | | '_ \ / _` |/ __| |/ / __|
  // | (_| (_| | | | |_) | (_| | (__|   <\__ \
  //  \___\__,_|_|_|_.__/ \__,_|\___|_|\_\___/

  let mutable onLogMsg         : Option<LogLevel -> string -> unit>     = None
  let mutable onConfigured     : Option<RaftNode array -> unit>        = None
  let mutable onNodeAdded      : Option<RaftNode -> unit>              = None
  let mutable onNodeUpdated    : Option<RaftNode -> unit>              = None
  let mutable onNodeRemoved    : Option<RaftNode -> unit>              = None
  let mutable onStateChanged   : Option<RaftState -> RaftState -> unit> = None
  let mutable onApplyLog       : Option<StateMachine -> unit>          = None
  let mutable onCreateSnapshot : Option<unit -> StateMachine>          = None

  member self.OnLogMsg
    with set cb = onLogMsg <- Some cb

  member self.OnConfigured
    with set cb = onConfigured <- Some cb

  member self.OnNodeAdded
    with set cb = onNodeAdded <- Some cb

  member self.OnNodeUpdated
    with set cb = onNodeUpdated <- Some cb

  member self.OnNodeRemoved
    with set cb = onNodeRemoved <- Some cb

  member self.OnStateChanged
    with set cb = onStateChanged <- Some cb

  member self.OnApplyLog
    with set cb = onApplyLog <- Some cb

  member self.OnCreateSnapshot
    with set cb = onCreateSnapshot <- Some cb

  member self.IsLeader
    with get () =
      let state = readTVar appState |> atomically
      state.Raft.IsLeader

  //                           _
  //  _ __ ___   ___ _ __ ___ | |__   ___ _ __ ___
  // | '_ ` _ \ / _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
  // | | | | | |  __/ | | | | | |_) |  __/ |  \__ \
  // |_| |_| |_|\___|_| |_| |_|_.__/ \___|_|  |___/

  member self.Periodic() =
    let state = readTVar appState |> atomically
    periodicR state cbs
    |> writeTVar appState
    |> atomically

  /// ## Start the Raft engine
  ///
  /// Start the Raft engine and start processing requests.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Start() =
    printfn "Starting Raft Server"
    lock locker <| fun _ ->
      try
        self.Debug "RaftServer: starting"
        serverState <- ServiceStatus.Starting

        self.Debug "RaftServer: initializing server loop"
        server := Some (startServer appState cbs)

        self.Debug "RaftServer: initializing application"
        initialize appState cbs

        self.Debug "RaftServer: initializing periodic loop"
        let tkn = startPeriodic appState cbs
        periodictoken := Some tkn

        self.Debug "RaftServer: running"
        serverState <- ServiceStatus.Running
      with
        | exn ->
          self.Cancel()
          self.Err <| sprintf "RaftServer: Exeception in Start: %A" exn.Message
          serverState <- ServiceStatus.Failed (Other exn.Message)

  /// ## Cancel
  ///
  /// Cancel the periodic loop, dispose of the server socket and reset all connections to self
  /// server.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member private self.Cancel() =
    try
      // cancel the running async tasks so we don't cause an election
      self.Debug "RaftServer: cancel periodic loop"
      cancelToken periodictoken
    with
      | exn ->
        exn.Message
        |> sprintf "RaftServer Error: could not cancel periodic loop: %s"
        |> self.Err

    try
      // dispose of the server
      self.Debug "RaftServer: disposing server"
      Option.bind (dispose >> Some) (!server) |> ignore
    with
      | exn ->
        exn.Message
        |> sprintf "RaftServer Error: Could not dispose of server: %s"
        |> self.Err

    try
      self.Debug "RaftServer: disposing sockets"
      self.State.Connections
      |> resetConnections
    with
      | exn ->
        exn.Message
        |> sprintf "RaftServer Error: Could not dispose of connections: %s"
        |> self.Err

  /// ## Stop the Raft engine, sockets and all.
  ///
  /// Stop the Raft engine
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Stop() =
    lock locker <| fun _ ->
      if serverState = ServiceStatus.Running then
        self.Debug "RaftServer: stopping"
        serverState <- ServiceStatus.Stopping

        // cancel the running async tasks so we don't cause an election
        self.Debug "RaftServer: cancel periodic loop"
        cancelToken periodictoken

        self.Debug "RaftServer: dispose server"
        Option.bind (dispose >> Some) (!server) |> ignore

        self.Debug "RaftServer: disposing sockets"
        self.State.Connections
        |> resetConnections

        self.Debug "RaftServer: saving state to disk"
        let state = readTVar appState |> atomically
        saveRaft options state.Raft
        |> Either.mapError (sprintf "An error occurred saving state to disk: %A" >> self.Err)
        |> ignore

        self.Debug "RaftServer: stopped"
        serverState <- ServiceStatus.Stopped

  member self.Options
    with get () =
      let state = readTVar appState |> atomically
      state.Options
    and set opts =
      let state = readTVar appState |> atomically
      writeTVar appState { state with Options = opts } |> atomically

  member self.GetClient (node: RaftNode) =
    let state = self.State
    match RaftContext.getConnection state node.Id with
    | Some socket -> socket
    | _ ->
      let socket = mkReqSocket node state.Context
      socket
      |> RaftContext.addConnection state
      |> writeTVar appState
      |> atomically
      socket

  member self.Context
    with get () = context

  /// Alas, we may only *look* at the current state.
  member self.State
    with get () = atomically (readTVar appState)

  member self.Append (entry: StateMachine) =
    appendEntry entry appState cbs

  member self.ForceTimeout() =
    forceElection appState cbs |> atomically

  member self.Debug (msg: string) : unit =
    let state = self.State
    cbs.LogMsg Debug state.Raft.Node msg

  member self.Info (msg: string) : unit =
    let state = self.State
    cbs.LogMsg Info state.Raft.Node msg

  member self.Warn (msg: string) : unit =
    let state = self.State
    cbs.LogMsg Warn state.Raft.Node msg

  member self.Err (msg: string) : unit =
    let state = self.State
    cbs.LogMsg Err state.Raft.Node msg

  member self.ServerState with get () = serverState

  //  ____  _                           _     _
  // |  _ \(_)___ _ __   ___  ___  __ _| |__ | | ___
  // | | | | / __| '_ \ / _ \/ __|/ _` | '_ \| |/ _ \
  // | |_| | \__ \ |_) | (_) \__ \ (_| | |_) | |  __/
  // |____/|_|___/ .__/ \___/|___/\__,_|_.__/|_|\___|
  //             |_|

  interface IDisposable with
    member self.Dispose() =
      self.Stop()

  //  ____        __ _     ___       _             __
  // |  _ \ __ _ / _| |_  |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___
  // | |_) / _` | |_| __|  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \
  // |  _ < (_| |  _| |_   | || | | | ||  __/ |  |  _| (_| | (_|  __/
  // |_| \_\__,_|_|  \__| |___|_| |_|\__\___|_|  |_|  \__,_|\___\___|

  interface IRaftCallbacks with

    member self.SendRequestVote node req  =
      let state = self.State
      let request = RequestVote(state.Raft.Node.Id,req)
      let client = self.GetClient node
      let result = performRequest request client

      match result with
      | Right response ->
        match response with
        | RequestVoteResponse(sender, vote) -> Some vote
        | resp ->
          resp
          |> sprintf "SendRequestVote: Unexpected Response: %A"
          |> self.Err
          None
      | Left error ->
        nodeUri node
        |> sprintf "SendRequestVote: encountered error \"%A\" during request to %s" error
        |> self.Err
        None

    member self.SendAppendEntries (node: RaftNode) (request: AppendEntries) =
      let state = self.State
      let request = AppendEntries(state.Raft.Node.Id, request)
      let client = self.GetClient node
      let result = performRequest request client

      match result with
      | Right response ->
        match response with
        | AppendEntriesResponse(sender, ar) -> Some ar
        | resp ->
          resp
          |> sprintf "SendAppendEntries: Unexpected Response:  %A"
          |> self.Err
          None
      | Left error ->
        nodeUri node
        |> sprintf "SendAppendEntries: Error \"%A\" received for request to %s" error
        |> self.Err
        None

    member self.SendInstallSnapshot node is =
      let state = self.State
      let client = self.GetClient node
      let request = InstallSnapshot(state.Raft.Node.Id, is)
      let result = performRequest request client

      match result with
      | Right response ->
        match response with
        | InstallSnapshotResponse(sender, ar) -> Some ar
        | resp ->
          resp
          |> sprintf "SendInstallSnapshot: Unexpected Response: %A"
          |> self.Err
          None
      | Left error ->
        nodeUri node
        |> sprintf "SendInstallSnapshot: Error \"%A\" received for request to %s" error
        |> self.Err
        None

    //     _                _          ____               _
    //    / \   _ __  _ __ | |_   _   / ___|_ __ ___   __| |
    //   / _ \ | '_ \| '_ \| | | | | | |   | '_ ` _ \ / _` |
    //  / ___ \| |_) | |_) | | |_| | | |___| | | | | | (_| |
    // /_/   \_\ .__/| .__/|_|\__, |  \____|_| |_| |_|\__,_|
    //         |_|   |_|      |___/

    member self.ApplyLog sm =
      match onApplyLog with
      | Some cb -> cb sm
      | _       -> ()
      sprintf "Applying state machine command (%A)" sm
      |> self.Info

    //  _   _           _
    // | \ | | ___   __| | ___  ___
    // |  \| |/ _ \ / _` |/ _ \/ __|
    // | |\  | (_) | (_| |  __/\__ \
    // |_| \_|\___/ \__,_|\___||___/

    member self.NodeAdded node   =
      try
        sprintf "Node was added. %s" (string node.Id)
        |> self.Debug

        match onNodeAdded with
        | Some cb -> cb node
        | _       -> ()

      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeUpdated node =
      try
        sprintf "Node was updated. %s" (string node.Id)
        |> self.Debug

        match onNodeUpdated with
        | Some cb -> cb node
        | _       -> ()
      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeRemoved node =
      try
        sprintf "Node was removed. %s" (string node.Id)
        |> self.Debug

        match onNodeRemoved with
        | Some cb -> cb node
        | _       -> ()
      with
        | exn -> handleException "NodeAdded" exn

    //   ____ _
    //  / ___| |__   __ _ _ __   __ _  ___  ___
    // | |   | '_ \ / _` | '_ \ / _` |/ _ \/ __|
    // | |___| | | | (_| | | | | (_| |  __/\__ \
    //  \____|_| |_|\__,_|_| |_|\__, |\___||___/
    //                          |___/

    member self.Configured nodes =
      let logstr = sprintf "Cluster configuration done!"

      match onConfigured with
      | Some cb -> cb nodes
      | _       -> ()

      self.Debug logstr

    member self.PrepareSnapshot (raft: RaftValue) =
      match onCreateSnapshot with
      | Some cb ->
        let currIdx = Log.index raft.Log
        let prevTerm = Log.term raft.Log
        let term = raft.CurrentTerm
        let nodes = raft.Peers |> Map.toArray |> Array.map snd
        let data = cb ()
        Snapshot(Id.Create(), currIdx + 1u, term, currIdx, prevTerm, nodes, data)
        |> Log.fromEntries
        |> Some
      | _ ->
        self.Err "Unable to create snapshot. No data handler specified."
        None

    member self.PersistSnapshot log =
      sprintf "PersistSnapshot insert id: %A" (LogEntry.getId log |> string)
      |> self.Debug

    member self.RetrieveSnapshot () =
      failwith "implement RetrieveSnapshot again"

    /// ## Raft state changed
    ///
    /// Signals the Raft instance has changed its State.
    ///
    /// ### Signature:
    /// - old: old Raft state
    /// - new: new Raft state
    ///
    /// Returns: unit
    member self.StateChanged old current =
      let logstr = sprintf "state changed from %A to %A" old current

      match onStateChanged with
      | Some cb -> cb old current
      | _       -> ()

      self.Debug logstr

    /// ## Persist the vote for passed node to disk.
    ///
    /// Persist the vote for the passed node to disk.
    ///
    /// ### Signature:
    /// - node: Node to persist
    ///
    /// Returns: unit
    member self.PersistVote (node: RaftNode option) =
      try
        self.State
        |> RaftContext.getRaft
        |> saveRaft options
        |> Either.mapError
          (fun err ->
            printfn "Could not persit vote change. %A" err)
        |> ignore

        "PersistVote reset VotedFor" |> self.Debug
      with
        | exn -> handleException "PersistTerm" exn

    /// ## Persit the new term in metadata file
    ///
    /// Save the current term in metatdata file.
    ///
    /// ### Signature:
    /// - arg: arg
    /// - arg: arg
    /// - arg: arg
    ///
    /// Returns: unit
    member self.PersistTerm term =
      try
        self.State
        |> RaftContext.getRaft
        |> saveRaft options
        |> Either.mapError
          (fun err ->
            printfn "Could not persit vote change. %A" err)
        |> ignore

        sprintf "PersistTerm term: %A" term |> self.Debug
      with
        | exn -> handleException "PersistTerm" exn

    /// ## Persist a log to disk
    ///
    /// Save a log to disk.
    ///
    /// ### Signature:
    /// - log: Log to persist
    ///
    /// Returns: unit
    member self.PersistLog log =
      try
        sprintf "PersistLog insert id: %A" (LogEntry.getId log |> string)
        |> self.Debug
      with
        | exn->
          handleException "PersistLog" exn

    /// ## Callback to delete a log entry from database
    ///
    /// Delete a log entry from the database.
    ///
    /// ### Signature:
    /// - log: LogEntry to delete
    ///
    /// Returns: unit
    member self.DeleteLog log =
      try
        sprintf "DeleteLog id: %A" (LogEntry.getId log |> string)
        |> self.Debug
      with
        | exn -> handleException "DeleteLog" exn

    member self.LogMsg level node str =
      let log = sprintf "NODE: %s %s" (string node.Id) str
      match onLogMsg with
        | Some cb -> cb level log
        | _ -> ()

  override self.ToString() =
    sprintf "Connections:%s\nNodes:%s\nRaft:%s\nLog:%s"
      (self.State.Connections |> string |> String.indent 4)
      (Map.fold (fun m _ t -> sprintf "%s\n%s" m (string t)) "" self.State.Raft.Peers |> String.indent 4)
      (self.State.Raft.ToString() |> String.indent 4)
      (string self.State.Raft.Log |> String.indent 4)

  //   ____ _           _               ____ _
  //  / ___| |_   _ ___| |_ ___ _ __   / ___| |__   __ _ _ __   __ _  ___  ___
  // | |   | | | | / __| __/ _ \ '__| | |   | '_ \ / _` | '_ \ / _` |/ _ \/ __|
  // | |___| | |_| \__ \ ||  __/ |    | |___| | | | (_| | | | | (_| |  __/\__ \
  //  \____|_|\__,_|___/\__\___|_|     \____|_| |_|\__,_|_| |_|\__, |\___||___/
  //                                                           |___/

  member self.JoinCluster(ip: string, port: int) =
    let state = readTVar appState |> atomically
    let newstate =
      raft {
        "requesting to join" |> self.Debug

        let leader = tryJoin (IpAddress.Parse ip) (uint32 port) cbs state

        match leader with
        | Right leader ->
          sprintf "Reached leader: %A Adding to nodes." leader.Id
          |> infoMsg state cbs
          do! Raft.addNodeM leader
          do! Raft.becomeFollower ()
        | Left err ->
          sprintf "Joining cluster failed. %A" err
          |> errMsg state cbs

      } |> evalRaft state.Raft cbs

    writeTVar appState (RaftContext.updateRaft newstate state)
    |> atomically

  member self.LeaveCluster() =
    let state = readTVar appState |> atomically
    let newstate =
      raft {
        "requesting to leave" |> self.Debug

        do! Raft.setTimeoutElapsedM 0u

        match tryLeave appState cbs with
        | Right true  -> "Successfully left cluster." |> infoMsg state cbs // FIXME: this might need more consequences than this
        | Right false -> "Could not leave cluster."   |> errMsg state cbs
        | Left err ->
          err
          |> sprintf "Could not leave cluster. %A"
          |> errMsg state cbs

        do! Raft.becomeFollower ()

        let! peers = Raft.getNodesM ()

        for kv in peers do
          do! Raft.removeNodeM kv.Value

      } |> evalRaft state.Raft cbs

    writeTVar appState (RaftContext.updateRaft newstate state)
    |> atomically


  member self.AddNode(id: string, ip: string, port: int) =
    let state = readTVar appState |> atomically

    if Raft.isLeader state.Raft then

      let change =
        { Node.create (Id.Parse id) with
            IpAddr = IpAddress.Parse ip
            Port   = uint16 port }
        |> NodeAdded

      let result =
        raft {
          let! term = Raft.currentTermM ()
          let entry = JointConsensus(Id.Create(), 0u, term, [| change |], None)
          do! Raft.debug "AddNode: appending entry to enter joint-consensus"
          return! Raft.receiveEntry entry
        }
        |> runRaft state.Raft cbs

      match result with
      | Right (appended, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (RaftContext.updateRaft raftState state) |> atomically

        // block until entry has been committed
        let ok = waitForCommit appended appState cbs

        if ok then
          Some appended
        else
          None

      | Left (err, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
        None
    else
      self.Err "Unable to add node. Not leader."
      None

  member self.RmNode(id: string) =
    let state = readTVar appState |> atomically

    if Raft.isLeader state.Raft then

      let result =
        raft {
          let! node = Raft.getNodeM (Id.Parse id)
          match node with
          | Some peer ->
            let! term = Raft.currentTermM ()
            let changes = [| NodeRemoved peer |]
            let entry = JointConsensus(Id.Create(), 0u, term, changes, None)
            do! Raft.debug "RmNode: appending entry to enter joint-consensus"
            let! appended = Raft.receiveEntry entry
            return Some appended
          | _ ->
            do! Raft.warn "Node could not be removed. Node not found."
            return None
        }
        |> runRaft state.Raft cbs

      match result with
      | Right (Some appended, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (RaftContext.updateRaft raftState state) |> atomically

        // block until entry has been committed
        let ok = waitForCommit appended appState cbs

        if ok then
          Some appended
        else
          None

      | Left (err, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
        None

      | _ -> None
    else
      self.Err "Unable to remove node. Not leader."
      None
