namespace Pallet.Tests

open System
open System.Net
open Fuchu
open Fuchu.Test
open Pallet.Core

[<AutoOpen>]
module JointConsensus =

  //      _       _       _      ____
  //     | | ___ (_)_ __ | |_   / ___|___  _ __  ___  ___ _ __  ___ _   _ ___
  //  _  | |/ _ \| | '_ \| __| | |   / _ \| '_ \/ __|/ _ \ '_ \/ __| | | / __|
  // | |_| | (_) | | | | | |_  | |__| (_) | | | \__ \  __/ | | \__ \ |_| \__ \
  //  \___/ \___/|_|_| |_|\__|  \____\___/|_| |_|___/\___|_| |_|___/\__,_|___/

  let server_periodic_executes_all_cfg_changes =
    testCase "periodic executes all cfg changes" <| fun _ ->
      let term = 1UL

      let cbs =
        { mkcbs (ref ()) with
            SendRequestVote = fun _ _ -> Some { Term = term; Granted = true; Reason = None } }

      let node1 = Node.create (RaftId.Create()) ()
      let node2 = Node.create (RaftId.Create()) ()

      let log =
        JointConsensus(RaftId.Create(), 3UL, 0UL, [| NodeAdded node2 |],
                Some <| JointConsensus(RaftId.Create(), 2UL, 0UL, [| NodeRemoved node1 |],
                           Some <| JointConsensus(RaftId.Create(), 1UL, 0UL, [| NodeAdded node1 |], None)))

      let getstuff r =
        Map.toList r.Peers
        |> List.map (snd >> Node.getId)
        |> List.sort

      raft {
        do! becomeLeader ()
        do! receiveEntry log >>= ignoreM
        let! me = selfM()

        do! expectM "Should have 1 nodes" 1UL numNodes
        do! periodic 10UL
        do! expectM "Should have 2 nodes" 2UL numNodes
        do! expectM "Should have correct nodes" (List.sort [me.Id; node2.Id]) getstuff
      }
      |> runWithDefaults
      |> noError

  let server_added_node_should_become_voting_once_it_caught_up =
    testCase "added node should become voting once it caught up" <| fun _ ->
      let nid2 = RaftId.Create()
      let node = Node.create nid2 ()

      let mkjc term =
        JointConsensus(RaftId.Create(), 1UL, term, [| NodeAdded(node) |] , None)

      let mkcnf term nodes =
        Configuration(RaftId.Create(), 1UL, term, nodes , None)

      let ci = ref 0UL
      let state = Raft.create (Node.create (RaftId.Create()) ())
      let count = ref 0
      let cbs = { mkcbs (ref ()) with
                    SendAppendEntries = fun _ _ -> Some { Term = 0UL; Success = true; CurrentIndex = !ci; FirstIndex = 1UL }
                    HasSufficientLogs = fun _   -> count := 1 + !count }
                  :> IRaftCallbacks<_,_>

      raft {
        do! setElectionTimeoutM 1000UL
        do! becomeLeader ()
        do! expectM "Should have commit idx of zero" 0UL commitIndex
        do! expectM "Should have node count of one" 1UL numNodes
        let! term = currentTermM ()

        // Add the first entry
        let! idx = currentIndexM ()
        ci := idx                       // otherwise we get a StaleResponse error
        let! one = receiveEntry (Log.make term ())

        // Add another entry
        let! idx = currentIndexM ()
        ci := idx
        let! two = receiveEntry (Log.make term ())

        let! r1 = responseCommitted one
        let! r2 = responseCommitted two

        do! expectM "'one' should be committed" true (konst r1)
        do! expectM "'two' should be committed" true (konst r2)

        // enter the 2-phase commit for configuration change
        let! idx = currentIndexM ()
        ci := idx
        let! three = receiveEntry (mkjc term)
        let! r3 = responseCommitted three
        do! expectM "'three' should be committed" true (konst r3)

        // call periodic to apply join consensus entry
        do! expectM "Should not be in joint-consensus yet" false inJointConsensus
        let! idx = currentIndexM ()
        ci := idx
        do! periodic 1000UL
        do! expectM "Should be in joint-consensus now" true inJointConsensus

        // add another regular entry
        let! idx = currentIndexM ()
        ci := idx
        let! four = receiveEntry (Log.make term ())
        let! r4 = responseCommitted four
        do! expectM "'four' should not be committed" false (konst r4)

        // and another
        let! idx = currentIndexM ()
        ci := idx
        let! five  = receiveEntry (Log.make term ())
        let! r5 = responseCommitted five
        do! expectM "'five' should not be committed" false (konst r5)

        do! expectM "Should be non-voting node for start" false (getNode nid2 >> Option.get >> Node.isVoting)
        do! expectM "Should be in joining state for start" Joining (getNode nid2 >> Option.get >> Node.getState)

        // call periodic to ensure these are applied
        let! idx = currentIndexM ()
        ci := idx + 1UL
        do! periodic 1000UL

        let! r6 = responseCommitted three
        let! r7 = responseCommitted four
        let! r8 = responseCommitted five

        do! expectM "'three' should be committed" true (konst r6)
        do! expectM "'four' should be committed"  true (konst r7)
        do! expectM "'five' should be committed"  true (konst r8)

        // call periodic again to ensure that all are committed and applied
        let! idx = currentIndexM ()
        ci := idx + 2UL
        do! periodic 1000UL

        // since all are applied we append the configuration change to end joint consensus
        let! idx = currentIndexM ()
        ci := idx + 3UL
        let! term = currentTermM ()
        let! nodes = getNodesM () >>= (Map.toArray >> returnM) >>= (Array.map snd >> returnM)
        let! six  = receiveEntry (mkcnf term nodes)
        let! r9 = responseCommitted six
        do! expectM "'six' should be committed"  true (konst r9)

        // the Configuration entry needs to be applied
        let! idx = currentIndexM ()
        ci := idx + 3UL
        do! periodic 1000UL

        // one last time to ensure the node has be updated to Running
        let! idx = currentIndexM ()
        ci := idx + 4UL
        do! periodic 1000UL

        do! expectM "should have called the 'hassufficientlogs' callback" 1 (konst !count)
        do! expectM "Should be voting node now" true (getNode nid2 >> Option.get >> Node.isVoting)
        do! expectM "Should be in running state now" Running (getNode nid2 >> Option.get >> Node.getState)
      }
      |> runWithRaft state cbs
      |> noError

  open System.Text.RegularExpressions

  let server_should_use_old_and_new_config_during_intermittend_elections =
    testCase "should use old and new config during intermittend elections" <| fun _ ->
      let n = 10UL                      // we want ten nodes overall

      let nodes =
        [| for n in 0UL .. (n - 1UL) do      // subtract one for the implicitly
            let nid = RaftId.Create()
            yield (nid, Node.create nid ()) |] // create node in the Raft state

      let ci = ref 0UL
      let term = ref 1UL

      let lokk = new Object()
      let vote = { Granted = true; Term = !term; Reason = None }

      let cbs =
        { mkcbs (ref ()) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !term; Success = true; CurrentIndex = !ci; FirstIndex = 1UL }
          } :> IRaftCallbacks<_,_>

      raft {
        let me = snd nodes.[0]
        do! setSelfM me
        do! setPeersM (nodes |> Map.ofArray)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM !term
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        do! expectM "Should have $n nodes" n numNodes

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 nodes in total

        let! t = currentTermM ()
        term := t

        do! expectM "Should use the regular configuration" false inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1UL .. (n / 2UL) do
          do! receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in base configuration" Leader getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        let entry =
          nodes
          |> Array.take (int <| n / 2UL)
          |> Array.map snd
          |> Log.mkConfigChange 1UL peers

        let! idx = currentIndexM ()
        ci := idx

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        let! committed = responseCommitted response
        do! expectM "Should have committed the config change" true (konst committed)

        do! periodic 1000UL

        do! expectM "Should still have correct node count for new configuration" (n / 2UL) numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ \
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    __) |
        // |  __/ |  __/ (__| |_| | (_) | | | |  / __/
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_____|
        //
        // now in joint consensus state, with 2 configurations (old and new)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM (!term + 1UL)
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        let! t = currentTermM ()
        term := t

        // testing with the new configuration (the nodes with the lower id values)
        // We only need the votes from 2 more nodes out of the old configuration
        // to form a majority.
        for idx in 1UL .. ((n / 2UL) / 2UL) do
          let nid = fst <| nodes.[int idx]
          do! receiveVoteResponse nid { vote with Term = !term }

        do! expectM "Should be leader in joint consensus with votes from the new configuration" Leader getState

        //       _           _   _               _____
        //   ___| | ___  ___| |_(_) ___  _ __   |___ /
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \    |_ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/
        //
        // still in joint consensus state

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM (!term + 1UL)
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        let! t = currentTermM ()
        term := t

        // testing with the old configuration (the nodes with the higher id
        // values that have been removed with the joint consensus entry)
        for idx in (n / 2UL) .. (n - 1UL) do
          let nid = fst nodes.[int idx]
          do! receiveVoteResponse nid { vote with Term = !term }

        do! expectM "Should be leader in joint consensus with votes from the old configuration" Leader getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        let! t = currentTermM ()
        term := t

        let! idx = currentIndexM ()
        ci := idx

        let entry = Log.mkConfig !term Array.empty

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        let! committed = responseCommitted response
        do! expectM "Should have committed the config change" true (konst committed)

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        do! expectM "Should only have half the nodes" (n / 2UL) numNodes
        do! expectM "Should have None as ConfigChange" None lastConfigChange

        //       _           _   _               _  _
        //   ___| | ___  ___| |_(_) ___  _ __   | || |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | || |_
        // |  __/ |  __/ (__| |_| | (_) | | | | |__   _|
        //  \___|_|\___|\___|\__|_|\___/|_| |_|    |_|
        //
        // with the new configuration only (should not work with nodes in old config anymore)

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM (!term + 1UL)
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        let! t = currentTermM ()
        term := t

        for nid in 1UL .. ((n / 2UL) / 2UL) do
          do! receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in election with regular configuration" Leader getState

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new nodes
        let entry =
          nodes
          |> Array.map snd
          |> Log.mkConfigChange 1UL peers

        let! idx = currentIndexM ()
        ci := idx

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        do! expectM "Should still have correct node count for new configuration 2" n numPeers
        do! expectM "Should still have correct logical node count 2" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration 2" (n / 2UL) numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange 2" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)

        //       _           _   _               ____
        //   ___| | ___  ___| |_(_) ___  _ __   | ___|
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  |___ \
        // |  __/ |  __/ (__| |_| | (_) | | | |  ___) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |____/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM (!term + 1UL)
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        let! t = currentTermM ()
        term := t

        // should become candidate with the old configuration of 5 nodes only
        for nid in 1UL .. ((n / 2UL) / 2UL) do
          do! receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in election in joint consensus with old configuration" Leader getState

        //       _           _   _                __
        //   ___| | ___  ___| |_(_) ___  _ __    / /_
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | '_ \
        // |  __/ |  __/ (__| |_| | (_) | | | | | (_) |
        //  \___|_|\___|\___|\__|_|\___/|_| |_|  \___/

        // same as calling becomeCandidate but not circumventing requestAllVotes
        do! setTermM (!term + 1UL)
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        let! t = currentTermM ()
        term := t

        // should become candidate with the new configuration of 10 nodes also
        for id in (n / 2UL) .. (n - 2UL) do
          let nid = fst nodes.[int id]
          let! result = getNodeM nid
          match result with
            | Some node ->
              // the nodes are not able to vote at first, because they will need
              // to be up to date to do that
              do! updateNodeM { node with State = Running; Voting = true }
              do! receiveVoteResponse nid { vote with Term = !term }
            | _ -> failwith "Node not found. :("

        do! expectM "Should be leader in election in joint consensus with new configuration" Leader getState

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete.                      |___/

        let! t = currentTermM ()
        term := t

        let! idx = currentIndexM ()
        ci := idx

        let entry = Log.mkConfig !term Array.empty
        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        do! expectM "Should have all the nodes" n numNodes
        do! expectM "Should have None as ConfigChange" None lastConfigChange
      }
      |> runWithCBS cbs
      |> noError

  let server_should_revert_to_follower_state_on_config_change_removal =
    testCase "should revert to follower state on config change removal" <| fun _ ->
      let n = 10UL                      // we want ten nodes overall

      let ci = ref 0UL
      let term = ref 1UL
      let lokk = new Object()

      let nodes =
        [| for n in 0UL .. (n - 1UL) do      // subtract one for the implicitly
            let nid = RaftId.Create()
            yield (nid, Node.create nid ()) |] // create node in the Raft state

      let vote = { Granted = true; Term = !term; Reason = None }

      let cbs =
        { mkcbs (ref ()) with
            SendAppendEntries = fun _ req ->
              lock lokk <| fun _ ->
                Some { Term = !term; Success = true; CurrentIndex = !ci; FirstIndex = 1UL }
          } :> IRaftCallbacks<_,_>

      raft {
        let self = snd nodes.[0]
        do! setSelfM self

        do! setPeersM (nodes |> Map.ofArray)

        // same as calling becomeCandidate, but w/o the IO
        do! setTermM !term
        do! resetVotesM ()
        do! voteForMyself ()
        do! setLeaderM None
        do! setStateM Candidate

        do! expectM "Should have be candidate" Candidate getState
        do! expectM "Should have $n nodes" n numNodes

        //       _           _   _               _
        //   ___| | ___  ___| |_(_) ___  _ __   / |
        //  / _ \ |/ _ \/ __| __| |/ _ \| '_ \  | |
        // |  __/ |  __/ (__| |_| | (_) | | | | | |
        //  \___|_|\___|\___|\__|_|\___/|_| |_| |_|
        //
        // with the full cluster of 10 nodes in total
        let! t = currentTermM ()
        term := t

        do! expectM "Should use the regular configuration" false inJointConsensus

        // we need only 5 votes coming in (plus our own) to make a majority
        for nid in 1UL .. (n / 2UL) do
          do! receiveVoteResponse (fst nodes.[int nid]) { vote with Term = !term }

        do! expectM "Should be leader in base configuration" Leader getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/
        let! t = currentTermM ()
        term := t

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        let entry =
          nodes
          |> Array.map snd
          |> Array.skip (int <| n / 2UL)
          |> Log.mkConfigChange !term peers

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL

        do! expectM "Should still have correct node count for new configuration" (n / 2UL) numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)
        do! expectM "Should be found in joint consensus configuration myself" true (getNode self.Id >> Option.isSome)

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        let! t = currentTermM ()
        term := t

        let entry = Log.mkConfig !term Array.empty
        let! response = receiveEntry entry

        let! result = responseCommitted response
        expect "Should not be committed yet" false id result

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1001UL

        let! result = responseCommitted response
        expect "Should be committed now" true id result

        do! expectM "Should only have half the nodes" (n / 2UL) numNodes
        do! expectM "Should have None as ConfigChange" None lastConfigChange
        do! expectM "Should be able to find myself" false (getNode self.Id >> Option.isSome)
        do! expectM "Should be follower now" Follower getState
      }
      |> runWithCBS cbs
      |> noError

  let server_should_send_appendentries_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new Object()
      let count = ref 0
      let ci = ref 0UL
      let term = ref 1UL
      let init = Raft.create (Node.create (RaftId.Create()) ())
      let cbs = { mkcbs (ref ()) with
                    SendAppendEntries = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Success = true; Term = !term; CurrentIndex = !ci; FirstIndex = 1UL } }
                :> IRaftCallbacks<_,_>

      let n = 10UL                       // we want ten nodes overall

      let nodes =
        [| for n in 1UL .. (n - 1UL) do      // subtract one for the implicitly
            let nid = RaftId.Create()
            yield (nid, Node.create nid ()) |] // create node in the Raft state
        |> Map.ofArray

      raft {
        let! self = getSelfM ()
        do! becomeLeader ()             // increases term!

        let! t = currentTermM ()
        term := t

        do! expectM "Should have be Leader" Leader getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of nodes

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          Map.toArray nodes
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.mkConfigChange !term peers

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        do! periodic 1000UL             // need to call periodic apply entry (add nodes for real)
        do! periodic 1000UL             // appendAllEntries now called

        do! expectM "Should still have correct node count for new configuration" n numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" 1UL numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)
        do! expectM "Should be in joint consensus configuration" true inJointConsensus

        let! t = currentTermM ()
        term := t

        let! response = receiveEntry (Log.make !term ())
        let! committed = responseCommitted response
        do! expectM "Should not be committed" false (konst committed)

        do! expectM "Count should be n" ((n - 1UL) * 2UL) (uint64 !count |> konst)
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_send_requestvote_to_all_servers_in_joint_consensus =
    testCase "should send appendentries to all servers in joint consensus" <| fun _ ->
      let lokk = new Object()
      let count = ref 0
      let term = ref 1UL
      let init = Raft.create (Node.create (RaftId.Create()) ())
      let cbs = { mkcbs (ref ()) with
                    SendRequestVote = fun _ _ ->
                      lock lokk <| fun _ ->
                        count := 1 + !count
                        Some { Granted = true; Term = !term; Reason = None } }
                :> IRaftCallbacks<_,_>

      let n = 10UL                       // we want ten nodes overall

      let nodes =
        [| for n in 1UL .. (n - 1UL) do      // subtract one for the implicitly
            let nid = RaftId.Create()
            yield (nid, Node.create nid ()) |] // create node in the Raft state

      raft {
        let! self = getSelfM ()

        do! becomeLeader ()             // increases term!
        do! expectM "Should have be Leader" Leader getState

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/  adding a ton of nodes

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          nodes
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.mkConfigChange 1UL peers

        let! response = receiveEntry entry

        do! periodic 1000UL

        do! expectM "Should still have correct node count for new configuration" n numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" 1UL numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)
        do! expectM "Should be in joint consensus configuration" true inJointConsensus

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        for peer in peers do
          do! updateNodeM { peer with State = Running; Voting = true }

        do! startElection ()

        expect "Count should be n" (n - 1UL) uint64 !count
      }
      |> runWithRaft init cbs
      |> noError

  let server_should_use_old_and_new_config_during_intermittend_appendentries =
    testCase "should use old and new config during intermittend appendentries" <| fun _ ->
      skiptest "NOOOOT WOOORKING YETTTTTT!@"

      printfn "-------------------------------------------------------------------------"
      let n = 10UL                       // we want ten nodes overall

      let nodes =
        [| for n in 0UL .. (n - 1UL) do      // subtract one for the implicitly
            let nid = RaftId.Create()
            yield (nid, Node.create nid ()) |] // create node in the Raft state

      let self = snd nodes.[0]
      let lokk = new Object()
      let ci = ref 0UL
      let term = ref 1UL
      let count = ref 0
      let init = Raft.create self
      let cbs =
        { mkcbs (ref()) with
            SendAppendEntries = fun _ _ ->
              lock lokk <| fun _ ->
                count := 1 + !count
                Some { Success = true; Term = !term; CurrentIndex = !ci; FirstIndex = 1UL } }
        :> IRaftCallbacks<_,_>

      printfn "-------------------------------------------------------------------------"
      raft {
        printfn "~~!!~~~~~~~~~~~~~~~~~~"
        do! setPeersM (nodes |> Map.ofArray)
        printfn "~~!!~~~~~~~~~~~~~~~~~~"
        do! becomeLeader ()          // increases term!

        printfn "~~!!~~~~~~~~~~~~~~~~~~"
        let! t = currentTermM ()
        term := t
        printfn "-2-2-2"

        do! expectM "Should have be Leader" Leader getState
        do! expectM "Should have $n nodes" n numNodes

        //                                         __ _
        //  _ __   _____      __   ___ ___  _ __  / _(_) __ _
        // | '_ \ / _ \ \ /\ / /  / __/ _ \| '_ \| |_| |/ _` |
        // | | | |  __/\ V  V /  | (_| (_) | | | |  _| | (_| |
        // |_| |_|\___| \_/\_/    \___\___/|_| |_|_| |_|\__, |
        //                                              |___/

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        printfn "-1-1-1"

        // we establish a new cluster configuration *without* the last 5 nodes
        // with node id's 5 - 9
        let entry =
          nodes
          |> Array.map snd
          |> Array.take (int <| n / 2UL)
          |> Log.mkConfigChange !term peers

        let! response = receiveEntry entry

        let! idx = currentIndexM ()
        ci := idx

        printfn "000"
        do! periodic 1000UL

        do! expectM "Should still have correct node count for new configuration" (n / 2UL) numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" n numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)
        do! expectM "Should be in joint consensus configuration" true inJointConsensus

        expect "Count should be correct" ((n - 1UL) * 2UL) uint64 !count

        //                    _                    _        _             _
        //   __ _ _ __  _ __ | |_   _    ___ _ __ | |_ _ __(_) ___  ___  / |
        //  / _` | '_ \| '_ \| | | | |  / _ \ '_ \| __| '__| |/ _ \/ __| | |
        // | (_| | |_) | |_) | | |_| | |  __/ | | | |_| |  | |  __/\__ \ | |
        //  \__,_| .__/| .__/|_|\__, |  \___|_| |_|\__|_|  |_|\___||___/ |_|
        //       |_|   |_|      |___/  in a joint consensus configuration

        printfn "111"
        let! committed = responseCommitted response
        expect "should not have been committed" false id committed

        let! term = currentTermM ()
        let! idx = currentIndexM ()
        let aer = { Success      = true
                    Term         = term
                    CurrentIndex = idx
                    FirstIndex   = 1UL }

        for nid in (n / 2UL) .. (n - 1UL) do
          do! receiveAppendEntriesResponse (fst nodes.[int nid]) aer

        let! committed = responseCommitted response
        expect "should be committed" true id committed

        //                                  __ _                       _   _
        //  _ __ ___        ___ ___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
        // | '__/ _ \_____ / __/ _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
        // | | |  __/_____| (_| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
        // |_|  \___|      \___\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
        // is now complete!                      |___/

        let! term = currentTermM ()
        let entry = Log.mkConfig term Array.empty
        let! response = receiveEntry entry

        do! expectM "Should only have half the nodes" (n / 2UL) numNodes
        do! expectM "Should have None as ConfigChange" None lastConfigChange

        //            _     _                   _
        //   __ _  __| | __| |  _ __   ___   __| | ___  ___
        //  / _` |/ _` |/ _` | | '_ \ / _ \ / _` |/ _ \/ __|
        // | (_| | (_| | (_| | | | | | (_) | (_| |  __/\__ \
        //  \__,_|\__,_|\__,_| |_| |_|\___/ \__,_|\___||___/

        let! peers = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)

        // we establish a new cluster configuration with 5 new nodes
        let entry =
          nodes
          |> Array.map snd
          |> Array.append [| self |]
          |> Log.mkConfigChange 1UL peers

        let! response = receiveEntry entry

        do! expectM "Should still have correct node count for new configuration" n numPeers
        do! expectM "Should still have correct logical node count" n numLogicalPeers
        do! expectM "Should still have correct node count for old configuration" (n / 2UL) numOldPeers
        do! expectM "Should have JointConsensus entry as ConfigChange" (Log.id entry) (lastConfigChange >> Option.get >> Log.id)

        //                    _                    _        _             ____
        //   __ _ _ __  _ __ | |_   _    ___ _ __ | |_ _ __(_) ___  ___  |___ \
        //  / _` | '_ \| '_ \| | | | |  / _ \ '_ \| __| '__| |/ _ \/ __|   __) |
        // | (_| | |_) | |_) | | |_| | |  __/ | | | |_| |  | |  __/\__ \  / __/
        //  \__,_| .__/| .__/|_|\__, |  \___|_| |_|\__|_|  |_|\___||___/ |_____|
        //       |_|   |_|      |___/

        let! result = responseCommitted response
        expect "Should not be committed" false id result

        let! term = currentTermM ()
        let! idx = currentIndexM ()
        let aer = { Success      = true
                    Term         = term
                    CurrentIndex = idx
                    FirstIndex   = 1UL }

        for nid in 1UL .. (n / 2UL) do
          do! receiveAppendEntriesResponse (fst nodes.[int nid]) aer

        let! result = responseCommitted response
        expect "Should be committed" true id result

      }
      |> runWithRaft init cbs
      |> noError
