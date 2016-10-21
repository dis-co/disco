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

//  ____        __ _     ____                             ____  _        _
// |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __  / ___|| |_ __ _| |_ ___
// | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__| \___ \| __/ _` | __/ _ \
// |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |     ___) | || (_| | ||  __/
// |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|    |____/ \__\__,_|\__\___|

type RaftServerState =
  | Starting
  | Running
  | Stopping
  | Stopped
  | Failed  of string

[<AutoOpen>]
module RaftServerStateHelpers =

  let hasFailed = function
    | Failed _ -> true
    |        _ -> false

//  ____        __ _     ____
// |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __
// | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__|
// |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |
// |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|

type RaftServer(options: IrisConfig, context: ZeroMQ.ZContext) as this =
  let locker = new Object()

  let serverState = ref Stopped

  let server : Zmq.Rep option ref = ref None
  let periodictoken               = ref None

  let cbs = this :> IRaftCallbacks
  let appState =
    match mkState context options with
    | Right state -> newTVar state
    | Left error  -> Error.exitWith error

  let connections = newTVar Map.empty

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
        this.Debug "RaftServer: starting"
        serverState := Starting

        this.Debug "RaftServer: initializing server loop"
        server := Some (startServer appState cbs)

        this.Debug "RaftServer: initializing application"
        initialize appState cbs

        this.Debug "RaftServer: initializing periodic loop"
        let tkn = startPeriodic appState cbs
        periodictoken := Some tkn

        this.Debug "RaftServer: running"
        serverState := Running
      with
        | :? ZeroMQ.ZException as exn ->
          this.Err <| sprintf "RaftServer: ZMQ Exeception in Start: %A" exn.Message
          serverState := Failed (sprintf "[ZMQ Exception] %A" exn.Message)

        | exn ->
          this.Err <| sprintf "RaftServer: Exeception in Start: %A" exn.Message
          serverState := Failed exn.Message

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
      match !serverState with
      | Starting | Stopping | Stopped | Failed _ as state ->
        this.Debug <| sprintf "RaftServer: stopping failed. Invalid state %A" state

      | Running ->
        this.Debug "RaftServer: stopping"
        serverState := Stopping

        // cancel the running async tasks so we don't cause an election
        this.Debug "RaftServer: cancel periodic loop"
        cancelToken periodictoken

        this.Debug "RaftServer: dispose server"
        Option.bind (dispose >> Some) (!server) |> ignore

        this.Debug "RaftServer: disposing sockets"
        readTVar connections
        |> atomically
        |> resetConnections

        this.Debug "RaftServer: saving state to disk"
        let state = readTVar appState |> atomically
        saveRaft options state.Raft
        |> Either.mapError
          (fun err ->
            printfn "An error occurred: %A" err)
        |> ignore

        this.Debug "RaftServer: stopped"
        serverState := Stopped

  member self.Options
    with get () =
      let state = readTVar appState |> atomically
      state.Options
    and set opts =
      let state = readTVar appState |> atomically
      writeTVar appState { state with Options = opts } |> atomically

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

  member self.ServerState with get () = !serverState

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
      let conns = readTVar connections |> atomically

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | RequestVoteResponse(sender, vote) -> Some vote
            | resp ->
              sprintf "SendRequestVote: Unexpected Response: %A" resp
              |> this.Err
              None
        | _ ->
          sprintf "SendRequestVote: No response received for request to %s" (string node.Id)
          |> this.Err
          None

    member self.SendAppendEntries (node: RaftNode) (request: AppendEntries) =
      let state = self.State
      let conns = readTVar connections |> atomically
      let request = AppendEntries(state.Raft.Node.Id, request)

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | AppendEntriesResponse(sender, ar) -> Some ar
            | resp ->
              sprintf "SendAppendEntries: Unexpected Response:  %A" resp
              |> this.Err
              None
        | _ ->
          sprintf "SendAppendEntries: No response received for request to %s" (string node.Id)
          |> this.Err
          None

    member self.SendInstallSnapshot node is =
      let state = self.State
      let conns = readTVar connections |> atomically

      let request = InstallSnapshot(state.Raft.Node.Id, is)

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | InstallSnapshotResponse(sender, ar) -> Some ar
            | resp ->
              sprintf "SendInstallSnapshot: Unexpected Response: %A" resp
              |> this.Err
              None
        | _ ->
          sprintf "SendInstallSnapshot: No response received for request to %s" (string node.Id)
          |> this.Err
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
      |> this.Info

    //  _   _           _
    // | \ | | ___   __| | ___  ___
    // |  \| |/ _ \ / _` |/ _ \/ __|
    // | |\  | (_) | (_| |  __/\__ \
    // |_| \_|\___/ \__,_|\___||___/

    member self.NodeAdded node   =
      try
        sprintf "Node was added. %s" (string node.Id)
        |> this.Debug

        match onNodeAdded with
        | Some cb -> cb node
        | _       -> ()

      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeUpdated node =
      try
        sprintf "Node was updated. %s" (string node.Id)
        |> this.Debug

        match onNodeUpdated with
        | Some cb -> cb node
        | _       -> ()
      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeRemoved node =
      try
        sprintf "Node was removed. %s" (string node.Id)
        |> this.Debug

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

      this.Debug logstr

    member self.PrepareSnapshot (raft: Raft) =
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
      |> this.Debug

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

      this.Debug logstr

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

        "PersistVote reset VotedFor" |> this.Debug
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

        sprintf "PersistTerm term: %A" term |> this.Debug
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
        |> this.Debug
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
        |> this.Debug
      with
        | exn -> handleException "DeleteLog" exn

    member self.LogMsg level node str =
      let doLog msg =
        let now = DateTime.Now |> unixTime
        let tid = String.Format("[{0,2}]", Thread.CurrentThread.ManagedThreadId)
        let lvl = String.Format("[{0,5}]", string level)
        let log = sprintf "%s [%d / %s / %s] %s" lvl now tid (string node.Id) msg

        match onLogMsg with
          | Some cb -> cb level log
          | _ -> ()

        printfn "%s" log

      match self.State.Options.RaftConfig.LogLevel with
      | Debug -> doLog str
      | Info  -> match level with
                  | Info | Warn | Err -> doLog str
                  | _ -> ()
      | Warn  -> match level with
                  | Warn | Err   -> doLog str
                  | _ -> ()
      | Err   -> // default is to show only errors
                match level with
                  | Err -> doLog str
                  | _ -> ()

  override self.ToString() =
    sprintf "Connections:%s\nNodes:%s\nRaft:%s\nLog:%s"
      (readTVar connections |> atomically |> string |> indent 4)
      (Map.fold (fun m _ t -> sprintf "%s\n%s" m (string t)) "" self.State.Raft.Peers |> indent 4)
      (self.State.Raft.ToString() |> indent 4)
      (string self.State.Raft.Log |> indent 4)

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
        "requesting to join" |> this.Debug

        let leader = tryJoin (IpAddress.Parse ip) (uint32 port) cbs state

        match leader with
        | Some leader ->
          sprintf "Reached leader: %A Adding to nodes." leader.Id
          |> infoMsg state cbs
          do! addNodeM leader
          do! becomeFollower ()
        | _ -> "Joining cluster failed." |> errMsg state cbs

      } |> evalRaft state.Raft cbs

    writeTVar appState (RaftContext.updateRaft newstate state)
    |> atomically

  member self.LeaveCluster() =
    let state = readTVar appState |> atomically
    let newstate =
      raft {
        "requesting to leave" |> this.Debug

        do! setTimeoutElapsedM 0u

        match tryLeave appState cbs with
        | Some true ->
          "Successfully left cluster." |> infoMsg state cbs
        | _ ->
          "Could not leave cluster." |> infoMsg state cbs

        do! becomeFollower ()

        let! peers = getNodesM ()

        for kv in peers do
          do! removeNodeM kv.Value

      } |> evalRaft state.Raft cbs

    writeTVar appState (RaftContext.updateRaft newstate state)
    |> atomically


  member self.AddNode(id: string, ip: string, port: int) =
    let state = readTVar appState |> atomically

    if isLeader state.Raft then

      let change =
        { Node.create (Id.Parse id) with
            IpAddr = IpAddress.Parse ip
            Port   = uint16 port }
        |> NodeAdded

      let result =
        raft {
          let! term = currentTermM ()
          let entry = JointConsensus(Id.Create(), 0u, term, [| change |], None)
          do! debug "AddNode: appending entry to enter joint-consensus"
          return! receiveEntry entry
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
      this.Err "Unable to add node. Not leader."
      None

  member self.RmNode(id: string) =
    let state = readTVar appState |> atomically

    if isLeader state.Raft then

      let result =
        raft {
          let! node = getNodeM (Id.Parse id)
          match node with
          | Some peer ->
            let! term = currentTermM ()
            let changes = [| NodeRemoved peer |]
            let entry = JointConsensus(Id.Create(), 0u, term, changes, None)
            do! debug "RmNode: appending entry to enter joint-consensus"
            let! appended = receiveEntry entry
            return Some appended
          | _ ->
            do! warn "Node could not be removed. Node not found."
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
      this.Err "Unable to remove node. Not leader."
      None
