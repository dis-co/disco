namespace Pallet.Tests

open Fuchu
open Fuchu.Test

[<AutoOpen>]
module Tests =

  [<Tests>]
  let palletTests =
    testList "Pallet tests"
      [
        // Continue
        validation_dsl_validation

        // Node
        node_init_test

        // Log
        log_new_log_is_empty
        log_is_non_empty
        log_have_correct_index
        log_get_at_index
        log_find_by_id
        log_depth_test
        log_resFold_short_circuit_test
        log_concat_length_test
        log_concat_monotonicity_test
        log_get_entries_until_test
        log_concat_ensure_no_duplicate_entries
        log_append_ensure_no_duplicate_entries
        log_concat_ensure_no_duplicate_but_unique_entries
        log_snapshot_remembers_last_state
        log_untilExcluding_should_return_expected_enries
        log_append_should_work_with_snapshots_too
        log_firstIndex_should_return_correct_results
        log_getn_should_return_right_number_of_entries

        // Server
        candidate_becomes_candidate_is_candidate
        candidate_election_timeout_and_no_leader_results_in_new_election
        candidate_receives_majority_of_votes_becomes_leader
        candidate_recv_appendentries_frm_leader_results_in_follower
        candidate_recv_appendentries_from_same_term_results_in_step_down
        candidate_recv_requestvote_response_becomes_follower_if_current_term_is_less_than_term
        candidate_requestvote_includes_logidx
        candidate_will_not_respond_to_voterequest_if_it_has_already_voted

        follower_becomes_candidate_when_election_timeout_occurs
        follower_becomes_follower_does_not_clear_voted_for
        follower_becomes_follower_is_follower
        follower_becoming_candidate_increments_current_term
        follower_becoming_candidate_requests_votes_from_other_servers
        follower_becoming_candidate_resets_election_timeout
        follower_becoming_candidate_votes_for_self
        follower_dont_grant_vote_if_candidate_has_a_less_complete_log
        follower_recv_appendentries_add_new_entries_not_already_in_log
        follower_recv_appendentries_delete_entries_if_conflict_with_new_entries
        follower_recv_appendentries_delete_entries_if_current_idx_greater_than_prev_log_idx
        follower_recv_appendentries_does_not_add_dupe_entries_already_in_log
        follower_recv_appendentries_does_not_log_if_no_entries_are_specified
        follower_recv_appendentries_does_not_need_node
        follower_recv_appendentries_failure_includes_current_idx
        follower_recv_appendentries_increases_log
        follower_recv_appendentries_reply_false_if_doesnt_have_log_at_prev_log_idx_which_matches_prev_log_term
        follower_recv_appendentries_reply_false_if_term_less_than_currentterm
        follower_recv_appendentries_resets_election_timeout
        follower_recv_appendentries_set_commitidx_to_LeaderCommit
        follower_recv_appendentries_set_commitidx_to_prevLogIdx
        follower_recv_appendentries_updates_currentterm_if_term_gt_currentterm

        leader_append_entry_to_log_increases_idxno
        leader_becomes_leader_does_not_clear_voted_for
        leader_becomes_leader_is_leader
        leader_recv_appendentries_response_decrements_to_lower_next_idx
        leader_recv_appendentries_response_do_not_increase_commit_idx_because_of_old_terms_with_majority
        leader_recv_appendentries_response_drop_message_if_term_is_old
        leader_recv_appendentries_response_duplicate_does_not_decrement_match_idx
        leader_recv_appendentries_response_failure_does_not_set_node_nextid_to_0
        leader_recv_appendentries_response_increase_commit_idx_when_majority_have_entry_and_atleast_one_newer_entry
        leader_recv_appendentries_response_increment_idx_of_node
        leader_recv_appendentries_response_jumps_to_lower_next_idx
        leader_recv_appendentries_response_retry_only_if_leader
        leader_recv_appendentries_steps_down_if_newer
        leader_recv_appendentries_steps_down_if_newer_term
        leader_recv_entry_does_not_send_new_appendentries_to_slow_nodes
        leader_recv_entry_is_committed_returns_0_if_not_committed
        leader_recv_entry_is_committed_returns_neg_1_if_invalidated
        leader_recv_entry_resets_election_timeout
        leader_recv_requestvote_does_not_step_down
        leader_recv_requestvote_responds_with_granting_if_term_is_higher
        leader_recv_requestvote_responds_without_granting
        leader_responds_to_entry_msg_when_entry_is_committed
        leader_retries_appendentries_with_decremented_NextIdx_log_inconsistency
        leader_sends_appendentries_when_node_has_next_idx_of_0
        leader_sends_appendentries_with_NextIdx_when_PrevIdx_gt_NextIdx
        leader_sends_appendentries_with_leader_commit
        leader_sends_appendentries_with_prevLogIdx
        leader_sends_empty_appendentries_every_request_timeout
        leader_when_becomes_leader_all_nodes_have_nextidx_equal_to_lastlog_idx_plus_1
        leader_when_it_becomes_a_leader_sends_empty_appendentries

        non_leader_recv_entry_msg_fails

        recv_requestvote_add_unknown_candidate
        recv_requestvote_candidate_step_down_if_term_is_higher_than_current_term
        recv_requestvote_dont_grant_vote_if_we_didnt_vote_for_this_candidate
        recv_requestvote_fails_if_term_less_than_current_term
        recv_requestvote_reply_true_if_term_greater_than_or_equal_to_current_term
        recv_requestvote_reset_timeout
        recv_requestvote_response_dont_increase_votes_for_me_when_not_granted
        recv_requestvote_response_dont_increase_votes_for_me_when_term_is_not_equal
        recv_requestvote_response_increase_votes_for_me
        recv_requestvote_response_must_be_candidate_to_receive

        server_add_node_makes_non_voting_node_voting
        server_append_entry_is_retrievable
        server_apply_entry_increments_last_applied_idx
        server_cfg_sets_num_nodes
        server_currentterm_defaults_to_zero
        server_election_start_increments_term
        server_election_timeout_does_no_promote_us_to_leader_if_there_is_only_1_node
        server_idx_starts_at_one
        server_increment_lastApplied_when_lastApplied_lt_commitidx

        server_periodic_elapses_election_timeout
        server_periodic_should_trigger_snapshotting
        server_periodic_executes_all_cfg_changes

        server_should_apply_each_log_when_receiving_a_snapshot
        server_should_merge_snaphot_and_existing_log_when_receiving_a_snapshot
        server_should_fire_node_callbacks_on_config_change


        server_recv_entry_adds_missing_node_on_addnode
        server_recv_entry_added_node_should_be_nonvoting
        server_recv_entry_auto_commits_if_we_are_the_only_node
        server_recv_entry_fails_if_there_is_already_a_voting_change
        server_recv_entry_removes_node_on_removenode
        server_added_node_should_become_voting_once_it_caught_up
        server_remove_node
        server_set_currentterm_sets_term
        server_set_state

        server_should_not_request_vote_from_failed_nodes
        server_should_not_consider_failed_nodes_when_deciding_vote_outcome
        server_should_call_persist_callback_for_each_appended_log
        server_should_call_delete_callback_for_each_deleted_log

        server_starts_as_follower
        server_starts_with_election_timeout_of_6000m
        server_starts_with_request_timeout_of_1000ms
        server_voted_for_records_who_we_voted_for
        server_votes_are_majority_is_true
        server_voting_results_in_voting
        server_wont_apply_entry_if_there_isnt_a_majority
        server_wont_apply_entry_if_we_dont_have_entry_to_apply

        // joint consensus
        server_should_use_old_and_new_config_during_intermittend_elections
        server_should_revert_to_follower_state_on_config_change_removal
        server_should_use_old_and_new_config_during_intermittend_appendentries
        server_should_send_appendentries_to_all_servers_in_joint_consensus
        server_should_send_requestvote_to_all_servers_in_joint_consensus

        should_call_state_changed_callback_on_state_change
        should_call_node_updated_callback_on_node_udpated

        shouldgrantvote_alredy_voted
        shouldgrantvote_log_empty
        shouldgrantvote_raft_last_log_valid
        shouldgrantvote_raft_log_term_smaller_vote_logterm
        shouldgrantvote_vote_term_too_small

        ///////////////////////////////////////////////////
        //  ____                            _            //
        // / ___|  ___ ___ _ __   __ _ _ __(_) ___  ___  //
        // \___ \ / __/ _ \ '_ \ / _` | '__| |/ _ \/ __| //
        //  ___) | (_|  __/ | | | (_| | |  | | (_) \__ \ //
        // |____/ \___\___|_| |_|\__,_|_|  |_|\___/|___/ //
        ///////////////////////////////////////////////////

        scenario_leader_appears

        // ////////////////////////////////
        // // Monad                      //
        // ////////////////////////////////

        test_raft_monad
      ]
