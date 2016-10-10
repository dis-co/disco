namespace Iris.Tests.Raft

open System.Net
open Fuchu
open Fuchu.Test
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
      let id1 = Id.Create()
      raft {
         do! expectM  "Should one node" 1u numNodes
         do! addNodeM (Node.create id1)
         do! expectM  "Should two nodes" 2u numNodes

         let! node = getNodeM id1
         do! voteFor node

         do! expectM "Should have voted for last id" id1 (votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let server_idx_starts_at_one =
    testCase "Raft server index should start at 1" <| fun _ ->
      raft {
         do! expectM "Should have default idx" 0u currentIndex
         do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
         do! expectM "Should have current idx" 1u currentIndex
         do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
         do! expectM "Should have current idx" 2u currentIndex
         do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
         do! expectM "Should have current idx" 3u currentIndex
      }
      |> runWithDefaults
      |> noError

  let server_currentterm_defaults_to_zero =
    testCase "Raft server current Term should default to zero" <| fun _ ->
      raft {
        do! expectM "Should be Zero" 0u currentTerm
      }
      |> runWithDefaults
      |> noError

  let server_set_currentterm_sets_term =
    testCase "Raft server set term sets term" <| fun _ ->
      raft {
        do! setTermM 5u
        do! expectM "Should be correct term" 5u currentTerm
      }
      |> runWithDefaults
      |> noError

  let server_voting_results_in_voting =
    testCase "Raft server voting should set voted for" <| fun _ ->
      let node1 = Node.create (Id.Create())
      let node2 = Node.create (Id.Create())

      raft {
        // add node and vote for it
        do! addNodeM node1
        do! voteFor (Some node1)
        do! expectM "should be correct id" node1.Id (votedFor >> Option.get)
        do! addNodeM node2
        do! voteFor (Some node2)
        do! expectM "should be correct id" node2.Id (votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let server_add_node_makes_non_voting_node_voting =
    testCase "Raft add node now makes non-voting node voting" <| fun _ ->
      let node = Node.create (Id.Create())

      raft {
        do! addNonVotingNodeM node
        let! peer = getNodeM node.Id
        expect "Non-voting node should not be voting" false Node.isVoting (Option.get peer)
        do! addNodeM node
        let! peer = getNodeM node.Id
        expect "Node should be voting" true Node.isVoting (Option.get peer)
        do! expectM "Should have two nodes (incl. self)" 2u numNodes
      }
      |> runWithDefaults
      |> noError

  let server_remove_node =
    testCase "Raft remove node should set correct node count" <| fun _ ->
      let node1 = Node.create (Id.Create())
      let node2 = Node.create (Id.Create())

      raft {
        do! addNodeM node1
        do! expectM "Should have Node count of two" 2u numNodes
        do! addNodeM node2
        do! expectM "Should have Node count of three" 3u numNodes
        do! removeNodeM node1
        do! expectM "Should have Node count of two" 2u numNodes
        do! removeNodeM node2
        do! expectM "Should have Node count of one" 1u numNodes
      }
      |> runWithDefaults
      |> noError

  let server_election_start_increments_term =
    testCase "Raft election increments current term" <| fun _ ->
      raft {
        do! setTermM 2u
        do! startElection ()
        do! expectM "Raft should have correct term" 3u currentTerm
      }
      |> runWithDefaults
      |> noError


  let server_set_state =
    testCase "Raft set state should set supplied state" <| fun _ ->
      raft {
        do! setStateM Leader
        do! expectM "Raft should be leader now" Leader getState
      }
      |> runWithDefaults
      |> noError

  let server_starts_as_follower =
    testCase "Raft starts as follower" <| fun _ ->
      raft {
        do! expectM "Raft state should be Follower" Follower getState
      }
      |> runWithDefaults
      |> noError

  let server_append_entry_is_retrievable =
    testCase "Raft should be able to retrieve entry and data by index" <| fun _ ->
      let msg1 = DataSnapshot State.Empty
      let msg2 = DataSnapshot State.Empty
      let msg3 = DataSnapshot State.Empty

      let init = mkRaft (Node.create (Id.Create()))
      let cbs = mkcbs (ref (DataSnapshot State.Empty)) :> IRaftCallbacks

      raft {
        do! setStateM Candidate
        do! setTermM 5u

        do! createEntryM msg2 >>= ignoreM
        let! entry = getEntryAtM 1u
        match Option.get entry with
          | LogEntry(_,_,_,data,_) ->
            Assert.Equal("Should have correct contents", msg2, data)
          | _ -> failwith "Should be a Log"

        do! createEntryM msg3 >>= ignoreM
        let! entry = getEntryAtM 2u
        match Option.get entry with
          | LogEntry(_,_,_,data,_) ->
            Assert.Equal("Should have correct contents", msg3, data)
          | _ -> failwith "Should be a Log"
      }
      |> runWithRaft init cbs
      |> noError

  let server_wont_apply_entry_if_we_dont_have_entry_to_apply =
    testCase "Raft won't apply entry if we don't have entry to apply" <| fun _ ->
      raft {
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! applyEntries ()
        do! expectM "Last applied index should be zero" 0u lastAppliedIdx
        do! expectM "Last commit index should be zero"  0u commitIndex
      }
      |> runWithDefaults
      |> noError

  let server_wont_apply_entry_if_there_isnt_a_majority =
    testCase "Raft won't apply a change if the is not a majority" <| fun _ ->
      let nodes = // create 5 nodes
        Array.map (fun n -> Node.create (Id.Create())) [|1u..5u|]

      raft {
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! addNodesM nodes
        do! applyEntries ()
        do! expectM "Should not have incremented last applied index" 0u lastAppliedIdx
        do! expectM "Should not have incremented commit index" 0u commitIndex
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! applyEntries () >>= ignoreM
        do! expectM "fhould not have incremented last applied index" 0u lastAppliedIdx
        do! expectM "Should not have incremented commit index" 0u commitIndex
      }
      |> runWithDefaults
      |> noError


  let server_increment_lastApplied_when_lastApplied_lt_commitidx =
    testCase "Raft increment lastApplied when lastApplied lt commitidx" <| fun _ ->
      raft {
        do! setStateM Follower
        do! setTermM 1u
        do! setLastAppliedIdxM 0u
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! setCommitIndexM 1u
        do! periodic 1u
        do! expectM "1) Last applied index should be one" 1u lastAppliedIdx
      }
      |> runWithDefaults
      |> noError

  let server_apply_entry_increments_last_applied_idx =
    testCase "Raft applyEntry increments LastAppliedIndex" <| fun _ ->
      raft {
        do! setLastAppliedIdxM 0u
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! setCommitIndexM 1u
        do! applyEntries ()
        do! expectM "2) Last applied index should be one" 1u lastAppliedIdx
      }
      |> runWithDefaults
      |> noError

  let server_periodic_elapses_election_timeout =
    testCase "Raft Periodic elapses election timeout" <| fun _ ->
      raft {
        do! setElectionTimeoutM 1000u
        do! expectM "Timeout elapsed should be zero" 0u timeoutElapsed
        do! periodic 0u
        do! expectM "Timeout elapsed should be zero" 0u timeoutElapsed
        do! periodic 100u
        do! expectM "Timeout elapsed should be 100" 100u timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let server_election_timeout_does_no_promote_us_to_leader_if_there_is_only_1_node =
    testCase "Election timeout does not promote us to leader if there is only 1 node" <| fun _ ->
      raft {
        do! addNodeM (Node.create (Id.Create()))
        do! setElectionTimeoutM 1000u
        do! periodic 1001u
        do! expectM "Should not be Leader" false isLeader
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_auto_commits_if_we_are_the_only_node =
    testCase "Receive entry auto-commits if we are the only node" <| fun _ ->
      let entry = LogEntry(Id.Create(),0u,0u,DataSnapshot State.Empty,None)
      raft {
        do! setElectionTimeoutM 1000u
        do! becomeLeader ()
        do! expectM "Should have commit idx 0u" 0u commitIndex

        let! result = receiveEntry entry

        do! expectM "Should have log count 1u" 1u numLogs
        do! expectM "Should have commit idx 1u" 1u commitIndex
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_fails_if_there_is_already_a_voting_change =
    testCase "Receive entry fails if there is already a voting change" <| fun _ ->
      let node = Node.create (Id.Create())
      let mklog term =
        JointConsensus(Id.Create(), 1u, term, [| NodeAdded(node) |] , None)

      raft {
        do! setElectionTimeoutM 1000u
        do! becomeLeader ()
        do! expectM "Should have commit idx of zero" 0u commitIndex

        let! term = currentTermM ()
        let! result = receiveEntry (mklog term)

        do! periodic 1000u             // important, as only now the changes take effect

        do! expectM "Should have log count of one" 1u numLogs

        let! term = currentTermM ()
        return! receiveEntry (mklog term)
      }
      |> runWithDefaults
      |> expectError UnexpectedVotingChange

  let server_recv_entry_adds_missing_node_on_addnode =
    testCase "recv entry adds missing node on addnode" <| fun _ ->
      let node = Node.create (Id.Create())

      let mklog term =
        JointConsensus(Id.Create(), 1u, term, [| NodeAdded(node) |] , None)

      raft {
        do! setElectionTimeoutM 1000u
        do! becomeLeader ()
        do! expectM "Should have commit idx of zero" 0u commitIndex
        do! expectM "Should have node count of one" 1u numNodes
        let! term = currentTermM ()
        let! result = receiveEntry (mklog term)
        do! periodic 10u
        do! expectM "Should have node count of two" 2u numNodes
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_added_node_should_be_nonvoting =
    testCase "recv entry added node should be nonvoting" <| fun _ ->
      let nid = Id.Create()
      let node = Node.create nid
      let mklog term =
        JointConsensus(Id.Create(), 1u, term, [| NodeAdded(node) |] , None)

      raft {
        do! setElectionTimeoutM 1000u
        do! becomeLeader ()
        do! expectM "Should have commit idx of zero" 0u commitIndex
        do! expectM "Should have node count of one" 1u numNodes

        let! term = currentTermM ()
        let! result = receiveEntry (mklog term)

        do! periodic 10u

        do! expectM "Should be non-voting node for start" false (getNode nid >> Option.get >> Node.isVoting)
      }
      |> runWithDefaults
      |> noError

  let server_recv_entry_removes_node_on_removenode =
    testCase "recv entry removes node on removenode" <| fun _ ->
      let term = ref 0u
      let ci = ref 0u

      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendAppendEntries = fun _ _ ->
              Some  { Term = !term; Success = true; CurrentIndex = !ci; FirstIndex = 1u } }
        :> IRaftCallbacks

      let node = Node.create (Id.Create())

      let mklog term =
        JointConsensus(Id.Create(), 1u, term, [| NodeRemoved node |] , None)

      raft {
        do! setElectionTimeoutM 1000u
        do! addNodeM node
        do! becomeLeader ()
        do! expectM "Should have node count of two" 2u numNodes

        ci := 1u

        let! result = receiveEntry (mklog !term)
        let! committed = responseCommitted result

        ci := 2u

        do! periodic 1000u

        // after entry was applied, we'll see the change
        do! expectM "Should have node count of one" 1u numNodes
      }
      |> runWithCBS cbs
      |> noError


  let server_cfg_sets_num_nodes =
    testCase "Configuration sets the number of nodes counter" <| fun _ ->
      let count = 12u

      let flip f b a = f b a
      let nodes =
        List.map (fun n -> Node.create (Id.Create())) [1u..count]

      raft {
        for node in nodes do
          do! addNodeM node
        do! expectM "Should have 13 nodes now" 13u numNodes
      }
      |> runWithDefaults
      |> noError

  let server_votes_are_majority_is_true =
    testCase "Vote are majority is majority" <| fun _ ->
      majority 3u 1u
      |> expect "1) Should not be a majority" false id

      majority 3u 2u
      |> expect "2) Should be a majority" true id

      majority 5u 2u
      |> expect "3) Should not be a majority" false id

      majority 5u 3u
      |> expect "4) Should be a majority" true id

      majority 1u 2u
      |> expect "5) Should not be a majority" false id

      majority 4u 2u
      |> expect "6) Should not be a majority" false id

  let recv_requestvote_response_dont_increase_votes_for_me_when_not_granted =
    testCase "Receive vote response does not increase votes for me when not granted" <| fun _ ->
      let node = Node.create (Id.Create())

      raft {
        do! addNodeM node
        do! setTermM 1u
        do! setStateM Candidate
        do! expectM "Votes for me should be zero" 0u numVotesForMe

        let! term = currentTermM ()
        let response = { Term = term; Granted = false; Reason = Some NoError }
        let! result = receiveVoteResponse node.Id response
        do! expectM "Votes for me should be zero" 0u numVotesForMe
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_response_dont_increase_votes_for_me_when_term_is_not_equal =
    testCase "Recv requestvote response does not increase votes for me when term is not equal" <| fun _ ->
      let node = Node.create (Id.Create())

      raft {
        do! addNodeM node
        do! setTermM 3u
        do! setStateM Candidate
        do! expectM "Should have zero votes for me" 0u numVotesForMe

        let response = { Term = 2u; Granted = true; Reason = None }
        return! receiveVoteResponse node.Id response
      }
      |> runWithDefaults
      |> expectError VoteTermMismatch

  let recv_requestvote_response_increase_votes_for_me =
    testCase "Recv requestvote response increase votes for me" <| fun _ ->
      let node = Node.create (Id.Create())
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendRequestVote = fun _ _ -> Some { Term = 2u; Granted = true; Reason = None } }
        :> IRaftCallbacks

      raft {
        do! addNodeM node
        do! setTermM 1u
        do! expectM "Should have zero votes for me" 0u numVotesForMe
        do! becomeCandidate ()
        do! expectM "Should have two votes for me" 2u numVotesForMe
      }
      |> runWithCBS cbs
      |> noError

  let recv_requestvote_response_must_be_candidate_to_receive =
    testCase "recv requestvote response must be candidate to receive" <| fun _ ->
      let node = Node.create (Id.Create())

      raft {
        do! addNodeM node
        do! setTermM 1u
        let response = { Term = 1u; Granted = true; Reason = None }
        do! receiveVoteResponse node.Id response
      }
      |> runWithDefaults
      |> expectError NotCandidate

  let recv_requestvote_fails_if_term_less_than_current_term =
    testCase "recv requestvote fails if term less than current term" <| fun _ ->
      let node = Node.create (Id.Create())

      raft {
        do! addNodeM node
        do! setTermM 3u
        do! becomeCandidate ()
        let! response = receiveVoteResponse node.Id { Term = 3u; Granted = true; Reason = None }
        do! expectM "Should have term 4" 4u currentTerm
      }
      |> runWithDefaults
      |> expectError VoteTermMismatch

  ////////////////////////////////////////////////////////////////////////////////////
  //  ____  _                 _     _  ____                 _ __     __    _        //
  // / ___|| |__   ___  _   _| | __| |/ ___|_ __ __ _ _ __ | |\ \   / /__ | |_ ___  //
  // \___ \| '_ \ / _ \| | | | |/ _` | |  _| '__/ _` | '_ \| __\ \ / / _ \| __/ _ \ //
  //  ___) | | | | (_) | |_| | | (_| | |_| | | | (_| | | | | |_ \ V / (_) | ||  __/ //
  // |____/|_| |_|\___/ \__,_|_|\__,_|\____|_|  \__,_|_| |_|\__| \_/ \___/ \__\___| //
  ////////////////////////////////////////////////////////////////////////////////////

  let shouldgrantvote_vote_term_too_small =
    testCase "grantVote should be false when vote term too small" <| fun _ ->
      let node = Node.create (Id.Create())

      let vote =
        { Term = 1u
        ; Candidate = node
        ; LastLogIndex = 1u
        ; LastLogTerm = 1u
        }

      raft {
        do! setTermM 2u
        let! (res,_) = shouldGrantVote vote
        expect "Should not grant vote" false id res
      }
      |> runWithDefaults
      |> noError


  let shouldgrantvote_alredy_voted =
    testCase "grantVote should be false when already voted" <| fun _ ->
      let node = Node.create (Id.Create())

      let vote =
        { Term = 2u
        ; Candidate = node
        ; LastLogIndex = 1u
        ; LastLogTerm = 1u
        }

      raft {
        do! setTermM 2u
        do! voteForMyself ()
        let! (res,_) = shouldGrantVote vote
        expect "Should not grant vote" false id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_log_empty =
    testCase "grantVote should be true when log is empty" <| fun _ ->
      let node = Node.create (Id.Create())

      let vote =
        { Term = 1u
        ; Candidate = node
        ; LastLogIndex = 1u
        ; LastLogTerm = 1u
        }

      raft {
        do! addNodeM node
        do! setTermM 1u
        do! voteFor None
        do! expectM "Should have currentIndex zero" 0u currentIndex
        do! expectM "Should have voted for nobody" None votedFor
        let! (res,_) = shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_raft_log_term_smaller_vote_logterm =
    testCase "grantVote should be true if last raft log term is smaller than vote last log term " <| fun _ ->
      let node = Node.create (Id.Create())

      let vote =
        { Term = 2u
        ; Candidate = node
        ; LastLogIndex = 1u
        ; LastLogTerm = 2u
        }

      raft {
        do! addNodeM node
        do! setTermM 1u
        do! voteFor None
        do! expectM "Should have currentIndex zero" 0u currentIndex
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! expectM "Should have currentIndex one" 2u currentIndex
        let! (res,_) = shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let shouldgrantvote_raft_last_log_valid =
    testCase "grantVote should be true if last raft log is valid" <| fun _ ->
      let node = Node.create (Id.Create())

      let vote =
        { Term = 2u
        ; Candidate = node
        ; LastLogIndex = 3u
        ; LastLogTerm = 2u
        }

      raft {
        do! addNodeM node
        do! setTermM 2u
        do! voteFor None
        do! expectM "Should have currentIndex zero" 0u currentIndex
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! createEntryM (DataSnapshot State.Empty) >>= ignoreM
        do! expectM "Should have currentIndex one" 2u currentIndex
        let! (res,_) = shouldGrantVote vote
        expect "Should grant vote" true id res
      }
      |> runWithDefaults
      |> noError

  let leader_recv_requestvote_does_not_step_down =
    testCase "leader recv requestvote does not step down" <| fun _ ->
      let peer = Node.create (Id.Create())

      raft {
        do! addNodeM peer
        do! setTermM 1u
        do! voteForMyself ()
        do! becomeLeader ()
        do! expectM "Should be leader" Leader getState
        let request =
          { Term = 1u
          ; Candidate = peer
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }
        let! resp = receiveVoteRequest peer.Id request
        do! expectM "Should be leader" Leader getState
      }
      |> runWithDefaults
      |> noError


  let recv_requestvote_reply_true_if_term_greater_than_or_equal_to_current_term =
    testCase "recv requestvote reply true if term greater than or equal to current term" <| fun _ ->
      let peer = Node.create (Id.Create())

      raft {
        do! addNodeM peer
        do! setTermM 1u
        let request =
          { Term = 2u
          ; Candidate = peer
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }
        let! resp = receiveVoteRequest peer.Id request
        expect "Should be granted" true Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_reset_timeout =
    testCase "recv requestvote reset timeout" <| fun _ ->
      let peer = Node.create (Id.Create())

      raft {
        do! addNodeM peer
        do! setTermM 1u
        do! setElectionTimeoutM 1000u
        do! periodic 900u
        let request =
          { Term = 2u
          ; Candidate = peer
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }
        let! resp = receiveVoteRequest peer.Id request
        expect "Vote should be granted" true Vote.granted resp
        do! expectM "Timeout Elapsed should be reset" 0u timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_candidate_step_down_if_term_is_higher_than_current_term =
    testCase "recv requestvote candidate step down if term is higher than current term" <| fun _ ->
      let peer = Node.create (Id.Create())

      raft {
        do! addNodeM peer
        do! becomeCandidate ()
        do! setTermM 1u
        do! expectM "Should have voted for myself" true votedForMyself
        do! expectM "Should have term 1" 1u currentTerm
        let request =
          { Term = 2u
          ; Candidate = peer
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }
        let! resp = receiveVoteRequest peer.Id request
        do! expectM "Should now be Follower" Follower getState
        do! expectM "Should have term 2" 2u currentTerm
        do! expectM "Should have voted for peer" peer.Id (votedFor >> Option.get)
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_add_unknown_candidate =
    testCase "recv_requestvote_adds_candidate" <| fun _ ->
      let peer = Node.create (Id.Create())
      let other = Node.create (Id.Create())

      raft {
        do! addNodeM peer
        do! becomeCandidate ()
        do! setTermM 1u
        do! expectM "Should have voted for myself" true votedForMyself
        let request =
          { Term = 2u
          ; Candidate = other
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }
        let! resp = receiveVoteRequest other.Id request
        do! expectM "Should have added node" None (getNode other.Id)
        expect "Should not have granted vote" false Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let recv_requestvote_dont_grant_vote_if_we_didnt_vote_for_this_candidate =
    testCase "recv_requestvote_dont_grant_vote_if_we_didnt_vote_for_this_candidate" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let request =
        { Term = 1u
        ; Candidate = peer1
        ; LastLogIndex = 1u
        ; LastLogTerm = 1u
        }

      raft {
        do! addNodesM [| peer1; peer2 |]
        do! setTermM 1u
        do! voteForMyself ()
        do! setTermM 1u
        do! expectM "Should have voted for myself" true votedForMyself
        do! expectM "Should have 3 nodes" 3u numNodes

        let! raft' = get
        let req1 = { request with Candidate = raft'.Node }

        let! result = receiveVoteRequest peer2.Id req1
        expect "Should not have granted vote" false Vote.granted result
      }
      |> runWithDefaults
      |> noError

  let follower_becomes_follower_is_follower =
    testCase "follower becomes follower is follower" <| fun _ ->
      raft {
        do! becomeLeader ()
        do! expectM "Should be leader now" Leader getState
        do! becomeFollower ()
        do! expectM "Should be follower now" Follower getState
      }
      |> runWithDefaults
      |> noError

  let follower_becomes_follower_does_not_clear_voted_for =
    testCase "follower becomes follower does not clear voted for" <| fun _ ->
      raft {
        do! voteForMyself ()
        do! expectM "Should have voted for myself" true votedForMyself
        do! becomeFollower ()
        do! expectM "Should have voted for myself" true votedForMyself
      }
      |> runWithDefaults
      |> noError

  let candidate_becomes_candidate_is_candidate =
    testCase "candidate becomes candidate is candidate" <| fun _ ->
      raft {
        do! becomeCandidate ()
        do! expectM "Should be Candidate" true isCandidate
      }
      |> runWithDefaults
      |> noError

  let candidate_election_timeout_and_no_leader_results_in_new_election =
    testCase "candidate election timeout and no leader results in new election"  <| fun _ ->
      // When the election timeout is reached and we didn't get enougth votes to
      // become leader yet, periodic is expected to re-start the elections (and
      // thereby increasing the term again).
      let peer = Node.create (Id.Create())
      raft {
        do! addNodeM peer
        do! setElectionTimeoutM 1000u
        do! expectM "Should be at term zero" 0u currentTerm
        do! becomeCandidate ()
        do! expectM "Should be at term one" 1u currentTerm
        do! periodic 1001u
        do! expectM "Should be at term two" 2u currentTerm
      }
      |> runWithDefaults
      |> noError


  let follower_becomes_candidate_when_election_timeout_occurs =
    testCase "follower becomes candidate when election timeout occurs" <| fun _ ->
      let peer = Node.create (Id.Create())

      raft {
        do! setElectionTimeoutM 1000u
        do! addNodeM peer
        do! periodic 1001u
        do! expectM "Should be candidate now" Candidate getState
      }
      |> runWithDefaults
      |> noError


  let follower_dont_grant_vote_if_candidate_has_a_less_complete_log =
    testCase "follower dont grant vote if candidate has a less complete log" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log1 = LogEntry(Id.Create(), 0u, 1u, (DataSnapshot State.Empty), None)
      let log2 = LogEntry(Id.Create(), 0u, 2u, (DataSnapshot State.Empty), None)

      raft {
        do! addPeerM peer
        do! setTermM 1u
        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM

        let! state = get
        let vote : VoteRequest =
          { Term = 1u
          ; Candidate = state.Node
          ; LastLogIndex = 1u
          ; LastLogTerm = 1u
          }

        let! resp = receiveVoteRequest peer.Id vote
        expect "Should have failed" false id resp.Granted

        do! setTermM 2u

        let! resp = receiveVoteRequest peer.Id { vote with Term = 2u; LastLogTerm = 3u; }
        expect "Should be granted" true Vote.granted resp
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_increments_current_term =
    testCase "follower becoming candidate increments current term" <| fun _ ->
      raft {
        do! expectM "Should have term 0" 0u currentTerm
        do! becomeCandidate ()
        do! expectM "Should have term 1" 1u currentTerm
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_votes_for_self =
    testCase "follower becoming candidate votes for self" <| fun _ ->
      raft {
        let peer = Node.create (Id.Create())
        let! raft' = get
        do! addNodeM peer
        do! expectM "Should have no VotedFor" None votedFor
        do! becomeCandidate ()
        do! expectM "Should have voted for myself" (Some raft'.Node.Id) votedFor
        do! expectM "Should have one vote for me" 1u numVotesForMe
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_resets_election_timeout =
    testCase "follower becoming candidate resets election timeout" <| fun _ ->
      raft {
        do! setElectionTimeoutM 1000u
        do! expectM "Should have zero elapsed timout" 0u timeoutElapsed
        do! periodic 900u
        do! expectM "Should have 900 elapsed timout" 900u timeoutElapsed
        do! becomeCandidate ()
        do! expectM "Should have timeout elapsed below 1000" true (timeoutElapsed >> ((>) 1000u))
      }
      |> runWithDefaults
      |> noError

  let follower_becoming_candidate_requests_votes_from_other_servers =
    testCase "follower becoming candidate requests votes from other servers" <| fun _ ->
      let peer0 = Node.create (Id.Create())
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())

      let state : Raft = mkRaft peer0
      let lokk = new System.Object()
      let i = ref 0
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendRequestVote = fun _ _ ->
              lock lokk <| fun _ ->
                i := !i + 1
                Some { Granted = true; Term = 3u; Reason = None } }
        :> IRaftCallbacks

      raft {
        do! addNodeM peer1
        do! addNodeM peer2
        do! setTermM 2u
        do! becomeCandidate ()
        expect "Should have two vote requests" 2 id !i
      }
      |> runWithRaft state cbs
      |> noError

  let candidate_receives_majority_of_votes_becomes_leader =
    testCase "candidate receives majority of votes becomes leader" <| fun _ ->
      let self  = Node.create (Id.Create())
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let peer3 = Node.create (Id.Create())
      let peer4 = Node.create (Id.Create())

      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendRequestVote = fun n _ -> Some { Term = 1u; Granted = true; Reason = None } }
        :> IRaftCallbacks

      raft {
        do! addPeersM [| peer1; peer2; peer3; peer4 |]
        do! expectM "Should have 5 nodes" 5u numNodes
        do! becomeCandidate ()
        do! expectM "Should be leader" true isLeader
      }
      |> runWithCBS cbs
      |> noError

  let candidate_will_not_respond_to_voterequest_if_it_has_already_voted =
    testCase "candidate will not respond to voterequest if it has already voted" <| fun _ ->
      raft {
        let! raft' = get
        let peer = Node.create (Id.Create())
        let vote : VoteRequest =
          { Term = 0u                // term must be equal or lower that raft's
          ; Candidate = raft'.Node    // term for this to work
          ; LastLogIndex = 0u
          ; LastLogTerm = 0u
          }
        do! addPeerM peer
        do! voteFor (Some raft'.Node)
        let! resp = receiveVoteRequest peer.Id vote
        expect "Should have failed" true Vote.declined resp
      }
      |> runWithDefaults
      |> noError

  let candidate_requestvote_includes_logidx =
    testCase "candidate requestvote includes logidx" <| fun _ ->
      let self = Node.create (Id.Create())
      let raft' : Raft = mkRaft self
      let sender = Sender.create
      let response = { Term = 5u; Granted = true; Reason = None }
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendRequestVote = senderRequestVote sender (Some response) }
        :> IRaftCallbacks

      raft {
        let peer1 = Node.create (Id.Create())
        let peer2 = Node.create (Id.Create())

        let log =
          LogEntry(Id.Create(),0u, 3u, DataSnapshot State.Empty,
            Some <| LogEntry(Id.Create(),0u, 1u, DataSnapshot State.Empty,
              Some <| LogEntry(Id.Create(),0u, 1u, DataSnapshot State.Empty, None)))

        do! addPeersM [| peer1; peer2 |]
        do! setStateM Candidate
        do! setTermM 5u
        do! appendEntryM log >>= ignoreM

        let! request = sendVoteRequest peer1
        Async.RunSynchronously request |> ignore

        do! receiveVoteResponse peer1.Id response

        let vote = List.head (!sender.Outbox) |> getVote

        expect "should have last log index be 3" 3u Vote.lastLogIndex vote
        expect "should have last term be 5" 5u Vote.term vote
        expect "should have last log term be 3" 3u Vote.lastLogTerm vote
        expect "should have candidate id be me" self Vote.candidate vote
      }
      |> runWithRaft raft' cbs
      |> noError

  let candidate_recv_requestvote_response_becomes_follower_if_current_term_is_less_than_term =
    testCase "candidate recv requestvote response becomes follower if current term is less than term" <| fun _ ->
      raft {
        let peer = Node.create (Id.Create())
        let response = { Term = 2u ; Granted = false; Reason = None }
        do! addPeerM peer
        do! setTermM 1u
        do! setStateM Candidate
        do! voteFor None
        do! expectM "Should not be follower" false isFollower
        do! expectM "Should not *have* a leader" None currentLeader
        do! expectM "Should have term 1" 1u currentTerm
        do! receiveVoteResponse peer.Id response
        do! expectM "Should be Follower" Follower getState
        do! expectM "Should have term 2" 2u currentTerm
        do! expectM "Should have voted for nobody" None votedFor
      }
      |> runWithDefaults
      |> noError


  let candidate_recv_appendentries_frm_leader_results_in_follower =
    testCase "candidate recv appendentries frm leader results in follower" <| fun _ ->
      let peer = Node.create (Id.Create())
      let ae : AppendEntries =
        { Term = 1u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 0u
        ; LeaderCommit = 0u
        ; Entries = None
        }

      raft {
        do! addPeerM peer
        do! setStateM Candidate
        do! voteFor None
        do! expectM "Should not be follower" false isFollower
        do! expectM "Should have no leader" None currentLeader
        do! expectM "Should have term 0u" 0u currentTerm
        let! resp = receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should be follower" Follower getState
        do! expectM "Should have peer as leader" (Some peer.Id) currentLeader
        do! expectM "Should have term 1" 1u currentTerm
        do! expectM "Should have voted for noone" None votedFor
      }
      |> runWithDefaults
      |> noError

  let candidate_recv_appendentries_from_same_term_results_in_step_down =
    testCase "candidate recv appendentries from same term results in step down" <| fun _ ->
      let peer = Node.create (Id.Create())
      let ae : AppendEntries =
        { Term = 2u
        ; PrevLogIdx = 1u
        ; PrevLogTerm = 1u
        ; LeaderCommit = 0u
        ; Entries = None
        }

      raft {
        do! addPeerM peer
        do! setTermM 2u
        do! setStateM Candidate
        do! expectM "Should not be follower" false isFollower
        let! resp = receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should not be candidate anymore" false isCandidate
      }
      |> runWithDefaults
      |> noError

  let leader_becomes_leader_is_leader =
    testCase "leader becomes leader is leader" <| fun _ ->
      raft {
        do! becomeLeader ()
        do! expectM "Should be leader" Leader getState
      }
      |> runWithDefaults
      |> noError

  let leader_becomes_leader_does_not_clear_voted_for =
    testCase "leader becomes leader does not clear voted for" <| fun _ ->
      raft {
        let! raft' = get
        do! voteForMyself ()
        do! expectM "Should have voted for myself" (Some raft'.Node.Id) votedFor
        do! becomeLeader ()
        do! expectM "Should still have votedFor" (Some raft'.Node.Id) votedFor
      }
      |> runWithDefaults
      |> noError

  let leader_when_becomes_leader_all_nodes_have_nextidx_equal_to_lastlog_idx_plus_1 =
    testCase "leader when becomes leader all nodes have nextidx equal to lastlog idx plus 1" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())

      raft {
        do! addPeerM peer1
        do! addPeerM peer2
        do! setStateM Candidate
        do! becomeLeader ()
        let! raft' = get
        let cidx = currentIndex raft' + 1u

        for peer in raft'.Peers do
          if peer.Value.Id <> raft'.Node.Id then
            expect "Should have correct nextIndex" cidx id peer.Value.NextIndex
      }
      |> runWithDefaults
      |> noError

  let leader_when_it_becomes_a_leader_sends_empty_appendentries =
    testCase "leader when it becomes a leader sends empty appendentries" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())

      let lokk = new System.Object()
      let count = ref 0
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendAppendEntries = fun _ _ ->
              lock lokk <| fun _ -> count := !count + 1
              Some { Success = true; Term = 0u; CurrentIndex = 1u; FirstIndex = 1u } }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer1
        do! addPeerM peer2
        do! setStateM Candidate
        do! becomeLeader ()
        expect "Should have two messages" 2 id !count
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_responds_to_entry_msg_when_entry_is_committed =
    testCase "leader responds to entry msg when entry is committed" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log = LogEntry(Id.Create(),0u,0u,DataSnapshot State.Empty,None)

      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! expectM "Should have log count 0u" 0u numLogs
        let! resp = receiveEntry log
        do! expectM "Should have log count 1u" 1u numLogs
        do! applyEntries ()
        let response = { Term = 0u; Success = true; CurrentIndex = 1u; FirstIndex = 1u }
        do! receiveAppendEntriesResponse peer.Id response
        let! committed = responseCommitted resp
        expect "Should be committed" true id committed
      }
      |> runWithDefaults
      |> noError


  let non_leader_recv_entry_msg_fails =
    testCase "non leader recv entry msg fails" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log = LogEntry(Id.Create(),0u,0u,DataSnapshot State.Empty,None)

      raft {
        do! addNodeM peer
        do! setStateM Follower
        let! resp = receiveEntry log
        return "never reached"
      }
      |> runWithDefaults
      |> expectError NotLeader

  let leader_sends_appendentries_with_NextIdx_when_PrevIdx_gt_NextIdx =
    testCase "leader sends appendentries with NextIdx when PrevIdx gt NextIdx" <| fun _ ->
      let peer = { Node.create (Id.Create()) with NextIndex = 4u }
      let raft' : Raft = defaultServer "localhost"
      let sender = Sender.create
      let log = LogEntry(Id.Create(),0u, 1u, DataSnapshot State.Empty, None)
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! sendAllAppendEntriesM ()
        expect "Should have one message in cue" 1 List.length (!sender.Outbox)
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_sends_appendentries_with_leader_commit =
    testCase "leader sends appendentries with leader commit" <| fun _ ->
      let peer = { Node.create (Id.Create()) with NextIndex = 4u }
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader

        for n in 0 .. 9 do
          let l = LogEntry(Id.Create(), 0u, 1u, DataSnapshot State.Empty, None)
          do! appendEntryM l >>= ignoreM

        do! setCommitIndexM 10u
        do! sendAllAppendEntriesM ()

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have leader commit 10u" 10u (fun ae -> ae.LeaderCommit)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_sends_appendentries_with_prevLogIdx =
    testCase "leader sends appendentries with prevLogIdx" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader

        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIndex 0" 0u (fun ae -> ae.PrevLogIdx)

        let log = LogEntry(Id.Create(),0u,2u,DataSnapshot State.Empty,None)

        do! appendEntryM log >>= ignoreM
        do! setNextIndexM peer.Id 1u

        let! peer = getNodeM peer.Id >>= (Option.get >> returnM)

        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> assume "Should have PrevLogIdx 0" 0u (fun ae -> ae.PrevLogIdx)
        |> assume "Should have one entry" 1u (fun ae -> ae.Entries |> Option.get |> LogEntry.depth )
        |> assume "Should have entry with correct id" (LogEntry.getId log) (fun ae -> ae.Entries |> Option.get |> LogEntry.getId)
        |> expect "Should have entry with term" 2u (fun ae -> ae.Entries |> Option.get |> LogEntry.term)

        sender.Outbox := List.empty // reset outbox

        do! setNextIndexM peer.Id 2u
        let! peer = getNodeM peer.Id >>= (Option.get >> returnM)
        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 1" 1u (fun ae -> ae.PrevLogIdx)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_sends_appendentries_when_node_has_next_idx_of_0 =
    testCase "leader sends appendentries when node has next idx of 0" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader
        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 0" 0u (fun ae -> ae.PrevLogIdx)

        sender.Outbox := List.empty // reset outbox

        let log = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty, None)

        do! setNextIndexM peer.Id 1u
        do! appendEntryM log >>= ignoreM
        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> List.head
        |> getAppendEntries
        |> expect "Should have PrevLogIdx 0" 0u (fun ae -> ae.PrevLogIdx)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_retries_appendentries_with_decremented_NextIdx_log_inconsistency =
    testCase "leader retries appendentries with decremented NextIdx log inconsistency" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader
        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        (!sender.Outbox)
        |> expect "Should have a message" 1 List.length
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_append_entry_to_log_increases_idxno =
    testCase "leader append entry to log increases idxno" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let raft' = defaultServer "local"
      let sender = Sender.create
      let cbs = mkcbs (ref (DataSnapshot State.Empty)) :> IRaftCallbacks

      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! expectM "Should have zero logs" 0u numLogs
        let! resp = receiveEntry log
        do! expectM "Should have on log" 1u numLogs
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_increase_commit_idx_when_majority_have_entry_and_atleast_one_newer_entry =
    testCase "leader recv appendentries response increase commit idx when majority have entry and atleast one newer entry" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let peer3 = Node.create (Id.Create())
      let peer4 = Node.create (Id.Create())

      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log1 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log2 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log3 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)

      let response =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 3u
        ; FirstIndex = 1u
        }

      raft {
        do! addNodesM [| peer1; peer2; peer3; peer4; |]
        do! setStateM Leader
        do! setTermM 1u
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM

        // peer 1
        let! request = sendAppendEntry peer1
        Async.RunSynchronously request |> ignore

        // peer 2
        let! request = sendAppendEntry peer2
        Async.RunSynchronously request |> ignore

        do! receiveAppendEntriesResponse peer1.Id response
        // first response, no majority yet, will not set commit idx
        do! expectM "Should have commit index 0" 0u commitIndex

        do! receiveAppendEntriesResponse peer2.Id response
        //  leader will now have majority followers who have appended this log
        do! expectM "Should have commit index 3" 3u commitIndex

        do! expectM "Should have last applied index 0" 0u lastAppliedIdx
        do! periodic 1u
        // should have now applied all committed ertries
        do! expectM "Should have last applied index 3" 3u lastAppliedIdx
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_duplicate_does_not_decrement_match_idx =
    testCase "leader recv appendentries response duplicate does not decrement match idx" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())

      let response =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 1u
        ; FirstIndex = 1u
        }

      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs = mkcbs (ref (DataSnapshot State.Empty)) :> IRaftCallbacks

      let log1 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log2 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log3 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)

      raft {
        do! addNodesM [| peer1; peer2; |]
        do! setStateM Leader
        do! setTermM 1u
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM
        do! sendAllAppendEntriesM ()
        do! receiveAppendEntriesResponse peer1.Id response
        do! receiveAppendEntriesResponse peer2.Id response
        do! expectM "Should have matchIdx 1" 1u (getNode peer1.Id >> Option.get >> Node.getMatchIndex)
        do! receiveAppendEntriesResponse peer1.Id response
        do! expectM "Should still have matchIdx 1" 1u (getNode peer1.Id >> Option.get >> Node.getMatchIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_do_not_increase_commit_idx_because_of_old_terms_with_majority =
    testCase "leader recv appendentries response do not increase commit idx because of old terms with majority" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let peer3 = Node.create (Id.Create())
      let peer4 = Node.create (Id.Create())

      let response =
        { Term         = 1u
        ; Success      = true
        ; CurrentIndex = 1u
        ; FirstIndex   = 1u }

      let cbs = mkcbs (ref (DataSnapshot State.Empty)) :> IRaftCallbacks

      let log1 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log2 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log3 = LogEntry(Id.Create(),0u,2u,DataSnapshot State.Empty,None)

      raft {
        do! addNodesM [| peer1; peer2; peer3; peer4 |]
        do! setStateM Leader
        do! setTermM 2u
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM

        let! request = sendAppendEntry peer1
        Async.RunSynchronously request |> ignore

        let! request = sendAppendEntry peer2
        Async.RunSynchronously request |> ignore

        do! receiveAppendEntriesResponse peer1.Id response
        do! expectM "Should have commit index 0" 0u commitIndex

        do! receiveAppendEntriesResponse peer2.Id response
        do! expectM "Should have commit index 0" 0u commitIndex

        do! periodic 1u
        do! expectM "Should have lastAppliedIndex 0" 0u lastAppliedIdx

        let! request = sendAppendEntry peer1
        Async.RunSynchronously request |> ignore

        let! request = sendAppendEntry peer2
        Async.RunSynchronously request |> ignore

        do! receiveAppendEntriesResponse peer1.Id { response with CurrentIndex = 2u; FirstIndex = 2u }
        do! expectM "Should have commit index 0" 0u commitIndex

        do! receiveAppendEntriesResponse peer2.Id { response with CurrentIndex = 2u; FirstIndex = 2u }
        do! expectM "Should have commit index 0" 0u commitIndex

        do! periodic 1u
        do! expectM "Should have lastAppliedIndex 0" 0u lastAppliedIdx

        let! request = sendAppendEntry peer1
        Async.RunSynchronously request |> ignore

        let! request = sendAppendEntry peer2
        Async.RunSynchronously request |> ignore

        do! receiveAppendEntriesResponse peer1.Id { response with Term = 2u; CurrentIndex = 3u; FirstIndex = 3u }
        do! expectM "Should have commit index 0" 0u commitIndex

        do! receiveAppendEntriesResponse peer2.Id { response with Term = 2u; CurrentIndex = 3u; FirstIndex = 3u }
        do! expectM "Should have commit index 3" 3u commitIndex

        do! periodic 1u
        do! expectM "Should have lastAppliedIndex 3" 3u lastAppliedIdx
      }
      |> runWithCBS cbs
      |> noError

  let leader_recv_appendentries_response_jumps_to_lower_next_idx =
    testCase "leader recv appendentries response jumps to lower next idx" <| fun _ ->
      let peer = Node.create (Id.Create())

      let lokk = new System.Object()
      let count = ref 0
      let appendReq = ref None

      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendAppendEntries = fun n ae ->
              lock lokk <| fun _ -> count := !count + 1
              appendReq := Some ae
              None }
        :> IRaftCallbacks

      let log1 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log2 = LogEntry(Id.Create(),0u,2u,DataSnapshot State.Empty,None)
      let log3 = LogEntry(Id.Create(),0u,3u,DataSnapshot State.Empty,None)
      let log4 = LogEntry(Id.Create(),0u,4u,DataSnapshot State.Empty,None)

      let response =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 1u
        ; FirstIndex = 1u }

      raft {
        do! addNodeM peer
        do! setStateM Leader
        do! setTermM 2u
        do! setCommitIndexM 0u
        do! setLastAppliedIdxM 0u
        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM
        do! appendEntryM log4 >>= ignoreM
        do! becomeLeader ()

        do! expectM "Should have nextIdx 5" 5u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! expectM "Should have a msg 1" 1 (konst !count)

        // need to get an up-to-date version of the peer, because its nextIdx
        // will have been bumped when becoming leader!
        let! peer = getNodeM peer.Id >>= (Option.get >> returnM)

        do! sendAllAppendEntriesM ()

        expect "Should have prevLogIdx 4" 4u AppendRequest.prevLogIndex (!appendReq |> Option.get)
        expect "Should have prevLogTerm 4" 4u AppendRequest.prevLogTerm (!appendReq |> Option.get)

        let! term = currentTermM ()
        do! receiveAppendEntriesResponse peer.Id { response with Term = term; Success = false; CurrentIndex = 1u }

        do! expectM "Should have NextIdx 2" 2u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! expectM "Should have MatchIdx 2" 1u (getNode peer.Id >> Option.get >> Node.getMatchIndex)
        do! expectM "Should have 2 msgs"    2   (konst !count)

        do! sendAllAppendEntriesM ()

        expect "Should have prevLogIdx 1"  1u AppendRequest.prevLogIndex (!appendReq |> Option.get)
        expect "Should have prevLogTerm 1" 1u AppendRequest.prevLogTerm  (!appendReq |> Option.get)
      }
      |> runWithCBS cbs
      |> noError


  let leader_recv_appendentries_response_decrements_to_lower_next_idx =
    testCase "leader recv appendentries response decrements to lower next idx" <| fun _ ->
      let peer = Node.create (Id.Create())
      let lokk = new System.Object()

      let ci = ref 0u
      let term = ref 2u
      let result = ref false
      let count = ref 0

      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with
            SendAppendEntries = fun n ae ->
              lock lokk <| fun _ -> count := !count + 1
              Some { Term         = !term
                   ; Success      = !result
                   ; CurrentIndex = !ci
                   ; FirstIndex   = 0u }
          } :> IRaftCallbacks

      let log1 = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)
      let log2 = LogEntry(Id.Create(),0u,2u,DataSnapshot State.Empty,None)
      let log3 = LogEntry(Id.Create(),0u,3u,DataSnapshot State.Empty,None)
      let log4 = LogEntry(Id.Create(),0u,4u,DataSnapshot State.Empty,None)

      raft {
        do! addNodeM peer
        do! setTermM !term
        do! setCommitIndexM 0u

        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM
        do! appendEntryM log4 >>= ignoreM

        ci := 0u
        do! becomeLeader ()

        do! expectM "Should have correct NextIndex" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! expectM "Should have correct MatchIndex" 0u (getNode peer.Id >> Option.get >> Node.getMatchIndex)
        do! expectM "Should have been called once" 1  (konst !count)

        // need to get updated peer, because nextIdx will be bumped when
        // becoming leader!
        let! peer = getNodeM peer.Id >>= (Option.get >> returnM)

        // we pretend that the follower `peer` has now successfully appended those logs
        let! t = currentTermM ()
        term := t
        ci := 4u
        result := true

        // send again and process responses
        do! sendAllAppendEntriesM ()

        do! expectM "Should finally have NextIndex 5"  5u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! expectM "Should finally have MatchIndex 4" 4u (getNode peer.Id >> Option.get >> Node.getMatchIndex)
        do! expectM "Should have been called twice" 2 (konst !count)
      }
      |> runWithCBS cbs
      |> noError

  let leader_recv_appendentries_response_retry_only_if_leader =
    testCase "leader recv appendentries response retry only if leader" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())

      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref (DataSnapshot State.Empty)) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = LogEntry(Id.Create(),0u,1u,DataSnapshot State.Empty,None)

      let response =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 1u
        ; FirstIndex = 1u
        }

      raft {
        do! addNodesM [| peer1; peer2 |]
        do! setTermM 1u
        do! setCommitIndexM 0u
        do! setStateM Leader
        do! setLastAppliedIdxM 0u

        do! appendEntryM log >>= ignoreM

        let! request = sendAppendEntry peer1
        Async.RunSynchronously request |> ignore

        let! request = sendAppendEntry peer2
        Async.RunSynchronously request |> ignore

        do! expectM "Should have 2 msgs" 2 (fun _ -> List.length !sender.Outbox)
        do! becomeFollower ()
        do! receiveAppendEntriesResponse peer1.Id response
      }
      |> runWithRaft raft' cbs
      |> expectError NotLeader

  let leader_recv_entry_resets_election_timeout =
    testCase "leader recv entry resets election timeout" <| fun _ ->
      let log = LogEntry(Id.Create(), 0u, 1u, DataSnapshot State.Empty, None)
      raft {
        do! setElectionTimeoutM 1000u
        do! setStateM Leader
        do! periodic 1000u
        let! response = receiveEntry log
        do! expectM "Should have reset timeout elapsed" 0u timeoutElapsed
      }
      |> runWithDefaults
      |> noError

  let leader_recv_entry_is_committed_returns_0_if_not_committed =
    testCase "leader recv entry is committed returns 0 if not committed" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log = LogEntry(Id.Create(), 0u, 1u, DataSnapshot State.Empty, None)

      raft {
        do! addPeerM peer
        do! setStateM Leader

        do! setCommitIndexM 0u
        let! response = receiveEntry log
        let! committed = responseCommitted response
        expect "Should not have committed" false id committed

        do! setCommitIndexM 1u
        let! response = receiveEntry log
        let! committed = responseCommitted response
        expect "Should have committed" true id committed
      }
      |> runWithDefaults
      |> noError

  let leader_recv_entry_is_committed_returns_neg_1_if_invalidated =
    testCase "leader recv entry is committed returns neg 1 if invalidated" <| fun _ ->
      let peer = Node.create (Id.Create())
      let log = Log.make 1u (DataSnapshot State.Empty)

      let ae =
        { LeaderCommit = 1u
        ; Term = 2u
        ; PrevLogIdx = 0u
        ; PrevLogTerm = 0u
        ; Entries = Log.make 2u defSM |> Some
        }

      raft {
        do! addNodeM peer
        do! setStateM Leader
        do! setCommitIndexM 0u
        do! setTermM 1u

        do! expectM "Should have current idx 0u" 0u currentIndex

        let! response = receiveEntry log
        let! committed = responseCommitted response

        expect "Should not have committed entry" false id committed
        expect "Should have term 1u" 1u Entry.term response
        expect "Should have index 1u" 1u Entry.index response

        do! expectM "(1) Should have current idx 1u" 1u currentIndex
        do! expectM "Should have commit idx 0u" 0u commitIndex

        let! resp = receiveAppendEntries (Some peer.Id) ae

        expect "Should have succeeded" true AppendRequest.succeeded resp

        do! expectM "(2) Should have current idx 1" 1u currentIndex
        do! expectM "Should have commit idx 1" 1u commitIndex

        return! responseCommitted response
      }
      |> runWithDefaults
      |> expectError EntryInvalidated


  let leader_recv_entry_does_not_send_new_appendentries_to_slow_nodes =
    testCase "leader recv entry does not send new appendentries to slow nodes" <| fun _ ->
      skiptest "NO CONGESTION CONTROL CURRENTLY IMPLEMENTED"

      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = Log.make 1u defSM

      raft {
        do! addNodeM peer
        do! setStateM Leader
        do! setTermM 1u
        do! setCommitIndexM 0u
        do! setNextIndexM peer.Id 1u
        do! appendEntryM log >>= ignoreM
        let! response = receiveEntry log

        !sender.Outbox
        |> expect "Should have no msg" 0 List.length
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_failure_does_not_set_node_nextid_to_0 =
    testCase "leader recv appendentries response failure does not set node nextid to 0" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref defSM) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let log = Log.make 1u defSM
      let resp =
        { Term = 1u
        ; Success = false
        ; CurrentIndex = 0u
        ; FirstIndex = 0u
        }

      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! setTermM 1u
        do! setCommitIndexM 0u
        do! appendEntryM log >>= ignoreM

        let! request = sendAppendEntry peer
        Async.RunSynchronously request |> ignore

        do! receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx Works 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx Dont work 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_response_increment_idx_of_node =
    testCase "leader recv appendentries response increment idx of node" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref defSM) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let resp =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 0u
        ; FirstIndex = 0u
        }

      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! setTermM 1u
        do! expectM "Should have nextIdx 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError


  let leader_recv_appendentries_response_drop_message_if_term_is_old =
    testCase "leader recv appendentries response drop message if term is old" <| fun _ ->
      let peer = Node.create (Id.Create())
      let raft' = defaultServer "localhost"
      let sender = Sender.create
      let cbs =
        { mkcbs (ref defSM) with SendAppendEntries = senderAppendEntries sender None }
        :> IRaftCallbacks

      let resp =
        { Term = 1u
        ; Success = true
        ; CurrentIndex = 1u
        ; FirstIndex = 1u
        }
      raft {
        do! addPeerM peer
        do! setStateM Leader
        do! setTermM 2u
        do! expectM "Should have nextIdx 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
        do! receiveAppendEntriesResponse peer.Id resp
        do! expectM "Should have nextIdx 1" 1u (getNode peer.Id >> Option.get >> Node.getNextIndex)
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_appendentries_steps_down_if_newer =
    testCase "leader recv appendentries steps down if newer" <| fun _ ->
      let peer = Node.create (Id.Create())
      let ae =
        { Term = 6u
        ; PrevLogIdx = 6u
        ; PrevLogTerm = 5u
        ; LeaderCommit = 0u
        ; Entries = None
        }
      raft {
        let! raft' = get
        let nid = Some raft'.Node.Id
        do! addNodeM peer
        do! setStateM Leader
        do! setTermM 5u
        do! expectM "Should be leader" true isLeader
        do! expectM "Should be leader" true (currentLeader >> ((=) nid))
        let! response = receiveAppendEntries (Some peer.Id) ae
        do! expectM "Should be follower" true isFollower
        do! expectM "Should follow peer" true (currentLeader >> ((=) nid))
      }
      |> runWithDefaults
      |> noError

  let leader_recv_appendentries_steps_down_if_newer_term =
    testCase "leader recv appendentries steps down if newer term" <| fun _ ->
      let peer = Node.create (Id.Create())
      let resp =
        { Term = 6u
        ; PrevLogIdx = 5u
        ; PrevLogTerm = 5u
        ; LeaderCommit = 0u
        ; Entries = None
        }
      raft {
        do! addNodeM peer
        do! setStateM Leader
        do! setTermM 5u
        let! response = receiveAppendEntries (Some peer.Id) resp
        do! expectM "Should be follower" true isFollower
      }
      |> runWithDefaults
      |> noError

  let leader_sends_empty_appendentries_every_request_timeout =
    testCase "leader sends empty appendentries every request timeout" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let raft' = defaultServer "localhost"

      let lokk = new System.Object()

      let count = ref 0

      let response =
        ref { Term = 0u
            ; Success = true
            ; CurrentIndex = 1u
            ; FirstIndex = 1u }

      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ _ ->
              lock lokk <| fun _ ->
                count := !count + 1
              Some !response
            }
        :> IRaftCallbacks

      raft {
        do! addNodesM [| peer1; peer2 |]
        do! setElectionTimeoutM 1000u
        do! setRequestTimeoutM 500u
        do! expectM "Should have timout elapsed 0" 0u timeoutElapsed

        do! setStateM Candidate
        do! becomeLeader ()

        do! expectM "Should have 2 messages " 2 (konst !count)

        // update CurrentIndex to latest nodeIdx to prevent StaleResponse error
        let! node1 = getNodeM peer1.Id

        response := { !response with
                        CurrentIndex = Option.get node1 |> Node.getNextIndex |> ((+) 1u) }

        do! periodic 501u

        do! expectM "Should have 4 messages" 4 (konst !count) // because 2 peers
      }
      |> runWithRaft raft' cbs
      |> noError

  let leader_recv_requestvote_responds_without_granting =
    testCase "leader recv requestvote responds without granting" <| fun _ ->
      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let sender = Sender.create
      let resp = { Term = 1u; Granted = true; Reason = None }

      let vote =
        { Term = 1u
        ; Candidate = peer2
        ; LastLogIndex = 0u
        ; LastLogTerm = 0u }

      raft {
        do! addNodesM [| peer1; peer2 |]
        do! setElectionTimeoutM 1000u
        do! setRequestTimeoutM 500u
        do! expectM "Should have timout elapsed 0" 0u timeoutElapsed
        do! startElection ()
        do! receiveVoteResponse peer1.Id resp
        do! expectM "Should be leader" Leader getState
        let! resp = receiveVoteRequest peer2.Id vote
        expect "Should have declined vote" true Vote.declined resp
      }
      |> runWithDefaults
      |> noError


  let leader_recv_requestvote_responds_with_granting_if_term_is_higher =
    testCase "leader recv requestvote responds with granting if term is higher" <| fun _ ->

      let peer1 = Node.create (Id.Create())
      let peer2 = Node.create (Id.Create())
      let sender = Sender.create
      let resp = { Term = 1u; Granted = true; Reason = None }

      let vote =
        { Term = 2u
        ; Candidate = peer2
        ; LastLogIndex = 0u
        ; LastLogTerm = 0u }

      raft {
        do! addNodesM [| peer1; peer2 |]
        do! setElectionTimeoutM 1000u
        do! setRequestTimeoutM 500u
        do! expectM "Should have timout elapsed 0" 0u timeoutElapsed

        do! startElection ()
        do! receiveVoteResponse peer1.Id resp
        do! expectM "Should be Leader" true isLeader
        let! resp = receiveVoteRequest peer2.Id vote
        do! expectM "Should be Follower" true isFollower
      }
      |> runWithDefaults
      |> noError


  let server_should_not_request_vote_from_failed_nodes =
    testCase "should not request vote from failed nodes" <| fun _ ->
      let node1 =   Node.create (Id.Create())
      let node2 =   Node.create (Id.Create())
      let node3 =   Node.create (Id.Create())
      let node4 = { Node.create (Id.Create())  with State = RaftNodeState.Failed }

      let mutable i = 0

      let raft' = mkRaft node1
      let cbs =
        { mkcbs (ref defSM) with SendRequestVote = fun _ _ -> i <- i + 1; None }
        :> IRaftCallbacks

      raft {
        do! addPeersM [| node2; node3; node4 |]
        do! setElectionTimeoutM 1000u
        do! periodic 1001u
        expect "Should have sent 2 requests" 2 id i
      }
      |> runWithRaft raft' cbs
      |> noError




  let server_should_not_consider_failed_nodes_when_deciding_vote_outcome =
    testCase "should not consider failed nodes when deciding vote outcome" <| fun _ ->
      let node1 =   Node.create (Id.Create())
      let node2 =   Node.create (Id.Create())
      let node3 = { Node.create (Id.Create())  with State = RaftNodeState.Failed }
      let node4 = { Node.create (Id.Create())  with State = RaftNodeState.Failed }

      let resp = { Term = 1u; Granted = true; Reason = None }

      raft {
        do! addPeersM [| node1; node2; node3; node4 |]
        do! setElectionTimeoutM 1000u
        do! periodic 1001u
        do! receiveVoteResponse node1.Id resp
        do! expectM "Should be leader now" Leader getState
      }
      |> runWithDefaults
      |> noError


  let server_periodic_should_trigger_snapshotting =
    testCase "periodic should trigger snapshotting when MaxLogDepth is reached" <| fun _ ->
      raft {
        let term = 1u
        let depth = 40u
        let! me = selfM ()

        do! setMaxLogDepthM depth
        do! setTermM term

        for n in 0u .. depth do
          do! appendEntryM (Log.make term defSM) >>= ignoreM

        do! setLeaderM (Some me.Id)
        do! expectM "Should have correct number of entries" (depth + 1u) numLogs
        do! periodic 10u
        do! expectM "Should have correct number of entries" 1u numLogs
      }
      |> runWithDefaults
      |> noError

  let server_should_apply_each_log_when_receiving_a_snapshot =
    testCase "should apply each log when receiving a snapshot" <| fun _ ->
      let idx = 9u
      let term = 1u
      let count = ref 0

      let init = defaultServer "holy crap"
      let cbs =
        { mkcbs (ref defSM) with ApplyLog = fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      let nodes =
        [| "one"; "two"; "three" |]
        |> Array.mapi (fun i _ -> Node.create (Id.Create()))

      let is: InstallSnapshot =
        { Term = term
        ; LeaderId = Id.Create()
        ; LastTerm = term
        ; LastIndex = idx
        ; Data = Snapshot(Id.Create(), idx, term, idx, term, nodes, defSM) }

      raft {
        do! setTermM term
        let! response = receiveInstallSnapshot is
        do! expectM "Should have correct number of nodes" 4u numNodes // including our own node
        do! expectM "Should have correct number of log entries" 1u numLogs
        expect "Should have called ApplyLog once" 1 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_merge_snaphot_and_existing_log_when_receiving_a_snapshot =
    testCase "should merge snaphot and existing log when receiving a snapshot" <| fun _ ->
      let idx = 9u
      let num = 5u
      let term = 1u
      let count = ref 0

      let init = defaultServer "holy crap"
      let cbs =
        { mkcbs (ref defSM) with
            ApplyLog = fun l ->
              count := !count + 1
          }
        :> IRaftCallbacks

      let nodes =
        [| "one"; "two"; "three" |]
        |> Array.mapi (fun i _ -> Node.create (Id.Create()))

      let is: InstallSnapshot =
        { Term = term
        ; LeaderId = Id.Create()
        ; LastTerm = term
        ; LastIndex = idx
        ; Data = Snapshot(Id.Create(), idx, term, idx, term, nodes, defSM)
        }

      raft {
        do! setTermM term
        for n in 0u .. (idx + num) do
          do! appendEntryM (Log.make term (DataSnapshot State.Empty)) >>= ignoreM

        do! applyEntries ()

        let! response = receiveInstallSnapshot is

        do! expectM "Should have correct number of nodes" 4u numNodes // including our own node
        do! expectM "Should have correct number of log entries" 7u numLogs
        expect "Should have called ApplyLog once" 7 id !count
      }
      |> runWithRaft init cbs
      |> noError


  let server_should_fire_node_callbacks_on_config_change =
    testCase "should fire node callbacks on config change" <| fun _ ->
      let count = ref 0

      let init = defaultServer "holy crap"

      let cb _ l =
        count := !count + 1

      let cbs =
        { mkcbs (ref defSM) with
            NodeAdded   = cb "added"
            NodeRemoved = cb "removed"
        } :> IRaftCallbacks

      raft {
        let node = Node.create (Id.Create())

        do! setStateM Leader

        do! appendEntryM (JointConsensus(Id.Create(), 0u, 0u, [| NodeAdded(node)|] ,None)) >>= ignoreM
        do! setCommitIndexM 1u
        do! applyEntries ()

        expect "Should have count 1" 1 id !count

        do! appendEntryM (JointConsensus(Id.Create(), 0u, 0u, [| NodeRemoved node |] ,None)) >>= ignoreM
        do! setCommitIndexM 3u
        do! applyEntries ()

        expect "Should have count 2" 2 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_call_persist_callback_for_each_appended_log =
    testCase "should call persist callback for each appended log" <| fun _ ->
      let count = ref List.empty

      let init = defaultServer "holy crap"

      let cb l = count := LogEntry.getId l :: !count

      let cbs =
        { mkcbs (ref defSM) with
            PersistLog = cb
        } :> IRaftCallbacks

      raft {
        let log1 = Log.make 0u defSM
        let log2 = Log.make 0u defSM
        let log3 = Log.make 0u defSM

        let ids =
          [ log3; log2; log1; ]
          |> List.map LogEntry.getId

        do! setStateM Leader

        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2  >>= ignoreM
        do! appendEntryM log3  >>= ignoreM

        expect "should have correct ids" ids id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_call_delete_callback_for_each_deleted_log =
    testCase "should call delete callback for each deleted log" <| fun _ ->
      let log1 = Log.make 0u defSM
      let log2 = Log.make 0u defSM
      let log3 = Log.make 0u defSM

      let count = ref [ log3; log2; log1; ]

      let init = defaultServer "holy crap"

      let cb l =
        let fltr l r = LogEntry.getId l <> LogEntry.getId r
        in count := List.filter (fltr l) !count

      let cbs =
        { mkcbs (ref defSM) with
            DeleteLog = cb
        } :> IRaftCallbacks

      raft {
        do! setStateM Leader

        do! appendEntryM log1 >>= ignoreM
        do! appendEntryM log2 >>= ignoreM
        do! appendEntryM log3 >>= ignoreM

        do! removeEntryM 3u
        do! expectM "Should have only 2 entries" 2u numLogs

        do! removeEntryM 2u
        do! expectM "Should have only 1 entry" 1u numLogs

        do! removeEntryM 1u
        do! expectM "Should have zero entries" 0u numLogs

        expect "should have deleted all logs" List.empty id !count
      }
      |> runWithRaft init cbs
      |> noError


  let should_call_node_updated_callback_on_node_udpated =
    testCase "call node updated callback on node udpated" <| fun _ ->
      let count = ref 0
      let init = mkRaft (Node.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    NodeUpdated = fun _ -> count := 1 + !count }
                :> IRaftCallbacks

      raft {
        let node = Node.create (Id.Create())
        do! addNodeM node
        do! updateNodeM { node with State = RaftNodeState.Joining }
        do! updateNodeM { node with State = RaftNodeState.Running }
        do! updateNodeM { node with State = RaftNodeState.Failed }

        expect "Should have called once" 3 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let should_call_state_changed_callback_on_state_change =
    testCase "call state changed callback on state change" <| fun _ ->
      let count = ref 0
      let init = mkRaft (Node.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    StateChanged = fun _ _ -> count := 1 + !count }
                :> IRaftCallbacks

      raft {
        do! becomeCandidate ()
        do! becomeLeader ()
        do! becomeFollower ()
        expect "Should have called once" 3 id !count
      }
      |> runWithRaft init cbs
      |> noError

  let should_respond_to_appendentries_with_correct_next_idx =
    testCase "respond to appendentries with correct next idx" <| fun _ ->
      let term = 1u

      raft {
        do! setTermM term
        do! becomeLeader ()

        let! response = Log.make term defSM |> receiveEntry
        let! committed = responseCommitted response

        do! expectM "Should be committed" true (konst committed)

        let! response = Log.make term defSM |> receiveEntry
        let! committed = responseCommitted response

        do! expectM "Should be committed" true (konst committed)

        let peer = Node.create (Id "0xdeadbeef")
        do! becomeFollower ()
        do! addNodeM peer

        let! term = currentTermM ()
        let! ci = currentIndexM ()
        let! fi = firstIndexM term

        let ping : AppendEntries =
          { Term         = term
          ; PrevLogIdx   = ci
          ; PrevLogTerm  = term
          ; LeaderCommit = ci
          ; Entries      = None }

        let! response = receiveAppendEntries (Some peer.Id) ping

        do! expectM "Should have correct CurrentIndex" ci (konst response.CurrentIndex)
        do! expectM "Should have correct FirstIndex" fi (konst response.FirstIndex >> Some)
        do! expectM "Should have correct Term" term (konst response.Term)
        do! expectM "Should be success" true (konst response.Success)
      }
      |> runWithDefaults
      |> noError

  let should_call_apply_entries_callback =
    testCase "call apply entries callback" <| fun _ ->
      let count = ref 0

      let cbs =
        { mkcbs (ref defSM) with ApplyLog = fun _ -> count := !count + 1 }
        :> IRaftCallbacks

      raft {
        do! setTermM 1u
        do! becomeLeader ()

        let! term = currentTermM ()

        let log = Log.make term defSM
        let! result = receiveEntry log

        do! periodic 10u

        let! committed = responseCommitted result

        do! expectM "Should have committed entry" true (konst committed)
        do! expectM "Should have called callback" 1 (konst !count)
      }
      |> runWithCBS cbs
      |> noError
