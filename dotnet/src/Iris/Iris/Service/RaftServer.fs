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

  [<RequireQualifiedAccess>]
  type private Msg =
    | Join           of IpAddress * uint32
    | Leave
    | Get
    | Periodic
    | AddCmd         of StateMachine
    | Request        of RaftRequest
    | Response       of RaftResponse
    | AddNode        of RaftNode
    | RmNode         of Id
    | IsCommitted    of EntryResponse

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | Entry          of EntryResponse
    | Response       of RaftResponse
    | State          of RaftAppContext
    | IsCommitted    of bool

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  type private Message = Msg * ReplyChan

  type private StateArbiter = MailboxProcessor<Message>

  type RaftServer private (arbiter: StateArbiter) =

    interface IDisposable with

      member self.Dispose() = ()

  // ** periodicR

  let private periodic (state: RaftAppContext) (channel: ReplyChan) =
    Reply.Ok
    |> Either.succeed
    |> channel.Reply

    Raft.periodic (uint32 state.Options.RaftConfig.PeriodicInterval)
    |> evalRaft state.Raft state.Callbacks
    |> RaftContext.updateRaft state

  // ** appendEntry

  let private appendEntry (state: RaftAppContext)
                          (entry: RaftLogEntry) :
                          Either<IrisError * RaftAppContext, EntryResponse * RaftAppContext> =
    let result =
      entry
      |> Raft.receiveEntry
      |> runRaft state.Raft state.Callbacks

    match result with

    | Right (appended, raftState) ->
      (appended, RaftContext.updateRaft state raftState)
      |> Either.succeed

    | Left (err, raftState) ->
      (err, RaftContext.updateRaft state raftState)
      |> Either.fail

  // ** appendCommand

  let private appendCommand (state: RaftAppContext) (cmd: StateMachine) =
    cmd
    |> Log.make state.Raft.CurrentTerm
    |> appendEntry state

  // ** onConfigDone

  let private onConfigDone (state: RaftAppContext) =
    "appending entry to exit joint-consensus into regular configuration"
    |> Logger.debug state.Raft.Node.Id tag

    state.Raft.Peers
    |> Map.toArray
    |> Array.map snd
    |> Log.mkConfig state.Raft.CurrentTerm
    |> appendEntry state

  // ** addNodes

  /// ## addNodes
  ///
  /// Enter the Joint-Consensus by apppending a respective log entry.
  ///
  /// ### Signature:
  /// - state: current RaftAppContext to work against
  /// - nodes: the changes to make to the current cluster configuration
  ///
  /// Returns: Either<RaftError * RaftValue, unit * Raft, EntryResponse * RaftValue>
  let private addNodes (state: RaftAppContext) (nodes: RaftNode array) =
    if Raft.isLeader state.Raft then
      nodes
      |> Array.map NodeAdded
      |> Log.mkConfigChange state.Raft.CurrentTerm
      |> appendEntry state
    else
      Logger.err state.Raft.Node.Id tag "Unable to add node. Not leader."
      (NotLeader, state)
      |> Either.fail

  // ** addNewNode

  let private addNewNode (state: RaftAppContext) (id: Id) (ip: IpAddress) (port: uint32) =
    sprintf "attempting to add node with
         %s %s:%d" (string id) (string ip) port
    |> Logger.debug state.Raft.Node.Id tag

    [| { Node.create id with

          IpAddr = ip
          Port   = uint16 port } |]
    |> addNodes state

  // ** removeNodes

  /// ## removeNodes
  ///
  /// Function to execute a two-phase commit for adding/removing members from the cluster.
  ///
  /// ### Signature:
  /// - changes: configuration changes to make to the cluster
  /// - success: RaftResponse to return when successful
  /// - appState: transactional variable to work against
  ///
  /// Returns: RaftResponse
  let private removeNodes (state: RaftAppContext) (nodes: RaftNode array) =
    "appending entry to enter joint-consensus"
    |> Logger.debug state.Raft.Node.Id tag

    nodes
    |> Array.map NodeRemoved
    |> Log.mkConfigChange state.Raft.CurrentTerm
    |> appendEntry state

  // ** removeNode

  let private removeNode (state: RaftAppContext) (id: Id) =
    if Raft.isLeader state.Raft then
      string id
      |> sprintf "attempting to remove node with
         %s"
      |> Logger.debug state.Raft.Node.Id tag

      let potentialChange =
        state.Raft
        |> Raft.getNode id

      match potentialChange with

      | Some node -> removeNodes state [| node |]
      | None ->
        sprintf "Unable to remove node. Not found: %s" (string id)
        |> Logger.err state.Raft.Node.Id tag

        (MissingNode (string id), state)
        |> Either.fail
    else
      "Unable to remove node. Not leader."
      |> Logger.err state.Raft.Node.Id tag

      (NotLeader, state)
      |> Either.fail

  // ** processAppendEntries

  let private processAppendEntries (state: RaftAppContext)
                                   (channel: ReplyChan)
                                   (sender: Id)
                                   (ae: AppendEntries) =
    let result =
      Raft.receiveAppendEntries (Some sender) ae
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (response, newstate) ->
      AppendEntriesResponse(state.Raft.Node.Id, response)
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

    | Left (err, newstate) ->
      ErrorResponse err
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

  // ** processVoteRequest

  let private processVoteRequest (state: RaftAppContext)
                                 (channel: ReplyChan)
                                 (sender: Id)
                                 (vr: VoteRequest) =
    let result =
      Raft.receiveVoteRequest sender vr
      |> runRaft state.Raft state.Callbacks

    match result with

    | Right (response, newstate) ->
      RequestVoteResponse(state.Raft.Node.Id, response)
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

    | Left (err, newstate) ->
      ErrorResponse err
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

  // ** processInstallSnapshot

  let private processInstallSnapshot (state: RaftAppContext)
                                     (channel: ReplyChan)
                                     (sender: Id)
                                     (snapshot: InstallSnapshot) =
    Logger.err state.Raft.Node.Id tag "INSTALLSNAPSHOT REQUEST NOT HANDLED YET"
    // let snapshot = createSnapshot () |> runRaft raft'
    let ar = { Term         = state.Raft.CurrentTerm
             ; Success      = false
             ; CurrentIndex = Raft.currentIndex state.Raft
             ; FirstIndex   = match Raft.firstIndex state.Raft.CurrentTerm state.Raft with
                              | Some idx -> idx
                              | _        -> 0u }
    InstallSnapshotResponse(state.Raft.Node.Id, ar)
    |> Reply.Response
    |> Either.succeed
    |> channel.Reply
    state

  // ** doRedirect

  /// ## Redirect to leader
  ///
  /// Gets the current leader node from the Raft state and returns a corresponding RaftResponse.
  ///
  /// ### Signature:
  /// - state: RaftAppContext
  ///
  /// Returns: Either<IrisError,RaftResponse>
  let private doRedirect (state: RaftAppContext) (channel: ReplyChan) =
    match Raft.getLeader state.Raft with
    | Some node ->
      Redirect node
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      state

    | None ->
      ErrorResponse (Other "No known leader")
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      state

  // ** processHandshake

  /// ## Process a HandShake request by a certain Node.
  ///
  /// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
  /// leader if we are currently not Leader.
  ///
  /// ### Signature:
  /// - node: Node which wants to join the cluster
  /// - appState: current TVar<RaftAppContext>
  ///
  /// Returns: RaftResponse
  let private processHandshake (state: RaftAppContext)
                               (channel: ReplyChan)
                               (node: RaftNode) =
    if Raft.isLeader state.Raft then
      match addNodes state [| node |] with
      | Right (entry, newstate) ->
        Reply.Entry entry
        |> Either.succeed
        |> channel.Reply
        newstate

      | Left (err, newstate) ->
        ErrorResponse err
        |> Reply.Response
        |> Either.succeed
        |> channel.Reply
        newstate
    else
      doRedirect state channel

  // ** processHandwaive

  let private processHandwaive (state: RaftAppContext)
                               (channel: ReplyChan)
                               (node: RaftNode) =
    if Raft.isLeader state.Raft then
      match removeNode state node.Id with
      | Right (entry, newstate) ->
        Reply.Entry entry
        |> Either.succeed
        |> channel.Reply
        newstate

      | Left (err, newstate) ->
        ErrorResponse err
        |> Reply.Response
        |> Either.succeed
        |> channel.Reply
        newstate
    else
      doRedirect state channel

  // ** processRequest

  let private processRequest (state: RaftAppContext)
                             (channel: ReplyChan)
                             (request: RaftRequest) =
    match request with
    | AppendEntries (id, ae)   -> processAppendEntries   state channel id ae
    | RequestVote (id, vr)     -> processVoteRequest     state channel id vr
    | InstallSnapshot (id, is) -> processInstallSnapshot state channel id is
    | HandShake node           -> processHandshake       state channel node
    | HandWaive node           -> processHandwaive       state channel node

  // ** processAppendResponse

  let private processAppendResponse (state: RaftAppContext)
                                    (channel: ReplyChan)
                                    (sender: Id)
                                    (ar: AppendResponse) =
    let result =
      Raft.receiveAppendEntriesResponse sender ar
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) ->
      Reply.Ok
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

    | Left (err, newstate) ->
      err
      |> Either.fail
      |> channel.Reply
      RaftContext.updateRaft state newstate

  // ** processVoteResponse

  let private processVoteResponse (state: RaftAppContext)
                                  (channel: ReplyChan)
                                  (sender: Id)
                                  (vr: VoteResponse) =
    let result =
      Raft.receiveVoteResponse sender vr
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) ->
      Reply.Ok
      |> Either.succeed
      |> channel.Reply
      RaftContext.updateRaft state newstate

    | Left (err, newstate) ->
      err
      |> Either.fail
      |> channel.Reply
      RaftContext.updateRaft state newstate

  // ** processSnapshotResponse

  let private processSnapshotResponse (state: RaftAppContext)
                                      (channel: ReplyChan)
                                      (sender: Id)
                                      (ar: AppendResponse) =
    "FIX RESPONSE PROCESSING FOR SNAPSHOT REQUESTS"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processRedirect

  let private processRedirect (state: RaftAppContext)
                              (channel: ReplyChan)
                              (leader: RaftNode) =
    "FIX REDIRECT RESPONSE PROCESSING"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processWelcome

  let private processWelcome (state: RaftAppContext)
                             (channel: ReplyChan)
                             (leader: RaftNode) =

    "FIX WELCOME RESPONSE PROCESSING"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processArrivederci

  let private processArrivederci (state: RaftAppContext)
                                 (channel: ReplyChan) =

    "FIX ARRIVEDERCI RESPONSE PROCESSING"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processErrorResponse

  let private processErrorResponse (state: RaftAppContext)
                                   (channel: ReplyChan)
                                   (error: IrisError) =

    error
    |> sprintf "received error response: %A"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processResponse

  let private processResponse (state: RaftAppContext)
                              (channel: ReplyChan)
                              (response: RaftResponse) =
    match response with
    | RequestVoteResponse     (sender, vote) -> processVoteResponse     state channel sender vote
    | AppendEntriesResponse   (sender, ar)   -> processAppendResponse   state channel sender ar
    | InstallSnapshotResponse (sender, ar)   -> processSnapshotResponse state channel sender ar
    | ErrorResponse            error         -> processErrorResponse    state channel error
    | _                                      -> state

  // ** loop

  let private loop (initial: RaftAppContext) (inbox: MailboxProcessor<Message>) =
    let rec act state =
      async {
        let! (cmd, channel) = inbox.Receive()

        let newstate =
          match cmd with
          | Msg.Join (ip, port) ->
            // channel.Reply (Right () Ok)
            // tryJoinCluster state ip port

            implement "Join"

          | Msg.Leave ->
            // channel.Reply (Right () Ok)
            // tryLeaveCluster state

            implement "Leave"

          | Msg.Periodic -> periodic state channel

          // Add a new StateMachine Command to the distributed log
          | Msg.AddCmd cmd ->
            match appendCommand state cmd with
            | Right (entry, newstate) ->
              Reply.Entry entry
              |> Either.succeed
              |> channel.Reply
              newstate

            | Left (err, newstate) ->
              err
              |> Either.fail
              |> channel.Reply
              newstate

          // Process an server request
          | Msg.Request request -> processRequest state channel request

          // Process a server response
          | Msg.Response response -> processResponse state channel response

          // Get the current state
          | Msg.Get ->
            Reply.State state
            |> Either.succeed
            |> channel.Reply
            state

          // Add a new node to the cluster
          | Msg.AddNode node ->
            match addNodes state [| node |] with
            | Right (entry, newstate) ->
              Reply.Entry entry
              |> Either.succeed
              |> channel.Reply
              newstate

            | Left (err, newstate) ->
              err
              |> Either.fail
              |> channel.Reply
              newstate

          // Remove a known node from the cluster
          | Msg.RmNode id ->
            match removeNode state id with
            | Right (entry, newstate) ->
              Reply.Entry entry
              |> Either.succeed
              |> channel.Reply
              newstate

            | Left (err, newstate) ->
              err
              |> Either.fail
              |> channel.Reply
              newstate

          | Msg.IsCommitted entry ->
            let result =
              Raft.responseCommitted entry
              |> runRaft state.Raft state.Callbacks

            match result with
            | Right (committed, _) ->
              committed
              |> Reply.IsCommitted
              |> Either.succeed
              |> channel.Reply
              state

            | Left  (err, _) ->
              err
              |> Either.fail
              |> channel.Reply
              state

        do! act newstate
      }
    act initial

  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  //.|____/.\___|_|....\_/.\___|_|............................................................

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
  let private waitForCommit (arbiter: StateArbiter) (appended: EntryResponse) =
    let timeout = 50 // ms!
    let delta = 2 // ms!

    let ok = ref (Right true)
    let run = ref true
    let iterations = ref 0

    // wait for the entry to be committed by everybody
    while !run && !iterations < timeout do
      let response = arbiter.PostAndReply(fun chan -> Msg.IsCommitted appended, chan)

      match response with
      | Right (Reply.IsCommitted result) ->
        ok := Right result

      | Right _ ->
        let error = Other "Msg.IsCommitted always expects Reply.IsCommitted. Do your homework."
        ok := Left error
        run := false

      | Left error ->
        ok  := Left error
        run := false

      match !ok with
      | Right true | Left _ -> run := false
      | _ ->
        printfn "%s not yet committed" (string appended.Id)
        iterations := !iterations + delta
        Thread.Sleep delta

    !ok

  // ** handleRequest

  let private handleRequest (arbiter: StateArbiter) (msg: RaftRequest) : RaftResponse =
    let result =
      either {
        let! reply = arbiter.PostAndReply(fun chan -> Msg.Request msg,chan)

        match reply with
        | Reply.Response response ->       // the base case it, the response is ready
          return response

        | Reply.Entry entry ->
          let! committed = waitForCommit arbiter entry

          match msg, committed with
          | HandShake _, true ->
            let! reply = arbiter.PostAndReply(fun chan -> Msg.Get,chan)
            match reply with
            | Reply.State state ->
              return Welcome state.Raft.Node
            | other ->
              return
                sprintf "Unexpected reply from StateArbiter: %A" other
                |> Other
                |> ErrorResponse

          | HandWaive _, true ->
            return Arrivederci

          | HandWaive _, false | HandShake _, false ->
            return ErrorResponse ResponseTimeout

          | other ->
            return
              sprintf "Unexpected reply StateArbiter: %A" other
              |> Other
              |> ErrorResponse

        | other ->
          return
            sprintf "Unexpected reply StateArbiter: %A" other
            |> Other
            |> ErrorResponse
      }

    match result with
    | Right resp -> resp
    | Left error -> ErrorResponse error


  // ** handler

  let private handler (arbiter: StateArbiter) (request: byte array) =
    let request = Binary.decode<IrisError,RaftRequest> request

    let response =
      match request with
      | Right message -> handleRequest arbiter message
      | Left error    -> ErrorResponse error

    response |> Binary.encode

  // ** startServer

  let private startServer (state: RaftAppContext) =
    let uri =
      state.Raft.Node
      |> nodeUri

    implement "startServer"

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
    { state with
         Connections = Map.empty }

  // ** rand

  let private rand = new System.Random()

  // ** startPeriodic

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
  let private startPeriodic (arbiter: StateArbiter) =
    let token = new CancellationTokenSource()
    let rec proc () =
      async {
        let! result = arbiter.PostAndAsyncReply(fun chan -> Msg.Get,chan)
        match result with
        | Right (Reply.State state) ->
          let! _ = arbiter.PostAndAsyncReply(fun chan -> Msg.Periodic, chan)
          Thread.Sleep(int state.Options.RaftConfig.PeriodicInterval) // sleep for 100ms
        | Right reply ->
          printfn "WARNING: received garbage reply in periodic function %A" reply
        | Left error ->
          printfn "ERROR: %A" error
        return! proc ()                                             // recurse
      }
    Async.Start(proc(), token.Token)
    token                               // return the cancellation token source so this loop can be

  // ** tryJoin

  let private tryJoin (state: RaftAppContext) (ip: IpAddress) (port: uint32) =
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

    // execute the join request with
         a newly created "fake" node
    _tryJoin 0 { Node.create (Id.Create()) with

                  IpAddr = ip
                  Port = uint16 port }

  // ** tryJoinCluster

  let private tryJoinCluster (state: RaftAppContext) (ip: IpAddress) (port: uint32) =
    raft {
      Logger.debug state.Raft.Node.Id tag "requesting to join"

      let leader = tryJoin state ip port

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
    |> RaftContext.updateRaft state

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

    }
    |> evalRaft state.Raft state.Callbacks
    |> RaftContext.updateRaft state


  // ** forceElection

  let private forceElection state =
    raft {
      let! timeout = Raft.electionTimeoutM ()
      do! Raft.setTimeoutElapsedM timeout
      do! Raft.periodic timeout
    }
    |> evalRaft state.Raft state.Callbacks
    |> RaftContext.updateRaft state

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
    RaftContext.updateRaft state newstate

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

  //     | Right  response ->
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
