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
open FlatBuffers

#if !FABLE_COMPILER && !DISCO_NODES

open SharpYaml.Serialization

#endif

// * EntryResponse

/// Response to an AppendEntry request
///
/// ## Constructor:
///  - `Id`    - the generated unique identified for the entry
///  - `Term`  - the entry's term
///  - `Index` - the entry's index in the log
type EntryResponse =
  {  Id    : LogId
     Term  : Term
     Index : Index }

  // ** ToString
  override self.ToString() =
    sprintf "Entry added with Id: %A in term: %d at log index: %d"
      (string self.Id)
      self.Term
      self.Index

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = EntryResponseFB.CreateIdVector(builder,self.Id.ToByteArray())
    EntryResponseFB.StartEntryResponseFB(builder)
    EntryResponseFB.AddId(builder, id)
    EntryResponseFB.AddTerm(builder, int self.Term)
    EntryResponseFB.AddIndex(builder, int self.Index)
    EntryResponseFB.EndEntryResponseFB(builder)

  static member FromFB(fb: EntryResponseFB) =
    either {
      let! id = Id.decodeId fb
      return {
        Id = id
        Term = term fb.Term
        Index = index fb.Index
      }
    }

// * Entry

[<RequireQualifiedAccess>]
module Entry =
  // ** id
  let inline id    (er : EntryResponse) = er.Id

  // ** term
  let inline term  (er : EntryResponse) = er.Term

  // ** index
  let inline index (er : EntryResponse) = er.Index

// * VoteRequest

// __     __    _       ____                            _
// \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_
//  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __|
//   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_
//    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__|
//                                   |_|

/// Request to Vote for a new Leader
///
/// ## Vote:
///  - `Term`         -  the current term, to force any other leader/candidate to step down
///  - `Candidate`    -  the unique mem id of candidate for leadership
///  - `LastLogIndex` -  the index of the candidates last log entry
///  - `LastLogTerm`  -  the index of the candidates last log entry
type VoteRequest =
  { Term         : Term
    Candidate    : RaftMember
    LastLogIndex : Index
    LastLogTerm  : Term }

  // ** ToOffset
  member self.ToOffset(builder: FlatBufferBuilder) =
    let mem = self.Candidate.ToOffset(builder)
    VoteRequestFB.StartVoteRequestFB(builder)
    VoteRequestFB.AddTerm(builder, int self.Term)
    VoteRequestFB.AddLastLogTerm(builder, int self.LastLogTerm)
    VoteRequestFB.AddLastLogIndex(builder, int self.LastLogIndex)
    VoteRequestFB.AddCandidate(builder, mem)
    VoteRequestFB.EndVoteRequestFB(builder)

  // ** FromFB
  static member FromFB (fb: VoteRequestFB) : Either<DiscoError, VoteRequest> =
    either {
      let candidate = fb.Candidate
      if candidate.HasValue then
        let! mem = RaftMember.FromFB candidate.Value
        return { Term         = term fb.Term
                 Candidate    = mem
                 LastLogIndex = index fb.LastLogIndex
                 LastLogTerm  = term fb.LastLogTerm }
      else
        return!
          "Could not parse empty MemberFB"
          |> Error.asParseError "VoteRequest.FromFB"
          |> Either.fail
    }

// * VoteResponse

// __     __    _       ____
// \ \   / /__ | |_ ___|  _ \ ___  ___ _ __   ___  _ __  ___  ___
//  \ \ / / _ \| __/ _ \ |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//   \ V / (_) | ||  __/  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
//    \_/ \___/ \__\___|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//                                    |_|

/// Result of a vote
///
/// ## Result:
///  - `Term`    - current term for candidate to apply
///  - `Granted` - result of vote
type VoteResponse =
  { Term    : Term
    Granted : bool
    Reason  : DiscoError option }

  // ** FromFB
  static member FromFB (fb: VoteResponseFB) : Either<DiscoError, VoteResponse> =
    either {
      let! reason =
        let reason = fb.Reason
        if reason.HasValue then
          DiscoError.FromFB reason.Value
          |> Either.map Some
        else
          Right None

      return { Term    = term fb.Term
               Granted = fb.Granted
               Reason  = reason }
    }

  // ** ToOffset
  member self.ToOffset(builder: FlatBufferBuilder) =
    let err = Option.map (fun (r: DiscoError) -> r.ToOffset(builder)) self.Reason
    VoteResponseFB.StartVoteResponseFB(builder)
    VoteResponseFB.AddTerm(builder, int self.Term)
    match err with
      | Some offset -> VoteResponseFB.AddReason(builder, offset)
      | _ -> ()
    VoteResponseFB.AddGranted(builder, self.Granted)
    VoteResponseFB.EndVoteResponseFB(builder)


// * module Vote
[<RequireQualifiedAccess>]
module Vote =

  // ** term
  let inline term         (vote : VoteRequest) = vote.Term

  // ** candiate
  let inline candidate    (vote : VoteRequest) = vote.Candidate

  // ** lastLogIndex
  let inline lastLogIndex (vote : VoteRequest) = vote.LastLogIndex

  // ** lastLogTerm
  let inline lastLogTerm  (vote : VoteRequest) = vote.LastLogTerm

  // ** granted
  let inline granted  (vote : VoteResponse) = vote.Granted

  // ** declined
  let inline declined (vote : VoteResponse) = not vote.Granted


// * AppendEntries

//     _                               _ _____       _        _
//    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
//   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
//  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
// /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
//         |_|   |_|

/// AppendEntries message.
///
/// This message is used to tell mems if it's safe to apply entries to the
/// FSM. Can be sent without any entries as a keep alive message.  This
/// message could force a leader/candidate to become a follower.
///
/// ## Message:
///  - `Term`        - currentTerm, to force other leader/candidate to step down
///  - `PrevLogIdx`  - the index of the log just before the newest entry for the mem who receive this message
///  - `PrevLogTerm` - the term of the log just before the newest entry for the mem who receives this message
///  - `LeaderCommit`- the index of the entry that has been appended to the majority of the cluster. Entries up to this index will be applied to the FSM
type AppendEntries =
  { Term         : Term
    PrevLogIdx   : Index
    PrevLogTerm  : Term
    LeaderCommit : Index
    Entries      : RaftLogEntry option }

  // ** optics

  static member Term_ =
    (fun (ae:AppendEntries) -> ae.Term),
    (fun term (ae:AppendEntries) -> { ae with Term = term })

  static member PrevLogIdx_ =
    (fun (ae:AppendEntries) -> ae.PrevLogIdx),
    (fun prevLogIdx (ae:AppendEntries) -> { ae with PrevLogIdx = prevLogIdx })

  static member PrevLogTerm_ =
    (fun (ae:AppendEntries) -> ae.PrevLogTerm),
    (fun prevLogTerm (ae:AppendEntries) -> { ae with PrevLogTerm = prevLogTerm })

  static member LeaderCommit_ =
    (fun (ae:AppendEntries) -> ae.LeaderCommit),
    (fun leaderCommit (ae:AppendEntries) -> { ae with LeaderCommit = leaderCommit })

  static member Entries_ =
    (fun (ae:AppendEntries) -> ae.Entries),
    (fun entries (ae:AppendEntries) -> { ae with Entries = entries })

  // ** FromFB
  static member FromFB (fb: AppendEntriesFB) : Either<DiscoError,AppendEntries> =
    either {
      let! entries =
        if fb.EntriesLength = 0 then
          Either.succeed None
        else
          let raw = Array.zeroCreate fb.EntriesLength
          for i in 0 .. (fb.EntriesLength - 1) do
            let entry = fb.Entries(i)
            if entry.HasValue then
              raw.[i] <- entry.Value
          RaftLogEntry.FromFB raw

      return { Term         = term  fb.Term
               PrevLogIdx   = index fb.PrevLogIdx
               PrevLogTerm  = term  fb.PrevLogTerm
               LeaderCommit = index fb.LeaderCommit
               Entries      = entries }
    }

  // ** ToOffset
  member self.ToOffset(builder: FlatBufferBuilder) =
    let entries =
      Option.map
        (fun (entries: RaftLogEntry) ->
          let offsets = entries.ToOffset(builder)
          AppendEntriesFB.CreateEntriesVector(builder, offsets))
        self.Entries

    AppendEntriesFB.StartAppendEntriesFB(builder)
    AppendEntriesFB.AddTerm(builder, int self.Term)
    AppendEntriesFB.AddPrevLogTerm(builder, int self.PrevLogTerm)
    AppendEntriesFB.AddPrevLogIdx(builder, int self.PrevLogIdx)
    AppendEntriesFB.AddLeaderCommit(builder, int self.LeaderCommit)

    Option.map (fun offset -> AppendEntriesFB.AddEntries(builder, offset)) entries
    |> ignore

    AppendEntriesFB.EndAppendEntriesFB(builder)

// * AppendResponse

//     _                               _ ____
//    / \   _ __  _ __   ___ _ __   __| |  _ \ ___  ___ _ __   ___  _ __  ___  ___
//   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//  / ___ \| |_) | |_) |  __/ | | | (_| |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//         |_|   |_|                                   |_|

/// Appendentries response message.
///
/// an be sent without any entries as a keep alive message.
/// his message could force a leader/candidate to become a follower.
///
/// ## Response Message:
///  - `Term`       - currentTerm, to force other leader/candidate to step down
///  - `Success`    - true if follower contained entry matching prevLogidx and prevLogTerm
///  - `CurrentIdx` - This is the highest log IDX we've received and appended to our log
///  - `FirstIdx`   - The first idx that we received within the appendentries message
type AppendResponse =
  { Term         : Term
    Success      : bool
    CurrentIndex : Index
    FirstIndex   : Index }

  // ** optics

  static member Term_ =
    (fun (ar:AppendResponse) -> ar.Term),
    (fun term (ar:AppendResponse) -> { ar with Term = term })

  static member Success_ =
    (fun (ar:AppendResponse) -> ar.Success),
    (fun success (ar:AppendResponse) -> { ar with Success = success })

  static member CurrentIndex_ =
    (fun (ar:AppendResponse) -> ar.CurrentIndex),
    (fun currentIndex (ar:AppendResponse) -> { ar with CurrentIndex = currentIndex })

  static member FirstIndex_ =
    (fun (ar:AppendResponse) -> ar.FirstIndex),
    (fun firstIndex (ar:AppendResponse) -> { ar with FirstIndex = firstIndex })

  // ** FromFB
  static member FromFB (fb: AppendResponseFB) : Either<DiscoError,AppendResponse> =
    Right { Term         = term fb.Term
            Success      = fb.Success
            CurrentIndex = index fb.CurrentIndex
            FirstIndex   = index fb.FirstIndex }

  // ** ToOffset
  member self.ToOffset(builder: FlatBufferBuilder) =
    AppendResponseFB.StartAppendResponseFB(builder)
    AppendResponseFB.AddTerm(builder, int self.Term)
    AppendResponseFB.AddSuccess(builder, self.Success)
    AppendResponseFB.AddFirstIndex(builder, int self.FirstIndex)
    AppendResponseFB.AddCurrentIndex(builder, int self.CurrentIndex)
    AppendResponseFB.EndAppendResponseFB(builder)

// * module AppendRequest

[<RequireQualifiedAccess>]
module AppendRequest =

  open Aether

  // ** getters

  let term         = Optic.get AppendEntries.Term_
  let prevLogIdx   = Optic.get AppendEntries.PrevLogIdx_
  let prevLogTerm  = Optic.get AppendEntries.PrevLogTerm_
  let leaderCommit = Optic.get AppendEntries.LeaderCommit_
  let entries      = Optic.get AppendEntries.Entries_

  // ** setters

  let setTerm         = Optic.set AppendEntries.Term_
  let setPrevLogIdx   = Optic.set AppendEntries.PrevLogIdx_
  let setPrevLogTerm  = Optic.set AppendEntries.PrevLogTerm_
  let setLeaderCommit = Optic.set AppendEntries.LeaderCommit_
  let setEntries      = Optic.set AppendEntries.Entries_

  // ** numEntries

  let inline numEntries ar =
    match ar.Entries with
      | Some entries -> LogEntry.depth entries
      | _            -> 0

// * AppendResponse

module AppendResponse =

  open Aether

  // ** getters

  let term  = Optic.get AppendResponse.Term_
  let success  = Optic.get AppendResponse.Success_
  let currentIndex  = Optic.get AppendResponse.CurrentIndex_
  let firstIndex  = Optic.get AppendResponse.FirstIndex_

  // ** setters

  let setTerm  = Optic.set AppendResponse.Term_
  let setSuccess  = Optic.set AppendResponse.Success_
  let setCurrentIndex  = Optic.set AppendResponse.CurrentIndex_
  let setFirstIndex  = Optic.set AppendResponse.FirstIndex_

  // ** succeeded

  let succeeded = success

  // ** failed

  let failed = success >> not

// * InstallSnapshot

//  ___           _        _ _ ____                        _           _
// |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
//  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
//  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
// |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
//                                              |_|

type InstallSnapshot =
  { Term:      Term
    LeaderId:  MemberId
    LastIndex: Index
    LastTerm:  Term
    Data:      RaftLogEntry }

  // ** ToOffset

  member self.ToOffset (builder: FlatBufferBuilder) =
    let data = InstallSnapshotFB.CreateDataVector(builder, self.Data.ToOffset(builder))
    let leaderid = InstallSnapshotFB.CreateLeaderIdVector(builder,self.LeaderId.ToByteArray())
    InstallSnapshotFB.StartInstallSnapshotFB(builder)
    InstallSnapshotFB.AddTerm(builder, int self.Term)
    InstallSnapshotFB.AddLeaderId(builder, leaderid)
    InstallSnapshotFB.AddLastTerm(builder, int self.LastTerm)
    InstallSnapshotFB.AddLastIndex(builder, int self.LastIndex)
    InstallSnapshotFB.AddData(builder, data)
    InstallSnapshotFB.EndInstallSnapshotFB(builder)

  // ** FromFB

  static member FromFB (fb: InstallSnapshotFB) =
    either  {
      let! decoded =
        if fb.DataLength > 0 then
          let raw = Array.zeroCreate fb.DataLength
          for i in 0 .. (fb.DataLength - 1) do
            let data = fb.Data(i)
            if data.HasValue then
              raw.[i] <- data.Value
          RaftLogEntry.FromFB raw
        else
          "Invalid InstallSnapshot (no log data)"
          |> Error.asParseError "InstallSnapshot.FromFB"
          |> Either.fail

      match decoded with
      | Some entries ->
        let! leaderId = Id.decodeLeaderId fb
        return {
          Term      = term fb.Term
          LeaderId  = leaderId
          LastIndex = index fb.LastIndex
          LastTerm  = term fb.LastTerm
          Data      = entries
        }
      | _ ->
        return!
          "Invalid InstallSnapshot (no log data)"
          |> Error.asParseError "InstallSnapshot.FromFB"
          |> Either.fail
    }

// * Callback Interface

/////////////////////////////////////////////////
//   ____      _ _ _                _          //
//  / ___|__ _| | | |__   __ _  ___| | __      //
// | |   / _` | | | '_ \ / _` |/ __| |/ /      //
// | |__| (_| | | | |_) | (_| | (__|   <       //
//  \____\__,_|_|_|_.__/ \__,_|\___|_|\_\      //
//                                             //
//  ___       _             __                 //
// |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  //
//  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \ //
//  | || | | | ||  __/ |  |  _| (_| | (_|  __/ //
// |___|_| |_|\__\___|_|  |_|  \__,_|\___\___| //
/////////////////////////////////////////////////

type IRaftCallbacks =

  /// Request a vote from given Raft server
  abstract member SendRequestVote: peer:RaftMember -> request:VoteRequest -> unit

  /// Send AppendEntries message to given server
  abstract member SendAppendEntries: peer:RaftMember -> request:AppendEntries -> unit

  /// Send InstallSnapshot command to given serve
  abstract member SendInstallSnapshot: peer:RaftMember -> request:InstallSnapshot -> unit

  /// given the current state of Raft, prepare and return a snapshot value of
  /// current application state
  abstract member PrepareSnapshot: current:RaftState -> RaftLog option

  /// perist the given Snapshot value to disk. For safety reasons this MUST
  /// flush all changes to disk.
  abstract member PersistSnapshot: snapshot:RaftLogEntry -> unit

  /// attempt to load a snapshot from disk. return None if no snapshot was found
  abstract member RetrieveSnapshot: unit  -> RaftLogEntry option

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
  abstract member PersistLog: log:RaftLogEntry -> unit

  /// persist the removal of the passed entry from the log to disk. For safety
  /// reasons this callback MUST flush the change to disk.
  abstract member DeleteLog: log:RaftLogEntry -> unit

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

type RaftState =
  { /// this server's own RaftMember information
    Member            : RaftMember
    /// this server's current Raft state, i.e. follower, leader or candidate
    State             : MemberState
    /// the server's current term, a monotonic counter for election cycles
    CurrentTerm       : Term
    /// tracks the current Leader Id, or None if there isn't currently a leader
    CurrentLeader     : MemberId option
    /// map of all known members in the cluster
    Peers             : Map<MemberId,RaftMember>
    /// map of the previous cluster configuration. set if currently in a configuration change
    OldPeers          : Map<MemberId,RaftMember> option
    /// count of all members in the cluster
    NumMembers        : int
    /// the candidate this server voted for in its current term or None if it hasn't voted for any
    /// other member yet
    VotedFor          : MemberId option
    /// the replicated state machine command log
    Log               : RaftLog
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
    ConfigChangeEntry : RaftLogEntry option }

  // ** ToString

  override self.ToString() =
    sprintf "Member              = %s
State             = %A
CurrentTerm       = %A
CurrentLeader     = %A
NumMembers          = %A
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
      self.State
      self.CurrentTerm
      self.CurrentLeader
      self.NumMembers
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

  member self.IsLeader
    with get () =
      match self.CurrentLeader with
      | Some lid -> self.Member.Id = lid
      | _ -> false

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

  static member FromYaml (yaml: RaftStateYaml) : Either<DiscoError, RaftState> =
    either {
      let! id = DiscoId.TryParse yaml.Member

      let! leader =
        if isNull yaml.Leader
        then Right None
        else DiscoId.TryParse yaml.Leader |> Either.map Some

      let! votedfor =
        if isNull yaml.VotedFor
        then Right None
        else DiscoId.TryParse yaml.VotedFor |> Either.map Some

      return {
        Member            = Member.create id
        State             = Follower
        CurrentTerm       = yaml.Term
        CurrentLeader     = leader
        Peers             = Map.empty
        OldPeers          = None
        NumMembers        = 0
        VotedFor          = votedfor
        Log               = Log.empty
        CommitIndex       = index 0
        LastAppliedIdx    = index 0
        TimeoutElapsed    = 0<ms>
        ElectionTimeout   = yaml.ElectionTimeout * 1<ms>
        RequestTimeout    = yaml.RequestTimeout * 1<ms>
        MaxLogDepth       = yaml.MaxLogDepth
        ConfigChangeEntry = None
      }
    }

  #endif

// * RaftMonad

[<NoComparison;NoEquality>]
type RaftMonad<'Env,'State,'T,'Error> =
  MkRM of ('Env -> 'State -> Either<'Error * 'State,'T * 'State>)

// * RaftM

type RaftM<'t,'err> =
  RaftMonad<IRaftCallbacks, RaftState, 't, 'err>
