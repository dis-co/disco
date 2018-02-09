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

// * EntryResponse

/// Response to an AppendEntry request

type EntryResponse =
  { Id:    LogId
    Term:  Term
    Index: Index }

  // ** ToString

  override self.ToString() =
    sprintf "Entry added with Id: %A in term: %d at log index: %d"
      (string self.Id)
      self.Term
      self.Index

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let id = EntryResponseFB.CreateIdVector(builder,self.Id.ToByteArray())
    EntryResponseFB.StartEntryResponseFB(builder)
    EntryResponseFB.AddId(builder, id)
    EntryResponseFB.AddTerm(builder, int self.Term)
    EntryResponseFB.AddIndex(builder, int self.Index)
    EntryResponseFB.EndEntryResponseFB(builder)

  // ** FromFB

  static member FromFB(fb: EntryResponseFB) =
    either {
      let! id = Id.decodeId fb
      return {
        Id = id
        Term = 1<term> * fb.Term
        Index = 1<index> * fb.Index
      }
    }

  // ** optics

  static member Id_ =
    (fun (er: EntryResponse) -> er.Id),
    (fun id (er: EntryResponse) -> { er with Id = id })

  static member Term_ =
    (fun (er: EntryResponse) -> er.Term),
    (fun term (er: EntryResponse) -> { er with Term = term })

  static member Index_ =
    (fun (er: EntryResponse) -> er.Index),
    (fun index (er: EntryResponse) -> { er with Index = index })

// * EntryResponse module

[<RequireQualifiedAccess>]
module EntryResponse =

  // ** getting

  let id = Optic.get EntryResponse.Id_
  let term = Optic.get EntryResponse.Term_
  let index = Optic.get EntryResponse.Index_

  // ** setting

  let setId = Optic.set EntryResponse.Id_
  let setTerm = Optic.set EntryResponse.Term_
  let setIndex = Optic.set EntryResponse.Index_

  // ** create

  let create term index : EntryResponse =
    { Id = DiscoId.Create()
      Term = term
      Index = index }

// * VoteRequest

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
        return { Term         = 1<term> * fb.Term
                 Candidate    = mem
                 LastLogIndex = 1<index> * fb.LastLogIndex
                 LastLogTerm  = 1<term> * fb.LastLogTerm }
      else
        return!
          "Could not parse empty MemberFB"
          |> Error.asParseError "VoteRequest.FromFB"
          |> Either.fail
    }

  // ** optics

  static member Term_ =
    (fun (vr:VoteRequest) -> vr.Term),
    (fun term (vr:VoteRequest) -> { vr with Term = term })

  static member Candidate_ =
    (fun (vr:VoteRequest) -> vr.Candidate),
    (fun candidate (vr:VoteRequest) -> { vr with Candidate = candidate })

  static member LastLogIndex_ =
    (fun (vr:VoteRequest) -> vr.LastLogIndex),
    (fun lastLogIndex (vr:VoteRequest) -> { vr with LastLogIndex = lastLogIndex })

  static member LastLogTerm_ =
    (fun (vr:VoteRequest) -> vr.LastLogTerm),
    (fun lastLogTerm (vr:VoteRequest) -> { vr with LastLogTerm = lastLogTerm })

// * VoteRequest module

module VoteRequest =

  // ** getters

  let term = Optic.get VoteRequest.Term_
  let candidate = Optic.get VoteRequest.Candidate_
  let lastLogIndex = Optic.get VoteRequest.LastLogIndex_
  let lastLogTerm = Optic.get VoteRequest.LastLogTerm_

  // ** setters

  let setTerm = Optic.set VoteRequest.Term_
  let setCandidate = Optic.set VoteRequest.Candidate_
  let setLastLogIndex = Optic.set VoteRequest.LastLogIndex_
  let setLastLogTerm = Optic.set VoteRequest.LastLogIndex_

// * VoteResponse

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
      return {
        Term    = 1<term> * fb.Term
        Granted = fb.Granted
        Reason  = reason
      }
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

  // ** optics

  static member Term_ =
    (fun (vr:VoteResponse) -> vr.Term),
    (fun term (vr:VoteResponse) -> { vr with Term = term })

  static member Granted_ =
    (fun (vr:VoteResponse) -> vr.Granted),
    (fun granted (vr:VoteResponse) -> { vr with Granted = granted })

  static member Reason_ =
    (fun (vr:VoteResponse) -> vr.Reason),
    (fun reason (vr:VoteResponse) -> { vr with Reason = reason })

// * VoteResponse module

[<RequireQualifiedAccess>]
module VoteResponse =

  // ** getters

  let term = Optic.get VoteResponse.Term_
  let granted = Optic.get VoteResponse.Granted_
  let reason = Optic.get VoteResponse.Reason_

  // ** setters

  let setTerm = Optic.set VoteResponse.Term_
  let setGranted = Optic.set VoteResponse.Granted_
  let setReason = Optic.set VoteResponse.Reason_

  // ** declined

  let declined = granted >> not

// * AppendEntries

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
    Entries      : LogEntry option }

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
          LogEntry.FromFB raw

      return { Term         = 1<term> * fb.Term
               PrevLogIdx   = 1<index> * fb.PrevLogIdx
               PrevLogTerm  = 1<term> * fb.PrevLogTerm
               LeaderCommit = 1<index> * fb.LeaderCommit
               Entries      = entries }
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    let entries =
      Option.map
        (fun (entries: LogEntry) ->
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

// * AppendRequest module

module AppendEntries =

  // ** getters

  let term = Optic.get AppendEntries.Term_
  let prevLogIdx = Optic.get AppendEntries.PrevLogIdx_
  let prevLogTerm = Optic.get AppendEntries.PrevLogTerm_
  let leaderCommit = Optic.get AppendEntries.LeaderCommit_
  let entries = Optic.get AppendEntries.Entries_

  // ** setters

  let setTerm = Optic.set AppendEntries.Term_
  let setPrevLogIdx = Optic.set AppendEntries.PrevLogIdx_
  let setPrevLogTerm = Optic.set AppendEntries.PrevLogTerm_
  let setLeaderCommit = Optic.set AppendEntries.LeaderCommit_
  let setEntries = Optic.set AppendEntries.Entries_

  // ** numEntries

  let numEntries = entries >> function
    | Some entries -> LogEntry.depth entries
    | _            -> 0

// * AppendResponse

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

  static member FromFB (fb: AppendResponseFB) =
    Either.succeed {
      Term         = 1<term> * fb.Term
      Success      = fb.Success
      CurrentIndex = 1<index> * fb.CurrentIndex
      FirstIndex   = 1<index> * fb.FirstIndex
    }

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    AppendResponseFB.StartAppendResponseFB(builder)
    AppendResponseFB.AddTerm(builder, int self.Term)
    AppendResponseFB.AddSuccess(builder, self.Success)
    AppendResponseFB.AddFirstIndex(builder, int self.FirstIndex)
    AppendResponseFB.AddCurrentIndex(builder, int self.CurrentIndex)
    AppendResponseFB.EndAppendResponseFB(builder)

// * AppendResponse module

module AppendResponse =

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
    Data:      LogEntry }

  // ** optics

  static member Term_ =
    (fun (is:InstallSnapshot) -> is.Term),
    (fun term (is:InstallSnapshot) -> { is with Term = term })

  static member LeaderId_ =
    (fun (is:InstallSnapshot) -> is.LeaderId),
    (fun leaderId (is:InstallSnapshot) -> { is with LeaderId = leaderId })

  static member LastIndex_ =
    (fun (is:InstallSnapshot) -> is.LastIndex),
    (fun lastIndex (is:InstallSnapshot) -> { is with LastIndex = lastIndex })

  static member LastTerm_ =
    (fun (is:InstallSnapshot) -> is.LastTerm),
    (fun lastTerm (is:InstallSnapshot) -> { is with LastTerm = lastTerm })

  static member Data_ =
    (fun (is:InstallSnapshot) -> is.Data),
    (fun data (is:InstallSnapshot) -> { is with Data = data })

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
          LogEntry.FromFB raw
        else
          "Invalid InstallSnapshot (no log data)"
          |> Error.asParseError "InstallSnapshot.FromFB"
          |> Either.fail

      match decoded with
      | Some entries ->
        let! leaderId = Id.decodeLeaderId fb
        return {
          Term      = 1<term> * fb.Term
          LeaderId  = leaderId
          LastIndex = 1<index> * fb.LastIndex
          LastTerm  = 1<term> * fb.LastTerm
          Data      = entries
        }
      | _ ->
        return!
          "Invalid InstallSnapshot (no log data)"
          |> Error.asParseError "InstallSnapshot.FromFB"
          |> Either.fail
    }

// * InstallSnapshot module

module InstallSnapshot =

  // ** getters

  let term = Optic.get InstallSnapshot.Term_
  let leaderId = Optic.get InstallSnapshot.LeaderId_
  let lastIndex = Optic.get InstallSnapshot.LastIndex_
  let lastTerm = Optic.get InstallSnapshot.LastTerm_
  let data = Optic.get InstallSnapshot.Data_

  // ** setters

  let setTerm = Optic.set InstallSnapshot.Term_
  let setLeaderId = Optic.set InstallSnapshot.LeaderId_
  let setLastIndex = Optic.set InstallSnapshot.LastIndex_
  let setLastTerm = Optic.set InstallSnapshot.LastTerm_
  let setData = Optic.set InstallSnapshot.Data_
