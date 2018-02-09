(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Raft

// * Imports

open System
open Disco.Core

// * Raft

[<RequireQualifiedAccess>]
module rec Raft =

  // ** tag

  let private tag (str: string) = String.Format("Raft.{0}",str)

  // ** rand

  let private rand = new System.Random()

  // ** handleConfiguration

  let private handleConfiguration mems (state: RaftState) =
    let parting =
      mems
      |> Array.map (fun (mem: RaftMember) -> mem.Id)
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

  let private handleJointConsensus (changes) (state:RaftState) =
    state
    |> RaftState.applyChanges changes
    |> RaftState.setOldPeers (Some state.Peers)

  // ** createResponse

  /// Preliminary Checks on the AppendEntry value

  let private createResponse (nid: MemberId option) (msg: AppendEntries) =
    raft {
      let! term = currentTerm ()
      let! current = currentIndex ()
      let! first = firstIndex term >>= (Option.defaultValue 0<index> >> returnM)

      let resp: AppendResponse =
        { Term         = term
          Success      = false
          CurrentIndex = current
          FirstIndex   = first }

      // 1) If this mem is currently candidate and both its and the requests
      // term are equal, we become follower and reset VotedFor.
      let! candidate = isCandidate ()
      let! currentlyLeader = isLeader ()
      let! num = numMembers ()
      let newLeader = currentlyLeader && num = 1
      if (candidate || newLeader) && term = msg.Term then
        do! voteFor None
        do! setLeader nid
        do! becomeFollower ()
        return Right resp
      // 2) Else, if the current mem's term value is lower than the requests
      // term, we take become follower and set our own term to higher value.
      elif term < msg.Term then
        do! setCurrentTerm msg.Term
        do! setLeader nid
        do! becomeFollower ()
        return
          resp
          |> AppendResponse.setTerm msg.Term
          |> Either.succeed
      // 3) Else, finally, if the msg's Term is lower than our own we reject the
      // the request entirely.
      elif msg.Term < term then
        let! idx = currentIndex()
        return
          resp
          |> AppendResponse.setCurrentIndex idx
          |> Either.fail
      else
        return Either.succeed resp
    }

  // ** handleConflicts

  // If an existing entry conflicts with a new one (same index
  // but different terms), delete the existing entry and all that
  // follow it (§5.3)

  let private handleConflicts (request: AppendEntries) =
    raft {
      let idx = request.PrevLogIdx + 1<index>
      let! local = entryAt idx

      match request.Entries with
      | Some entries ->
        let remote = LogEntry.last entries
        // find the entry in the local log that corresponds to position of
        // then log in the request and compare their terms
        match local with
        | Some entry ->
          if LogEntry.term entry <> LogEntry.term remote then
            // removes entry at idx (and all following entries)
            do! removeEntry idx
        | _ -> ()
      | _ ->
        if Option.isSome local then
          do! removeEntry idx
    }

  // ** applyRemainder

  let private applyRemainder (msg : AppendEntries) (resp : AppendResponse) =
    raft {
      match msg.Entries with
      | Some entries ->
        let! result = appendEntry entries
        match result with
        | Some log ->
          let! fst = currentTerm () >>= firstIndex
          let fidx =
            match fst with
            | Some fidx -> fidx
            | _         -> msg.PrevLogIdx + ((log |> LogEntry.depth |> int) * 1<index>)
          return
            resp
            |> AppendResponse.setCurrentIndex (LogEntry.index log)
            |> AppendResponse.setFirstIndex fidx
        | _ -> return resp
      | _ -> return resp
    }

  // ** requestSetCommitIndex

  /// If leaderCommit > commitIndex, set commitIndex =
  /// min(leaderCommit, index of most recent entry)
  let private requestSetCommitIndex (msg : AppendEntries) =
    raft {
      let! state = get
      let! cmmtidx = commitIndex ()
      let ldridx = msg.LeaderCommit
      if cmmtidx < ldridx then
        let! current = currentIndex ()
        let lastLogIdx = max current 1<index>
        let newIndex = min lastLogIdx msg.LeaderCommit
        do! setCommitIndex newIndex
    }

  // ** processEntry

  let private processEntry nid msg resp =
    raft {
      do! handleConflicts msg
      let! response = applyRemainder msg resp
      do! requestSetCommitIndex msg
      do! setLeader nid
      return AppendResponse.setSuccess true response
    }

  // ** checkAndProcess

  ///  2. Reply false if log doesn't contain an entry at prevLogIndex whose
  /// term matches prevLogTerm (§5.3)

  let private checkAndProcess entry nid msg resp =
    raft {
      let! current = currentIndex ()

      if current < msg.PrevLogIdx then
        do! msg.PrevLogIdx
            |> sprintf "Failed (ci: %d) < (prev log idx: %d)" current
            |> logError "receiveAppendEntries"
        return resp
      else
        let term = LogEntry.term entry
        if term <> msg.PrevLogTerm then
          do! sprintf "Failed (term %d) != (prev log term %d) (ci: %d) (prev log idx: %d)"
                  term
                  msg.PrevLogTerm
                  current
                  msg.PrevLogIdx
              |> logError "receiveAppendEntries"
          do! removeEntry msg.PrevLogIdx
          return AppendResponse.setCurrentIndex (msg.PrevLogIdx - 1<index>) resp
        else
          return! processEntry nid msg resp
    }

  // ** updateMemberIndices

  let private updateMemberIndices (resp : AppendResponse) (mem : RaftMember) =
    raft {
      let peer =
        mem
        |> Member.setNextIndex (resp.CurrentIndex + 1<index>)
        |> Member.setMatchIndex resp.CurrentIndex

      let! current = currentIndex ()

      let notVoting = not (Member.isVoting peer)
      let notLogs   = not (Member.hasSufficientLogs peer)
      let idxOk     = current <= resp.CurrentIndex + 1<index>

      if notVoting && idxOk && notLogs then
        let updated = Member.setHasSufficientLogs peer
        do! updateMember updated
      else
        do! updateMember peer
    }

  // ** shouldCommit

  let private shouldCommit peers state resp =
    let folder (votes: int) nid (mem: RaftMember) =
      if nid = state.Member.Id || not (Member.isVoting mem)
      then votes
      elif mem.MatchIndex > 0<index> then
        match RaftState.entryAt mem.MatchIndex state with
        | Some entry ->
          if LogEntry.term entry = state.CurrentTerm && resp.CurrentIndex <= mem.MatchIndex
          then votes + 1
          else votes
        | _ -> votes
      else votes

    let commit = RaftState.commitIndex state
    let num = RaftState.countMembers peers
    let votes = Map.fold folder 1 peers

    (num / 2) < votes && commit < resp.CurrentIndex

  // ** responseSetCommitIndex

  let private responseSetCommitIndex (resp: AppendResponse) =
    raft {
      let! state = get
      let! inConsensus = inJointConsensus ()
      let commitOk =
        if inConsensus then
          // handle the joint consensus case
          match state.OldPeers with
          | Some peers ->
            let older = shouldCommit peers       state resp
            let newer = shouldCommit state.Peers state resp
            older || newer
          | _ -> shouldCommit state.Peers state resp
        else
          // the base case, not in joint consensus
          shouldCommit state.Peers state resp
      if commitOk then
        do! setCommitIndex resp.CurrentIndex
    }

  // ** receiveAppendEntries

  let receiveAppendEntries (nid: MemberId option) (msg: AppendEntries) =
    raft {
      do! setTimeoutElapsed 0<ms>      // reset timer, so we don't start an election

      // log this if any entries are to be processed
      if Option.isSome msg.Entries then
        let! current = currentIndex ()

        do! sprintf "from: %A term: %d (ci: %d) (lc-idx: %d) (pli: %d) (plt: %d) (entries: %d)"
              nid
              msg.Term
              current
              msg.LeaderCommit
              msg.PrevLogIdx
              msg.PrevLogTerm
              (Option.get msg.Entries |> LogEntry.depth)    // let the world know
            |> logDebug "receiveAppendEntries"

      let! result = createResponse nid msg  // check terms et al match, fail otherwise

      match result with
      | Right resp ->
        // this is not the first AppendEntry we're receiving
        if msg.PrevLogIdx > 0<index> then
          let! entry = entryAt msg.PrevLogIdx
          match entry with
          | Some log ->
            return! checkAndProcess log nid msg resp
          | _ ->
            do! msg.PrevLogIdx
                |> String.format "Failed. No log at (prev-log-idx: {0})"
                |> logError "receiveAppendEntries"
            return resp
        else
          return! processEntry nid msg resp
      | Left err -> return err
    }

  // ** receiveAppendEntriesResponse

  let rec receiveAppendEntriesResponse (nid : MemberId) resp =
    raft {
      let! mem = getMember nid
      match mem with
      | None ->
        do! logError
              "receiveAppendEntriesResponse"
              ("Failed: NoMember " + string nid)
        return!
          string nid
          |> sprintf "Node not found: %s"
          |> Error.asRaftError (tag "receiveAppendEntriesResponse")
          |> failM
      | Some peer ->
        if resp.CurrentIndex <> 0<index> && resp.CurrentIndex < peer.MatchIndex then
          do! sprintf "Failed: peer not up to date yet (ci: %d) (match idx: %d)"
                resp.CurrentIndex
                peer.MatchIndex
              |> logError "receiveAppendEntriesResponse"
          // set to current index at follower and try again
          do! peer
              |> Member.setNextIndex (resp.CurrentIndex + 1<index>)
              |> Member.setMatchIndex resp.CurrentIndex
              |> updateMember
        else
          let! leader = isLeader ()
          // we only process this if we are indeed the leader of the pack
          if leader then
            let! term = currentTerm ()
            //  If response contains term T > currentTerm: set currentTerm = T
            //  and convert to follower (§5.3)
            if term < resp.Term then
              let str = sprintf "Failed: (term: %d) < (resp.Term: %d)" term resp.Term
              do! logError "receiveAppendEntriesResponse" str
              do! setCurrentTerm resp.Term
              do! setLeader (Some nid)
              do! becomeFollower ()
            elif term <> resp.Term then
              let str = sprintf "Failed: (term: %d) != (resp.Term: %d)" term resp.Term
              do! logError "receiveAppendEntriesResponse" str
            elif not resp.Success then
              // If AppendEntries fails because of log inconsistency:
              // decrement nextIndex and retry (§5.3)
              if resp.CurrentIndex < peer.NextIndex - 1<index> then
                let! idx = currentIndex ()
                let nextIndex = min (resp.CurrentIndex + 1<index>) idx

                do! nextIndex
                    |> sprintf "Failed: cidx < nxtidx. setting nextIndex for %O to %d" peer.Id
                    |> logError "receiveAppendEntriesResponse"

                do! setNextIndex peer.Id nextIndex
                do! setMatchIndex peer.Id (nextIndex - 1<index>)
              else
                let nextIndex = peer.NextIndex - 1<index>

                do! nextIndex
                    |> sprintf "Failed: cidx >= nxtidx. setting nextIndex for %O to %d" peer.Id
                    |> logError "receiveAppendEntriesResponse"

                do! setNextIndex peer.Id nextIndex
                do! setMatchIndex peer.Id (nextIndex - 1<index>)
            else
              do! updateMemberIndices resp peer
              do! responseSetCommitIndex resp
          else
            return!
              "Not Leader"
              |> Error.asRaftError (tag "receiveAppendEntriesResponse")
              |> failM
    }

  // ** sendAppendEntry

  let sendAppendEntry (peer: RaftMember) =
    raft {
      let! state = get
      let! entries = entriesUntil peer.NextIndex

      let request: AppendEntries =
        { Term         = state.CurrentTerm
          PrevLogIdx   = 0<index>
          PrevLogTerm  = 0<term>
          LeaderCommit = state.CommitIndex
          Entries      = entries }

      if peer.NextIndex > 1<index> then
        let! result = entryAt (peer.NextIndex - 1<index>)
        let request =
          { request with
              PrevLogIdx = peer.NextIndex - 1<index>
              PrevLogTerm =
                  match result with
                    | Some(entry) -> LogEntry.term entry
                    | _           -> request.Term }
        do! sendAppendEntries peer request
      else
        do! sendAppendEntries peer request
    }

  // ** sendRemainingEntries

  let private sendRemainingEntries peerid =
    raft {
      let! peer = getMember peerid
      match peer with
      | Some mem ->
        let! entry = entryAt (Member.nextIndex mem)
        if Option.isSome entry then
          do! sendAppendEntry mem
      | _ -> return ()
    }

  // ** sendAllAppendEntries

  let sendAllAppendEntries () =
    raft {
      let! self = self ()
      let! peers = logicalPeers ()
      for KeyValue(id,peer) in peers do
        if id <> self.Id then
          do! sendAppendEntry peer
      do! setTimeoutElapsed 0<ms>
    }

  // ** createSnapshot

  /// utiltity to create a snapshot for the current application and raft state

  let createSnapshot (state: RaftState) (data: StateMachine) =
    let peers = Map.toArray state.Peers |> Array.map snd
    Log.snapshot peers data state.Log

  // ** sendInstallSnapshot

  let sendInstallSnapshot mem =
    raft {
      let! env = read
      let! term = currentTerm()
      let! leader = self () >>= (Member.id >> returnM)
      match env.RetrieveSnapshot () with
      | Some (Snapshot(_,idx,term,_,_,_,_) as snapshot) ->
        env.SendInstallSnapshot mem {
          Term      = term
          LeaderId  = leader
          LastIndex = idx
          LastTerm  = term
          Data      = snapshot
        }
      | _ -> ()
    }

  // ** responseCommitted

  /// Check if an entry corresponding to a receiveEntry result has actually been
  /// committed to the state machine.

  let responseCommitted (resp : EntryResponse) =
    raft {
      let! entry = entryAt resp.Index
      match entry with
      | None -> return false
      | Some entry ->
        if resp.Term <> LogEntry.term entry
        then
          return!
            "Entry Invalidated"
            |> Error.asRaftError (tag "responseCommitted")
            |> failM
        else
          let! cidx = commitIndex ()
          return resp.Index <= cidx
    }

  // ** handleLog

  let private handleLog entry resp =
    raft {
      let! result = appendEntry entry

      match result with
      | Some appended ->
        let! selfId = self () >>= (Member.id >> returnM)
        let! maxDepth = maxLogDepth ()
        let! peers = logicalPeers ()

        // iterate through all peers and call sendAppendEntries to each
        for peer in peers do
          let mem = peer.Value
          if mem.Id <> selfId then
            let nxtidx = Member.nextIndex mem
            let! cidx = currentIndex ()

            // calculate whether we need to send a snapshot or not
            // uint's wrap around, so normalize to int first (might cause trouble with big numbers)
            let difference =
              let d = cidx - nxtidx
              if d < 0<index> then 0<index> else d

            if difference <= (1<index> * (int maxDepth) + 1<index>) then
              // Only send new entries. Don't send the entry to peers who are
              // behind, to prevent them from becoming congested.
              do! sendAppendEntry mem
            else
              // because this mem is way behind in the cluster, get it up to speed
              // with a snapshot
              do! sendInstallSnapshot mem

        do! updateCommitIndex ()
        let! term = currentTerm ()
        return
          { resp with
              Id = LogEntry.id appended
              Term = term
              Index = LogEntry.index appended }
      | _ ->
        return!
          "Append Entry failed"
          |> Error.asRaftError (tag "handleLog")
          |> failM
    }

  // ** receiveEntry

  let receiveEntry (entry: LogEntry) =
    raft {
      let! leader = isLeader ()
      let! configChange = configChangeEntry()

      if LogEntry.isConfigChange entry && Option.isSome configChange then
        do! logDebug "receiveEntry" "Error: UnexpectedVotingChange"
        return!
          "Unexpected Voting Change"
          |> Error.asRaftError (tag "receiveEntry")
          |> failM
      elif leader then
        let! term = currentTerm()
        let! idx = log () >>= (Log.index >> returnM)

        do! term
            |> sprintf "(id: %A) (idx: %d) (term: %d)"
              (LogEntry.id entry)
              (idx + 1<index>)
            |> logDebug "receiveEntry"

        let response = EntryResponse.create 0<term> 0<index>

        match entry with
        | LogEntry(id,_,_,data,_) ->
          let log = LogEntry(id, 0<index>, term, data, None)
          return! handleLog log response

        | Configuration(id,_,_,mems,_) ->
          let log = Configuration(id, 0<index>, term, mems, None)
          return! handleLog log response

        | JointConsensus(id,_,_,changes,_) ->
          let log = JointConsensus(id, 0<index>, term, changes, None)
          return! handleLog log response

        | _ ->
          return!
            "Log Format Error"
            |> Error.asRaftError (tag "receiveEntry")
            |> failM
      else
        return!
          "Not Leader"
          |> Error.asRaftError (tag "receiveEntry")
          |> failM
    }

  // ** notifyChange

  let notifyChange (cbs: IRaftCallbacks) change =
    match change with
      | MemberAdded(mem)   -> cbs.MemberAdded   mem
      | MemberRemoved(mem) -> cbs.MemberRemoved mem

  // ** applyEntry

  let applyEntry (cbs: IRaftCallbacks) = function
    | JointConsensus(_,_,_,changes,_) ->
      Array.iter (notifyChange cbs) changes
      cbs.JointConsensus changes
    | Configuration(_,_,_,mems,_) -> cbs.Configured mems
    | LogEntry(_,_,_,data,_) -> cbs.ApplyLog data
    | Snapshot(_,_,_,_,_,_,data) as snapshot ->
      cbs.PersistSnapshot snapshot
      cbs.ApplyLog data

  // ** applyEntries

  let applyEntries () =
    raft {
      let! state = get
      let lai = state.LastAppliedIdx
      let coi = state.CommitIndex
      if lai <> coi then
        let logIdx = lai + 1<index>
        let! result = entriesUntil logIdx
        match result with
        | Some entries ->
          let! cbs = read

          let str =
            LogEntry.depth entries
            |> sprintf "applying %d entries to state machine"

          do! logInfo "applyEntries" str

          // Apply log chain in the order it arrived
          let state, change =
            LogEntry.foldr
              (fun (state, current) -> function
                | Configuration(_,_,_,mems,_) as config ->
                  // set the peers map
                  let newstate = handleConfiguration mems state
                  // when a new configuration is added, under certain circumstances a mem change
                  // might not have been applied yet, so calculate those dangling changes
                  let changes = RaftState.calculateChanges state.Peers newstate.Peers
                  // apply dangling changes
                  do Array.iter (notifyChange cbs) changes
                  // apply the entry by calling the callback
                  do applyEntry cbs config
                  (newstate, None)
                | JointConsensus(_,_,_,changes,_) as config ->
                  let state = handleJointConsensus changes state
                  do applyEntry cbs config
                  (state, Some (LogEntry.head config))
                | entry ->
                  do applyEntry cbs entry
                  (state, current))
              (state, state.ConfigChangeEntry)
              entries

          do! match change with
              | Some _ -> "setting ConfigChangeEntry to JointConsensus"
              | None   -> "resetting ConfigChangeEntry"
              |> logDebug "applyEntries"

          do! put { state with ConfigChangeEntry = change }

          if LogEntry.contains LogEntry.isConfiguration entries then
            let selfIncluded (state: RaftState) =
              Map.containsKey state.Member.Id state.Peers
            let! included = selfIncluded |> zoom
            if not included then
              let str =
                string state.Member.Id
                |> sprintf "self (%s) not included in new configuration"
              do! logDebug "applyEntries" str
              do! setLeader None
              do! becomeFollower ()
            /// snapshot now:
            ///
            /// the cluster was just re-configured, and if any of (possibly) just removed members were
            /// to be added again, the replay log they would receive when joining would cause them to
            /// be automatically being removed again. this is why, after the configuration changes are
            /// done we need to create a snapshot of the raft log, which won't contain those commands.
            do! doSnapshot()

          let! state = get
          if not (RaftState.isLeader state) && LogEntry.contains LogEntry.isConfiguration entries then
            do! logDebug "applyEntries" "not leader and new configuration is applied. Updating mems."
            for kv in state.Peers do
              if kv.Value.Status <> Running then
                do! updateMember { kv.Value with Status = Running; Voting = true }

          let idx = LogEntry.index entries
          do! logDebug "applyEntries" <| sprintf "setting LastAppliedIndex to %d" idx
          do! setLastAppliedIndex idx
        | _ ->
          do! logDebug "applyEntries" (sprintf "no log entries found for index %d" logIdx)
    }

  // ** receiveInstallSnapshot

  (*
   *  1. Reply immediately if term < currentTerm
   *  2. Create new snapshot file if first chunk (offset is 0)
   *  3. Write data into snapshot file at given offset
   *  4. Reply and wait for more data chunks if done is false
   *  5. Save snapshot file, discard any existing or partial snapshot with a smaller index
   *  6. If existing log entry has same index and term as snapshot’s last included entry, retain log entries following it and reply
   *  7. Discard the entire log
   *  8. Reset state machine using snapshot contents (and load snapshot’s cluster configuration)
   *)
  let receiveInstallSnapshot (is: InstallSnapshot) =
    raft {
      let! cbs = read
      let! term = currentTerm ()

      if is.Term < term then
        return!
          "Invalid Term"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM

      do! setTimeoutElapsed 0<ms>

      match is.Data with
      | Snapshot(_,idx,_,_,_,mems, _) as snapshot ->

        // IMPROVEMENT: implementent chunked transmission as per paper
        do cbs.PersistSnapshot snapshot

        let! state = get

        let! remaining = entriesUntilExcluding idx

        // update the cluster configuration
        let peers =
          Array.map toPair mems
          |> Map.ofArray
          |> Map.add state.Member.Id state.Member

        do! setPeers peers

        // update log with snapshot and possibly merge existing entries
        match remaining with
          | Some entries ->
            do! Log.empty
                |> Log.append is.Data
                |> Log.append entries
                |> setLog
          | _ ->
            do! updateLogEntries is.Data |> modify

        // set the current leader to mem which sent snapshot
        do! setLeader (Some is.LeaderId)

        // apply all entries in the new log
        let! state = get

        match state.Log.Data with
        | Some data ->
          LogEntry.foldr (fun _ entry -> applyEntry cbs entry) () data
        | _ -> failwith "Fatal. Snapshot applied, but log is empty. Aborting."

        // reset the counters,to apply all entries in the log
        do! setLastAppliedIndex (Log.index state.Log)
        do! setCommitIndex (Log.index state.Log)

        // construct reply
        let! term = currentTerm ()
        let! ci = currentIndex ()
        let! fi = firstIndex term >>= (Option.defaultValue 0<index> >> returnM)

        return {
          Term         = term
          Success      = true
          CurrentIndex = ci
          FirstIndex   = fi
        }
      | _ ->
        return!
          "Snapshot Format Error"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM
    }

  // ** doSnapshot

  let doSnapshot () =
    raft {
      let! cbs = read
      let! state = get
      match cbs.PrepareSnapshot state with
      | Some snapshot ->
        do! setLog snapshot
        match snapshot.Data with
        | Some snapshot -> cbs.PersistSnapshot snapshot
        | _ -> ()
      | _ -> ()
    }

  // ** maybeSnapshot

  let maybeSnapshot () =
    raft {
      let! state = get
      if Log.length state.Log >= int state.MaxLogDepth then
        do! doSnapshot ()
    }

  // ** maybeSetIndex

  let private maybeSetIndex nid nextIdx matchIdx =
    updateMembers $ fun peer ->
      if Member.isVoting peer && peer.Id <> nid then
        peer
        |> Member.setNextIndex nextIdx
        |> Member.setMatchIndex matchIdx
      else peer

  // ** becomeLeader

  /// Become leader afer a successful election
  let becomeLeader _ =
    raft {
      let! self = self()
      do! logInfo "becomeLeader" "becoming leader"
      let! current = currentIndex ()
      do! setState Leader
      do! setLeader (Some self.Id)
      do! maybeSetIndex self.Id (current + 1<index>) 0<index>
      do! sendAllAppendEntries ()
    }

  // ** becomeFollower

  let becomeFollower _ =
    raft {
      do! logInfo "becomeFollower" "becoming follower"
      do! setState Follower
    }

  // ** becomeCandidate

  //  ____
  // | __ )  ___  ___ ___  _ __ ___   ___
  // |  _ \ / _ \/ __/ _ \| '_ ` _ \ / _ \
  // | |_) |  __/ (_| (_) | | | | | |  __/
  // |____/ \___|\___\___/|_| |_| |_|\___|
  //   ____                _ _     _       _
  //  / ___|__ _ _ __   __| (_) __| | __ _| |_ ___
  // | |   / _` | '_ \ / _` | |/ _` |/ _` | __/ _ \
  // | |__| (_| | | | | (_| | | (_| | (_| | ||  __/
  //  \____\__,_|_| |_|\__,_|_|\__,_|\__,_|\__\___|

  /// After timeout a Member must become Candidate
  let becomeCandidate () =
    raft {
      do! logInfo "becomeCandidate" "becoming candidate"
      let! state = get
      let term = state.CurrentTerm + 1<term>
      do! logDebug "becomeCandidate" <| sprintf "setting term to %d" term
      do! setCurrentTerm term
      do! resetVotes ()
      do! voteForMyself ()
      do! setLeader None
      do! setState Candidate
      // 150–300ms see page 6 in https://raft.github.io/raft.pdf
      let elapsed = 1<ms> * rand.Next(10, int state.ElectionTimeout)
      do! logDebug "becomeCandidate" <| sprintf "setting timeoutElapsed to %d" elapsed
      do! setTimeoutElapsed elapsed
      do! requestAllVotes ()
    }

  // ** receiveVoteResponse

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Send ->                    _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let receiveVoteResponse (nid : MemberId) (vote : VoteResponse) =
    raft {
      let! state = get

      do! (if vote.Granted then "granted" else "not granted")
          |> sprintf "%O responded to vote request with: %s" nid
          |> logDebug "receiveVoteResponse"

      /// The term must not be bigger than current raft term,
      /// otherwise set term to vote term become follower.
      if vote.Term > state.CurrentTerm then
        do! sprintf "(vote term: %d) > (current term: %d). Setting to %d."
              vote.Term
              state.CurrentTerm
              state.CurrentTerm
            |> logDebug "receiveVoteResponse"
        do! setCurrentTerm vote.Term
        do! setLeader (Some nid)
        do! becomeFollower ()

      /// If the vote term is smaller than current term it is considered an
      /// error and the client will be notified.
      elif vote.Term < state.CurrentTerm then
        do! sprintf "Failed: (vote term: %d) < (current term: %d). VoteTermMismatch."
              vote.Term
              state.CurrentTerm
            |> logDebug "receiveVoteResponse"
        return!
          "Vote Term Mismatch"
          |> Error.asRaftError (tag "receiveVoteResponse")
          |> failM

      /// Process the vote if current state of our Raft must be candidate..
      else
        match state.State with
        | Leader -> return ()
        | Follower ->
          /// ...otherwise we respond with the respective RaftError.
          do! logDebug "receiveVoteResponse" "Failed: NotCandidate"
          return!
            "Not Candidate"
            |> Error.asRaftError (tag "receiveVoteResponse")
            |> failM
        | Candidate ->
          if vote.Granted then
            let! mem = getMember nid
            match mem with
            // Could not find the mem in current configuration(s)
            | None ->
              do! logDebug "receiveVoteResponse" "Failed: vote granted but NoMember"
              return!
                "No Node"
                |> Error.asRaftError (tag "receiveVoteResponse")
                |> failM
            // found the mem
            | Some mem ->
              do! setVoting mem true

              let! transitioning = inJointConsensus ()

              // in joint consensus
              if transitioning then
                //      _       _       _
                //     | | ___ (_)_ __ | |_
                //  _  | |/ _ \| | '_ \| __|
                // | |_| | (_) | | | | | |_
                //  \___/ \___/|_|_| |_|\__| consensus.
                //
                // we probe for a majority in both configurations
                let! newConfig = numVotesForMe () >>= regularMajority
                let! oldConfig = numVotesForMeOldConfig () >>= oldConfigMajority

                do! sprintf "In JointConsensus (majority new config: %b) (majority old config: %b)"
                      newConfig
                      oldConfig
                    |> logDebug "receiveVoteResponse"

                // and finally, become leader if we have a majority in either
                // configuration
                if newConfig || oldConfig then
                  do! becomeLeader ()
              else
                //  ____                  _
                // |  _ \ ___  __ _ _   _| | __ _ _ __
                // | |_) / _ \/ _` | | | | |/ _` | '__|
                // |  _ <  __/ (_| | |_| | | (_| | |
                // |_| \_\___|\__, |\__,_|_|\__,_|_| configuration.
                //            |___/
                // the base case: we are not in joint consensus so we just use
                // regular configuration functions
                let! majority = numVotesForMe () >>= regularMajority

                do! sprintf "(majority for config: %b)" majority
                    |> logDebug "receiveVoteResponse"

                if majority then
                  do! becomeLeader ()
      }

  // ** sendVoteRequest

  /// Request a from a given peer
  let sendVoteRequest (mem : RaftMember) =
    raft {
      let! term = currentTerm()
      let! self = self()
      let! log = log()
      let! cbs = read
      do! mem.Status
          |> sprintf "(to: %s) (state: %A)" (string mem.Id)
          |> logDebug "sendVoteRequest"
      do cbs.SendRequestVote mem {
        Term         = term
        Candidate    = self
        LastLogIndex = Log.index log
        LastLogTerm  = Log.term log
      }
    }

  // ** requestAllVotes

  let requestAllVotes () =
    raft {
        let! self = self ()
        let! peers = logicalPeers ()
        do! logInfo "requestAllVotes" "requesting all votes"
        for peer in peers do
          if self.Id <> peer.Value.Id then
            do! sendVoteRequest peer.Value
      }

  // ** validateTerm

  ///////////////////////////////////////////////////////
  //   ____  Should I?       _    __     __    _       //
  //  / ___|_ __ __ _ _ __ | |_  \ \   / /__ | |_ ___  //
  // | |  _| '__/ _` | '_ \| __|  \ \ / / _ \| __/ _ \ //
  // | |_| | | | (_| | | | | |_    \ V / (_) | ||  __/ //
  //  \____|_|  \__,_|_| |_|\__|    \_/ \___/ \__\___| //
  ///////////////////////////////////////////////////////

  /// if the vote's term is lower than this servers current term,
  /// decline the vote
  let private validateTerm (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Invalid Term")
    (vote.Term < state.CurrentTerm, err)

  // ** alreadyVoted

  let private alreadyVoted (state: RaftState) =
    let err = RaftError (tag "shouldGrantVote","Already Voted")
    (Option.isSome state.VotedFor, err)

  // ** validateLastLog

  let private validateLastLog vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid Last Log")
    let result =
      vote.LastLogTerm = RaftState.lastLogTerm state &&
      RaftState.currentIndex state <= vote.LastLogIndex
    (result,err)

  // ** validateLastLogTerm

  let private validateLastLogTerm vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid LastLogTerm")
    (RaftState.lastLogTerm state < vote.LastLogTerm, err)

  // ** validateCurrentIdx

  let private validateCurrentIdx state =
    let err = RaftError (tag "shouldGrantVote","Invalid Current Index")
    (RaftState.currentIndex state = 0<index>, err)

  // ** validateCandiate

  let private validateCandidate (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Candidate Unknown")
    (RaftState.getMember vote.Candidate.Id state |> Option.isNone, err)

  // ** shouldGrantVote

  let shouldGrantVote (vote: VoteRequest) =
    raft {
      let err = RaftError(tag "shouldGrantVote","Log Incomplete")
      let! state = get
      let result =
        validation {       // predicate               result  input
          return! validate (validateTerm vote)        false   state
          return! validate  alreadyVoted              false   state
          return! validate (validateCandidate vote)   false   state
          return! validate  validateCurrentIdx        true    state
          return! validate (validateLastLogTerm vote) true    state
          return! validate (validateLastLog vote)     true    state
          return (false, err)
        }
        |> runValidation

      if fst result then
        do! vote.Candidate.Id
            |> sprintf "granted vote to %O"
            |> logDebug "shouldGrantVote"
      else
        do! snd result
            |> sprintf "did not grant vote to %O. reason: %A" vote.Candidate.Id
            |> logDebug "shouldGrantVote"
      return result
    }

  // ** maybeResetFollower

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Receive->                  _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let private maybeResetFollower (nid: MemberId) (vote : VoteRequest) =
    raft {
      let! term = currentTerm ()
      if term < vote.Term then
        do! logDebug "maybeResetFollower" "current term < vote Term, resetting to follower state"
        do! setCurrentTerm vote.Term
        do! setLeader (Some nid)
        do! becomeFollower ()
        do! voteFor None
    }

  // ** processVoteRequest

  let private processVoteRequest (vote : VoteRequest) =
    raft {
      let! result = shouldGrantVote vote
      match result with
      | (true,_) ->
        let! leader = isLeader ()
        let! candidate = isCandidate ()
        if not leader && not candidate then
          do! voteForId vote.Candidate.Id
          do! setTimeoutElapsed 0<ms>
          let! term = currentTerm ()
          return {
            Term    = term
            Granted = true
            Reason  = None
          }
        else
          do! logDebug "processVoteRequest" "vote request denied: NotVotingState"
          return!
            "Not Voting State"
            |> Error.asRaftError (tag "processVoteRequest")
            |> failM
      | (false, err) ->
        let! term = currentTerm ()
        return {
          Term    = term
          Granted = false
          Reason  = Some err
        }
    }

  // ** receiveVoteRequest

  let receiveVoteRequest (nid : MemberId) (vote : VoteRequest) =
    raft {
      let! mem = getMember nid
      match mem with
      | Some _ ->
        do! maybeResetFollower nid vote
        let! result = processVoteRequest vote

        let str = sprintf "mem %s requested vote. granted: %b"
                    (string nid)
                    result.Granted
        do! logInfo "receiveVoteRequest" str
        return result
      | _ ->
        do! logInfo "receiveVoteRequest" <| sprintf "requested denied. NoMember %s" (string nid)

        let! trm = currentTerm ()
        let err = RaftError (tag "processVoteRequest", "Not Voting State")
        return {
          Term    = trm
          Granted = false
          Reason  = Some err
        }
    }

  // ** startElection

  //  ____  _             _     _____ _           _   _
  // / ___|| |_ __ _ _ __| |_  | ____| | ___  ___| |_(_) ___  _ __
  // \___ \| __/ _` | '__| __| |  _| | |/ _ \/ __| __| |/ _ \| '_ \
  //  ___) | || (_| | |  | |_  | |___| |  __/ (__| |_| | (_) | | | |
  // |____/ \__\__,_|_|   \__| |_____|_|\___|\___|\__|_|\___/|_| |_|

  /// start an election by becoming candidate
  let startElection () =
    raft {
      let! currentIndex = currentIndex ()
      let! elapsed = timeoutElapsed ()
      let! electionTimeout = electionTimeout ()
      let! term = currentTerm ()
      let str =
        String.Format(
          "(elapsed: {0}) (elec-timeout: {1}) (term: {2}) (ci: {3})",
          elapsed,
          electionTimeout,
          currentTerm,
          currentIndex)
      do! logDebug "startElection" str
      do! becomeCandidate ()
    }

  // ** periodic

  //  ____           _           _ _
  // |  _ \ ___ _ __(_) ___   __| (_) ___
  // | |_) / _ \ '__| |/ _ \ / _` | |/ __|
  // |  __/  __/ |  | | (_) | (_| | | (__
  // |_|   \___|_|  |_|\___/ \__,_|_|\___|

  let periodic (elapsed : Timeout) =
    raft {
      let! state = state()
      let! currentlyElapsed = timeoutElapsed ()
      do! setTimeoutElapsed (currentlyElapsed + elapsed)

      match state with
      | Leader ->
        // if in JointConsensus
        let! consensus = inJointConsensus ()
        let! timedout = requestTimedOut ()

        if consensus then
          // check if any mems are still marked non-voting/Joining
          // are mems are voting and have caught up
          let! waiting = hasNonVotingMembers ()
          if not waiting then
            let! term = currentTerm ()
            let response = EntryResponse.create term 0<index>
            let! mems = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
            let log = Configuration(response.Id, 0<index>, term, mems, None)
            do! handleLog log response >>= ignoreM
          else
            do! sendAllAppendEntries ()
        // the regular case is we need to ping our followers so as to not provoke an election
        elif timedout then
          do! sendAllAppendEntries ()
      | _ ->
        // have to double check the code here to ensure new elections are really only called when
        // not enough votes could be garnered
        let! num = numMembers ()
        let! timedout = electionTimedOut ()
        if timedout && num > 1 then
          do! startElection ()
        elif timedout && num = 1 then
          do! becomeLeader ()
        else
          do! recountPeers ()

      let! coi = commitIndex ()
      let! lai = lastAppliedIndex ()
      if lai < coi then
        do! applyEntries ()
      do! maybeSnapshot ()
    }
