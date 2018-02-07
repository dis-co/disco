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
        do! Raft.addMemberM peer
        do! expectM "Should have no current leader" None Raft.currentLeader
        do! Raft.setTermM (term 5)

        let msg =
          { Term         = term 1
          ; PrevLogIdx   = index 0
          ; PrevLogTerm  = term 0
          ; LeaderCommit = index 0
          ; Entries      = None }

        let! result = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Request should have failed" true AppendResponse.failed result
        do! expectM "Should still not have a leader" None Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_need_mem =
    testCase "follower recv appendentries does not need mem" <| fun _ ->
      raft {
        do! Raft.addMemberM (Member.create (DiscoId.Create()))
        let msg =
          { Term         = term 1
          ; PrevLogIdx   = index 0
          ; PrevLogTerm  = term 0
          ; LeaderCommit = index 1
          ; Entries      = None }

        let! response = Raft.receiveAppendEntries None msg
        expect "Request should be success" true AppendResponse.succeeded response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_updates_currentterm_if_term_gt_currentterm =
    testCase "follower recv appendentries updates currentterm if term gt currentterm" <| fun _ ->

      raft {
        let peer = Member.create (DiscoId.Create())
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        do! expectM "Should not have a leader" None Raft.currentLeader
        let msg = {
          Term = term 2
          PrevLogIdx = index 0
          PrevLogTerm = term 0
          LeaderCommit = index 0
          Entries = None
        }

        let! (response: AppendResponse) = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should be successful" true AppendResponse.succeeded response
        expect "Response should have term 2" (term 2) AppendResponse.term response

        do! expectM "Raft should have term 2" (term 2) Raft.currentTerm
        do! expectM "should have leader" (Some peer.Id) Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_log_if_no_entries_are_specified =
    testCase "follower recv appendentries does not log if no entries are specified" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! Raft.addMemberM peer
        do! Raft.setStateM Follower
        do! expectM "Should have 0 log entries" 0 Raft.numLogs
        let msg =
          { Term = term 1
          ; PrevLogIdx = index 1
          ; PrevLogTerm = term 4
          ; LeaderCommit = index 5
          ; Entries = None
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should still have 0 log entries" 0 Raft.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_increases_log =
    testCase "follower recv appendentries increases log" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! Raft.addMemberM peer
        do! Raft.setStateM Follower
        do! expectM "Should log count 0" 0 Raft.numLogs
        let msg =
          { Term = term 3
          ; PrevLogIdx = index 0
          ; PrevLogTerm = term 1
          ; LeaderCommit = index 5
          ; Entries = Log.make (term 2) defSM |> Some
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have log count 1" 1 Raft.numLogs
        let! entry = Raft.getEntryAtM (index 1)
        expect "Should have term 2" (term 2) (Option.get >> LogEntry.term) entry
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_reply_false_if_doesnt_have_log_at_prev_log_idx_which_matches_prev_log_term =
    testCase "follower recv appendentries reply false if doesnt have log at prev log idx which matches prev log term" <| fun _ ->
      raft {
        let peer = Member.create (DiscoId.Create())
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 2)

        let msg =
          { Term = term 2
          ; PrevLogIdx = index 1
          ; PrevLogTerm = term 1
          ; LeaderCommit = index 5
          ; Entries = Log.make (term 0) defSM |> Some
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not have succeeded" true AppendResponse.failed response
      }
      |> runWithDefaults
      |> ignore

  let _entries_for_conflict_tests (payload : StateMachine array) =
    raft {
      for t in payload do
        do! Raft.createEntryM t >>= ignoreM
    }

  let follower_recv_appendentries_delete_entries_if_conflict_with_new_entries =
    testCase "follower recv appendentries delete entries if conflict with new entries" <| fun _ ->
      let raft' = defaultServer ()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      raft {
        let getNth n =
          Raft.getEntryAt n  >>
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

        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)

        do! _entries_for_conflict_tests data // add some log entries

        let addCue = AddCue {
          Id = DiscoId.Create()
          Name = name "four"
          Slices = [| |]
        }

        let newer = {
          Term         = term 2
          PrevLogIdx   = index 1
          PrevLogTerm  = term 1
          LeaderCommit = index 5
          Entries      = Log.make (term 2) addCue |> Some
        }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendResponse.succeeded response

        do! expectM "Should have 2 entries" 2 Raft.numLogs

        do! expectM "First should have 'one' value"   (data.[0]) (getNth (index 1))
        do! expectM "second should have 'four' value" (addCue)   (getNth (index 2))
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_delete_entries_if_current_idx_greater_than_prev_log_idx =
    testCase "follower recv appendentries delete entries if current idx greater than prev log idx" <| fun _ ->
      let getNth n =
        raft {
          let! entry = Raft.getEntryAtM n
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
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term = term 2
          ; PrevLogIdx = index 1
          ; PrevLogTerm = term 1
          ; LeaderCommit = index 5
          ; Entries = None
          }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendResponse.succeeded response
        do! expectM "Should have 1 log entry" 1 Raft.numLogs
        let! entry = getNth (index 1)
        expect "Should have correct value" (Some data.[0]) id entry
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_add_new_entries_not_already_in_log =
    testCase "follower recv appendentries add new entries not already in log" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), index 2, term 1, DataSnapshot (State.Empty),
            Some <| LogEntry((DiscoId.Create()), index 2, term 1, DataSnapshot (State.Empty), None))

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)

        let newer =
          { Term = term 1
          ; PrevLogIdx = index 0
          ; PrevLogTerm = term 1
          ; LeaderCommit = index 5
          ; Entries = Some log
          }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have 2 logs" 2 Raft.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_add_dupe_entries_already_in_log =
    testCase "follower recv appendentries does not add dupe entries already in log" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let entry = LogEntry((DiscoId.Create()), index 2, term 1, DataSnapshot (State.Empty), None)
      let log = Log.fromEntries entry

      let next =
        { Term = term 1
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 1
        ; LeaderCommit = index 5
        ; Entries = Some entry
        }

      let raft' = defaultServer ()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should be a success" true AppendResponse.succeeded response

        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should still be a success" true AppendResponse.succeeded response
        do! expectM "Should have log count 1" 1 Raft.numLogs

        let log'' = Log.append (Log.make (term 1) (DataSnapshot (State.Empty))) log
        let msg = { next with Entries = log''.Data }

        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendResponse.succeeded response
        do! expectM "Should have 2 entries now" 2 Raft.numLogs
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_prevLogIdx =
    testCase "follower recv appendentries set commitidx to prevLogIdx" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), index 0, term 1, DataSnapshot (State.Empty),
            Some <| LogEntry((DiscoId.Create()), index 0, term 1, DataSnapshot (State.Empty),
                Some <| LogEntry((DiscoId.Create()), index 0, term 1, DataSnapshot (State.Empty),
                    Some <| LogEntry((DiscoId.Create()), index 0, term 1, DataSnapshot (State.Empty), None))))

      let msg =
        { Term = term 1
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 1
        ; LeaderCommit = index 5
        ; Entries = Some log
        }

      raft {
        do! Raft.addMemberM peer
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should have been successful" true AppendResponse.succeeded response
        expect "Should have correct CurrentIndex" (index 4) AppendResponse.currentIndex response
        do! expectM "Should have commit index 4" (index 4) Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_LeaderCommit =
    testCase "follower recv appendentries set commitidx to LeaderCommit" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log =
        LogEntry((DiscoId.Create()), index 0, term 1,  DataSnapshot (State.Empty),
          Some <| LogEntry((DiscoId.Create()), index 0, term 1,  DataSnapshot (State.Empty),
              Some <| LogEntry((DiscoId.Create()), index 0, term 1,  DataSnapshot (State.Empty),
                  Some <| LogEntry((DiscoId.Create()), index 0, term 1,  DataSnapshot (State.Empty), None))))

      let msg =
        { Term = term 1
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 1
        ; LeaderCommit = index 0
        ; Entries = Some log
        }

      raft {
        do! Raft.addMemberM peer
        let! response1 = Raft.receiveAppendEntries (Some peer.Id) msg
        let! response2 = Raft.receiveAppendEntries (Some peer.Id) { msg with PrevLogIdx = index 3; LeaderCommit = index 3; Entries = None }
        expect "Should have been successful" true AppendResponse.succeeded response2
        do! expectM "Should have commit index 3" (index 3) Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore


  let follower_recv_appendentries_failure_includes_current_idx =
    testCase "follower recv appendentries failure includes current idx" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let log id = LogEntry(id, index  0, term 1, DataSnapshot (State.Empty), None)

      let msg =
        { Term = term 0
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term  0
        ; LeaderCommit = index 0
        ; Entries = None
        }

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        do! Raft.appendEntryM (log (DiscoId.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should not be successful" true AppendResponse.failed response
        expect "Should have current index 1" (index 1) AppendResponse.currentIndex response

        do! Raft.appendEntryM (log (DiscoId.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not be successful" true AppendResponse.failed response
        expect "Should have current index 2" (index 2) AppendResponse.currentIndex response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_resets_election_timeout =
    testCase "follower recv appendentries resets election timeout" <| fun _ ->
      let peer = Member.create (DiscoId.Create())

      let msg =
        { Term = term 1
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 0
        ; LeaderCommit = index 0
        ; Entries = None
        }

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.addMemberM peer
        do! Raft.periodic 900<ms>
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should have timeout elapsed 0" 0<ms> Raft.timeoutElapsed
      }
      |> runWithDefaults
      |> ignore
