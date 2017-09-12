namespace Iris.Tests.Raft

open Expecto
open Iris.Raft
open Iris.Core

[<AutoOpen>]
module ServerTests =

  ////////////////////////////////////////
  //  ____                              //
  // / ___|  ___ _ ____   _____ _ __    //
  // \___ \ / _ \ '__\ \ / / _ \ '__|   //
  //  ___) |  __/ |   \ V /  __/ |      //
  // |____/ \___|_|    \_/ \___|_|      //
  ////////////////////////////////////////

  let server_voted_for_records_who_we_voted_for =
    testCase "Raft server voted for records who we voted for" <| fun _ ->
      let id1 = IrisId.Create()
      raft {
         do! expectM  "Should one mem" 1 Raft.numMembers
         do! Raft.addMemberM (Member.create id1)
         do! expectM  "Should two mems" 2 Raft.numMembers

         let! mem = Raft.getMemberM id1
         do! Raft.voteFor mem

         do! expectM "Should have voted for last id" id1 (Raft.votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let server_idx_starts_at_one =
    testCase "Raft server index should start at 1" <| fun _ ->
      raft {
         do! expectM "Should have default idx" (index 0) Raft.currentIndex
         do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
         do! expectM "Should have current idx" (index 1) Raft.currentIndex
         do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
         do! expectM "Should have current idx" (index 2) Raft.currentIndex
         do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
         do! expectM "Should have current idx" (index 3) Raft.currentIndex
      }
      |> runWithDefaults
      |> noError

  let server_currentterm_defaults_to_zero =
    testCase "Raft server current Term should default to zero" <| fun _ ->
      raft {
        do! expectM "Should be Zero" (term 0) Raft.currentTerm //
      }
      |> runWithDefaults
      |> noError

  let server_set_currentterm_sets_term =
    testCase "Raft server set term sets term" <| fun _ ->
      raft {
        do! Raft.setTermM (term 5)
        do! expectM "Should be correct term" (term 5) Raft.currentTerm
      }
      |> runWithDefaults
      |> noError

  let server_voting_results_in_voting =
    testCase "Raft server voting should set voted for" <| fun _ ->
      let mem1 = Member.create (IrisId.Create())
      let mem2 = Member.create (IrisId.Create())

      raft {
        // add mem and vote for it
        do! Raft.addMemberM mem1
        do! Raft.voteFor (Some mem1)
        do! expectM "should be correct id" mem1.Id (Raft.votedFor >> Option.get)
        do! Raft.addMemberM mem2
        do! Raft.voteFor (Some mem2)
        do! expectM "should be correct id" mem2.Id (Raft.votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let server_add_mem_makes_non_voting_mem_voting =
    testCase "Raft add mem now makes non-voting mem voting" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      raft {
        do! Raft.addNonVotingMemberM mem
        let! peer = Raft.getMemberM mem.Id
        expect "Non-voting mem should not be voting" false Member.isVoting (Option.get peer)
        do! Raft.addMemberM mem
        let! peer = Raft.getMemberM mem.Id
        expect "Member should be voting" true Member.isVoting (Option.get peer)
        do! expectM "Should have two mems (incl. self)" 2 Raft.numMembers
      }
      |> runWithDefaults
      |> noError

  let server_remove_mem =
    testCase "Raft remove mem should set correct mem count" <| fun _ ->
      let mem1 = Member.create (IrisId.Create())
      let mem2 = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM mem1
        do! expectM "Should have Member count of two" 2 Raft.numMembers
        do! Raft.addMemberM mem2
        do! expectM "Should have Member count of three" 3 Raft.numMembers
        do! Raft.removeMemberM mem1
        do! expectM "Should have Member count of two" 2 Raft.numMembers
        do! Raft.removeMemberM mem2
        do! expectM "Should have Member count of one" 1 Raft.numMembers
      }
      |> runWithDefaults
      |> noError

  let server_election_start_increments_term =
    testCase "Raft election increments current term" <| fun _ ->
      raft {
        do! Raft.setTermM (term 2)
        do! Raft.startElection ()
        do! expectM "Raft should have correct term" (term 3) Raft.currentTerm
      }
      |> runWithDefaults
      |> noError


  let server_set_state =
    testCase "Raft set state should set supplied state" <| fun _ ->
      raft {
        do! Raft.setStateM Leader
        do! expectM "Raft should be leader now" Leader Raft.getState
      }
      |> runWithDefaults
      |> noError

  let server_starts_as_follower =
    testCase "Raft starts as follower" <| fun _ ->
      raft {
        do! expectM "Raft state should be Follower" Follower Raft.getState
      }
      |> runWithDefaults
      |> noError

  let server_append_entry_is_retrievable =
    testCase "Raft should be able to retrieve entry and data by index" <| fun _ ->
      let msg1 = DataSnapshot (State.Empty)
      let msg2 = DataSnapshot (State.Empty)
      let msg3 = DataSnapshot (State.Empty)

      let init = defaultServer()
      let cbs = Callbacks.Create (ref (DataSnapshot (State.Empty))) :> IRaftCallbacks

      raft {
        do! Raft.setStateM Candidate
        do! Raft.setTermM (term 5)

        do! Raft.createEntryM msg2 >>= ignoreM
        let! entry = Raft.getEntryAtM (index 1)
        match Option.get entry with
          | LogEntry(_,_,_,data,_) ->
            Expect.equal data msg2 "Should have correct contents"
          | _ -> failwith "Should be a Log"

        do! Raft.createEntryM msg3 >>= ignoreM
        let! entry = Raft.getEntryAtM (index 2)
        match Option.get entry with
          | LogEntry(_,_,_,data,_) ->
            Expect.equal data msg3 "Should have correct contents"
          | _ -> failwith "Should be a Log"
      }
      |> runWithRaft init cbs
      |> noError

  let server_wont_apply_entry_if_we_dont_have_entry_to_apply =
    testCase "Raft won't apply entry if we don't have entry to apply" <| fun _ ->
      raft {
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.applyEntries ()

        let! lidx = Raft.lastAppliedIdx()
        let! cidx = Raft.commitIndexM()

        expect "Last applied index should be zero" 0<index> id lidx
        expect "Last commit index should be zero"  0<index> id cidx
      }
      |> runWithDefaults
      |> noError

  let server_wont_apply_entry_if_there_isnt_a_majority =
    testCase "Raft won't apply a change if the is not a majority" <| fun _ ->
      let mems = // create 5 mems
        Array.map
          (fun _ ->
            let id = IrisId.Create()
            (id, Member.create id))
          [| 1 .. 5 |]
        |> Map.ofArray

      raft {
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.addMembersM mems
        do! Raft.applyEntries ()

        let! lidx = Raft.lastAppliedIdx()
        let! cidx = Raft.commitIndexM()

        expect "Should not have incremented last applied index" 0<index> id lidx
        expect "Should not have incremented commit index"  0<index> id cidx

        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! Raft.applyEntries () >>= ignoreM

        let! lidx = Raft.lastAppliedIdx()
        let! cidx = Raft.commitIndexM()

        expect "Should not have incremented last applied index" 0<index> id lidx
        expect "Should not have incremented commit index"  0<index> id cidx
      }
      |> runWithDefaults
      |> noError


  let server_increment_lastApplied_when_lastApplied_lt_commitidx =
    testCase "Raft increment lastApplied when lastApplied lt commitidx" <| fun _ ->
      raft {
        do! Raft.setStateM Follower
        do! Raft.setTermM (term 1)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! Raft.setCommitIndexM (index 1)
        do! Raft.periodic 1<ms>
        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 1" 1<index> id lidx
      }
      |> runWithDefaults
      |> noError

  let server_apply_entry_increments_last_applied_idx =
    testCase "Raft applyEntry increments LastAppliedIndex" <| fun _ ->
      raft {
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! Raft.setCommitIndexM (index 1)
        do! Raft.applyEntries ()
        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 1" 1<index> id lidx
      }
      |> runWithDefaults
      |> noError

  let server_periodic_elapses_election_timeout =
    testCase "Raft Periodic elapses election timeout" <| fun _ ->
      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! expectM "Timeout elapsed should be zero" 0<ms> Raft.timeoutElapsed
        do! Raft.periodic 0<ms>
        do! expectM "Timeout elapsed should be zero" 0<ms> Raft.timeoutElapsed
        do! Raft.periodic 100<ms>
        do! expectM "Timeout elapsed should be 100" 100<ms> Raft.timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let server_election_timeout_does_no_promote_us_to_leader_if_there_is_only_1_mem =
    testCase "Election timeout does not promote us to leader if there is only 1 mem" <| fun _ ->
      raft {
        do! Raft.addMemberM (Member.create (IrisId.Create()))
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.periodic 1001<ms>
        do! expectM "Should not be Leader" false Raft.isLeader
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_auto_commits_if_we_are_the_only_mem =
    testCase "Receive entry auto-commits if we are the only mem" <| fun _ ->
      let entry = LogEntry(IrisId.Create(),index 0,term 0,DataSnapshot (State.Empty),None)
      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx 0" (index 0) Raft.commitIndex

        let! result = Raft.receiveEntry entry

        do! expectM "Should have log count 1" 1 Raft.numLogs
        do! expectM "Should have commit idx 1" (index 1) Raft.commitIndex
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_fails_if_there_is_already_a_voting_change =
    testCase "Receive entry fails if there is already a voting change" <| fun _ ->
      let mem = Member.create (IrisId.Create())
      let mklog term =
        JointConsensus(IrisId.Create(), index 1, term, [| ConfigChange.MemberAdded(mem) |] , None)

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" (index 0) Raft.commitIndex

        let! term = Raft.currentTermM ()
        let! result = Raft.receiveEntry (mklog term)

        do! Raft.periodic 1000<ms>             // important, as only now the changes take effect

        do! expectM "Should have log count of one" 1 Raft.numLogs

        let! term = Raft.currentTermM ()
        return! Raft.receiveEntry (mklog term)
      }
      |> runWithDefaults
      |> expectError (IrisError.RaftError ("Raft.receiveEntry","Unexpected Voting Change"))

  let server_recv_entry_adds_missing_mem_on_addmem =
    testCase "recv entry adds missing mem on addmem" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let mklog term =
        JointConsensus(IrisId.Create(), index 1, term, [| ConfigChange.MemberAdded(mem) |] , None)

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" (index 0) Raft.commitIndex
        do! expectM "Should have mem count of one" 1 Raft.numMembers
        let! term = Raft.currentTermM ()
        let! result = Raft.receiveEntry (mklog term)
        do! Raft.periodic 10<ms>
        do! expectM "Should have mem count of two" 2 Raft.numMembers
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_added_mem_should_be_nonvoting =
    testCase "recv entry added mem should be nonvoting" <| fun _ ->
      let nid = IrisId.Create()
      let mem = Member.create nid
      let mklog term =
        JointConsensus(IrisId.Create(), index 1, term, [| ConfigChange.MemberAdded(mem) |] , None)

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" (index 0) Raft.commitIndex
        do! expectM "Should have mem count of one" 1 Raft.numMembers

        let! term = Raft.currentTermM ()
        let! result = Raft.receiveEntry (mklog term)

        do! Raft.periodic 10<ms>

        do! expectM "Should be non-voting mem for start" false (Raft.getMember nid >> Option.get >> Member.isVoting)
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_removes_mem_on_removemem =
    testCase "recv entry removes mem on removemem" <| fun _ ->
      let term = ref (term 0)
      let ci = ref (index 0)
      let mem = Member.create (IrisId.Create())

      let mklog term =
        JointConsensus(IrisId.Create(), index 1, term, [| ConfigChange.MemberRemoved mem |] , None)

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.addMemberM mem
        do! Raft.becomeLeader ()
        do! expectM "Should have mem count of two" 2 Raft.numMembers

        ci := 1<index>

        let! result = Raft.receiveEntry (mklog !term)

        do! Raft.receiveAppendEntriesResponse mem.Id {
              Term = !term
              Success = true
              CurrentIndex = !ci
              FirstIndex = index 1
            }

        ci := 2<index>

        do! Raft.periodic 1000<ms>

        // after entry was applied, we'll see the change
        do! expectM "Should have mem count of one" 1 Raft.numMembers
      }
      |> runWithDefaults
      |> noError


  let server_cfg_sets_num_mems =
    testCase "Configuration sets the number of mems counter" <| fun _ ->
      let count = 12

      let flip f b a = f b a
      let mems =
        List.map (fun n -> Member.create (IrisId.Create())) [1..count]

      raft {
        for mem in mems do
          do! Raft.addMemberM mem
        do! expectM "Should have 13 mems now" 13 Raft.numMembers
      }
      |> runWithDefaults
      |> noError

  let server_votes_are_majority_is_true =
    testCase "Vote are majority is majority" <| fun _ ->
      Raft.majority 3 1
      |> expect "1) Should not be a majority" false id

      Raft.majority 3 2
      |> expect "2) Should be a majority" true id

      Raft.majority 5 2
      |> expect "3) Should not be a majority" false id

      Raft.majority 5 3
      |> expect "4) Should be a majority" true id

      Raft.majority 1 2
      |> expect "5) Should not be a majority" false id

      Raft.majority 4 2
      |> expect "6) Should not be a majority" false id

  let recv_requestvote_response_dont_increase_votes_for_me_when_not_granted =
    testCase "Receive vote response does not increase votes for me when not granted" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 1)
        do! Raft.setStateM Candidate
        do! expectM "Votes for me should be zero" 0 Raft.numVotesForMe

        let! term = Raft.currentTermM ()
        let response = { Term = term; Granted = false; Reason = Some OK }
        let! result = Raft.receiveVoteResponse mem.Id response
        do! expectM "Votes for me should be zero" 0 Raft.numVotesForMe
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_response_dont_increase_votes_for_me_when_term_is_not_equal =
    testCase "Recv requestvote response does not increase votes for me when term is not equal" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 3)
        do! Raft.setStateM Candidate
        do! expectM "Should have zero votes for me" 0 Raft.numVotesForMe

        let response = { Term = term 2; Granted = true; Reason = None }
        return! Raft.receiveVoteResponse mem.Id response
      }
      |> runWithDefaults
      |> expectError (IrisError.RaftError("Raft.receiveVoteResponse", "Vote Term Mismatch"))

  let recv_requestvote_response_increase_votes_for_me =
    testCase "Recv requestvote response increase votes for me" <| fun _ ->
      let mem = Member.create (IrisId.Create())
      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 1)
        do! expectM "Should have zero votes for me" 0 Raft.numVotesForMe
        do! Raft.becomeCandidate ()
        do! Raft.receiveVoteResponse mem.Id { Term = term 2; Granted = true; Reason = None }
        do! expectM "Should have two votes for me" 2 Raft.numVotesForMe
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_response_must_be_candidate_to_receive =
    testCase "recv requestvote response must be candidate to receive" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let err =
        "Not Candidate"
        |> Error.asRaftError "Raft.receiveVoteResponse"

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 1)
        let response = { Term = term 1; Granted = true; Reason = None }
        do! Raft.receiveVoteResponse mem.Id response
      }
      |> runWithDefaults
      |> expectError err

  let recv_requestvote_fails_if_term_less_than_current_term =
    testCase "recv requestvote fails if term less than current term" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let err =
        "Vote Term Mismatch"
        |> Error.asRaftError "Raft.receiveVoteResponse"

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 3)
        do! Raft.becomeCandidate ()
        let! response = Raft.receiveVoteResponse mem.Id {
              Term = term 3
              Granted = true
              Reason = None
            }
        do! expectM "Should have term 4" (term 4) Raft.currentTerm
      }
      |> runWithDefaults
      |> expectError err

  ////////////////////////////////////////////////////////////////////////////////////
  //  ____  _                 _     _  ____                 _ __     __    _        //
  // / ___|| |__   ___  _   _| | __| |/ ___|_ __ __ _ _ __ | |\ \   / /__ | |_ ___  //
  // \___ \| '_ \ / _ \| | | | |/ _` | |  _| '__/ _` | '_ \| __\ \ / / _ \| __/ _ \ //
  //  ___) | | | | (_) | |_| | | (_| | |_| | | | (_| | | | | |_ \ V / (_) | ||  __/ //
  // |____/|_| |_|\___/ \__,_|_|\__,_|\____|_|  \__,_|_| |_|\__| \_/ \___/ \__\___| //
  ////////////////////////////////////////////////////////////////////////////////////

  let shouldgrantvote_vote_term_too_small =
    testCase "grantVote should be false when vote term too small" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let vote =
        { Term = term 1
        ; Candidate = mem
        ; LastLogIndex = index 1
        ; LastLogTerm = term 1
        }

      raft {
        do! Raft.setTermM (term 2)
        let! (res,_) = Raft.shouldGrantVote vote
        expect "Should not grant vote" false id res
      }
      |> runWithDefaults
      |> noError


  let shouldgrantvote_alredy_voted =
    testCase "grantVote should be false when already voted" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let vote =
        { Term = term 2
        ; Candidate = mem
        ; LastLogIndex = index 1
        ; LastLogTerm = term 1
        }

      raft {
        do! Raft.setTermM (term 2)
        do! Raft.voteForMyself ()
        let! (res,_) = Raft.shouldGrantVote vote
        expect "Should not grant vote" false id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_log_empty =
    testCase "grantVote should be true when log is empty" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let vote =
        { Term = term 1
        ; Candidate = mem
        ; LastLogIndex = index 1
        ; LastLogTerm = term 1
        }

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 1)
        do! Raft.voteFor None
        do! expectM "Should have currentIndex zero" (index 0) Raft.currentIndex
        do! expectM "Should have voted for nobody" None Raft.votedFor
        let! (res,_) = Raft.shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_raft_log_term_smaller_vote_logterm =
    testCase "grantVote should be true if last raft log term is smaller than vote last log term " <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let vote =
        { Term = term 2
        ; Candidate = mem
        ; LastLogIndex = index 1
        ; LastLogTerm = term 2
        }

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 1)
        do! Raft.voteFor None
        do! expectM "Should have currentIndex zero" (index 0) Raft.currentIndex
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! expectM "Should have currentIndex one" (index 2) Raft.currentIndex
        let! (res,_) = Raft.shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_raft_last_log_valid =
    testCase "grantVote should be true if last raft log is valid" <| fun _ ->
      let mem = Member.create (IrisId.Create())

      let vote =
        { Term = term 2
        ; Candidate = mem
        ; LastLogIndex = index 3
        ; LastLogTerm = term 2
        }

      raft {
        do! Raft.addMemberM mem
        do! Raft.setTermM (term 2)
        do! Raft.voteFor None
        do! expectM "Should have currentIndex zero" (index 0) Raft.currentIndex
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! Raft.createEntryM (DataSnapshot (State.Empty)) >>= ignoreM
        do! expectM "Should have currentIndex one" (index 2) Raft.currentIndex
        let! (res,_) = Raft.shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let leader_recv_requestvote_does_not_step_down =
    testCase "leader recv requestvote does not step down" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        do! Raft.voteForMyself ()
        do! Raft.becomeLeader ()
        do! expectM "Should be leader" Leader Raft.getState
        let request =
          { Term = term 1
          ; Candidate = peer
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }
        let! resp = Raft.receiveVoteRequest peer.Id request
        do! expectM "Should be leader" Leader Raft.getState
      }
      |> runWithDefaults
      |> noError


  let recv_requestvote_reply_true_if_term_greater_than_or_equal_to_current_term =
    testCase "recv requestvote reply true if term greater than or equal to current term" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        let request =
          { Term = term 2
          ; Candidate = peer
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }
        let! resp = Raft.receiveVoteRequest peer.Id request
        expect "Should be granted" true Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_reset_timeout =
    testCase "recv requestvote reset timeout" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM (term 1)
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.periodic 900<ms>
        let request =
          { Term = term 2
          ; Candidate = peer
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }
        let! resp = Raft.receiveVoteRequest peer.Id request
        expect "Vote should be granted" true Vote.granted resp
        do! expectM "Timeout Elapsed should be reset" 0<ms> Raft.timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_candidate_step_down_if_term_is_higher_than_current_term =
    testCase "recv requestvote candidate step down if term is higher than current term" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM peer
        do! Raft.becomeCandidate ()
        do! Raft.setTermM (term 1)
        do! expectM "Should have voted for myself" true Raft.votedForMyself
        do! expectM "Should have term 1" (term 1) Raft.currentTerm
        let request =
          { Term = term 2
          ; Candidate = peer
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }
        let! resp = Raft.receiveVoteRequest peer.Id request
        do! expectM "Should now be Follower" Follower Raft.getState
        do! expectM "Should have term 2" (term 2) Raft.currentTerm
        do! expectM "Should have voted for peer" peer.Id (Raft.votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_add_unknown_candidate =
    testCase "recv_requestvote_adds_candidate" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let other = Member.create (IrisId.Create())

      raft {
        do! Raft.addMemberM peer
        do! Raft.becomeCandidate ()
        do! Raft.setTermM (term 1)
        do! expectM "Should have voted for myself" true Raft.votedForMyself
        let request =
          { Term = term 2
          ; Candidate = other
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }
        let! resp = Raft.receiveVoteRequest other.Id request
        do! expectM "Should have added mem" None (Raft.getMember other.Id)
        expect "Should not have granted vote" false Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_dont_grant_vote_if_we_didnt_vote_for_this_candidate =
    testCase "recv_requestvote_dont_grant_vote_if_we_didnt_vote_for_this_candidate" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let request =
        { Term = term 1
        ; Candidate = peer1
        ; LastLogIndex = index 1
        ; LastLogTerm = term 1
        }

      raft {
        do! Raft.addMembersM (Map.ofArray [| (peer1.Id, peer1); (peer2.Id, peer2) |])
        do! Raft.setTermM (term 1)
        do! Raft.voteForMyself ()
        do! Raft.setTermM (term 1)
        do! expectM "Should have voted for myself" true Raft.votedForMyself
        do! expectM "Should have 3 mems" 3 Raft.numMembers

        let! raft' = get
        let req1 = { request with Candidate = raft'.Member }

        let! result = Raft.receiveVoteRequest peer2.Id req1
        expect "Should not have granted vote" false Vote.granted result
      }
      |> runWithDefaults
      |> noError

  let follower_becomes_follower_is_follower =
    testCase "follower becomes follower is follower" <| fun _ ->
      raft {
        do! Raft.becomeLeader ()
        do! expectM "Should be leader now" Leader Raft.getState
        do! Raft.becomeFollower ()
        do! expectM "Should be follower now" Follower Raft.getState
      }
      |> runWithDefaults
      |> noError

  let follower_becomes_follower_does_not_clear_voted_for =
    testCase "follower becomes follower does not clear voted for" <| fun _ ->
      raft {
        do! Raft.voteForMyself ()
        do! expectM "Should have voted for myself" true Raft.votedForMyself
        do! Raft.becomeFollower ()
        do! expectM "Should have voted for myself" true Raft.votedForMyself
      }
      |> runWithDefaults
      |> noError

  let candidate_becomes_candidate_is_candidate =
    testCase "candidate becomes candidate is candidate" <| fun _ ->
      raft {
        do! Raft.becomeCandidate ()
        do! expectM "Should be Candidate" true Raft.isCandidate
      }
      |> runWithDefaults
      |> noError

  let candidate_election_timeout_and_no_leader_results_in_new_election =
    testCase "candidate election timeout and no leader results in new election"  <| fun _ ->
      // When the election timeout is reached and we didn't get enougth votes to
      // become leader yet, periodic is expected to re-start the elections (and
      // thereby increasing the term again).
      let peer = Member.create (IrisId.Create())
      raft {
        do! Raft.addMemberM peer
        do! Raft.setElectionTimeoutM 1000<ms>
        do! expectM "Should be at term zero" (term 0) Raft.currentTerm
        do! Raft.becomeCandidate ()
        do! expectM "Should be at term one" (term 1) Raft.currentTerm
        do! Raft.periodic 1001<ms>
        do! expectM "Should be at term two" (term 2) Raft.currentTerm
      }
      |> runWithDefaults
      |> noError


  let follower_becomes_candidate_when_election_timeout_occurs =
    testCase "follower becomes candidate when election timeout occurs" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.addMemberM peer
        do! Raft.periodic 1001<ms>
        do! expectM "Should be candidate now" Candidate Raft.getState
      }
      |> runWithDefaults
      |> noError


  let follower_dont_grant_vote_if_candidate_has_a_less_complete_log =
    testCase "follower dont grant vote if candidate has a less complete log" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log1 = LogEntry(IrisId.Create(), index 0, term 1, (DataSnapshot (State.Empty)), None)
      let log2 = LogEntry(IrisId.Create(), index 0, term 2, (DataSnapshot (State.Empty)), None)

      raft {
        do! Raft.addPeerM peer
        do! Raft.setTermM (term 1)
        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM

        let! state = get
        let vote : VoteRequest =
          { Term = term 1
          ; Candidate = state.Member
          ; LastLogIndex = index 1
          ; LastLogTerm = term 1
          }

        let! resp = Raft.receiveVoteRequest peer.Id vote
        expect "Should have failed" false id resp.Granted

        do! Raft.setTermM (term 2)

        let! resp = Raft.receiveVoteRequest peer.Id { vote with Term = term 2; LastLogTerm = term 3; }
        expect "Should be granted" true Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_increments_current_term =
    testCase "follower becoming candidate increments current term" <| fun _ ->
      raft {
        do! expectM "Should have term 0" (term 0) Raft.currentTerm
        do! Raft.becomeCandidate ()
        do! expectM "Should have term 1" (term 1) Raft.currentTerm
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_votes_for_self =
    testCase "follower becoming candidate votes for self" <| fun _ ->
      raft {
        let peer = Member.create (IrisId.Create())
        let! raft' = get
        do! Raft.addMemberM peer
        do! expectM "Should have no VotedFor" None Raft.votedFor
        do! Raft.becomeCandidate ()
        do! expectM "Should have voted for myself" (Some raft'.Member.Id) Raft.votedFor
        do! expectM "Should have one vote for me" 1 Raft.numVotesForMe
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_resets_election_timeout =
    testCase "follower becoming candidate resets election timeout" <| fun _ ->
      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! expectM "Should have zero elapsed timout" 0<ms> Raft.timeoutElapsed
        do! Raft.periodic 900<ms>
        do! expectM "Should have 900 elapsed timout" 900<ms> Raft.timeoutElapsed
        do! Raft.becomeCandidate ()
        do! expectM "Should have timeout elapsed below 1000" true (Raft.timeoutElapsed >> ((>) 1000<ms>))
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_requests_votes_from_other_servers =
    testCase "follower becoming candidate requests votes from other servers" <| fun _ ->
      let peer0 = Member.create (IrisId.Create())
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())

      let state = Raft.create peer0
      let lokk = new System.Object()
      let i = ref 0
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty))) with
            SendRequestVote = fun _ _ -> lock lokk <| fun _ -> i := !i + 1 }
        :> IRaftCallbacks

      raft {
        do! Raft.addMemberM peer1
        do! Raft.addMemberM peer2
        do! Raft.setTermM (term 2)
        do! Raft.becomeCandidate ()
        expect "Should have two vote requests" 2 id !i
      }
      |> runWithRaft state cbs
      |> noError

  let candidate_receives_majority_of_votes_becomes_leader =
    testCase "candidate receives majority of votes becomes leader" <| fun _ ->
      let peers =
        [| for n in 0 .. 3 do
             let peer = Member.create (IrisId.Create())
             yield (peer.Id, peer) |]
        |> Map.ofArray

      raft {
        do! Raft.addPeersM peers
        do! expectM "Should have 5 mems" 5 Raft.numMembers
        do! Raft.becomeCandidate ()

        let! term = Raft.currentTermM ()

        for KeyValue(id,_) in peers do
          do! Raft.receiveVoteResponse id { Term = term; Granted = true; Reason = None }

        do! expectM "Should be leader" true Raft.isLeader
      }
      |> runWithDefaults
      |> noError

  let candidate_will_not_respond_to_voterequest_if_it_has_already_voted =
    testCase "candidate will not respond to voterequest if it has already voted" <| fun _ ->
      raft {
        let! raft' = get
        let peer = Member.create (IrisId.Create())
        let vote : VoteRequest =
          { Term = term 0                // term must be equal or lower that raft's
            Candidate = raft'.Member    // term for this to work
            LastLogIndex = index 0
            LastLogTerm = term 0 }
        do! Raft.addPeerM peer
        do! Raft.voteFor (Some raft'.Member)
        let! resp = Raft.receiveVoteRequest peer.Id vote
        expect "Should have failed" true Vote.declined resp
      }
      |> runWithDefaults
      |> noError

  let candidate_requestvote_includes_logidx =
    testCase "candidate requestvote includes logidx" <| fun _ ->
      let self = Member.create (IrisId.Create())
      let raft' = Raft.create self
      let sender = Sender.create
      let response = { Term = term 5; Granted = true; Reason = None }
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendRequestVote = senderRequestVote sender (Some response) }
        :> IRaftCallbacks

      raft {
        let peer1 = Member.create (IrisId.Create())
        let peer2 = Member.create (IrisId.Create())

        let peers =
          [| peer1; peer2 |]
          |> Array.map (fun p -> (p.Id,p))
          |> Map.ofArray

        let log =
          LogEntry(IrisId.Create(),index 0, term 3, DataSnapshot (State.Empty),
            Some <| LogEntry(IrisId.Create(),index 0, term 1, DataSnapshot (State.Empty),
              Some <| LogEntry(IrisId.Create(),index 0, term 1, DataSnapshot (State.Empty), None)))

        do! Raft.addPeersM peers
        do! Raft.setStateM Candidate
        do! Raft.setTermM (term 5)
        do! Raft.appendEntryM log >>= ignoreM

        let! request = Raft.sendVoteRequest peer1

        do! Raft.receiveVoteResponse peer1.Id response

        let vote = List.head (!sender.Outbox) |> getVote

        expect "should have last log index be 3" (index 3) Vote.lastLogIndex vote
        expect "should have last term be 5" (term 5) Vote.term vote
        expect "should have last log term be 3" (term 3) Vote.lastLogTerm vote
        expect "should have candidate id be me" self Vote.candidate vote
      }
      |> runWithRaft raft' cbs
      |> noError

  let candidate_recv_requestvote_response_becomes_follower_if_current_term_is_less_than_term =
    testCase "candidate recv requestvote response becomes follower if current term is less than term" <| fun _ ->
      raft {
        let peer = Member.create (IrisId.Create())
        let response = { Term = term 2 ; Granted = false; Reason = None }
        do! Raft.addPeerM peer
        do! Raft.setTermM (term 1)
        do! Raft.setStateM Candidate
        do! Raft.voteFor None
        do! expectM "Should not be follower" false Raft.isFollower
        do! expectM "Should not *have* a leader" None Raft.currentLeader
        do! expectM "Should have term 1" (term 1) Raft.currentTerm
        do! Raft.receiveVoteResponse peer.Id response
        do! expectM "Should be Follower" Follower Raft.getState
        do! expectM "Should have term 2" (term 2) Raft.currentTerm
        do! expectM "Should have voted for nobody" None Raft.votedFor
      }
      |> runWithDefaults
      |> noError


  let candidate_recv_appendentries_frm_leader_results_in_follower =
    testCase "candidate recv appendentries frm leader results in follower" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let ae : AppendEntries =
        { Term = term 1
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 0
        ; LeaderCommit = index 0
        ; Entries = None
        }

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Candidate
        do! Raft.voteFor None
        do! expectM "Should not be follower" false Raft.isFollower
        do! expectM "Should have no leader" None Raft.currentLeader
        do! expectM "Should have term 0" (term 0) Raft.currentTerm
        let! resp = Raft.receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should be follower" Follower Raft.getState
        do! expectM "Should have peer as leader" (Some peer.Id) Raft.currentLeader
        do! expectM "Should have term 1" (term 1) Raft.currentTerm
        do! expectM "Should have voted for noone" None Raft.votedFor
      }
      |> runWithDefaults
      |> noError

  let candidate_recv_appendentries_from_same_term_results_in_step_down =
    testCase "candidate recv appendentries from same term results in step down" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let ae : AppendEntries =
        { Term = term 2
        ; PrevLogIdx = index 1
        ; PrevLogTerm = term 1
        ; LeaderCommit = index 0
        ; Entries = None
        }

      raft {
        do! Raft.addPeerM peer
        do! Raft.setTermM (term 2)
        do! Raft.setStateM Candidate
        do! expectM "Should not be follower" false Raft.isFollower
        let! resp = Raft.receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should not be candidate anymore" false Raft.isCandidate
      }
      |> runWithDefaults
      |> noError

  let leader_becomes_leader_is_leader =
    testCase "leader becomes leader is leader" <| fun _ ->
      raft {
        do! Raft.becomeLeader ()
        do! expectM "Should be leader" Leader Raft.getState
      }
      |> runWithDefaults
      |> noError

  let leader_becomes_leader_does_not_clear_voted_for =
    testCase "leader becomes leader does not clear voted for" <| fun _ ->
      raft {
        let! raft' = get
        do! Raft.voteForMyself ()
        do! expectM "Should have voted for myself" (Some raft'.Member.Id) Raft.votedFor
        do! Raft.becomeLeader ()
        do! expectM "Should still have votedFor" (Some raft'.Member.Id) Raft.votedFor
      }
      |> runWithDefaults
      |> noError

  let leader_when_becomes_leader_all_mems_have_nextidx_equal_to_lastlog_idx_plus_1 =
    testCase "leader when becomes leader all mems have nextidx equal to lastlog idx plus 1" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())

      raft {
        do! Raft.addPeerM peer1
        do! Raft.addPeerM peer2
        do! Raft.setStateM Candidate
        do! Raft.becomeLeader ()
        let! raft' = get
        let cidx = Raft.currentIndex raft' + index 1

        for peer in raft'.Peers do
          if peer.Value.Id <> raft'.Member.Id then
            expect "Should have correct nextIndex" cidx id peer.Value.NextIndex
      }
      |> runWithDefaults
      |> noError

  let leader_when_it_becomes_a_leader_sends_empty_appendentries =
    testCase "leader when it becomes a leader sends empty appendentries" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())

      let lokk = new System.Object()
      let count = ref 0
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = fun _ _ -> lock lokk <| fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer1
        do! Raft.addPeerM peer2
        do! Raft.setStateM Candidate
        do! Raft.becomeLeader ()
        expect "Should have two messages" 2 id !count
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_responds_to_entry_msg_when_entry_is_committed =
    testCase "leader responds to entry msg when entry is committed" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log = LogEntry(IrisId.Create(),index 0,term 0,DataSnapshot (State.Empty),None)

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! expectM "Should have log count 0" 0 Raft.numLogs
        let! resp = Raft.receiveEntry log
        do! expectM "Should have log count 1" 1 Raft.numLogs
        do! Raft.applyEntries ()
        let response = { Term = term 0; Success = true; CurrentIndex = index 1; FirstIndex = index 1 }
        do! Raft.receiveAppendEntriesResponse peer.Id response
        let! committed = Raft.responseCommitted resp
        expect "Should be committed" true id committed
      }
      |> runWithDefaults
      |> noError


  let non_leader_recv_entry_msg_fails =
    testCase "non leader recv entry msg fails" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log = LogEntry(IrisId.Create(),index 0,term 0,DataSnapshot (State.Empty),None)

      let err =
        "Not Leader"
        |> Error.asRaftError "Raft.receiveEntry"

      raft {
        do! Raft.addMemberM peer
        do! Raft.setStateM Follower
        let! resp = Raft.receiveEntry log
        return "never reached"
      }
      |> runWithDefaults
      |> expectError err

  let leader_sends_appendentries_with_NextIdx_when_PrevIdx_gt_NextIdx =
    testCase "leader sends appendentries with NextIdx when PrevIdx gt NextIdx" <| fun _ ->
      let peer = { Member.create (IrisId.Create()) with NextIndex = index 4 }
      let raft' : RaftValue = defaultServer ()
      let sender = Sender.create
      let log = LogEntry(IrisId.Create(),index 0, term 1, DataSnapshot (State.Empty), None)
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! Raft.sendAllAppendEntriesM ()
        expect "Should have one message in cue" 1 List.length (!sender.Outbox)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_sends_appendentries_with_leader_commit =
    testCase "leader sends appendentries with leader commit" <| fun _ ->
      let peer = { Member.create (IrisId.Create()) with NextIndex = index 4 }
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader

        for n in 0 .. 9 do
          let l = LogEntry(IrisId.Create(), index 0, term 1, DataSnapshot (State.Empty), None)
          do! Raft.appendEntryM l >>= ignoreM

        do! Raft.setCommitIndexM (index 10)
        do! Raft.sendAllAppendEntriesM ()

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have leader commit 10" (index 10) (fun ae -> ae.LeaderCommit)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_sends_appendentries_with_prevLogIdx =
    testCase "leader sends appendentries with prevLogIdx" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader

        let! request = Raft.sendAppendEntry peer

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIndex 0" (index 0) (fun ae -> ae.PrevLogIdx)

        let log = LogEntry(IrisId.Create(),index 0,term 2,DataSnapshot (State.Empty),None)

        do! Raft.appendEntryM log >>= ignoreM
        do! Raft.setNextIndexM peer.Id (index 1)

        let! peer = Raft.getMemberM peer.Id >>= (Option.get >> returnM)

        let! request = Raft.sendAppendEntry peer

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> assume "Should have PrevLogIdx 0" (index 0) (fun ae -> ae.PrevLogIdx)
        |> assume "Should have one entry" 1 (fun ae -> ae.Entries |> Option.get |> LogEntry.depth)
        |> assume "Should have entry with correct id" (LogEntry.getId log) (fun ae -> ae.Entries |> Option.get |> LogEntry.getId)
        |> expect "Should have entry with term" (term 2) (fun ae -> ae.Entries |> Option.get |> LogEntry.getTerm)

        sender.Outbox := List.empty // reset outbox

        do! Raft.setNextIndexM peer.Id (index 2)
        let! peer = Raft.getMemberM peer.Id >>= (Option.get >> returnM)
        let! request = Raft.sendAppendEntry peer

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 1" (index 1) (fun ae -> ae.PrevLogIdx)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_sends_appendentries_when_mem_has_next_idx_of_0 =
    testCase "leader sends appendentries when mem has next idx of 0" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        let! request = Raft.sendAppendEntry peer

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 0" (index 0) (fun ae -> ae.PrevLogIdx)

        sender.Outbox := List.empty // reset outbox

        let log = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty), None)

        do! Raft.setNextIndexM peer.Id (index 1)
        do! Raft.appendEntryM log >>= ignoreM
        let! request = Raft.sendAppendEntry peer

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 0" (index 0) (fun ae -> ae.PrevLogIdx)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_retries_appendentries_with_decremented_NextIdx_log_inconsistency =
    testCase "leader retries appendentries with decremented NextIdx log inconsistency" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        let! request = Raft.sendAppendEntry peer
        (!sender.Outbox)
        |> expect "Should have a message" 1 List.length
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_append_entry_to_log_increases_idxno =
    testCase "leader append entry to log increases idxno" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs = Callbacks.Create (ref (DataSnapshot (State.Empty))) :> IRaftCallbacks

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! expectM "Should have zero logs" 0 Raft.numLogs
        let! resp = Raft.receiveEntry log
        do! expectM "Should have on log" 1 Raft.numLogs
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_increase_commit_idx_when_majority_have_entry_and_atleast_one_newer_entry =
    testCase "leader recv appendentries response increase commit idx when majority have entry and atleast one newer entry" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let peer3 = Member.create (IrisId.Create())
      let peer4 = Member.create (IrisId.Create())

      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log1 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log2 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log3 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)

      let response =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 3
        ; FirstIndex = index 1
        }

      let peers =
        [| peer1; peer2; peer3; peer4; |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 1)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM
        do! Raft.appendEntryM log3 >>= ignoreM

        // peer 1
        let! request = Raft.sendAppendEntry peer1

        // peer 2
        let! request = Raft.sendAppendEntry peer2

        do! Raft.receiveAppendEntriesResponse peer1.Id response
        // first response, no majority yet, will not set commit idx
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.receiveAppendEntriesResponse peer2.Id response
        //  leader will now have majority followers who have appended this log
        do! expectM "Should have commit index 3" (index 3) Raft.commitIndex

        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 0" 0<index> id lidx

        do! Raft.periodic 1<ms>

        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 3" 3<index> id lidx
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_duplicate_does_not_decrement_match_idx =
    testCase "leader recv appendentries response duplicate does not decrement match idx" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())

      let response =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 1
        ; FirstIndex = index 1
        }

      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs = Callbacks.Create (ref (DataSnapshot (State.Empty))) :> IRaftCallbacks

      let log1 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log2 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log3 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)

      let peers =
        [| peer1; peer2; |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 1)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM
        do! Raft.appendEntryM log3 >>= ignoreM
        do! Raft.sendAllAppendEntriesM ()
        do! Raft.receiveAppendEntriesResponse peer1.Id response
        do! Raft.receiveAppendEntriesResponse peer2.Id response
        do! expectM "Should have matchIdx 1" (index 1) (Raft.getMember peer1.Id >> Option.get >> Member.getMatchIndex)
        do! Raft.receiveAppendEntriesResponse peer1.Id response
        do! expectM "Should still have matchIdx 1" (index 1) (Raft.getMember peer1.Id >> Option.get >> Member.getMatchIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_do_not_increase_commit_idx_because_of_old_terms_with_majority =
    testCase "leader recv appendentries response do not increase commit idx because of old terms with majority" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let peer3 = Member.create (IrisId.Create())
      let peer4 = Member.create (IrisId.Create())

      let response =
        { Term         = term 1
        ; Success      = true
        ; CurrentIndex = index 1
        ; FirstIndex   = index 1 }

      let cbs = Callbacks.Create (ref (DataSnapshot (State.Empty))) :> IRaftCallbacks

      let log1 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log2 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log3 = LogEntry(IrisId.Create(),index 0,term 2,DataSnapshot (State.Empty),None)

      let peers =
        [| peer1; peer2; peer3; peer4 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 2)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM
        do! Raft.appendEntryM log3 >>= ignoreM

        let! request = Raft.sendAppendEntry peer1

        let! request = Raft.sendAppendEntry peer2

        do! Raft.receiveAppendEntriesResponse peer1.Id response
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.receiveAppendEntriesResponse peer2.Id response
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.periodic 1<ms>

        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 0" 0<index> id lidx

        let! request = Raft.sendAppendEntry peer1

        let! request = Raft.sendAppendEntry peer2

        do! Raft.receiveAppendEntriesResponse peer1.Id { response with CurrentIndex = index 2; FirstIndex = index 2 }
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.receiveAppendEntriesResponse peer2.Id { response with CurrentIndex = index 2; FirstIndex = index 2 }
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.periodic 1<ms>

        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 0" 0<index> id lidx

        let! request = Raft.sendAppendEntry peer1

        let! request = Raft.sendAppendEntry peer2

        do! Raft.receiveAppendEntriesResponse peer1.Id { response with Term = term 2; CurrentIndex = index 3; FirstIndex = index 3 }
        do! expectM "Should have commit index 0" (index 0) Raft.commitIndex

        do! Raft.receiveAppendEntriesResponse peer2.Id { response with Term = term 2; CurrentIndex = index 3; FirstIndex = index 3 }
        do! expectM "Should have commit index 3" (index 3) Raft.commitIndex

        do! Raft.periodic 1<ms>

        let! lidx = Raft.lastAppliedIdx()
        expect "Should have last applied index 3" 3<index> id lidx
      }
      |> runWithCBS cbs
      |> noError

  let leader_recv_appendentries_response_jumps_to_lower_next_idx =
    testCase "leader recv appendentries response jumps to lower next idx" <| fun _ ->
      let peer = Member.create (IrisId.Create())

      let lokk = new System.Object()
      let count = ref 0
      let appendReq = ref None

      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty))) with
            SendAppendEntries = fun n ae ->
              lock lokk <| fun _ -> count := !count + 1
              appendReq := Some ae }
        :> IRaftCallbacks

      let log1 = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)
      let log2 = LogEntry(IrisId.Create(),index 0,term 2,DataSnapshot (State.Empty),None)
      let log3 = LogEntry(IrisId.Create(),index 0,term 3,DataSnapshot (State.Empty),None)
      let log4 = LogEntry(IrisId.Create(),index 0,term 4,DataSnapshot (State.Empty),None)

      let response =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 1
        ; FirstIndex = index 1 }

      raft {
        do! Raft.addMemberM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 2)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setLastAppliedIdxM (index 0)
        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM
        do! Raft.appendEntryM log3 >>= ignoreM
        do! Raft.appendEntryM log4 >>= ignoreM
        do! Raft.becomeLeader ()

        do! expectM "Should have nextIdx 5" (index 5) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! expectM "Should have a msg 1" 1 (konst !count)

        // need to get an up-to-date version of the peer, because its nextIdx
        // will have been bumped when becoming leader!
        let! peer = Raft.getMemberM peer.Id >>= (Option.get >> returnM)

        do! Raft.sendAllAppendEntriesM ()

        expect "Should have prevLogIdx 4" (index 4) AppendRequest.prevLogIndex (!appendReq |> Option.get)
        expect "Should have prevLogTerm 4" (term 4) AppendRequest.prevLogTerm (!appendReq |> Option.get)

        let! trm = Raft.currentTermM ()
        do! Raft.receiveAppendEntriesResponse peer.Id { response with Term = trm; Success = false; CurrentIndex = index 1 }

        do! expectM "Should have NextIdx 2" (index 2) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! expectM "Should have MatchIdx 2" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getMatchIndex)
        do! expectM "Should have 2 msgs"    2   (konst !count)

        do! Raft.sendAllAppendEntriesM ()

        expect "Should have prevLogIdx 1" (index 1) AppendRequest.prevLogIndex (!appendReq |> Option.get)
        expect "Should have prevLogTerm 1" (term 1) AppendRequest.prevLogTerm  (!appendReq |> Option.get)
      }
      |> runWithCBS cbs
      |> noError


  let leader_recv_appendentries_response_decrements_to_lower_next_idx =
    testCase "leader recv appendentries response decrements to lower next idx" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let lokk = new System.Object()

      let ci = ref (index 0)
      let trm = ref (term 2)
      let result = ref false
      let count = ref 0

      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty))) with
            SendAppendEntries = fun n ae -> lock lokk <| fun _ -> count := !count + 1
          } :> IRaftCallbacks

      let makeResponse () =
        { Term         = !trm
          Success      = !result
          CurrentIndex = !ci
          FirstIndex   = index 0 }

      raft {
        do! Raft.addMemberM peer
        do! Raft.setTermM !trm
        do! Raft.setCommitIndexM (index 0)

        for n in 1 .. 4 do
          do! LogEntry(IrisId.Create(),0<index>,term n,DataSnapshot(State.Empty),None)
              |> Raft.appendEntryM
              >>= ignoreM

        ci := 0<index>

        do! Raft.becomeLeader()

        do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should have correct NextIndex"  (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! expectM "Should have correct MatchIndex" (index 0) (Raft.getMember peer.Id >> Option.get >> Member.getMatchIndex)
        do! expectM "Should have been called once" 1  (konst !count)

        // need to get updated peer, because nextIdx will be bumped when
        // becoming leader!
        let! peer = Raft.getMemberM peer.Id >>= (Option.get >> returnM)

        // we pretend that the follower `peer` has now successfully appended those logs
        let! t = Raft.currentTermM ()
        trm := t
        ci := (index 4)
        result := true

        // send again and process responses
        do! Raft.sendAllAppendEntriesM ()
        do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should finally have NextIndex 5"  (index 5) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! expectM "Should finally have MatchIndex 4" (index 4) (Raft.getMember peer.Id >> Option.get >> Member.getMatchIndex)
        do! expectM "Should have been called twice" 2 (konst !count)
      }
      |> runWithCBS cbs
      |> noError

  let leader_recv_appendentries_response_retry_only_if_leader =
    testCase "leader recv appendentries response retry only if leader" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())

      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref (DataSnapshot (State.Empty)))
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = LogEntry(IrisId.Create(),index 0,term 1,DataSnapshot (State.Empty),None)

      let response =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 1
        ; FirstIndex = index 1
        }

      let err =
        "Not Leader"
        |> Error.asRaftError "Raft.receiveAppendEntriesResponse"

      let peers =
        [| peer1; peer2 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setTermM (term 1)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setStateM Leader
        do! Raft.setLastAppliedIdxM (index 0)

        do! Raft.appendEntryM log >>= ignoreM

        let! request = Raft.sendAppendEntry peer1

        let! request = Raft.sendAppendEntry peer2

        do! expectM "Should have 2 msgs" 2 (fun _ -> List.length !sender.Outbox)
        do! Raft.becomeFollower ()
        do! Raft.receiveAppendEntriesResponse peer1.Id response
      }
      |> runWithRaft raft' cbs
      |> expectError err

  let leader_recv_entry_resets_election_timeout =
    testCase "leader recv entry resets election timeout" <| fun _ ->
      let log = LogEntry(IrisId.Create(), index 0, term 1, DataSnapshot (State.Empty), None)
      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.setStateM Leader
        do! Raft.periodic 1000<ms>
        let! response = Raft.receiveEntry log
        do! expectM "Should have reset timeout elapsed" 0<ms> Raft.timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let leader_recv_entry_is_committed_returns_0_if_not_committed =
    testCase "leader recv entry is committed returns 0 if not committed" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log = LogEntry(IrisId.Create(), index 0, term 1, DataSnapshot (State.Empty), None)

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader

        do! Raft.setCommitIndexM (index 0)
        let! response = Raft.receiveEntry log
        let! committed = Raft.responseCommitted response
        expect "Should not have committed" false id committed

        do! Raft.setCommitIndexM (index 1)
        let! response = Raft.receiveEntry log
        let! committed = Raft.responseCommitted response
        expect "Should have committed" true id committed
      }
      |> runWithDefaults
      |> noError

  let leader_recv_entry_is_committed_returns_neg_1_if_invalidated =
    testCase "leader recv entry is committed returns neg 1 if invalidated" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let log = Log.make (term 1) (DataSnapshot (State.Empty))

      let ae =
        { LeaderCommit = index 1
        ; Term = term 2
        ; PrevLogIdx = index 0
        ; PrevLogTerm = term 0
        ; Entries = Log.make (term 2) defSM |> Some
        }

      let err =
        "Entry Invalidated"
        |> Error.asRaftError "Raft.responseCommitted"

      raft {
        do! Raft.addMemberM peer
        do! Raft.setStateM Leader
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setTermM (term 1)

        do! expectM "Should have current idx 0" (index 0) Raft.currentIndex

        let! response = Raft.receiveEntry log
        let! committed = Raft.responseCommitted response

        expect "Should not have committed entry" false id committed
        expect "Should have term 1" (term 1) Entry.term response
        expect "Should have index 1" (index 1) Entry.index response

        do! expectM "(1) Should have current idx 1" (index 1) Raft.currentIndex
        do! expectM "Should have commit idx 0" (index 0) Raft.commitIndex

        let! resp = Raft.receiveAppendEntries (Some peer.Id) ae

        expect "Should have succeeded" true AppendRequest.succeeded resp

        do! expectM "(2) Should have current idx 1" (index 1) Raft.currentIndex
        do! expectM "Should have commit idx 1" (index 1) Raft.commitIndex

        return! Raft.responseCommitted response
      }
      |> runWithDefaults
      |> expectError err


  let leader_recv_entry_does_not_send_new_appendentries_to_slow_mems =
    testCase "leader recv entry does not send new appendentries to slow mems" <| fun _ ->
      skiptest "NO CONGESTION CONTROL CURRENTLY IMPLEMENTED"

      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref defSM) with
            SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = Log.make (term 1) defSM

      raft {
        do! Raft.addMemberM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 1)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.setNextIndexM peer.Id (index 1)
        do! Raft.appendEntryM log >>= ignoreM
        let! response = Raft.receiveEntry log

        !sender.Outbox
        |> expect "Should have no msg" 0 List.length
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_failure_does_not_set_mem_nextid_to_0 =
    testCase "leader recv appendentries response failure does not set mem nextid to 0" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref defSM)
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = Log.make (term 1) defSM
      let resp =
        { Term = term 1
        ; Success = false
        ; CurrentIndex = index 0
        ; FirstIndex = index 0
        }

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 1)
        do! Raft.setCommitIndexM (index 0)
        do! Raft.appendEntryM log >>= ignoreM

        let! request = Raft.sendAppendEntry peer

        do! Raft.receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx Works 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! Raft.receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx Dont work 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_increment_idx_of_mem =
    testCase "leader recv appendentries response increment idx of mem" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref defSM)
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let resp =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 0
        ; FirstIndex = index 0
        }

      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 1)
        do! expectM "Should have nextIdx 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! Raft.receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_drop_message_if_term_is_old =
    testCase "leader recv appendentries response drop message if term is old" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let raft' = defaultServer ()
      let sender = Sender.create
      let cbs =
        { Callbacks.Create (ref defSM)
            with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let resp =
        { Term = term 1
        ; Success = true
        ; CurrentIndex = index 1
        ; FirstIndex = index 1
        }
      raft {
        do! Raft.addPeerM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 2)
        do! expectM "Should have nextIdx 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
        do! Raft.receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx 1" (index 1) (Raft.getMember peer.Id >> Option.get >> Member.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_steps_down_if_newer =
    testCase "leader recv appendentries steps down if newer" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let ae =
        { Term = term 6
          PrevLogIdx = index 6
          PrevLogTerm = term 5
          LeaderCommit = index 0
          Entries = None }

      raft {
        let! raft' = get
        let nid = Some raft'.Member.Id
        let pid = Some peer.Id
        do! Raft.addMemberM peer
        do! Raft.setStateM Leader
        do! Raft.setLeaderM (Some raft'.Member.Id)
        do! Raft.setTermM (term 5)
        do! expectM "Should be leader" true Raft.isLeader
        do! expectM "Should be leader" true (Raft.currentLeader >> ((=) nid))
        let! response = Raft.receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should be follower" true Raft.isFollower
        do! expectM "Should follow peer" true (Raft.currentLeader >> ((=) pid))
      }
      |> runWithDefaults
      |> noError

  let leader_recv_appendentries_steps_down_if_newer_term =
    testCase "leader recv appendentries steps down if newer term" <| fun _ ->
      let peer = Member.create (IrisId.Create())
      let resp =
        { Term = term 6
        ; PrevLogIdx = index 5
        ; PrevLogTerm = term 5
        ; LeaderCommit = index 0
        ; Entries = None
        }
      raft {
        do! Raft.addMemberM peer
        do! Raft.setStateM Leader
        do! Raft.setTermM (term 5)
        let! response = Raft.receiveAppendEntries (Some peer.Id) resp
        do! expectM "Should be follower" true Raft.isFollower
      }
      |> runWithDefaults
      |> noError

  let leader_sends_empty_appendentries_every_request_timeout =
    testCase "leader sends empty appendentries every request timeout" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let raft' = defaultServer ()

      let lokk = new System.Object()

      let count = ref 0

      let response =
        ref { Term = term 0
              Success = true
              CurrentIndex = index 1
              FirstIndex = index 1 }

      let cbs =
        { Callbacks.Create (ref defSM)
            with SendAppendEntries = fun _ _ -> lock lokk <| fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      let peers =
        [| peer1; peer2 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.setRequestTimeoutM 500<ms>
        do! expectM "Should have timout elapsed 0" 0<ms> Raft.timeoutElapsed

        do! Raft.setStateM Candidate
        do! Raft.becomeLeader ()

        do! expectM "Should have 2 messages " 2 (konst !count)

        // update CurrentIndex to latest memIdx to prevent StaleResponse error
        let! mem1 = Raft.getMemberM peer1.Id

        response := { !response with
                        CurrentIndex = Option.get mem1 |> Member.getNextIndex |> ((+) (index 1)) }

        do! Raft.periodic 501<ms>

        do! expectM "Should have 4 messages" 4 (konst !count) // because 2 peers
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_requestvote_responds_without_granting =
    testCase "leader recv requestvote responds without granting" <| fun _ ->
      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let sender = Sender.create
      let resp = { Term = term 1; Granted = true; Reason = None }

      let vote =
        { Term = term 1
        ; Candidate = peer2
        ; LastLogIndex = index 0
        ; LastLogTerm = term 0 }

      let peers =
        [| peer1; peer2 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.setRequestTimeoutM 500<ms>
        do! expectM "Should have timout elapsed 0" 0<ms> Raft.timeoutElapsed
        do! Raft.startElection ()
        do! Raft.receiveVoteResponse peer1.Id resp
        do! expectM "Should be leader" Leader Raft.getState
        let! resp = Raft.receiveVoteRequest peer2.Id vote
        expect "Should have declined vote" true Vote.declined resp
      }
      |> runWithDefaults
      |> noError


  let leader_recv_requestvote_responds_with_granting_if_term_is_higher =
    testCase "leader recv requestvote responds with granting if term is higher" <| fun _ ->

      let peer1 = Member.create (IrisId.Create())
      let peer2 = Member.create (IrisId.Create())
      let sender = Sender.create
      let resp = { Term = term 1; Granted = true; Reason = None }

      let vote =
        { Term = term 2
        ; Candidate = peer2
        ; LastLogIndex = index 0
        ; LastLogTerm = term 0 }

      let peers =
        [| peer1; peer2 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addMembersM peers
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.setRequestTimeoutM 500<ms>
        do! expectM "Should have timout elapsed 0" 0<ms> Raft.timeoutElapsed

        do! Raft.startElection ()
        do! Raft.receiveVoteResponse peer1.Id resp
        do! expectM "Should be Leader" true Raft.isLeader
        let! resp = Raft.receiveVoteRequest peer2.Id vote
        do! expectM "Should be Follower" true Raft.isFollower
      }
      |> runWithDefaults
      |> noError


  let server_should_also_request_vote_from_failed_mems =
    testCase "should also request vote from failed mems" <| fun _ ->
      let mem1 =   Member.create (IrisId.Create())
      let mem2 =   Member.create (IrisId.Create())
      let mem3 =   Member.create (IrisId.Create())
      let mem4 = { Member.create (IrisId.Create())  with State = RaftMemberState.Failed }

      let mutable i = 0

      let raft' = Raft.create mem1
      let cbs =
        { Callbacks.Create (ref defSM) with SendRequestVote = fun _ _ -> i <- i + 1 }
        :> IRaftCallbacks

      let peers =
        [| mem2; mem3; mem4 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addPeersM peers
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.periodic 1001<ms>
        expect "Should have sent 3 requests" 3 id i
      }
      |> runWithRaft raft' cbs
      |> noError


  let server_should_not_consider_failed_mems_when_deciding_vote_outcome =
    testCase "should not consider failed mems when deciding vote outcome" <| fun _ ->
      let mem1 =   Member.create (IrisId.Create())
      let mem2 =   Member.create (IrisId.Create())
      let mem3 = { Member.create (IrisId.Create())  with State = RaftMemberState.Failed }
      let mem4 = { Member.create (IrisId.Create())  with State = RaftMemberState.Failed }

      let resp = { Term = term 1; Granted = true; Reason = None }

      let peers =
        [| mem1; mem2; mem3; mem4 |]
        |> Array.map toPair
        |> Map.ofArray

      raft {
        do! Raft.addPeersM peers
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.periodic 1001<ms>
        do! Raft.receiveVoteResponse mem1.Id resp
        do! expectM "Should be leader now" Leader Raft.getState
      }
      |> runWithDefaults
      |> noError


  let server_periodic_should_trigger_snapshotting =
    testCase "periodic should trigger snapshotting when MaxLogDepth is reached" <| fun _ ->
      raft {
        let trm = term 1
        let depth = 40
        let! me = Raft.selfM ()

        do! Raft.setMaxLogDepthM depth
        do! Raft.setTermM trm

        for n in 0 .. depth do
          do! Raft.appendEntryM (Log.make trm defSM) >>= ignoreM

        do! Raft.setLeaderM (Some me.Id)
        do! expectM "Should have correct number of entries" (depth + 1) Raft.numLogs
        do! Raft.periodic 10<ms>
        do! expectM "Should have correct number of entries" 1 Raft.numLogs
      }
      |> runWithDefaults
      |> noError

  let server_should_apply_each_log_when_receiving_a_snapshot =
    testCase "should apply each log when receiving a snapshot" <| fun _ ->
      let idx = index 9
      let trm = term 1
      let count = ref 0

      let init = defaultServer ()
      let cbs =
        { Callbacks.Create (ref defSM) with ApplyLog = fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      let mems =
        [| "one"; "two"; "three" |]
        |> Array.mapi (fun i _ -> Member.create (IrisId.Create()))

      let is: InstallSnapshot =
        { Term = trm
        ; LeaderId = IrisId.Create()
        ; LastTerm = trm
        ; LastIndex = idx
        ; Data = Snapshot(IrisId.Create(), idx, trm, idx, trm, mems, defSM) }

      raft {
        do! Raft.setTermM trm
        let! response = Raft.receiveInstallSnapshot is
        do! expectM "Should have correct number of mems" 4 Raft.numMembers // including our own mem
        do! expectM "Should have correct number of log entries" 1 Raft.numLogs
        expect "Should have called ApplyLog once" 1 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_merge_snaphot_and_existing_log_when_receiving_a_snapshot =
    testCase "should merge snaphot and existing log when receiving a snapshot" <| fun _ ->
      let idx = index 9
      let num = 5
      let trm = term 1
      let count = ref 0

      let init = defaultServer ()
      let cbs =
        { Callbacks.Create (ref defSM) with ApplyLog = fun l -> count := !count + 1 }
        :> IRaftCallbacks

      let mems =
        [| "one"; "two"; "three" |]
        |> Array.mapi (fun i _ -> Member.create (IrisId.Create()))

      let is: InstallSnapshot =
        { Term = trm
        ; LeaderId = IrisId.Create()
        ; LastTerm = trm
        ; LastIndex = idx
        ; Data = Snapshot(IrisId.Create(), idx, trm, idx, trm, mems, defSM)
        }

      raft {
        do! Raft.setTermM trm
        for n in 0 .. (int idx + num) do
          do! Raft.appendEntryM (Log.make trm (DataSnapshot (State.Empty))) >>= ignoreM

        do! Raft.applyEntries ()

        let! response = Raft.receiveInstallSnapshot is

        do! expectM "Should have correct number of mems" 4 Raft.numMembers // including our own mem
        do! expectM "Should have correct number of log entries" 7 Raft.numLogs
        expect "Should have called ApplyLog once" 7 id !count
      }
      |> runWithRaft init cbs
      |> noError


  let server_should_fire_mem_callbacks_on_config_change =
    testCase "should fire mem callbacks on config change" <| fun _ ->
      let count = ref 0

      let init = defaultServer ()

      let cb _ l =
        count := !count + 1

      let cbs =
        { Callbacks.Create (ref defSM) with
            MemberAdded   = cb "added"
            MemberRemoved = cb "removed"
        } :> IRaftCallbacks

      raft {
        let mem = Member.create (IrisId.Create())

        do! Raft.setStateM Leader

        do! Raft.appendEntryM (JointConsensus(IrisId.Create(), index 0, term 0, [| ConfigChange.MemberAdded(mem)|] ,None)) >>= ignoreM
        do! Raft.setCommitIndexM (index 1)
        do! Raft.applyEntries ()

        expect "Should have count 1" 1 id !count

        do! Raft.appendEntryM (JointConsensus(IrisId.Create(), index 0, term 0, [| ConfigChange.MemberRemoved mem |] ,None)) >>= ignoreM
        do! Raft.setCommitIndexM (index 3)
        do! Raft.applyEntries ()

        expect "Should have count 2" 2 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_call_persist_callback_for_each_appended_log =
    testCase "should call persist callback for each appended log" <| fun _ ->
      let count = ref List.empty

      let init = defaultServer ()

      let cb l = count := LogEntry.getId l :: !count

      let cbs =
        { Callbacks.Create (ref defSM) with
            PersistLog = cb
        } :> IRaftCallbacks

      raft {
        let log1 = Log.make (term 0) defSM
        let log2 = Log.make (term 0) defSM
        let log3 = Log.make (term 0) defSM

        let ids =
          [ log3; log2; log1; ]
          |> List.map LogEntry.getId

        do! Raft.setStateM Leader

        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2  >>= ignoreM
        do! Raft.appendEntryM log3  >>= ignoreM

        expect "should have correct ids" ids id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_call_delete_callback_for_each_deleted_log =
    testCase "should call delete callback for each deleted log" <| fun _ ->
      let log1 = Log.make (term 0) defSM
      let log2 = Log.make (term 0) defSM
      let log3 = Log.make (term 0) defSM

      let count = ref [ log3; log2; log1; ]

      let init = defaultServer ()

      let cb l =
        let fltr l r = LogEntry.getId l <> LogEntry.getId r
        in count := List.filter (fltr l) !count

      let cbs =
        { Callbacks.Create (ref defSM) with
            DeleteLog = cb
        } :> IRaftCallbacks

      raft {
        do! Raft.setStateM Leader

        do! Raft.appendEntryM log1 >>= ignoreM
        do! Raft.appendEntryM log2 >>= ignoreM
        do! Raft.appendEntryM log3 >>= ignoreM

        do! Raft.removeEntryM (index 3)
        do! expectM "Should have only 2 entries" 2 Raft.numLogs

        do! Raft.removeEntryM (index 2)
        do! expectM "Should have only 1 entry" 1 Raft.numLogs

        do! Raft.removeEntryM (index 1)
        do! expectM "Should have zero entries" 0 Raft.numLogs

        expect "should have deleted all logs" List.empty id !count
      }
      |> runWithRaft init cbs
      |> noError


  let should_call_mem_updated_callback_on_mem_udpated =
    testCase "call mem updated callback on mem udpated" <| fun _ ->
      let count = ref 0
      let init = defaultServer()
      let cbs = { Callbacks.Create (ref defSM)
                    with MemberUpdated = fun _ -> count := 1 + !count }
                :> IRaftCallbacks

      raft {
        let mem = Member.create (IrisId.Create())
        do! Raft.addMemberM mem
        do! Raft.updateMemberM { mem with State = RaftMemberState.Joining }
        do! Raft.updateMemberM { mem with State = RaftMemberState.Running }
        do! Raft.updateMemberM { mem with State = RaftMemberState.Failed }

        expect "Should have called once" 3 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let should_call_state_changed_callback_on_state_change =
    testCase "call state changed callback on state change" <| fun _ ->
      let count = ref 0
      let init = defaultServer()
      let cbs = { Callbacks.Create (ref defSM) with
                    StateChanged = fun _ _ -> count := 1 + !count }
                :> IRaftCallbacks

      raft {
        do! Raft.becomeCandidate ()
        do! Raft.becomeLeader ()
        do! Raft.becomeFollower ()
        expect "Should have called once" 3 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let should_respond_to_appendentries_with_correct_next_idx =
    testCase "respond to appendentries with correct next idx" <| fun _ ->
      let trm = term 1

      raft {
        do! Raft.setTermM trm
        do! Raft.becomeLeader ()

        let! response = Log.make trm defSM |> Raft.receiveEntry
        let! committed = Raft.responseCommitted response

        do! expectM "Should be committed" true (konst committed)

        let! response = Log.make trm defSM |> Raft.receiveEntry
        let! committed = Raft.responseCommitted response

        do! expectM "Should be committed" true (konst committed)

        let peer = Member.create (IrisId.Create())
        do! Raft.becomeFollower ()
        do! Raft.addMemberM peer

        let! trm = Raft.currentTermM ()
        let! ci = Raft.currentIndexM ()
        let! fi = Raft.firstIndexM trm

        let ping : AppendEntries =
          { Term         = trm
          ; PrevLogIdx   = ci
          ; PrevLogTerm  = trm
          ; LeaderCommit = ci
          ; Entries      = None }

        let! response = Raft.receiveAppendEntries (Some peer.Id) ping

        do! expectM "Should have correct CurrentIndex" ci (konst response.CurrentIndex)
        do! expectM "Should have correct FirstIndex" fi (konst response.FirstIndex >> Some)
        do! expectM "Should have correct Term" trm (konst response.Term)
        do! expectM "Should be success" true (konst response.Success)
      }
      |> runWithDefaults
      |> noError

  let should_call_apply_entries_callback =
    testCase "call apply entries callback" <| fun _ ->
      let count = ref 0

      let cbs =
        { Callbacks.Create (ref defSM) with ApplyLog = fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      raft {
        do! Raft.setTermM (term 1)
        do! Raft.becomeLeader ()

        let! term = Raft.currentTermM ()

        let log = Log.make term defSM
        let! result = Raft.receiveEntry log

        do! Raft.periodic 10<ms>

        let! committed = Raft.responseCommitted result

        do! expectM "Should have committed entry" true (konst committed)
        do! expectM "Should have called callback" 1 (konst !count)
      }
      |> runWithCBS cbs
      |> noError
