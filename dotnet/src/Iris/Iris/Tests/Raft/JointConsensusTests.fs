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
      let term = 1u

      let cbs =
        { mkcbs (ref defSM) with
            SendRequestVote = fun _ _ -> Some { Term = term; Granted = true; Reason = None } }

      let node1 = Node.create (Id.Create())
      let node2 = Node.create (Id.Create())

      let log =
        JointConsensus(Id.Create(), 3u, 0u, [| NodeAdded node2 |],
                Some <| JointConsensus(Id.Create(), 2u, 0u, [| NodeRemoved node1 |],
                           Some <| JointConsensus(Id.Create(), 1u, 0u, [| NodeAdded node1 |], None)))

      let getstuff r =
        Map.toList r.Peers
        |> List.map (snd >> Node.getId)
        |> List.sort

      raft {
        do! Raft.becomeLeader ()
        do! Raft.receiveEntry log >>= ignoreM
        let! me = Raft.selfM()

        do! expectM "Should have 1 nodes" 1u Raft.numNodes
        do! Raft.periodic 10u
        do! expectM "Should have 2 nodes" 2u Raft.numNodes
        do! expectM "Should have correct nodes" (List.sort [me.Id; node2.Id]) getstuff
      }
      |> runWithDefaults
      |> noError

  let server_added_node_should_become_voting_once_it_caught_up =
    testCase "added node should become voting once it caught up" <| fun _ ->
      let nid2 = Id.Create()
      let node = Node.create nid2

      let mkjc term =
        JointConsensus(Id.Create(), 1u, term, [| NodeAdded(node) |] , None)

      let mkcnf term nodes =
        Configuration(Id.Create(), 1u, term, nodes , None)

      let ci = ref 0u
      let state = Raft.mkRaft (Node.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendAppendEntries = fun _ _ ->
                      Some { Term = 0u; Success = true; CurrentIndex = !ci; FirstIndex = 1u }
                  } :> IRaftCallbacks

      raft {
        do! Raft.setElectionTimeoutM 1000u
        do! Raft.becomeLeader ()
        do! expectM "Should have commit idx of zero" 0u Raft.commitIndex
        do! expectM "Should have node count of one" 1u Raft.numNodes
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
        do! Raft.periodic 1000u
        do! expectM "Should be in joint-consensus now" true Raft.inJointConsensus

        do! expectM "Should be non-voting node for start" false (Raft.getNode nid2 >> Option.get >> Node.isVoting)
        do! expectM "Should be in joining state for start" Joining (Raft.getNode nid2 >> Option.get >> Node.getState)

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
        ci := idx + 1u
        do! Raft.periodic 1000u

        // when the server notices that all nodes are up-to-date it will atomatically append
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
      let n = 10u                      // we want ten nodes overall

      let nodes =
        [| for n in 0u .. (n - 1u) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Node.create nid) |] // create node in the Raft state

      let ci = ref 0u
      let term = ref 1u

      let lokk = new System.Object()
      let vote = { Granted = true; Term = !term; Reason = None }

      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !term; Success = true; CurrentIndex = !ci; FirstIndex = 1u }
          } :> IRaftCallbacks

      raft {
        let me = snd nodes.[0]
        do! Raft.setSelfM me
        do! Raft.setPeersM (nodes |> Map.ofArray)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM !term
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        do! expectM "Should have $n nodes" n Raft.numNodes

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 nodes in total

        let! t = Raft.currentTermM ()
        term := t

        do! expectM "Should use the regular configuration" false Raft.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1u .. (n / 2u) do
          do! Raft.receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in base configuration" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        let entry =
          nodes
          |> Array.take (int <| n / 2u)
          |> Array.map (snd >> ConfigChange.NodeRemoved)
          |> Log.mkConfigChange 1u

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000u

        let! committed = Raft.responseCommitted response
        do! expectM "Should have committed the config change" true (konst committed)

        do! Raft.periodic 1000u

        do! expectM "Should still have correct node count for new configuration" (n / 2u) Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ \
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    __) |
        // |  __/ |  __/ (__| |_| | (_) | | | |  / __/
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_____|
        //
        // now in joint consensus state, with 2 configurations (old and new)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!term + 1u)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        term := t

        // testing with the new configuration (the nodes with the lower id values)
        // We only need the votes from 2 more nodes out of the old configuration
        // to form a majority.
        for idx in 1u .. ((n / 2u) / 2u) do
          let nid = fst <| nodes.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !term }

        do! expectM "Should be leader in joint consensus with votes from the new configuration" Leader Raft.getState

        //       _           _   _               _____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ /
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    |_ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/
        //
        // still in joint consensus state

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!term + 1u)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        term := t

        // testing with the old configuration (the nodes with the higher id
        // values that have been removed with the joint consensus entry)
        for idx in (n / 2u) .. (n - 1u) do
          let nid = fst nodes.[int idx]
          do! Raft.receiveVoteResponse nid { vote with Term = !term }

        do! expectM "Should be leader in joint consensus with votes from the old configuration" Leader Raft.getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends Configuration entry
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000u

        // when configuration entry is considered committed, joint-consensus is over
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000u

        do! expectM "Should only have half the nodes" (n / 2u) Raft.numNodes
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange

        //       _           _   _               _  _
        //   ___| | ___  ___| |_(_) ___  _ __   | || |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | || |_
        // |  __/ |  __/ (__| |_| | (_) | | | | |__   _|
        //  \___|_|\___|\___|\__|_|\___/|_| |_|    |_|
        //
        // with the new configuration only (should not work with nodes in old config anymore)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!term + 1u)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        term := t

        for nid in 1u .. ((n / 2u) / 2u) do
          do! Raft.receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in election with regular configuration" Leader Raft.getState

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new nodes
        let entry =
          nodes
          |> Array.map (snd >> ConfigChange.NodeAdded)
          |> Log.mkConfigChange 1u

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000u

        do! expectM "Should still have correct node count for new configuration 2" n Raft.numPeers
        do! expectM "Should still have correct logical node count 2" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration 2" (n / 2u) Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange 2" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   | ___|
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  |___ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!term + 1u)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        term := t

        // should become candidate with the old configuration of 5 nodes only
        for nid in 1u .. ((n / 2u) / 2u) do
          do! Raft.receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in election in joint consensus with old configuration" Leader Raft.getState

        //       _           _   _                __
        //   ___| | ___  ___| |_(_) ___  _ __    / /_
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | '_ \
        // |  __/ |  __/ (__| |_| | (_) | | | | | (_) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_|  \___/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! Raft.setTermM (!term + 1u)
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        let! t = Raft.currentTermM ()
        term := t

        // should become candidate with the new configuration of 10 nodes also
        for id in (n / 2u) .. (n - 1u) do
          let nid = fst nodes.[int id]
          let! result = Raft.getNodeM nid
          match result with
            | Some node ->
              // the nodes are not able to vote at first, because they will need
              // to be up to date to do that
              // do! updateNodeM { node with State = Running; Voting = true }
              do! Raft.receiveVoteResponse nid { vote with Term = !term }
            | _ -> failwith "Node not found. :("

        do! expectM "Should be leader in election in joint consensus with new configuration" Leader Raft.getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete.                      |___/

        // append Configuration and wait for it to be committed
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000u

        // make sure Configuration is committed
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000u

        do! expectM "Should have all the nodes" n Raft.numNodes
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange
      }
      |> runWithCBS cbs
      |> noError

  let server_should_revert_to_follower_state_on_config_change_removal =
    testCase "should revert to follower state on config change removal" <| fun _ ->
      let n = 10u                      // we want ten nodes overall

      let ci = ref 0u
      let term = ref 1u
      let lokk = new System.Object()

      let nodes =
        [| for n in 0u .. (n - 1u) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Node.create nid) |] // create node in the Raft state

      let vote = { Granted = true; Term = !term; Reason = None }

      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !term; Success = true; CurrentIndex = !ci; FirstIndex = 1u }
          } :> IRaftCallbacks

      raft {
        let self = snd nodes.[0]        //
        do! Raft.setSelfM self

        do! Raft.setPeersM (nodes |> Map.ofArray)

        // same as calling becomeCandidate, but w/o the IO
        do! Raft.setTermM !term
        do! Raft.resetVotesM ()
        do! Raft.voteForMyself ()
        do! Raft.setLeaderM None
        do! Raft.setStateM Candidate

        do! expectM "Should have be candidate" Candidate Raft.getState
        do! expectM "Should have $n nodes" n Raft.numNodes

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 nodes in total
        let! t = Raft.currentTermM ()
        term := t

        do! expectM "Should use the regular configuration" false Raft.inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1u .. (n / 2u) do
          do! Raft.receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in base configuration" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! t = Raft.currentTermM ()
        term := t

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        let entry =
          nodes
          |> Array.map (snd >> ConfigChange.NodeRemoved)
          |> Array.skip (int <| n / 2u)
          |> Log.mkConfigChange !term

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000u

        do! expectM "Should still have correct node count for new configuration" (n / 2u) Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be found in joint consensus configuration myself" true (Raft.getNode self.Id >> Option.isSome)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        // appends a Configuration entry
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1001u

        // finalizes the joint-consensus mode
        let! t = Raft.currentTermM ()
        term := t
        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1001u

        do! expectM "Should only have half one node (myself)" 1u Raft.numNodes
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange
      }
      |> runWithCBS cbs
      |> noError

  let server_should_send_appendentries_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new System.Object()
      let count = ref 0
      let ci = ref 0u
      let term = ref 1u
      let init = Raft.mkRaft (Node.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendAppendEntries = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Success = true; Term = !term; CurrentIndex = !ci; FirstIndex = 1u } }
                :> IRaftCallbacks

      let n = 10u                       // we want ten nodes overall

      let nodes =
        [| for n in 1u .. (n - 1u) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Node.create nid) |] // create node in the Raft state
        |> Map.ofArray

      raft {
        let! self = Raft.getSelfM ()
        do! Raft.becomeLeader ()             // increases term!

        let! t = Raft.currentTermM ()
        term := t

        do! expectM "Should be Leader" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of nodes

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          Map.toArray nodes
          |> Array.map (snd >> ConfigChange.NodeAdded)
          |> Array.append [| ConfigChange.NodeAdded self |]
          |> Log.mkConfigChange !term

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! Raft.periodic 1000u             // need to call periodic apply entry (add nodes for real)
        do! Raft.periodic 1000u             // appendAllEntries now called

        do! expectM "Should still have correct node count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" 1u Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be in joint consensus configuration" true Raft.inJointConsensus

        let! t = Raft.currentTermM ()
        term := t

        let! response = Raft.receiveEntry (Log.make !term defSM)
        let! committed = Raft.responseCommitted response
        do! expectM "Should not be committed" false (konst committed)

        do! expectM "Count should be n" ((n - 1u) * 2u) (uint32 !count |> konst)
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_send_requestvote_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new System.Object()
      let count = ref 0
      let term = ref 1u
      let init = Raft.mkRaft (Node.create (Id.Create()))
      let cbs = { mkcbs (ref defSM) with
                    SendRequestVote = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Granted = true; Term = !term; Reason = None } }
                :> IRaftCallbacks

      let n = 10u                       // we want ten nodes overall

      let nodes =
        [| for n in 1u .. (n - 1u) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Node.create nid) |] // create node in the Raft state

      raft {
        let! self = Raft.getSelfM ()

        do! Raft.becomeLeader ()             // increases term!
        do! expectM "Should have be Leader" Leader Raft.getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of nodes

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          nodes
          |> Array.map (snd >> ConfigChange.NodeAdded)
          |> Array.append [| ConfigChange.NodeAdded self |]
          |> Log.mkConfigChange 1u

        let! response = Raft.receiveEntry entry

        do! Raft.periodic 1000u

        do! expectM "Should still have correct node count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" 1u Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)
        do! expectM "Should be in joint consensus configuration" true Raft.inJointConsensus

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        for peer in peers do
          do! Raft.updateNodeM { peer with State = Running; Voting = true }

        do! Raft.startElection ()

        expect "Count should be n" (n - 1u) uint32 !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_use_old_and_new_config_during_intermittend_appendentries =
    testCase "should use old and new config during intermittend appendentries" <| fun _ ->
      let n = 10u                       // we want ten nodes overall

      let nodes =
        [| for n in 0u .. (n - 1u) do      // subtract one for the implicitly
            let nid = Id.Create()
            yield (nid, Node.create nid) |] // create node in the Raft state

      let self = snd nodes.[0]
      let lokk = new System.Object()
      let ci = ref 0u
      let term = ref 1u
      let count = ref 0
      let init = Raft.mkRaft self
      let cbs =
        { mkcbs (ref defSM) with
            SendAppendEntries = fun _ _ ->
              lock lokk <| fun _ ->
                count := 1 + !count
                Some { Success = true; Term = !term; CurrentIndex = !ci; FirstIndex = 1u } }
        :> IRaftCallbacks

      raft {
        do! Raft.setPeersM (nodes |> Map.ofArray)
        do! Raft.setStateM Candidate
        do! Raft.setTermM !term
        do! Raft.becomeLeader ()          // increases term!

        let! t = Raft.currentTermM ()
        term := t

        do! expectM "Should have be Leader" Leader Raft.getState
        do! expectM "Should have $n nodes" n Raft.numNodes

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          nodes
          |> Array.map (snd >> ConfigChange.NodeRemoved)
          |> Array.take (int <| n / 2u)
          |> Log.mkConfigChange !term

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        do! expectM "This count should be correct" ((n - 1u) * 2u) (uint32 !count |> konst)

        let! committed = Raft.responseCommitted response
        do! expectM "should not have been committed" false (konst committed)

        do! Raft.periodic 1000u             // now the new configuration should be committed

        do! expectM "Should still have correct node count for new configuration" (n / 2u) Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n Raft.numOldPeers
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
        do! Raft.periodic 1000u

        let! idx = Raft.currentIndexM ()
        ci := idx
        do! Raft.periodic 1000u

        do! expectM "Should only have half the nodes" (n / 2u) Raft.numNodes
        do! expectM "Should have None as ConfigChange" None Raft.lastConfigChange

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = Raft.getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new nodes
        let entry =
          nodes
          |> Array.map (snd >> ConfigChange.NodeAdded)
          |> Array.append [| ConfigChange.NodeAdded self |]
          |> Log.mkConfigChange 1u 

        let! response = Raft.receiveEntry entry

        let! idx = Raft.currentIndexM ()
        ci := idx

        let! result = Raft.responseCommitted response
        do! expectM "Should not be committed" false (konst result)

        do! Raft.periodic 1000u

        do! expectM "Should still have correct node count for new configuration" n Raft.numPeers
        do! expectM "Should still have correct logical node count" n Raft.numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" (n / 2u) Raft.numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (LogEntry.getId entry) (Raft.lastConfigChange >> Option.get >> LogEntry.getId)

        let! result = Raft.responseCommitted response
        do! expectM "Should be committed" true (konst result)
      }
      |> runWithRaft init cbs
      |> noError
