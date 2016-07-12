namespace Iris.Service

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

[<AutoOpen>]
module RaftServer =

  let getHostName () =
    System.Net.Dns.GetHostName()

  let formatUri (data: IrisNode) =
    sprintf "tcp://%s:%d" (string data.IpAddr) data.Port

  let mkRaft (options: RaftOptions) =
    let node =
      { MemberId = createGuid()
      ; HostName = getHostName()
      ; IpAddr   = IpAddress.Parse options.IpAddr
      ; Port     = options.RaftPort
      ; TaskId   = None
      ; Status   = IrisNodeStatus.Running }
      |> Node.create (RaftId options.RaftId)
    Raft.create node

  let mkState (context: Context) (options: RaftOptions) : AppState =
    { Clients     = []
    ; Sessions    = []
    ; Projects    = Map.empty
    ; Peers       = Map.empty
    ; Connections = Map.empty
    ; Raft        = mkRaft options
    ; Context     = context
    ; Options     = options
    }

  let cancelToken (cts: CancellationTokenSource option ref) =
    match !cts with
    | Some token ->
      token.Cancel()
      cts := None
    | _ -> ()

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
        let sock = req state.Context
        let addr = formatUri node.Data

        connect sock addr

        let newstate =
          { state with
              Connections = Map.add node.Data.MemberId sock state.Connections }

        do! writeTVar appState newstate

        return sock
    }

  /// ## Send RaftMsg to node
  ///
  /// Sends given RaftMsg to node. If the request times out, None is return to indicate
  /// failure. Otherwise the de-serialized RaftMsg response is returned, wrapped in option to
  /// indicate whether de-serialization was successful.
  ///
  /// ### Signature:
  /// - thing:    RaftMsg to send
  /// - node:     node to send the message to
  /// - appState: application state TVar
  ///
  /// Returns: RaftMsg option
  let performRequest (thing: RaftRequest) (node: Node<IrisNode>) appState =
    stm {
      let mutable frames = [| |]

      let handler _ (msgs : Message array) =
        frames <- Array.map (Message.data >> decode<RaftResponse>) msgs

      let! state = readTVar appState
      let! client = getSocket node appState

      thing |> encode |>> client

      let result =
        [ pollIn handler client ]
        |> poll (int64 state.Raft.RequestTimeout)

      if result && Array.isEmpty frames |> not then
        return frames.[0]               // for now we only process single-ZFrame messages in Raft
      else
        return None
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
  /// Gets the current leader node from the Raft state and returns a corresponding RaftMsg response.
  ///
  /// ### Signature:
  /// - state: AppState
  ///
  /// Returns: Stm<RaftMsg>
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
  /// Returns: Stm<RaftMsg>
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

  let handleAppendResponse sender ar appState cbs =
    stm {
      let! state = readTVar appState

      do! receiveAppendEntriesResponse sender ar
          |> evalRaft state.Raft cbs
          |> flip updateRaft state
          |> writeTVar appState
    }

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

  let handleVoteResponse sender rep appState cbs =
    stm {
      let! state = readTVar appState

      do! receiveVoteResponse sender rep
          |> evalRaft state.Raft cbs
          |> flip updateRaft state
          |> writeTVar appState
    }

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
      let server = rep state.Context
      let uri = state.Raft.Node.Data |> formatUri

      bind server uri

      let rec proc () =
        async {
          let msg : RaftRequest option =
            recv server |> decode

          let response =
            match msg with
            | Some message ->
              handleRequest message appState cbs
              |> atomically
            | None ->
              ErrorResponse <| OtherError "Unable to decipher request"

          response |> encode |> send server

          if token.IsCancellationRequested then
            unbind server uri
            dispose server
          else
            return! proc ()
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
  /// Returns: Async<(Node<IrisNode> * RaftMsg)>
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

  //  ____        __ _     ____
  // |  _ \ __ _ / _| |_  / ___|  ___ _ ____   _____ _ __
  // | |_) / _` | |_| __| \___ \ / _ \ '__\ \ / / _ \ '__|
  // |  _ < (_| |  _| |_   ___) |  __/ |   \ V /  __/ |
  // |_| \_\__,_|_|  \__| |____/ \___|_|    \_/ \___|_|

  type RaftServer(options: RaftOptions, context: fszmq.Context) as this =
    let max_retry = 5
    let timeout = 10UL

    let servertoken   = ref None
    let workertoken   = ref None
    let periodictoken = ref None

    let cbs = this :> IRaftCallbacks<_,_>
    let appState = mkState context options |> newTVar

    let requestWorker =
      let cts = new CancellationTokenSource()
      workertoken := Some cts
      new Actor<(Node<IrisNode> * RaftRequest)> (requestLoop appState cbs, cts.Token)

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
      stm {
        requestWorker.Start()

        do!  initialize appState cbs
        let! srvtkn = startServer appState cbs
        let! prdtkn = startPeriodic timeout appState cbs

        servertoken   := Some srvtkn
        periodictoken := Some prdtkn
      } |> atomically

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
      stm {
        // cancel the running async tasks
        cancelToken periodictoken
        cancelToken servertoken
        cancelToken workertoken

        let! state = readTVar appState

        // disconnect all cached sockets
        state.Connections
        |> Map.iter (fun (mid: MemberId) (sock: Socket) ->
                    let nodeinfo = List.tryFind (fun client -> client.MemberId = mid) state.Clients
                    match nodeinfo with
                      | Some info -> formatUri info |> disconnect sock
                      | _         -> ())


        do! writeTVar appState { state with Connections = Map.empty }


        failwith "FIXME: STOP SHOULD ALSO PERSIST LAST STATE TO DISK"
      } |> atomically

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
      failwith "FIXME: ForceTimeout"

    member self.Log msg =
      let state = self.State
      cbs.LogMsg state.Raft.Node msg

    //  ____  _                           _     _
    // |  _ \(_)___ _ __   ___  ___  __ _| |__ | | ___
    // | | | | / __| '_ \ / _ \/ __|/ _` | '_ \| |/ _ \
    // | |_| | \__ \ |_) | (_) \__ \ (_| | |_) | |  __/
    // |____/|_|___/ .__/ \___/|___/\__,_|_.__/|_|\___|
    //             |_|

    interface IDisposable with
      member self.Dispose() = failwith "FIXME: DISPOSE"


    //  ____        __ _     ___       _             __
    // |  _ \ __ _ / _| |_  |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___
    // | |_) / _` | |_| __|  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \
    // |  _ < (_| |  _| |_   | || | | | ||  __/ |  |  _| (_| | (_|  __/
    // |_| \_\__,_|_|  \__| |___|_| |_|\__\___|_|  |_|  \__,_|\___\___|

    interface IRaftCallbacks<StateMachine,IrisNode> with
      member self.SendRequestVote node req  =
        let state = self.State
        (node, RequestVote(state.Raft.Node.Id,req))
        |> requestWorker.Post

      member self.SendAppendEntries node ae =
        let state = self.State
        (node, AppendEntries(state.Raft.Node.Id, ae))
        |> requestWorker.Post

      member self.SendInstallSnapshot node is =
        let state = self.State
        (node, InstallSnapshot(state.Raft.Node.Id, is))
        |> requestWorker.Post

      member self.PrepareSnapshot raft = failwith "FIXME: PrepareSnapshot"
      member self.PersistSnapshot log = failwith "FIXME: PersistSnapshot"
      member self.RetrieveSnapshot () = failwith "FIXME: RetrieveSnapshot"
      member self.ApplyLog sm      = failwith "FIXME: ApplyLog"
      member self.NodeAdded node   = failwith "FIXME: Node was added."
      member self.NodeUpdated node = failwith "FIXME: Node was updated."
      member self.NodeRemoved node = failwith "FIXME: Node was removed."
      member self.Configured nodes = failwith "FIXME: Cluster configuration done."
      member self.StateChanged o n = failwithf "FIXME: State changed from %A to %A" o n

      member self.PersistVote node = failwith "FIXME: PersistVote"
      member self.PersistTerm node = failwith "FIXME: PersistTerm"
      member self.PersistLog log   = failwith "FIXME: LogOffer"
      member self.DeleteLog log    = failwith "FIXME: LogPoll"
      member self.HasSufficientLogs node = failwith "FIXME: HasSufficientLogs"

      member self.LogMsg node str =
        if options.Debug then
          printfn "%s" str
