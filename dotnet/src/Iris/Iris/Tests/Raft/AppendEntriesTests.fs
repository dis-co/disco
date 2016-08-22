namespace Iris.Tests.Raft

open System
open System.Net
open Fuchu
open Fuchu.Test
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
      let peer = Node.create (Guid.Create())()

      raft {
        do! addNodeM peer
        do! expectM "Should have no current leader" None Raft.currentLeader
        do! setTermM 5UL

        let msg =
          { Term         = 1UL
          ; PrevLogIdx   = 0UL
          ; PrevLogTerm  = 0UL
          ; LeaderCommit = 0UL
          ; Entries      = None }

        let! result = receiveAppendEntries (Some peer.Id) msg
        expect "Request should have failed" true AppendRequest.failed result
        do! expectM "Should still not have a leader" None Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_need_node =
    testCase "follower recv appendentries does not need node" <| fun _ ->
      raft {
        do! addNodeM (Node.create (Guid.Create()) ())
        let msg =
          { Term         = 1UL
          ; PrevLogIdx   = 0UL
          ; PrevLogTerm  = 0UL
          ; LeaderCommit = 1UL
          ; Entries      = None }

        let! response = receiveAppendEntries None msg
        expect "Request should be success" true AppendRequest.succeeded response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_updates_currentterm_if_term_gt_currentterm =
    testCase "follower recv appendentries updates currentterm if term gt currentterm" <| fun _ ->

      raft {
        let peer = Node.create (Guid.Create()) ()
        do! addNodeM peer
        do! setTermM 1UL
        do! expectM "Should not have a leader" None currentLeader
        let msg =
          { Term = 2UL
          ; PrevLogIdx = 0UL
          ; PrevLogTerm = 0UL
          ; LeaderCommit = 0UL
          ; Entries = None
          }
        let! response = receiveAppendEntries (Some peer.Id) msg

        expect "Should be successful" true AppendRequest.succeeded response
        expect "Response should have term 2" 2UL AppendRequest.term response

        do! expectM "Raft should have term 2" 2UL currentTerm
        do! expectM "Raft should have leader" (Some peer.Id) Raft.currentLeader
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_log_if_no_entries_are_specified =
    testCase "follower recv appendentries does not log if no entries are specified" <| fun _ ->
      raft {
        let peer = Node.create (Guid.Create()) ()
        do! addNodeM peer
        do! setStateM Follower
        do! expectM "Should have 0 log entries" 0UL numLogs
        let msg =
          { Term = 1UL
          ; PrevLogIdx = 1UL
          ; PrevLogTerm = 4UL
          ; LeaderCommit = 5UL
          ; Entries = None
          }
        let! response = receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should still have 0 log entries" 0UL Raft.numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_increases_log =
    testCase "follower recv appendentries increases log" <| fun _ ->
      raft {
        let peer = Node.create (Guid.Create()) ()
        do! addNodeM peer
        do! setStateM Follower
        do! expectM "Should log count 0" 0UL numLogs
        let msg =
          { Term = 3UL
          ; PrevLogIdx = 0UL
          ; PrevLogTerm = 1UL
          ; LeaderCommit = 5UL
          ; Entries = Log.make 2UL () |> Some
          }
        let! response = receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have log count 1" 1UL Raft.numLogs
        let! entry = getEntryAtM 1UL
        expect "Should have term 2" 2UL (Option.get >> Log.entryTerm) entry
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_reply_false_if_doesnt_have_log_at_prev_log_idx_which_matches_prev_log_term =
    testCase "follower recv appendentries reply false if doesnt have log at prev log idx which matches prev log term" <| fun _ ->
      raft {
        let peer = Node.create (Guid.Create()) ()
        do! addNodeM peer
        do! setTermM 2UL

        let msg =
          { Term = 2UL
          ; PrevLogIdx = 1UL
          ; PrevLogTerm = 1UL
          ; LeaderCommit = 5UL
          ; Entries = Log.make 0UL () |> Some
          }
        let! response = receiveAppendEntries (Some peer.Id) msg
        expect "Should not have succeeded" true AppendRequest.failed response
      }
      |> runWithDefaults
      |> ignore

  let _entries_for_conflict_tests (payload : 'a array) =
    raft {
      for t in payload do
        do! createEntryM t >>= ignoreM
    }

  let follower_recv_appendentries_delete_entries_if_conflict_with_new_entries =
    testCase "follower recv appendentries delete entries if conflict with new entries" <| fun _ ->
      let raft' = defaultServer "string tango"
      let cbs = mkcbs (ref "please") :> IRaftCallbacks<_,_>

      raft {
        let getNth n =
          getEntryAt n >>
          Option.get   >>
          Log.data     >>
          Option.get

        let data = [| "one"; "two"; "three"; |]
        let peer = Node.create (Guid.Create()) "peer"

        do! addNodeM peer
        do! setTermM 1UL

        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term         = 2UL
          ; PrevLogIdx   = 1UL
          ; PrevLogTerm  = 1UL
          ; LeaderCommit = 5UL
          ; Entries      = Log.make 2UL "four" |> Some
          }

        let! response = receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendRequest.succeeded response

        do! expectM "Should have 2 entries" 2UL numLogs

        do! expectM "First should have 'one' value" "one" (getNth 1UL)
        do! expectM "second should have 'four' value" "four" (getNth 2UL)
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_delete_entries_if_current_idx_greater_than_prev_log_idx =
    testCase "follower recv appendentries delete entries if current idx greater than prev log idx" <| fun _ ->
      let getNth n =
        raft {
          let! entry = getEntryAtM n
          return entry |> Option.get |> Log.data
          }

      let data = [| "one"; "two"; "three"; |]
      let peer = Node.create (Guid.Create()) "peer"
      let raft' = defaultServer "string tango"
      let cbs = mkcbs (ref "let go") :> IRaftCallbacks<_,_>

      raft {
        do! addNodeM peer
        do! setTermM 1UL
        do! _entries_for_conflict_tests data // add some log entries

        let newer =
          { Term = 2UL
          ; PrevLogIdx = 1UL
          ; PrevLogTerm = 1UL
          ; LeaderCommit = 5UL
          ; Entries = None
          }

        let! response = receiveAppendEntries (Some peer.Id) newer
        expect "Should have succeeded" true AppendRequest.succeeded response
        do! expectM "Should have 1 log entry" 1UL numLogs
        let! entry = getNth 1UL
        expect "Should not have a value" (Some "one") id entry
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_add_new_entries_not_already_in_log =
    testCase "follower recv appendentries add new entries not already in log" <| fun _ ->
      let peer = Node.create (Guid.Create()) ()

      let log =
        LogEntry((Guid.Create()), 2UL, 1UL,  (),
            Some <| LogEntry((Guid.Create()), 2UL, 1UL, (), None))

      raft {
        do! addNodeM peer
        do! setTermM 1UL

        let newer =
          { Term = 1UL
          ; PrevLogIdx = 0UL
          ; PrevLogTerm = 1UL
          ; LeaderCommit = 5UL
          ; Entries = Some log
          }

        let! response = receiveAppendEntries (Some peer.Id) newer
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have 2 logs" 2UL numLogs
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_does_not_add_dupe_entries_already_in_log =
    testCase "follower recv appendentries does not add dupe entries already in log" <| fun _ ->
      let peer = Node.create (Guid.Create()) "node"

      let entry = LogEntry((Guid.Create()), 2UL, 1UL,  "one", None)
      let log = Log.fromEntries entry

      let next =
        { Term = 1UL
        ; PrevLogIdx = 0UL
        ; PrevLogTerm = 1UL
        ; LeaderCommit = 5UL
        ; Entries = Some entry
        }

      let raft' = defaultServer "server"

      let cbs = mkcbs (ref "fucking hell") :> IRaftCallbacks<_,_>

      raft {
        do! addNodeM peer
        do! setTermM 1UL
        let! response = receiveAppendEntries (Some peer.Id) next
        expect "Should be a success" true AppendRequest.succeeded response

        let! response = receiveAppendEntries (Some peer.Id) next
        expect "Should still be a success" true AppendRequest.succeeded response
        do! expectM "Should have log count 1" 1UL numLogs

        let log'' = Log.append (Log.make 1UL "two") log
        let msg = { next with Entries = log''.Data }

        let! response = receiveAppendEntries (Some peer.Id) msg
        expect "Should be a success" true AppendRequest.succeeded response
        do! expectM "Should have 2 entries now" 2UL numLogs
      }
      |> runWithRaft raft' cbs
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_prevLogIdx =
    testCase "follower recv appendentries set commitidx to prevLogIdx" <| fun _ ->
      let peer = Node.create (Guid.Create()) ()

      let log =
        LogEntry((Guid.Create()), 0UL, 1UL,  (),
            Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (),
                Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (),
                    Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (), None))))

      let msg =
        { Term = 1UL
        ; PrevLogIdx = 0UL
        ; PrevLogTerm = 1UL
        ; LeaderCommit = 5UL
        ; Entries = Some log
        }

      raft {
        do! addNodeM peer
        let! response = receiveAppendEntries (Some peer.Id) msg
        expect "Should have been successful" true AppendRequest.succeeded response
        expect "Should have correct CurrentIndex" 4UL AppendRequest.currentIndex response
        do! expectM "Should have commit index 4" 4UL Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_set_commitidx_to_LeaderCommit =
    testCase "follower recv appendentries set commitidx to LeaderCommit" <| fun _ ->
      let peer = Node.create (Guid.Create()) ()

      let log =
        LogEntry((Guid.Create()), 0UL, 1UL,  (),
          Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (),
              Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (),
                  Some <| LogEntry((Guid.Create()), 0UL, 1UL,  (), None))))

      let msg =
        { Term = 1UL
        ; PrevLogIdx = 0UL
        ; PrevLogTerm = 1UL
        ; LeaderCommit = 0UL
        ; Entries = Some log
        }

      raft {
        do! addNodeM peer
        let! response1 = receiveAppendEntries (Some peer.Id) msg
        let! response2 = receiveAppendEntries (Some peer.Id) { msg with PrevLogIdx = 3UL; LeaderCommit = 3UL; Entries = None }
        expect "Should have been successful" true AppendRequest.succeeded response2
        do! expectM "Should have commit index 3" 3UL Raft.commitIndex
      }
      |> runWithDefaults
      |> ignore


  let follower_recv_appendentries_failure_includes_current_idx =
    testCase "follower recv appendentries failure includes current idx" <| fun _ ->
      let peer = Node.create (Guid.Create()) ()

      let log id = LogEntry(id, 0UL, 1UL,  (), None)

      let msg =
        { Term = 0UL
        ; PrevLogIdx = 0UL
        ; PrevLogTerm = 0UL
        ; LeaderCommit = 0UL
        ; Entries = None
        }

      raft {
        do! addNodeM peer
        do! setTermM 1UL
        do! appendEntryM (log (Guid.Create())) >>= ignoreM
        let! response = receiveAppendEntries (Some peer.Id) msg

        expect "Should not be successful" true AppendRequest.failed response
        expect "Should have current index 1" 1UL AppendRequest.currentIndex response

        do! appendEntryM (log (Guid.Create())) >>= ignoreM
        let! response = receiveAppendEntries (Some peer.Id) msg
        expect "Should not be successful" true AppendRequest.failed response
        expect "Should have current index 2" 2UL AppendRequest.currentIndex response
      }
      |> runWithDefaults
      |> ignore

  let follower_recv_appendentries_resets_election_timeout =
    testCase "follower recv appendentries resets election timeout" <| fun _ ->
      let peer = Node.create (Guid.Create()) ()

      let msg =
        { Term = 1UL
        ; PrevLogIdx = 0UL
        ; PrevLogTerm = 0UL
        ; LeaderCommit = 0UL
        ; Entries = None
        }

      raft {
        do! setElectionTimeoutM 1000UL
        do! addNodeM peer
        do! periodic 900UL
        let! response = receiveAppendEntries (Some peer.Id) msg
        do! expectM "Should have timeout elapsed 0" 0UL timeoutElapsed
      }
      |> runWithDefaults
      |> ignore
