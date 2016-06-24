namespace Iris.Service

open System
open System.Net
open System.Threading
open Iris.Core
open Pallet.Core
open fszmq
open fszmq.Context
open fszmq.Socket
open fszmq.Polling
open FSharpx.Stm

[<AutoOpen>]
module AppContext =

  let parseIp str =
    try IPAddress.Parse str
    with
      | ex ->
        printfn "Error parsing IP address: %s" ex.Message
        exit 1

  let getHostName () =
    System.Net.Dns.GetHostName()

  let formatUri (data: IrisNode) =
    sprintf "tcp://%A:%d" data.IpAddr data.Port

  let mkRaft (options: RaftOptions) =
    let node =
      { HostName = getHostName()
      ; IpAddr   = parseIp options.IpAddr
      ; Port     = options.RaftPort }
      |> Node.create options.RaftId
    Raft.create node

  let decode _ = failwith "FIXME"
  let encode _ = failwith "FIXME"

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
    let context = new Context()

    let mutable options = opts
    let mutable updateCount = 0UL

    let AppState = newTVar 0
    let RaftState = mkRaft opts |> newTVar

    // -------------------------------------------------------------------------
    let withConnection timeout (node: Node<IrisNode>) (thing: RaftMsg) =
      use client = req context
      let addr = formatUri node.Data
      connect client addr
      thing |> encode |>> client

      let mutable bytes : byte array = Array.empty

      let handler _ (msg : Message array) =
        // bytes <- msg.Unwrap().Read()
        failwith "UH OH"

      let result =
        [ pollIn handler client ]
        |> poll (int64 timeout)

      if result then
        Some bytes
      else None

    // -------------------------------------------------------------------------
    let requestWorker =
      new Actor<(Node<IrisNode> * RaftMsg)>
        (fun inbox ->
          let rec loop () =
            async {
              let! (node, msg) = inbox.Receive()

              stm {
                let result : RaftMsg option =
                  withConnection (this.State.RequestTimeout) node msg
                  |> Option.map decode

                let! raft' = readTVar RaftState

                match result with
                  | Some response ->
                    match response with
                      | AppendEntriesResponse(nid,resp) ->
                        do! receiveAppendEntriesResponse nid resp
                            |> evalRaft raft' (this :> IRaftCallbacks<_,_>)
                            |> writeTVar RaftState
                      | RequestVoteResponse(nid,vote) ->
                        do! receiveVoteResponse nid vote
                            |> evalRaft raft' (this :> IRaftCallbacks<_,_>)
                            |> writeTVar RaftState
                      | ErrorResponse err ->
                        sprintf "ERROR: %A" err
                        |> this.Log
                      | _ as resp ->
                        sprintf "UNEXPECTED RESPONSE: %A" resp
                        |> this.Log
                  | None ->
                    printfn "must mark node as failed now and possibly fire a callback"
                    this.Log "Request timeout! Marking s Failed"
              } |> atomically

              return! loop ()
            }
          loop ())

    // -------------------------------------------------------------------------
    let tryJoin (leader: Node<IrisNode>) =
      let raft' = readTVar RaftState |> atomically

      let rec _tryJoin retry node' =
        if retry < max_retry then
          sprintf "Trying to join cluster. [retry: %d] [node: %A]" retry node'
          |> this.Log

          let msg = HandShake(raft'.Node)
          let result : RaftMsg option =
            withConnection 2000u node' msg
            |> Option.map decode

          match result with
            | Some response ->
              match response with
                // | MembershipResponse (Redirect, Some other) -> _tryJoin (retry + 1) other
                // | MembershipResponse (Redirect, None) ->
                //   this.Log "Node reachable but could not redirect to Leader. Aborting."
                //   exit 1
                // | MembershipResponse (Hello, _) ->
                //   this.Log "HandShake successful. Waiting to be updated"
                | ErrorResponse err ->
                  sprintf "Unexpected error occurred. %A" err |> this.Log
                  exit 1
                | _ as resp ->
                  sprintf "Unexpected response. Aborting.\n%A" resp |> this.Log
                  exit 1
            | _ ->
              this.Log "Node unreachable. Aborting."
              exit 1
        else
          sprintf "Connection attempts unsuccesful. Aborting." |> this.Log
          exit 1

      printfn "joining leader %A now" leader
      _tryJoin 0 leader

    // -------------------------------------------------------------------------
    let tryLeave _ =
      let raft' = readTVar RaftState |> atomically

      let rec _tryLeave retry (node: Node<IrisNode>) =
        sprintf "Trying to join cluster. [retry: %d] [node: %A]" retry node
        |> this.Log

        let msg = HandWaive(raft'.Node)
        let result : RaftMsg option =
          withConnection 2000u node msg
          |> Option.map decode

        match result with
          | Some response ->
            match response with
              // | MembershipResponse (Redirect, Some other) -> _tryLeave (retry + 1) other
              // | MembershipResponse (Redirect, None) ->
              //   this.Log "Node reachable but could not redirect to Leader. Aborting."
              //   exit 1
              // | MembershipResponse (ByeBye, _) ->
              //   this.Log "HandWaive successful."
              | ErrorResponse err ->
                sprintf "Unexpected error occurred. %A" err |> this.Log
                exit 1
              | _ as resp ->
                sprintf "Unexpected response. Aborting.\n%A" resp |> this.Log
                exit 1
          | _ ->
            this.Log "Node unreachable. Aborting."
            exit 1

      if not <| isLeader raft' then
        match raft'.CurrentLeader with
          | Some lid ->
          match getNode lid raft' with
            | Some node -> _tryLeave max_retry node
            | _ ->
              this.Log "Leader not found. Exiting without saying goodbye."
          | _ ->
            this.Log "Leader not found. Exiting without saying goodbye."
      else
        let term = currentTerm raft'
        let entry = JointConsensus(Guid.NewGuid(),0u,term ,[| NodeRemoved raft'.Node |], Array.empty,None)
        receiveEntry entry
        |> evalRaft raft' (this :> IRaftCallbacks<_,_>)
        |> writeTVar RaftState
        |> atomically
        Thread.Sleep(3000)

    // -------------------------------------------------------------------------
    let initialise _ =
      stm {
        let! s = readTVar RaftState

        let term = 0u // this likely needs to be adjusted when
                      // loading state from disk

        let entry = JointConsensus(Guid.NewGuid(),0u,term,[| NodeAdded s.Node |], Array.empty,None)

        let s' =
          raft {
            do! setTermM term
            do! setRequestTimeoutM 500u
            do! setElectionTimeoutM 1000u

            if options.Start then
              let! result = appendEntryM entry
              do! becomeLeader ()
              do! periodic 1001u
            else
              let leader =
                { HostName = "<empty>"
                ; IpAddr = Option.get options.LeaderIp |> parseIp
                ; Port   = Option.get options.LeaderPort |> int
                }
                |> Node.create (Option.get options.LeaderId)
              tryJoin leader
            }
          |> evalRaft s (this :> IRaftCallbacks<_,_>)

        do! writeTVar RaftState s'
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
      with get () = context

    /// We can *LOOK* at the current state, but *HAVE* to use the Funnel to set
    /// it. This gives us the benefit of linearizabilty of transactions.
    member self.State
      with get () = readTVar RaftState |> atomically

    member self.Log msg =
      let state = self.State
      (self :> IRaftCallbacks<StateMachine,IrisNode>).LogMsg state.Node msg

    member self.Append entry =
      stm {
        let! raft' = readTVar RaftState
        do! raft {
              let! result = receiveEntry entry
              do! periodic 1001u
              return result
            }
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState
      } |> atomically

    member self.Timeout _ =
      stm {
        let! raft' = readTVar RaftState

        do! raft {
              let! timeout = electionTimeoutM ()
              do! setTimeoutElapsedM timeout
              do! periodic timeout
            }
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState
      } |> atomically

    member self.ReceiveVoteRequest sender req =
      stm {
        let! raft' = readTVar RaftState

        let result =
          receiveVoteRequest sender req
          |> runRaft raft' (self :> IRaftCallbacks<_,_>)

        match result with
          | Right  (resp, state) ->
            do! writeTVar RaftState state
            return RequestVoteResponse(raft'.Node.Id, resp)

          | Middle (resp, state) ->
            do! writeTVar RaftState state
            return RequestVoteResponse(raft'.Node.Id, resp)

          | Left (err,  state) ->
            do! writeTVar RaftState state
            return ErrorResponse err

      } |> atomically

    member self.ReceiveVoteResponse sender rep =
      stm {
        let! raft' = readTVar RaftState

        do! receiveVoteResponse sender rep
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState

        return EmptyResponse
      } |> atomically


    member self.ReceiveAppendEntries sender ae =
      stm {
        let! raft' = readTVar RaftState

        let result =
          receiveAppendEntries (Some sender) ae
          |> runRaft raft' (self :> IRaftCallbacks<_,_>)

        match result with
          | Right  (resp, state) ->
            do! writeTVar RaftState state
            return AppendEntriesResponse(raft'.Node.Id, resp)

          | Middle (resp, state) ->
            do! writeTVar RaftState state
            return AppendEntriesResponse(raft'.Node.Id, resp)

          | Left (err, state) ->
            do! writeTVar RaftState state
            return ErrorResponse err

      } |> atomically

    member self.ReceiveAppendResponse sender ar =
      stm {
        let! raft' = readTVar RaftState

        do! receiveAppendEntriesResponse sender ar
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState

        return EmptyResponse
      } |> atomically


    member self.AddNode node =
      stm {
        let! raft' = readTVar RaftState
        let term = currentTerm raft'
        let entry = JointConsensus(Guid.NewGuid(),0u,term,[| NodeAdded node |],Array.empty,None)
        let response = receiveEntry entry
                       |> runRaft raft' (self :> IRaftCallbacks<_,_>)

        match response with
          | Left(err, r) ->
            printfn "error %A" err
            do! writeTVar RaftState r
          | Middle(_, r) ->
            printfn "<Middle>"
            do! writeTVar RaftState r
          | Right(resp, r) ->
            printfn "response %A" resp
            do! writeTVar RaftState r

      } |> atomically

    member self.RemoveNode node =
      stm {
        let! raft' = readTVar RaftState
        let term = currentTerm raft'
        let entry = JointConsensus(Guid.NewGuid(),0u,term,[| NodeRemoved node |],Array.empty,None)
        do! receiveEntry entry
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState
      } |> atomically

    member self.InstallSnapshot node snapshot =
      stm {
        let! raft' = readTVar RaftState
        // do! createSnapshot ()
        //     |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
        //     |> writeTVar RaftState
        return InstallSnapshotResponse (raft'.Node.Id, true)
      } |> atomically

    member self.Periodic elapsed =
      stm {
        let! raft' = readTVar RaftState

        do! periodic elapsed
            |> evalRaft raft' (self :> IRaftCallbacks<_,_>)
            |> writeTVar RaftState

      } |> atomically

    ////////////////////////////////////////////////////
    //  _       _             __                      //
    // (_)_ __ | |_ ___ _ __ / _| __ _  ___ ___  ___  //
    // | | '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \/ __| //
    // | | | | | ||  __/ |  |  _| (_| | (_|  __/\__ \ //
    // |_|_| |_|\__\___|_|  |_|  \__,_|\___\___||___/ //
    ////////////////////////////////////////////////////

    interface IDisposable with
      member self.Dispose() =
        dispose context

    interface IRaftCallbacks<StateMachine,IrisNode> with
      member self.SendRequestVote node req  =
        let state = self.State
        (node, RequestVote(state.Node.Id,req))
        |> requestWorker.Post

      member self.SendAppendEntries node ae =
        let state = self.State
        (node, AppendEntries(state.Node.Id, ae))
        |> requestWorker.Post

      member self.SendInstallSnapshot node is =
        let state = self.State
        (node, InstallSnapshot(state.Node.Id, is))
        |> requestWorker.Post

      member self.PrepareSnapshot raft =
        stm {
          let! s = readTVar AppState
          let snapshot = createSnapshot DataSnapshot raft
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
