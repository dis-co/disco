namespace Iris.Service

// * Imports

open System
open System.Threading
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Zmq
open Iris.Raft
open FSharpx.Functional
open Utilities
open Persistence

// * RaftServer

[<RequireQualifiedAccess>]
module RaftServer =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  [<Literal>]
  let tag = "RaftServer"

  // ** RaftCommand

  type private Cmd =
    | Join    of IpAddress * uint32
    | Leave
    | Get
    | Append  of StateMachine
    | AddNode of RaftNode
    | RmNode  of RaftNode

  [<NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | Entry of EntryResponse
    | State of RaftAppContext

  type private Message = Cmd * AsyncReplyChannel<Either<IrisError,Reply>>

  // ** waitForCommit

  /// ## waitForCommit
  ///
  /// Block execution until an entry has successfully been committed in the cluster.
  ///
  /// ### Signature:
  /// - appended: EntryResponse returned by receiveEntry
  /// - state: RaftAppContext transactional state variable
  ///
  /// Returns: bool
  let private waitForCommit (appended: EntryResponse) (state: RaftAppContext) =
    let ok = ref true
    let run = ref true

    // wait for the entry to be committed by everybody
    while !run do
      let committed =
        Raft.responseCommitted appended
        |> runRaft state.Raft state.Callbacks
        |> fun result ->
          match result with
            | Right (committed, _) -> committed
            | Left _ ->
              run := false
              ok  := false
              false

      if committed then
        run := false
      else
        printfn "%s not committed" (string appended.Id)
    !ok

  // ** joinCluster

  /// ## joinCluster
  ///
  /// Enter the Joint-Consensus by apppending a respective log entry.
  ///
  /// ### Signature:
  /// - changes: the changes to make to the current cluster configuration
  /// - state: current RaftAppContext to work against
  ///
  /// Returns: Either<RaftError * RaftValue, unit * Raft, EntryResponse * RaftValue>
  let private joinCluster (nodes: RaftNode array) (state: RaftAppContext) =
    let changes = Array.map NodeAdded nodes
    let result =
      raft {
        let! term = Raft.currentTermM ()
        let entry = JointConsensus(Id.Create(), 0u, term, changes, None) //
        do! Raft.debug "joinCluster" "appending entry to enter joint-consensus"
        return! Raft.receiveEntry entry
      }
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (appended, raftState) ->
      let newstate = RaftContext.updateRaft raftState state

      // block until entry has been committed
      let ok = waitForCommit appended state

      if ok then
        Welcome raftState.Node, newstate
      else
        ErrorResponse (Other "Could not commit JointConsensus"), newstate

    | Left (err, raftState) ->
      // save the new raft value back to the TVar
      let newstate = RaftContext.updateRaft raftState state

      ErrorResponse err, newstate

  // ** onConfigDone

  let private onConfigDone (state: RaftAppContext) =
    let result =
      raft {
        let! term = Raft.currentTermM ()
        let! nodes = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)
        let entry = Log.mkConfig term nodes
        let str = "appending entry to exit joint-consensus into regular configuration"
        do! Raft.debug "onConfigDone" str
        return! Raft.receiveEntry entry
      }
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (appended, raftState) ->
      // save the new raft value back to the TVar
      let newstate = RaftContext.updateRaft raftState state

      // block until entry has been committed
      let ok = waitForCommit appended state

      if ok then
        Some appended, newstate
      else
        None, newstate

    | Left (err, raftState) ->
      // save the new raft value back to the TVar
      let newstate = RaftContext.updateRaft raftState state
      None, newstate

  // ** leaveCluster

  /// ## leaveCluster
  ///
  /// Function to execute a two-phase commit for adding/removing members from the cluster.
  ///
  /// ### Signature:
  /// - changes: configuration changes to make to the cluster
  /// - success: RaftResponse to return when successful
  /// - appState: transactional variable to work against
  ///
  /// Returns: RaftResponse
  let private leaveCluster (nodes: RaftNode array) (state: RaftAppContext) =
    let changes = Array.map NodeRemoved nodes
    let result =
      raft {
        let! term = Raft.currentTermM ()
        let entry = JointConsensus(Id.Create(), 0u, term, changes, None)
        do! Raft.debug "leaveCluster" "appending entry to enter joint-consensus"
        return! Raft.receiveEntry entry
      }
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (appended, raftState) ->
      // save the new raft value back to the TVar
      let newstate = RaftContext.updateRaft raftState state

      // block until entry has been committed
      let ok = waitForCommit appended state

      if ok then
        // now that all nodes are in joint-consensus we need to wait and finalize the 2-phase commit
        Arrivederci, newstate
      else
        ErrorResponse (Other "Could not commit Joint-Consensus"), newstate

    | Left (err, raftState) ->
      let newstate = RaftContext.updateRaft raftState state
      ErrorResponse err, newstate

  // ** doRedirect

  /// ## Redirect to leader
  ///
  /// Gets the current leader node from the Raft state and returns a corresponding RaftResponse.
  ///
  /// ### Signature:
  /// - state: RaftAppContext
  ///
  /// Returns: Stm<RaftResponse>
  let private doRedirect state =
    match Raft.getLeader state.Raft with
    | Some node -> Redirect node, state
    | _         -> ErrorResponse (Other "No known leader"), state

  // ** handleAppendEntries

  /// ## Handle AppendEntries requests
  ///
  /// Handler for AppendEntries requests. Returns an appropriate response value.
  ///
  /// ### Signature:
  /// - sender:   Raft node which sent the request
  /// - ae:       AppendEntries request value
  /// - appState: RaftAppContext TVar
  ///
  /// Returns: RaftResponse
  let private handleAppendEntries sender ae (state: RaftAppContext) =
    let result =
      Raft.receiveAppendEntries (Some sender) ae
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (resp, raftState) ->
      let newstate = RaftContext.updateRaft raftState state
      AppendEntriesResponse(raftState.Node.Id, resp), newstate

    | Left (err, raftState) ->
      let newstate = RaftContext.updateRaft raftState state
      ErrorResponse err, newstate

  // ** handleAppendResponse

  /// ## Handle the AppendEntries request response.
  ///
  /// Handle the request entries response.
  ///
  /// ### Signature:
  /// - sender: Node who replied
  /// - ar: AppendResponse to process
  /// - appState: TVar<RaftAppContext>
  ///
  /// Returns: unit
  let private handleAppendResponse sender ar (state: RaftAppContext) =
    Raft.receiveAppendEntriesResponse sender ar
    |> evalRaft state.Raft state.Callbacks
    |> flip RaftContext.updateRaft state

  // ** handleVoteRequest

  /// ## Handle a vote request.
  ///
  /// Handle a vote request and return a response.
  ///
  /// ### Signature:
  /// - sender: Node which sent request
  /// - req: the `VoteRequest`
  /// - appState: current TVar<RaftAppContext>
  ///
  /// Returns: unit
  let private handleVoteRequest sender req (state: RaftAppContext) =
    let result =
      Raft.receiveVoteRequest sender req
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (resp, raftState) ->
      let newstate = RaftContext.updateRaft raftState state
      RequestVoteResponse(raftState.Node.Id, resp), newstate

    | Left (err, raftState) ->
      let newstate = RaftContext.updateRaft raftState state
      ErrorResponse err, newstate

  // ** handleVoteReponse

  /// ## Handle the response to a vote request.
  ///
  /// Handle the response to a vote request.
  ///
  /// ### Signature:
  /// - sender: Node which sent the response
  /// - resp: VoteResponse to process
  /// - appState: current TVar<RaftAppContext>
  ///
  /// Returns: unit
  let private handleVoteResponse sender rep (state: RaftAppContext) =
    Raft.receiveVoteResponse sender rep
    |> evalRaft state.Raft state.Callbacks
    |> flip RaftContext.updateRaft state

  // ** handleHandshake

  /// ## Handle a HandShake request by a certain Node.
  ///
  /// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
  /// leader if we are currently not Leader.
  ///
  /// ### Signature:
  /// - node: Node which wants to join the cluster
  /// - appState: current TVar<RaftAppContext>
  ///
  /// Returns: RaftResponse
  let private handleHandshake node (state: RaftAppContext) =
    if Raft.isLeader state.Raft then
      joinCluster [| node |] state
    else
      doRedirect state

  // ** handleHandwaive

  let private handleHandwaive node (state: RaftAppContext) =
    if Raft.isLeader state.Raft then
      leaveCluster [| node |] state
    else
      doRedirect state

  // ** handleInstallSnapshot

  let private handleInstallSnapshot node snapshot (state: RaftAppContext) =
    // let snapshot = createSnapshot () |> runRaft raft'
    let ar = { Term         = state.Raft.CurrentTerm
              ; Success      = false
              ; CurrentIndex = Raft.currentIndex state.Raft
              ; FirstIndex   = match Raft.firstIndex state.Raft.CurrentTerm state.Raft with
                                | Some idx -> idx
                                | _        -> 0u }
    InstallSnapshotResponse(state.Raft.Node.Id, ar), state

  // ** handleRequest

  let private handleRequest msg (state: RaftAppContext) =
    match msg with
    | RequestVote (sender, req) ->
      handleVoteRequest sender req state

    | AppendEntries (sender, ae) ->
      handleAppendEntries  sender ae state

    | HandShake node ->
      handleHandshake node state

    | HandWaive node ->
      handleHandwaive node state

    | InstallSnapshot (sender, snapshot) ->
      handleInstallSnapshot sender snapshot state

  // ** appendLog

  let private appendEntry (entry: RaftLogEntry)
                          (state: RaftAppContext) :
                          Either<IrisError * RaftAppContext, EntryResponse * RaftAppContext> =
    either {
      let result =
        entry
        |> Raft.receiveEntry
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (appended, raftState) ->
        let newstate = RaftContext.updateRaft raftState state
        let ok = waitForCommit appended state
        if ok then
          return appended, newstate
        else
          let err =
            "Unable to commit entry"
            |> Other
          return! Either.fail (err, newstate)
      | Left (err, raftState) ->
        let newstate = RaftContext.updateRaft raftState state
        return! Either.fail (err, newstate)
    }

  // ** appendEntry

  let private appendCommand (cmd: StateMachine)
                            (state: RaftAppContext) :
                             Either<IrisError * RaftAppContext, EntryResponse * RaftAppContext> =
    Raft.currentTerm state.Raft
    |> flip Log.make cmd
    |> flip appendEntry state

  // ** addNode

  let private addNode (id: Id) (ip: IpAddress) (port: uint32) (state: RaftAppContext) =
    if Raft.isLeader state.Raft then
      sprintf "attempting to add node with %s %s:%d" (string id) (string ip) port
      |> Logger.debug state.Raft.Node.Id tag

      let change =
        { Node.create id with
            IpAddr = ip
            Port   = uint16 port }
        |> NodeAdded

      JointConsensus(Id.Create(), 0u, state.Raft.CurrentTerm, [| change |], None)
      |> flip appendEntry state
    else
      Logger.err state.Raft.Node.Id tag "Unable to add node. Not leader."
      (NotLeader, state)
      |> Either.fail

  // ** removeNode

  let private removeNode (id: Id) (state: RaftAppContext) =
    if Raft.isLeader state.Raft then
      string id
      |> sprintf "attempting to remove node with %s"
      |> Logger.debug state.Raft.Node.Id tag

      let potentialChange =
        state.Raft
        |> Raft.getNode id
        |> Option.map NodeRemoved

      match potentialChange with
      | Some change ->
        JointConsensus(Id.Create(), 0u, state.Raft.CurrentTerm, [| change |], None)
        |> flip appendEntry state

      | None ->
        sprintf "Unable to remove node. Not found: %s" (string id)
        |> Logger.err state.Raft.Node.Id tag

        (MissingNode (string id), state)
        |> Either.fail
    else
      Logger.err state.Raft.Node.Id tag "Unable to remove node. Not leader."
      (NotLeader, state)
      |> Either.fail

  // ** startServer

  let private startServer (state: RaftAppContext) =
    let uri =
      state.Raft.Node
      |> nodeUri

    implement "startServer"

  // ** handler

  let private handler (state: RaftAppContext) (request: byte array) =
    let request = Binary.decode<IrisError,RaftRequest> request

    let response, newstate =
      match request with
      | Right message -> handleRequest message state
      | Left  error   -> ErrorResponse error, state

    response |> Binary.encode, newstate

  // ** mkConnections

  let private addConnections state =
    Map.fold
      (fun current _ (node: RaftNode) ->
        if node.Id <> state.Raft.Node.Id then
          mkReqSocket node
          |> RaftContext.addConnection state
        else
          state)
      state
      state.Raft.Peers

  // ** resetConnections

  let private destroyConnections (state: RaftAppContext) =
    Map.iter (konst dispose) state.Connections
    { state with Connections = Map.empty }

  // ** rand

  let private rand = new System.Random()

  // ** periodicR

  let private periodicR (state: RaftAppContext) =
    Raft.periodic (uint32 state.Options.RaftConfig.PeriodicInterval)
    |> evalRaft state.Raft state.Callbacks
    |> flip RaftContext.updateRaft state

  // ** startPeriodid

  /// ## startPeriodic
  ///
  /// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
  /// loop can be cancelled at a later time.
  ///
  /// ### Signature:
  /// - timeoput: interval at which the loop runs
  /// - appState: current RaftAppContext TVar
  ///
  /// Returns: CancellationTokenSource
  let private startPeriodic state =
    let token = new CancellationTokenSource()
    let rec proc () =
      async {
        let newstate = periodicR state

        Thread.Sleep(int state.Options.RaftConfig.PeriodicInterval) // sleep for 100ms
        return! proc ()                                  // recurse
      }
    Async.Start(proc(), token.Token)
    token                               // return the cancellation token source so this loop can be

  // ** tryJoin

  let private tryJoin (ip: IpAddress) (port: uint32) (state: RaftAppContext) : Either<IrisError, RaftNode> =
    let rec _tryJoin retry peer =
      either {
        if retry < int state.Options.RaftConfig.MaxRetries then
          use client = mkReqSocket peer

          sprintf "Retry: %d" retry
          |> Logger.debug state.Raft.Node.Id "tryJoin"

          let request = HandShake(state.Raft.Node)
          let! result = rawRequest request client

          sprintf "Result: %A" result
          |> Logger.debug state.Raft.Node.Id "tryJoin"

          match result with
          | Welcome node ->
            sprintf "Received Welcome from %A" node.Id
            |> Logger.debug state.Raft.Node.Id "tryJoin"
            return node

          | Redirect next ->
            sprintf "Got redirected to %A" (nodeUri next)
            |> Logger.info state.Raft.Node.Id "tryJoin"
            return! _tryJoin (retry + 1) next

          | ErrorResponse err ->
            sprintf "Unexpected error occurred. %A" err
            |> Logger.err state.Raft.Node.Id "tryJoin"
            return! Either.fail err

          | resp ->
            sprintf "Unexpected response. %A" resp
            |> Logger.err state.Raft.Node.Id "tryJoin"
            return!
              Other "Unexpected response"
              |> Either.fail
        else
          "Too many unsuccesful connection attempts."
          |> Logger.err state.Raft.Node.Id "tryJoin"
          return!
            Other "Too many unsuccesful connection attempts."
            |> Either.fail
      }

    // execute the join request with a newly created "fake" node
    _tryJoin 0 { Node.create (Id.Create()) with
                  IpAddr = ip
                  Port = uint16 port }

  // ** tryJoinCluster

  let tryJoinCluster (ip: IpAddress) (port: uint32) (state: RaftAppContext) =
    raft {
      Logger.debug state.Raft.Node.Id tag "requesting to join"

      let leader = tryJoin ip port state

      match leader with
      | Right leader ->
        sprintf "Reached leader: %A Adding to nodes." leader.Id
        |> Logger.info state.Raft.Node.Id tag

        do! Raft.addNodeM leader
        do! Raft.becomeFollower ()

      | Left err ->
        sprintf "Joining cluster failed. %A" err
        |> Logger.err state.Raft.Node.Id tag

    } |> evalRaft state.Raft state.Callbacks
    |> flip RaftContext.updateRaft state

  // ** tryLeave

  /// ## Attempt to leave a Raft cluster
  ///
  /// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
  /// AppendEntries request with a JointConsensus entry.
  ///
  /// ### Signature:
  /// - appState: RaftAppContext TVar
  ///
  /// Returns: unit
  let private tryLeave (state: RaftAppContext) : Either<IrisError,bool> =
    let rec _tryLeave retry node =
      either {
        if retry < int state.Options.RaftConfig.MaxRetries then
          use client = mkReqSocket node

          let request = HandWaive(state.Raft.Node)
          let! result = rawRequest request client

          match result with
          | Redirect other ->
            if retry <= int state.Options.RaftConfig.MaxRetries then
              return! _tryLeave (retry + 1) other
            else
              return!
                Other "Too many retries, aborting."
                |> Either.fail
          | Arrivederci       -> return true
          | ErrorResponse err -> return! Either.fail err
          | resp              -> return! Either.fail (Other "Unexpected response")
        else
          return! Either.fail (Other "Too many unsuccesful connection attempts.")
      }

    match state.Raft.CurrentLeader with
    | Some nid ->
      match Map.tryFind nid state.Raft.Peers with
      | Some node -> _tryLeave 0 node
      | _         ->
        Other "Node data for leader id not found"
        |> Either.fail
    | _ ->
      Other "No known Leader"
      |> Either.fail

  // ** leaveCluster

  let private tryLeaveCluster (state: RaftAppContext) =
    raft {
      Logger.debug state.Raft.Node.Id tag "requesting to leave"

      do! Raft.setTimeoutElapsedM 0u

      match tryLeave state with
      | Right true  ->
        "Successfully left cluster."
        |> Logger.info state.Raft.Node.Id tag // FIXME: this might need more consequences than this

      | Right false ->
        "Could not leave cluster."
        |> Logger.err state.Raft.Node.Id tag

      | Left err ->
        err
        |> sprintf "Could not leave cluster. %A"
        |> Logger.err state.Raft.Node.Id tag

      do! Raft.becomeFollower ()

      let! peers = Raft.getNodesM ()

      for kv in peers do
        do! Raft.removeNodeM kv.Value

    } |> evalRaft state.Raft state.Callbacks


  // ** forceElection

  let private forceElection state =
    raft {
      let! timeout = Raft.electionTimeoutM ()
      do! Raft.setTimeoutElapsedM timeout
      do! Raft.periodic timeout
    }
    |> evalRaft state.Raft state.Callbacks
    |> flip RaftContext.updateRaft state

  // ** prepareSnapshot

  let private prepareSnapshot state snapshot =
    Raft.createSnapshot (DataSnapshot snapshot) state.Raft


  // ** mkState

  /// ## Create an RaftAppState value
  ///
  /// Given the `RaftOptions`, create or load data and construct a new `RaftAppState` for the
  /// `RaftServer`.
  ///
  /// ### Signature:
  /// - context: `ZeroMQ` `Context`
  /// - options: `RaftOptions`
  ///
  /// Returns: RaftAppState
  let private mkState (options: IrisConfig) (callbacks: IRaftCallbacks) =
    either {
      let! raft = getRaft options
      return { Status      = ServiceStatus.Starting
               Raft        = raft
               Connections = Map.empty
               Callbacks   = callbacks
               Options     = options }
    }

  // ** initializeState

  let private initializeState state =
    let newstate =
      raft {
        let term = 0u
        do! Raft.setTermM term
        let! num = Raft.numNodesM ()

        if num = 1u then
          do! Raft.setTimeoutElapsedM 0u
          do! Raft.becomeLeader ()
        else
          // set the timeout to something random, to prevent split votes
          let timeout : Long =
            rand.Next(0, int state.Raft.ElectionTimeout)
            |> uint32
          do! Raft.setTimeoutElapsedM timeout
          do! Raft.becomeFollower ()
      } |> evalRaft state.Raft state.Callbacks

    "initialize: saving new state"
    |> Logger.debug state.Raft.Node.Id "initialize"

    // tryJoin leader
    RaftContext.updateRaft newstate state

  // ** loop

  let private loop (initial: RaftAppContext) (inbox: MailboxProcessor<Message>) =
    let rec act state =
      async {
        let! (cmd, channel) = inbox.Receive()

        let newstate =
          match cmd with
          | Join (ip, port) ->
            channel.Reply (Right Ok)
            tryJoinCluster ip port state

          | Leave           ->
            channel.Reply (Right Ok)
            tryLeaveCluster state

          | Append cmd ->
            match appendEntry cmd state with
            | Right (entry, newstate) ->
              channel.Reply (Right (Entry entry))
              newstate

            | Left (err, newstate) ->
              channel.Reply (Left err)
              newstate

          | Get ->
            channel.Reply (Right state)
            state

          | AddNode (id, ip, addr) ->
            match addNode id ip addr state with
            | Right (entry, newstate) ->
              channel.Reply (Right (Entry entry))
              newstate

            | Left (err, newstate) ->
              channel.Reply (Left err)
              newstate

          | RmNode id ->
            match removeNode id state with
            | Right (entry, newstate) ->
              channel.Reply (Right (Entry entry))
              newstate

            | Left (err, newstate) ->
              channel.Reply (Left err)
              newstate

        do! loop newstate
      }
    act initial

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  let start (options: IrisConfig, callbacks: IRaftCallbacks) =

    let initialState =
      match mkState options callbacks with
      | Right state ->
        state
        |> addConnections

      | Left error ->
        Error.exitWith error

    let nodeid =
      initialState
      |> RaftContext.getNodeId

    let socket : Zmq.Rep = new Zmq.Rep()

    let periodic = startPeriodic

    // returns a simpl
    { new IDisposable with
        member self.Dispose() =
          failwith "add all disposable stuff here" }

  // member self.IsLeader
  //   with get () =
  //     let state = readTVar appState |> atomically
  //     state.Raft.IsLeader

  // member self.Periodic() =
  //   let state = readTVar appState |> atomically
  //   periodicR state cbs
  //   |> writeTVar appState
  //   |> atomically

  // /// ## Start the Raft engine
  // ///
  // /// Start the Raft engine and start processing requests.
  // ///
  // /// ### Signature:
  // /// - unit: unit
  // ///
  // /// Returns: unit
  // member self.Start() =
  //   Logger.info nodeid tag "starting"

  //   lock server <| fun _ ->
  //     try
  //       Logger.debug nodeid tag "initialize server state"
  //       serverState <- ServiceStatus.Starting

  //       Logger.debug nodeid tag "initialize server loop"
  //       server := Some (startServer appState cbs)

  //       Logger.debug nodeid tag "initialize application"
  //       initialize appState cbs

  //       Logger.debug nodeid tag "initialize connections"
  //       mkConnections appState

  //       Logger.debug nodeid tag "initialize periodic loop"
  //       let tkn = startPeriodic appState cbs
  //       periodictoken := Some tkn

  //       Logger.debug nodeid tag "server running"
  //       serverState <- ServiceStatus.Running
  //     with
  //       | exn ->
  //         self.Cancel()

  //         sprintf "Exeception in Start: %A" exn.Message
  //         |> Logger.err nodeid tag

  //         serverState <- ServiceStatus.Failed (Other exn.Message)

  // /// ## Cancel
  // ///
  // /// Cancel the periodic loop, dispose of the server socket and reset all connections to self
  // /// server.
  // ///
  // /// ### Signature:
  // /// - unit: unit
  // ///
  // /// Returns: unit
  // member private self.Cancel() =
  //   try
  //     // cancel the running async tasks so we don't cause an election
  //     Logger.debug nodeid tag "cancel periodic loop"
  //     maybeCancelToken periodictoken
  //   with
  //     | exn ->
  //       exn.Message
  //       |> sprintf "RaftServer Error: could not cancel periodic loop: %s"
  //       |> Logger.err nodeid tag

  //   try
  //     // dispose of the server
  //     Logger.debug nodeid tag "disposing server"
  //     Option.bind (dispose >> Some) (!server) |> ignore
  //   with
  //     | exn ->
  //       exn.Message
  //       |> sprintf "Error: Could not dispose server: %s"
  //       |> Logger.err nodeid tag

  //   try
  //     Logger.debug nodeid tag "disposing sockets"
  //     self.State.Connections
  //     |> resetConnections
  //   with
  //     | exn ->
  //       exn.Message
  //       |> sprintf "Error: Could not dispose of connections: %s"
  //       |> Logger.err nodeid tag

  // /// ## Stop the Raft engine, sockets and all.
  // ///
  // /// Stop the Raft engine
  // ///
  // /// ### Signature:
  // /// - unit: unit
  // ///
  // /// Returns: unit
  // member self.Stop() =
  //   lock server <| fun _ ->
  //     if serverState = ServiceStatus.Running then
  //       Logger.debug nodeid tag "stopping"
  //       serverState <- ServiceStatus.Stopping

  //       // cancel the running async tasks so we don't cause an election
  //       Logger.debug nodeid tag "cancel periodic loop"
  //       maybeCancelToken periodictoken

  //       Logger.debug nodeid tag "dispose server"
  //       Option.bind (dispose >> Some) (!server) |> ignore

  //       Logger.debug nodeid tag "disposing sockets"
  //       self.State.Connections
  //       |> resetConnections

  //       Logger.debug nodeid tag  "saving state to disk"
  //       let state = readTVar appState |> atomically
  //       saveRaft options state.Raft
  //       |> Either.mapError
  //         (fun msg ->
  //           msg
  //           |> sprintf "An error occurred saving state to disk: %A"
  //           |> Logger.err nodeid tag)
  //       |> ignore

  //       Logger.debug nodeid tag "stopped"
  //       serverState <- ServiceStatus.Stopped

  // interface IRaftCallbacks with

  //   member self.SendRequestVote node req  =
  //     let state = self.State
  //     let request = RequestVote(state.Raft.Node.Id,req)
  //     let client = self.GetClient node
  //     let result = performRequest request client

  //     match result with
  //     | Right response ->
  //       match response with
  //       | RequestVoteResponse(sender, vote) -> Some vote
  //       | resp ->
  //         resp
  //         |> sprintf "SendRequestVote: Unexpected Response: %A"
  //         |> Logger.err nodeid tag
  //         None

  //     | Left error ->
  //       nodeUri node
  //       |> sprintf "SendRequestVote: encountered error \"%A\" during request to %s" error
  //       |> Logger.err nodeid tag
  //       None

  //   member self.SendAppendEntries (node: RaftNode) (request: AppendEntries) =
  //     let state = self.State
  //     let request = AppendEntries(state.Raft.Node.Id, request)
  //     let client = self.GetClient node
  //     let result = performRequest request client

  //     match result with
  //     | Right response ->
  //       match response with
  //       | AppendEntriesResponse(sender, ar) -> Some ar
  //       | resp ->
  //         resp
  //         |> sprintf "SendAppendEntries: Unexpected Response:  %A"
  //         |> Logger.err nodeid tag
  //         None
  //     | Left error ->
  //       nodeUri node
  //       |> sprintf "SendAppendEntries: Error \"%A\" received for request to %s" error
  //       |> Logger.err nodeid tag
  //       None

  //   member self.SendInstallSnapshot node is =
  //     let state = self.State
  //     let client = self.GetClient node
  //     let request = InstallSnapshot(state.Raft.Node.Id, is)
  //     let result = performRequest request client

  //     match result with
  //     | Right response ->
  //       match response with
  //       | InstallSnapshotResponse(sender, ar) -> Some ar
  //       | resp ->
  //         resp
  //         |> sprintf "SendInstallSnapshot: Unexpected Response: %A"
  //         |> Logger.err nodeid tag
  //         None
  //     | Left error ->
  //       nodeUri node
  //       |> sprintf "SendInstallSnapshot: Error \"%A\" received for request to %s" error
  //       |> Logger.err nodeid tag
  //       None

  //   //     _                _          ____               _
  //   //    / \   _ __  _ __ | |_   _   / ___|_ __ ___   __| |
  //   //   / _ \ | '_ \| '_ \| | | | | | |   | '_ ` _ \ / _` |
  //   //  / ___ \| |_) | |_) | | |_| | | |___| | | | | | (_| |
  //   // /_/   \_\ .__/| .__/|_|\__, |  \____|_| |_| |_|\__,_|
  //   //         |_|   |_|      |___/

  //   member self.ApplyLog sm =
  //     match onApplyLog with
  //     | Some cb -> cb sm
  //     | _       -> ()

  //     sprintf "Applying state machine command (%A)" sm
  //     |> Logger.info nodeid tag

  //   //  _   _           _
  //   // | \ | | ___   __| | ___  ___
  //   // |  \| |/ _ \ / _` |/ _ \/ __|
  //   // | |\  | (_) | (_| |  __/\__ \
  //   // |_| \_|\___/ \__,_|\___||___/

  //   member self.NodeAdded node   =
  //     try
  //       match onNodeAdded with
  //       | Some cb -> cb node
  //       | _       -> ()

  //       sprintf "Node was added. %s" (string node.Id)
  //       |> Logger.info nodeid tag

  //     with
  //       | exn -> handleException "NodeAdded" exn

  //   member self.NodeUpdated node =
  //     try
  //       sprintf "Node was updated. %s" (string node.Id)
  //       |> Logger.debug nodeid tag

  //       match onNodeUpdated with
  //       | Some cb -> cb node
  //       | _       -> ()
  //     with
  //       | exn -> handleException "NodeAdded" exn

  //   member self.NodeRemoved node =
  //     try
  //       sprintf "Node was removed. %s" (string node.Id)
  //       |> Logger.debug nodeid tag

  //       match onNodeRemoved with
  //       | Some cb -> cb node
  //       | _       -> ()
  //     with
  //       | exn -> handleException "NodeAdded" exn

  //   //   ____ _
  //   //  / ___| |__   __ _ _ __   __ _  ___  ___
  //   // | |   | '_ \ / _` | '_ \ / _` |/ _ \/ __|
  //   // | |___| | | | (_| | | | | (_| |  __/\__ \
  //   //  \____|_| |_|\__,_|_| |_|\__, |\___||___/
  //   //                          |___/

  //   member self.Configured nodes =
  //     match onConfigured with
  //     | Some cb -> cb nodes
  //     | _       -> ()

  //     Logger.debug nodeid tag "Cluster configuration done!"

  //   member self.PrepareSnapshot (raft: RaftValue) =
  //     match onCreateSnapshot with
  //     | Some cb ->
  //       let currIdx = Log.index raft.Log
  //       let prevTerm = Log.term raft.Log
  //       let term = raft.CurrentTerm
  //       let nodes = raft.Peers |> Map.toArray |> Array.map snd
  //       let data = cb ()
  //       Snapshot(Id.Create(), currIdx + 1u, term, currIdx, prevTerm, nodes, data)
  //       |> Log.fromEntries
  //       |> Some
  //     | _ ->
  //       Logger.err nodeid tag "Unable to create snapshot. No data handler specified."
  //       None

  //   member self.PersistSnapshot log =
  //     sprintf "PersistSnapshot insert id: %A" (LogEntry.getId log |> string)
  //     |> Logger.debug nodeid tag

  //   member self.RetrieveSnapshot () =
  //     failwith "implement RetrieveSnapshot again"

  //   /// ## Raft state changed
  //   ///
  //   /// Signals the Raft instance has changed its State.
  //   ///
  //   /// ### Signature:
  //   /// - old: old Raft state
  //   /// - new: new Raft state
  //   ///
  //   /// Returns: unit
  //   member self.StateChanged old current =
  //     match onStateChanged with
  //     | Some cb -> cb old current
  //     | _       -> ()

  //     sprintf "state changed from %A to %A" old current
  //     |> Logger.info nodeid tag

  //   /// ## Persist the vote for passed node to disk.
  //   ///
  //   /// Persist the vote for the passed node to disk.
  //   ///
  //   /// ### Signature:
  //   /// - node: Node to persist
  //   ///
  //   /// Returns: unit
  //   member self.PersistVote (node: RaftNode option) =
  //     try
  //       self.State
  //       |> RaftContext.getRaft
  //       |> saveRaft options
  //       |> Either.mapError
  //         (fun err ->
  //           printfn "Could not persit vote change. %A" err)
  //       |> ignore

  //       "PersistVote reset VotedFor" |> Logger.debug nodeid tag
  //     with
  //       | exn -> handleException "PersistTerm" exn

  //   /// ## Persit the new term in metadata file
  //   ///
  //   /// Save the current term in metatdata file.
  //   ///
  //   /// ### Signature:
  //   /// - arg: arg
  //   /// - arg: arg
  //   /// - arg: arg
  //   ///
  //   /// Returns: unit
  //   member self.PersistTerm term =
  //     try
  //       self.State
  //       |> RaftContext.getRaft
  //       |> saveRaft options
  //       |> Either.mapError
  //         (fun err ->
  //           printfn "Could not persit vote change. %A" err)
  //       |> ignore

  //       sprintf "PersistTerm term: %A" term |> Logger.debug nodeid tag
  //     with
  //       | exn -> handleException "PersistTerm" exn

  //   /// ## Persist a log to disk
  //   ///
  //   /// Save a log to disk.
  //   ///
  //   /// ### Signature:
  //   /// - log: Log to persist
  //   ///
  //   /// Returns: unit
  //   member self.PersistLog log =
  //     try
  //       sprintf "PersistLog insert id: %A" (LogEntry.getId log |> string)
  //       |> Logger.debug nodeid tag
  //     with
  //       | exn->
  //         handleException "PersistLog" exn

  //   /// ## Callback to delete a log entry from database
  //   ///
  //   /// Delete a log entry from the database.
  //   ///
  //   /// ### Signature:
  //   /// - log: LogEntry to delete
  //   ///
  //   /// Returns: unit
  //   member self.DeleteLog log =
  //     try
  //       sprintf "DeleteLog id: %A" (LogEntry.getId log |> string)
  //       |> Logger.debug nodeid tag
  //     with
  //       | exn -> handleException "DeleteLog" exn

  //   /// ## LogMsg
  //   ///
  //   /// Triggers a new event on LogObservable.
  //   ///
  //   /// ### Signature:
  //   /// - level: LogLevel
  //   /// - node:  RaftNode
  //   /// - str:   string
  //   ///
  //   /// Returns: unit
  //   member self.LogMsg node site level str =
  //     Logger.log level node.Id site str

  // override self.ToString() =
  //   sprintf "Connections:%s\nNodes:%s\nRaft:%s\nLog:%s"
  //     (self.State.Connections |> string |> String.indent 4)
  //     (Map.fold (fun m _ t -> sprintf "%s\n%s" m (string t)) "" self.State.Raft.Peers |> String.indent 4)
  //     (self.State.Raft.ToString() |> String.indent 4)
  //     (string self.State.Raft.Log |> String.indent 4)
