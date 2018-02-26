(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace rec Disco.Raft

// * Imports

open System
open System.Net
open Disco.Core
open Disco.Serialization
open Aether
open Aether.Operators
open FlatBuffers

#if !FABLE_COMPILER && !DISCO_NODES

open SharpYaml.Serialization

#endif

// * Callback Interface

/// IRaftCallbacks are an abstraction layer to allow for separation of pure code and side effects
/// which occur in the RaftMonad, and to be able to modularize and test the monadic code without
/// actual IO.
///
/// A free monad might have also been a good choice, providing the possibility to swap out
/// interpreters in different contexts.

type IRaftCallbacks =

  /// Request a vote from given Raft server
  abstract member SendRequestVote: peer:RaftMember -> request:VoteRequest -> unit

  /// Send AppendEntries message to given server
  abstract member SendAppendEntries: peer:RaftMember -> request:AppendEntries -> unit

  /// Send InstallSnapshot command to given serve
  abstract member SendInstallSnapshot: peer:RaftMember -> request:InstallSnapshot -> unit

  /// given the current state of Raft, prepare and return a snapshot value of
  /// current application state
  abstract member PrepareSnapshot: current:RaftState -> Log option

  /// perist the given Snapshot value to disk. For safety reasons this MUST
  /// flush all changes to disk.
  abstract member PersistSnapshot: snapshot:LogEntry -> unit

  /// attempt to load a snapshot from disk. return None if no snapshot was found
  abstract member RetrieveSnapshot: unit  -> LogEntry option

  /// apply the given command to state machine
  abstract member ApplyLog: command:StateMachine -> unit

  /// a new server was added to the configuration
  abstract member MemberAdded: peer:RaftMember -> unit

  /// a new server was added to the configuration
  abstract member MemberUpdated: peer:RaftMember -> unit

  /// a server was removed from the configuration
  abstract member MemberRemoved: peer:RaftMember -> unit

  /// a cluster configuration transition was successfully applied
  abstract member Configured: members:RaftMember array  -> unit

  /// a cluster configuration transition was successfully applied
  abstract member JointConsensus: changes:ConfigChange array  -> unit

  /// the state of Raft itself has changed from old state to new given state
  abstract member StateChanged: oldstate:MemberState -> newstate:MemberState -> unit

  /// the leader node changed
  abstract member LeaderChanged: leader:MemberId option -> unit

  /// persist vote data to disk. For safety reasons this callback MUST flush
  /// the change to disk.
  abstract member PersistVote: peer:RaftMember option -> unit

  /// persist term data to disk. For safety reasons this callback MUST flush
  /// the change to disk>
  abstract member PersistTerm: term:Term -> unit

  /// persist an entry added to the log to disk. For safety reasons this
  /// callback MUST flush the change to disk.
  abstract member PersistLog: log:LogEntry -> unit

  /// persist the removal of the passed entry from the log to disk. For safety
  /// reasons this callback MUST flush the change to disk.
  abstract member DeleteLog: log:LogEntry -> unit

// * RaftStateYaml

type RaftStateYaml() =
  [<DefaultValue>] val mutable Member          : string
  [<DefaultValue>] val mutable Term            : Term
  [<DefaultValue>] val mutable Leader          : string
  [<DefaultValue>] val mutable VotedFor        : string
  [<DefaultValue>] val mutable ElectionTimeout : int
  [<DefaultValue>] val mutable RequestTimeout  : int
  [<DefaultValue>] val mutable MaxLogDepth     : int

// * RaftState

/// State to hold all Raft-specific fields. Progression of the Raft system is a series of incremental
/// changes to a value of this type.

type RaftState =
  { /// this server's own RaftMember information
    MemberId          : MemberId
    /// the server's current term, a monotonic counter for election cycles
    CurrentTerm       : Term
    /// tracks the current Leader Id, or None if there isn't currently a leader
    CurrentLeader     : MemberId option
    /// map of all known members in the cluster
    Peers             : Map<MemberId,RaftMember>
    /// map of the previous cluster configuration. set if currently in a configuration change
    OldPeers          : Map<MemberId,RaftMember> option
    /// the candidate this server voted for in its current term or None if it hasn't voted for any
    /// other member yet
    VotedFor          : MemberId option
    /// the replicated state machine command log
    Log               : Log
    /// index of latest log entry known to be committed
    CommitIndex       : Index
    /// index of latest log entry applied to state machine
    LastAppliedIdx    : Index
    /// amount of time left until a new election will be called
    TimeoutElapsed    : Timeout
    /// amount of time that needs to pass before a new election is called
    ElectionTimeout   : Timeout
    /// amount of time to pass until we consider requests to be failed
    RequestTimeout    : Timeout
    /// maximum log depth to reach before automatic snapshotting triggers
    MaxLogDepth       : int
    /// the log entry which has a voting configuration change, otherwise None
    ConfigChangeEntry : LogEntry option }

  // ** optics

  static member MemberId_ =
    (fun (rs:RaftState) -> rs.MemberId),
    (fun memberId (rs:RaftState) -> { rs with MemberId = memberId })

  static member CurrentTerm_ =
    (fun (rs:RaftState) -> rs.CurrentTerm),
    (fun currentTerm (rs:RaftState) -> { rs with CurrentTerm = currentTerm })

  static member CurrentLeader_ =
    (fun (rs:RaftState) -> rs.CurrentLeader),
    (fun currentLeader (rs:RaftState) -> { rs with CurrentLeader = currentLeader })

  static member Peers_ =
    (fun (rs:RaftState) -> rs.Peers),
    (fun peers (rs:RaftState) -> { rs with Peers = peers })

  static member OldPeers_ =
    (fun (rs:RaftState) -> rs.OldPeers),
    (fun oldPeers (rs:RaftState) -> { rs with OldPeers = oldPeers })

  static member VotedFor_ =
    (fun (rs:RaftState) -> rs.VotedFor),
    (fun votedFor (rs:RaftState) -> { rs with VotedFor = votedFor })

  static member Log_ =
    (fun (rs:RaftState) -> rs.Log),
    (fun log (rs:RaftState) -> { rs with Log = log })

  static member CommitIndex_ =
    (fun (rs:RaftState) -> rs.CommitIndex),
    (fun commitIndex (rs:RaftState) -> { rs with CommitIndex = commitIndex })

  static member LastAppliedIndex_ =
    (fun (rs:RaftState) -> rs.LastAppliedIdx),
    (fun lastAppliedIdx (rs:RaftState) -> { rs with LastAppliedIdx = lastAppliedIdx })

  static member TimeoutElapsed_ =
    (fun (rs:RaftState) -> rs.TimeoutElapsed),
    (fun timeoutElapsed (rs:RaftState) -> { rs with TimeoutElapsed = timeoutElapsed })

  static member ElectionTimeout_ =
    (fun (rs:RaftState) -> rs.ElectionTimeout),
    (fun electionTimeout (rs:RaftState) -> { rs with ElectionTimeout = electionTimeout })

  static member RequestTimeout_ =
    (fun (rs:RaftState) -> rs.RequestTimeout),
    (fun requestTimeout (rs:RaftState) -> { rs with RequestTimeout = requestTimeout })

  static member MaxLogDepth_ =
    (fun (rs:RaftState) -> rs.MaxLogDepth),
    (fun maxLogDepth (rs:RaftState) -> { rs with MaxLogDepth = maxLogDepth })

  static member ConfigChangeEntry_ =
    (fun (rs:RaftState) -> rs.ConfigChangeEntry),
    (fun configChangeEntry (rs:RaftState) -> { rs with ConfigChangeEntry = configChangeEntry })

  static member CurrentIndex_ = RaftState.Log_ >-> Log.Index_

  // ** ToString

  override self.ToString() =
    sprintf "Member              = %s
State             = %A
CurrentTerm       = %A
CurrentLeader     = %A
NumMembers        = %A
NumOldMembers     = %A
VotedFor          = %A
MaxLogDepth       = %A
CommitIndex       = %A
LastAppliedIdx    = %A
TimeoutElapsed    = %A
ElectionTimeout   = %A
RequestTimeout    = %A
ConfigChangeEntry = %s
"
      (self.Member.ToString())
      self.Member.State
      self.CurrentTerm
      self.CurrentLeader
      (Map.count self.Peers)
      (Option.map Map.count self.OldPeers)
      self.VotedFor
      self.MaxLogDepth
      self.CommitIndex
      self.LastAppliedIdx
      self.TimeoutElapsed
      self.ElectionTimeout
      self.RequestTimeout
      (if Option.isSome self.ConfigChangeEntry then
        Option.get self.ConfigChangeEntry |> string
       else Constants.EMPTY)

  // ** IsLeader

  member self.IsLeader =
    match self.CurrentLeader with
    | Some lid -> self.Member.Id = lid
    | _ -> false

  // ** Member

  member self.Member: RaftMember =
    match Map.tryFind self.MemberId self.Peers with
    | Some mem -> mem
    | None ->
      match Option.bind (Map.tryFind self.MemberId) self.OldPeers with
      | Some mem -> mem
      | None -> failwith "could not find current member in peers map."

  // ** ToYaml

  #if !FABLE_COMPILER && !DISCO_NODES

  member self.ToYaml() =
    let yaml = RaftStateYaml()
    yaml.Member <- string self.Member.Id
    yaml.Term <- self.CurrentTerm

    Option.map
      (fun leader -> yaml.Leader <- string leader)
      self.CurrentLeader
    |> ignore

    Option.map
      (fun voted -> yaml.VotedFor <- string voted)
      self.VotedFor
    |> ignore

    yaml.ElectionTimeout <- int self.ElectionTimeout
    yaml.RequestTimeout <- int self.RequestTimeout
    yaml.MaxLogDepth <- self.MaxLogDepth
    yaml

  // ** FromYaml

  static member FromYaml (yaml: RaftStateYaml): DiscoResult<RaftState> =
    result {
      let! id = DiscoId.TryParse yaml.Member

      let! leader =
        if isNull yaml.Leader
        then Ok None
        else DiscoId.TryParse yaml.Leader |> Result.map Some

      let! votedfor =
        if isNull yaml.VotedFor
        then Ok None
        else DiscoId.TryParse yaml.VotedFor |> Result.map Some

      let mem = Member.create id
      return {
        MemberId          = id
        CurrentTerm       = yaml.Term
        CurrentLeader     = leader
        Peers             = Map [ (id, mem) ]
        OldPeers          = None
        VotedFor          = votedfor
        Log               = Log.empty
        CommitIndex       = 0<index>
        LastAppliedIdx    = 0<index>
        TimeoutElapsed    = 0<ms>
        ElectionTimeout   = yaml.ElectionTimeout * 1<ms>
        RequestTimeout    = yaml.RequestTimeout * 1<ms>
        MaxLogDepth       = yaml.MaxLogDepth
        ConfigChangeEntry = None
      }
    }

  #endif

// * RaftState module

/// Pure functions to manipulate RaftState values. To work with Raft code, please create/use the
/// functions from the RaftMonad module.

[<RequireQualifiedAccess>]
module RaftState =

  // ** getters

  let self (state: RaftState) = state.Member
  let state = self >> Member.state
  let memberId = Optic.get RaftState.MemberId_
  let currentTerm = Optic.get RaftState.CurrentTerm_
  let currentLeader = Optic.get RaftState.CurrentLeader_
  let peers = Optic.get RaftState.Peers_
  let oldPeers = Optic.get RaftState.OldPeers_
  let votedFor = Optic.get RaftState.VotedFor_
  let log = Optic.get RaftState.Log_
  let commitIndex = Optic.get RaftState.CommitIndex_
  let lastAppliedIndex = Optic.get RaftState.LastAppliedIndex_
  let timeoutElapsed = Optic.get RaftState.TimeoutElapsed_
  let electionTimeout = Optic.get RaftState.ElectionTimeout_
  let requestTimeout = Optic.get RaftState.RequestTimeout_
  let maxLogDepth = Optic.get RaftState.MaxLogDepth_
  let configChangeEntry = Optic.get RaftState.ConfigChangeEntry_

  // ** setters

  let setMemberId = Optic.set RaftState.MemberId_
  let setCurrentTerm = Optic.set RaftState.CurrentTerm_
  let setCurrentLeader = Optic.set RaftState.CurrentLeader_
  let setPeers = Optic.set RaftState.Peers_
  let setOldPeers = Optic.set RaftState.OldPeers_
  let setVotedFor = Optic.set RaftState.VotedFor_
  let setLog = Optic.set RaftState.Log_
  let setCommitIndex = Optic.set RaftState.CommitIndex_
  let setLastAppliedIndex = Optic.set RaftState.LastAppliedIndex_
  let setTimeoutElapsed = Optic.set RaftState.TimeoutElapsed_
  let setElectionTimeout = Optic.set RaftState.ElectionTimeout_
  let setRequestTimeout = Optic.set RaftState.RequestTimeout_
  let setMaxLogDepth = Optic.set RaftState.MaxLogDepth_
  let setConfigChangeEntry = Optic.set RaftState.ConfigChangeEntry_

  // ** setSelf

  let setSelf (mem: RaftMember) state =
    state
    |> peers
    |> Map.add mem.Id mem
    |> flip setPeers state
    |> setMemberId mem.Id

  // ** setState

  let setState memState (state:RaftState) =
    state.Member
    |> Member.setState memState
    |> flip setSelf state

  // ** create

  let create (self: RaftMember) =
    { MemberId          = self.Id
      CurrentTerm       = 0<term>
      CurrentLeader     = None
      Peers             = Map.ofList [(self.Id, self)]
      OldPeers          = None
      VotedFor          = None
      Log               = Log.empty
      CommitIndex       = 0<index>
      LastAppliedIdx    = 0<index>
      TimeoutElapsed    = 0<ms>
      ElectionTimeout   = Constants.RAFT_ELECTION_TIMEOUT * 1<ms>
      RequestTimeout    = Constants.RAFT_REQUEST_TIMEOUT * 1<ms>
      MaxLogDepth       = Constants.RAFT_MAX_LOGDEPTH
      ConfigChangeEntry = None }

  // ** numMembers

  let numMembers = Optic.get RaftState.Peers_ >> Map.count

  // ** numOldMembers

  let numOldMembers =
    Optic.get RaftState.OldPeers_
    >> Option.map Map.count
    >> Option.defaultValue 0

  // ** currentIndex

  let currentIndex = Optic.get RaftState.CurrentIndex_

  // ** isFollower

  let isFollower (state:RaftState) = state.Member.State = Follower

  // ** isCandidate

  let isCandidate (state:RaftState) = state.Member.State = Candidate

  // ** isLeader

  let isLeader (state:RaftState) = state.Member.State = Leader

  // ** inJointConsensus

  let inJointConsensus = configChangeEntry >> function
    | Some (JointConsensus _) -> true
    | _                       -> false

  // ** hasNonVotingMembers

  let hasNonVotingMembers (state: RaftState) =
    Map.fold
      (fun b _ n ->
        if b then
          b
        else
          not (Member.hasSufficientLogs n && Member.isVoting n))
      false
      state.Peers

  // ** configurationChanges

  let configurationChanges = configChangeEntry >> function
    | Some (JointConsensus(_,_,_,changes,_)) -> Some changes
    | _ -> None

  // ** logicalPeers

  let logicalPeers (state: RaftState) =
    // when setting the NumMembers counter we have to include the old config
    if inJointConsensus state then
        // take the old peers as seed and apply the new peers on top
      match state.OldPeers with
      | Some peers -> Map.fold (fun m k n -> Map.add k n m) peers state.Peers
      | _ -> state.Peers
    else state.Peers

  // ** countMembers

  let countMembers peers = Map.count peers

  // ** numLogicalPeers

  let numLogicalPeers: RaftState -> int = logicalPeers >> countMembers

  // ** hasMember

  let hasMember nid = peers >> Map.containsKey nid

  // ** getMember

  let getMember (nid : MemberId) (state: RaftState) =
    if inJointConsensus state then
      state
      |> logicalPeers
      |> Map.tryFind nid
    else
      Map.tryFind nid state.Peers

  // ** updateMember

  /// Update a member in the RaftState. If it has structurally changed, the first part of the
  /// two-tuple returned indicates that a change occurred.

  let updateMember (mem: RaftMember) (state: RaftState) =
    let members = peers state
    // if we are in joint consensus, we must update the mem value in either the
    // new or the old configuration, or both.
    if inJointConsensus state then
      // first process the regular members
      let peersUpdated, peers =
        match Map.tryFind mem.Id members with
        | Some old when old <> mem -> true, Map.add mem.Id mem members
        | _ -> false, members
      // next, look at the old cluster configuration and update if changed
      let oldPeersUpdated, oldPeers =
        match state.OldPeers with
        | Some peers ->
          match Map.tryFind mem.Id peers with
          | Some old when old <> mem -> true, Some (Map.add mem.Id mem peers)
          | _ -> false, Some peers
        | _ ->
          // if OldPeers is empty, but there is a ConfigChangeEntry re-build the OldPeers
          match configurationChanges state with
          | Some changes ->
            let peers =
              Array.fold (fun m -> function
                | MemberAdded peer -> Map.remove peer.Id m // we must do the inverse operation here
                | MemberRemoved peer -> Map.add peer.Id peer m) /// and here too
                members
                changes
            if Map.containsKey mem.Id peers
            then true, Some (Map.add mem.Id mem peers)
            else false, Some peers
          | _ -> false, None
      let state =
        state
        |> setPeers peers
        |> setOldPeers oldPeers
      peersUpdated || oldPeersUpdated, state
    else
      let peersUpdated, peers =
        match Map.tryFind mem.Id members with
        | Some old when old <> mem -> true, Map.add mem.Id mem members
        | _ -> false, members
      peersUpdated, setPeers peers state

  // ** updateMembers

  let updateMembers (f: RaftMember -> RaftMember) state =
    state
    |> logicalPeers
    |> Map.fold
      (fun (current, state') _ mem ->
        let updated, state'' = updateMember (f mem) state'
        current || updated, state'')
      (false, state)

  // ** setNextIndex

  /// Set the nextIndex field on Member corresponding to supplied Id (should it exist, that is).

  let setNextIndex (nid : MemberId) idx (state: RaftState) =
    let mem = getMember nid state
    let nextIdx = if idx < 1<index> then 1<index> else idx
    match mem with
    | Some mem ->
      mem
      |> Member.setNextIndex nextIdx
      |> flip updateMember state
    | _ -> false, state

  // ** setMatchIndex

  /// Set the matchIndex field on Member to supplied index.

  let setMatchIndex nid idx (state: RaftState) =
    let mem = getMember nid state
    match mem with
    | Some peer ->
      peer
      |> Member.setMatchIndex idx
      |> flip updateMember state
    | _ -> false, state

  // ** setLeader

  /// Set States CurrentLeader field to supplied MemberId.

  let setLeader (leader : MemberId option) (state: RaftState) =
    if leader <> state.CurrentLeader then
      let peers =
        Map.map
          (fun id peer ->
            if Some id = leader
            then Member.setState Leader peer
            else Member.setState Follower peer)
          state.Peers
      let state =
        state
        |> setCurrentLeader leader
        |> setPeers peers
      true, state
    else false, state

  // ** voteFor

  let voteFor (mem: RaftMember option) =
    mem
    |> Option.map Member.id
    |> setVotedFor

  // ** setVoting

  let setVoting (mem: RaftMember) (vote: bool) =
    mem
    |> Member.setVotedForMe vote
    |> updateMember

  // ** addMember

  /// Adds a mem to the list of known Members and increments NumMembers counter

  let addMember (mem: RaftMember) state =
    state
    |> peers
    |> Map.add mem.Id mem
    |> flip setPeers state

  // ** addNonVotingMember

  /// Add a Non-voting Peer to the list of known Members

  let addNonVotingMember =
    Member.setVoting false >> Member.setStatus Joining >> addMember

  // ** removeMember

  /// Remove a Peer from the list of known Members and decrement NumMembers counter
  let removeMember (mem: RaftMember) (state: RaftState) =
    state
    |> peers
    |> Map.remove mem.Id
    |> flip setPeers state

  // ** applyChanges

  let applyChanges changes state =
    let folder _state = function
      | MemberAdded   mem -> addNonVotingMember mem _state
      | MemberRemoved mem -> removeMember       mem _state
    Array.fold folder state changes

  // ** addMembers

  let addMembers mems state =
    Map.fold (fun m _ n -> addMember n m) state mems

  // ** setMemberState

  let setMemberState mem memstate state =
    match getMember mem state with
    | Some mem ->
      mem
      |> Member.setState memstate
      |> flip updateMember state
    | _ -> false, state

  // ** resetVotes

  let resetVotes state =
    let reset = Map.map (fun _ -> Member.setVotedForMe false)
    { state with
        Peers = reset state.Peers
        OldPeers = Option.map reset state.OldPeers }

  // ** votedForMyself

  let votedForMyself (state: RaftState) =
    match state.VotedFor with
    | Some(nid) -> nid = state.Member.Id
    | _ -> false

  // ** votingMembersForConfig

  let votingMembersForConfig peers =
    let counter r _ n =
      if Member.isVoting n then r + 1 else r
    Map.fold counter 0 peers

  // ** votingMembers

  let votingMembers = peers >> votingMembersForConfig

  // ** votingMembersForOldConfig

  let votingMembersForOldConfig =
    oldPeers
    >> Option.map votingMembersForConfig
    >> Option.defaultValue 0

  // ** numLogs

  let numLogs = log >> Log.length

  // ** firstIndex

  let firstIndex term = log >> Log.firstIndex term

  // ** getLeader

  let getLeader state =
    state
    |> currentLeader
    |> Option.bind (flip getMember state)

  // ** requestTimedOut

  let requestTimedOut (state: RaftState) : bool =
    state.RequestTimeout <= state.TimeoutElapsed

  // ** electionTimedOut

  let electionTimedOut (state: RaftState) : bool =
    state.ElectionTimeout <= state.TimeoutElapsed

  // ** lastLogTerm

  let lastLogTerm = log >> Log.term

  // ** entryAt

  let entryAt idx = log >> Log.at idx

  // ** entriesUntil

  let entriesUntil idx = log >> Log.until idx

  // ** entriesUntilExcluding

  let entriesUntilExcluding idx = log >> Log.untilExcluding idx

  // ** updateCommitIndex

  let updateCommitIndex (state: RaftState) =
    setCommitIndex
      $ if numMembers state = 1
        then currentIndex state
        else commitIndex state
      $ state

  // ** calculateChanges

  let calculateChanges (oldPeers: Map<MemberId,RaftMember>) (newPeers: Map<MemberId,RaftMember>) =
    let oldmems = Map.toArray oldPeers |> Array.map snd
    let newmems = Map.toArray newPeers |> Array.map snd

    let additions =
      Array.fold
        (fun lst (newmem: RaftMember) ->
          match Array.tryFind (Member.id >> (=) newmem.Id) oldmems with
            | Some _ -> lst
            |      _ -> MemberAdded(newmem) :: lst) [] newmems

    Array.fold
      (fun lst (oldmem: RaftMember) ->
        match Array.tryFind (Member.id >> (=) oldmem.Id) newmems with
          | Some _ -> lst
          | _ -> MemberRemoved(oldmem) :: lst) additions oldmems
    |> List.toArray

  // ** majority

  /// Determine the majority from a total number of eligible voters and their respective votes. This
  /// function is generic and should expect any numeric types.
  ///
  /// Turning off the warning about the cast due to sufficiently constrained requirements on the
  /// input type (op_Explicit, comparison and division).
  ///
  /// ### Signature:
  /// - total: the total number of votes cast
  /// - yays: the number of yays in this election

  let majority total yays =
    if total = 0 || yays = 0 then
      false
    elif yays > total then
      false
    else
      yays > (total / 2)

  // ** numVotesForConfig

  let numVotesForConfig (self: RaftMember) (votedFor: MemberId option) peers =
    let counter m _ (peer : RaftMember) =
      if (peer.Id <> self.Id) && Member.canVote peer
        then m + 1
        else m

    let start =
      match votedFor with
      | Some(nid) -> if nid = self.Id then 1 else 0
      | _         -> 0

    Map.fold counter start peers

  // ** numVotesForMe

  let numVotesForMe (state: RaftState) =
    numVotesForConfig state.Member state.VotedFor state.Peers

  // ** numVotesForMeOldConfig

  let numVotesForMeOldConfig (state: RaftState) =
    match state.OldPeers with
    | Some peers -> numVotesForConfig state.Member state.VotedFor peers
    |      _     -> 0

  // ** handleConfiguration

  let handleConfiguration mems (state: RaftState) =
    let parting =
      mems
      |> Array.map Member.id
      |> Array.contains state.Member.Id
      |> not

    let peers =
      if parting
      // we have been kicked out of the configuration
      then Map [(state.Member.Id, state.Member)]
      // we are still part of the new cluster configuration
      else
        mems
        |> Array.map toPair
        |> Map.ofArray

    state
    |> RaftState.setPeers peers
    |> RaftState.setOldPeers None

  // ** handleJointConsensus

  let handleJointConsensus (changes) (state:RaftState) =
    state
    |> RaftState.applyChanges changes
    |> RaftState.setOldPeers (Some state.Peers)

  // ** selfIncluded

  let selfIncluded state = Map.containsKey state.MemberId state.Peers
