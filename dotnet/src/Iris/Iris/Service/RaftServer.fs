namespace Iris.Service

// * Imports

open System
open System.Threading
open System.Collections
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

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,Req>

  let private disposeAll (connections: Connections) =
    for KeyValue(_,connection) in connections do
      dispose connection

  // ** RaftAppContext

  [<NoComparison;NoEquality>]
  type RaftAppContext =
    { Status:      ServiceStatus
      Raft:        RaftValue
      Options:     IrisConfig
      Callbacks:   IRaftCallbacks
      Server:      Zmq.Rep
      Periodic:    IDisposable
      Connections: Connections }

    interface IDisposable with
      member self.Dispose() =
        dispose self.Periodic
        disposeAll self.Connections
        self.Connections.Clear()
        dispose self.Server

  // ** Reply

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | Entry          of EntryResponse
    | Response       of RaftResponse
    | State          of RaftAppContext
    | Status         of ServiceStatus
    | IsCommitted    of bool

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Load           of config:IrisConfig   * chan:ReplyChan
    | Unload         of chan:ReplyChan
    | Join           of ip:IpAddress        * port:uint16 * chan:ReplyChan
    | Leave          of chan:ReplyChan
    | Get            of chan:ReplyChan
    | Status         of chan:ReplyChan
    | Periodic
    | ForceElection
    | AddCmd         of sm:StateMachine     * chan:ReplyChan
    | Request        of req:RaftRequest     * chan:ReplyChan
    | Response       of resp:RaftResponse   * chan:ReplyChan
    | AddNode        of node:RaftNode       * chan:ReplyChan
    | RmNode         of id:Id               * chan:ReplyChan
    | IsCommitted    of entry:EntryResponse * chan:ReplyChan

    override self.ToString() =
      match self with
      | Load       (config,_)    -> sprintf "Load: %A" config
      | Unload             _     -> sprintf "Unload"
      | Join       (ip,port,_)   -> sprintf "Join: %s %d" (string ip) port
      | Leave               _    -> "Leave"
      | Get                 _    -> "Get"
      | Status              _    -> "Status"
      | Periodic                 -> "Periodic"
      | ForceElection            -> "ForceElection"
      | AddCmd         (sm,_)    -> sprintf "AddCmd: %A" sm
      | Request        (req,_)   -> sprintf "Request: %A" req
      | Response       (resp,_)  -> sprintf "Response: %A" resp
      | AddNode        (node,_)  -> sprintf "AddNode: %A" node
      | RmNode         (id,_)    -> sprintf "RmNode: %A" id
      | IsCommitted    (entry,_) -> sprintf "IsCommitted: %A" entry

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<RaftEvent>>

  // ** StateArbiter

  type private StateArbiter = MailboxProcessor<Msg>

  // ** RaftServerState

  [<NoComparison;NoEquality>]
  type private RaftServerState =
    | Idle
    | Loaded of RaftAppContext

  // ** RaftServer

  type IRaftServer =
    inherit IDisposable

    abstract Node          : Either<IrisError,RaftNode>
    abstract NodeId        : Either<IrisError,Id>
    abstract Load          : IrisConfig -> Either<IrisError, unit>
    abstract Unload        : unit -> Either<IrisError, unit>
    abstract Append        : StateMachine -> Either<IrisError, EntryResponse>
    abstract ForceElection : unit -> Either<IrisError, unit>
    abstract State         : Either<IrisError, RaftAppContext>
    abstract Status        : Either<IrisError, ServiceStatus>
    abstract Subscribe     : (RaftEvent -> unit) -> IDisposable
    abstract Start         : unit -> Either<IrisError, unit>
    abstract Periodic      : unit -> Either<IrisError, unit>
    abstract JoinCluster   : IpAddress -> uint16 -> Either<IrisError, unit>
    abstract LeaveCluster  : unit -> Either<IrisError, unit>
    abstract AddNode       : RaftNode -> Either<IrisError, EntryResponse>
    abstract RmNode        : Id -> Either<IrisError, EntryResponse>
    abstract Connections   : Either<IrisError, Connections>

  //  _   _      _
  // | | | | ___| |_ __   ___ _ __ ___
  // | |_| |/ _ \ | '_ \ / _ \ '__/ __|
  // |  _  |  __/ | |_) |  __/ |  \__ \
  // |_| |_|\___|_| .__/ \___|_|  |___/
  //              |_|

  // ** getRaft

  /// ## pull Raft state value out of RaftAppContext value
  ///
  /// Get Raft state value from RaftAppContext.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: Raft
  let private getRaft (context: RaftAppContext) =
    context.Raft

  // ** getNode

  /// ## getNode
  ///
  /// Return the current node.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: RaftNode
  let private getNode (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf

  // ** getNodeId

  /// ## getNodeId
  ///
  /// Return the current node Id.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: Id
  let private getNodeId (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf
    |> Node.getId

  // ** updateRaft

  /// ## Update Raft in RaftAppContext
  ///
  /// Update the Raft field of a given RaftAppContext
  ///
  /// ### Signature:
  /// - raft: new Raft value to add to RaftAppContext
  /// - state: RaftAppContext to update
  ///
  /// Returns: RaftAppContext
  let private updateRaft (context: RaftAppContext) (raft: RaftValue) : RaftAppContext =
    { context with Raft = raft }

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
  /// - state: RaftServerState transactional state variable
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
      let response = arbiter.PostAndReply(fun chan -> Msg.IsCommitted(appended, chan))

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
        printfn "%A not yet committed" (string appended.Id)
        iterations := !iterations + delta
        Thread.Sleep delta

    !ok

  // ** requestHandler

  let private requestHandler (arbiter: StateArbiter) (data: byte array) =
    let handle request =
      either {
        let! message = Binary.decode<IrisError,RaftRequest> request
        let! reply = arbiter.PostAndReply(fun chan -> Msg.Request(message,chan))

        match reply with
        | Reply.Response response ->       // the base case it, the response is ready
          return response

        | Reply.Entry entry ->
          let! committed = waitForCommit arbiter entry

          match message, committed with
          | HandShake _, true ->
            let! reply = arbiter.PostAndReply(fun chan -> Msg.Get chan)
            match reply with
            | Reply.State state ->
              return Welcome state.Raft.Node
            | other ->
              return!
                sprintf "Unexpected reply from StateArbiter: %A" other
                |> RaftError
                |> Either.fail

          | HandWaive _, true ->
            return Arrivederci

          | HandWaive _, false | HandShake _, false ->
            return!
              ResponseTimeout
              |> Either.fail
          | other ->
            return!
              sprintf "Unexpected reply StateArbiter: %A" other
              |> RaftError
              |> Either.fail

        | other ->
          return!
            sprintf "Unexpected reply StateArbiter: %A" other
            |> RaftError
            |> Either.fail
      }

    match handle data with
    | Right response -> response
    | Left error     -> ErrorResponse error
    |> Binary.encode

  //   ____      _ _ _                _
  //  / ___|__ _| | | |__   __ _  ___| | _____
  // | |   / _` | | | '_ \ / _` |/ __| |/ / __|
  // | |__| (_| | | | |_) | (_| | (__|   <\__ \
  //  \____\__,_|_|_|_.__/ \__,_|\___|_|\_\___/

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
      |> sprintf "SendRequestVote: encountered error %A in request to %A" error
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
      |> sprintf "SendAppendEntries: received error %A in request to %A" error
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
      |> sprintf "SendInstallSnapshot: received error %A in request to %A" error
      |> Logger.err self tag
      None

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
          printfn "PrepareSnapshot"
          None

        member self.RetrieveSnapshot () =
          printfn "PrepareSnapshot"
          None

        member self.PersistSnapshot log =
          printfn "PersistSnapshot"

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
          printfn "PersistVote"
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
          printfn "PersistTerm"
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
          printfn "PersistLog"

        member self.DeleteLog log =
          printfn "DeleteLog"

        member self.LogMsg node callsite level msg =
          Logger.log level node.Id callsite msg

        }


  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  // ** appendEntry

  let private appendEntry (state: RaftAppContext) (entry: RaftLogEntry) =
    let result =
      entry
      |> Raft.receiveEntry
      |> runRaft state.Raft state.Callbacks

    match result with

    | Right (appended, raftState) ->
      (appended, updateRaft state raftState)
      |> Either.succeed

    | Left (err, raftState) ->
      (err, updateRaft state raftState)
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
  /// - state: current RaftServerState to work against
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
         %A %A:%d" (string id) (string ip) port
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
         %A"
      |> Logger.debug state.Raft.Node.Id tag

      let potentialChange =
        state.Raft
        |> Raft.getNode id

      match potentialChange with

      | Some node -> removeNodes state [| node |]
      | None ->
        sprintf "Unable to remove node. Not found: %A" (string id)
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
                                   (sender: Id)
                                   (ae: AppendEntries)
                                   (channel: ReplyChan) =
    let result =
      Raft.receiveAppendEntries (Some sender) ae
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (response, newstate) ->
      AppendEntriesResponse(state.Raft.Node.Id, response)
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

    | Left (err, newstate) ->
      ErrorResponse err
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

  // ** processVoteRequest

  let private processVoteRequest (state: RaftAppContext)
                                 (sender: Id)
                                 (vr: VoteRequest)
                                 (channel: ReplyChan) =
    let result =
      Raft.receiveVoteRequest sender vr
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (response, newstate) ->
      RequestVoteResponse(state.Raft.Node.Id, response)
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

    | Left (err, newstate) ->
      ErrorResponse err
      |> Reply.Response
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

  // ** processInstallSnapshot

  let private processInstallSnapshot (state: RaftAppContext)
                                     (sender: Id)
                                     (snapshot: InstallSnapshot)
                                     (channel: ReplyChan) =
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
  /// - state: RaftServerState
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
  /// - appState: current TVar<RaftServerState>
  ///
  /// Returns: RaftResponse
  let private processHandshake (state: RaftAppContext)
                               (node: RaftNode)
                               (channel: ReplyChan) =
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
                               (node: RaftNode)
                               (channel: ReplyChan) =
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

  // ** processAppendResponse

  let private processAppendResponse (state: RaftAppContext)
                                    (sender: Id)
                                    (ar: AppendResponse)
                                    (channel: ReplyChan) =
    let result =
      Raft.receiveAppendEntriesResponse sender ar
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) ->
      Reply.Ok
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

    | Left (err, newstate) ->
      err
      |> Either.fail
      |> channel.Reply
      updateRaft state newstate

  // ** processVoteResponse

  let private processVoteResponse (state: RaftAppContext)
                                  (sender: Id)
                                  (vr: VoteResponse)
                                  (channel: ReplyChan) =
    let result =
      Raft.receiveVoteResponse sender vr
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) ->
      Reply.Ok
      |> Either.succeed
      |> channel.Reply
      updateRaft state newstate

    | Left (err, newstate) ->
      err
      |> Either.fail
      |> channel.Reply
      updateRaft state newstate

  // ** processSnapshotResponse

  let private processSnapshotResponse (state: RaftAppContext)
                                      (sender: Id)
                                      (ar: AppendResponse)
                                      (channel: ReplyChan) =
    "FIX RESPONSE PROCESSING FOR SNAPSHOT REQUESTS"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processRedirect

  let private processRedirect (state: RaftAppContext)
                              (leader: RaftNode)
                              (channel: ReplyChan) =
    "FIX REDIRECT RESPONSE PROCESSING"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

  // ** processWelcome

  let private processWelcome (state: RaftAppContext)
                             (leader: RaftNode)
                             (channel: ReplyChan) =

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
                                   (error: IrisError)
                                   (channel: ReplyChan) =

    error
    |> sprintf "received error response: %A"
    |> Logger.err state.Raft.Node.Id tag

    Reply.Ok
    |> Either.succeed
    |> channel.Reply
    state

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
  /// - appState: RaftServerState TVar
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

  //  _                    _ _
  // | |    ___   __ _  __| (_)_ __   __ _
  // | |   / _ \ / _` |/ _` | | '_ \ / _` |
  // | |__| (_) | (_| | (_| | | | | | (_| |
  // |_____\___/ \__,_|\__,_|_|_| |_|\__, |
  //                                 |___/

  // ** rand

  let private rand = new System.Random()

  // ** initializeRaft

  let private initializeRaft (state: RaftValue) (callbacks: IRaftCallbacks) =
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
          rand.Next(0, int state.ElectionTimeout)
          |> uint32
        do! Raft.setTimeoutElapsedM timeout
        do! Raft.becomeFollower ()
    }
    |> runRaft state callbacks

  // ** startPeriodic

  /// ## startPeriodic
  ///
  /// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
  /// loop can be cancelled at a later time.
  ///
  /// ### Signature:
  /// - timeoput: interval at which the loop runs
  /// - appState: current RaftServerState TVar
  ///
  /// Returns: CancellationTokenSource
  let private startPeriodic (interval: int) (arbiter: StateArbiter) : IDisposable =
    MailboxProcessor.Start(fun inbox ->
      let rec loop n =
        async {
          inbox.Post()                  // kick the machine
          let! msg = inbox.Receive()
          arbiter.Post(Msg.Periodic)
          do! Async.Sleep(interval) // sleep for inverval (ms)
          return! loop (n + 1)
        }
      loop 0)
    :> IDisposable

  // ** load

  let private load (config: IrisConfig)
                   (subscriptions: Subscriptions)
                   (chan: ReplyChan)
                   (agent: StateArbiter) =
    either {
      let connections = new Connections()

      let! raftstate = Persistence.getRaft config
      let addr = raftstate.Node |> nodeUri
      let server = new Zmq.Rep(addr, requestHandler agent)

      match server.Start() with
      | Right _ ->
        let callbacks =
          mkCallbacks
            raftstate.Node.Id
            connections
            subscriptions

        Map.iter
          (fun _ (peer: RaftNode) ->
            if peer.Id <> raftstate.Node.Id then
              getConnection raftstate.Node.Id connections peer
              |> ignore)
          raftstate.Peers

        // periodic function
        let interval = int config.RaftConfig.PeriodicInterval
        let periodic = startPeriodic interval agent

        match initializeRaft raftstate callbacks with
        | Right (_, newstate) ->
          return
            Loaded { Status      = ServiceStatus.Running
                     Raft        = newstate
                     Callbacks   = callbacks
                     Options     = config
                     Periodic    = periodic
                     Server      = server
                     Connections = connections }

        | Left (err, _) ->
          dispose server
          disposeAll connections
          dispose periodic
          return! Either.fail err

      | Left error ->
        dispose server
        return! Either.fail error
    }

  // ** handleLoad

  let private handleLoad (state: RaftServerState)
                         (config: IrisConfig)
                         (subscriptions: Subscriptions)
                         (chan: ReplyChan)
                         (agent: StateArbiter) =
    match state with
    | Loaded data -> dispose data
    | Idle -> ()

    match load config subscriptions chan agent with
    | Right state ->
      Reply.Ok
      |> Either.succeed
      |> chan.Reply
      state

    | Left error ->
      error
      |> Either.fail
      |> chan.Reply
      Idle

  // ** handleUnload

  let private handleUnload (state: RaftServerState) (chan: ReplyChan) =
    match state with
    | Idle ->
      Reply.Ok
      |> Either.succeed
      |> chan.Reply
      state
    | Loaded data ->
      dispose data
      Reply.Ok
      |> Either.succeed
      |> chan.Reply
      Idle

  // ** handleStatus

  let private handleStatus (state: RaftServerState) (chan: ReplyChan) =
    match state with
    | Idle ->
      ServiceStatus.Stopped
      |> Reply.Status
      |> Either.succeed
      |> chan.Reply
    | Loaded data ->
      data.Status
      |> Reply.Status
      |> Either.succeed
      |> chan.Reply
    state

  // ** handleJoin

  let private handleJoin (state: RaftServerState) (ip: IpAddress) (port: UInt16) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match tryJoinCluster data ip port with
      | Right (_, newstate) ->
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
        newstate
        |> updateRaft data
        |> Loaded

      | Left (error, newstate) ->
        error
        |> Either.fail
        |> chan.Reply
        newstate
        |> updateRaft data
        |> Loaded

  // ** handleLeave

  let private handleLeave (state: RaftServerState) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match tryLeaveCluster data with
      | Right (_, newstate) ->
        Reply.Ok
        |> Either.succeed
        |> chan.Reply
        newstate
        |> updateRaft data
        |> Loaded

      | Left (error, newstate) ->
        error
        |> Either.fail
        |> chan.Reply
        newstate
        |> updateRaft data
        |> Loaded

  // ** handleForceElection

  let private handleForceElection (state: RaftServerState) =
    match state with
    | Idle -> state
    | Loaded data ->
      match forceElection data with
      | Right (_, newstate) ->
        newstate
        |> updateRaft data
        |> Loaded

      | Left (err, newstate) ->
        err
        |> sprintf "Unable to force an election: %A"
        |> Logger.err newstate.Node.Id tag

        newstate
        |> updateRaft data
        |> Loaded

  // ** handleAddCmd

  let private handleAddCmd (state: RaftServerState) (cmd: StateMachine) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match appendCommand data cmd with
      | Right (entry, newstate) ->
        Reply.Entry entry
        |> Either.succeed
        |> chan.Reply
        newstate
        |> Loaded

      | Left (err, newstate) ->
        err
        |> Either.fail
        |> chan.Reply
        newstate
        |> Loaded

  // ** handleResponse

  let private handleResponse (state: RaftServerState) (response: RaftResponse) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match response with
      | RequestVoteResponse     (sender, vote) -> processVoteResponse     data sender vote chan
      | AppendEntriesResponse   (sender, ar)   -> processAppendResponse   data sender ar   chan
      | InstallSnapshotResponse (sender, ar)   -> processSnapshotResponse data sender ar   chan
      | ErrorResponse            error         -> processErrorResponse    data error       chan
      | _                                      -> data
      |> Loaded


  // ** handleRequest

  let private handleRequest (state: RaftServerState) (req: RaftRequest) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match req with
      | AppendEntries (id, ae)   -> processAppendEntries   data id ae chan
      | RequestVote (id, vr)     -> processVoteRequest     data id vr chan
      | InstallSnapshot (id, is) -> processInstallSnapshot data id is chan
      | HandShake node           -> processHandshake       data node  chan
      | HandWaive node           -> processHandwaive       data node  chan
      |> Loaded

  // ** handlePeriodic

  let private handlePeriodic (state: RaftServerState) =
    match state with
    | Idle -> Idle
    | Loaded data ->
      uint32 data.Options.RaftConfig.PeriodicInterval
      |> Raft.periodic
      |> evalRaft data.Raft data.Callbacks
      |> updateRaft data
      |> Loaded

  // ** handleGet

  let private handleGet (state: RaftServerState) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      Reply.State data
      |> Either.succeed
      |> chan.Reply
      state


  // ** handleAddNode

  let private handleAddNode (state: RaftServerState) (node: RaftNode) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match addNodes data [| node |] with
      | Right (entry, newstate) ->
        Reply.Entry entry
        |> Either.succeed
        |> chan.Reply
        newstate
        |> Loaded

      | Left (err, newstate) ->
        err
        |> Either.fail
        |> chan.Reply
        newstate
        |> Loaded

  // ** handleRemoveNode

  let private handleRemoveNode (state: RaftServerState) (id: Id) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      match removeNode data id with
      | Right (entry, newstate) ->
        Reply.Entry entry
        |> Either.succeed
        |> chan.Reply
        newstate
        |> Loaded

      | Left (err, newstate) ->
        err
        |> Either.fail
        |> chan.Reply
        newstate
        |> Loaded

  // ** handleIsCommitted

  let private handleIsCommitted (state: RaftServerState) (entry: EntryResponse) (chan: ReplyChan) =
    match state with
    | Idle ->
      "No config loaded"
      |> RaftError
      |> Either.fail
      |> chan.Reply
      state

    | Loaded data ->
      let result =
        Raft.responseCommitted entry
        |> runRaft data.Raft data.Callbacks

      match result with
      | Right (committed, _) ->
        committed
        |> Reply.IsCommitted
        |> Either.succeed
        |> chan.Reply
        state

      | Left  (err, _) ->
        err
        |> Either.fail
        |> chan.Reply
        state

  // ** loop

  let private loop (subscriptions: Subscriptions) (inbox: StateArbiter) =
    let rec act state =
      async {
        let! cmd = inbox.Receive()

        let newstate =
          match cmd with
          | Msg.Load (config, chan)    -> handleLoad          state config subscriptions chan inbox
          | Msg.Unload chan            -> handleUnload        state                      chan
          | Msg.Status chan            -> handleStatus        state                      chan
          | Msg.Join (ip, port, chan)  -> handleJoin          state ip port              chan
          | Msg.Leave chan             -> handleLeave         state                      chan
          | Msg.Periodic               -> handlePeriodic      state
          | Msg.ForceElection          -> handleForceElection state
          | Msg.AddCmd (cmd, chan)     -> handleAddCmd        state cmd                  chan
          | Msg.Request (req, chan)    -> handleRequest       state req                  chan
          | Msg.Response (resp, chan)  -> handleResponse      state resp                 chan
          | Msg.Get chan               -> handleGet           state                      chan
          | Msg.AddNode (node, chan)   -> handleAddNode       state node                 chan
          | Msg.RmNode (id, chan)      -> handleRemoveNode    state id                   chan
          | Msg.IsCommitted (ety,chan) -> handleIsCommitted   state ety                  chan

        do! act newstate
      }
    act Idle

  // ** withOk

  let private withOk (msgcb: ReplyChan -> Msg) (agent: StateArbiter) : Either<IrisError,unit> =
    match agent.PostAndReply(msgcb) with
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
    match agent.PostAndReply(fun chan -> Msg.AddCmd(cmd,chan)) with
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

  // ** getStatus

  let private getStatus (agent: StateArbiter) =
    match agent.PostAndReply(fun chan -> Msg.Status chan) with
    | Right (Reply.Status status) -> Right status

    | Right other ->
      sprintf "Received garbage reply from agent: %A" other
      |> Other
      |> Either.fail

    | Left error ->
      Either.fail error

  // ** addNode

  let private addNode (agent: StateArbiter) (node: RaftNode) =
    match agent.PostAndReply(fun chan -> Msg.AddNode(node,chan)) with
    | Right (Reply.Entry entry) -> Right entry
    | Right other ->
      sprintf "Unexpected reply by agent: %A" other
      |> Other
      |> Either.fail
    | Left error ->
      Either.fail error

  // ** rmNode

  let private rmNode (agent: StateArbiter) (id: Id) =
    match agent.PostAndReply(fun chan -> Msg.RmNode(id,chan)) with
    | Right (Reply.Entry entry) -> Right entry
    | Right other ->
      sprintf "Unexpected reply by agent: %A" other
      |> Other
      |> Either.fail
    | Left error ->
      Either.fail error

  // ** getState

  let private getState (agent: StateArbiter) =
    match agent.PostAndReply(fun chan -> Msg.Get chan) with
    | Right (Reply.State state) -> Right state

    | Right other ->
      sprintf "Received garbage reply from agent: %A" other
      |> Other
      |> Either.fail

    | Left error ->
      Either.fail error

  // ** startServer

  let private startServer (agent: StateArbiter) =
    Either.succeed ()

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  [<RequireQualifiedAccess>]
  module RaftServer =

    let create () =
      either {
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

        let agent = new StateArbiter(loop subscriptions)

        agent.Start()

        return
          { new IRaftServer with
              member self.Load (config: IrisConfig) =
                match agent.PostAndReply(fun chan -> Msg.Load(config,chan)) with
                | Right Reply.Ok -> Right ()
                | Right other ->
                  sprintf "Unexpected reply type from agent: %A" other
                  |> RaftError
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Unload () =
                match agent.PostAndReply(fun chan -> Msg.Unload chan) with
                | Right Reply.Ok -> Right ()
                | Right other ->
                  sprintf "Unexpected reply type from agent: %A" other
                  |> RaftError
                  |> Either.fail
                | Left error ->
                  error
                  |> Either.fail

              member self.Node
                with get () =
                  match getState agent with
                  | Right state ->
                    state.Raft.Node
                    |> Either.succeed
                  | Left error ->
                    Either.fail error

              member self.NodeId
                with get () =
                  match getState agent with
                  | Right state ->
                    state.Raft.Node.Id
                    |> Either.succeed
                  | Left error ->
                    Either.fail error

              member self.Append cmd =
                addCmd agent cmd

              member self.Status
                with get () = getStatus agent

              member self.ForceElection () =
                agent.Post Msg.ForceElection
                |> Either.succeed

              member self.Periodic () =
                agent.Post Msg.Periodic
                |> Either.succeed

              member self.JoinCluster ip port =
                withOk (fun chan -> Msg.Join(ip, port, chan)) agent

              member self.LeaveCluster () =
                withOk (fun chan -> Msg.Leave chan) agent

              member self.AddNode node = addNode agent node

              member self.RmNode id = rmNode agent id

              member self.State
                with get () = getState agent

              member self.Subscribe (callback: RaftEvent -> unit) =
                { new IObserver<RaftEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Start () = startServer agent

              member self.Connections
                with get () =
                  match getState agent with
                  | Right ctx  -> ctx.Connections |> Either.succeed
                  | Left error -> error |> Either.fail

              member self.Dispose () =
                match agent.PostAndReply(fun chan -> Msg.Unload chan) with
                | Left error -> printfn "unable to dispose: %A" error
                | Right _ -> ()
                subscriptions.Clear()
                dispose agent
            }
      }

    let isLeader (server: IRaftServer) : bool =
      match server.State with
      | Right state -> Raft.isLeader state.Raft
      | _ -> false
