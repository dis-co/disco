namespace Iris.Service

#if !IRIS_NODES

// * Imports

open System
open System.Threading
open System.Collections
open System.Collections.Concurrent
open Iris.Zmq
open Iris.Core
open Iris.Core.Utils
open Iris.Service.Interfaces
open Iris.Raft
open FSharpx.Functional
open Utilities
open Persistence
open Hopac
open Hopac.Infixes

// * Raft

[<AutoOpen>]
module Raft =

  //  ____       _            _
  // |  _ \ _ __(_)_   ____ _| |_ ___
  // | |_) | '__| \ \ / / _` | __/ _ \
  // |  __/| |  | |\ V / (_| | ||  __/
  // |_|   |_|  |_| \_/ \__,_|\__\___|

  // ** tag

  let private tag (str: string) = sprintf "RaftServer.%s" str

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,IClient>

  // ** Reply

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Reply =
    | Ok
    | Entry  of EntryResponse
    | State  of RaftAppContext
    | Status of ServiceStatus

  // ** ReplyChan

  type private ReplyChan = AsyncReplyChannel<Either<IrisError,Reply>>

  // type private ReplyChan = BlockingCollection<Either<IrisError,Reply>>
  //
  // IDEA:
  //
  // What if the reply channel was actually just a BlockingCollection? It could be just passed
  // around as a regular value.
  //
  // But then, we could also make it so the real blocking parts (waitForCommit) could just be
  // re-scheduled messages that copy over the chan to the internal check message also do work!
  // Hmmm..

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Load           of chan:ReplyChan * config:IrisConfig
    | Unload         of chan:ReplyChan
    | Join           of chan:ReplyChan * ip:IpAddress * port:uint16
    | Leave          of chan:ReplyChan
    | Get            of chan:ReplyChan
    | Status         of chan:ReplyChan
    | Periodic
    | ForceElection
    | RawRequest     of request:RawRequest
    | AddCmd         of chan:ReplyChan * sm:StateMachine
    | AddMember      of chan:ReplyChan * mem:RaftMember
    | RmMember       of chan:ReplyChan * id:Id
    | IsCommitted    of started:DateTime * entry:EntryResponse * chan:ReplyChan
    | ReqCommitted   of started:DateTime * entry:EntryResponse * response:RawResponse

    override self.ToString() =
      match self with
      | Load       (_,config)     -> sprintf "Load:  %A" config
      | Unload             _      -> sprintf "Unload"
      | Join       (_,ip,port)    -> sprintf "Join: %s %d" (string ip) port
      | RawRequest    request     -> sprintf "RawRequest with bytes: %d" (Array.length request.Body)
      | Leave         _           -> "Leave"
      | Get           _           -> "Get"
      | Status        _           -> "Status"
      | Periodic                  -> "Periodic"
      | ForceElection             -> "ForceElection"
      | AddCmd        (_,sm)      -> sprintf "AddCmd:  %A" sm
      | AddMember     (_,mem)     -> sprintf "AddMember:  %A" mem
      | RmMember      (_,id)      -> sprintf "RmMember:  %A" id
      | IsCommitted   (_,_,entry) -> sprintf "IsCommitted:  %A" entry
      | ReqCommitted  (_,_,entry) -> sprintf "ReqCommitted:  %A" entry

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<RaftEvent>>

  // ** StateArbiter

  type private StateArbiter = MailboxProcessor<Msg>

  // ** RaftServerState

  [<NoComparison;NoEquality>]
  type private RaftServerState =
    | Idle
    | Loaded of RaftAppContext

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

  // ** getMember

  /// ## getMember
  ///
  /// Return the current mem.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: RaftMember
  let private getMember (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf

  // ** getMemberId

  /// ## getMemberId
  ///
  /// Return the current mem Id.
  ///
  /// ### Signature:
  /// - context: RaftAppContext
  ///
  /// Returns: Id
  let private getMemberId (context: RaftAppContext) =
    context
    |> getRaft
    |> Raft.getSelf
    |> Member.getId

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

  // ** postCommand

  let inline private postCommand (arbiter: StateArbiter) (cb: ReplyChan -> Msg) =
    async {
      let! result = arbiter.PostAndTryAsyncReply(cb, Constants.COMMAND_TIMEOUT)
      match result with
      | Some response -> return response
      | None ->
        return
          "Command Timeout"
          |> Error.asOther (tag "postCommand")
          |> Either.fail
    }
    |> Async.RunSynchronously

  //   ____      _ _ _                _
  //  / ___|__ _| | | |__   __ _  ___| | _____
  // | |   / _` | | | '_ \ / _` |/ __| |/ / __|
  // | |__| (_| | | | |_) | (_| | (__|   <\__ \
  //  \____\__,_|_|_|_.__/ \__,_|\___|_|\_\___/

  // ** getConnection

  let private getConnection (connections: Connections) (peer: RaftMember) =
    match connections.TryGetValue peer.Id with
    | true, connection -> connection
    | _ ->
      let connection = mkReqSocket peer
      while not (connections.TryAdd(peer.Id, connection)) do
        "Unable to add connection. Retrying."
        |> Logger.err (tag "getConnection")
        Thread.Sleep 1
      connection

  // ** sendRequestVote

  let private sendRequestVote (self: Id)
                              (connections: Connections)
                              (peer: RaftMember)
                              (request: VoteRequest) :
                              VoteResponse option =

    let request = RequestVote(self, request)
    let client = getConnection connections peer
    let result = performRequest client request

    match result with
    | Right (RequestVoteResponse(_, vote)) -> Some vote
    | Right other ->
      other
      |> sprintf "Unexpected Response:  %A"
      |> Logger.err (tag "sendRequestVote")
      None

    | Left error ->
      dispose client
      connections.TryRemove peer.Id |> ignore
      Uri.raftUri peer
      |> sprintf "Encountered error %A in request to  %A" error
      |> Logger.err (tag "sendRequestVote")
      None

  // ** sendAppendEntries

  let private sendAppendEntries (self: Id)
                                (connections: Connections)
                                (peer: RaftMember)
                                (request: AppendEntries) =

    let request = AppendEntries(self, request)
    let client = getConnection connections peer
    let result = performRequest client request

    match result with
    | Right (AppendEntriesResponse(_, ar)) -> Some ar
    | Right response ->
      response
      |> sprintf "Unexpected Response:   %A"
      |> Logger.err (tag "sendAppendEntries")
      None
    | Left error ->
      Uri.raftUri peer
      |> sprintf "SendAppendEntries: received error  %A in request to  %A" error
      |> Logger.err (tag "sendAppendEntries")
      None

  // ** sendInstallSnapshot

  let private sendInstallSnapshot (self: Id)
                                  (connections: Connections)
                                  (peer: RaftMember)
                                  (is: InstallSnapshot) =
    let client = getConnection connections peer
    let request = InstallSnapshot(self, is)
    let result = performRequest client request

    match result with
    | Right (InstallSnapshotResponse(_, ar)) -> Some ar
    | Right response ->
      response
      |> sprintf "Unexpected Response:  %A"
      |> Logger.err (tag "sendInstallSnapshot")
      None
    | Left error ->
      Uri.raftUri peer
      |> sprintf "SendInstallSnapshot: received error  %A in request to  %A" error
      |> Logger.err (tag "sendInstallSnapshot")
      None

  let private trigger (subscriptions: Subscriptions) (ev: RaftEvent) =
    Tracing.trace "RaftServer.trigger" <| fun () ->
      for subscription in subscriptions do
        subscription.OnNext ev

  // ** mkCallbacks

  let private mkCallbacks (id: Id)
                          (connections: Connections)
                          (subscriptions: Subscriptions) =

    { new IRaftCallbacks with

        member self.SendRequestVote peer request =
          Tracing.trace "RaftServer.sendRequestVote" <| fun () ->
            sendRequestVote id connections peer request

        member self.SendAppendEntries peer request =
          Tracing.trace "RaftServer.sendAppendEntries" <| fun () ->
            sendAppendEntries id connections peer request

        member self.SendInstallSnapshot peer request =
          Tracing.trace "RaftServer.sendInstallSnapshot" <| fun () ->
            sendInstallSnapshot id connections peer request

        member self.PrepareSnapshot raft =
          Tracing.trace "RaftServer.prepareSnapshot" <| fun () ->
            let ch:Ch<State option> = Ch()

            ch
            |> RaftEvent.CreateSnapshot
            |> trigger subscriptions

            let result =
              job {
                let! state = Ch.take ch
                return state
              }
              |> Hopac.run

            Option.map
              (fun snapshot -> Raft.createSnapshot (DataSnapshot snapshot) raft)
              result

        member self.RetrieveSnapshot () =
          Tracing.trace "RaftServer.retrieveSnapshot" <| fun () ->
            let ch:Ch<RaftLogEntry option> = Ch()

            asynchronously <| fun () ->
              ch
              |> RaftEvent.RetrieveSnapshot
              |> trigger subscriptions

            job {
              let! state = Ch.take ch
              return state
            }
            |> Hopac.run

        member self.PersistSnapshot log =
          Tracing.trace "RaftServer.persistSnapshot" <| fun () ->
            log
            |> RaftEvent.PersistSnapshot
            |> trigger subscriptions

        member self.ApplyLog cmd =
          Tracing.trace "RaftServer.applyLog" <| fun () ->
            cmd
            |> RaftEvent.ApplyLog
            |> trigger subscriptions

        member self.MemberAdded mem =
          Tracing.trace "RaftServer.memberAdded" <| fun () ->
            mem
            |> RaftEvent.MemberAdded
            |> trigger subscriptions

        member self.MemberUpdated mem =
          Tracing.trace "RaftServer.memberUpdated" <| fun () ->
            mem
            |> RaftEvent.MemberUpdated
            |> trigger subscriptions

        member self.MemberRemoved mem =
          Tracing.trace "RaftServer.memberRemoved" <| fun () ->
            mem
            |> RaftEvent.MemberRemoved
            |> trigger subscriptions

        member self.Configured mems =
          Tracing.trace "RaftServer.configured" <| fun () ->
            mems
            |> RaftEvent.Configured
            |> trigger subscriptions

        member self.StateChanged oldstate newstate =
          Tracing.trace "RaftServer.stateChanged" <| fun () ->
            (oldstate, newstate)
            |> RaftEvent.StateChanged
            |> trigger subscriptions

        member self.PersistVote mem =
          Tracing.trace "RaftServer.persistVote" <| fun () ->
            printfn "PersistVote"

  //     try
  //       self.State
  //       |> RaftContext.getRaft
  //       |> saveRaft options
  //       |> Either.mapError
  //         (fun err ->
  //           printfn "Could not persit vote change.  %A" err)
  //       |> ignore

  //       "PersistVote reset VotedFor" |> Logger.debug tag
  //     with

  //       | exn -> handleException "PersistTerm" exn

        member self.PersistTerm term =
          Tracing.trace "RaftServer.persistTerm" <| fun () ->
            printfn "PersistTerm"
  //     try
  //       self.State
  //       |> RaftContext.getRaft
  //       |> saveRaft options
  //       |> Either.mapError
  //         (fun err ->
  //           printfn "Could not persit vote change.  %A" err)
  //       |> ignore

  //       sprintf "PersistTerm term:  %A" term |> Logger.debug tag
  //     with

  //       | exn -> handleException "PersistTerm" exn

        member self.PersistLog log =
          Tracing.trace "RaftServer.persistLog" <| fun () ->
            printfn "PersistLog"

        member self.DeleteLog log =
          Tracing.trace "RaftServer.deleteLog" <| fun () ->
            printfn "DeleteLog"

        }


  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  // ** appendEntry

  let private appendEntry (state: RaftAppContext) (entry: RaftLogEntry) =
    Tracing.trace "RaftServer.appendEntry" <| fun () ->
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
    Tracing.trace "RaftServer.onConfigDone" <| fun () ->
      "appending entry to exit joint-consensus into regular configuration"
      |> Logger.debug (tag "onConfigDone")

      state.Raft.Peers
      |> Map.toArray
      |> Array.map snd
      |> Log.mkConfig state.Raft.CurrentTerm
      |> appendEntry state

  // ** addMembers

  /// ## addMembers
  ///
  /// Enter the Joint-Consensus by apppending a respective log entry.
  ///
  /// ### Signature:
  /// - state: current RaftServerState to work against
  /// - mems: the changes to make to the current cluster configuration
  ///
  /// Returns: Either<RaftError * RaftValue, unit * Raft, EntryResponse * RaftValue>
  let private addMembers (state: RaftAppContext) (mems: RaftMember array) =
    Tracing.trace "RaftServer.addMembers" <| fun () ->
      if Raft.isLeader state.Raft then
        mems
        |> Array.map ConfigChange.MemberAdded
        |> Log.mkConfigChange state.Raft.CurrentTerm
        |> appendEntry state
      else
        "Unable to add new member. Not leader."
        |> Logger.err (tag "addMembers")
        (RaftError(tag "addMembers", "Not Leader"), state)
        |> Either.fail

  // ** addNewMember

  let private addNewMember (state: RaftAppContext) (id: Id) (ip: IpAddress) (port: uint32) =
    Tracing.trace "RaftServer.addNewMember" <| fun () ->
      sprintf "attempting to add mem with
            %A  %A:%d" (string id) (string ip) port
      |> Logger.debug (tag "addNewMember")

      [| { Member.create id with
            IpAddr = ip
            Port   = uint16 port } |]
      |> addMembers state

  // ** removeMembers

  /// ## removeMembers
  ///
  /// Function to execute a two-phase commit for adding/removing members from the cluster.
  ///
  /// ### Signature:
  /// - changes: configuration changes to make to the cluster
  /// - success: RaftResponse to return when successful
  /// - appState: transactional variable to work against
  ///
  /// Returns: RaftResponse
  let private removeMembers (state: RaftAppContext) (mems: RaftMember array) =
    Tracing.trace "RaftServer.removeMembers" <| fun () ->
      "appending entry to enter joint-consensus"
      |> Logger.debug (tag "removeMembers")

      mems
      |> Array.map ConfigChange.MemberRemoved
      |> Log.mkConfigChange state.Raft.CurrentTerm
      |> appendEntry state

  // ** removeMember

  let private removeMember (state: RaftAppContext) (id: Id) =
    Tracing.trace "RaftServer.removeMember" <| fun () ->
      if Raft.isLeader state.Raft then
        string id
        |> sprintf "attempting to remove members with id %A"
        |> Logger.debug (tag "removeMember")

        let potentialChange =
          state.Raft
          |> Raft.getMember id

        match potentialChange with

        | Some mem -> removeMembers state [| mem |]
        | None ->
          sprintf "Unable to remove member. Not found:  %A" (string id)
          |> Logger.err (tag "removeMember")

          (RaftError(tag "removeMember", sprintf "Missing Member: %A" id), state)
          |> Either.fail
      else
        "Unable to remove mem. Not leader."
        |> Logger.err (tag "removeMember")

        (RaftError(tag "removeMember","Not Leader"), state)
        |> Either.fail

  // ** processAppendEntries

  let private processAppendEntries (state: RaftAppContext) (sender: Id) (ae: AppendEntries) (raw: RawRequest) =
    Tracing.trace "RaftServer.processAppendEntries" <| fun () ->
      let result =
        Raft.receiveAppendEntries (Some sender) ae
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        asynchronously <| fun _ ->
          (state.Raft.Member.Id, response)
          |> AppendEntriesResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate

      | Left (err, newstate) ->
        asynchronously <| fun _ ->
          err
          |> ErrorResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate

  // ** processAppendEntry

  let private processAppendEntry (state: RaftAppContext) (cmd: StateMachine) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.processAppendEntry" <| fun () ->
      if Raft.isLeader state.Raft then    // I'm leader, so I try to append command
        match appendCommand state cmd with
        | Right (entry, newstate) ->       // command was appended, now queue a message and the later
          asynchronously <| fun _ ->
            let response =                  // response to check its committed status, eventually
              entry                         // timing out or responding to the server
              |> AppendEntryResponse
              |> Binary.encode
              |> RawResponse.fromRequest raw
            (DateTime.Now, entry, response)
            |> Msg.ReqCommitted
            |> arbiter.Post
          newstate
        | Left (err, newstate) ->          // Request was unsuccessful, respond immeditately
          asynchronously <| fun _ ->
            err
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          newstate
      else
        match Raft.getLeader state.Raft with // redirect to known leader or fail
        | Some mem ->
          asynchronously <| fun _ ->
            mem
            |> Redirect
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          state
        | None ->
          asynchronously <| fun _ ->
            "Not leader and no known leader."
            |> Error.asRaftError (tag "processAppendEntry")
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          state

  // ** processVoteRequest

  let private processVoteRequest (state: RaftAppContext) (sender: Id) (vr: VoteRequest) (raw: RawRequest) =
    Tracing.trace "RaftServer.processVoteRequest" <| fun () ->
      let result =
        Raft.receiveVoteRequest sender vr
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        asynchronously <| fun _ ->
          (state.Raft.Member.Id, response)
          |> RequestVoteResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate

      | Left (err, newstate) ->
        asynchronously <| fun _ ->
          err
          |> ErrorResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate

  // ** processInstallSnapshot

  let private processInstallSnapshot (state: RaftAppContext) (mem: Id) (is: InstallSnapshot) (raw: RawRequest) =
    Tracing.trace "RaftServer.processInstallSnapshot" <| fun () ->
      let result =
        Raft.receiveInstallSnapshot is
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        asynchronously <| fun _ ->
          (state.Raft.Member.Id, response)
          |> InstallSnapshotResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate
      | Left (error, newstate) ->
        asynchronously <| fun _ ->
          error
          |> ErrorResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        updateRaft state newstate

  // ** doRedirect

  /// ## Redirect to leader
  ///
  /// Gets the current leader mem from the Raft state and returns a corresponding RaftResponse.
  ///
  /// ### Signature:
  /// - state: RaftServerState
  ///
  /// Returns: Either<IrisError,RaftResponse>
  let private doRedirect (state: RaftAppContext) (raw: RawRequest) =
    Tracing.trace "RaftServer.doRedirect" <| fun () ->
      match Raft.getLeader state.Raft with
      | Some mem ->
        asynchronously <| fun _ ->
          mem
          |> Redirect
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        state
      | None ->
        asynchronously <| fun _ ->
          "No known leader"
          |> Error.asRaftError (tag "doRedirect")
          |> ErrorResponse
          |> Binary.encode
          |> RawResponse.fromRequest raw
          |> state.Server.Respond
        state

  // ** processHandshake

  /// ## Process a HandShake request by a certain Mem.
  ///
  /// Handle a request to join the cluster. Respond with Welcome if everything is OK. Redirect to
  /// leader if we are currently not Leader.
  ///
  /// ### Signature:
  /// - mem: Mem which wants to join the cluster
  /// - appState: current TVar<RaftServerState>
  ///
  /// Returns: RaftResponse
  let private processHandshake (state: RaftAppContext) (mem: RaftMember) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.processHandshake" <| fun () ->
      if Raft.isLeader state.Raft then
        match addMembers state [| mem |] with
        | Right (entry, newstate) ->
          asynchronously <| fun _ ->
            let response =                  // response to check its committed status, eventually
              mem
              |> Welcome
              |> Binary.encode
              |> RawResponse.fromRequest raw
            (DateTime.Now, entry, response)
            |> Msg.ReqCommitted
            |> arbiter.Post
          newstate
        | Left (err, newstate) ->
          asynchronously <| fun _ ->
            err
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          newstate
      else
        doRedirect state raw

  // ** processHandwaive

  let private processHandwaive (state: RaftAppContext) (mem: RaftMember) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.processHandwaive" <| fun () ->
      if Raft.isLeader state.Raft then
        match removeMember state mem.Id with
        | Right (entry, newstate) ->
          asynchronously <| fun _ ->
            let response =                  // response to check its committed status, eventually
              Arrivederci
              |> Binary.encode
              |> RawResponse.fromRequest raw
            (DateTime.Now, entry, response)
            |> Msg.ReqCommitted
            |> arbiter.Post
          newstate
        | Left (err, newstate) ->
          asynchronously <| fun _ ->
            err
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          newstate
      else
        doRedirect state raw

  // // ** processAppendEntriesResponse

  // let private processAppendEntriesResponse (state: RaftAppContext) (mem: Id) (ar: AppendResponse) =
  //   let result =
  //     Raft.receiveAppendEntriesResponse mem ar
  //     |> runRaft state.Raft state.Callbacks

  //   match result with
  //   | Right (_, newstate)  -> updateRaft state newstate
  //   | Left (err, newstate) -> err, updateRaft state newstate

  // // ** processVoteResponse

  // let private processVoteResponse (state: RaftAppContext)
  //                                 (sender: Id)
  //                                 (vr: VoteResponse)
  //                                 (channel: ReplyChan) =
  //   let result =
  //     Raft.receiveVoteResponse sender vr
  //     |> runRaft state.Raft state.Callbacks

  //   match result with
  //   | Right (_, newstate) ->
  //     Reply.Ok
  //     |> Either.succeed
  //     |> channel.Reply
  //     updateRaft state newstate

  //   | Left (err, newstate) ->
  //     err
  //     |> Either.fail
  //     |> channel.Reply
  //     updateRaft state newstate

  // // ** processSnapshotResponse

  // let private processSnapshotResponse (state: RaftAppContext)
  //                                     (sender: Id)
  //                                     (ar: AppendResponse)
  //                                     (channel: ReplyChan) =
  //   "FIX RESPONSE PROCESSING FOR SNAPSHOT REQUESTS"
  //   |> Logger.err (tag "processSnapshotResponse")

  //   Reply.Ok
  //   |> Either.succeed
  //   |> channel.Reply
  //   state

  // // ** processRedirect

  // let private processRedirect (state: RaftAppContext)
  //                             (leader: RaftMember)
  //                             (channel: ReplyChan) =
  //   "FIX REDIRECT RESPONSE PROCESSING"
  //   |> Logger.err (tag "processRedirect")

  //   Reply.Ok
  //   |> Either.succeed
  //   |> channel.Reply
  //   state

  // // ** processWelcome

  // let private processWelcome (state: RaftAppContext)
  //                            (leader: RaftMember)
  //                            (channel: ReplyChan) =

  //   "FIX WELCOME RESPONSE PROCESSING"
  //   |> Logger.err (tag "processWelcome")

  //   Reply.Ok
  //   |> Either.succeed
  //   |> channel.Reply
  //   state

  // // ** processArrivederci

  // let private processArrivederci (state: RaftAppContext)
  //                                (channel: ReplyChan) =

  //   "FIX ARRIVEDERCI RESPONSE PROCESSING"
  //   |> Logger.err (tag "processArrivederci")

  //   Reply.Ok
  //   |> Either.succeed
  //   |> channel.Reply
  //   state

  // // ** processErrorResponse

  // let private processErrorResponse (state: RaftAppContext)
  //                                  (error: IrisError)
  //                                  (channel: ReplyChan) =

  //   error
  //   |> sprintf "received error response:  %A"
  //   |> Logger.err (tag "processErrorResponse")

  //   Reply.Ok
  //   |> Either.succeed
  //   |> channel.Reply
  //   state

  // ** tryJoin

  let private tryJoin (state: RaftAppContext) (ip: IpAddress) (port: uint16) =
    let rec _tryJoin retry peer =
      either {
        if retry < int state.Options.Raft.MaxRetries then
          use client = mkReqSocket peer

          sprintf "Retry: %d" retry
          |> Logger.debug "tryJoin"

          let request = HandShake(state.Raft.Member)
          let! result = rawRequest request client

          sprintf "Result:  %A" result
          |> Logger.debug "tryJoin"

          match result with
          | Welcome mem ->
            sprintf "Received Welcome from  %A" mem.Id
            |> Logger.debug "tryJoin"
            return mem

          | Redirect next ->
            sprintf "Got redirected to  %A" (Uri.raftUri next)
            |> Logger.info "tryJoin"
            return! _tryJoin (retry + 1) next

          | ErrorResponse err ->
            sprintf "Unexpected error occurred.  %A" err
            |> Logger.err "tryJoin"
            return! Either.fail err

          | resp ->
            sprintf "Unexpected response.  %A" resp
            |> Logger.err "tryJoin"
            return!
              "Unexpected response"
              |> Error.asRaftError (tag "tryJoin")
              |> Either.fail
        else
          "Too many unsuccesful connection attempts."
          |> Logger.err "tryJoin"
          return!
            "Too many unsuccesful connection attempts."
            |> Error.asRaftError (tag "tryJoin")
            |> Either.fail
      }

    Tracing.trace "RaftServer.tryJoin" <| fun () ->
      // execute the join request with a newly created "fake" mem
      _tryJoin 0 { Member.create (Id.Create()) with
                    IpAddr = ip
                    Port   = port }

  // ** tryJoinCluster

  let private tryJoinCluster (state: RaftAppContext) (ip: IpAddress) (port: uint16) =
    Tracing.trace "RaftServer.tryJoinCluster" <| fun () ->
      raft {
        "requesting to join"
        |> Logger.debug (tag "tryJoinCluster")

        let leader = tryJoin state ip port

        match leader with
        | Right leader ->
          sprintf "Reached leader:  %A Adding to mems." leader.Id
          |> Logger.info (tag "tryJoinCluster")

          do! Raft.addMemberM leader
          do! Raft.becomeFollower ()

        | Left err ->
          sprintf "Joining cluster failed.  %A" err
          |> Logger.err (tag "tryJoinCluster")

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
    let rec _tryLeave retry mem =
      either {
        if retry < int state.Options.Raft.MaxRetries then
          use client = mkReqSocket mem

          let request = HandWaive(state.Raft.Member)
          let! result = rawRequest request client

          match result with

          | Redirect other ->
            if retry <= int state.Options.Raft.MaxRetries then
              return! _tryLeave (retry + 1) other
            else
              return!
                "Too many retries, aborting."
                |> Error.asRaftError (tag "tryLeave")
                |> Either.fail
          | Arrivederci       -> return true
          | ErrorResponse err -> return! Either.fail err
          | resp ->
            return!
              "Unexpected response"
              |> Error.asRaftError (tag "tryLeave")
              |> Either.fail
        else
          return!
            "Too many unsuccesful connection attempts."
            |> Error.asRaftError (tag "tryLeave")
            |> Either.fail
      }

    Tracing.trace "RaftServer.tryLeave" <| fun () ->
      match state.Raft.CurrentLeader with
      | Some nid ->
        match Map.tryFind nid state.Raft.Peers with

        | Some mem -> _tryLeave 0 mem
        | _         ->
          "Member data for leader id not found"
          |> Error.asRaftError (tag "tryLeave")
          |> Either.fail
      | _ ->
        "No known Leader"
        |> Error.asRaftError (tag "tryLeave")
        |> Either.fail

  // ** leaveCluster

  let private tryLeaveCluster (state: RaftAppContext) =
    Tracing.trace "RaftServer.tryLeaveCluster" <| fun () ->
      raft {
        do! Raft.setTimeoutElapsedM 0u

        match tryLeave state with

        | Right true  ->
          // FIXME: this might need more consequences than this
          "Successfully left cluster."
          |> Logger.info (tag "tryLeaveCluster")

        | Right false ->
          "Could not leave cluster."
          |> Logger.err (tag "tryLeaveCluster")

        | Left err ->
          err
          |> sprintf "Could not leave cluster.  %A"
          |> Logger.err (tag "tryLeaveCluster")

        do! Raft.becomeFollower ()

        let! peers = Raft.getMembersM ()

        for kv in peers do
          do! Raft.removeMemberM kv.Value

      }
      |> runRaft state.Raft state.Callbacks

  // ** forceElection

  let private forceElection (state: RaftAppContext) =
    Tracing.trace "RaftServer.forceElection" <| fun () ->
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
    Tracing.trace "RaftServer.initializeRaft" <| fun () ->
      raft {
        let term = 0u
        do! Raft.setTermM term
        let! num = Raft.numMembersM ()

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
          let! _ = inbox.Receive()
          arbiter.Post(Msg.Periodic)
          do! Async.Sleep(interval) // sleep for inverval (ms)
          return! loop (n + 1)
        }
      loop 0)
    :> IDisposable

  // ** load

  let private load (config: IrisConfig) (subscriptions: Subscriptions) (agent: StateArbiter) =
    Tracing.trace "RaftServer.load" <| fun () ->
      either {
        let connections = new Connections()

        let! raftstate = Persistence.getRaft config

        try
          let frontend = raftstate.Member |> Uri.raftUri
          let backend = Constants.RAFT_BACKEND_PREFIX + string raftstate.Member.Id
          let! server = Broker.create raftstate.Member.Id 5 frontend backend
          let srvobs = server.Subscribe(Msg.RawRequest >> agent.Post)

          let callbacks =
            mkCallbacks
              raftstate.Member.Id
              connections
              subscriptions

          Map.iter
            (fun _ (peer: RaftMember) ->
              if peer.Id <> raftstate.Member.Id then
                getConnection connections peer
                |> ignore)
            raftstate.Peers

          // periodic function
          let interval = int config.Raft.PeriodicInterval
          let periodic = startPeriodic interval agent

          match initializeRaft raftstate callbacks with
          | Right (_, newstate) ->
            return
              Loaded { Status      = ServiceStatus.Running
                       Raft        = newstate
                       Callbacks   = callbacks
                       Options     = config
                       Server      = server
                       Disposables = [ periodic; srvobs ]
                       Connections = connections }
          | Left (err, _) ->
            dispose server
            connections |> Seq.iter (fun (KeyValue(_,connection)) ->
              dispose connection)
            dispose periodic
            return! Either.fail err
        with
          | exn ->
            return!
              exn.Message
              |> Error.asRaftError "load"
              |> Either.fail
      }

  // ** handleLoad

  let private handleLoad (state: RaftServerState)
                         (chan: ReplyChan)
                         (config: IrisConfig)
                         (subscriptions: Subscriptions)
                         (agent: StateArbiter) =
    Tracing.trace "RaftServer.handleLoad" <| fun () ->
      match state with
      | Loaded data -> dispose data
      | Idle -> ()

      match load config subscriptions agent with
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
    Tracing.trace "RaftServer.handleUnload" <| fun () ->
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
    Tracing.trace "RaftServer.handleStatus" <| fun () ->
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

  let private handleJoin (state: RaftServerState) (chan: ReplyChan) (ip: IpAddress) (port: UInt16) =
    Tracing.trace "RaftServer.handleJoin" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleJoin")
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
    Tracing.trace "RaftServer.handleLeave" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleLeave")
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
    Tracing.trace "RaftServer.handleForceElection" <| fun () ->
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
          |> sprintf "Unable to force an election:  %A"
          |> Logger.err (tag "handleForceElection")

          newstate
          |> updateRaft data
          |> Loaded

  // ** handleAddCmd

  let private handleAddCmd (state: RaftServerState) (chan: ReplyChan) (cmd: StateMachine) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleAddCmd" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleAddCmd")
        |> Either.fail
        |> chan.Reply
        state

      | Loaded data ->
        match appendCommand data cmd with
        | Right (entry, newstate) ->
          (DateTime.Now, entry, chan)
          |> Msg.IsCommitted
          |> arbiter.Post
          Loaded newstate

        | Left (err, newstate) ->
          err
          |> Either.fail
          |> chan.Reply
          Loaded newstate


  // // ** handleResponse

  // let private handleResponse (state: RaftServerState) (chan: ReplyChan) (response: RaftResponse) =
  //   match state with
  //   | Idle ->
  //     "No config loaded"
  //     |> Error.asRaftError (tag "handleResponse")
  //     |> Either.fail
  //     |> chan.Reply
  //     state

  //   | Loaded data ->
  //     match response with
  //     | RequestVoteResponse     (sender, vote) -> processVoteResponse          data sender vote chan
  //     | AppendEntriesResponse   (sender, ar)   -> processAppendEntriesResponse data sender ar   chan
  //     | InstallSnapshotResponse (sender, ar)   -> processSnapshotResponse      data sender ar   chan
  //     | ErrorResponse            error         -> processErrorResponse         data error       chan
  //     | _                                      -> data
  //     |> Loaded

  // // ** handleRequest

  // let private handleRequest (state: RaftServerState) (chan: ReplyChan) (req: RaftRequest) =
  //   match state with
  //   | Idle ->
  //     "No config loaded"
  //     |> Error.asRaftError (tag "handleRequest")
  //     |> Either.fail
  //     |> chan.Add
  //     state

  //   | Loaded data ->
  //     match req with
  //     | AppendEntries (id, ae)   -> processAppendEntries   data id ae
  //     | AppendEntry  sm          -> processAppendEntry     data sm
  //     | RequestVote (id, vr)     -> processVoteRequest     data id vr
  //     | InstallSnapshot (id, is) -> processInstallSnapshot data id is
  //     | HandShake mem            -> processHandshake       data mem
  //     | HandWaive mem            -> processHandwaive       data mem
  //     |> Loaded

  // ** handlePeriodic

  let private handlePeriodic (state: RaftServerState) =
    Tracing.trace "RaftServer.handlePeriodic" <| fun () ->
      match state with
      | Idle -> Idle
      | Loaded data ->
        uint32 data.Options.Raft.PeriodicInterval
        |> Raft.periodic
        |> evalRaft data.Raft data.Callbacks
        |> updateRaft data
        |> Loaded

  // ** handleGet

  let private handleGet (state: RaftServerState) (chan: ReplyChan) =
    Tracing.trace "RaftServer.handleGet" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleGet")
        |> Either.fail
        |> chan.Reply
        state

      | Loaded data ->
        Reply.State data
        |> Either.succeed
        |> chan.Reply
        state


  // ** handleAddMember

  let private handleAddMember (state: RaftServerState) (chan: ReplyChan) (mem: RaftMember) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleAddMember" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleAddMember")
        |> Either.fail
        |> chan.Reply
        state

      | Loaded data ->
        match addMembers data [| mem |] with
        | Right (entry, newstate) ->
          (DateTime.Now, entry, chan)
          |> Msg.IsCommitted
          |> arbiter.Post
          Loaded newstate

        | Left (err, newstate) ->
          err
          |> Either.fail
          |> chan.Reply
          newstate
          |> Loaded

  // ** handleRemoveMember

  let private handleRemoveMember (state: RaftServerState) (chan: ReplyChan) (id: Id) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleRemoveMember" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleRemoveMember")
        |> Either.fail
        |> chan.Reply
        state

      | Loaded data ->
        match removeMember data id with
        | Right (entry, newstate) ->
          (DateTime.Now, entry, chan)
          |> Msg.IsCommitted
          |> arbiter.Post
          Loaded newstate

        | Left (err, newstate) ->
          err
          |> Either.fail
          |> chan.Reply
          Loaded newstate

  // ** handleIsCommitted

  let private handleIsCommitted (state: RaftServerState) (ts: DateTime) (entry: EntryResponse) (chan: ReplyChan) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleIsCommitted" <| fun () ->
      match state with
      | Idle ->
        "No config loaded"
        |> Error.asRaftError (tag "handleIsCommitted")
        |> Either.fail
        |> chan.Reply
        state

      | Loaded data ->
        let result =
          Raft.responseCommitted entry
          |> runRaft data.Raft data.Callbacks

        let delta = DateTime.Now - ts

        match result with
        | Right (true, newstate) ->        // the entry was committed, hence we reply to the caller
          asynchronously <| fun _ ->
            entry
            |> Reply.Entry
            |> Either.succeed
            |> chan.Reply

            delta.TotalMilliseconds
            |> sprintf "Completed request in %fms"
            |> Logger.debug "handleIsCommitted"

          newstate
          |> updateRaft data
          |> Loaded

        | Right (false, newstate) ->       // the entry was not yet committed
          if int delta.TotalMilliseconds > Constants.COMMAND_TIMEOUT then
            asynchronously <| fun _ ->                        // the maximum timout has been crossed, hence the request
              "Command timed out"          // failed miserably
              |> Error.asRaftError "handleIsCommitted"
              |> Either.fail
              |> chan.Reply

              delta.TotalMilliseconds
              |> sprintf "Command append failed after %fms"
              |> Logger.err "handleIsCommitted"
          else
            job {                        // now we re-queue the message to check again in 1ms
              do! timeOutMillis 1000
              (ts, entry, chan)
              |> Msg.IsCommitted
              |> arbiter.Post
            } |> Hopac.start

          newstate
          |> updateRaft data
          |> Loaded

        | Left (err, newstate) ->          // encountered an error during check. request failed
          err
          |> Either.fail
          |> chan.Reply

          newstate
          |> updateRaft data
          |> Loaded

  // ** processRequest

  let private processRequest (data: RaftAppContext) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.processRequest" <| fun () ->
      either {
        let! request = Binary.decode<RaftRequest> raw.Body

        let newstate =
          match request with
          | AppendEntries (id, ae)   -> processAppendEntries   data id  ae  raw
          | RequestVote (id, vr)     -> processVoteRequest     data id  vr  raw
          | InstallSnapshot (id, is) -> processInstallSnapshot data id  is  raw
          | AppendEntry  sm          -> processAppendEntry     data sm  raw arbiter
          | HandShake mem            -> processHandshake       data mem raw arbiter
          | HandWaive mem            -> processHandwaive       data mem raw arbiter

        return newstate
      }

  // ** handleRawRequest

  let private handleRawRequest (state: RaftServerState) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleRawRequest" <| fun () ->
      match state with
      | Loaded data ->
        match processRequest data raw arbiter with
        | Right newdata -> Loaded newdata
        | Left error ->
          asynchronously <| fun _ ->
            error
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> data.Server.Respond
          state
      | Idle -> state

  // ** handleReqCommitted

  let private handleReqCommitted (state: RaftServerState) (ts: DateTime) (entry: EntryResponse) (raw: RawResponse) (arbiter: StateArbiter) =
    Tracing.trace "RaftServer.handleReqCommitted" <| fun () ->
      match state with
      | Loaded data ->
        let result =
          Raft.responseCommitted entry
          |> runRaft data.Raft data.Callbacks

        let delta = DateTime.Now - ts

        match result with
        | Right (true, newstate) ->
          asynchronously <| fun _ ->
            data.Server.Respond raw
            delta
            |> fun delta -> delta.TotalMilliseconds
            |> sprintf "Entry took %fms to commit"
            |> Logger.debug "handleReqCommitted"
          updateRaft data newstate
          |> Loaded

        | Right (false, newstate) ->
          if int delta.TotalMilliseconds > Constants.COMMAND_TIMEOUT then
            asynchronously <| fun _ ->
              let body =
                "AppendEntry timed out"
                |> Error.asRaftError "handleReqCommitted"
                |> ErrorResponse
                |> Binary.encode
              { raw with Body = body }
              |> data.Server.Respond

              delta
              |> fun delta -> delta.TotalMilliseconds
              |> sprintf "AppendEntry timed out: %f"
              |> Logger.debug "handleReqCommitted"
            updateRaft data newstate
            |> Loaded
          else
            job {
              do! timeOutMillis 1
              (ts, entry, raw)
              |> Msg.ReqCommitted
              |> arbiter.Post
            } |> Hopac.start
            updateRaft data newstate
            |> Loaded
        | Left (err, newstate) ->
          asynchronously <| fun _ ->
            { raw with Body = err |> ErrorResponse |> Binary.encode }
            |> data.Server.Respond
          updateRaft data newstate
          |> Loaded
      | Idle -> state

  // ** loop

  let private loop (subs: Subscriptions) (inbox: StateArbiter) =
    let rec act state =
      async {
        let! cmd = inbox.Receive()

        let newstate =
          Tracing.trace "RaftServer.loop" <| fun () ->
            match cmd with
            | Msg.Load (chan,config)     -> handleLoad          state chan config subs inbox
            | Msg.Unload chan            -> handleUnload        state chan
            | Msg.Status chan            -> handleStatus        state chan
            | Msg.Join (chan, ip, port)  -> handleJoin          state chan ip port
            | Msg.Leave chan             -> handleLeave         state chan
            | Msg.Periodic               -> handlePeriodic      state
            | Msg.ForceElection          -> handleForceElection state
            | Msg.AddCmd (chan, cmd)     -> handleAddCmd        state chan cmd inbox
            | Msg.Get chan               -> handleGet           state chan
            | Msg.AddMember  (chan, mem) -> handleAddMember     state chan mem inbox
            | Msg.RmMember    (chan, id) -> handleRemoveMember  state chan id  inbox
            | Msg.IsCommitted (t,e,c)    -> handleIsCommitted   state t e c    inbox
            | Msg.RawRequest   request   -> handleRawRequest    state request  inbox
            | Msg.ReqCommitted (t,e,r)   -> handleReqCommitted  state t e r    inbox

        do! act newstate
      }
    act Idle

  // ** withOk

  let private withOk (msgcb: ReplyChan -> Msg) (agent: StateArbiter) : Either<IrisError,unit> =
    match postCommand agent msgcb with
    | Right Reply.Ok -> Right ()

    | Right other ->
      sprintf "Received garbage reply from agent:  %A" other
      |> Error.asRaftError (tag "withOk")
      |> Either.fail

    | Left error ->
      Either.fail error

  // ** addCmd

  let private addCmd (agent: StateArbiter)
                     (cmd: StateMachine) :
                     Either<IrisError, EntryResponse> =
    match postCommand agent (fun chan -> Msg.AddCmd(chan,cmd)) with
    | Right (Reply.Entry entry) -> Either.succeed entry
    | Right other ->
      sprintf "Received garbage reply from agent:  %A" other
      |> Error.asRaftError (tag "addCmd")
      |> Either.fail
    | Left error -> Either.fail error

  // ** getStatus

  let private getStatus (agent: StateArbiter) =
    match postCommand agent (fun chan -> Msg.Status chan) with
    | Right (Reply.Status status) -> Right status

    | Right other ->
      sprintf "Received garbage reply from agent:  %A" other
      |> Error.asRaftError (tag "getStatus")
      |> Either.fail

    | Left error ->
      Either.fail error

  // ** addMember

  let private addMember (agent: StateArbiter) (mem: RaftMember) =
    match postCommand agent (fun chan -> Msg.AddMember(chan,mem)) with
    | Right (Reply.Entry entry) -> Right entry
    | Right other ->
      sprintf "Unexpected reply by agent:  %A" other
      |> Error.asRaftError (tag "addMember")
      |> Either.fail
    | Left error ->
      Either.fail error

  // ** rmMember

  let private rmMember (agent: StateArbiter) (id: Id) =
    match postCommand agent (fun chan -> Msg.RmMember(chan,id)) with
    | Right (Reply.Entry entry) -> Right entry
    | Right other ->
      sprintf "Unexpected reply by agent:  %A" other
      |> Error.asRaftError (tag "rmMember")
      |> Either.fail
    | Left error ->
      Either.fail error

  // ** getState

  let private getState (agent: StateArbiter) =
    match postCommand agent (fun chan -> Msg.Get chan) with
    | Right (Reply.State state) -> Right state

    | Right other ->
      sprintf "Received garbage reply from agent:  %A" other
      |> Error.asRaftError (tag "getState")
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
                Tracing.trace "RaftServer.Load()" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Load(chan,config)) with
                  | Right Reply.Ok -> Right ()
                  | Right other ->
                    sprintf "Unexpected reply type from agent:  %A" other
                    |> Error.asRaftError (tag "create")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.Unload () =
                Tracing.trace "RaftServer.UnLoad()" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Unload chan) with
                  | Right Reply.Ok -> Right ()
                  | Right other ->
                    sprintf "Unexpected reply type from agent:  %A" other
                    |> Error.asRaftError (tag "create")
                    |> Either.fail
                  | Left error ->
                    error
                    |> Either.fail

              member self.Member
                with get () =
                  Tracing.trace "RaftServer.Member" <| fun () ->
                    match getState agent with
                    | Right state ->
                      state.Raft.Member
                      |> Either.succeed
                    | Left error ->
                      Either.fail error

              member self.MemberId
                with get () =
                  Tracing.trace "RaftServer.MemberId" <| fun () ->
                    match getState agent with
                    | Right state ->
                      state.Raft.Member.Id
                      |> Either.succeed
                    | Left error ->
                      Either.fail error

              member self.Append cmd =
                Tracing.trace "RaftServer.Append" <| fun () ->
                  addCmd agent cmd

              member self.Status
                with get () =
                  Tracing.trace "RaftServer.Status" <| fun () ->
                    getStatus agent

              member self.ForceElection () =
                Tracing.trace "RaftServer.ForceElection" <| fun () ->
                  Msg.ForceElection
                  |> agent.Post
                  |> Either.succeed

              member self.Periodic () =
                Tracing.trace "RaftServer.Periodic" <| fun () ->
                  Msg.Periodic
                  |> agent.Post
                  |> Either.succeed

              member self.JoinCluster ip port =
                Tracing.trace "RaftServer.JoinCluster" <| fun () ->
                  withOk (fun chan -> Msg.Join(chan,ip,port)) agent

              member self.LeaveCluster () =
                Tracing.trace "RaftServer.LeaveCluster" <| fun () ->
                  withOk (fun chan -> Msg.Leave chan) agent

              member self.AddMember mem =
                Tracing.trace "RaftServer.AddMember" <| fun () ->
                  addMember agent mem

              member self.RmMember id =
                Tracing.trace "RaftServer.RmMember" <| fun () ->
                  rmMember agent id

              member self.State
                with get () =
                  Tracing.trace "RaftServer.State" <| fun () ->
                    getState agent

              member self.Subscribe (callback: RaftEvent -> unit) =
                { new IObserver<RaftEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Start () =
                Tracing.trace "RaftServer.Start" <| fun () ->
                  startServer agent

              member self.Connections
                with get () =
                  Tracing.trace "RaftServer.Connections" <| fun () ->
                    match getState agent with
                    | Right ctx  -> ctx.Connections |> Either.succeed
                    | Left error -> error |> Either.fail

              member self.IsLeader
                with get () =
                  Tracing.trace "RaftServer.IsLeader" <| fun () ->
                    match getState agent with
                    | Right state -> Raft.isLeader state.Raft
                    | _ -> false

              member self.Leader
                with get () =
                  Tracing.trace "RaftServer.Leader" <| fun () ->
                    match getState agent with
                    | Right state -> Raft.getLeader state.Raft |> Either.succeed
                    | Left error -> Either.fail error

              member self.Dispose () =
                Tracing.trace "RaftServer.Dispose()" <| fun () ->
                  match postCommand agent (fun chan -> Msg.Unload chan) with
                  | Left error -> printfn "unable to dispose:  %A" error
                  | Right _ -> ()
                  subscriptions.Clear()
                  dispose agent
            }
      }

#endif
