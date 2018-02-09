(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests.Raft

open System.Net
open Expecto
open Disco.Core
open Disco.Raft

[<AutoOpen>]
module JointConsensus =

  //      _       _       _      ____
  //     | | ___ (_)_ __ | |_   / ___|___  _ __  ___  ___ _ __  ___ _   _ ___
  //  _  | |/ _ \| | '_ \| __| | |   / _ \| '_ \/ __|/ _ \ '_ \/ __| | | / __|
  // | |_| | (_) | | | | | |_  | |__| (_) | | | \__ \  __/ | | \__ \ |_| \__ \
  //  \___/ \___/|_|_| |_|\__|  \____\___/|_| |_|___/\___|_| |_|___/\__,_|___/

  let server_periodic_executes_all_cfg_changes =
    testCase "periodic executes all cfg changes" <| fun _ ->
      let trm = 1<term>

      let response = { Term = trm; Granted = true; Reason = None }

      let mem1 = Member.create (DiscoId.Create())
      let mem2 = Member.create (DiscoId.Create())

      let log =
        JointConsensus(DiscoId.Create(), 3<index>, 0<term>, [| MemberAdded mem2 |],
                Some <| JointConsensus(DiscoId.Create(),2<index>,0<term>, [| MemberRemoved mem1 |],
                           Some <| JointConsensus(DiscoId.Create(),1<index>,0<term>, [| MemberAdded mem1 |], None)))

      let getstuff r =
        Map.toList r.Peers
        |> List.map (snd >> Member.id)
        |> List.sort

      raft {
        do! Raft.becomeLeader ()
        do! Raft.receiveEntry log >>= ignoreM
        let! me = self ()

        do! expectM "Should have 1 mems" 1 RaftState.numMembers
        do! Raft.periodic 10<ms>
        do! expectM "Should have 2 mems" 2 RaftState.numMembers
        do! expectM "Should have correct mems" (List.sort [me.Id; mem2.Id]) getstuff
      }
      |> runWithDefaults
      |> noError

  let server_added_mem_should_become_voting_once_it_caught_up =
    testCase "added mem should become voting once it caught up" <| fun _ ->
      let nid2 = DiscoId.Create()
      let mem = Member.create nid2

      let mkjc term =
        JointConsensus(DiscoId.Create(),1<index>, term, [| MemberAdded(mem) |] , None)

      let mkcnf term mems =
        Configuration(DiscoId.Create(),1<index>, term, mems , None)

      let ci = ref 0<index>
      let state = defaultServer()
      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      let makeResponse() =
        { Term = 0<term>
          Success = true
          CurrentIndex = !ci
          FirstIndex = 1<index> }

      raft {
        do! setElectionTimeout 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" 0<index> RaftState.commitIndex
        do! expectM "Should have mem count of one" 1 RaftState.numMembers
        let! term = currentTerm ()

        // Add the first entry
        let! idx = currentIndex ()
        ci := idx                       // otherwise we get a StaleResponse error
        let! one = Raft.receiveEntry (Log.make term defSM)

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        // Add another entry
        let! idx = currentIndex ()
        ci := idx
        let! two = Raft.receiveEntry (Log.make term defSM)

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! r1 = Raft.responseCommitted one
        let! r2 = Raft.responseCommitted two

        do! expectM "'one' should be committed" true (konst r1)
        do! expectM "'two' should be committed" true (konst r2)

        // enter the 2-phase commit for configuration change
        let! idx = currentIndex ()
        ci := idx
        let! three = Raft.receiveEntry (mkjc term)

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! r3 = Raft.responseCommitted three
        do! expectM "'three' should be committed" true (konst r3)

        // call periodic to apply join consensus entry
        do! expectM "Should not be in joint-consensus yet" false RaftState.inJointConsensus
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        do! expectM "Should be in joint-consensus now" true RaftState.inJointConsensus

        do! expectM "Should be non-voting mem for start" false (RaftState.getMember nid2 >> Option.get >> Member.isVoting)
        do! expectM "Should be in joining state for start" Joining (RaftState.getMember nid2 >> Option.get >> Member.status)

        // add another regular entry
        let! idx = currentIndex ()
        ci := idx
        let! four = Raft.receiveEntry (Log.make term defSM)

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! r4 = Raft.responseCommitted four
        do! expectM "'four' should not be committed" false (konst r4)

        // and another
        let! idx = currentIndex ()
        ci := idx
        let! five  = Raft.receiveEntry (Log.make term defSM)

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! r5 = Raft.responseCommitted five
        do! expectM "'five' should not be committed" false (konst r5)

        do! expectM "Should still be in joint-consensus" true RaftState.inJointConsensus

        // call periodic to ensure these are applied
        let! idx = currentIndex ()
        ci := idx + 1<index>
        do! Raft.periodic 1000<ms>

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        // when the server notices that all mems are up-to-date it will atomatically append
        // a Configuration entry to exit the JointConsensus
        do! expectM "Should not be in joint-consensus anymore" false RaftState.inJointConsensus
        do! expectM "Should have nothing in ConfigChange" None RaftState.configChangeEntry

        let! r6 = Raft.responseCommitted three
        let! r7 = Raft.responseCommitted four
        let! r8 = Raft.responseCommitted five

        do! expectM "'three' should be committed" true (konst r6)
        do! expectM "'four' should be committed"  true (konst r7)
        do! expectM "'five' should be committed"  true (konst r8)
      }
      |> runWithRaft state cbs
      |> noError

  open System.Text.RegularExpressions

  let server_should_use_old_and_new_config_during_intermittend_elections =
    testCase "should use old and new config during intermittend elections" <| fun _ ->
      let n = 10                      // we want ten mems overall

      let mems =
        [| for n in 0 .. (n - 1) do      // subtract one for the implicitly
            let nid = DiscoId.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let ci = ref 0<index>
      let trm = ref 1<term>

      let lokk = new System.Object()
      let vote = { Granted = true; Term = !trm; Reason = None }

      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      let makeResponse() =
        { Term = !trm
          Success = true
          CurrentIndex = !ci
          FirstIndex = 1<index> }

      raft {
        let me = snd mems.[0]
        do! setSelf me
        do! setPeers (mems |> Map.ofArray)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm !trm
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        do! expectM "Should have $n mems" n RaftState.numMembers

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 mems in total

        let! t = currentTerm ()
        trm := t

        do! expectM "Should use the regular configuration" false RaftState.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1 .. (n / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in base configuration" Leader RaftState.state

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        let entry =
          mems
          |> Array.take (n / 2)
          |> Array.map snd
          |> Log.calculateChanges peers
          |> Log.mkConfigChange 1<term>

        let! idx = currentIndex ()
        ci := idx

        let! response = Raft.receiveEntry entry
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! committed = Raft.responseCommitted response
        do! expectM "Should have committed the config change" true (konst committed)

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "(1) Should still have correct mem count for new configuration" (n / 2) RaftState.numMembers
        do! expectM "(1) Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "(1) Should still have correct mem count for old configuration" n RaftState.numOldMembers
        do! expectM "(1) Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ \
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    __) |
        // |  __/ |  __/ (__| |_| | (_) | | | |  / __/
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_____|
        //
        // now in joint consensus state, with 2 configurations (old and new)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm (!trm + 1<term>)
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        let! t = currentTerm ()
        trm := t

        // testing with the new configuration (the mems with the lower id values)
        // We only need the votes from 2 more mems out of the old configuration
        // to form a majority.
        for idx in 1 .. ((n / 2) / 2) do
          let nid = fst <| mems.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !trm }

        do! expectM "Should be leader in joint consensus with votes from the new configuration" Leader RaftState.state

        //       _           _   _               _____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ /
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    |_ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/
        //
        // still in joint consensus state

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm (!trm + 1<term>)
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        let! t = currentTerm ()
        trm := t

        // testing with the old configuration (the mems with the higher id
        // values that have been removed with the joint consensus entry)
        for idx in (n / 2) .. (n - 1) do
          let nid = fst mems.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !trm }

        do! expectM "Should be leader in joint consensus with votes from the old configuration" Leader RaftState.state

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends Configuration entry
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        // when configuration entry is considered committed, joint-consensus is over
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1000<ms>
        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should only have half the mems" (n / 2) RaftState.numMembers
        do! expectM "Should have None as ConfigChange" None RaftState.configChangeEntry

        //       _           _   _               _  _
        //   ___| | ___  ___| |_(_) ___  _ __   | || |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | || |_
        // |  __/ |  __/ (__| |_| | (_) | | | | |__   _|
        //  \___|_|\___|\___|\__|_|\___/|_| |_|    |_|
        //
        // with the new configuration only (should not work with mems in old config anymore)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm (!trm + 1<term>)
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        let! t = currentTerm ()
        trm := t

        for nid in 1 .. ((n / 2) / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in election with regular configuration" Leader RaftState.state

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new mems
        let entry =
          mems
          |> Array.map snd
          |> Log.calculateChanges peers
          |> Log.mkConfigChange 1<term>

        let! idx = currentIndex ()
        ci := idx

        let! response = Raft.receiveEntry entry
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "(2) Should still have correct mem count for new configuration 2" n RaftState.numMembers
        do! expectM "(2) Should still have correct logical mem count 2" n RaftState.numLogicalPeers
        do! expectM "(2) Should still have correct mem count for old configuration 2" (n / 2) RaftState.numOldMembers
        do! expectM "(2) Should have JointConsensus entry as ConfigChange 2" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   | ___|
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  |___ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm (!trm + 1<term>)
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        let! t = currentTerm ()
        trm := t

        // should become candidate with the old configuration of 5 mems only
        for nid in 1 .. ((n / 2) / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in election in joint consensus with old configuration" Leader RaftState.state

        //       _           _   _                __
        //   ___| | ___  ___| |_(_) ___  _ __    / /_
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | '_ \
        // |  __/ |  __/ (__| |_| | (_) | | | | | (_) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_|  \___/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setCurrentTerm (!trm + 1<term>)
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        let! t = currentTerm ()
        trm := t

        // should become candidate with the new configuration of 10 mems also
        for id in (n / 2) .. (n - 1) do
          let nid = fst mems.[int id]
          let! result = getMember nid
          match result with
            | Some mem ->
              // the mems are not able to vote at first, because they will need
              // to be up to date to do that
              do! updateMember { mem with Status = Running; Voting = true }
              do! Raft.receiveVoteResponse nid { vote with Term = !trm }
            | _ -> failwith "Member not found. :("

        do! expectM "Should be leader in election in joint consensus with new configuration" Leader RaftState.state

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete.                      |___/

        // append Configuration and wait for it to be committed
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        // make sure Configuration is committed
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should have all the mems" n RaftState.numMembers
        do! expectM "Should have None as ConfigChange" None RaftState.configChangeEntry
      }
      |> runWithCBS cbs
      |> noError

  let server_should_revert_to_follower_state_on_config_change_removal =
    testCase "should revert to follower state on config change removal" <| fun _ ->
      let n = 10                      // we want ten mems overall

      let ci = ref 0<index>
      let trm = ref 1<term>
      let lokk = new System.Object()

      let mems =
        [| for n in 0 .. (n - 1) do      // subtract one for the implicitly
            let nid = DiscoId.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let vote = { Granted = true; Term = !trm; Reason = None }

      let cbs = Callbacks.Create (ref defSM) :> IRaftCallbacks

      let makeResponse() =
        { Term = !trm
          Success = true
          CurrentIndex = !ci
          FirstIndex = 1<index> }

      raft {
        let self = snd mems.[0]        //
        do! setSelf self

        do! setPeers (mems |> Map.ofArray)

        // same as calling becomeCandidate, but w/o the IO
        do! setCurrentTerm !trm
        do! resetVotes ()
        do! voteForMyself ()
        do! setLeader None
        do! setState Candidate

        do! expectM "Should have be candidate" Candidate RaftState.state
        do! expectM "Should have $n mems" n RaftState.numMembers

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 mems in total
        let! t = currentTerm ()
        trm := t

        do! expectM "Should use the regular configuration" false RaftState.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1 .. (n / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in base configuration" Leader RaftState.state

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! t = currentTerm ()
        trm := t

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        let entry =
          mems
          |> Array.map snd
          |> Array.skip (n / 2)
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should still have correct mem count for new configuration" (n / 2) RaftState.numMembers
        do! expectM "Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" n RaftState.numOldMembers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)
        do! expectM "Should be found in joint consensus configuration myself" true (RaftState.getMember self.Id >> Option.isSome)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends a Configuration entry
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1001<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        // finalizes the joint-consensus mode
        let! t = currentTerm ()
        trm := t
        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1001<ms>
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1001<ms>
        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        for peer in peers do
          do! makeResponse() |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should only have half one mem (myself)" 1 RaftState.numMembers
        do! expectM "Should have None as ConfigChange" None RaftState.configChangeEntry
      }
      |> runWithCBS cbs
      |> noError

  let server_should_send_appendentries_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new System.Object()
      let count = ref 0
      let ci = ref 0<index>
      let trm = ref 1<term>
      let init = defaultServer()
      let cbs = { Callbacks.Create (ref defSM)
                    with SendAppendEntries = fun _ _ -> lock lokk <| fun _ -> count := 1 + !count }
                :> IRaftCallbacks

      // let response = Some { Success = true; Term = !trm; CurrentIndex = !ci; FirstIndex = 1<index> } }

      let n = 10                       // we want ten mems overall

      let mems =
        [| for n in 1 .. (n - 1) do      // subtract one for the implicitly
            let nid = DiscoId.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state
        |> Map.ofArray

      raft {
        let! self = self ()
        do! Raft.becomeLeader ()             // increases term!

        let! t = currentTerm ()
        trm := t

        do! expectM "Should be Leader" Leader RaftState.state

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of mems

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          Map.toArray mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry

        let! idx = currentIndex ()
        ci := idx

        do! Raft.periodic 1000<ms>             // need to call periodic apply entry (add mems for real)
        do! Raft.periodic 1000<ms>             // appendAllEntries now called

        do! expectM "Should still have correct mem count for new configuration" n RaftState.numMembers
        do! expectM "Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" 1 RaftState.numOldMembers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)
        do! expectM "Should be in joint consensus configuration" true RaftState.inJointConsensus

        let! t = currentTerm ()
        trm := t

        let! response = Raft.receiveEntry (Log.make !trm defSM)
        let! committed = Raft.responseCommitted response
        do! expectM "Should not be committed" false (konst committed)

        do! expectM "Count should be n" ((n - 1) * 2) (!count |> konst)
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_send_requestvote_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new System.Object()
      let count = ref 0
      let trm = ref 1<term>
      let init = defaultServer()
      let cbs = { Callbacks.Create (ref defSM) with
                    SendRequestVote = fun _ _ -> lock lokk <| fun _ -> count := 1 + !count }
                :> IRaftCallbacks

      // let response = Some { Granted = true; Term = !trm; Reason = None } }

      let n = 10                       // we want ten mems overall

      let mems =
        [| for n in 1 .. (n - 1) do      // subtract one for the implicitly
            let nid = DiscoId.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      raft {
        let! self = self ()

        do! Raft.becomeLeader ()             // increases term!
        do! expectM "Should have be Leader" Leader RaftState.state

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of mems

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange 1<term>

        let! response = Raft.receiveEntry entry

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration" n RaftState.numMembers
        do! expectM "Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" 1 RaftState.numOldMembers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)
        do! expectM "Should be in joint consensus configuration" true RaftState.inJointConsensus

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        for peer in peers do
          do! updateMember { peer with Status = Running; Voting = true }

        do! Raft.startElection ()

        expect "Count should be n" (n - 1) id !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_use_old_and_new_config_during_intermittend_appendentries =
    testCase "should use old and new config during intermittend appendentries" <| fun _ ->
      let n = 10                       // we want ten mems overall

      let mems =
        [| for n in 0 .. (n - 1) do      // subtract one for the implicitly
            let nid = DiscoId.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let self = snd mems.[0]
      let lokk = new System.Object()
      let ci = ref 0<index>
      let trm = ref 1<term>
      let count = ref 0
      let init = RaftState.create self
      let cbs =
        { Callbacks.Create (ref defSM) with
            SendAppendEntries = fun _ _ -> lock lokk <| fun _ -> count := 1 + !count }
        :> IRaftCallbacks

      let makeResponse() =
        { Success = true
          Term = !trm
          CurrentIndex = !ci
          FirstIndex = 1<index> }

      raft {
        do! setPeers (mems |> Map.ofArray)
        do! setState Candidate
        do! setCurrentTerm !trm

        do! Raft.becomeLeader ()          // increases term!

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        let! t = currentTerm ()
        trm := t

        do! expectM "Should have be Leader" Leader RaftState.state
        do! expectM "Should have $n mems" n RaftState.numMembers

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          mems
          |> Array.map snd
          |> Array.take (n / 2)
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry

        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx

        do! expectM "This count should be correct" ((n - 1) * 2) (!count |> konst)

        let! committed = Raft.responseCommitted response
        do! expectM "should not have been committed" false (konst committed)

        // this periodic call will mark the config change as done
        // and append a JointConsensus entry
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        // now the new configuration should be committed, since the previously
        // apppended JointConsensus is now also marked comitted
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "(1) Should still have correct mem count for new configuration" (n / 2) RaftState.numMembers
        do! expectM "(1) Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "(1) Should still have correct mem count for old configuration" n RaftState.numOldMembers
        do! expectM "(1) Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)
        do! expectM "(1) Should be in joint consensus configuration" true RaftState.inJointConsensus

        let! committed = Raft.responseCommitted response
        do! expectM "should have been committed" true (konst committed)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        // after this periodic, the new cluster configuraion is applied
        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)
        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "(2) Should only have half the mems" (n / 2) RaftState.numMembers
        do! expectM "(2) Should have None as ConfigChange" None RaftState.configChangeEntry

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = getMembers () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new mems
        let entry =
          mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange 1<term>


        let! response = Raft.receiveEntry entry
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        let! idx = currentIndex ()
        ci := idx

        let! result = Raft.responseCommitted response
        do! expectM "Should not be committed" false (konst result)

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        do! Raft.periodic 1000<ms>
        for peer in peers do
          do! makeResponse () |> Raft.receiveAppendEntriesResponse peer.Id

        do! expectM "Should still have correct mem count for new configuration" n RaftState.numMembers
        do! expectM "Should still have correct logical mem count" n RaftState.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" (n / 2) RaftState.numOldMembers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.id entry) (RaftState.configChangeEntry >> Option.get >> LogEntry.id)

        let! result = Raft.responseCommitted response
        do! expectM "Should be committed" true (konst result)
      }
      |> runWithRaft init cbs
      |> noError
