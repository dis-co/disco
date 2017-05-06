namespace Iris.Service

// * Imports

#if !IRIS_NODES

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

  let private tag (str: string) =
    String.Format("RaftServer.{0}", str)

  // ** IRaftStore

  type IRaftStore<'t when 't : not struct> =
    abstract State: 't
    abstract Update: 't -> unit

  // ** RaftStore module

  module RaftStore =

    let create<'t when 't : not struct> (initial: 't) =
      let mutable state = initial

      { new IRaftStore<'t> with
          member self.State with get () = state
          member self.Update update =
            Interlocked.CompareExchange<'t>(&state, update, state)
            |> ignore }

  // ** Connections

  type private Connections = ConcurrentDictionary<Id,IClient>

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Join           of ip:IpAddress * port:uint16
    | Leave
    | Periodic
    | ForceElection
    | Start
    | RawRequest     of request:RawRequest
    | RawResponse    of response:RawResponse
    | AddCmd         of sm:StateMachine
    | AddMember      of mem:RaftMember
    | RemoveMember   of id:Id
    | ReqCommitted   of started:DateTime * entry:EntryResponse * response:RawResponse
    // | IsCommitted    of started:DateTime * entry:EntryResponse

    override msg.ToString() =
      match msg with
      | Start                     -> "Start"
      | Join          (ip,port)   -> sprintf "Join: %s %d" (string ip) port
      | RawRequest      _         -> "RawRequest"
      | RawResponse     _         -> "RawResponse"
      | Leave                     -> "Leave"
      | Periodic                  -> "Periodic"
      | ForceElection             -> "ForceElection"
      | AddCmd          sm        -> sprintf "AddCmd:  %A" sm
      | AddMember       mem       -> sprintf "AddMember:  %O" mem.Id
      | RemoveMember        id    -> sprintf "RemoveMember:  %O" id
      | ReqCommitted  (_,entry,_) -> sprintf "ReqCommitted:  %A" entry
      // | IsCommitted   (_,entry)   -> sprintf "IsCommitted:  %A" entry

  // ** Subscriptions

  type private Subscriptions = ResizeArray<IObserver<RaftEvent>>

  // ** StateArbiter

  type private StateArbiter = MailboxProcessor<Msg>

  // ** getRaft

  /// ## pull Raft state value out of RaftServerState value
  ///
  /// Get Raft state value from RaftServerState.
  ///
  /// ### Signature:
  /// - context: RaftServerState
  ///
  /// Returns: Raft
  let private getRaft (context: RaftServerState) =
    context.Raft

  // ** getMember

  /// ## getMember
  ///
  /// Return the current mem.
  ///
  /// ### Signature:
  /// - context: RaftServerState
  ///
  /// Returns: RaftMember
  let private getMember (context: RaftServerState) =
    context
    |> getRaft
    |> Raft.getSelf

  // ** getMemberId

  /// ## getMemberId
  ///
  /// Return the current mem Id.
  ///
  /// ### Signature:
  /// - context: RaftServerState
  ///
  /// Returns: Id
  let private getMemberId (context: RaftServerState) =
    context
    |> getRaft
    |> Raft.getSelf
    |> Member.getId

  // ** updateRaft

  /// ## Update Raft in RaftServerState
  ///
  /// Update the Raft field of a given RaftServerState
  ///
  /// ### Signature:
  /// - raft: new Raft value to add to RaftServerState
  /// - state: RaftServerState to update
  ///
  /// Returns: RaftServerState
  let private updateRaft (context: RaftServerState) (raft: RaftValue) : RaftServerState =
    { context with Raft = raft }

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
                              (request: VoteRequest) : unit =

    let request = RequestVote(self, request)
    let client = getConnection connections peer
    performRequest client request

  // ** sendAppendEntries

  let private sendAppendEntries (self: Id)
                                (connections: Connections)
                                (peer: RaftMember)
                                (request: AppendEntries) =

    let request = AppendEntries(self, request)
    let client = getConnection connections peer
    performRequest client request

  // ** sendInstallSnapshot

  let private sendInstallSnapshot (self: Id)
                                  (connections: Connections)
                                  (peer: RaftMember)
                                  (is: InstallSnapshot) =
    let client = getConnection connections peer
    let request = InstallSnapshot(self, is)
    performRequest client request

  // ** notify

  let private notify (subscriptions: Subscriptions) (ev: RaftEvent) =
    Tracing.trace (tag "trigger") <| fun () ->
      for subscription in subscriptions do
        subscription.OnNext ev

  // ** mkCallbacks

  let private mkCallbacks (id: Id)
                          (connections: Connections)
                          (subscriptions: Subscriptions) =

    { new IRaftCallbacks with

        member self.SendRequestVote peer request =
          Tracing.trace (tag "sendRequestVote") <| fun () ->
            sendRequestVote id connections peer request

        member self.SendAppendEntries peer request =
          Tracing.trace (tag "sendAppendEntries") <| fun () ->
            sendAppendEntries id connections peer request

        member self.SendInstallSnapshot peer request =
          Tracing.trace (tag "sendInstallSnapshot") <| fun () ->
            sendInstallSnapshot id connections peer request

        member self.PrepareSnapshot raft =
          Tracing.trace (tag "prepareSnapshot") <| fun () ->
            let ch:Ch<State option> = Ch()

            ch
            |> RaftEvent.CreateSnapshot
            |> notify subscriptions

            let result =
              job {
                let! state = Ch.take ch
                return state
              }
              |> Hopac.run

            Option.map (DataSnapshot >> Raft.createSnapshot raft) result

        member self.RetrieveSnapshot () =
          Tracing.trace (tag "retrieveSnapshot") <| fun () ->
            let ch:Ch<RaftLogEntry option> = Ch()

            asynchronously <| fun () ->
              ch
              |> RaftEvent.RetrieveSnapshot
              |> notify subscriptions

            job {
              let! state = Ch.take ch
              return state
            }
            |> Hopac.run

        member self.PersistSnapshot log =
          Tracing.trace (tag "persistSnapshot") <| fun () ->
            log
            |> RaftEvent.PersistSnapshot
            |> notify subscriptions

        member self.ApplyLog cmd =
          Tracing.trace (tag "applyLog") <| fun () ->
            cmd
            |> RaftEvent.ApplyLog
            |> notify subscriptions

        member self.MemberAdded mem =
          Tracing.trace (tag "memberAdded") <| fun () ->
            mem
            |> RaftEvent.MemberAdded
            |> notify subscriptions

        member self.MemberUpdated mem =
          Tracing.trace (tag "memberUpdated") <| fun () ->
            mem
            |> RaftEvent.MemberUpdated
            |> notify subscriptions

        member self.MemberRemoved mem =
          Tracing.trace (tag "memberRemoved") <| fun () ->
            mem
            |> RaftEvent.MemberRemoved
            |> notify subscriptions

        member self.Configured mems =
          Tracing.trace (tag "configured") <| fun () ->
            mems
            |> RaftEvent.Configured
            |> notify subscriptions

        member self.StateChanged oldstate newstate =
          Tracing.trace (tag "stateChanged") <| fun () ->
            (oldstate, newstate)
            |> RaftEvent.StateChanged
            |> notify subscriptions

        member self.PersistVote mem =
          Tracing.trace (tag "persistVote") <| fun () ->
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
          Tracing.trace (tag "persistTerm") <| fun () ->
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
          Tracing.trace (tag "persistLog") <| fun () ->
            printfn "PersistLog"

        member self.DeleteLog log =
          Tracing.trace (tag "deleteLog") <| fun () ->
            printfn "DeleteLog"

        }


  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  // ** appendEntry

  let private appendEntry (state: RaftServerState) (entry: RaftLogEntry) =
    Tracing.trace (tag "appendEntry") <| fun () ->
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

  let private appendCommand (state: RaftServerState) (cmd: StateMachine) =
    cmd
    |> Log.make state.Raft.CurrentTerm
    |> appendEntry state

  // ** onConfigDone

  let private onConfigDone (state: RaftServerState) =
    Tracing.trace (tag "onConfigDone") <| fun () ->
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
  let private addMembers (state: RaftServerState) (mems: RaftMember array) =
    Tracing.trace (tag "addMembers") <| fun () ->
      if Raft.isLeader state.Raft then
        mems
        |> Array.map ConfigChange.MemberAdded
        |> Log.mkConfigChange state.Raft.CurrentTerm
        |> appendEntry state
      else
        let msg = "Unable to add new member. Not leader."
        let error = Error.asRaftError (tag "addMembers") msg
        Logger.err (tag "addMembers") msg
        Either.fail (error, state)

  // ** addNewMember

  let private addNewMember (state: RaftServerState) (id: Id) (ip: IpAddress) (port: uint32) =
    Tracing.trace (tag "addNewMember") <| fun () ->
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
  let private removeMembers (state: RaftServerState) (mems: RaftMember array) =
    Tracing.trace (tag "removeMembers") <| fun () ->
      "appending entry to enter joint-consensus"
      |> Logger.debug (tag "removeMembers")

      mems
      |> Array.map ConfigChange.MemberRemoved
      |> Log.mkConfigChange state.Raft.CurrentTerm
      |> appendEntry state

  // ** removeMember

  let private removeMember (state: RaftServerState) (id: Id) =
    Tracing.trace (tag "removeMember") <| fun () ->
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
          let msg = sprintf "Unable to remove member. Not found:  %A" (string id)
          let error = Error.asRaftError (tag "removeMember") msg
          Logger.err (tag "removeMember") msg
          Either.fail (error, state)
      else
        let msg = "Unable to remove mem. Not leader."
        let error = Error.asRaftError (tag "removeMember") msg
        Logger.err (tag "removeMember") msg
        Either.fail (error, state)

  // ** processAppendEntries

  let private processAppendEntries (state: RaftServerState) (sender: Id) (ae: AppendEntries) (raw: RawRequest) =
    Tracing.trace (tag "processAppendEntries") <| fun () ->
      let result =
        Raft.receiveAppendEntries (Some sender) ae
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        (state.Raft.Member.Id, response)
        |> AppendEntriesResponse
        |> Binary.encode
        |> RawResponse.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

      | Left (err, newstate) ->
        err
        |> ErrorResponse
        |> Binary.encode
        |> RawResponse.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

  // ** processAppendEntry

  let private processAppendEntry (state: RaftServerState) (cmd: StateMachine) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace (tag "processAppendEntry") <| fun () ->
      if Raft.isLeader state.Raft then    // I'm leader, so I try to append command
        match appendCommand state cmd with
        | Right (entry, newstate) ->     // command was appended, now queue a message and the later
          let response =                // response to check its committed status, eventually
            entry                       // timing out or responding to the server
            |> AppendEntryResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
          (DateTime.Now, entry, response)
          |> Msg.ReqCommitted
          |> arbiter.Post
          newstate
        | Left (err, newstate) ->        // Request was unsuccessful, respond immeditately
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

  let private processVoteRequest (state: RaftServerState) (sender: Id) (vr: VoteRequest) (raw: RawRequest) =
    Tracing.trace (tag "processVoteRequest") <| fun () ->
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

  let private processInstallSnapshot (state: RaftServerState) (mem: Id) (is: InstallSnapshot) (raw: RawRequest) =
    Tracing.trace (tag "processInstallSnapshot") <| fun () ->
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
  let private doRedirect (state: RaftServerState) (raw: RawRequest) =
    Tracing.trace (tag "doRedirect") <| fun () ->
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
  let private processHandshake (state: RaftServerState) (mem: RaftMember) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace (tag "processHandshake") <| fun () ->
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

  let private processHandwaive (state: RaftServerState) (mem: RaftMember) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace (tag "processHandwaive") <| fun () ->
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

  // ** processAppendEntriesResponse

  let private processAppendEntriesResponse (state: RaftServerState) (mem: Id) (ar: AppendResponse) =
    let result =
      Raft.receiveAppendEntriesResponse mem ar
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate)  -> updateRaft state newstate
    | Left (err, newstate) ->
      err
      |> RaftEvent.RaftError
      |> notify state.Subscriptions
      updateRaft state newstate

  // ** processVoteResponse

  let private processVoteResponse (state: RaftServerState) (sender: Id) (vr: VoteResponse) =
    let result =
      Raft.receiveVoteResponse sender vr
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) -> updateRaft state newstate
    | Left (err, newstate) ->
      err
      |> RaftEvent.RaftError
      |> notify state.Subscriptions
      updateRaft state newstate

  // ** processSnapshotResponse

  let private processSnapshotResponse (state: RaftServerState) (sender: Id) (ar: AppendResponse) =
    "FIX RESPONSE PROCESSING FOR SNAPSHOT REQUESTS"
    |> Logger.err (tag "processSnapshotResponse")
    state

  // ** processRedirect

  let private processRedirect (state: RaftServerState) (leader: RaftMember) =
    "FIX REDIRECT RESPONSE PROCESSING"
    |> Logger.err (tag "processRedirect")
    state

  // ** processWelcome

  let private processWelcome (state: RaftServerState) (leader: RaftMember) =

    "FIX WELCOME RESPONSE PROCESSING"
    |> Logger.err (tag "processWelcome")

    state

  // ** processArrivederci

  let private processArrivederci (state: RaftServerState) =
    "FIX ARRIVEDERCI RESPONSE PROCESSING"
    |> Logger.err (tag "processArrivederci")
    state

  // ** processErrorResponse

  let private processErrorResponse (state: RaftServerState) (error: IrisError) =
    error
    |> sprintf "received error response:  %A"
    |> Logger.err (tag "processErrorResponse")
    state

  // ** tryJoin

  let private tryJoin (state: RaftServerState) (ip: IpAddress) (port: uint16) =
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

    Tracing.trace (tag "tryJoin") <| fun () ->
      // execute the join request with a newly created "fake" mem
      _tryJoin 0 { Member.create (Id.Create()) with
                    IpAddr = ip
                    Port   = port }

  // ** tryJoinCluster

  let private tryJoinCluster (state: RaftServerState) (ip: IpAddress) (port: uint16) =
    Tracing.trace (tag "tryJoinCluster") <| fun () ->
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
  let private tryLeave (state: RaftServerState) : Either<IrisError,bool> =
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

    Tracing.trace (tag "tryLeave") <| fun () ->
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

  let private tryLeaveCluster (state: RaftServerState) =
    Tracing.trace (tag "tryLeaveCluster") <| fun () ->
      raft {
        do! Raft.setTimeoutElapsedM 0<ms>

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

  let private forceElection (state: RaftServerState) =
    Tracing.trace (tag "forceElection") <| fun () ->
      raft {
        let! timeout = Raft.electionTimeoutM ()
        do! Raft.setTimeoutElapsedM timeout
        do! Raft.periodic timeout
      }
      |> runRaft state.Raft state.Callbacks

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


  // ** handleJoin

  let private handleJoin (state: RaftServerState) (ip: IpAddress) (port: UInt16) =
    Tracing.trace (tag "handleJoin") <| fun () ->
      match tryJoinCluster state ip port with
      | Right (_, newstate) ->
        notify state.Subscriptions RaftEvent.JoinedCluster
        updateRaft state newstate
      | Left (error, newstate) ->
        error
        |> RaftEvent.RaftError
        |> notify state.Subscriptions
        updateRaft state newstate

  // ** handleLeave

  let private handleLeave (state: RaftServerState) =
    Tracing.trace (tag "handleLeave") <| fun () ->
      match tryLeaveCluster state with
      | Right (_, newstate) ->
        notify state.Subscriptions RaftEvent.LeftCluster
        updateRaft state newstate

      | Left (error, newstate) ->
        error
        |> string
        |> Logger.err (tag "handleLeave")
        error
        |> RaftEvent.RaftError
        |> notify state.Subscriptions
        updateRaft state newstate

  // ** handleForceElection

  let private handleForceElection (state: RaftServerState) =
    Tracing.trace (tag "handleForceElection") <| fun () ->
      match forceElection state with
      | Right (_, newstate) -> updateRaft state newstate
      | Left (err, newstate) ->
        err
        |> sprintf "Unable to force an election:  %A"
        |> Logger.err (tag "handleForceElection")

        err
        |> RaftEvent.RaftError
        |> notify state.Subscriptions

        updateRaft state newstate

  // ** handleAddCmd

  let private handleAddCmd (state: RaftServerState) (cmd: StateMachine) (arbiter: StateArbiter) =
    Tracing.trace (tag "handleAddCmd") <| fun () ->
      match appendCommand state cmd with
      | Right (entry, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> arbiter.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleAddCmd")
        err
        |> RaftEvent.RaftError
        |> notify state.Subscriptions
        newstate

  // ** handlePeriodic

  let private handlePeriodic (state: RaftServerState) =
    Tracing.trace (tag "handlePeriodic") <| fun () ->
      int state.Options.Raft.PeriodicInterval * 1<ms>
      |> Raft.periodic
      |> evalRaft state.Raft state.Callbacks
      |> updateRaft state

  // ** handleAddMember

  let private handleAddMember (state: RaftServerState) (mem: RaftMember) (arbiter: StateArbiter) =
    Tracing.trace (tag "handleAddMember") <| fun () ->
      match addMembers state [| mem |] with
      | Right (entry, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> arbiter.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleAddMember")
        err
        |> RaftEvent.RaftError
        |> notify state.Subscriptions
        newstate

  // ** handleRemoveMember

  let private handleRemoveMember (state: RaftServerState) (id: Id) (arbiter: StateArbiter) =
    Tracing.trace (tag "handleRemoveMember") <| fun () ->
      match removeMember state id with
      | Right (entry, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> arbiter.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleRemoveMember")

        err
        |> RaftEvent.RaftError
        |> notify state.Subscriptions
        newstate

  // ** handleIsCommitted

  (*

  let private handleIsCommitted (state: RaftServerState) (ts: DateTime) (entry: EntryResponse) (chan: ReplyChan) (arbiter: StateArbiter) =
    Tracing.trace (tag "handleIsCommitted") <| fun () ->
      let result =
        Raft.responseCommitted entry
        |> runRaft state.Raft state.Callbacks

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
  *)

  // ** processRequest

  let private processRequest (data: RaftServerState) (raw: RawRequest) (arbiter: StateArbiter) =
    Tracing.trace (tag "processRequest") <| fun () ->
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
    Tracing.trace (tag "handleRawRequest") <| fun () ->
      match processRequest state raw arbiter with
      | Right newdata -> newdata
      | Left error ->
        error
        |> ErrorResponse
        |> Binary.encode
        |> RawResponse.fromRequest raw
        |> state.Server.Respond
        state

  // ** handleReqCommitted

  let private handleReqCommitted (state: RaftServerState) (ts: DateTime) (entry: EntryResponse) (raw: RawResponse) (arbiter: StateArbiter) =
    Tracing.trace (tag "handleReqCommitted") <| fun () ->
      let result =
        Raft.responseCommitted entry
        |> runRaft state.Raft state.Callbacks

      let delta = DateTime.Now - ts

      match result with
      | Right (true, newstate) ->
        state.Server.Respond raw

        delta
        |> fun delta -> delta.TotalMilliseconds
        |> sprintf "Entry took %fms to commit"
        |> Logger.debug "handleReqCommitted"

        updateRaft state newstate

      | Right (false, newstate) ->
        if int delta.TotalMilliseconds > Constants.COMMAND_TIMEOUT then
          "AppendEntry timed out"
          |> Error.asRaftError "handleReqCommitted"
          |> ErrorResponse
          |> Binary.encode
          |> fun body -> { raw with Body = body }
          |> state.Server.Respond

          delta
          |> fun delta -> delta.TotalMilliseconds
          |> sprintf "AppendEntry timed out: %f"
          |> Logger.debug "handleReqCommitted"

          updateRaft state newstate
        else
          (ts, entry, raw)
          |> Msg.ReqCommitted
          |> arbiter.Post
          updateRaft state newstate
      | Left (err, newstate) ->
        err
        |> ErrorResponse
        |> Binary.encode
        |> fun body -> { raw with Body = body }
        |> state.Server.Respond
        updateRaft state newstate

  // ** handleRawResponse

  let private handleRawResponse (state: RaftServerState) (raw: RawResponse) arbiter =
    match raw.Body |> Binary.decode with
    | Right response ->
      match response with
      | RequestVoteResponse     (sender, vote) -> processVoteResponse          state sender vote
      | AppendEntriesResponse   (sender, ar)   -> processAppendEntriesResponse state sender ar
      | InstallSnapshotResponse (sender, ar)   -> processSnapshotResponse      state sender ar
      | ErrorResponse            error         -> processErrorResponse         state error
      | _                                      -> state
    | Left error ->
      error
      |> string
      |> Logger.err (tag "handleRawRespose")
      state

  // ** handleStart

  let private handleStart (state: RaftServerState) (agent: StateArbiter) =
    // periodic function
    let interval = int state.Options.Raft.PeriodicInterval
    let periodic = startPeriodic interval agent
    notify state.Subscriptions RaftEvent.Started
    { state with Disposables = periodic :: state.Disposables }

  // ** loop

  let private loop (store: IRaftStore<RaftServerState>) (inbox: StateArbiter) =
    let rec act () =
      async {
        let! cmd = inbox.Receive()
        let state = store.State
        let newstate =
          Tracing.trace (tag "loop") <| fun () ->
            match cmd with
            | Msg.Start                         -> handleStart         state          inbox
            | Msg.Join        (ip, port)        -> handleJoin          state ip port
            | Msg.Leave                         -> handleLeave         state
            | Msg.Periodic                      -> handlePeriodic      state
            | Msg.ForceElection                 -> handleForceElection state
            | Msg.AddCmd             cmd        -> handleAddCmd        state cmd      inbox
            | Msg.AddMember          mem        -> handleAddMember     state mem      inbox
            | Msg.RemoveMember        id        -> handleRemoveMember  state id       inbox
            | Msg.RawRequest     request        -> handleRawRequest    state request  inbox
            | Msg.RawResponse   response        -> handleRawResponse   state response inbox
            | Msg.ReqCommitted (ts, entry, raw) -> handleReqCommitted state ts entry raw inbox
        store.Update newstate
        do! act ()
      }
    act ()

  //  ____        _     _ _
  // |  _ \ _   _| |__ | (_) ___
  // | |_) | | | | '_ \| | |/ __|
  // |  __/| |_| | |_) | | | (__
  // |_|    \__,_|_.__/|_|_|\___|

  [<RequireQualifiedAccess>]
  module RaftServer =

    // ** rand

    let private rand = System.Random()

    // ** initializeRaft

    let private initializeRaft (callbacks: IRaftCallbacks)
                              (state: RaftValue)
                              : Either<IrisError, RaftValue> =
      Tracing.trace (tag "initializeRaft") <| fun () ->
        raft {
          let term = term 0
          do! Raft.setTermM term
          let! num = Raft.numMembersM ()

          if num = 1 then
            do! Raft.setTimeoutElapsedM 0<ms>
            do! Raft.becomeLeader ()
          else
            // set the timeout to something random, to prevent split votes
            let timeout = 1<ms> * rand.Next(0, int state.ElectionTimeout)
            do! Raft.setTimeoutElapsedM timeout
            do! Raft.becomeFollower ()
        }
        |> runRaft state callbacks
        |> Either.mapError fst
        |> Either.map snd

    // ** create

    let create (config: IrisConfig) =
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

        let! (callbacks, raftState) = either {
            let! raftState = Persistence.getRaft config
            let callbacks = mkCallbacks raftState.Member.Id connections subscriptions
            let! initialized = initializeRaft callbacks raftState
            return callbacks, initialized
          }

        let store =
          { Status = ServiceStatus.Stopped
            Raft = raftState
            Options = config
            Callbacks = callbacks
            Server = Unchecked.defaultof<IBroker>
            Disposables = []
            Connections = connections
            Subscriptions = subscriptions }
          |> RaftStore.create

        let agent = new StateArbiter(loop store)

        return
          { new IRaftServer with
              member self.Start () =
                Tracing.trace (tag "Start") <| fun () -> either {
                  if store.State.Status = ServiceStatus.Stopped then
                    let frontend = Uri.raftUri raftState.Member

                    let backend =
                      raftState.Member.Id
                      |> string
                      |> Some
                      |> Uri.inprocUri Constants.RAFT_BACKEND_PREFIX

                    let! server = Broker.create {
                        Id = raftState.Member.Id
                        MinWorkers = 5uy
                        MaxWorkers = 20uy
                        Frontend = frontend
                        Backend = backend
                        RequestTimeout = uint32 Constants.REQ_TIMEOUT
                      }

                    let srvobs = server.Subscribe(Msg.RawRequest >> agent.Post)

                    Map.iter
                      (fun _ (peer: RaftMember) ->
                        if peer.Id <> raftState.Member.Id then
                          getConnection connections peer
                          |> ignore)
                      raftState.Peers

                    let update =
                      { store.State with
                          Server = server
                          Disposables = [ srvobs ] }

                    store.Update update
                    agent.Start()
                    agent.Post Msg.Start
                  else
                    return!
                      "Service already running or disposed"
                      |> Error.asRaftError (tag "Start")
                      |> Either.fail
                }

              member self.Member
                with get () = store.State.Raft.Member

              member self.MemberId
                with get () = store.State.Raft.Member.Id

              member self.Append cmd =
                cmd |> Msg.AddCmd |> agent.Post

              member self.Status
                with get () = store.State.Status

              member self.ForceElection () =
                agent.Post Msg.ForceElection

              member self.Periodic () =
                agent.Post Msg.Periodic

              member self.JoinCluster ip port =
                (ip, port) |> Msg.Join |> agent.Post

              member self.LeaveCluster () =
                agent.Post Msg.Leave

              member self.AddMember mem =
                mem |> Msg.AddMember |> agent.Post

              member self.RemoveMember id =
                id |> Msg.RemoveMember |> agent.Post

              member self.State
                with get () = store.State

              member self.Subscribe (callback: RaftEvent -> unit) =
                { new IObserver<RaftEvent> with
                    member self.OnCompleted() = ()
                    member self.OnError(error) = ()
                    member self.OnNext(value) = callback value }
                |> listener.Subscribe

              member self.Connections
                with get () = store.State.Connections

              member self.IsLeader
                with get () = Raft.isLeader store.State.Raft

              member self.Leader
                with get () = Raft.getLeader store.State.Raft

              member self.Dispose () =
                dispose agent
                dispose store.State
            }
      }

#endif

// * Playground

#if INTERACTIVE

#time "on"

type State = string list

type IStore =
  inherit IDisposable
  abstract Append: string -> unit
  abstract State: State
  abstract Clear: unit -> unit

module AsyncTests =
  open System

  type private ReplyChan = AsyncReplyChannel<State>

  type private Msg =
    | State  of ReplyChan
    | Append of string
    | Clear

  let private loop (inbox: MailboxProcessor<Msg>) =
    let rec impl (state: State) = async {
      let! msg = inbox.Receive()
      let newstate =
        match msg with
        | Append str -> str :: state
        | State chan -> chan.Reply state; state
        | Clear -> []
      return! impl newstate
    }
    impl []

  let create () =
    let mbp = MailboxProcessor<Msg>.Start(loop)
    { new IStore with
        member self.Append str = str |> Msg.Append |> mbp.Post
        member self.State
          with get () = mbp.PostAndReply(Msg.State)
        member self.Clear() = Msg.Clear |> mbp.Post
        member self.Dispose () = ()
      }

module HopacTests =

  open System
  open System.Collections.Generic
  open Hopac
  open Hopac.Infixes

  type private ReplyChan = IVar<State>

  type private Msg =
    | Append of string
    | State of ReplyChan
    | Clear

  let rec private loop (rcv: Ch<Msg>) (state: State) = job {
      let! msg = Ch.take rcv
      match msg with
      | Msg.Append str ->
        let newstate = str :: state
        return! loop rcv newstate
      | Msg.State chan ->
        do! IVar.fill chan state
        return! loop rcv state
      | Msg.Clear ->
        return! loop rcv []
    }

  let create () =
    let send = Ch()
    loop send [] |> Hopac.start
    { new IStore with
        member self.Append str = str |> Msg.Append |> Ch.give send |> Hopac.start
        member self.State
          with get () =
            let ivar = IVar()
            ivar |> Msg.State |> Ch.send send |> Hopac.queue
            ivar |> IVar.read |> Hopac.run
        member self.Clear() =
          Msg.Clear |> Ch.send send |> Hopac.queue
        member self.Dispose () = ()
      }

let asrv = AsyncTests.create ()

for n in 0 .. 4000000 do
  n |> string |> asrv.Append

asrv.Clear()
asrv.State

let hsrv = HopacTests.create ()

for n in 0 .. 4000000 do
  n |> string |> hsrv.Append

hsrv.Clear()
hsrv.State

// Thread-safe, multi-reader, single-writer state

type IState<'t when 't : not struct> =
  abstract State: 't
  abstract Update: 't -> unit

module IState =
  open System.Threading

  let create<'t when 't : not struct> (initial: 't) =
    let mutable state = initial

    { new IState<'t> with
        member self.State with get () = state
        member self.Update update =
          Interlocked.CompareExchange<'t>(&state, update, state)
          |> ignore }


type State = { i: int }

let mutable t = { i = 0 }

let a = { i = 1 }
let b = { i = 3 }

Interlocked.CompareExchange<State>(&t, b, t)

t

#endif
