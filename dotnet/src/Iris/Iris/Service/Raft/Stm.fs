module Iris.Service.Raft.Stm

// ----------------------------------------------------------------------------------------- //
//                                    ____ _____ __  __                                      //
//                                   / ___|_   _|  \/  |                                     //
//                                   \___ \ | | | |\/| |                                     //
//                                    ___) || | | |  | |                                     //
//                                   |____/ |_| |_|  |_|                                     //
// ----------------------------------------------------------------------------------------- //

open Iris.Core
open Iris.Service
open Iris.Raft
open Iris.Service.Raft
// open FSharpx.Stm
open FSharpx.Functional
open System.Threading
open Utilities
open System
open Zmq
open Db

type TVar<'a> = 'a ref

let newTVar (value: 'a) = ref value

let readTVar (var: TVar<'a>) = !var

let writeTVar (var: TVar<'a>) (value: 'a) = var := value

let atomically = id

let logMsg level (state: RaftAppState) (cbs: IRaftCallbacks) (msg: string) =
  cbs.LogMsg level state.Raft.Node msg

let debugMsg state cbs msg = logMsg Debug state cbs msg
let infoMsg state cbs msg = logMsg Info state cbs msg
let warnMsg state cbs msg = logMsg Warn state cbs msg
let errMsg state cbs msg = logMsg Err state cbs msg

/// ## waitForCommit
///
/// Block execution until an entry has successfully been committed in the cluster.
///
/// ### Signature:
/// - appended: EntryResponse returned by receiveEntry
/// - appState: TVar<RaftAppState> transactional state variable
/// - cbs: IRaftCallbacks
///
/// Returns: bool
let waitForCommit (appended: EntryResponse) (appState: TVar<RaftAppState>) cbs =
  let ok = ref true
  let run = ref true

  // wait for the entry to be committed by everybody
  while !run do
    let state = readTVar appState |> atomically
    let committed =
      responseCommitted appended
      |> runRaft state.Raft cbs
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

/// ## enterJointConsensus
///
/// Enter the Joint-Consensus by apppending a respective log entry.
///
/// ### Signature:
/// - changes: the changes to make to the current cluster configuration
/// - state: current RaftAppState to work against
/// - cbs: IRaftCallbacks
///
/// Returns: Either<RaftError * Raft, unit * Raft, EntryResponse * Raft>
let joinCluster (nodes: RaftNode array) (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  let changes = Array.map NodeAdded nodes
  let result =
    raft {
      let! term = currentTermM ()
      let entry = JointConsensus(Id.Create(), 0u, term, changes, None) //
      do! debug "HandShake: appending entry to enter joint-consensus"
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
      Welcome raftState.Node
    else
      ErrorResponse <| Other "Could not commit JointConsensus"

  | Left (err, raftState) ->
    // save the new raft value back to the TVar
    writeTVar appState (updateRaft raftState state) |> atomically
    ErrorResponse err

let onConfigDone (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  let result =
    raft {
      let! term = currentTermM ()
      let! nodes = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)
      let entry = Log.mkConfig term nodes
      do! debug "onConfigDone: appending entry to exit joint-consensus into regular configuration"
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


/// ## leaveCluster
///
/// Function to execute a two-phase commit for adding/removing members from the cluster.
///
/// ### Signature:
/// - changes: configuration changes to make to the cluster
/// - success: RaftResponse to return when successful
/// - appState: transactional variable to work against
/// - cbs: IRaftCallbacks
///
/// Returns: RaftResponse
let leaveCluster (nodes: RaftNode array) (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  let changes = Array.map NodeRemoved nodes
  let result =
    raft {
      let! term = currentTermM ()
      let entry = JointConsensus(Id.Create(), 0u, term, changes, None)
      do! debug "HandWaive: appending entry to enter joint-consensus"
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
      // now that all nodes are in joint-consensus we need to wait and finalize the 2-phase commit
      Arrivederci
    else
      ErrorResponse <| Other "Could not commit Joint-Consensus"

  | Left (err,_) ->
    ErrorResponse err

/// ## Redirect to leader
///
/// Gets the current leader node from the Raft state and returns a corresponding RaftResponse.
///
/// ### Signature:
/// - state: RaftAppState
///
/// Returns: Stm<RaftResponse>
let doRedirect state =
  match getLeader state.Raft with
  | Some node -> Redirect node
  | _         -> ErrorResponse (Other "No known leader")

/// ## Handle AppendEntries requests
///
/// Handler for AppendEntries requests. Returns an appropriate response value.
///
/// ### Signature:
/// - sender:   Raft node which sent the request
/// - ae:       AppendEntries request value
/// - appState: RaftAppState TVar
/// - cbs:      Raft callbacks
///
/// Returns: Stm<RaftResponse>
let handleAppendEntries sender ae (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically

  let result =
    receiveAppendEntries (Some sender) ae
    |> runRaft state.Raft cbs

  match result with
  | Right (resp, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    AppendEntriesResponse(raftState.Node.Id, resp)

  | Left (err, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    ErrorResponse err

/// ## Handle the AppendEntries request response.
///
/// Handle the request entries response.
///
/// ### Signature:
/// - sender: Node who replied
/// - ar: AppendResponse to process
/// - appState: TVar<RaftAppState>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleAppendResponse sender ar (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically

  receiveAppendEntriesResponse sender ar
  |> evalRaft state.Raft cbs
  |> flip updateRaft state
  |> writeTVar appState
  |> atomically

/// ## Handle a vote request.
///
/// Handle a vote request and return a response.
///
/// ### Signature:
/// - sender: Node which sent request
/// - req: the `VoteRequest`
/// - appState: current TVar<RaftAppState>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleVoteRequest sender req (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically

  let result =
    receiveVoteRequest sender req
    |> runRaft state.Raft cbs

  match result with
  | Right (resp, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    RequestVoteResponse(raftState.Node.Id, resp)

  | Left (err, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    ErrorResponse err

/// ## Handle the response to a vote request.
///
/// Handle the response to a vote request.
///
/// ### Signature:
/// - sender: Node which sent the response
/// - resp: VoteResponse to process
/// - appState: current TVar<RaftAppState>
///
/// Returns: unit
let handleVoteResponse sender rep (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  receiveVoteResponse sender rep
  |> evalRaft state.Raft cbs
  |> flip updateRaft state
  |> writeTVar appState
  |> atomically

/// ## Handle a HandShake request by a certain Node.
///
/// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
/// leader if we are currently not Leader.
///
/// ### Signature:
/// - node: Node which wants to join the cluster
/// - appState: current TVar<RaftAppState>
/// - cbs: IRaftCallbacks
///
/// Returns: RaftResponse
let handleHandshake node (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  if isLeader state.Raft then
    joinCluster [| node |] appState cbs
  else
    doRedirect state

let handleHandwaive node (appState: TVar<RaftAppState>) cbs =
  let state = readTVar appState |> atomically
  if isLeader state.Raft then
    leaveCluster [| node |] appState cbs
  else
    doRedirect state

let appendEntry (cmd: StateMachine) appState cbs =
  let state = readTVar appState |> atomically

  let result =
    receiveEntry (Log.make (currentTerm state.Raft) cmd)
    >>= returnM
    |> runRaft state.Raft cbs

  match result with
  | Right (appended, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    let ok = waitForCommit appended appState cbs
    if ok then
      Some appended
    else
      None
  | Left (err, raftState) ->
    writeTVar appState (updateRaft raftState state) |> atomically
    sprintf "encountered error in receiveEntry: %A" err
    |> errMsg state cbs
    None

let handleInstallSnapshot node snapshot (appState: TVar<RaftAppState>) cbs =
  // let snapshot = createSnapshot () |> runRaft raft' cbs
  let state = readTVar appState |> atomically
  let ar = { Term         = state.Raft.CurrentTerm
           ; Success      = false
           ; CurrentIndex = currentIndex state.Raft
           ; FirstIndex   = match firstIndex state.Raft.CurrentTerm state.Raft with
                            | Some idx -> idx
                            | _        -> 0u }
  InstallSnapshotResponse(state.Raft.Node.Id, ar)

let handleRequest msg (state: TVar<RaftAppState>) cbs : RaftResponse =
  match msg with
  | RequestVote (sender, req) ->
    handleVoteRequest sender req state cbs

  | AppendEntries (sender, ae) ->
    handleAppendEntries  sender ae state cbs

  | HandShake node ->
    handleHandshake node state cbs

  | HandWaive node ->
    handleHandwaive node state cbs

  | InstallSnapshot (sender, snapshot) ->
    handleInstallSnapshot sender snapshot state cbs

let startServer (appState: TVar<RaftAppState>) (cbs: IRaftCallbacks) =
  let uri =
    readTVar appState
    |> atomically
    |> fun state -> state.Raft.Node
    |> nodeUri

  let handler (request: byte array) : byte array =
    let request : RaftRequest option = Binary.decode request
    let response =
      match request with
      | Some message -> handleRequest message appState cbs
      | None         -> ErrorResponse (Other "Unable to decipher request")

    response |> Binary.encode

  let server = new Rep(uri, handler)
  server.Start()
  server

let periodicR (state: RaftAppState) cbs =
  periodic (uint32 state.Options.RaftConfig.PeriodicInterval)
  |> evalRaft state.Raft cbs
  |> flip updateRaft state

/// ## startPeriodic
///
/// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
/// loop can be cancelled at a later time.
///
/// ### Signature:
/// - timeoput: interval at which the loop runs
/// - appState: current RaftAppState TVar
/// - cbs: Raft Callbacks
///
/// Returns: CancellationTokenSource
let startPeriodic appState cbs =
  let token = new CancellationTokenSource()
  let rec proc () =
    async {
      let state = readTVar appState |> atomically

      periodicR state cbs
      |> writeTVar appState
      |> atomically

      Thread.Sleep(int state.Options.RaftConfig.PeriodicInterval) // sleep for 100ms
      return! proc ()                                  // recurse
    }
  Async.Start(proc(), token.Token)
  token                               // return the cancellation token source so this loop can be

// -------------------------------------------------------------------------
let tryJoin (ip: IpAddress) (port: uint32) cbs (state: RaftAppState) =
  let rec _tryJoin retry uri =
    if retry < int state.Options.RaftConfig.MaxRetries then
      let client = mkClientSocket uri state

      sprintf "Trying To Join Cluster. Retry: %d" retry
      |> debugMsg state cbs

      let request = HandShake(state.Raft.Node)
      let result = rawRequest request client

      sprintf "Result: %A" result
      |> debugMsg state cbs

      dispose client

      match result with
      | Some (Welcome node) ->
        sprintf "Received Welcome from %A" node.Id
        |> debugMsg state cbs
        Some node

      | Some (Redirect next) ->
        sprintf "Got redirected to %A" (nodeUri next)
        |> infoMsg state cbs
        _tryJoin (retry + 1) (nodeUri next)

      | Some (ErrorResponse err) ->
        sprintf "Unexpected error occurred. %A" err
        |> errMsg state cbs
        None

      | Some resp ->
        sprintf "Unexpected response. %A" resp
        |> errMsg state cbs
        None

      | _ ->
        sprintf "Node: %A unreachable." uri
        |> errMsg state cbs
        None
    else
      "Too many unsuccesful connection attempts."
      |> errMsg state cbs
      None

  formatUri ip (int port) |> _tryJoin 0

/// ## Attempt to leave a Raft cluster
///
/// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
/// AppendEntries request with a JointConsensus entry.
///
/// ### Signature:
/// - appState: RaftAppState TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let tryLeave (appState: TVar<RaftAppState>) cbs : bool option =
  let state = readTVar appState |> atomically

  let rec _tryLeave retry (uri: string) =
    if retry < int state.Options.RaftConfig.MaxRetries then
      let client = mkClientSocket uri state
      let request = HandWaive(state.Raft.Node)
      let result = rawRequest request client

      dispose client

      match result with
      | Some (Redirect other) ->
        if retry <= int state.Options.RaftConfig.MaxRetries then
          nodeUri other |> _tryLeave (retry + 1)
        else
          "Too many retries. aborting" |> errMsg state cbs
          None

      | Some Arrivederci ->
        Some true

      | Some (ErrorResponse err) ->
        sprintf "Unexpected error occurred. %A" err |> errMsg state cbs
        None

      | Some resp ->
        sprintf "Unexpected response.\n%A" resp |> errMsg state cbs
        None

      | _ ->
        "Node unreachable." |> errMsg state cbs
        None
    else
      "Too many unsuccesful connection attempts." |> errMsg state cbs
      None

  match state.Raft.CurrentLeader with
    | Some nid ->
      match Map.tryFind nid state.Raft.Peers with
        | Some node -> nodeUri node |> _tryLeave 0
        | _         ->
          "Node data for leader id not found" |> errMsg state cbs
          None
    | _ ->
      "no known leader" |> errMsg state cbs
      None


let forceElection appState cbs =
  let state = readTVar appState |> atomically

  raft {
    let! timeout = electionTimeoutM ()
    do! setTimeoutElapsedM timeout
    do! periodic timeout
  }
  |> evalRaft state.Raft cbs
  |> flip updateRaft state
  |> writeTVar appState
  |> atomically

let prepareSnapshot appState snapshot =
  let state = readTVar appState |> atomically
  createSnapshot (DataSnapshot snapshot) state.Raft

let resetConnections (connections: Map<Id,Zmq.Req>) =
  Map.iter (fun _ sock -> dispose sock) connections

let initialize appState cbs =
  let state = readTVar appState |> atomically

  let newstate =
    raft {
      let term = 0u
      do! setTermM term
      do! setTimeoutElapsedM 0u

      let! num = numNodesM ()

      if num = 1u then
        do! becomeLeader ()
      else
        do! becomeFollower ()

    } |> evalRaft state.Raft cbs

  "initialize: saving new state"
  |> debugMsg state cbs

  // tryJoin leader
  writeTVar appState (updateRaft newstate state)
  |> atomically
