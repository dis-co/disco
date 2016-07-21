module Iris.Service.Raft.Server

open System
open System.Threading
open Iris.Core
open Iris.Core.Utils
open Pallet.Core
open fszmq
open fszmq.Context
open fszmq.Socket
open fszmq.Polling
open FSharpx.Stm
open FSharpx.Functional
open Utilities
open Stm
open Db

type RequestWorkerType = Node<IrisNode> * RaftRequest

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
  | Failed

[<AutoOpen>]
module RaftServerStateHelpers =

  let hasFailed = function
    | Failed -> true
    | _      -> false


//  ____        __ _     ____
// |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __
// | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__|
// |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |
// |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|

type RaftServer(options: RaftOptions, context: fszmq.Context) as this =
  let timeout = 10UL

  let database =
    let path = options.DataDir </> DB_NAME
    match openDB path with
      | Some db -> db
      | _       ->
        match createDB path with
          | Some db -> db
          | _       ->
            printfn "[RaftServer] unable to open Database. Aborting."
            exit 1

  let serverState = ref Stopped

  let servertoken   = ref None
  let workertoken   = ref None
  let periodictoken = ref None

  let cbs = this :> IRaftCallbacks<_,_>
  let appState = mkState context options |> newTVar

  let requestWorker : Actor<RequestWorkerType> option ref = ref None

  let post (msg: RequestWorkerType) =
    match !requestWorker with
      | Some worker -> worker.Post msg
      | _           -> printfn "[WARNING] requestWorker was not initalized properly yet"

  //                           _
  //  _ __ ___   ___ _ __ ___ | |__   ___ _ __ ___
  // | '_ ` _ \ / _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
  // | | | | | |  __/ | | | | | |_) |  __/ |  \__ \
  // |_| |_| |_|\___|_| |_| |_|_.__/ \___|_|  |___/

  /// ## Start the Raft engine
  ///
  /// Start the Raft engine and start processing requests.
  ///
  /// ### Signature:
  /// - unit: unit
  ///
  /// Returns: unit
  member self.Start() =
    try
      serverState := Starting

      let cts = new CancellationTokenSource()
      workertoken := Some cts

      let worker = new Actor<RequestWorkerType> (requestLoop appState cbs, cts.Token)
      worker.Start()
      requestWorker := Some worker

      initialize appState cbs |> atomically

      let srvtkn = startServer appState cbs |> atomically
      let prdtkn = startPeriodic timeout appState cbs |> atomically

      servertoken   := Some srvtkn
      periodictoken := Some prdtkn

      serverState := Running
    with
      | :? ZMQError as exn ->
        serverState := Failed

  /// ## Stop the Raft engine, sockets and all.
  ///
  /// Description
  ///
  /// ### Signature:
  /// - arg: arg
  /// - arg: arg
  /// - arg: arg
  ///
  /// Returns: Type
  member self.Stop() =
    match !serverState with
      | Starting | Stopping | Stopped | Failed _ -> ()
      | Running ->
        serverState := Stopping

        // cancel the running async tasks
        cancelToken periodictoken
        cancelToken servertoken
        cancelToken workertoken

        resetConnections appState |> atomically

        let state = readTVar appState |> atomically
        saveRaft state.Raft database

        serverState := Stopped

    dispose database

  member self.Options
    with get () =
      let state = readTVar appState |> atomically
      state.Options
    and set opts =
      stm {
        let! state = readTVar appState
        do! writeTVar appState { state with Options = opts }
      } |> atomically

  member self.Context
    with get () = context

  /// Alas, we may only *look* at the current state.
  member self.State
    with get () = atomically (readTVar appState)

  member self.Append entry =
    appendEntry entry appState cbs
    |> atomically

  member self.EntryCommitted resp =
    stm {
      let! state = readTVar appState

      let committed =
        match responseCommitted resp |> runRaft state.Raft cbs with
        | Right (committed, _) -> committed
        | _                    -> false

      return committed
    } |> atomically

  member self.ForceTimeout() =
    forceElection appState cbs |> atomically

  member self.Log msg =
    let state = self.State
    cbs.LogMsg state.Raft.Node msg

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

  interface IRaftCallbacks<StateMachine,IrisNode> with
    member self.SendRequestVote node req  =
      let state = self.State
      (node, RequestVote(state.Raft.Node.Id,req)) |> post

    member self.SendAppendEntries node ae =
      let state = self.State
      (node, AppendEntries(state.Raft.Node.Id, ae)) |> post

    member self.SendInstallSnapshot node is =
      let state = self.State
      (node, InstallSnapshot(state.Raft.Node.Id, is)) |> post

    member self.ApplyLog sm      = failwith "FIXME: ApplyLog"
    member self.NodeAdded node   = failwith "FIXME: Node was added."
    member self.NodeUpdated node = failwith "FIXME: Node was updated."
    member self.NodeRemoved node = failwith "FIXME: Node was removed."
    member self.Configured nodes = failwith "FIXME: Cluster configuration done."

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
      printfn "[StateChanged] from %A to %A" old current

    /// ## Persist the vote for passed node to disk.
    ///
    /// Persist the vote for the passed node to disk.
    ///
    /// ### Signature:
    /// - node: Node to persist
    ///
    /// Returns: unit
    member self.PersistVote (node: Node option) =
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
            sprintf "[PersistVote] persisted vote for node: %A" (string peer.Id) |> self.Log
          | _         ->
            meta.VotedFor <- null
            saveRaftMetadata meta database
            sprintf "[PersistVote] persisted reset of VotedFor" |> self.Log
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
        sprintf "[PersistTerm] saved term: %A" term |> self.Log
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
        sprintf "[PersistLog] id: %A" (Log.id log |> string)
        |> self.Log
      with
        | exn -> handleException "PersistLog" exn

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
        |> sprintf "[DeleteLog] id: %A result: %b" (Log.id log |> string)
        |> self.Log
      with
        | exn -> handleException "DeleteLog" exn

    member self.HasSufficientLogs node = failwith "FIXME: HasSufficientLogs"

    member self.LogMsg node str =
      if options.Debug then
        printfn "%s" str
