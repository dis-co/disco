namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Collections.Concurrent
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Zmq
open Iris.Raft
open FSharpx.Functional
open Utilities
open Persistence

// * Raft

module Raft =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  [<Literal>]
  let private tag = "RaftServer"

  // ** RaftEvent

  type RaftEvent =
    | ApplyLog       of StateMachine
    | NodeAdded      of RaftNode
    | NodeRemoved    of RaftNode
    | NodeUpdated    of RaftNode
    | Configured     of RaftNode array
    | StateChanged   of RaftState * RaftState
    | CreateSnapshot of string

  // ** Msg

  [<RequireQualifiedAccess>]
  type private Msg =
    | Initialize
    | Join           of IpAddress * uint16
    | Leave
    | Get
    | Status
    | Periodic
    | ForceElection
    | AddCmd         of StateMachine
    | Request        of RaftRequest
    | Response       of RaftResponse
    | AddNode        of RaftNode
    | RmNode         of Id
    | IsCommitted    of EntryResponse

  // ** Reply

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | Entry          of EntryResponse
    | Response       of RaftResponse
    | State          of RaftAppContext
    | Status         of ServiceStatus
    | IsCommitted    of bool

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,Req>

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<RaftEvent>>

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Message

  type private Message = Msg * ReplyChan

  // ** StateArbiter

  type private StateArbiter = MailboxProcessor<Message>

  // ** RaftServer

  type IRaftServer =
    inherit IDisposable

    abstract Node          : RaftNode
    abstract NodeId        : Id
    abstract Append        : StateMachine -> Either<IrisError, EntryResponse>
    abstract ForceElection : unit -> Either<IrisError, unit>
    abstract State         : Either<IrisError,RaftAppContext>
    abstract Status        : Either<IrisError,ServiceStatus>
    abstract Subscribe     : (RaftEvent -> unit) -> IDisposable
    abstract Start         : unit -> Either<IrisError,unit>
    abstract Periodic      : unit -> Either<IrisError,unit>
    abstract JoinCluster   : IpAddress -> uint16 -> Either<IrisError,unit>
    abstract LeaveCluster  : unit -> Either<IrisError,unit>
    abstract AddNode       : RaftNode -> Either<IrisError,EntryResponse>
    abstract RmNode        : Id -> Either<IrisError,EntryResponse>

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
      |> Array.map ConfigChange.NodeAdded
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
    |> Array.map ConfigChange.NodeRemoved
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

  // ** tryJoin

  let private tryJoin (state: RaftAppContext) (ip: IpAddress) (port: uint16) =
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
                  Port   = port }

  // ** tryJoinCluster

  let private tryJoinCluster (state: RaftAppContext) (ip: IpAddress) (port: uint16) =
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

    }
    |> runRaft state.Raft state.Callbacks

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
    |> runRaft state.Raft state.Callbacks

  // ** forceElection

  let private forceElection (state: RaftAppContext) =
    raft {
      let! timeout = Raft.electionTimeoutM ()
      do! Raft.setTimeoutElapsedM timeout
      do! Raft.periodic timeout
    }
    |> runRaft state.Raft state.Callbacks

  // ** rand

  let private rand = new System.Random()

  // ** initializeState

  let private initializeState (state: RaftAppContext) =
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
    }
    |> runRaft state.Raft state.Callbacks

  // ** loop

  let private loop (initial: RaftAppContext) (inbox: MailboxProcessor<Message>) =
    let rec act state =
      async {
        let! (cmd, channel) = inbox.Receive()

        let newstate =
          match cmd with
          | Msg.Initialize ->
            match initializeState state with
            | Right (_, newstate) ->
              Reply.Ok
              |> Either.succeed
              |> channel.Reply
              { RaftContext.updateRaft state newstate with
                  Status = ServiceStatus.Running }

            | Left (error, newstate) ->
              error
              |> Either.fail
              |> channel.Reply
              { RaftContext.updateRaft state newstate with
                  Status = ServiceStatus.Failed error }

          | Msg.Status ->
            Reply.Status state.Status
            |> Either.succeed
            |> channel.Reply
            state

          | Msg.Join (ip, port) ->
            match tryJoinCluster state ip port with
            | Right (_, newstate) ->
              Reply.Ok
              |> Either.succeed
              |> channel.Reply
              RaftContext.updateRaft state newstate

            | Left (error, newstate) ->
              error
              |> Either.fail
              |> channel.Reply
              RaftContext.updateRaft state newstate

          | Msg.Leave ->
            match tryLeaveCluster state with
            | Right (_, newstate) ->
              Reply.Ok
              |> Either.succeed
              |> channel.Reply
              RaftContext.updateRaft state newstate

            | Left (error, newstate) ->
              error
              |> Either.fail
              |> channel.Reply
              RaftContext.updateRaft state newstate

          | Msg.Periodic -> periodic state channel

          | Msg.ForceElection ->
            match forceElection state with
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


  // ** requestHandler

  let private requestHandler (arbiter: StateArbiter) (request: byte array) =
    let request = Binary.decode<IrisError,RaftRequest> request

    let response =
      match request with
      | Right message -> handleRequest arbiter message
      | Left error    -> ErrorResponse error

    response |> Binary.encode

  // ** getConnection

  let private getConnection (self: Id) (connections: Connections) (peer: RaftNode) : Req =
    match connections.TryGetValue peer.Id with
    | true, connection -> connection
    | false, _ ->
      let addr = nodeUri peer
      let connection = mkReqSocket peer
      while not (connections.TryAdd(peer.Id, connection)) do
        Logger.err self tag "Unable to add connection. Retrying."
        Thread.Sleep 1
      connection

  // ** sendRequestVote

  let private sendRequestVote (self: Id)
                              (connections: Connections)
                              (peer: RaftNode)
                              (request: VoteRequest) :
                              VoteResponse option =

    let request = RequestVote(self, request)
    let client = getConnection self connections peer
    let result = performRequest client request

    match result with
    | Right (RequestVoteResponse(sender, vote)) -> Some vote
    | Right other ->
      other
      |> sprintf "SendRequestVote: Unexpected Response: %A"
      |> Logger.err self tag
      None

    | Left error ->
      nodeUri peer
      |> sprintf "SendRequestVote: encountered error %A in request to %s" error
      |> Logger.err self tag
      None

  // ** sendAppendEntries

  let private sendAppendEntries (self: Id)
                                (connections: Connections)
                                (peer: RaftNode)
                                (request: AppendEntries) =

    let request = AppendEntries(self, request)
    let client = getConnection self connections peer
    let result = performRequest client request

    match result with
    | Right (AppendEntriesResponse(sender, ar)) -> Some ar
    | Right response ->
      response
      |> sprintf "SendAppendEntries: Unexpected Response:  %A"
      |> Logger.err self tag
      None
    | Left error ->
      nodeUri peer
      |> sprintf "SendAppendEntries: received error %A in request to %s" error
      |> Logger.err self tag
      None

  // ** sendInstallSnapshot

  let private sendInstallSnapshot (self: Id)
                                  (connections: Connections)
                                  (peer: RaftNode)
                                  (is: InstallSnapshot) =
    let client = getConnection self connections peer
    let request = InstallSnapshot(self, is)
    let result = performRequest client request

    match result with
    | Right (InstallSnapshotResponse(sender, ar)) -> Some ar
    | Right response ->
      response
      |> sprintf "SendInstallSnapshot: Unexpected Response: %A"
      |> Logger.err self tag
      None
    | Left error ->
      nodeUri peer
      |> sprintf "SendInstallSnapshot: received error %A in request to %s" error
      |> Logger.err self tag
      None

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
  let private startPeriodic (arbiter: StateArbiter) (cts: CancellationTokenSource) =
    let rec proc () =
      async {
        let! result = arbiter.PostAndAsyncReply(fun chan -> Msg.Get,chan)
        match result with
        | Right (Reply.State state) ->
          let! _ = arbiter.PostAndAsyncReply(fun chan -> Msg.Periodic, chan)
          do! Async.Sleep(int state.Options.RaftConfig.PeriodicInterval) // sleep for 100ms

        | Right reply ->
          printfn "WARNING: received garbage reply in periodic function %A" reply

        | Left error ->
          printfn "ERROR: %A" error
        return! proc ()
      }
    try
      Async.Start(proc(), cts.Token)
      |> Either.succeed
    with
      | exn ->
        exn.Message
        |> Other
        |> Either.fail

  // ** prepareSnapshot

  let private prepareSnapshot state snapshot =
    Raft.createSnapshot (DataSnapshot snapshot) state.Raft
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

  let private trigger (subscriptions: Subscriptions) (ev: RaftEvent) =
    for subscription in subscriptions do
      subscription.OnNext ev

  // ** mkCallbacks

  let private mkCallbacks (id: Id)
                          (connections: Connections)
                          (subscriptions: Subscriptions) =

    { new IRaftCallbacks with
        member self.SendRequestVote peer request =
          sendRequestVote id connections peer request

        member self.SendAppendEntries peer request =
          sendAppendEntries id connections peer request

        member self.SendInstallSnapshot peer request =
          sendInstallSnapshot id connections peer request

        member self.PrepareSnapshot raft =
          implement "PrepareSnapshot"

        member self.RetrieveSnapshot () =
          implement "RetrieveSnapshot"

        member self.PersistSnapshot log =
          implement "PersistSnapshot"

        member self.ApplyLog cmd =
          RaftEvent.ApplyLog cmd
          |> trigger subscriptions

        member self.NodeAdded node =
          RaftEvent.NodeAdded node
          |> trigger subscriptions

        member self.NodeUpdated node =
          RaftEvent.NodeUpdated node
          |> trigger subscriptions

        member self.NodeRemoved node =
          RaftEvent.NodeRemoved node
          |> trigger subscriptions

        member self.Configured nodes =
          RaftEvent.Configured nodes
          |> trigger subscriptions

        member self.StateChanged oldstate newstate =
          RaftEvent.StateChanged(oldstate, newstate)
          |> trigger subscriptions

        member self.PersistVote node =
          implement "PersistVote"
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

        member self.PersistTerm term =
          implement "PersistTerm"
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

        member self.PersistLog log =
          implement "PersistLog"

        member self.DeleteLog log =
          implement "DeleteLog"

        member self.LogMsg node callsite level msg =
          Logger.log level node.Id callsite msg

        }

  // ** initConnections

  let private initConnections (state: RaftValue) (connections: Connections) =
    for KeyValue(_,node) in state.Peers do
      if node.Id <> state.Node.Id then
        getConnection state.Node.Id connections node
        |> ignore

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
  let private mkState (options: IrisConfig)
                      (connections: Connections)
                      (subscriptions: Subscriptions) =
    either {
      let! raft = getRaft options

      initConnections raft connections

      return { Status    = ServiceStatus.Starting
               Raft      = raft
               Callbacks = mkCallbacks raft.Node.Id connections subscriptions
               Options   = options }
    }

  // ** withOk

  let private withOk (msg: Msg) (agent: StateArbiter) : Either<IrisError,unit> =
    match agent.PostAndReply(fun chan -> msg, chan) with
    | Right Reply.Ok -> Right ()

    | Right other ->
      sprintf "Received garbage reply from agent: %A" other
      |> Other
      |> Either.fail

    | Left error ->
      Either.fail error

  // ** addCmd

  let private addCmd (agent: StateArbiter)
                     (cmd: StateMachine) :
                     Either<IrisError, EntryResponse> =
    match agent.PostAndReply(fun chan -> Msg.AddCmd cmd,chan) with
    | Right (Reply.Entry entry) ->
      match waitForCommit agent entry with
      | Right true -> Either.succeed entry

      | Right false ->
        ResponseTimeout
        |> Either.fail

      | Left error ->
        error
        |> Either.fail

    | Right other ->
      sprintf "Received garbage reply from agent: %A" other
      |> Other
      |> Either.fail

    | Left error ->
      Either.fail error


  [<RequireQualifiedAccess>]
  module RaftServer =

    //  ____        _     _ _
    // |  _ \ _   _| |__ | (_) ___
    // | |_) | | | | '_ \| | |/ __|
    // |  __/| |_| | |_) | | | (__
    // |_|    \__,_|_.__/|_|_|\___|

    let create (options: IrisConfig) =
      either {
        let connections = new Connections()
        let subscriptions = new Subscriptions()

        let listener =
          { new IObservable<RaftEvent> with
              member self.Subscribe(obs) =
                lock subscriptions <| fun _ ->
                  subscriptions.Add obs

                { new IDisposable with
                    member self.Dispose () =
                      lock subscriptions <| fun _ ->
                        subscriptions.Remove obs
                        |> ignore } }

        let! state = mkState options connections subscriptions

        let addr =
          state
          |> RaftContext.getNode
          |> nodeUri

        let agent = new StateArbiter(loop state)
        let server = new Zmq.Rep(addr, requestHandler agent)
        let periodic = new CancellationTokenSource()

        return
          { new IRaftServer with
              member self.Node
                with get () = state.Raft.Node

              member self.NodeId
                with get () = state.Raft.Node.Id

              member self.Append cmd =
                addCmd agent cmd

              member self.Status
                with get () =
                  match agent.PostAndReply(fun chan -> Msg.Status,chan) with
                  | Right (Reply.Status status) -> Right status

                  | Right other ->
                    sprintf "Received garbage reply from agent: %A" other
                    |> Other
                    |> Either.fail

                  | Left error ->
                    Either.fail error

              member self.ForceElection () =
                withOk Msg.ForceElection agent

              member self.Periodic () =
                withOk Msg.Periodic agent

              member self.JoinCluster ip port =
                withOk (Msg.Join(ip, port)) agent

              member self.LeaveCluster () =
                withOk Msg.Leave agent

              member self.AddNode node =
                match agent.PostAndReply(fun chan -> Msg.AddNode node,chan) with
                | Right (Reply.Entry entry) -> Right entry
                | Right other ->
                  sprintf "Unexpected reply by agent: %A" other
                  |> Other
                  |> Either.fail
                | Left error ->
                  Either.fail error

              member self.RmNode id =
                match agent.PostAndReply(fun chan -> Msg.RmNode id,chan) with
                | Right (Reply.Entry entry) -> Right entry
                | Right other ->
                  sprintf "Unexpected reply by agent: %A" other
                  |> Other
                  |> Either.fail
                | Left error ->
                  Either.fail error

              member self.State
                with get () =
                  match agent.PostAndReply(fun chan -> Msg.Get,chan) with
                  | Right (Reply.State state) -> Right state

                  | Right other ->
                    sprintf "Received garbage reply from agent: %A" other
                    |> Other
                    |> Either.fail

                  | Left error ->
                    Either.fail error

              member self.Subscribe (callback: RaftEvent -> unit) =
                { new IObserver<RaftEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Start () =
                either {
                  agent.Start()
                  do! startPeriodic agent periodic
                  do! withOk Msg.Initialize agent
                  do! server.Start()
                }

              member self.Dispose () =
                dispose periodic
                for KeyValue(_, connection) in connections do
                  dispose connection
                subscriptions.Clear()
                connections.Clear()
                dispose agent
            }
      }

    let isLeader (server: IRaftServer) : bool =
      match server.State with
      | Right state -> Raft.isLeader state.Raft
      | _ -> false
