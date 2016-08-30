module Iris.Service.Raft.Server

open System
open System.Threading
open Iris.Core
open Iris.Core.Utils
open Iris.Service
open Iris.Raft
// open FSharpx.Stm
open FSharpx.Functional
open Utilities
open Stm
open Db


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

type RaftServer(options: Config, context: ZeroMQ.ZContext) as this =
  let locker = new Object()

  let database =
    match openDB options.RaftConfig.DataDir with
    | Some db -> db
    | _       ->
      match createDB options.RaftConfig.DataDir with
        | Some db -> db
        | _       ->
          failwith "Persistence Error: unable to open/create a database."

  let serverState = ref Stopped

  let server : Zmq.Rep option ref = ref None
  let periodictoken               = ref None

  let cbs = this :> IRaftCallbacks<StateMachine>
  let appState = mkState context options |> newTVar
  let connections = newTVar Map.empty

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
        saveRaft state.Raft database

        this.Debug "RaftServer: dispose database"
        dispose database

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

  interface IRaftCallbacks<StateMachine> with

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
              this.Err <| sprintf "SendRequestVote: Unexpected Response: %A" resp
              None
        | _ ->
          this.Err <| sprintf "SendRequestVote: No response received for request to %s"
                       (string node.Id)
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
              this.Err <| sprintf "SendAppendEntries: Unexpected Response:  %A" resp
              None
        | _ ->
          this.Err <| sprintf "SendAppendEntries: No response received for request to %s"
                       (string node.Id)
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
              this.Err <| sprintf "SendInstallSnapshot: Unexpected Response: %A" resp
              None
        | _ ->
          this.Err <| sprintf "SendInstallSnapshot: No response received for request to %s"
                       (string node.Id)
          None

    member self.ApplyLog sm =
      sprintf "Applying state machine command (%A)" sm
      |> this.Info

    //  _   _           _
    // | \ | | ___   __| | ___  ___
    // |  \| |/ _ \ / _` |/ _ \/ __|
    // | |\  | (_) | (_| |  __/\__ \
    // |_| \_|\___/ \__,_|\___||___/

    member self.NodeAdded node   =
      try
        match findNode node.Id database with
          | Some _ -> updateNode database node
          |      _ -> insertNode database node
        sprintf "Node was added. %s" (string node.Id)
        |> this.Debug
      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeUpdated node =
      try
        match findNode node.Id database with
          | Some _ -> updateNode database node
          |      _ -> insertNode database node
        sprintf "Node was updated. %s" (string node.Id)
        |> this.Debug
      with
        | exn -> handleException "NodeAdded" exn

    member self.NodeRemoved node =
      try
        match findNode node.Id database with
          | Some _ -> deleteNode database node
          |      _ ->
            sprintf "Node could not be removed. Not found: %s" (string node.Id)
            |> this.Err

        sprintf "Node was removed. %s" (string node.Id)
        |> this.Debug
      with
        | exn -> handleException "NodeAdded" exn


    //   ____ _
    //  / ___| |__   __ _ _ __   __ _  ___  ___
    // | |   | '_ \ / _` | '_ \ / _` |/ _ \/ __|
    // | |___| | | | (_| | | | | (_| |  __/\__ \
    //  \____|_| |_|\__,_|_| |_|\__, |\___||___/
    //                          |___/


    member self.Configured nodes =
      sprintf "Cluster configuration done!"
      |> this.Debug

    member self.PrepareSnapshot raft = failwith "FIXME: PrepareSnapshot"
    member self.RetrieveSnapshot ()  = failwith "FIXME: RetrieveSnapshot"
    member self.PersistSnapshot log  = failwith "FIXME: PersistSnapshot"

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
      sprintf "state changed from %A to %A" old current
      |> this.Debug

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
        let meta =
          match getMetadata database with
            | Some meta -> meta
            | _         ->
              initMetadata database |> ignore
              let state = readTVar appState |> atomically
              saveMetadata state.Raft database

        match node with
          | Some peer ->
            meta.VotedFor <- string peer.Id
            saveRaftMetadata meta database
            sprintf "PersistVote for node: %A" (string peer.Id) |> this.Debug
          | _         ->
            meta.VotedFor <- null
            saveRaftMetadata meta database
            "PersistVote reset VotedFor" |> this.Debug
      with
        | exn -> handleException "PersistTerm" exn

    /// ## Persit the new term into the database
    ///
    /// Save the current term to the database.
    ///
    /// ### Signature:
    /// - arg: arg
    /// - arg: arg
    /// - arg: arg
    ///
    /// Returns: unit
    member self.PersistTerm term =
      try
        let meta =
          match getMetadata database with
            | Some meta -> meta
            | _         ->
              initMetadata database |> ignore
              let state = readTVar appState |> atomically
              saveMetadata state.Raft database

        meta.Term <- int64 term
        saveRaftMetadata meta database
        sprintf "PersistTerm term: %A" term |> this.Debug
      with
        | exn -> handleException "PersistTerm" exn

    /// ## Persist a log to disk
    ///
    /// Save a log to the database.
    ///
    /// ### Signature:
    /// - log: Log to persist
    ///
    /// Returns: unit
    member self.PersistLog log =
      try
        insertLogs log database
        sprintf "PersistLog insert id: %A" (Log.id log |> string)
        |> this.Debug
      with
        | _ ->
          try
            updateLogs log database
          with
            | exn ->
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
        deleteLogs log database
        |> sprintf "DeleteLog id: %A result: %b" (Log.id log |> string)
        |> this.Debug
      with
        | exn -> handleException "DeleteLog" exn

    member self.LogMsg level node str =
      let doLog msg =
        let now = DateTime.Now |> unixTime
        let tid = String.Format("[{0,2}]", Thread.CurrentThread.ManagedThreadId)
        let lvl = String.Format("[{0,5}]", string level)
        printfn "%s [%d / %s / %s] %s" lvl now tid (string node.Id) msg

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
    sprintf "Database:%s\nConnections:%s\nNodes:%s\nRaft:%s\nLog:%s"
      (dumpDB database |> indent 4)
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

    writeTVar appState (updateRaft newstate state)
    |> atomically

  member self.LeaveCluster() =
    let state = readTVar appState |> atomically
    let newstate =
      raft {
        "requesting to leave" |> this.Debug

        do! setTimeoutElapsedM 0UL

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

    writeTVar appState (updateRaft newstate state)
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
          let entry = JointConsensus(Id.Create(), 0UL, term, [| change |], None)
          do! debug "AddNode: appending entry to enter joint-consensus"
          return! receiveEntry entry
        }
        |> runRaft state.Raft cbs

      match result with
      | Right (appended, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (updateRaft raftState state) |> atomically

        // block until entry has been committed
        let ok = waitForCommit appended appState cbs

        if ok then
          Some appended
        else
          None

      | Left (err, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (updateRaft raftState state) |> atomically
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
            let entry = JointConsensus(Id.Create(), 0UL, term, changes, None)
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
        writeTVar appState (updateRaft raftState state) |> atomically

        // block until entry has been committed
        let ok = waitForCommit appended appState cbs

        if ok then
          Some appended
        else
          None

      | Left (err, raftState) ->
        // save the new raft value back to the TVar
        writeTVar appState (updateRaft raftState state) |> atomically
        None

      | _ -> None
    else
      this.Err "Unable to remove node. Not leader."
      None
