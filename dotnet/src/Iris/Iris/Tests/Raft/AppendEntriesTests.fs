namespace Iris.Tests.Raft

open System
open System.Net
open Expecto
open Iris.Raft
open Iris.Core

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
      let peer = Member.create (Id.Create())

      raft {
        do! Raft.addMemberM peer
        do! expectM "Should have no current leader" None Raft.currentLeader
        do! Raft.setTermM 5u

        let msg =
          { Term         = 1u
          ; PrevLogIdx   = 0u
          ; PrevLogTerm  = 0u
          ; LeaderCommit = 0u
          ; Entries      = None }

        let! result = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Request should have failed" true AppendRequest.failed result
        do! expectM "Should still not have a leader" None Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_need_mem =
    testCase "follower recv appendentries does not need mem" <| fun _ ->
      raft {
        do! Raft.addMemberM (Member.create (Id.Create()))
        let msg =
          { Term         = 1u
          ; PrevLogIdx   = 0u
          ; PrevLogTerm  = 0u
          ; LeaderCommit = 1u
          ; Entries      = None }

        let! response = Raft.receiveAppendEntries None msg
        expect "Request should be success" true AppendRequest.succeeded response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_updates_currentterm_if_term_gt_currentterm =
    testCase "follower recv appendentries updates currentterm if term gt currentterm" <| fun _ ->

      raft {
        let peer = Member.create (Id.Create())
        do! Raft.addMemberM peer
        do! Raft.setTermM 1u
        do! expectM "Should not have a leader" None Raft.currentLeader
        let msg =
          { Term = 2u
          ; PrevLogIdx = 0u
          ; PrevLogTerm = 0u
          ; LeaderCommit = 0u
          ; Entries = None
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should be successful" true AppendRequest.succeeded response
        expect "Response should have term 2" 2u AppendRequest.term response

        do! expectM "Raft should have term 2" 2u Raft.currentTerm
        do! expectM "should have leader" (Some peer.Id) Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_log_if_no_entries_are_specified =
    testCase "follower recv appendentries does not log if no entries are specified" <| fun _ ->
      raft {
        let peer = Member.create (Id.Create())
        do! Raft.addMemberM peer
        do! Raft.setStateM Follower
        do! expectM "Should have 0 log entries" 0u Raft.numLogs
        let msg =
          { Term = 1u
          ; PrevLogIdx = 1u
          ; PrevLogTerm = 4u
          ; LeaderCommit = 5u
          ; Entries = None
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should still have 0 log entries" 0u Raft.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_increases_log =
    testCase "follower recv appendentries increases log" <| fun _ ->
      raft {
        let peer = Member.create (Id.Create())
        do! Raft.addMemberM peer
        do! Raft.setStateM Follower
        do! expectM "Should log count 0" 0u Raft.numLogs
        let msg =
          { Term = 3u
          ; PrevLogIdx = 0u
          ; PrevLogTerm = 1u
          ; LeaderCommit = 5u
          ; Entries = Log.make 2u defSM |> Some
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have log count 1" 1u Raft.numLogs
        let! entry = Raft.getEntryAtM 1u
        expect "Should have term 2" 2u (Option.get >> LogEntry.term) entry
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_reply_false_if_doesnt_have_log_at_prev_log_idx_which_matches_prev_log_term =
    testCase "follower recv appendentries reply false if doesnt have log at prev log idx which matches prev log term" <| fun _ ->
      raft {
        let peer = Member.create (Id.Create())
        do! Raft.addMemberM peer
        do! Raft.setTermM 2u

        let msg =
          { Term = 2u
          ; PrevLogIdx = 1u
          ; PrevLogTerm = 1u
          ; LeaderCommit = 5u
          ; Entries = Log.make 0u defSM |> Some
          }
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not have succeeded" true AppendRequest.failed response
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
      let raft' = defaultServer "string tango"
      let cbs = mkcbs (ref defSM) :> IRaftCallbacks

      raft {
        let getNth n =
          Raft.getEntryAt n  >>
          Option.get    >>
          LogEntry.data >>
          Option.get

        let data =
          [| "one"; "two"; "three"; |]
          |> Array.map (fun name -> AddCue { Id = Id name; Name = name; Slices = [| |] })

        let peer = Member.create (Id.Create())

        do! Raft.addMemberM peer
        do! Raft.setTermM 1u

        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term         = 2u
          ; PrevLogIdx   = 1u
          ; PrevLogTerm  = 1u
          ; LeaderCommit = 5u
          ; Entries      = Log.make 2u (AddCue { Id = Id "four"; Name = "four"; Slices = [| |] }) |> Some
          }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendRequest.succeeded response

        do! expectM "Should have 2 entries" 2u Raft.numLogs

        do! expectM "First should have 'one' value" (AddCue { Id = Id "one"; Name = "one"; Slices = [| |] }) (getNth 1u)
        do! expectM "second should have 'four' value" (AddCue { Id = Id "four"; Name = "four"; Slices = [| |] }) (getNth 2u)
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
        |> Array.map (fun name -> AddCue { Id = Id name; Name = name; Slices = [| |] })

      let peer = Member.create (Id.Create())
      let raft' = defaultServer "string tango"
      let cbs = mkcbs (ref defSM) :> IRaftCallbacks

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM 1u
        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term = 2u
          ; PrevLogIdx = 1u
          ; PrevLogTerm = 1u
          ; LeaderCommit = 5u
          ; Entries = None
          }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendRequest.succeeded response
        do! expectM "Should have 1 log entry" 1u Raft.numLogs
        let! entry = getNth 1u
        expect "Should have correct value" (Some (AddCue { Id = Id "one"; Name = "one"; Slices = [| |] })) id entry
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_add_new_entries_not_already_in_log =
    testCase "follower recv appendentries add new entries not already in log" <| fun _ ->
      let peer = Member.create (Id.Create())

      let log =
        LogEntry((Id.Create()), 2u, 1u, DataSnapshot (State.Empty),
            Some <| LogEntry((Id.Create()), 2u, 1u, DataSnapshot (State.Empty), None))

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM 1u

        let newer =
          { Term = 1u
          ; PrevLogIdx = 0u
          ; PrevLogTerm = 1u
          ; LeaderCommit = 5u
          ; Entries = Some log
          }

        let! response = Raft.receiveAppendEntries (Some peer.Id) newer
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have 2 logs" 2u Raft.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_add_dupe_entries_already_in_log =
    testCase "follower recv appendentries does not add dupe entries already in log" <| fun _ ->
      let peer = Member.create (Id.Create())

      let entry = LogEntry((Id.Create()), 2u, 1u, DataSnapshot (State.Empty), None)
      let log = Log.fromEntries entry

      let next =
        { Term = 1u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 1u
        ; LeaderCommit = 5u
        ; Entries = Some entry
        }

      let raft' = defaultServer "server"

      let cbs = mkcbs (ref defSM) :> IRaftCallbacks

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM 1u
        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should be a success" true AppendRequest.succeeded response

        let! response = Raft.receiveAppendEntries (Some peer.Id) next
        expect "Should still be a success" true AppendRequest.succeeded response
        do! expectM "Should have log count 1" 1u Raft.numLogs

        let log'' = Log.append (Log.make 1u (DataSnapshot (State.Empty))) log
        let msg = { next with Entries = log''.Data }

        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have 2 entries now" 2u Raft.numLogs
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_prevLogIdx =
    testCase "follower recv appendentries set commitidx to prevLogIdx" <| fun _ ->
      let peer = Member.create (Id.Create())

      let log =
        LogEntry((Id.Create()), 0u, 1u, DataSnapshot (State.Empty),
            Some <| LogEntry((Id.Create()), 0u, 1u, DataSnapshot (State.Empty),
                Some <| LogEntry((Id.Create()), 0u, 1u, DataSnapshot (State.Empty),
                    Some <| LogEntry((Id.Create()), 0u, 1u, DataSnapshot (State.Empty), None))))

      let msg =
        { Term = 1u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 1u
        ; LeaderCommit = 5u
        ; Entries = Some log
        }

      raft {
        do! Raft.addMemberM peer
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should have been successful" true AppendRequest.succeeded response
        expect "Should have correct CurrentIndex" 4u AppendRequest.currentIndex response
        do! expectM "Should have commit index 4" 4u Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_LeaderCommit =
    testCase "follower recv appendentries set commitidx to LeaderCommit" <| fun _ ->
      let peer = Member.create (Id.Create())

      let log =
        LogEntry((Id.Create()), 0u, 1u,  DataSnapshot (State.Empty),
          Some <| LogEntry((Id.Create()), 0u, 1u,  DataSnapshot (State.Empty),
              Some <| LogEntry((Id.Create()), 0u, 1u,  DataSnapshot (State.Empty),
                  Some <| LogEntry((Id.Create()), 0u, 1u,  DataSnapshot (State.Empty), None))))

      let msg =
        { Term = 1u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 1u
        ; LeaderCommit = 0u
        ; Entries = Some log
        }

      raft {
        do! Raft.addMemberM peer
        let! response1 = Raft.receiveAppendEntries (Some peer.Id) msg
        let! response2 = Raft.receiveAppendEntries (Some peer.Id) { msg with PrevLogIdx = 3u; LeaderCommit = 3u; Entries = None }
        expect "Should have been successful" true AppendRequest.succeeded response2
        do! expectM "Should have commit index 3" 3u Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore


  let follower_recv_appendentries_failure_includes_current_idx =
    testCase "follower recv appendentries failure includes current idx" <| fun _ ->
      let peer = Member.create (Id.Create())

      let log id = LogEntry(id, 0u, 1u, DataSnapshot (State.Empty), None)

      let msg =
        { Term = 0u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 0u
        ; LeaderCommit = 0u
        ; Entries = None
        }

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM 1u
        do! Raft.appendEntryM (log (Id.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg

        expect "Should not be successful" true AppendRequest.failed response
        expect "Should have current index 1" 1u AppendRequest.currentIndex response

        do! Raft.appendEntryM (log (Id.Create())) >>= ignoreM
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        expect "Should not be successful" true AppendRequest.failed response
        expect "Should have current index 2" 2u AppendRequest.currentIndex response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_resets_election_timeout =
    testCase "follower recv appendentries resets election timeout" <| fun _ ->
      let peer = Member.create (Id.Create())

      let msg =
        { Term = 1u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 0u
        ; LeaderCommit = 0u
        ; Entries = None
        }

      raft {
        do! Raft.setElectionTimeoutM 1000u
        do! Raft.addMemberM peer
        do! Raft.periodic 900u
        let! response = Raft.receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should have timeout elapsed 0" 0u Raft.timeoutElapsed
      }
      |> runWithDefaults
      |> ignore
