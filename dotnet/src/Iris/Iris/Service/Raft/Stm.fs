module Iris.Service.Raft.Stm

// ----------------------------------------------------------------------------------------- //
//                                    ____ _____ __  __                                      //
//                                   / ___|_   _|  \/  |                                     //
//                                   \___ \ | | | |\/| |                                     //
//                                    ___) || | | |  | |                                     //
//                                   |____/ |_| |_|  |_|                                     //
// ----------------------------------------------------------------------------------------- //

open fszmq
open Iris.Core
open Iris.Service
open Pallet.Core
open FSharpx.Stm
open FSharpx.Functional
open System.Threading
open Utilities
open System
open Db

/// ## getSocket for Member
///
/// Gets the socket we memoized for given MemberId, else creates one and instantiates a
/// connection.
///
/// ### Signature:
/// - appState: current TVar<AppState>
///
/// Returns: Socket
let getSocket (node: Node) appState =
  stm {
    let! state = readTVar appState

    match Map.tryFind node.Data.MemberId state.Connections with
    | Some client -> return client
    | _           ->
      let! state = readTVar appState

      let socket = Context.req state.Context
      Socket.setOption socket (ZMQ.RCVTIMEO,int state.Raft.RequestTimeout)

      let addr = formatUri node.Data

      Socket.connect socket addr

      let newstate =
        { state with
            Connections = Map.add node.Data.MemberId socket state.Connections }

      do! writeTVar appState newstate

      return socket
  }

/// ## Dispose of a client socket
///
/// Dispose of a client socket that we don't need anymore.
///
/// ### Signature:
/// - node: Node whose socket should be disposed of.
/// - appState: AppState TVar
///
/// Returns: unit
let disposeSocket (node: Node) appState =
  stm {
    let! state = readTVar appState

    match Map.tryFind node.Data.MemberId state.Connections with
    | Some client ->
      dispose client
      let state = { state with Connections = Map.remove node.Data.MemberId state.Connections }
      do! writeTVar appState state
    | _  -> ()
  }

/// ## Receive a reply
///
/// Block until we receive a reply on client Socket. If operation times out, Dispose of the socket
/// (such that it can be re-created next time around).
///
/// ### Signature:
/// - client: Socket
///
/// Returns: RaftResponse option
let receiveReply (node: Node) (client: Socket) appState : RaftResponse option =
  try
    Socket.recv client |> decode
  with
    | :? TimeoutException ->
      disposeSocket node appState |> atomically
      None
    | exn -> handleException "receiveReply" exn

/// ## Send RaftRequest to node
///
/// Sends given RaftRequest to node. If the request times out, None is return to indicate
/// failure. Otherwise the de-serialized RaftResponse is returned, wrapped in option to
/// indicate whether de-serialization was successful.
///
/// ### Signature:
/// - thing:    RaftRequest to send
/// - node:     node to send the message to
/// - appState: application state TVar
///
/// Returns: RaftResponse option
let performRequest (request: RaftRequest) (node: Node<IrisNode>) appState =
  stm {
    let mutable frames = [| |]

    let handler _ (msgs : Message array) =
      frames <- Array.map (Message.data >> decode<RaftResponse>) msgs

    let! state = readTVar appState
    let! client = getSocket node appState

    request |> encode |> Socket.send client

    let reply = receiveReply node client appState

    return reply
  }

/// ## Run Raft periodic functions with AppState
///
/// Runs Raft's periodic function with the current AppState.
///
/// ### Signature:
/// - elapsed: seconds elapsed
/// - appState: AppState TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let periodicR elapsed appState cbs =
  stm {
    let! state = readTVar appState

    do! periodic elapsed
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
  }

/// ## Add a new node to the Raft cluster
///
/// Adds a new node the Raft cluster. This is done in the 2-phase commit model described in the
/// Raft paper.
///
/// ### Signature:
/// - node: Node to be added to the cluster
/// - appState: AppState TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let addNodeR node appState cbs =
  stm {
    let! state = readTVar appState

    let term = currentTerm state.Raft
    let changes = [| NodeAdded node |]
    let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)
    let response = receiveEntry entry |> runRaft state.Raft cbs

    match response with
      | Right(resp, raft) ->
        do! writeTVar appState (updateRaft raft state)

      | Middle(_, raft) ->
        do! writeTVar appState (updateRaft raft state)

      | Left(err, raft) ->
        do! writeTVar appState (updateRaft raft state)
  }

/// ## Remove a node from the Raft cluster
///
/// Safely remove a node from the Raft cluster. This operation also follows the 2-phase commit
/// model set out by the Raft paper.
///
/// ### Signature:
/// - ndoe: the node to remove from the current configuration
/// - appState: AppState TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let removeNodeR node appState cbs =
  stm {
    let! state = readTVar appState

    let term = currentTerm state.Raft
    let changes = [| NodeRemoved node |]
    let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)
    do! receiveEntry entry
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
  }

/// ## Redirect to leader
///
/// Gets the current leader node from the Raft state and returns a corresponding RaftResponse.
///
/// ### Signature:
/// - state: AppState
///
/// Returns: Stm<RaftResponse>
let redirectR state =
  stm {
    match getLeader state.Raft with
    | Some node -> return Redirect node
    | _         -> return ErrorResponse (OtherError "No known leader")
  }

/// ## Handle AppendEntries requests
///
/// Handler for AppendEntries requests. Returns an appropriate response value.
///
/// ### Signature:
/// - sender:   Raft node which sent the request
/// - ae:       AppendEntries request value
/// - appState: AppState TVar
/// - cbs:      Raft callbacks
///
/// Returns: Stm<RaftResponse>
let handleAppendEntries sender ae appState cbs =
  stm {
    let! state = readTVar appState

    let result =
      receiveAppendEntries (Some sender) ae
      |> runRaft state.Raft cbs

    match result with
      | Right (resp, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return AppendEntriesResponse(raft.Node.Id, resp)

      | Middle (resp, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return AppendEntriesResponse(raft.Node.Id, resp)

      | Left (err, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return ErrorResponse err
  }

/// ## Handle the AppendEntries request response.
///
/// Handle the request entries response.
///
/// ### Signature:
/// - sender: Node who replied
/// - ar: AppendResponse to process
/// - appState: TVar<AppState>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleAppendResponse sender ar appState cbs =
  stm {
    let! state = readTVar appState

    do! receiveAppendEntriesResponse sender ar
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
  }

/// ## Handle a vote request.
///
/// Handle a vote request and return a response.
///
/// ### Signature:
/// - sender: Node which sent request
/// - req: the `VoteRequest`
/// - appState: current TVar<AppState>
/// - cbs: IRaftCallbacks
///
/// Returns: unit
let handleVoteRequest sender req appState cbs =
  stm {
    let! state = readTVar appState

    let result =
      Raft.receiveVoteRequest sender req
      |> runRaft state.Raft cbs

    match result with
      | Right  (resp, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return RequestVoteResponse(raft.Node.Id, resp)

      | Middle (resp, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return RequestVoteResponse(raft.Node.Id, resp)

      | Left (err, raft) ->
        do! writeTVar appState (updateRaft raft state)
        return ErrorResponse err
  }

/// ## Handle the response to a vote request.
///
/// Handle the response to a vote request.
///
/// ### Signature:
/// - sender: Node which sent the response
/// - resp: VoteResponse to process
/// - appState: current TVar<AppState>
///
/// Returns: unit
let handleVoteResponse sender rep appState cbs =
  stm {
    let! state = readTVar appState

    do! receiveVoteResponse sender rep
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
  }

/// ## Handle a HandShake request by a certain Node.
///
/// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
/// leader if we are currently not Leader.
///
/// ### Signature:
/// - node: Node which wants to join the cluster
/// - appState: current TVar<AppState>
/// - cbs: IRaftCallbacks
///
/// Returns: RaftResponse
let handleHandshake node appState cbs =
  stm {
    let! state = readTVar appState
    if isLeader state.Raft then
      do! addNodeR node appState cbs
      return Welcome
    else
      return! redirectR state
  }

let handleHandwaive node appState cbs =
  stm {
    let! state = readTVar appState
    if isLeader state.Raft then
      do! removeNodeR node appState cbs
      return Arrivederci
    else
      return! redirectR state
  }

let appendEntry entry appState cbs =
  stm {
    let! state = readTVar appState

    let result =
      raft {
        let! result = receiveEntry entry
        // do! periodic 1001UL
        return result
      }
      |> runRaft state.Raft cbs

    let (response, newstate) =
      match result with
        | Right  (response, newstate) -> (Some response, newstate)
        | Middle (_, newstate)        -> (None, newstate)
        | Left   (err, newstate)      -> (None, newstate)

    do! writeTVar appState (updateRaft newstate state)

    return response
  }

let handleInstallSnapshot node snapshot appState cbs =
  stm {
    let! state = readTVar appState
    // do! createSnapshot ()
    //     |> evalRaft raft' cbs
    //     |> writeTVar raftState
    return InstallSnapshotResponse (state.Raft.Node.Id, { Term = state.Raft.CurrentTerm })
  }

let handleRequest msg appState cbs =
  stm {
    match msg with
      | RequestVote (sender, req) ->
        return! handleVoteRequest sender req appState cbs

      | AppendEntries (sender, ae) ->
        return! handleAppendEntries  sender ae appState cbs

      | HandShake node ->
        return! handleHandshake node appState cbs

      | HandWaive node ->
        return! handleHandwaive node appState cbs

      | InstallSnapshot (sender, snapshot) ->
        return! handleInstallSnapshot sender snapshot appState cbs
  }

let handleResponse msg appState cbs =
  stm {
    match msg with
      | RequestVoteResponse (sender, rep)  ->
        do! handleVoteResponse sender rep appState cbs

      | AppendEntriesResponse (sender, ar) ->
        do! handleAppendResponse sender ar appState cbs

      | InstallSnapshotResponse (sender, snapshot) ->
        printfn "[InstallSnapshotResponse RPC] done"

      | Redirect node ->
        failwithf "[HandShake] redirected us to %A" node

      | Welcome ->
        failwith "[HandShake] welcome to the fold"

      | Arrivederci ->
        failwith "[HandShake] bye bye "

      | ErrorResponse err ->
        failwithf "[ERROR] %A" err
  }

let startServer appState cbs =
  stm {
    let token = new CancellationTokenSource()

    let! state = readTVar appState
    let server = Context.rep state.Context
    let uri = state.Raft.Node.Data |> formatUri

    Socket.bind server uri

    let rec proc () =
      async {
        try
          let msg = new Message()
          Message.recv msg server

          let request : RaftRequest option =
            Message.data msg |> decode

          let response =
            match request with
            | Some message -> handleRequest message appState cbs |> atomically
            | None         -> ErrorResponse <| OtherError "Unable to decipher request"

          response |> encode |> Socket.send server

          dispose msg

          if token.IsCancellationRequested then
            Socket.unbind server uri
            dispose server
          else
            return! proc ()
        with
          | exn ->
            Socket.unbind server uri
            dispose server
      }

    Async.Start(proc (), token.Token)

    return token
  }

/// ## startPeriodic
///
/// Starts an asynchronous loop to run Raft's `periodic` function. Returns a token, with which the
/// loop can be cancelled at a later time.
///
/// ### Signature:
/// - timeoput: interval at which the loop runs
/// - appState: current AppState TVar
/// - cbs: Raft Callbacks
///
/// Returns: CancellationTokenSource
let startPeriodic timeout appState cbs =
  stm {
    let token = new CancellationTokenSource()

    let rec proc () =
      async {
          Thread.Sleep(int timeout)                   // sleep for 100ms
          periodicR timeout appState cbs |> atomically // kick the machine
          return! proc ()                             // recurse
        }

    Async.Start(proc(), token.Token)

    return token                      // return the cancellation token source so this loop can be
  }                                   // stopped at a  later time


// -------------------------------------------------------------------------
let tryJoin (leader: Node<IrisNode>) appState =
  let rec _tryJoin retry node' =
    stm {
      let! state = readTVar appState

      if retry < int state.Options.MaxRetries then
        printfn "Trying to join cluster. [retry: %d] [node: %A]" retry node'

        let msg = HandShake(state.Raft.Node)
        let! result = performRequest msg node' appState

        match result with
          | Some message ->
            match message with
              | Welcome ->
                printfn "HandShake successful. Waiting to be updated"

              | Redirect next ->
                do! _tryJoin (retry + 1) next

              | ErrorResponse err ->
                printfn "Unexpected error occurred. %A" err
                exit 1

              | res ->
                printfn "Unexpected response. Aborting.\n%A" res
                exit 1
          | _ ->
            printfn "Node: %A unreachable. Aborting." node'.Id
            exit 1
      else
        printfn "Too many connection attempts unsuccesful. Aborting."
        exit 1
    }

  printfn "joining leader %A now" leader
  _tryJoin 0 leader

/// ## Attempt to leave a Raft cluster
///
/// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
/// AppendEntries request with a JointConsensus entry.
///
/// ### Signature:
/// - appState: AppState TVar
/// - cbs: Raft callbacks
///
/// Returns: unit
let tryLeave appState cbs =
  let rec _tryLeave retry (node: Node<IrisNode>) =
    stm {
      printfn "Trying to join cluster. [retry: %A] [node: %A]" retry node
      let! state = readTVar appState
      let msg = HandWaive(state.Raft.Node)
      let! result = performRequest msg node appState

      match result with
        | Some message ->
          match message with
            | Redirect other ->
              if retry <= int state.Options.MaxRetries then
                do! _tryLeave (retry + 1) other
              else
                failwith "too many retries. aborting"

            | Arrivederci ->
              printfn "HandWaive successful."

            | ErrorResponse err ->
              printfn "Unexpected error occurred. %A" err
              exit 1

            | resp ->
              printfn "Unexpected response. Aborting.\n%A" resp
              exit 1
        | _ ->
          printfn "Node unreachable. Aborting."
          exit 1
    }

  stm {
    let! state = readTVar appState

    if not (isLeader state.Raft) then
      match Option.bind (flip getNode state.Raft) state.Raft.CurrentLeader with
        | Some node ->
          do! _tryLeave 0 node
        | _ ->
          printfn "Leader not found. Exiting without saying goodbye."
    else
      let term = currentTerm state.Raft
      let changes = [| NodeRemoved state.Raft.Node |]
      let entry = JointConsensus(RaftId.Create(), 0UL, term , changes, None)

      let! response = appendEntry entry appState cbs

      failwith "FIXME: must now block to await the committed state for response"
  }

/// ## requestLoop
///
/// Request loop.
///
/// ### Signature:
/// - inbox: MailboxProcessor
///
/// Returns: Async<(Node<IrisNode> * RaftRequest)>
let rec requestLoop appState cbs (inbox: Actor<(Node<IrisNode> * RaftRequest)>) =
  async {
    // block until there is a new message in my inbox
    let! (node, msg) = inbox.Receive()

    stm {
      let! response = performRequest msg node appState

      match response with
        | Some message ->
          do! handleResponse message appState cbs
        | _ ->
          printfn "[REQUEST TIMEOUT]: must mark node as failed now and fire a callback"
    } |> atomically

    return! requestLoop appState cbs inbox
  }

let forceElection appState cbs =
  stm {
    let! state = readTVar appState

    do! raft {
          let! timeout = electionTimeoutM ()
          do! setTimeoutElapsedM timeout
          do! periodic timeout
        }
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
  }

let prepareSnapshot appState =
  stm {
    let! state = readTVar appState
    let snapshot = createSnapshot (DataSnapshot "snip snap snapshot") state.Raft
    return snapshot
  }

let initialize appState cbs =
  stm {
    let! state = readTVar appState

    let term = 0UL                    // this likely needs to be adjusted when
                                      // loading state from disk

    let changes = [| NodeAdded state.Raft.Node |]
    let nodes =  [||]
    let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)

    let newstate =
      raft {
        do! setTermM term
        do! setRequestTimeoutM 500UL
        do! setElectionTimeoutM 1000UL

        if state.Options.Start then
          let! result = appendEntryM entry
          do! becomeLeader ()
          do! periodic 1001UL
        else
          let leader =
            { MemberId = createGuid()
            ; HostName = "<empty>"
            ; IpAddr = Option.get state.Options.LeaderIp   |> IpAddress.Parse
            ; Port   = Option.get state.Options.LeaderPort |> int
            ; TaskId = None
            ; Status = IrisNodeStatus.Running
            }
            |> Node.create (Option.get state.Options.LeaderId |> RaftId)
          failwith "FIXME: call tryJoin now"
      }
      |> evalRaft state.Raft cbs

    // tryJoin leader
    do! writeTVar appState (updateRaft newstate state)
  }
