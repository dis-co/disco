namespace Iris.Tests.Raft

open System.Net
open Expecto
open Iris.Core
open Iris.Raft

[<AutoOpen>]
module JointConsensus =

  //      _       _       _      ____
  //     | | ___ (_)_ __ | |_   / ___|___  _ __  ___  ___ _ __  ___ _   _ ___
  //  _  | |/ _ \| | '_ \| __| | |   / _ \| '_ \/ __|/ _ \ '_ \/ __| | | / __|
  // | |_| | (_) | | | | | |_  | |__| (_) | | | \__ \  __/ | | \__ \ |_| \__ \
  //  \___/ \___/|_|_| |_|\__|  \____\___/|_| |_|___/\___|_| |_|___/\__,_|___/

  let server_periodic_executes_all_cfg_changes =
    testCase "periodic executes all cfg changes" <| fun _ ->
      let trm = term 1

      let cbs =
        { mkcbs (ref defSM) with
            SendRequestVote = fun _ _ -> Some { Term = trm; Granted = true; Reason = None } }

      let mem1 = Member.create (Id.Create())
      let mem2 = Member.create (Id.Create())

      let log =
        JointConsensus(Id.Create(), index 3, term 0, [| MemberAdded mem2 |],
                Some <| JointConsensus(Id.Create(), index 2, term 0, [| MemberRemoved mem1 |],
                           Some <| JointConsensus(Id.Create(), index 1, term 0, [| MemberAdded mem1 |], None)))

      let getstuff r =
        Map.toList r.Peers
        |> List.map (snd >> Member.getId)
        |> List.sort

      raft {
        do! Raft.becomeLeader ()
        do! Raft.receiveEntry log >>= ignoreM
        let! me = Raft.selfM()

        do! expectM "Should have 1 mems" 1 Raft.numMembers
        do! Raft.periodic 10<ms>
        do! expectM "Should have 2 mems" 2 Raft.numMembers
        do! expectM "Should have correct mems" (List.sort [me.Id; mem2.Id]) getstuff
      }
      |> runWithDefaults
      |> noError

  let server_added_mem_should_become_voting_once_it_caught_up =
    testCase "added mem should become voting once it caught up" <| fun _ ->
      let nid2 = Id.Create()
      let mem = Member.create nid2

      let mkjc term =
        JointConsensus(Id.Create(), index 1, term, [| MemberAdded(mem) |] , None)

      let mkcnf term mems =
        Configuration(Id.Create(), index 1, term, mems , None)

      let ci = ref (index 0)
      let state = Raft.mkRaft (Member.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendAppendEntries = fun _ _ ->
                      Some { Term = term 0; Success = true; CurrentIndex = !ci; FirstIndex = index 1 }
                  } :> IRaftCallbacks

      raft {
        do! Raft.setElectionTimeoutM 1000<ms>
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" (index 0) Raft.commitIndex
        do! expectM "Should have mem count of one" 1 Raft.numMembers
        let! term = Raft.currentTermM ()

        // Add the first entry
        let! idx = Raft.currentIndexM ()
        ci := idx                       // otherwise we get a StaleResponse error
        let! one = Raft.receiveEntry (Log.make term defSM)

        // Add another entry
        let! idx = Raft.currentIndexM ()
        ci := idx
        let! two = Raft.receiveEntry (Log.make term defSM)

        let! r1 = Raft.responseCommitted one
        let! r2 = Raft.responseCommitted two

        do! expectM "'one' should be committed" true (konst r1)
        do! expectM "'two' should be committed" true (konst r2)

        // enter the 2-phase commit for configuration change
        let! idx = Raft.currentIndexM ()
        ci := idx
        let! three = Raft.receiveEntry (mkjc term)
        let! r3 = Raft.responseCommitted three
        do! expectM "'three' should be committed" true (konst r3)

        // call periodic to apply join consensus entry
        do! expectM "Should not be in joint-consensus yet" false Raft.inJointConsensus
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>
        do! expectM "Should be in joint-consensus now" true Raft.inJointConsensus

        do! expectM "Should be non-voting mem for start" false (Raft.getMember nid2 >> Option.get >> Member.isVoting)
        do! expectM "Should be in joining state for start" Joining (Raft.getMember nid2 >> Option.get >> Member.getState)

        // add another regular entry
        let! idx = Raft.currentIndexM ()
        ci := idx
        let! four = Raft.receiveEntry (Log.make term defSM)
        let! r4 = Raft.responseCommitted four
        do! expectM "'four' should not be committed" false (konst r4)

        // and another
        let! idx = Raft.currentIndexM ()
        ci := idx
        let! five  = Raft.receiveEntry (Log.make term defSM)
        let! r5 = Raft.responseCommitted five
        do! expectM "'five' should not be committed" false (konst r5)

        do! expectM "Should still be in joint-consensus" true Raft.inJointConsensus

        // call periodic to ensure these are applied
        let! idx = Raft.currentIndexM ()
        ci := idx + index 1
        do! Raft.periodic 1000<ms>

        // when the server notices that all mems are up-to-date it will atomatically append
        // a Configuration entry to exit the JointConsensus
        do! expectM "Should not be in joint-consensus anymore" false Raft.inJointConsensus
        do! expectM "Should have nothing in ConfigChange" None Raft.lastConfigChange

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
            let nid = Id.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let ci = ref (index 0)
      let trm = ref (term 1)

      let lokk = new System.Object()
      let vote = { Granted = true; Term = !trm; Reason = None }

      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !trm; Success = true; CurrentIndex = !ci; FirstIndex = index 1 }
          } :> IRaftCallbacks

      raft {
        let me = snd mems.[0]
        do! Raft.setSelfM me
        do! Raft.setPeersM (mems |> Map.ofArray)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM !trm
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        do! expectM "Should have $n mems" n Raft.numMembers

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 mems in total

        let! t = Raft.currentTermM ()
        trm := t

        do! expectM "Should use the regular configuration" false Raft.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1 .. (n / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in base configuration" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        let entry =
          mems
          |> Array.take (n / 2)
          |> Array.map snd
          |> Log.calculateChanges peers
          |> Log.mkConfigChange (term 1)

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000<ms>

        let! committed = Raft.responseCommitted response
        do! expectM "Should have committed the config change" true (konst committed)

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration" (n / 2) Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" n Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ \
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    __) |
        // |  __/ |  __/ (__| |_| | (_) | | | |  / __/
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_____|
        //
        // now in joint consensus state, with 2 configurations (old and new)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!trm + term 1)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        trm := t

        // testing with the new configuration (the mems with the lower id values)
        // We only need the votes from 2 more mems out of the old configuration
        // to form a majority.
        for idx in 1 .. ((n / 2) / 2) do
          let nid = fst <| mems.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !trm }

        do! expectM "Should be leader in joint consensus with votes from the new configuration" Leader Raft.getState

        //       _           _   _               _____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ /
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    |_ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/
        //
        // still in joint consensus state

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!trm + term 1)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        trm := t

        // testing with the old configuration (the mems with the higher id
        // values that have been removed with the joint consensus entry)
        for idx in (n / 2) .. (n - 1) do
          let nid = fst mems.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !trm }

        do! expectM "Should be leader in joint consensus with votes from the old configuration" Leader Raft.getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends Configuration entry
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        // when configuration entry is considered committed, joint-consensus is over
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        do! expectM "Should only have half the mems" (n / 2) Raft.numMembers
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange

        //       _           _   _               _  _
        //   ___| | ___  ___| |_(_) ___  _ __   | || |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | || |_
        // |  __/ |  __/ (__| |_| | (_) | | | | |__   _|
        //  \___|_|\___|\___|\__|_|\___/|_| |_|    |_|
        //
        // with the new configuration only (should not work with mems in old config anymore)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!trm + term 1)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        trm := t

        for nid in 1 .. ((n / 2) / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in election with regular configuration" Leader Raft.getState

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new mems
        let entry =
          mems
          |> Array.map snd
          |> Log.calculateChanges peers
          |> Log.mkConfigChange (term 1)

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration 2" n Raft.numPeers
        do! expectM "Should still have correct logical mem count 2" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration 2" (n / 2) Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange 2" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   | ___|
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  |___ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!trm + term 1)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        trm := t

        // should become candidate with the old configuration of 5 mems only
        for nid in 1 .. ((n / 2) / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in election in joint consensus with old configuration" Leader Raft.getState

        //       _           _   _                __
        //   ___| | ___  ___| |_(_) ___  _ __    / /_
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | '_ \
        // |  __/ |  __/ (__| |_| | (_) | | | | | (_) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_|  \___/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!trm + term 1)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        trm := t

        // should become candidate with the new configuration of 10 mems also
        for id in (n / 2) .. (n - 1) do
          let nid = fst mems.[int id]
          let! result = Raft.getMemberM nid
          match result with
            | Some mem ->
              // the mems are not able to vote at first, because they will need
              // to be up to date to do that
              // do! updateMemberM { mem with State = Running; Voting = true }
              do! Raft.receiveVoteResponse nid { vote with Term = !trm }
            | _ -> failwith "Member not found. :("

        do! expectM "Should be leader in election in joint consensus with new configuration" Leader Raft.getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete.                      |___/

        // append Configuration and wait for it to be committed
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        // make sure Configuration is committed
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        do! expectM "Should have all the mems" n Raft.numMembers
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange
      }
      |> runWithCBS cbs
      |> noError

  let server_should_revert_to_follower_state_on_config_change_removal =
    testCase "should revert to follower state on config change removal" <| fun _ ->
      let n = 10                      // we want ten mems overall

      let ci = ref (index 0)
      let trm = ref (term 1)
      let lokk = new System.Object()

      let mems =
        [| for n in 0 .. (n - 1) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let vote = { Granted = true; Term = !trm; Reason = None }

      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !trm; Success = true; CurrentIndex = !ci; FirstIndex = index 1 }
          } :> IRaftCallbacks

      raft {
        let self = snd mems.[0]        //
        do! Raft.setSelfM self

        do! Raft.setPeersM (mems |> Map.ofArray)

        // same as calling becomeCandidate, but w/o the IO
        do! Raft.setTermM !trm
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        do! expectM "Should have be candidate" Candidate Raft.getState
        do! expectM "Should have $n mems" n Raft.numMembers

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 mems in total
        let! t = Raft.currentTermM ()
        trm := t

        do! expectM "Should use the regular configuration" false Raft.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1 .. (n / 2) do
          do! Raft.receiveVoteResponse (fst mems.[int nid]) { vote with Term = !trm }

        do! expectM "Should be leader in base configuration" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! t = Raft.currentTermM ()
        trm := t

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        let entry =
          mems
          |> Array.map snd
          |> Array.skip (n / 2)
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration" (n / 2) Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" n Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be found in joint consensus configuration myself" true (Raft.getMember self.Id >> Option.isSome)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends a Configuration entry
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1001<ms>

        // finalizes the joint-consensus mode
        let! t = Raft.currentTermM ()
        trm := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1001<ms>

        do! expectM "Should only have half one mem (myself)" 1 Raft.numMembers
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange
      }
      |> runWithCBS cbs
      |> noError

  let server_should_send_appendentries_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new System.Object()
      let count = ref 0
      let ci = ref (index 0)
      let trm = ref (term 1)
      let init = Raft.mkRaft (Member.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendAppendEntries = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Success = true; Term = !trm; CurrentIndex = !ci; FirstIndex = index 1 } }
                :> IRaftCallbacks

      let n = 10                       // we want ten mems overall

      let mems =
        [| for n in 1 .. (n - 1) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state
        |> Map.ofArray

      raft {
        let! self = Raft.getSelfM ()
        do! Raft.becomeLeader ()             // increases term!

        let! t = Raft.currentTermM ()
        trm := t

        do! expectM "Should be Leader" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of mems

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          Map.toArray mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000<ms>             // need to call periodic apply entry (add mems for real)
        do! Raft.periodic 1000<ms>             // appendAllEntries now called

        do! expectM "Should still have correct mem count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" 1 Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be in joint consensus configuration" true Raft.inJointConsensus

        let! t = Raft.currentTermM ()
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
      let trm = ref (term 1)
      let init = Raft.mkRaft (Member.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendRequestVote = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Granted = true; Term = !trm; Reason = None } }
                :> IRaftCallbacks

      let n = 10                       // we want ten mems overall

      let mems =
        [| for n in 1 .. (n - 1) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      raft {
        let! self = Raft.getSelfM ()

        do! Raft.becomeLeader ()             // increases term!
        do! expectM "Should have be Leader" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of mems

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange (term 1)

        let! response = Raft.receiveEntry entry

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" 1 Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be in joint consensus configuration" true Raft.inJointConsensus

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        for peer in peers do
          do! Raft.updateMemberM { peer with State = Running; Voting = true }

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
            let nid = Id.Create()
            yield (nid, Member.create nid) |] // create mem in the Raft state

      let self = snd mems.[0]
      let lokk = new System.Object()
      let ci = ref (index 0)
      let trm = ref (term 1)
      let count = ref 0
      let init = Raft.mkRaft self
      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ _ ->
              lock lokk <| fun _ ->
                count := 1 + !count
                Some { Success = true; Term = !trm; CurrentIndex = !ci; FirstIndex = index 1 } }
        :> IRaftCallbacks

      raft {
        do! Raft.setPeersM (mems |> Map.ofArray)
        do! Raft.setStateM Candidate
        do! Raft.setTermM !trm
        do! Raft.becomeLeader ()          // increases term!

        let! t = Raft.currentTermM ()
        trm := t

        do! expectM "Should have be Leader" Leader Raft.getState
        do! expectM "Should have $n mems" n Raft.numMembers

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 mems
        // with mem id's 5 - 9
        let entry =
          mems
          |> Array.map snd
          |> Array.take (n / 2)
          |> Log.calculateChanges peers
          |> Log.mkConfigChange !trm

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! expectM "This count should be correct" ((n - 1) * 2) (!count |> konst)

        let! committed = Raft.responseCommitted response
        do! expectM "should not have been committed" false (konst committed)

        do! Raft.periodic 1000<ms>             // now the new configuration should be committed

        do! expectM "Should still have correct mem count for new configuration" (n / 2) Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" n Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be in joint consensus configuration" true Raft.inJointConsensus

        let! committed = Raft.responseCommitted response
        do! expectM "should have been committed" true (konst committed)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000<ms>

        do! expectM "Should only have half the mems" (n / 2) Raft.numMembers
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = Raft.getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new mems
        let entry =
          mems
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.calculateChanges peers
          |> Log.mkConfigChange (term 1)

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! result = Raft.responseCommitted response
        do! expectM "Should not be committed" false (konst result)

        do! Raft.periodic 1000<ms>

        do! expectM "Should still have correct mem count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical mem count" n Raft.numLogicalPeers
        do! expectM "Should still have correct mem count for old configuration" (n / 2) Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        let! result = Raft.responseCommitted response
        do! expectM "Should be committed" true (konst result)
      }
      |> runWithRaft init cbs
      |> noError
