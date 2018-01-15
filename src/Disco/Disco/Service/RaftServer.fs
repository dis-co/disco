(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service


// * Imports

#if !DISCO_NODES

open System
open System.Threading
open System.Collections.Concurrent
open Disco.Net
open Disco.Core
open Disco.Core.Utils
open Disco.Service.Interfaces
open Disco.Raft
open Persistence

// * Raft

module rec RaftServer =

  // ** tag
  let private tag (str: string) = String.format "RaftServer.{0}" str

  // ** Connections

  type private Connections = ConcurrentDictionary<PeerId,ITcpClient>

  // ** Subscriptions

  type private Subscriptions = Observable.Subscriptions<DiscoEvent>

  // ** RaftServerState

  [<NoComparison;NoEquality>]
  type private RaftServerState =
    { Status:         ServiceStatus
      Raft:           RaftState
      Options:        DiscoConfig
      Callbacks:      IRaftCallbacks
      Server:         ITcpServer
      Disposables:    IDisposable list
      Connections:    ConcurrentDictionary<PeerId,ITcpClient>
      Subscriptions:  Subscriptions
      Started:        AutoResetEvent
      Stopped:        AutoResetEvent }

  // ** Msg

  [<RequireQualifiedAccess;NoComparison;NoEquality>]
  type private Msg =
    | Periodic
    | ForceElection
    | Start
    | Started
    | Stop
    | Stopped
    | Notify            of DiscoEvent
    | RawServerResponse of response:Response
    | ServerEvent       of ev:TcpServerEvent
    | ClientEvent       of ev:TcpClientEvent
    | AddCmd            of sm:StateMachine
    | AddMember         of mem:RaftMember
    | RemoveMember      of id:MemberId
    | ReqCommitted      of started:DateTime * entry:EntryResponse * response:Response
    // | Join           of ip:IpAddress * port:uint16
    // | Leave
    // | IsCommitted    of started:DateTime * entry:EntryResponse

    // *** ToString

    override msg.ToString() =
      match msg with
      | Start                     -> "Start"
      | Started                   -> "Started"
      | Stop                   _  -> "Stop"
      | Stopped                _  -> "Stopped"
      | Notify                 e  -> sprintf "Notify: %A" e
      | ServerEvent            _  -> "ServerEvent"
      | ClientEvent            _  -> "ClientEvent"
      | RawServerResponse      _  -> "RawServerResponse"
      | Periodic                  -> "Periodic"
      | ForceElection             -> "ForceElection"
      | AddCmd          sm        -> sprintf "AddCmd:  %A" sm
      | AddMember       mem       -> sprintf "AddMember:  %O" mem.Id
      | RemoveMember        id    -> sprintf "RemoveMember:  %O" id
      | ReqCommitted  (_,entry,_) -> sprintf "ReqCommitted:  %A" entry
      // | Join          (ip,port)   -> sprintf "Join: %s %d" (string ip) port
      // | Leave                     -> "Leave"
      // | IsCommitted   (_,entry)   -> sprintf "IsCommitted:  %A" entry

  // ** RaftAgent

  type private RaftAgent = IActor<Msg>

  // ** tryPost

  let private tryPost (agent: RaftAgent) msg =
    try agent.Post msg
    with exn ->
      exn
      |> string
      |> Logger.err (tag "tryPost")

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
    |> Member.id

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
  let private updateRaft (context: RaftServerState) (raft: RaftState) : RaftServerState =
    { context with Raft = raft }

  // ** makePeerSocket

  let private makePeerSocket (peer: RaftMember) =
    let socket = TcpClient.create {
      ClientId = peer.Id
      PeerAddress = peer.IpAddress
      PeerPort = peer.RaftPort
      Timeout = (int Constants.REQ_TIMEOUT) * 1<ms>
    }
    socket.Connect()
    Some socket

  // ** getPeerSocket

  let private getPeerSocket (connections: Connections) (peer: MemberId)  =
    match connections.TryGetValue peer with
    | true, connection -> Some connection
    | _ -> None

  // ** registerPeerSocket

  let private registerPeerSocket (agent: RaftAgent) (socket: ITcpClient) =
    socket.Subscribe (Msg.ClientEvent >> tryPost agent) |> ignore
    socket

  // ** addPeerSocket

  let private addPeerSocket (connections: Connections) (socket: ITcpClient) =
    match connections.TryAdd(socket.ClientId, socket) with
    | true -> ()
    | false ->
      match connections.TryAdd(socket.ClientId, socket) with
      | true -> ()
      | false ->
        "unable to add peer socket after 1 retry"
        |> Logger.err (tag "addPeerSocket")

  // ** handleNotify

  let private handleNotify (state: RaftServerState) (ev: DiscoEvent) =
    Observable.onNext state.Subscriptions ev
    if state.Raft.IsLeader then
      match ev with
      | DiscoEvent.EnterJointConsensus _ -> onConfigDone state
      | _ -> state
    else state

  // ** sendRequest

  let private sendRequest (peer: RaftMember) connections agent request =
    match peer.Id |> getPeerSocket connections with
    | Some connection -> performRequest request connection
    | None ->
      peer.Id
      |> sprintf "unable to find peer socket for %O. starting one.."
      |> Logger.debug (tag "sendRequest")
      let connection =
        peer
        |> makePeerSocket
        |> Option.map (registerPeerSocket agent)
      Option.iter (performRequest request) connection
      Option.iter (addPeerSocket connections) connection

  // ** makeCallbacks

  let private makeCallbacks (id: MemberId)
                            (connections: Connections)
                            (callbacks: IRaftSnapshotCallbacks)
                            (agent: RaftAgent) =

    { new IRaftCallbacks with
        member self.SendRequestVote peer request =
          Tracing.trace (tag "sendRequestVote") <| fun () ->
            RequestVote(id, request)
            |> sendRequest peer connections agent

        member self.SendAppendEntries peer request =
          Tracing.trace (tag "sendAppendEntries") <| fun () ->
            AppendEntries(id, request)
            |> sendRequest peer connections agent

        member self.SendInstallSnapshot peer request =
          Tracing.trace (tag "sendInstallSnapshot") <| fun () ->
            InstallSnapshot(id, request)
            |> sendRequest peer connections agent

        member self.PrepareSnapshot raft =
          Tracing.trace (tag "prepareSnapshot") <| fun () ->
            callbacks.PrepareSnapshot ()
            |> Option.map (DataSnapshot >> Raft.createSnapshot raft)

        member self.RetrieveSnapshot () =
          Tracing.trace (tag "retrieveSnapshot") <| fun () ->
            callbacks.RetrieveSnapshot()

        member self.PersistSnapshot log =
          Tracing.trace (tag "persistSnapshot") <| fun () ->
            log
            |> DiscoEvent.PersistSnapshot
            |> Msg.Notify
            |> agent.Post

        member self.ApplyLog cmd =
          Tracing.trace (tag "applyLog") <| fun () ->
            cmd
            |> DiscoEvent.appendRaft
            |> Msg.Notify
            |> agent.Post

        member self.MemberAdded mem =
          Tracing.trace (tag "memberAdded") <| fun () ->
            AddMember mem
            |> DiscoEvent.appendRaft
            |> Msg.Notify
            |> agent.Post

        member self.MemberUpdated mem =
          Tracing.trace (tag "memberUpdated") <| fun () ->
            UpdateMember mem
            |> DiscoEvent.appendRaft
            |> Msg.Notify
            |> agent.Post

        member self.MemberRemoved mem =
          Tracing.trace (tag "memberRemoved") <| fun () ->
            RemoveMember mem
            |> DiscoEvent.appendRaft
            |> Msg.Notify
            |> agent.Post

        member self.JointConsensus changes =
          Tracing.trace (tag "configured") <| fun () ->
            changes
            |> DiscoEvent.EnterJointConsensus
            |> Msg.Notify
            |> agent.Post

        member self.Configured mems =
          Tracing.trace (tag "configured") <| fun () ->
            let ids = Array.map Member.id mems
            let keys = connections.Keys
            for id in keys do
              if not (Array.contains id ids) then
                let result, connection = connections.TryRemove id
                if result then dispose connection
            mems
            |> DiscoEvent.ConfigurationDone
            |> Msg.Notify
            |> agent.Post

        member self.StateChanged oldstate newstate =
          Tracing.trace (tag "stateChanged") <| fun () ->
            (oldstate, newstate)
            |> DiscoEvent.StateChanged
            |> Msg.Notify
            |> agent.Post

        member self.LeaderChanged newleader =
          Tracing.trace (tag "leaderChanged") <| fun () ->
            newleader
            |> DiscoEvent.LeaderChanged
            |> Msg.Notify
            |> agent.Post

        member self.PersistVote mem =
          Tracing.trace (tag "persistVote") <| fun () ->
            ignore mem

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
            ignore term

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
            ignore log

        member self.DeleteLog log =
          Tracing.trace (tag "deleteLog") <| fun () ->
            ignore log
        }

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
      let result =
        state.Raft.Peers
        |> Map.toArray
        |> Array.map snd
        |> Log.mkConfig state.Raft.CurrentTerm
        |> appendEntry state
      match result with
      | Right (entry, newstate) ->
        entry.Id
        |> String.format "appended new Configuration in {0}"
        |> Logger.info (tag "onConfigDone")
        newstate
      | Left (error, newstate) ->
        error
        |> String.format "error appending new Configruation: {0}"
        |> Logger.err (tag "onConfigDone")
        newstate

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

  let private removeMember (state: RaftServerState) (id: MemberId) =
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

  let private processAppendEntries (state: RaftServerState)
                                   (sender: MemberId)
                                   (ae: AppendEntries)
                                   (raw: Request) =

    Tracing.trace (tag "processAppendEntries") <| fun () ->
      let result =
        Raft.receiveAppendEntries (Some sender) ae
        |> runRaft state.Raft state.Callbacks
      match result with
      | Right (response, newstate) ->
        (state.Raft.Member.Id, response)
        |> AppendEntriesResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

      | Left (err, newstate) ->
        (state.Raft.Member.Id, err)
        |> ErrorResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

  // ** processAppendEntry

  let private processAppendEntry (state: RaftServerState)
                                 (cmd: StateMachine)
                                 (raw: Request)
                                 (agent: RaftAgent) =
    Tracing.trace (tag "processAppendEntry") <| fun () ->
      if Raft.isLeader state.Raft then  // I'm leader, so I try to append command
        match appendCommand state cmd with
        | Right (entry, newstate) ->     // command was appended, now queue a message and the later
          entry                         // response to check its committed status, eventually
          |> AppendEntryResponse         // timing out or responding to the server
          |> Binary.encode
          |> Response.fromRequest raw
          |> fun response -> Msg.ReqCommitted(DateTime.Now, entry, response)
          |> agent.Post
          newstate
        | Left (err, newstate) ->        // Request was unsuccessful, respond immeditately
          (state.Raft.Member.Id, err)
          |> ErrorResponse
          |> Binary.encode
          |> Response.fromRequest raw
          |> state.Server.Respond
          newstate
      else
        match Raft.getLeader state.Raft with // redirect to known leader or fail
        | Some mem ->
          mem
          |> Redirect
          |> Binary.encode
          |> Response.fromRequest raw
          |> state.Server.Respond
          state
        | None ->
          "Not leader and no known leader."
          |> Error.asRaftError (tag "processAppendEntry")
          |> fun err -> ErrorResponse(state.Raft.Member.Id, err)
          |> Binary.encode
          |> Response.fromRequest raw
          |> state.Server.Respond
          state

  // ** processVoteRequest

  let private processVoteRequest (state: RaftServerState)
                                 (sender: MemberId)
                                 (vr: VoteRequest)
                                 (raw: Request) =
    Tracing.trace (tag "processVoteRequest") <| fun () ->
      let result =
        Raft.receiveVoteRequest sender vr
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        (state.Raft.Member.Id, response)
        |> RequestVoteResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

      | Left (err, newstate) ->
        (state.Raft.Member.Id, err)
        |> ErrorResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate

  // ** processInstallSnapshot

  let private processInstallSnapshot (state: RaftServerState) (is: InstallSnapshot) (raw: Request) =
    Tracing.trace (tag "processInstallSnapshot") <| fun () ->
      let result =
        Raft.receiveInstallSnapshot is
        |> runRaft state.Raft state.Callbacks

      match result with
      | Right (response, newstate) ->
        (state.Raft.Member.Id, response)
        |> InstallSnapshotResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        updateRaft state newstate
      | Left (error, newstate) ->
        (state.Raft.Member.Id, error)
        |> ErrorResponse
        |> Binary.encode
        |> Response.fromRequest raw
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
  /// Returns: Either<DiscoError,RaftResponse>
  let private doRedirect (state: RaftServerState) (raw: Request) =
    Tracing.trace (tag "doRedirect") <| fun () ->
      match Raft.getLeader state.Raft with
      | Some mem ->
        mem
        |> Redirect
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        state
      | None ->
        "No known leader"
        |> Error.asRaftError (tag "doRedirect")
        |> fun error -> ErrorResponse(state.Raft.Member.Id, error)
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        state

  // ** processHandshake

  (*

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
  let private processHandshake (state: RaftServerState) (mem: RaftMember) (raw: RawRequest) (agent: RaftAgent) =
    Tracing.trace (tag "processHandshake") <| fun () ->
      if Raft.isLeader state.Raft then
        match addMembers state [| mem |] with
        | Right (entry, newstate) ->
            let response =                  // response to check its committed status, eventually
              mem
              |> Welcome
              |> Binary.encode
              |> RawResponse.fromRequest raw
            (DateTime.Now, entry, response)
            |> Msg.ReqCommitted
            |> agent.Post
          newstate
        | Left (err, newstate) ->
            err
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          newstate
      else
        doRedirect state raw
  *)

  // ** processHandwaive

  (*

  let private processHandwaive (state: RaftServerState) (mem: RaftMember) (raw: RawRequest) (agent: RaftAgent) =
    Tracing.trace (tag "processHandwaive") <| fun () ->
      if Raft.isLeader state.Raft then
        match removeMember state mem.Id with
        | Right (entry, newstate) ->
            let response =                  // response to check its committed status, eventually
              Arrivederci
              |> Binary.encode
              |> RawResponse.fromRequest raw
            (DateTime.Now, entry, response)
            |> Msg.ReqCommitted
            |> agent.Post
          newstate
        | Left (err, newstate) ->
            err
            |> ErrorResponse
            |> Binary.encode
            |> RawResponse.fromRequest raw
            |> state.Server.Respond
          newstate
      else
        doRedirect state raw
  *)

  // ** processAppendEntriesResponse

  let private processAppendEntriesResponse (state: RaftServerState)
                                           (mem: MemberId)
                                           (ar: AppendResponse)
                                           (agent: RaftAgent) =
    let result =
      Raft.receiveAppendEntriesResponse mem ar
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate)  -> updateRaft state newstate
    | Left (err, newstate) ->
      err
      |> DiscoEvent.RaftError
      |> Msg.Notify
      |> agent.Post
      updateRaft state newstate

  // ** processVoteResponse

  let private processVoteResponse (state: RaftServerState)
                                  (sender: MemberId)
                                  (vr: VoteResponse)
                                  (agent: RaftAgent) =
    let result =
      Raft.receiveVoteResponse sender vr
      |> runRaft state.Raft state.Callbacks

    match result with
    | Right (_, newstate) -> updateRaft state newstate
    | Left (err, newstate) ->
      err
      |> DiscoEvent.RaftError
      |> Msg.Notify
      |> agent.Post
      updateRaft state newstate

  // ** processSnapshotResponse

  let private processSnapshotResponse (state: RaftServerState)
                                      (sender: MemberId)
                                      (ar: AppendResponse) =
    processAppendEntriesResponse state sender ar

  // ** processRedirect

  let private processRedirect (state: RaftServerState) (_: RaftMember) =
    "FIX REDIRECT RESPONSE PROCESSING"
    |> Logger.err (tag "processRedirect")
    state

  // ** processWelcome

  (*

  let private processWelcome (state: RaftServerState) (leader: RaftMember) =

    "FIX WELCOME RESPONSE PROCESSING"
    |> Logger.err (tag "processWelcome")

    state
  *)

  // ** processArrivederci

  (*

  let private processArrivederci (state: RaftServerState) =
    "FIX ARRIVEDERCI RESPONSE PROCESSING"
    |> Logger.err (tag "processArrivederci")
    state
  *)

  // ** processErrorResponse

  let private processErrorResponse (state: RaftServerState)
                                   (_: MemberId)
                                   (error: DiscoError) =
    error
    |> sprintf "received error response:  %A"
    |> Logger.err (tag "processErrorResponse")
    state

  // ** tryJoin

  (*

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
  *)

  // ** tryJoinCluster

  (*

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
  *)

  // ** tryLeave

  (*

  /// ## Attempt to leave a Raft cluster
  ///
  /// Attempt to leave a Raft cluster by identifying the current cluster leader and sending an
  /// AppendEntries request with a JointConsensus entry.
  ///
  /// ### Signature:
  /// - appState: RaftServerState TVar
  ///
  /// Returns: unit
  let private tryLeave (state: RaftServerState) : Either<DiscoError,bool> =
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
  *)

  // ** leaveCluster

  (*

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
  *)

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
  /// - timeout: interval at which the loop runs
  /// - appState: current RaftServerState TVar
  ///
  /// Returns: CancellationTokenSource
  let private startPeriodic (interval: int) (agent: RaftAgent) : IDisposable =
    Periodically.run interval <| fun () ->
      agent.Post(Msg.Periodic)

  // ** handleJoin

  (*

  let private handleJoin (state: RaftServerState) (ip: IpAddress) (port: UInt16) =
    Tracing.trace (tag "handleJoin") <| fun () ->
      match tryJoinCluster state ip port with
      | Right (_, newstate) ->
        notify state.Subscriptions DiscoEvent.JoinedCluster
        updateRaft state newstate
      | Left (error, newstate) ->
        error
        |> DiscoEvent.RaftError
        |> notify state.Subscriptions
        updateRaft state newstate
  *)

  // ** handleLeave

  (*

  let private handleLeave (state: RaftServerState) =
    Tracing.trace (tag "handleLeave") <| fun () ->
      match tryLeaveCluster state with
      | Right (_, newstate) ->
        notify state.Subscriptions DiscoEvent.LeftCluster
        updateRaft state newstate

      | Left (error, newstate) ->
        error
        |> string
        |> Logger.err (tag "handleLeave")
        error
        |> DiscoEvent.RaftError
        |> notify state.Subscriptions
        updateRaft state newstate
  *)

  // ** handleForceElection

  let private handleForceElection (state: RaftServerState) (agent: RaftAgent) =
    Tracing.trace (tag "handleForceElection") <| fun () ->
      match forceElection state with
      | Right (_, newstate) -> updateRaft state newstate
      | Left (err, newstate) ->
        err
        |> sprintf "Unable to force an election:  %A"
        |> Logger.err (tag "handleForceElection")

        err
        |> DiscoEvent.RaftError
        |> Msg.Notify
        |> agent.Post

        updateRaft state newstate

  // ** handleAddCmd

  let private handleAddCmd (state: RaftServerState) (agent: RaftAgent) (cmd: StateMachine) =
    Tracing.trace (tag "handleAddCmd") <| fun () ->
      match appendCommand state cmd with
      | Right (_, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> agent.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleAddCmd")
        err
        |> DiscoEvent.RaftError
        |> Msg.Notify
        |> agent.Post
        newstate

  // ** handlePeriodic

  let private handlePeriodic (state: RaftServerState) =
    Tracing.trace (tag "handlePeriodic") <| fun () ->
      int state.Options.Raft.PeriodicInterval * 1<ms>
      |> Raft.periodic
      |> evalRaft state.Raft state.Callbacks
      |> updateRaft state

  // ** handleAddMember

  let private handleAddMember (state: RaftServerState) (agent: RaftAgent) (mem: RaftMember) =
    Tracing.trace (tag "handleAddMember") <| fun () ->
      mem
      |> makePeerSocket
      |> Option.map (registerPeerSocket agent)
      |> Option.iter (addPeerSocket state.Connections)
      match addMembers state [| mem |] with
      | Right (_, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> agent.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleAddMember")
        err
        |> DiscoEvent.RaftError
        |> Msg.Notify
        |> agent.Post
        newstate

  // ** handleRemoveMember

  let private handleRemoveMember (state: RaftServerState)
                                 (agent: RaftAgent)
                                 (id: MemberId) =
    Tracing.trace (tag "handleRemoveMember") <| fun () ->
      match removeMember state id with
      | Right (_, newstate) ->
        // (DateTime.Now, entry)
        // |> Msg.IsCommitted
        // |> agent.Post
        newstate

      | Left (err, newstate) ->
        err
        |> string
        |> Logger.err (tag "handleRemoveMember")

        err
        |> DiscoEvent.RaftError
        |> Msg.Notify
        |> agent.Post
        newstate

  // ** handleIsCommitted

  (*

  let private handleIsCommitted (state: RaftServerState) (ts: DateTime) (entry: EntryResponse) (agent: RaftAgent) (chan: ReplyChan) =
    Tracing.trace (tag "handleIsCommitted") <| fun () ->
      let result =
        Raft.responseCommitted entry
        |> runRaft state.Raft state.Callbacks

      let delta = DateTime.Now - ts

      match result with
      | Right (true, newstate) ->        // the entry was committed, hence we reply to the caller
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
            |> agent.Post
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

  let private processRequest (data: RaftServerState) (agent: RaftAgent) (raw: Request) =
    Tracing.trace (tag "processRequest") <| fun () ->
      either {
        let! request = Binary.decode<RaftRequest> raw.Body
        let newstate =
          match request with
          | AppendEntries (id, ae)  -> processAppendEntries   data id  ae  raw
          | RequestVote (id, vr)    -> processVoteRequest     data id  vr  raw
          | InstallSnapshot (_, is) -> processInstallSnapshot data     is  raw
          | AppendEntry  sm         -> processAppendEntry     data sm  raw agent
          // | HandShake mem        -> processHandshake       data mem raw agent
          // | HandWaive mem        -> processHandwaive       data mem raw agent
        return newstate
      }

  // ** handleServerRequest

  let private handleServerRequest (state: RaftServerState) (raw: Request) agent =
    Tracing.trace (tag "handleServerRequest") <| fun () ->
      match processRequest state agent raw with
      | Right newdata -> newdata
      | Left error ->
        (state.Raft.Member.Id, error)
        |> ErrorResponse
        |> Binary.encode
        |> Response.fromRequest raw
        |> state.Server.Respond
        state

  // ** handleServerEvent

  let private handleServerEvent state agent = function
    | TcpServerEvent.Connect(_, ip, port) ->
      sprintf "new connection from %O:%d" ip port
      |> Logger.debug (tag "handleServerEvent")
      state

    | TcpServerEvent.Disconnect(peer) ->
      sprintf "%O disconnected" peer
      |> Logger.debug (tag "handleServerEvent")
      state

    | TcpServerEvent.Request request ->
      handleServerRequest state request agent

    | TcpServerEvent.Response _ -> state

  // ** handleReqCommitted

  let private handleReqCommitted (state: RaftServerState)
                                 (agent: RaftAgent)
                                 (ts: DateTime)
                                 (entry: EntryResponse)
                                 (raw: Response) =

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
        |> Logger.debug (tag "handleReqCommitted")

        updateRaft state newstate

      | Right (false, newstate) ->
        if int delta.TotalMilliseconds > Constants.COMMAND_TIMEOUT then
          "AppendEntry timed out"
          |> Error.asRaftError "handleReqCommitted"
          |> fun error -> ErrorResponse(state.Raft.Member.Id, error)
          |> Binary.encode
          |> Response.create raw.RequestId raw.PeerId
          |> state.Server.Respond

          delta
          |> fun delta -> delta.TotalMilliseconds
          |> sprintf "AppendEntry timed out: %f"
          |> Logger.debug (tag "handleReqCommitted")
          updateRaft state newstate
        else
          (ts, entry, raw)
          |> Msg.ReqCommitted
          |> agent.Post
          updateRaft state newstate
      | Left (err, newstate) ->
        (state.Raft.Member.Id, err)
        |> ErrorResponse
        |> Binary.encode
        |> Response.create raw.RequestId raw.PeerId
        |> state.Server.Respond
        updateRaft state newstate

  // ** handleServerResponse

  let private handleServerResponse (state: RaftServerState) agent (raw: Response) =
    match Binary.decode raw.Body with
    | Right response ->
      match response with
      | RequestVoteResponse (sender, vote)   -> processVoteResponse state sender vote agent
      | AppendEntriesResponse (sender, ar)   -> processAppendEntriesResponse state sender ar agent
      | InstallSnapshotResponse (sender, ar) -> processSnapshotResponse state sender ar agent
      | ErrorResponse (sender, error)        -> processErrorResponse state sender error
      | _ -> state
    | Left error ->
      error
      |> string
      |> Logger.err (tag "handleRawRespose")
      state

  // ** handleClientState

  let private handleClientState (state: RaftServerState)
                                (id: MemberId)
                                raftState =
    raft {
      let! peer = Raft.getMemberM id
      match peer with
      | Some mem -> do! Raft.updateMemberM { mem with Status = raftState }
      | None -> ()
    }
    |> runRaft state.Raft state.Callbacks
    |> function
      | Right (_, newstate) -> updateRaft state newstate
      | Left (err,_) ->
        err
        |> String.format "Could not set new state on member: {0}"
        |> Logger.err (tag "handleClientState")
        state

  // ** handleClientResponse

  let private handleClientResponse (state: RaftServerState) (raw: Response) agent =
    match raw.Body |> Binary.decode with
    | Right (AppendEntryResponse entry) ->
      // FIXME:
      // this will likely take some more thought and handling
      sprintf "successfully appended entry in %O" entry.Id
      |> Logger.debug (tag "handleClientResponse")
      state
    | Right (AppendEntriesResponse(id, ar))   -> processAppendEntriesResponse state id ar agent
    | Right (RequestVoteResponse(id, vr))     -> processVoteResponse state id vr agent
    | Right (InstallSnapshotResponse(id, ar)) -> processSnapshotResponse state id ar agent
    | Right (ErrorResponse(id, error))        -> processErrorResponse state id error
    | Right (Redirect leader)                 -> processRedirect state leader
    | Left error ->
      error
      |> sprintf "Error decoding response: %O"
      |> Logger.err (tag "handleClientResponse")
      state

  // ** handleClientEvent

  let private handleClientEvent state agent = function
    | TcpClientEvent.Response response     -> handleClientResponse state response agent
    | TcpClientEvent.Request  _            -> state // in raft we do only unidirection com
    | TcpClientEvent.Connected peer        -> handleClientState state peer MemberStatus.Running
    | TcpClientEvent.Disconnected(peer, _) -> handleClientState state peer MemberStatus.Failed

  // ** handleStop

  let private handleStop (state: RaftServerState) (agent: RaftAgent) =
    agent.Post Msg.Stopped
    { state with Status = ServiceStatus.Stopping }

  // ** handleStopped

  let private handleStopped (state: RaftServerState) =
    state.Stopped.Set() |> ignore
    state

  // ** initializeRaft

  let private initializeRaft (callbacks: IRaftCallbacks) (state: RaftState)  =
    Tracing.trace (tag "initializeRaft") <| fun () ->
      let rand = System.Random()
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

  // ** handleStart

  let private handleStart (state: RaftServerState) (agent: RaftAgent) =
    match initializeRaft state.Callbacks state.Raft with
    | Right initialized ->
      // periodic function
      let interval = int state.Options.Raft.PeriodicInterval
      let periodic = startPeriodic interval agent
      ServiceType.Raft |> DiscoEvent.Started |> Msg.Notify |> agent.Post
      agent.Post Msg.Started
      { state with
          Status = ServiceStatus.Running
          Raft = initialized
          Disposables = periodic :: state.Disposables }
    | Left error ->
      sprintf "Fatal, could not initialize Raft: %O" error
      |> Logger.err (tag "handleStart")
      agent.Post Msg.Started
      { state with Status = ServiceStatus.Failed error  }

  // ** handleStarted

  let private handleStarted (state: RaftServerState) =
    state.Started.Set() |> ignore
    state

  // ** loop

  let private loop (store: IAgentStore<RaftServerState>) (inbox: RaftAgent) cmd =
    async {
      try
        let state = store.State
        let newstate =
          match cmd with
          | Msg.Start                         -> handleStart          state inbox
          | Msg.Started                       -> handleStarted        state
          | Msg.Stop                          -> handleStop           state inbox
          | Msg.Stopped                       -> handleStopped        state
          | Msg.Notify              ev        -> handleNotify         state ev
          | Msg.Periodic                      -> handlePeriodic       state
          | Msg.ForceElection                 -> handleForceElection  state inbox
          | Msg.AddCmd             cmd        -> handleAddCmd         state inbox cmd
          | Msg.AddMember          mem        -> handleAddMember      state inbox mem
          | Msg.RemoveMember        id        -> handleRemoveMember   state inbox id
          | Msg.ServerEvent         ev        -> handleServerEvent    state inbox ev
          | Msg.ClientEvent         ev        -> handleClientEvent    state inbox ev
          | Msg.RawServerResponse   response  -> handleServerResponse state inbox response
          | Msg.ReqCommitted (ts, entry, raw) -> handleReqCommitted   state inbox ts entry raw
          // | Msg.Join        (ip, port)        -> handleJoin          state ip port
          // | Msg.Leave                         -> handleLeave         state

        // once we received the signal to stop we don't allow any more updates to the state to get
        // a consistent result in the Dispose method (due to possibly queued up messages on the
        // actors queue)
        if not (Service.isStopping newstate.Status) then
          store.Update newstate
      with exn ->
        String.Format(
          "Message: {0}\nStackTrace: {1}\nInner Message: {2}\n Inner StackTrace: {3}",
          exn.Message,
          exn.StackTrace,
          exn.InnerException.Message,
          exn.InnerException.StackTrace)
        |> Logger.err (tag "loop")
    }

  // ** startMetrics

  let private startMetrics (agent:RaftAgent) =
    Periodically.run 1000 <| fun () ->
      let count = agent.CurrentQueueLength
      do Metrics.collect "raft_agent_message_count" count

  // ** create

  let create (config: DiscoConfig) callbacks =
    either {
      let cts = new CancellationTokenSource()
      let connections = new Connections()
      let store = AgentStore.create()

      let agent = Actor.create (loop store)

      let! raftState = Persistence.getRaft config
      let callbacks =
        makeCallbacks
          raftState.Member.Id
          connections
          callbacks
          agent

      store.Update
        { Status = ServiceStatus.Stopped
          Server = Unchecked.defaultof<ITcpServer>
          Raft = raftState
          Options = config
          Callbacks = callbacks
          Disposables = []
          Connections = connections
          Subscriptions = new Subscriptions()
          Started = new AutoResetEvent(false)
          Stopped = new AutoResetEvent(false) }

      return
        { new IRaftServer with
            member self.Start () =
              Tracing.trace (tag "Start") <| fun () ->
                if store.State.Status = ServiceStatus.Stopped then
                  let server = TcpServer.create {
                    ServerId = raftState.Member.Id
                    Listen = raftState.Member.IpAddress
                    Port = raftState.Member.RaftPort
                  }

                  // we must start the agent, so the dispose logic will work as expected
                  do agent.Start()
                  let metrics = startMetrics agent

                  match server.Start() with
                  | Right () ->
                    let srvobs = server.Subscribe(Msg.ServerEvent >> agent.Post)

                    Map.iter
                      (fun _ (peer: RaftMember) ->
                        if peer.Id <> raftState.Member.Id then
                          peer.Id
                          |> sprintf "adding peer socket for %O"
                          |> Logger.debug (tag "Start")
                          peer
                          |> makePeerSocket
                          |> Option.map (registerPeerSocket agent)
                          |> Option.iter (addPeerSocket connections))
                      raftState.Peers

                    store.Update
                      { store.State with
                          Server = server
                          Disposables = [ srvobs; metrics ] }

                    agent.Post Msg.Start // kick it off

                    let result = store.State.Started.WaitOne(TimeSpan.FromMilliseconds 1000.0)

                    if result then
                      match store.State.Status with
                      | ServiceStatus.Failed error ->
                        Either.fail error
                      | _ -> Either.succeed ()
                    else
                      "Timeout waiting for started signal"
                      |> Error.asRaftError (tag "Start")
                      |> Either.fail
                  | Left error ->
                    error
                    |> sprintf "error starting broker: %O"
                    |> Logger.err (tag "Start")
                    store.Update { store.State with Status = ServiceStatus.Failed error }
                    Either.fail error
                else
                  sprintf "Status error. %O" store.State.Status
                  |> Error.asRaftError (tag "Start")
                  |> Either.fail

            member self.Raft
              with get () = store.State.Raft

            member self.Member
              with get () = store.State.Raft.Member

            member self.MemberId
              with get () = store.State.Raft.Member.Id

            member self.Append cmd =
              cmd |> Msg.AddCmd |> agent.Post

            member self.Publish cmd =
              match cmd with
              | DiscoEvent.Append(_, cmd) -> self.Append cmd
              | _ -> ()

            member self.Status
              with get () = store.State.Status

            member self.ForceElection () =
              agent.Post Msg.ForceElection

            member self.Periodic () =
              agent.Post Msg.Periodic

            // member self.JoinCluster ip port =
            //   (ip, port) |> Msg.Join |> agent.Post

            // member self.LeaveCluster () =
            //   agent.Post Msg.Leave

            member self.AddMember mem =
              mem |> Msg.AddMember |> agent.Post

            member self.RemoveMember id =
              id |> Msg.RemoveMember |> agent.Post

            member self.Subscribe (callback: DiscoEvent -> unit) =
              Observable.subscribe callback store.State.Subscriptions

            member self.Connections
              with get () = store.State.Connections

            member self.IsLeader
              with get () = Raft.isLeader store.State.Raft

            member self.RaftState
              with get () = store.State.Raft.State

            member self.Leader
              with get () = Raft.getLeader store.State.Raft

            member self.Dispose () =
              if not (Service.isDisposed store.State.Status) then
                // tell the loop to settle and eventually stop processing state updates
                agent.Post Msg.Stop

                // dispose periodic functions
                for disp in store.State.Disposables do
                  dispose disp

                let result = store.State.Stopped.WaitOne(TimeSpan.FromMilliseconds 3000.0)

                if not result then
                  Logger.err (tag "Dispose") "timeout waiting for stop to complete"

                // clear all listener subscriptions
                store.State.Subscriptions.Clear()

                for KeyValue(_,client) in store.State.Connections do
                  dispose client

                // stop the actor
                try cts.Cancel()
                with | exn -> Logger.err (tag "Dispose") exn.Message
                tryDispose agent ignore // then stop the actor so it doesn't keep processing
                tryDispose cts ignore   // buffered msgs

                // clear connections
                self.Connections.Clear()

                tryDispose store.State.Server <| fun exn ->
                  exn.Message
                  |> sprintf "error disposing server: %s"
                  |> Logger.err (tag "Dispose")

                // mark as disposed
                store.Update { store.State with Status = ServiceStatus.Disposed }
          }
    }

#endif
