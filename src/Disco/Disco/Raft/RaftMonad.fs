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

// * RaftMonad

/// The RaftMonad is a custom Reader/State monad with a specific constructor to provide more type
/// safety.
///
/// State monads are functions that close over a state value. This state monad is extended by an
/// additional parameter 'Env, the reader monad part.
///
/// This module should be used when composing Raft code, as it wraps the pure RaftState manipulation
/// code with the callbacks environment needed to provide functionality for Raft.

[<NoComparison;NoEquality>]
type RaftMonad<'Env,'State,'T,'Error> =
  MkRM of ('Env -> 'State -> Either<'Error * 'State,'T * 'State>)

// * RaftM

type RaftM<'t,'err> = RaftMonad<IRaftCallbacks, RaftState, 't, 'err>

// * RaftMonad module

[<AutoOpen>]
module RaftMonad =

  // ** get

  /// get current Raft state
  let get = MkRM (fun _ s -> Right (s, s))

  // ** put

  /// update Raft/State to supplied value
  let put s = MkRM (fun _ _ -> Right ((), s))

  // ** read

  /// get the read-only environment value
  let read: RaftM<_,_> = MkRM (fun l s -> Right (l, s))

  // ** apply

  /// unwrap the closure and apply it to the supplied state/env
  let apply (env: 'e) (state: 's) (m: RaftMonad<'e,'s,_,_>)  =
    match m with | MkRM func -> func env state

  // ** runRaft

  /// run the monadic action against state and environment values
  let runRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    apply l s m

  // ** evalRaft

  /// run monadic action against supplied state and evironment and return new state
  let evalRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    match runRaft s l m with
    | Right (_,state) | Left (_,state) -> state

  // ** returnM

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Right` value. This means the computation will,
  /// if possible, continue to the next step.
  let returnM value : RaftMonad<'e,'s,'t,'err> =
    MkRM (fun _ state -> Right(value, state))

  // ** ignoreM

  let ignoreM _ : RaftMonad<'e,'s,unit,'err> =
    MkRM (fun _ state -> Right((), state))

  // ** failM

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Left` value. This means the computation will
  /// not continue past this step and no regular value will be returned.
  let failM l =
    MkRM (fun _ s -> Left (l, s))

  // ** returnFromM

  /// pass through the given action
  let returnFromM func : RaftMonad<'e,'s,'t,'err> =
    func

  // ** zeroM

  let zeroM () =
    MkRM (fun _ state -> Right((), state))

  // ** delayM

  let delayM (f: unit -> RaftMonad<'e,'s,'t,'err>) =
    MkRM (fun env state -> f () |> apply env state)

  // ** bindM

  /// Chain up effectful actions.
  let bindM (m: RaftMonad<'env,'state,'a,'err>)
            (f: 'a -> RaftMonad<'env,'state,'b,'err>) :
            RaftMonad<'env,'state,'b,'err> =
    MkRM (fun env state ->
      match apply env state m with
      | Right  (value,state') -> f value |> apply env state'
      | Left    err           -> Left err)

  // ** (>>=)

  let (>>=) = bindM

  // ** combineM

  let combineM (m1: RaftMonad<_,_,_,_>) (m2: RaftMonad<_,_,_,_>) =
    bindM m1 (fun _ -> m2)

  // ** tryWithM

  let tryWithM (body: RaftMonad<_,_,_,_>) (handler: exn -> RaftMonad<_,_,_,_>) =
    MkRM (fun env state ->
          try apply env state body
          with ex -> apply env state (handler ex))

  // ** tryFinallyM

  let tryFinallyM (body: RaftMonad<_,_,_,_>) handler : RaftMonad<_,_,_,_> =
    MkRM (fun env state ->
          try apply env state body
          finally handler ())

  // ** usingM

  let usingM (resource: ('a :> System.IDisposable)) (body: 'a -> RaftMonad<_,_,_,_>) =
    tryFinallyM (body resource)
      (fun _ -> if not <| isNull (box resource)
                then resource.Dispose())

  // ** whileM

  let rec whileM (guard: unit -> bool) (body: RaftMonad<_,_,_,_>) =
    match guard () with
    | true -> bindM body (fun _ -> whileM guard body)
    | _ -> zeroM ()

  // ** forM

  let rec forM (sequence: seq<_>) (body: 'a -> RaftMonad<_,_,_,_>) : RaftMonad<_,_,_,_> =
    usingM (sequence.GetEnumerator())
      (fun enum -> whileM enum.MoveNext (delayM (fun _ -> body enum.Current)))

  // ** RaftBuilder

  type RaftBuilder() =
    member __.Return(v) = returnM v
    member __.ReturnFrom(v) = returnFromM v
    member __.Bind(m, f) = bindM m f
    member __.Zero() = zeroM ()
    member __.Delay(f) = delayM f
    member __.Combine(a,b) = combineM a b
    member __.TryWith(body, handler) = tryWithM body handler
    member __.TryFinally(body, handler) = tryFinallyM body handler
    member __.Using(res, body) = usingM res body
    member __.While(guard, body) = whileM guard body
    member __.For(seq, body) = forM seq body

  // ** raft

  let raft = new RaftBuilder()

  // ** modify

  let modify (f: RaftState -> RaftState) =
    get >>= (f >> put)

  // ** zoom

  let zoom (f: RaftState -> 'a) =
    get >>= (f >> returnM)

  // ** tag

  let private tag (str: string) = String.Format("Raft.{0}",str)

  // ** logMsg

  let logMsg site level message =
    message
    |> Logger.log level (tag site)
    |> returnM

  // ** debug

  let debug site str = logMsg site Debug str

  // ** info

  let info site str = logMsg site Info str

  // ** warn

  let warn site str = logMsg site Warn str

  // ** error

  let error site str = logMsg site Err str

  // ** currentIndex

  let currentIndex () = zoom RaftState.currentIndex

  // ** currentTerm

  let currentTerm () = zoom RaftState.currentTerm

  // ** isFollower

  let isFollower () = zoom RaftState.isFollower

  // ** isCandidate

  let isCandidate () = zoom RaftState.isCandidate

  // ** isLeader

  let isLeader () = zoom RaftState.isLeader

  // ** inJointConsensus

  let inJointConsensus () = zoom RaftState.inJointConsensus

  // ** hasNonVotingMembers

  let hasNonVotingMembers () = zoom RaftState.hasNonVotingMembers

  // ** configurationChanges

  let configurationChanges () = zoom RaftState.configurationChanges

  // ** logicalPeers

  let logicalPeers () = zoom RaftState.logicalPeers

  // ** countMembers

  let countMembers () = zoom (RaftState.logicalPeers >> RaftState.countMembers)

  // ** numLogicalPeers

  let numLogicalPeers () = zoom RaftState.numLogicalPeers

  // ** recountPeers

  let recountPeers () = modify RaftState.recountPeers

  // ** hasMember

  let hasMember nid = zoom (RaftState.hasMember nid)

  // ** getMember

  let getMember nid = zoom (RaftState.getMember nid)

  // ** getMembers

  let getMembers () = zoom RaftState.peers

  // ** self

  let self () = zoom RaftState.self

  // ** setSelf

  let setSelf self = modify (RaftState.setSelf self)

  // ** configChangeEntry

  let configChangeEntry () = zoom RaftState.configChangeEntry

  // ** persistVote

  let persistVote mem =
    read >>= fun cbs -> cbs.PersistVote mem |> returnM

  // ** persistTerm

  let persistTerm term =
    read >>= fun cbs -> cbs.PersistTerm term |> returnM

  // ** persistLog

  let persistLog log =
    read >>= fun cbs -> cbs.PersistLog log |> returnM

  // ** setCurrentTerm

  let setCurrentTerm term =
    raft {
      do! modify (RaftState.setCurrentTerm term)
      do! persistTerm term
    }

  // ** state

  let state () = zoom RaftState.state

  // ** maxLogDepth

  let maxLogDepth () = zoom RaftState.maxLogDepth

  // ** setMaxLogDepth

  let setMaxLogDepth depth = modify (RaftState.setMaxLogDepth depth)

  // ** setPeers

  let setPeers peers = modify (RaftState.setPeers peers >> RaftState.recountPeers)

  // ** setOldPeers

  let setOldPeers peers = modify (RaftState.setOldPeers peers >> RaftState.recountPeers)

  // ** peers

  let peers () = zoom RaftState.peers

  // ** updateMember

  let updateMember (mem: RaftMember) =
    raft {
      let! state = get
      let updated, state = RaftState.updateMember mem state
      do! put state
      do! recountPeers ()
      if updated then
        let! cbs = read
        // if the mems has structurally changed fire the callback
        do cbs.MemberUpdated mem
    }

  // ** setNextIndex

  /// Set the nextIndex field on Member corresponding to supplied Id (should it exist, that is) and
  /// supplied index. Monadic action.

  let setNextIndex (nid : MemberId) idx =
    raft {
      let! state = get
      let update, state = RaftState.setNextIndex nid idx state
      do! put state
      if update then
        let! env = read
        do! getMember nid >>= (Option.iter env.MemberUpdated >> returnM)
    }

  // ** setMatchIndex

  let setMatchIndex nid idx =
    raft {
      let! state = get
      let update, state = RaftState.setMatchIndex nid idx state
      do! put state
      if update then
        let! env = read
        do! getMember nid >>= (Option.iter env.MemberUpdated >> returnM)
    }

  // ** setLeader

  /// Set States CurrentLeader field to supplied MemberId. Monadic action.

  let setLeader (leader : MemberId option) =
    raft {
      let! state = get
      let update, state = RaftState.setLeader leader state
      do! put state
      if update then
        let! env = read
        let! peers = logicalPeers ()
        for KeyValue(_,peer) in peers do
          do env.MemberUpdated peer
        do env.LeaderChanged leader
    }

  // ** voteFor

  /// Remeber who we have voted for in current election.
  let voteFor (mem: RaftMember option) =
    raft {
      do! modify (RaftState.voteFor mem)
      do! persistVote mem
    }

  // ** voteForId

  /// Remeber who we have voted for in current election
  let voteForId (nid : MemberId)  =
    raft {
      let! mem = getMember nid
      do! voteFor mem
    }

  // ** votedFor

  let votedFor () = zoom RaftState.votedFor

  // ** setVoting

  let setVoting (mem: RaftMember) (vote: bool) =
    raft {
      let msg = String.Format("setting mem {0} voting to {1}", mem.Id, vote)
      do! debug "setVoting" msg
      let! state = get
      let update, state = RaftState.setVoting mem vote state
      do! put state
      if update then
        let! env = read
        do env.MemberUpdated mem
    }

  // ** numMembers

  let numMembers () = zoom RaftState.numMembers

  // ** numOldPeers

  let numOldMembers () = zoom RaftState.numOldMembers

  // ** sendAppendEntries

  let sendAppendEntries (mem: RaftMember) (request: AppendEntries) =
    raft {
      let! idx = currentIndex ()
      let! cbs = read
      let msg =
        sprintf "to: %s ci: %d term: %d leader-commit: %d prv-log-idx: %d prev-log-term: %d"
          (string mem.Id)
          idx
          request.Term
          request.LeaderCommit
          request.PrevLogIdx
          request.PrevLogTerm
      do! debug "sendAppendEntries" msg
      do cbs.SendAppendEntries mem request
    }

  // ** addMember

  let addMember (mem: RaftMember) =
    raft {
      do! modify (RaftState.addMember mem)
      let! env = read
      do env.MemberAdded mem
    }

  // ** addNonVotingMember

  let addNonVotingMember mem = modify (RaftState.addNonVotingMember mem)

  // ** removeMember

  let removeMember mem =
    raft {
      do! modify (RaftState.removeMember mem)
      let! env = read
      do env.MemberRemoved mem
    }

  // ** applyChanges

  let applyChanges changes =
    raft {
      do! modify (RaftState.applyChanges changes)
      let! env = read
      for change in changes do
        match change with
        | MemberAdded mem   -> do env.MemberAdded mem
        | MemberRemoved mem -> do env.MemberRemoved mem
    }

  // ** addMembers

  let addMembers mems =
    raft {
      do! modify (RaftState.addMembers mems)
      let! env = read
      for KeyValue(_,mem) in mems do
        do env.MemberAdded mem
    }

  // ** setMemberState

  let setMemberState mem memstate =
    raft {
      let! state = get
      let updated, state = RaftState.setMemberState mem memstate state
      do! put state
      if updated then
        let! env = read
        let! mem = getMember mem
        do Option.iter env.MemberUpdated mem
    }

  // ** setState

  /// Set current RaftState to supplied state.

  let setState (newstate: MemberState) =
    raft {
      let! current = zoom RaftState.state
      if newstate <> current then
        let! env = read
        do! modify (RaftState.setState newstate)
        do env.StateChanged current newstate
    }

  // ** resetVotes

  let resetVotes () = modify RaftState.resetVotes

  // ** voteForMyself

  let voteForMyself () =
    get >>= fun state -> voteFor (Some state.Member)

  // ** votedForMyself

  let votedForMyself () = zoom RaftState.votedForMyself

  // ** votingMembers

  let votingMembers () = zoom RaftState.votingMembers

  // ** votingMembersForOldConfg

  let votingMembersForOldConfig () = zoom RaftState.votingMembersForOldConfig

  // ** numLogs

  let numLogs () = zoom RaftState.numLogs

  // ** firstIndex

  let firstIndex term = zoom (RaftState.firstIndex term)

  // ** currentLeader

  let currentLeader () = zoom RaftState.currentLeader

  // ** getLeader

  let getLeader () = zoom RaftState.getLeader

  // ** commitIndex

  let commitIndex () = zoom RaftState.commitIndex

  // ** setCommitIndex

  let setCommitIndex idx = modify (RaftState.setCommitIndex idx)

  // ** requestTimedOut

  let requestTimedOut () = zoom RaftState.requestTimedOut

  // ** electionTimedOut

  let electionTimedOut () = zoom RaftState.electionTimedOut

  // ** electionTimeout

  let electionTimeout () = zoom RaftState.electionTimeout

  // ** timeoutElapsed

  let timeoutElapsed () = zoom RaftState.timeoutElapsed

  // ** setTimeoutElapsed

  let setTimeoutElapsed elapsed = modify (RaftState.setTimeoutElapsed elapsed)

  // ** requestTimeout

  let requestTimeout () = zoom RaftState.requestTimeout

  // ** setRequestTimeout

  let setRequestTimeout timeout = modify (RaftState.setRequestTimeout timeout)

  // ** setElectionTimeout

  let setElectionTimeout timeout = modify (RaftState.setRequestTimeout timeout)

  // ** lastAppliedIndex

  let lastAppliedIndex () = zoom RaftState.lastAppliedIndex

  // ** setLastAppliedIndex

  let setLastAppliedIndex index = modify (RaftState.setLastAppliedIndex index)

  // ** lastLogTerm

  let lastLogTerm () = zoom RaftState.lastLogTerm

  // ** entryAt

  let entryAt idx = zoom (RaftState.entryAt idx)

  // ** entriesUntil

  let entriesUntil idx = zoom (RaftState.entriesUntil idx)

  // ** entriesUntilExcluding

  let entriesUntilExcluding idx = zoom (RaftState.entriesUntilExcluding idx)

  // ** log

  let log () = zoom RaftState.log

  // ** setLog

  let setLog log = modify (RaftState.setLog log)

  // ** updateMembers

  let updateMembers f =
    raft {
      let! state = get
      let updated, state = RaftState.updateMembers f state
      do! put state
      if updated then
        let! env = read
        let! peers = logicalPeers()
        for KeyValue(_,peer) in peers do
          do env.MemberUpdated peer
    }

  // ** appendEntry

  let appendEntry (entry: LogEntry) =
    raft {
      let! current = log ()

      // create the new log by appending
      let newlog = Log.append entry current
      do! setLog newlog

      // get back the entries just added
      // (with correct monotonic idx's)
      let result = Log.getn (LogEntry.depth entry) newlog

      match result with
      | Some entries -> do! persistLog entries
      | _ -> ()

      return result
    }

  // ** createEntry

  let createEntry (entry: StateMachine) =
    raft {
      let! term = currentTerm ()
      let log = LogEntry.create 0<index> term entry
      return! appendEntry log
    }

  // ** removeEntry

  /// Delete a log entry at the index specified. Returns the original value if
  /// the record is not found.

  let removeEntry idx =
    raft {
      let! env = read
      let! current = log ()
      match Log.at idx current with
      | Some log ->
        match LogEntry.pop log with
        | Some newlog ->
          // fire delete log callback for all removed items
          match Log.until idx current with
          | Some items -> LogEntry.iter (fun _ entry -> do env.DeleteLog entry) items
          | _ -> ()
          // save the modified log to state
          do! modify (updateLogEntries newlog)
        | _ ->
          do env.DeleteLog log
          do! modify (RaftState.setLog Log.empty)
      | _ -> ()
    }

  // ** updateLogEntries

  let updateLogEntries (entries: LogEntry) (state: RaftState) =
    { state with
        Log = { Index = LogEntry.index entries
                Depth = LogEntry.depth entries
                Data  = Some entries } }

  // ** updateCommitIndex

  let updateCommitIndex () = modify RaftState.updateCommitIndex

  // ** regularMajority

  /// Determine whether a vote count constitutes a majority in the *regular*
  /// configuration (does not cover the joint consensus state).

  let regularMajority votes =
    raft {
      let! num = votingMembers ()
      return RaftState.majority num votes
    }

  // ** oldConfigMajority

  let oldConfigMajority votes =
    raft {
      let! num = votingMembersForOldConfig ()
      return RaftState.majority num votes
    }

  // ** numVotesForMe

  let numVotesForMe () = zoom RaftState.numVotesForMe

  // ** numVotesForMeOldConfig

  let numVotesForMeOldConfig () = zoom RaftState.numVotesForMeOldConfig
