module Iris.Service.Raft.Server

open System
open System.Threading
open Iris.Core
open Iris.Core.Utils
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

type RaftServer(options: RaftOptions, context: ZeroMQ.ZContext) as this =
  let database =
    match openDB options.DataDir with
      | Some db ->
        this.Log "Found database at %A" options.DataDir
        db
      | _       ->
        match createDB options.DataDir with
          | Some db ->
            this.Log "Created new database at %A" options.DataDir
            db
          | _       ->
            this.Log "Unable to open/create a database. Aborting."
            failwith "Persistence Error: unable to open/create a database."

  let serverState = ref Stopped

  let server : Zmq.Rep option ref = ref None
  let periodictoken               = ref None

  let cbs = this :> IRaftCallbacks<_,_>
  let appState = mkState context options |> newTVar
  let connections = newTVar Map.empty

  //                           _
  //  _ __ ___   ___ _ __ ___ | |__   ___ _ __ ___
  // | '_ ` _ \ / _ \ '_ ` _ \| '_ \ / _ \ '__/ __|
  // | | | | | |  __/ | | | | | |_) |  __/ |  \__ \
  // |_| |_| |_|\___|_| |_| |_|_.__/ \___|_|  |___/

  member self.Periodic() =
    warn "remove this method when not needed anymore"
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
    try
      serverState := Starting

      server := Some (startServer appState cbs)

      initialize appState cbs

      let tkn = startPeriodic appState cbs
      periodictoken := Some tkn

      serverState := Running
    with
      | :? ZeroMQ.ZException as exn ->
        serverState := Failed (sprintf "[ZMQ Exception] %A" exn.Message)
      | exn ->
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
    match !serverState with
      | Starting | Stopping | Stopped | Failed _ -> ()
      | Running ->
        serverState := Stopping

        let _ = tryLeave appState cbs

        Option.bind (dispose >> Some) (!server) |> ignore

        // cancel the running async tasks
        cancelToken periodictoken

        readTVar connections
        |> atomically
        |> resetConnections

        let state = readTVar appState |> atomically
        saveRaft state.Raft database

        serverState := Stopped
    dispose database

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
      let request = RequestVote(state.Raft.Node.Id,req)

      self.Log <| sprintf "SendRequestVote to %A" (nodeUri node.Data)

      let conns = readTVar connections |> atomically

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | RequestVoteResponse(sender, vote) -> Some vote
            | resp -> failwithf "Expected VoteResponse but got: %A" resp
        | _ -> None

    member self.SendAppendEntries (node: Node) (request: AppendEntries) =
      let state = self.State
      let conns = readTVar connections |> atomically
      let request = AppendEntries(state.Raft.Node.Id, request)

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | AppendEntriesResponse(sender, ar) -> Some ar
            | resp -> failwithf "Expected AppendEntriesResponse but got: %A" resp
        | _ -> None

    member self.SendInstallSnapshot node is =
      let state = self.State
      let conns = readTVar connections |> atomically

      let request = InstallSnapshot(state.Raft.Node.Id, is)

      self.Log <| sprintf "SendInstallSnapshot to %A" (nodeUri node.Data)

      let response, conns = performRequest request node state conns

      writeTVar connections conns |> atomically

      match response with
        | Some message ->
          match message with
            | InstallSnapshotResponse(sender, ar) -> Some ar
            | resp -> failwithf "Expected InstallSnapshotResponse but got: %A" resp
        | _ -> None

    member self.ApplyLog sm =
      sprintf "Applying state machine command (%A)" sm
      |> self.Log

    //  _   _           _
    // | \ | | ___   __| | ___  ___
    // |  \| |/ _ \ / _` |/ _ \/ __|
    // | |\  | (_) | (_| |  __/\__ \
    // |_| \_|\___/ \__,_|\___||___/

    member self.NodeAdded node   =
      sprintf "Node was added. %s" (string node.Id)
      |> self.Log

    member self.NodeUpdated node =
      sprintf "Node was updated. %s" (string node.Id)
      |> self.Log

    member self.NodeRemoved node =
      sprintf "Node was removed. %s" (string node.Id)
      |> self.Log

    member self.Configured nodes =
      sprintf "Cluster configuration done!"
      |> self.Log

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
      |> self.Log

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
            sprintf "PersistVote for node: %A" (string peer.Id) |> self.Log
          | _         ->
            meta.VotedFor <- null
            saveRaftMetadata meta database
            "PersistVote reset VotedFor" |> self.Log
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
        sprintf "PersistTerm term: %A" term |> self.Log
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
        |> self.Log
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
        |> self.Log
      with
        | exn -> handleException "DeleteLog" exn

    member self.LogMsg node str =
      if self.State.Options.Debug then
        let now = DateTime.Now
        let tid = Thread.CurrentThread.ManagedThreadId
        printfn "[%d / %s / %s] %s" (unixTime now) (String.Format("{0,2}", string tid)) (string node.Id) str

  override self.ToString() =
    sprintf "Database:%s\nConnections:%s\nNodes:%s\nRaft:%s\nLog:%s"
      (dumpDb database |> indent 4)
      (readTVar connections |> atomically |> string |> indent 4)
      (Map.fold (fun m _ t -> sprintf "%s\n%s" m (string t)) "" self.State.Raft.Peers |> indent 4)
      (self.State.Raft.ToString() |> indent 4)
      (string self.State.Raft.Log |> indent 4)
