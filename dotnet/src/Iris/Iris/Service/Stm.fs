module Iris.Service.Stm

// ----------------------------------------------------------------------------------------- //
//                                    ____ _____ __  __                                      //
//                                   / ___|_   _|  \/  |                                     //
//                                   \___ \ | | | |\/| |                                     //
//                                    ___) || | | |  | |                                     //
//                                   |____/ |_| |_|  |_|                                     //
// ----------------------------------------------------------------------------------------- //

open Iris.Core
open Iris.Raft
// open FSharpx.Stm
open FSharpx.Functional
open System.Threading
open Utilities
open System
open Iris.Service.Zmq

type TVar<'a> = 'a ref

let newTVar (value: 'a) = ref value

let readTVar (var: TVar<'a>) = !var

let writeTVar (var: TVar<'a>) (value: 'a) = var := value

let atomically = id

let logMsg level site (state: RaftAppContext) (cbs: IRaftCallbacks) (msg: string) =
  cbs.LogMsg state.Raft.Node site level msg

let debugMsg state cbs site msg = logMsg Debug site state cbs msg
let infoMsg  state cbs site msg = logMsg Info site state cbs msg
let warnMsg state cbs site msg = logMsg Warn site state cbs msg
let errMsg state cbs site msg = logMsg Err site state cbs msg

/// ## waitForCommit
///
/// Block execution until an entry has successfully been committed in the cluster.
///
/// ### Signature:
/// - appended: EntryResponse returned by receiveEntry
/// - appState: TVar<RaftAppContext> transactional state variable
/// - cbs: IRaftCallbacks
///
/// Returns: bool
let waitForCommit (appended: EntryResponse) (appState: TVar<RaftAppContext>) cbs =
  let ok = ref true
  let run = ref true

  // wait for the entry to be committed by everybody
  while !run do
    let state = readTVar appState |> atomically
    let committed =
      Raft.responseCommitted appended
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
/// - state: current RaftAppContext to work against
/// - cbs: IRaftCallbacks
///
/// Returns: Either<RaftError * Raft, unit * Raft, EntryResponse * Raft>
let joinCluster (nodes: RaftNode array) (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  let changes = Array.map NodeAdded nodes
  let result =
    raft {
      let! term = Raft.currentTermM ()
      let entry = JointConsensus(Id.Create(), 0u, term, changes, None) //
      do! Raft.debug "joinCluster" "appending entry to enter joint-consensus"
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
      Welcome raftState.Node
    else
      ErrorResponse <| Other "Could not commit JointConsensus"

  | Left (err, raftState) ->
    // save the new raft value back to the TVar
    writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
    ErrorResponse err

let onConfigDone (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  let result =
    raft {
      let! term = Raft.currentTermM ()
      let! nodes = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)
      let entry = Log.mkConfig term nodes
      let str = "appending entry to exit joint-consensus into regular configuration"
      do! Raft.debug "onConfigDone" str
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
let leaveCluster (nodes: RaftNode array) (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  let changes = Array.map NodeRemoved nodes
  let result =
    raft {
      let! term = Raft.currentTermM ()
      let entry = JointConsensus(Id.Create(), 0u, term, changes, None)
      do! Raft.debug "leaveCluster" "appending entry to enter joint-consensus"
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
/// - state: RaftAppContext
///
/// Returns: Stm<RaftResponse>
let doRedirect state =
  match Raft.getLeader state.Raft with
  | Some node -> Redirect node
  | _         -> ErrorResponse (Other "No known leader")

/// ## Handle AppendEntries requests
///
/// Handler for AppendEntries requests. Returns an appropriate response value.
///
/// ### Signature:
/// - sender:   Raft node which sent the request
/// - ae:       AppendEntries request value
/// - appState: RaftAppContext TVar
/// - cbs:      Raft callbacks
///
/// Returns: Stm<RaftResponse>
let handleAppendEntries sender ae (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically

  let result =
    Raft.receiveAppendEntries (Some sender) ae
    |> runRaft state.Raft cbs

  match result with
  | Right (resp, raftState) ->
    writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
    AppendEntriesResponse(raftState.Node.Id, resp)

  | Left (err, raftState) ->
    writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
    ErrorResponse err

/// ## Handle the AppendEntries request response.
///
/// Handle the request entries response.
///
/// ### Signature:
/// - sender: Node who replied
/// - ar: AppendResponse to process
/// - appState: TVar<RaftAppContext>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleAppendResponse sender ar (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically

  Raft.receiveAppendEntriesResponse sender ar
  |> evalRaft state.Raft cbs
  |> flip RaftContext.updateRaft state
  |> writeTVar appState
  |> atomically

/// ## Handle a vote request.
///
/// Handle a vote request and return a response.
///
/// ### Signature:
/// - sender: Node which sent request
/// - req: the `VoteRequest`
/// - appState: current TVar<RaftAppContext>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleVoteRequest sender req (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically

  let result =
    Raft.receiveVoteRequest sender req
    |> runRaft state.Raft cbs

  match result with
  | Right (resp, raftState) ->
    writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
    RequestVoteResponse(raftState.Node.Id, resp)

  | Left (err, raftState) ->
    writeTVar appState (RaftContext.updateRaft raftState state) |> atomically
    ErrorResponse err

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
let handleVoteResponse sender rep (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  Raft.receiveVoteResponse sender rep
  |> evalRaft state.Raft cbs
  |> flip RaftContext.updateRaft state
  |> writeTVar appState
  |> atomically

/// ## Handle a HandShake request by a certain Node.
///
/// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
/// leader if we are currently not Leader.
///
/// ### Signature:
/// - node: Node which wants to join the cluster
/// - appState: current TVar<RaftAppContext>
/// - cbs: IRaftCallbacks
///
/// Returns: RaftResponse
let handleHandshake node (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  if Raft.isLeader state.Raft then
    joinCluster [| node |] appState cbs
  else
    doRedirect state

let handleHandwaive node (appState: TVar<RaftAppContext>) cbs =
  let state = readTVar appState |> atomically
  if Raft.isLeader state.Raft then
    leaveCluster [| node |] appState cbs
  else
    doRedirect state

let appendEntry (cmd: StateMachine) appState cbs =
  either {
    let state = readTVar appState |> atomically

    let update raftState =
      state
      |> RaftContext.updateRaft raftState
      |> writeTVar appState
      |> atomically

    let result =
      Log.make (Raft.currentTerm state.Raft) cmd
      |> Raft.receiveEntry
      |> runRaft state.Raft cbs

    match result with
    | Right (appended, raftState) ->
      update raftState
      let ok = waitForCommit appended appState cbs
      if ok then
        return appended
      else
        return!
          "Unable to commit entry"
          |> Other
          |> Either.fail
    | Left (err, raftState) ->
      update raftState
      return! Either.fail err
  }


let handleInstallSnapshot node snapshot (appState: TVar<RaftAppContext>) cbs =
  // let snapshot = createSnapshot () |> runRaft raft' cbs
  let state = readTVar appState |> atomically
  let ar = { Term         = state.Raft.CurrentTerm
           ; Success      = false
           ; CurrentIndex = Raft.currentIndex state.Raft
           ; FirstIndex   = match Raft.firstIndex state.Raft.CurrentTerm state.Raft with
                            | Some idx -> idx
                            | _        -> 0u }
  InstallSnapshotResponse(state.Raft.Node.Id, ar)

let handleRequest msg (state: TVar<RaftAppContext>) cbs : RaftResponse =
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

let startServer (appState: TVar<RaftAppContext>) (cbs: IRaftCallbacks) =
  let uri =
    readTVar appState
    |> atomically
    |> fun state -> state.Raft.Node
    |> nodeUri

  let handler (request: byte array) : byte array =
    let request = Binary.decode<IrisError,RaftRequest> request
    let response =
      match request with
      | Right message -> handleRequest message appState cbs
      | Left error    -> ErrorResponse error

    response |> Binary.encode

  let server = new Rep(uri, handler)
  server.Start()
  server

let periodicR (state: RaftAppContext) cbs =
  Raft.periodic (uint32 state.Options.RaftConfig.PeriodicInterval)
  |> evalRaft state.Raft cbs
  |> flip RaftContext.updateRaft state

/// ## startPeriodic
///
/// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
/// loop can be cancelled at a later time.
///
/// ### Signature:
/// - timeoput: interval at which the loop runs
/// - appState: current RaftAppContext TVar
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
let tryJoin (ip: IpAddress) (port: uint32) cbs (state: RaftAppContext) =
  let rec _tryJoin retry peer =
    either {
      if retry < int state.Options.RaftConfig.MaxRetries then
        let client = mkReqSocket peer state.Context

        sprintf "Retry: %d" retry
        |> debugMsg state cbs "tryJoin"

        let request = HandShake(state.Raft.Node)
        let! result = rawRequest request client

        sprintf "Result: %A" result
        |> debugMsg state cbs "tryJoin"

        dispose client

        match result with
        | Welcome node ->
          sprintf "Received Welcome from %A" node.Id
          |> debugMsg state cbs "tryJoin"
          return node

        | Redirect next ->
          sprintf "Got redirected to %A" (nodeUri next)
          |> infoMsg state cbs "tryJoin"
          return! _tryJoin (retry + 1) next

        | ErrorResponse err ->
          sprintf "Unexpected error occurred. %A" err
          |> errMsg state cbs "tryJoin"
          return! Either.fail err

        | resp ->
          sprintf "Unexpected response. %A" resp
          |> errMsg state cbs "tryJoin"
          return! Either.fail (Other "Unexpected response")
      else
        "Too many unsuccesful connection attempts."
        |> errMsg state cbs "tryJoin"
        return! Either.fail (Other "Too many unsuccesful connection attempts.")
    }

  _tryJoin 0 { Node.create (Id.Create()) with
                IpAddr = ip
                Port = uint16 port }

/// ## Attempt to leave a Raft cluster
///
/// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
/// AppendEntries request with a JointConsensus entry.
///
/// ### Signature:
/// - appState: RaftAppContext TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let tryLeave (appState: TVar<RaftAppContext>) cbs : Either<IrisError,bool> =
  let state = readTVar appState |> atomically

  let rec _tryLeave retry node =
    either {
      if retry < int state.Options.RaftConfig.MaxRetries then
        let client = mkReqSocket node state.Context
        let request = HandWaive(state.Raft.Node)
        let! result = rawRequest request client

        dispose client

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

let forceElection appState cbs =
  let state = readTVar appState |> atomically

  raft {
    let! timeout = Raft.electionTimeoutM ()
    do! Raft.setTimeoutElapsedM timeout
    do! Raft.periodic timeout
  }
  |> evalRaft state.Raft cbs
  |> flip RaftContext.updateRaft state
  |> writeTVar appState
  |> atomically

let prepareSnapshot appState snapshot =
  let state = readTVar appState |> atomically
  Raft.createSnapshot (DataSnapshot snapshot) state.Raft

let resetConnections (connections: Map<Id,Zmq.Req>) =
  Map.iter (fun _ sock -> dispose sock) connections

let initialize appState cbs =
  let state = readTVar appState |> atomically

  let newstate =
    raft {
      let term = 0u
      do! Raft.setTermM term
      do! Raft.setTimeoutElapsedM 0u

      let! num = Raft.numNodesM ()

      if num = 1u then
        do! Raft.becomeLeader ()
      else
        do! Raft.becomeFollower ()

    } |> evalRaft state.Raft cbs

  "initialize: saving new state"
  |> debugMsg state cbs "initialize"

  // tryJoin leader
  writeTVar appState (RaftContext.updateRaft newstate state)
  |> atomically
