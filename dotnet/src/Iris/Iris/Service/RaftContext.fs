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
module AppContext =

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

  let mkState (options: RaftOptions) : AppState =
    { Clients  = []
    ; Sessions = []
    ; Projects = Map.empty
    ; Peers    = Map.empty
    ; Raft     = mkRaft options
    }

  /////////////////////////////////////////////////////////////
  //     _                 ____            _            _    //
  //    / \   _ __  _ __  / ___|___  _ __ | |_ _____  _| |_  //
  //   / _ \ | '_ \| '_ \| |   / _ \| '_ \| __/ _ \ \/ / __| //
  //  / ___ \| |_) | |_) | |__| (_) | | | | ||  __/>  <| |_  //
  // /_/   \_\ .__/| .__/ \____\___/|_| |_|\__\___/_/\_\\__| //
  //         |_|   |_|                                       //
  /////////////////////////////////////////////////////////////

  type AppContext(opts: RaftOptions) as this =
    let max_retry = 5
    let zmqContext = new fszmq.Context()

    let mutable options = opts
    let mutable updateCount = 0UL

    let appState = mkState opts |> newTVar

    let cbs = this :> IRaftCallbacks<_,_>

    // -------------------------------------------------------------------------
    let withConnection timeout (node: Node<IrisNode>) (thing: RaftMsg) =
      use client = req zmqContext
      let addr = formatUri node.Data

      connect client addr
      thing |> encode |>> client

      let mutable frames = [| |]

      let handler _ (msgs : Message array) =
        frames <- Array.map (Message.data >> decode<RaftMsg>) msgs

      let result =
        [ pollIn handler client ]
        |> poll (int64 timeout)

      if result && Array.length frames > 0 then
        frames.[0]                      // for now we only process single-ZFrame messages in Raft
      else None

    // --------------------------------------------------------------------------------------------

    /// ## requestLoop
    ///
    /// Request loop.
    ///
    /// ### Signature:
    /// - inbox: MailboxProcessor
    ///
    /// Returns: Async<(Node<IrisNode> * RaftMsg)>
    let rec requestLoop (inbox: Actor<(Node<IrisNode> * RaftMsg)>) =
      async {
        let! (node, msg) = inbox.Receive()
        stm {
          let! state = readTVar appState

          let results = withConnection state.Raft.RequestTimeout node msg

          match results with
            | Some message ->
              match message with
                | AppendEntriesResponse (nid,resp) ->
                  do! receiveAppendEntriesResponse nid resp
                      |> evalRaft state.Raft cbs
                      |> flip updateRaft state
                      |> writeTVar appState

                | RequestVoteResponse(nid,vote) ->
                  do! receiveVoteResponse nid vote
                      |> evalRaft state.Raft cbs
                      |> flip updateRaft state
                      |> writeTVar appState

                | ErrorResponse err ->
                  sprintf "ERROR: %A" err |> this.Log

                | resp ->
                  sprintf "UNEXPECTED RESPONSE: %A" resp
                  |> this.Log

            | _ ->
              this.Log "[REQUEST TIMEOUT]: must mark node as failed now and fire a callback"
        } |> atomically
        return! requestLoop inbox
      }

    let requestWorker = new Actor<(Node<IrisNode> * RaftMsg)> (requestLoop)

    // -------------------------------------------------------------------------
    let tryJoin (leader: Node<IrisNode>) =
      let state = readTVar appState |> atomically

      let rec _tryJoin retry node' =
        if retry < max_retry then
          sprintf "Trying to join cluster. [retry: %d] [node: %A]" retry node'
          |> this.Log

          let msg = HandShake(state.Raft.Node)
          let result = withConnection state.Raft.RequestTimeout node' msg

          match result with
            | Some message ->
              match message with
                | Welcome ->
                  this.Log "HandShake successful. Waiting to be updated"

                | Redirect next ->
                  _tryJoin (retry + 1) next

                | ErrorResponse err ->
                  sprintf "Unexpected error occurred. %A" err |> this.Log
                  exit 1

                | res ->
                  sprintf "Unexpected response. Aborting.\n%A" res |> this.Log
                  exit 1
            | _ ->
              this.Log "Node unreachable. Aborting."
              exit 1
        else
          sprintf "Too many connection attempts unsuccesful. Aborting." |> this.Log
          exit 1

      this.Log <| sprintf "joining leader %A now" leader
      _tryJoin 0 leader

    // -------------------------------------------------------------------------
    let tryLeave _ =
      let state = readTVar appState |> atomically

      let rec _tryLeave retry (node: Node<IrisNode>) =
        sprintf "Trying to join cluster. [retry: %d] [node: %A]" retry node
        |> this.Log

        let msg = HandWaive(state.Raft.Node)
        let result = withConnection state.Raft.RequestTimeout node msg

        match result with
          | Some message ->
            match message with
              | Redirect other ->
                _tryLeave (retry + 1) other

              | Arrivederci ->
                this.Log "HandWaive successful."

              | ErrorResponse err ->
                sprintf "Unexpected error occurred. %A" err |> this.Log
                exit 1

              | resp ->
                sprintf "Unexpected response. Aborting.\n%A" resp |> this.Log
                exit 1
          | _ ->
            this.Log "Node unreachable. Aborting."
            exit 1

      if not (isLeader state.Raft) then
        match state.Raft.CurrentLeader with
          | Some lid ->
          match getNode lid state.Raft with
            | Some node -> _tryLeave max_retry node
            | _ ->
              this.Log "Leader not found. Exiting without saying goodbye."
          | _ ->
            this.Log "Leader not found. Exiting without saying goodbye."
      else
        let term = currentTerm state.Raft
        let changes = [| NodeRemoved state.Raft.Node |]
        let nodes =  [||]
        let entry = JointConsensus(RaftId.Create(), 0UL, term , changes, None)
        receiveEntry entry
        |> evalRaft state.Raft cbs
        |> flip updateRaft state
        |> writeTVar appState
        |> atomically
        Thread.Sleep(3000)

    // -------------------------------------------------------------------------
    let initialise _ =
      stm {
        let! state = readTVar appState

        let term = 0UL // this likely needs to be adjusted when
                       // loading state from disk

        let changes = [| NodeAdded state.Raft.Node |]
        let nodes =  [||]
        let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)

        let newstate =
          raft {
            do! setTermM term
            do! setRequestTimeoutM 500UL
            do! setElectionTimeoutM 1000UL

            if options.Start then
              let! result = appendEntryM entry
              do! becomeLeader ()
              do! periodic 1001UL
            else
              let leader =
                { MemberId = createGuid()
                ; HostName = "<empty>"
                ; IpAddr = Option.get options.LeaderIp   |> IpAddress.Parse
                ; Port   = Option.get options.LeaderPort |> int
                ; TaskId = None
                ; Status = IrisNodeStatus.Running
                }
                |> Node.create (Option.get options.LeaderId |> RaftId)
              tryJoin leader
          }
          |> evalRaft state.Raft cbs

        do! writeTVar appState (updateRaft newstate state)
      } |> atomically

    ////////////////////////////////////
    //  _       _ _                   //
    // (_)_ __ (_) |_                 //
    // | | '_ \| | __|                //
    // | | | | | | |_                 //
    // |_|_| |_|_|\__| the machine.   //
    ////////////////////////////////////

    do requestWorker.Start()

    ////////////////////////////////////////////////////
    //                           _                    //
    //  _ __ ___   ___ _ __ ___ | |__   ___ _ __ ___  //
    // | '_ ` _ \ / _ \ '_ ` _ \| '_ \ / _ \ '__/ __| //
    // | | | | | |  __/ | | | | | |_) |  __/ |  \__ \ //
    // |_| |_| |_|\___|_| |_| |_|_.__/ \___|_|  |___/ //
    ////////////////////////////////////////////////////

    member self.Start() = initialise ()

    member self.Stop() =
      tryLeave ()

    member self.Options
      with get () = options
      and  set o  = options <- o

    member self.Context
      with get () = zmqContext

    /// We can *LOOK* at the current state, but *HAVE* to use the Funnel to set
    /// it. This gives us the benefit of linearizabilty of transactions.
    member self.State
      with get () = readTVar appState |> atomically

    member self.Log msg =
      let state = self.State
      cbs.LogMsg state.Raft.Node msg

    member self.Append entry =
      stm {
        let! state = readTVar appState
        do! raft {
              let! result = receiveEntry entry
              do! periodic 1001UL
              return result
            }
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      } |> atomically

    member self.Timeout _ =
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
      } |> atomically

    member self.ReceiveVoteRequest sender req =
      stm {
        let! state = readTVar appState

        let result =
          receiveVoteRequest sender req
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

      } |> atomically

    member self.ReceiveVoteResponse sender rep =
      stm {
        let! state = readTVar appState

        do! receiveVoteResponse sender rep
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState

        return EmptyResponse
      } |> atomically


    member self.ReceiveAppendEntries sender ae =
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

      } |> atomically

    member self.ReceiveAppendResponse sender ar =
      stm {
        let! state = readTVar appState

        do! receiveAppendEntriesResponse sender ar
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState

        return EmptyResponse
      } |> atomically


    member self.AddNode node =
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

      } |> atomically

    member self.RemoveNode node =
      stm {
        let! state = readTVar appState

        let term = currentTerm state.Raft
        let changes = [| NodeRemoved node |]
        let entry = JointConsensus(RaftId.Create(), 0UL, term, changes, None)
        do! receiveEntry entry
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState
      } |> atomically

    member self.InstallSnapshot node snapshot =
      stm {
        let! state = readTVar appState
        // do! createSnapshot ()
        //     |> evalRaft raft' cbs
        //     |> writeTVar raftState
        return InstallSnapshotResponse (state.Raft.Node.Id, { Term = state.Raft.CurrentTerm })
      } |> atomically

    member self.Periodic elapsed =
      stm {
        let! state = readTVar appState

        do! periodic elapsed
            |> evalRaft state.Raft cbs
            |> flip updateRaft state
            |> writeTVar appState

      } |> atomically

    //  ____  _                           _     _
    // |  _ \(_)___ _ __   ___  ___  __ _| |__ | | ___
    // | | | | / __| '_ \ / _ \/ __|/ _` | '_ \| |/ _ \
    // | |_| | \__ \ |_) | (_) \__ \ (_| | |_) | |  __/
    // |____/|_|___/ .__/ \___/|___/\__,_|_.__/|_|\___|
    //             |_|

    interface IDisposable with
      member self.Dispose() =
        dispose zmqContext

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

      member self.PrepareSnapshot raft =
        stm {
          let! state = readTVar appState
          let snapshot = createSnapshot (DataSnapshot "snip snap snapshot") state.Raft
          return snapshot
        } |> atomically

      member self.PersistSnapshot log =
        printfn "now save the goddamn thing to disk m8"

      member self.RetrieveSnapshot () =
        stm {
          // nothign to retrieve yet
          return None
        } |> atomically

      member self.ApplyLog sm =
        stm {
          // match log with
          //   |
          return ()
        } |> atomically

      member self.NodeAdded node = printfn "Node was added."
      member self.NodeUpdated node = printfn "Node was updated."
      member self.NodeRemoved node = printfn "Node was removed."
      member self.Configured nodes = printfn "Cluster configuration done."
      member self.StateChanged o n = printfn "State changed from %A to %A" o n

      member self.PersistVote node = printfn "PersistVote"
      member self.PersistTerm node = printfn "PersistTerm"
      member self.PersistLog log = printfn "LogOffer"
      member self.DeleteLog log = printfn "LogPoll"
      member self.HasSufficientLogs node = printfn "Se"

      member self.LogMsg node str =
        if options.Debug then
          printfn "%s" str
