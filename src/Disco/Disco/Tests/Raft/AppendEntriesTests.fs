(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests.Raft

open System
open System.Net
open Expecto
open Disco.Raft
open Disco.Core

[<AutoOpen>]
module AppendEntries =

  ////////////////////////////////////////////////////////////////////////
  //     _                               _ _____       _   _            //
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_(_) ___  ___  //
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| |/ _ \/ __| //
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  __/\__ \ //
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|\___||___/ //
  //         |_|   |_|                                                  //
  ////////////////////////////////////////////////////////////////////////

  let follower_recv_appendentries_reply_false_if_term_less_than_currentterm =
    testCase "follower recv appendentries reply false if term less than currentterm" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      raft {
        do! addMember peer
        do! expectM "Should have no current leader" None RaftState.currentLeader
        do! setCurrentTerm 5<term>

        let msg =
          { Term         = 1<term>
            PrevLogIdx   = 0<index>
            PrevLogTerm  = 0<term>
            LeaderCommit = 0<index>
            Entries      = None }

        let! result = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Request should have failed" true AppendResponse.failed result
        do! expectM "Should still not have a leader" None RaftState.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_need_mem =
    testCase "follower recv appendentries does not need mem" <| fun _ ->
      raft {
        do! addMember (Member.create (DiscoId.Create()))
        let msg =
          { Term         = 1<term>
            PrevLogIdx   = 0<index>
            PrevLogTerm  = 0<term>
            LeaderCommit = 1<index>
            Entries      = None }

        let! response = Raft.receiveAppendEntries None msg
        expect "Request should be success" true AppendResponse.succeeded response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_updates_currentterm_if_term_gt_currentterm =
    testCase "follower recv appendentries updates currentterm if term gt currentterm" <| fun _ ->

      raft {
        let peer = Member.create (DiscoId.Create())
        do! addMember peer
        do! setCurrentTerm 1<term>
        do! expectM "Should not have a leader" None RaftState.currentLeader
        let msg = {
          Term = 2<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 0<term>
          LeaderCommit = 0<index>
          Entries = None
        }

        let! (response: AppendResponse) = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should be successful" true AppendResponse.succeeded response
        expect "Response should have term 2" 2<term> AppendResponse.term response

        do! expectM "Raft should have term 2" 2<term> RaftState.currentTerm
        do! expectM "should have leader" (Some peer.Id) RaftState.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_log_if_no_entries_are_specified =
    testCase "follower recv appendentries does not log if no entries are specified" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! addMember peer
        do! setState Follower
        do! expectM "Should have 0 log entries" 0 RaftState.numLogs
        let msg =
          { Term = 1<term>
            PrevLogIdx = 1<index>
            PrevLogTerm = 4<term>
            LeaderCommit = 5<index>
            Entries = None }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should still have 0 log entries" 0 RaftState.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_increases_log =
    testCase "follower recv appendentries increases log" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! addMember peer
        do! setState Follower
        do! expectM "Should log count 0" 0 RaftState.numLogs
        let msg =
          { Term = 3<term>
            PrevLogIdx = 0<index>
            PrevLogTerm = 1<term>
            LeaderCommit = 5<index>
            Entries = Log.make 2<term> defSM |> Some }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have log count 1" 1 RaftState.numLogs
        let! entry = entryAt 1<index>
        expect "Should have term 2" 2<term> (Option.get >> LogEntry.term) entry
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_reply_false_if_doesnt_have_log_at_prev_log_idx_which_matches_prev_log_term =
    testCase "follower recv appendentries reply false if doesnt have log at prev log idx which matches prev log term" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! addMember peer
        do! setCurrentTerm 2<term>

        let msg =
          { Term = 2<term>
            PrevLogIdx = 1<index>
            PrevLogTerm = 1<term>
            LeaderCommit = 5<index>
            Entries = Log.make 0<term> defSM |> Some }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not have succeeded" true AppendResponse.failed response
      }
      |> runWithDefaults
      |> ignore

  let _entries_for_conflict_tests (payload : StateMachine array) =
    raft {
      for t in payload do
        do! createEntry t >>= ignoreM
    }

  let follower_recv_appendentries_delete_entries_if_conflict_with_new_entries =
    testCase "follower recv appendentries delete entries if conflict with new entries" <| fun _ ->
      let raft' = defaultServer ()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      raft {
        let getNth n =
          RaftState.entryAt n >>
          Option.get    >>
          LogEntry.data >>
          Option.get

        let data =
          [| "one"; "two"; "three"; |]
          |> Array.map (fun name' -> AddCue {
            Id = DiscoId.Create()
            Name = name name'
            Slices = Array.empty
          })

        let peer = Member.create (DiscoId.Create())

        do! addMember peer
        do! setCurrentTerm 1<term>

        do! _entries_for_conflict_tests data // add some log entries

        let addCue = AddCue {
          Id = DiscoId.Create()
          Name = name "four"
          Slices = [| |]
        }

        let newer = {
          Term         = 2<term>
          PrevLogIdx   = 1<index>
          PrevLogTerm  = 1<term>
          LeaderCommit = 5<index>
          Entries      = Log.make 2<term> addCue |> Some
        }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendResponse.succeeded response

        do! expectM "Should have 2 entries" 2 RaftState.numLogs

        do! expectM "First should have 'one' value"   (data.[0]) (getNth 1<index>)
        do! expectM "second should have 'four' value" (addCue)   (getNth 2<index>)
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_delete_entries_if_current_idx_greater_than_prev_log_idx =
    testCase "follower recv appendentries delete entries if current idx greater than prev log idx" <| fun _ ->
      let getNth n =
        raft {
          let! entry = entryAt n
          return entry |> Option.get |> LogEntry.data
        }

      let data =
        [| "one"; "two"; "three"; |]
        |> Array.map (fun name' -> AddCue {
          Id = DiscoId.Create()
          Name = name name'
          Slices = Array.empty
        })

      let peer = Member.create (DiscoId.Create())
      let raft' = defaultServer ()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      raft {
        do! addMember peer
        do! setCurrentTerm 1<term>
        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term = 2<term>
            PrevLogIdx = 1<index>
            PrevLogTerm = 1<term>
            LeaderCommit = 5<index>
            Entries = None }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendResponse.succeeded response
        do! expectM "Should have 1 log entry" 1 RaftState.numLogs
        let! entry = getNth 1<index>
        expect "Should have correct value" (Some data.[0]) id entry
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_add_new_entries_not_already_in_log =
    testCase "follower recv appendentries add new entries not already in log" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), 2<index>, 1<term>, DataSnapshot (State.Empty),
            Some <| LogEntry((DiscoId.Create()), 2<index>, 1<term>, DataSnapshot (State.Empty), None))

      raft {
        do! addMember peer
        do! setCurrentTerm 1<term>

        let newer =
          { Term = 1<term>
            PrevLogIdx = 0<index>
            PrevLogTerm = 1<term>
            LeaderCommit = 5<index>
            Entries = Some log }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have 2 logs" 2 RaftState.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_add_dupe_entries_already_in_log =
    testCase "follower recv appendentries does not add dupe entries already in log" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let entry = LogEntry((DiscoId.Create()), 2<index>, 1<term>, DataSnapshot (State.Empty), None)
      let log = Log.fromEntries entry

      let next =
        { Term = 1<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 1<term>
          LeaderCommit = 5<index>
          Entries = Some entry }

      let raft' = defaultServer ()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      raft {
        do! addMember peer
        do! setCurrentTerm 1<term>
        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should be a success" true AppendResponse.succeeded response

        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should still be a success" true AppendResponse.succeeded response
        do! expectM "Should have log count 1" 1 RaftState.numLogs

        let log'' = Log.append (Log.make 1<term> (DataSnapshot (State.Empty))) log
        let msg = { next with Entries = log''.Data }

        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have 2 entries now" 2 RaftState.numLogs
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_prevLogIdx =
    testCase "follower recv appendentries set commitidx to prevLogIdx" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), 0<index>, 1<term>, DataSnapshot (State.Empty),
            Some $ LogEntry((DiscoId.Create()), 0<index>, 1<term>, DataSnapshot (State.Empty),
                Some $ LogEntry((DiscoId.Create()), 0<index>, 1<term>, DataSnapshot (State.Empty),
                    Some $ LogEntry((DiscoId.Create()), 0<index>, 1<term>, DataSnapshot (State.Empty), None))))

      let msg =
        { Term = 1<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 1<term>
          LeaderCommit = 5<index>
          Entries = Some log }

      raft {
        do! addMember peer
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should have been successful" true AppendResponse.succeeded response
        expect "Should have correct CurrentIndex" 4<index> AppendResponse.currentIndex response
        do! expectM "Should have commit index 4" 4<index> RaftState.commitIndex
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_LeaderCommit =
    testCase "follower recv appendentries set commitidx to LeaderCommit" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), 0<index>, 1<term>,  DataSnapshot (State.Empty),
          Some <| LogEntry((DiscoId.Create()), 0<index>, 1<term>,  DataSnapshot (State.Empty),
              Some <| LogEntry((DiscoId.Create()), 0<index>, 1<term>,  DataSnapshot (State.Empty),
                  Some <| LogEntry((DiscoId.Create()), 0<index>, 1<term>,  DataSnapshot (State.Empty), None))))

      let msg =
        { Term = 1<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 1<term>
          LeaderCommit = 0<index>
          Entries = Some log }

      raft {
        do! addMember peer
        let! response1 = Raft.receiveAppendEntries (Some peer.Id) msg
        let! response2 = Raft.receiveAppendEntries (Some peer.Id) { msg with PrevLogIdx = 3<index>; LeaderCommit = 3<index>; Entries = None }
        expect "Should have been successful" true AppendResponse.succeeded response2
        do! expectM "Should have commit index 3" 3<index> RaftState.commitIndex
      }
      |> runWithDefaults
      |> ignore


  let follower_recv_appendentries_failure_includes_current_idx =
    testCase "follower recv appendentries failure includes current idx" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log id = LogEntry(id, 0<index>, 1<term>, DataSnapshot (State.Empty), None)

      let msg =
        { Term = 0<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 0<term>
          LeaderCommit = 0<index>
          Entries = None }

      raft {
        do! addMember peer
        do! setCurrentTerm 1<term>
        do! appendEntry (log (DiscoId.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should not be successful" true AppendResponse.failed response
        expect "Should have current index 1" 1<index> AppendResponse.currentIndex response

        do! appendEntry (log (DiscoId.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not be successful" true AppendResponse.failed response
        expect "Should have current index 2" 2<index> AppendResponse.currentIndex response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_resets_election_timeout =
    testCase "follower recv appendentries resets election timeout" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let msg =
        { Term = 1<term>
          PrevLogIdx = 0<index>
          PrevLogTerm = 0<term>
          LeaderCommit = 0<index>
          Entries = None }

      raft {
        do! setElectionTimeout 1000<ms>
        do! addMember peer
        do! Raft.periodic 900<ms>
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should have timeout elapsed 0" 0<ms> RaftState.timeoutElapsed
      }
      |> runWithDefaults
      |> ignore
