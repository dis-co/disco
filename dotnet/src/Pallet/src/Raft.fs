namespace Pallet.Core

open System
open FSharpx.Functional

[<AutoOpen>]
module RaftMonad =

  /// get current Raft state
  let get = MkRM (fun _ s -> Right (s, s))

  /// update Raft/State to supplied value
  let put s = MkRM (fun _ _ -> Right ((), s))

  /// get the read-only environment value
  let read : RaftM<_,_,_,_,_> = MkRM (fun l s -> Right (l, s))

  /// unwrap the closure and apply it to the supplied state/env
  let apply (env: 'e) (state: 's) (m: RaftMonad<'e,'s,_,_,_>)  =
    match m with | MkRM func -> func env state

  /// run the monadic action against state and environment values
  let runRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'alt,'err>) =
    apply l s m

  /// run monadic action against supplied state and evironment and return new state
  let evalRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'alt,'err>) =
    match runRaft s l m with
      | Right (_,state) | Middle (_,state) | Left (_,state) -> state

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Right` value. This means the computation will,
  /// if possible, continue to the next step.
  let returnM value : RaftMonad<'e,'s,'t,'alt,'err> =
    MkRM (fun _ state -> Right(value, state))

  let ignoreM _ : RaftMonad<'e,'s,unit,'alt,'err> =
    MkRM (fun _ state -> Right((), state))

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Left` value. This means the computation will
  /// not continue past this step and no regular value will be returned.
  let failM l =
    MkRM (fun _ s -> Left (l, s))

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Middle` value. This means the computation will
  /// stop at this point, return a value and the current state.
  let stopM value =
    MkRM (fun _ state -> Middle(value, state))

  /// pass through the given action
  let returnFromM func : RaftMonad<'e,'s,'t,'alt,'err> =
    func

  let zeroM () =
    MkRM (fun _ state -> Right((), state))

  let delayM (f: unit -> RaftMonad<'e,'s,'t,'alt,'err>) =
    MkRM (fun env state -> f () |> apply env state)

  /// Chain up effectful actions.
  let bindM (m: RaftMonad<'env,'state,'a,'m,'err>)
            (f: 'a -> RaftMonad<'env,'state,'b,'m,'err>) :
            RaftMonad<'env,'state,'b,'m,'err> =
    MkRM (fun env state ->
          match apply env state m with
            | Right  (value,state') -> f value |> apply env state'
            | Middle (value,state') -> Middle (value,state')
            | Left    err           -> Left err)

  let (>>=) = bindM

  let combineM (m1: RaftMonad<_,_,_,_,_>) (m2: RaftMonad<_,_,_,_,_>) =
    bindM m1 (fun _ -> m2)

  let tryWithM (body: RaftMonad<_,_,_,_,_>) (handler: exn -> RaftMonad<_,_,_,_,_>) =
    MkRM (fun env state ->
          try apply env state body
          with ex -> apply env state (handler ex))

  let tryFinallyM (body: RaftMonad<_,_,_,_,_>) handler : RaftMonad<_,_,_,_,_> =
    MkRM (fun env state ->
          try apply env state body
          finally handler ())

  let usingM (resource: ('a :> System.IDisposable)) (body: 'a -> RaftMonad<_,_,_,_,_>) =
    tryFinallyM (body resource)
      (fun _ -> if not <| isNull (box resource)
                then resource.Dispose())

  let rec whileM (guard: unit -> bool) (body: RaftMonad<_,_,_,_,_>) =
    match guard () with
      | true -> bindM body (fun _ -> whileM guard body)
      | _ -> zeroM ()

  let rec forM (sequence: seq<_>) (body: 'a -> RaftMonad<_,_,_,_,_>) : RaftMonad<_,_,_,unit,_> =
    usingM (sequence.GetEnumerator())
      (fun enum -> whileM enum.MoveNext (delayM (fun _ -> body enum.Current)))

  type RaftBuilder() =
    member __.Return(v) = returnM v
    member __.ReturnFrom(v) = returnFromM v
    member __.Bind(m, f) = bindM m f
    member __.Zero() = zeroM ()
    member __.Delay(f) = delayM f
    member __.Combine(a,b) = combineM a b
    member __.TryWith(body, handler) = tryWithM body handler
    member __.TryFinally(body, handler) = tryFinallyM body handler
    member __.Using(res, body) = usingM res body
    member __.While(guard, body) = whileM guard body
    member __.For(seq, body) = forM seq body

  let raft = new RaftBuilder()

[<AutoOpen>]
module Raft =

  /////////////////////////////////////////////
  //  __  __                       _ _       //
  // |  \/  | ___  _ __   __ _  __| (_) ___  //
  // | |\/| |/ _ \| '_ \ / _` |/ _` | |/ __| //
  // | |  | | (_) | | | | (_| | (_| | | (__  //
  // |_|  |_|\___/|_| |_|\__,_|\__,_|_|\___| //
  /////////////////////////////////////////////

  let log node str =
    read >>= fun cbs ->
      cbs.LogMsg node (sprintf "[RAFT: %A] %s" node.Id str)
      |> returnM

  let sendRequestVote (cbs: IRaftCallbacks<_,_>) node req =
    cbs.SendRequestVote node req

  let sendAppendEntries (cbs: IRaftCallbacks<_,_>) node ae =
    cbs.SendAppendEntries node ae

  let sendAppendEntriesM node request =
    read >>= fun cbs ->
      cbs.SendAppendEntries node request
      |> returnM

  let persistVote node =
    read >>= fun cbs ->
      cbs.PersistVote node
      |> returnM

  let persistTerm node =
    read >>= fun cbs ->
      cbs.PersistTerm node
      |> returnM

  let persistLog log =
    read >>= fun cbs ->
      cbs.PersistLog log
      |> returnM

  let deleteLog log =
    read >>= fun cbs ->
      cbs.DeleteLog log
      |> returnM

  let hasSufficientLogs node =
    read >>= fun cbs ->
      cbs.HasSufficientLogs node
      |> returnM

  let modify (f: Raft<_,_> -> Raft<_,_>) =
    get >>= (f >> put)

  let zoomM (f: Raft<'d,'n> -> 'a) =
    get >>= (f >> returnM)

  ///////////////////////////////////////
  //  ____       _            _        //
  // |  _ \ _ __(_)_   ____ _| |_ ___  //
  // | |_) | '__| \ \ / / _` | __/ _ \ //
  // |  __/| |  | |\ V / (_| | ||  __/ //
  // |_|   |_|  |_| \_/ \__,_|\__\___| //
  ///////////////////////////////////////

  let private _rand = new System.Random()

  //////////////////////////////////////
  //   ____                _          //
  //  / ___|_ __ ___  __ _| |_ ___    //
  // | |   | '__/ _ \/ _` | __/ _ \   //
  // | |___| | |  __/ (_| | ||  __/   //
  //  \____|_|  \___|\__,_|\__\___|   //
  //////////////////////////////////////

  let create (self : Node<'n>) : Raft<'d,'n> =
    { Node              = self
    ; State             = Follower
    ; CurrentTerm       = 0UL
    ; CurrentLeader     = None
    ; Peers             = Map.ofList [(self.Id, self)]
    ; OldPeers          = None
    ; NumNodes          = 1UL
    ; VotedFor          = None
    ; Log               = Log.empty
    ; CommitIndex       = 0UL
    ; LastAppliedIdx    = 0UL
    ; TimeoutElapsed    = 0UL
    ; ElectionTimeout   = 1000UL // msec
    ; RequestTimeout    = 200UL  // msec
    ; MaxLogDepth       = 40UL   // items
    ; ConfigChangeEntry = None
    }

  /// Is the Raft value in Follower state.
  let isFollower (state: Raft<'d,'n>) =
    state.State = Follower

  let isFollowerM = fun _ -> zoomM isFollower

  /// Is the Raft value in Candate state.
  let isCandidate (state: Raft<'d,'n>) =
    state.State = Candidate

  let isCandidateM _ = zoomM isCandidate

  /// Is the Raft value in Leader state
  let isLeader (state: Raft<'d,'n>) =
    state.State = Leader

  let isLeaderM _ = zoomM isLeader

  let inJointConsensus (state: Raft<_,_>) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus _) -> true
      | _                       -> false

  let inJointConsensusM _ = zoomM inJointConsensus

  ////////////////////////////////////////////////
  //  _   _           _         ___             //
  // | \ | | ___   __| | ___   / _ \ _ __  ___  //
  // |  \| |/ _ \ / _` |/ _ \ | | | | '_ \/ __| //
  // | |\  | (_) | (_| |  __/ | |_| | |_) \__ \ //
  // |_| \_|\___/ \__,_|\___|  \___/| .__/|___/ //
  //                                |_|         //
  ////////////////////////////////////////////////

  let getChanges (state: Raft<_,_>) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus(_,_,_,changes,_)) -> Some changes
      | _ -> None

  let logicalPeers (state: Raft<_,_>) =
    // when setting the NumNodes counter we have to include the old config, if set
    if inJointConsensus state then
        // take the old peers as seed and apply the new peers on top
      match state.OldPeers with
        | Some peers -> Map.fold (fun m k n -> Map.add k n m) peers state.Peers
        | _ -> state.Peers
    else
      state.Peers

  let logicalPeersM _ = zoomM logicalPeers

  let countNodes peers = Map.fold (fun m _ _ -> m + 1UL) 0UL peers

  let numLogicalPeers (state: Raft<_,_>) =
    logicalPeers state |> countNodes

  let setNumPeers (state: Raft<_,_>) =
    { state with NumNodes = countNodes state.Peers }

  /// Set States Nodes to supplied Map of Nodes. Also cache count of nodes.
  let setPeers (peers : Map<NodeId,Node<'n>>) (state: Raft<'d,'n>) =
    { state with Peers = Map.add state.Node.Id state.Node peers }
    |> setNumPeers

  /// Adds a node to the list of known Nodes and increments NumNodes counter
  let addNode (node : Node<'n>) (state: Raft<'d,'n>) : Raft<'d,'n> =
    let exists = Map.containsKey node.Id state.Peers
    { state with
        Peers = Map.add node.Id node state.Peers
        NumNodes =
          if exists
            then state.NumNodes
            else state.NumNodes + 1UL }

  let addNodeM (node: Node<'n>) =
    get >>= (addNode node >> put)

  /// Alias for `addNode`
  let addPeer = addNode
  let addPeerM = addNodeM

  /// Add a Non-voting Peer to the list of known Nodes
  let addNonVotingNode (node : Node<'n>) (state: Raft<'d,'n>) =
    addNode { node with Voting = false; State = Joining } state

  /// Remove a Peer from the list of known Nodes and decrement NumNodes counter
  let removeNode (node : Node<'n>) (state: Raft<'d,'n>) =
    let numNodes =
      if Map.containsKey node.Id state.Peers
        then state.NumNodes - 1UL
        else state.NumNodes

    { state with
        Peers = Map.remove node.Id state.Peers
        NumNodes = numNodes }

  let updateNode (node : Node<'n>) (cbs: IRaftCallbacks<_,_>) (state: Raft<'d,'n>) =
    // if we are in joint consensus, we must update the node value in either the
    // new or the old configuration, or both.
    let regular = Map.containsKey node.Id state.Peers
    if inJointConsensus state then
      if regular then cbs.NodeUpdated node
      { state with
          Peers =
            if regular then
              Map.add node.Id node state.Peers
            else state.Peers
          OldPeers =
            match state.OldPeers with
              | Some peers ->
                if Map.containsKey node.Id peers then
                  if not regular then cbs.NodeUpdated node
                  Map.add node.Id node peers |> Some
                else Some peers
              | None ->                 // apply all required changes again
                let folder m = function // but this is an edge case
                  | NodeAdded   peer -> Map.add peer.Id peer m
                  | NodeRemoved peer -> Map.filter (fun k _ -> k <> peer.Id) m
                let changes = getChanges state |> Option.get
                let peers = Array.fold folder state.Peers changes
                if Map.containsKey node.Id peers
                then Map.add node.Id node peers |> Some
                else Some peers }
      |> setNumPeers
    else // base case
      if regular then cbs.NodeUpdated node
      { state with
          Peers =
            if Map.containsKey node.Id state.Peers
            then Map.add node.Id node state.Peers
            else state.Peers }

  let applyChanges changes state =
    let folder _state = function
      | NodeAdded   node -> addNonVotingNode node _state
      | NodeRemoved node -> removeNode       node _state
    Array.fold folder state changes

  let updateNodeM (node: Node<'n>) =
    read >>= fun env ->
      get >>= (updateNode node env >> put)

  let addNodes (nodes : Node<'n> array) (state: Raft<'d,'n>) =
    Array.fold (fun m n -> addNode n m) state nodes

  let addNodesM (nodes: Node<'n> array) =
    get >>= (addNodes nodes >> put)

  let addPeers = addNodes
  let addPeersM = addNodesM

  let addNonVotingNodeM (node: Node<'n>) =
    get >>= (addNonVotingNode node >> put)

  let removeNodeM (node: Node<'n>) =
    get >>= (removeNode node >> put)

  let hasNode (nid : NodeId) (state: Raft<'d,'n>) =
    Map.containsKey nid state.Peers

  let hasNodeM _ = hasNode >> zoomM

  let getNode (nid : NodeId) (state: Raft<'d,'n>) =
    if inJointConsensus state then
      logicalPeers state |> Map.tryFind nid
    else
      Map.tryFind nid state.Peers

  /// Find a peer by its Id. Return None if not found.
  let getNodeM nid = getNode nid |> zoomM

  let getNodes (state: Raft<_,_>) = state.Peers
  let getNodesM _ = zoomM getNodes

  let getSelf (state: Raft<_,_>) = state.Node
  let getSelfM _ = zoomM getSelf

  let setSelf (node: Node<_>) (state: Raft<_,_>) =
    { state with Node = node }

  let setSelfM node =
    setSelf node |> modify

  let lastConfigChange (state: Raft<_,_>) =
    state.ConfigChangeEntry

  let lastConfigChangeM _ =
    lastConfigChange |> zoomM

  ////////////////////////////////////////
  //  ____        __ _                  //
  // |  _ \ __ _ / _| |_                //
  // | |_) / _` | |_| __|               //
  // |  _ < (_| |  _| |_                //
  // |_| \_\__,_|_|  \__|               //
  ////////////////////////////////////////

  /// Set CurrentTerm on Raft to supplied term.
  let setTerm (term : Term) (state: Raft<'d,'n>) =
    { state with CurrentTerm = term }

  /// Set CurrentTerm to supplied value. Monadic action.
  let setTermM (term : Term) =
    setTerm term |> modify

  /// Set current RaftState to supplied state.
  let setState (rs : RaftState) (env: IRaftCallbacks<_,_>) (state: Raft<'d,'n>) =
    env.StateChanged state.State rs
    { state with
        State = rs
        CurrentLeader =
          if rs = Leader
          then Some(state.Node.Id)
          else state.CurrentLeader }

  /// Set current RaftState to supplied state. Monadic action.
  let setStateM (state : RaftState) =
    read >>= (setState state >> modify)

  /// Get current RaftState: Leader, Candidate or Follower
  let state (state: Raft<'d,'n>) =
    state.State

  /// Get current RaftState. Monadic action.
  let stateM _ = zoomM state

  /// Get Node associated with supplied raft value.
  let self (state: Raft<'d,'n>) =
    state.Node

  /// Get Node associated with supplied raft value. Monadic action.
  let selfM _ = zoomM self

  let setOldPeers (peers : Map<NodeId,Node<'n>> option) (state: Raft<'d,'n>) =
    { state with OldPeers = peers  } |> setNumPeers

  /// Set States Nodes to supplied Map of Nodes. Monadic action.
  let setPeersM (peers: Map<_,_>) =
    setPeers peers |> modify

  /// Set States Nodes to supplied Map of Nodes. Monadic action.
  let setOldPeersM (peers: Map<_,_> option) =
    setOldPeers peers |> modify

  /// Map over States Nodes with supplied mapping function
  let updatePeers (f: Node<'n> -> Node<'n>) (state: Raft<'d,'n>) =
    { state with Peers = Map.map (fun _ v -> f v) state.Peers }

  /// Map over States Nodes with supplied mapping function. Monadic action
  let updatePeersM (f: Node<'n> -> Node<'n>) =
    updatePeers f |> modify

  /// Set States CurrentLeader field to supplied NodeId.
  let setLeader (leader : NodeId option) (state: Raft<'d,'n>) =
    { state with CurrentLeader = leader }

  /// Set States CurrentLeader field to supplied NodeId. Monadic action.
  let setLeaderM (leader : NodeId option) =
    setLeader leader |> modify

  /// Set the nextIndex field on Node corresponding to supplied Id (should it
  /// exist, that is).
  let setNextIndex (nid : NodeId) idx cbs (state: Raft<'d,'n>) =
    let node = getNode nid state
    match node with
      | Some node ->
        { node with NextIndex = if idx < 1UL then 1UL else idx }
        |> fun node ->
          updateNode node cbs state
      | None -> state

  /// Set the nextIndex field on Node corresponding to supplied Id (should it
  /// exist, that is) and supplied index. Monadic action.
  let setNextIndexM (nid : NodeId) idx =
    read >>= (setNextIndex nid idx >> modify)

  /// Set the nextIndex field on all Nodes to supplied index.
  let setAllNextIndex idx (state: Raft<'d,'n>) =
    let updater _ p = { p with NextIndex = idx }
    if inJointConsensus state then
      { state with
          Peers = Map.map updater state.Peers
          OldPeers =
            match state.OldPeers with
              | Some peers -> Map.map updater peers |> Some
              | _ -> None }
    else
      { state with Peers = Map.map updater state.Peers }

  let setAllNextIndexM idx =
    setAllNextIndex idx |> modify

  /// Set the matchIndex field on Node to supplied index.
  let setMatchIndex nid idx env (state: Raft<_,_>) =
    let node = getNode nid state
    match node with
      | Some peer ->
        { peer with MatchIndex = idx }
        |> fun node ->
          updateNode node env state
      | _ -> state

  let setMatchIndexM nid idx =
    read >>= (setMatchIndex nid idx >> modify)

  /// Set the matchIndex field on all Nodes to supplied index.
  let setAllMatchIndex idx (state: Raft<'d,'n>) =
    let updater _ p = { p with MatchIndex = idx }
    if inJointConsensus state then
      { state with
          Peers = Map.map updater state.Peers
          OldPeers =
            match state.OldPeers with
              | Some peers -> Map.map updater peers |> Some
              | _ -> None }
    else
      { state with Peers = Map.map updater state.Peers }

  let setAllMatchIndexM idx =
    setAllMatchIndex idx |> modify

  /// Remeber who we have voted for in current election.
  let voteFor (node : Node<'n> option) =
    let doVoteFor state =
      { state with VotedFor = Option.map (fun (n : Node<'n>) -> n.Id) node }

    raft {
      let! state = get
      do! persistVote node
      do! doVoteFor state |> put
    }

  /// Remeber who we have voted for in current election
  let voteForId (nid : NodeId)  =
    raft {
      let! node = getNodeM nid
      do! voteFor node
    }

  let resetVotes (state: Raft<'d,'n>) =
    let resetter _ peer = Node.voteForMe peer false
    { state with
        Peers = Map.map resetter state.Peers
        OldPeers =
          match state.OldPeers with
            | Some peers -> Map.map resetter peers |> Some
            | _ -> None }

  let resetVotesM _ =
    resetVotes |> modify

  let voteForMyself _ =
    get >>= fun state -> voteFor (Some state.Node)

  let votedForMyself (state: Raft<'d,'n>) =
    match state.VotedFor with
      | Some(nid) -> nid = state.Node.Id
      | _ -> false

  let votedFor (state: Raft<'d,'n>) =
    state.VotedFor

  let votedForM _ = zoomM votedFor

  let setVoting (node : Node<'n>) (vote : bool) (state: Raft<'d,'n>) =
    let updated = Node.voteForMe node vote
    if inJointConsensus state then
      { state with
          Peers =
            if Map.containsKey updated.Id state.Peers then
              Map.add updated.Id updated state.Peers
            else state.Peers
          OldPeers =
            match state.OldPeers with
              | Some peers ->
                if Map.containsKey updated.Id peers then
                  Map.add updated.Id updated peers |> Some
                else Some peers
              | _ -> None }
    else
      { state with Peers = Map.add updated.Id updated state.Peers }

  let setVotingM node vote =
    setVoting node vote |> modify

  let currentIndex (state: Raft<'d,'n>) =
    Log.index state.Log

  let currentIndexM _ = zoomM currentIndex

  let numNodes (state: Raft<'d,'n>) =
    state.NumNodes

  let numNodesM _ = zoomM numNodes

  let numPeers = numNodes
  let numPeersM = numNodesM

  let numOldPeers (state: Raft<_,_>) =
    match state.OldPeers with
      | Some peers -> Map.fold (fun m _ _ -> m + 1UL) 0UL peers
      |      _     -> 0UL

  let numOldPeersM _ = zoomM numOldPeers

  let votingNodesForConfig peers =
    let counter r _ n =
      if Node.isVoting n then r + 1UL else r
    Map.fold counter 0UL peers

  let votingNodes (state: Raft<_,_>) =
    votingNodesForConfig state.Peers

  let votingNodesM _ = zoomM votingNodes

  let votingNodesForOldConfig (state: Raft<_,_>) =
    match state.OldPeers with
      | Some peers -> votingNodesForConfig peers
      | _ -> 0UL

  let votingNodesForOldConfigM _ = zoomM votingNodesForOldConfig

  let numLogs (state: Raft<'d,'n>) =
    Log.length state.Log

  let numLogsM _ = zoomM numLogs

  let currentTerm (state: Raft<'d,'n>) =
    state.CurrentTerm

  let currentTermM _ = zoomM currentTerm

  let firstIndex (term: Term) (state: Raft<_,_>) =
    Log.firstIndex term state.Log

  let firstIndexM (term: Term) =
    firstIndex term |> zoomM

  let currentLeader (state: Raft<'d,'n>) =
    state.CurrentLeader

  let currentLeaderM _ = zoomM currentLeader

  let getLeader (state: Raft<_,_>) =
    currentLeader state |> Option.bind (flip getNode state)

  let commitIndex (state: Raft<'d,'n>) =
    state.CommitIndex

  let commitIndexM _ = zoomM commitIndex

  let setCommitIndex (idx : Index) (state: Raft<'d,'n>) =
    { state with CommitIndex = idx }

  let setCommitIndexM (idx : Index) =
    setCommitIndex idx |> modify

  let requestTimedOut (state: Raft<'d,'n>) : bool =
    state.RequestTimeout <= state.TimeoutElapsed

  let requestTimedOutM _ = zoomM requestTimedOut

  let electionTimedOut (state: Raft<'d,'n>) : bool =
    state.ElectionTimeout <= state.TimeoutElapsed

  let electionTimedOutM _ = zoomM electionTimedOut

  let electionTimeout (state: Raft<'d,'n>) =
    state.ElectionTimeout

  let electionTimeoutM _ = zoomM electionTimeout

  let timeoutElapsed (state: Raft<'d,'n>) =
    state.TimeoutElapsed

  let timeoutElapsedM _ = zoomM timeoutElapsed

  let setTimeoutElapsed (timeout : Long) (state: Raft<'d,'n>) =
    { state with TimeoutElapsed = timeout }

  let setTimeoutElapsedM (timeout: Long) =
    setTimeoutElapsed timeout |> modify

  let requestTimeout (state: Raft<'d,'n>) =
    state.RequestTimeout

  let requestTimeoutM _ = zoomM requestTimeout

  let setRequestTimeout (timeout : Long) (state: Raft<'d,'n>) =
    { state with RequestTimeout = timeout }

  let setRequestTimeoutM (timeout: Long) =
    setRequestTimeout timeout |> modify

  let setElectionTimeout (timeout : Long) (state: Raft<'d,'n>) =
    { state with ElectionTimeout = timeout }

  let setElectionTimeoutM (timeout: Long) =
    setElectionTimeout timeout |> modify

  let lastAppliedIdx (state: Raft<'d,'n>) =
    state.LastAppliedIdx

  let lastAppliedIdxM _ = zoomM lastAppliedIdx

  let setLastAppliedIdx (idx : Index) (state: Raft<'d,'n>) =
    { state with LastAppliedIdx = idx }

  let setLastAppliedIdxM (idx: Index) =
    setLastAppliedIdx idx |> modify

  let maxLogDepth (state: Raft<_,_>) = state.MaxLogDepth

  let maxLogDepthM _ = zoomM maxLogDepth

  let becomeFollower _ =
    raft {
      let! state = get
      do! log state.Node "Becoming Follower"
      do! setStateM Follower
    }

  //////////////////////////////////////////
  //  _                   ___             //
  // | |    ___   __ _   / _ \ _ __  ___  //
  // | |   / _ \ / _` | | | | | '_ \/ __| //
  // | |__| (_) | (_| | | |_| | |_) \__ \ //
  // |_____\___/ \__, |  \___/| .__/|___/ //
  //             |___/        |_|         //
  //////////////////////////////////////////

  let lastLogTerm (state: Raft<'d,'n>) =
    Log.term state.Log

  let lastLogTermM _ = zoomM lastLogTerm

  let getEntryAt (idx : Index) (state: Raft<'d,'n>) : LogEntry<'d,'n> option =
    Log.at idx state.Log

  let getEntryAtM (idx: Index) = zoomM (getEntryAt idx)

  let getEntriesUntil (idx : Index) (state: Raft<'d,'n>) : LogEntry<'d,'n> option =
    Log.until idx state.Log

  let getEntriesUntilM (idx: Index) = zoomM (getEntriesUntil idx)

  let entriesUntilExcluding (idx: Index) (state: Raft<_,_>) =
    Log.untilExcluding idx state.Log

  let entriesUntilExcludingM (idx: Index) =
    entriesUntilExcluding idx |> zoomM

  let handleConfigChange (log: LogEntry<_,_>) (state: Raft<_,_>) =
    match log with
      | Configuration _ -> setOldPeers None state
      | JointConsensus(_,_,_,changes,_) ->
        let old = state.Peers
        applyChanges changes state
        |> setOldPeers (Some old)
      | _ -> state

  //                                   _ _____       _
  //   __ _ _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __ _   _
  //  / _` | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| | | |
  // | (_| | |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |_| |
  //  \__,_| .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|   \__, |
  //       |_|   |_|                                          |___/

  let appendEntry (log: LogEntry<'d,'n>) =
    raft {
      let! state = get

      // create the new log by appending and get back the entries just added
      // (with correct monotonic idx's)
      let newlog = Log.append log state.Log
      let appended = Log.getn (Log.depth log) newlog

      // look at the newly appended entries again (now they have their correct idx)
      match appended with
        | Some entries ->
          // hanlde configuration changes and keep track of the most recent one
          let state, change =
            LogEntry.foldr
              (fun (state, chng) lg ->
                match lg with
                  | Configuration _ as config ->
                    (handleConfigChange config state, None)
                  | JointConsensus _ as config ->
                    (handleConfigChange config state, Some config)
                  | _ -> (state, chng)) (state, state.ConfigChangeEntry) entries

          do! put { state with Log = newlog; ConfigChangeEntry = change }
        | None ->
          printfn "[WARNING] appended log entries are gone. This is bad."

      return appended
    }

  let appendEntryM (log: LogEntry<'d,'n>) =
    raft {
      let! result = appendEntry log
      match result with
        | Some entries ->
          do! persistLog entries
        | _ -> ()
      return result
    }

  //                      _       _____       _
  //   ___ _ __ ___  __ _| |_ ___| ____|_ __ | |_ _ __ _   _
  //  / __| '__/ _ \/ _` | __/ _ \  _| | '_ \| __| '__| | | |
  // | (__| | |  __/ (_| | ||  __/ |___| | | | |_| |  | |_| |
  //  \___|_|  \___|\__,_|\__\___|_____|_| |_|\__|_|   \__, |
  //                                                   |___/

  let createEntryM (d: 'd) =
    raft {
      let! state = get
      let log = LogEntry(RaftId.Create(),0UL,state.CurrentTerm,d,None)
      return! appendEntryM log
    }

  let updateLog (log: Log<_,_>) (state: Raft<_,_>) =
    { state with Log = log }

  let updateLogEntries (entries: LogEntry<_,_>) (state: Raft<_,_>) =
    { state with
        Log = { Index = LogEntry.index entries
                Depth = LogEntry.depth entries
                Data  = Some entries } }

  /// Delete a log entry at the index specified. Returns the original value if
  /// the record is not found.
  let removeEntry idx (cbs: IRaftCallbacks<_,_>) state =
    match Log.at idx state.Log with
      | Some log ->
        match LogEntry.pop log with
          | Some newlog ->
            match Log.until idx state.Log with
              | Some items ->
                Log.iter (fun _ entry -> cbs.DeleteLog entry) items
              | _ -> ()
            updateLogEntries newlog state
          | _ ->
            cbs.DeleteLog log
            updateLog Log.empty state
      | _ -> state

  let removeEntryM idx =
    raft {
      let! env = read
      do! removeEntry idx env |> modify
    }

  /////////////////////////////////////////////////////////////////////////////
  //     _    Receive                    _ _____       _        _            //
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___  //
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __| //
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \ //
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/ //
  //         |_|   |_|                                                       //
  /////////////////////////////////////////////////////////////////////////////

  /// Preliminary Checks on the AppendEntry value
  let inline private makeResponse (msg : AppendEntries<'d,'n>) =
    raft {
      let! state = get

      let resp =
        { Term         = currentTerm state
        ; Success      = false
        ; CurrentIndex = 0UL
        ; FirstIndex   = 0UL
        }

      // 1) If this node is currently candidate and both its and the requests
      // term are equal, we become follower and reset VotedFor.
      if isCandidate state && currentTerm state = msg.Term then
        do! voteFor None
        do! becomeFollower ()
        return Right resp
      // 2) Else, if the current node's term value is lower than the requests
      // term, we take become follower and set our own term to higher value.
      elif currentTerm state < msg.Term then
        let resp' = { resp with Term = msg.Term } // set response term
        do! setTermM msg.Term
        do! becomeFollower ()
        return Right resp'
      // 3) Else, finally, if the msg's Term is lower than our own we reject the
      // the request entirely.
      elif msg.Term < currentTerm state then
        return Left { resp with CurrentIndex = currentIndex state }
      else
        return Right resp
    }

  // If an existing entry conflicts with a new one (same index
  // but different terms), delete the existing entry and all that
  // follow it (§5.3)
  let inline private handleConflicts (request: AppendEntries<'d,'n>) =
    raft {
      let idx = request.PrevLogIdx + 1UL
      let! local = getEntryAtM idx

      match request.Entries with
        | Some entries ->
          let remote = Log.last entries
          // find the entry in the local log that corresponds to position of
          // then log in the request and compare their terms
          match local with
            | Some entry ->
              if Log.entryTerm entry <> Log.entryTerm remote then
                // removes entry at idx (and all following entries)
                do! removeEntryM idx
            | _ -> ()
        | _ ->
          if Option.isSome local then
            do! removeEntryM idx
    }

  let inline private applyRemainder (msg : AppendEntries<'d,'n>) (resp : AppendResponse) =
    raft {
      match msg.Entries with
        | Some entries ->
          let! result = appendEntryM entries
          match result with
            | Some log ->
              let! fst = currentTermM () >>= firstIndexM
              return { resp with
                        CurrentIndex = Log.entryIndex log
                        FirstIndex   =
                            match fst with
                              | Some fidx -> fidx
                              | _         -> msg.PrevLogIdx + Log.depth log }
            | _ -> return resp
        | _ -> return resp
    }

  /// If leaderCommit > commitIndex, set commitIndex =
  /// min(leaderCommit, index of most recent entry)
  let inline private maybeSetCommitIdx (msg : AppendEntries<'d,'n>) =
    raft {
      let! state = get
      if commitIndex state < msg.LeaderCommit
      then
        let lastLogIdx = max (currentIndex state) 1UL
        let newIndex = min lastLogIdx msg.LeaderCommit
        do! setCommitIndexM newIndex
    }

  let inline private logNodeNotFound msg =
    sprintf "AE no log at prev_idx %d" msg.PrevLogIdx

  let inline private _logPrevLogTermMismatch msg =
    sprintf "term mismatch at PrevLogIdx %d" msg.PrevLogIdx

  let inline private logPrevTermMismatch term msg raft =
    sprintf "AE term doesn't match prev_term (ie. %d vs %d) ci:%d pli:%d"
      term
      msg.PrevLogTerm
      (currentIndex raft)
      msg.PrevLogIdx

  let inline private processEntry nid msg resp =
    raft {
      do! handleConflicts msg
      let! response = applyRemainder msg resp
      do! maybeSetCommitIdx msg
      do! setLeaderM nid

      return { response with Success = true }
    }

  ///  2. Reply false if log doesn't contain an entry at prevLogIndex whose
  /// term matches prevLogTerm (§5.3)
  let inline private checkAndProcess entry nid msg resp =
    raft {
      let! state = get

      if currentIndex state < msg.PrevLogIdx then
        do! log state.Node (_logPrevLogTermMismatch msg)
        return resp
      else
        let term = LogEntry.term entry
        if term <> msg.PrevLogTerm then
          do! log state.Node (logPrevTermMismatch term msg state)
          let response = { resp with CurrentIndex = msg.PrevLogIdx - 1UL }
          do! removeEntryM msg.PrevLogIdx
          return response
        else
          return! processEntry nid msg resp
    }

  let inline private logAppendEntries node msg =
    raft {
      if Option.isSome msg.Entries then
        let! state = get
        let dbg =
          sprintf "recvd appendentries from: %A, t:%d ci:%d lc:%d pli:%d plt:%d #%d"
            node
            msg.Term
            (currentIndex state)
            msg.LeaderCommit
            msg.PrevLogIdx
            msg.PrevLogTerm
            (Option.get msg.Entries |> LogEntry.depth)
        do! log state.Node dbg
    }

  let receiveAppendEntries (nid: NodeId option) (msg: AppendEntries<'d,'n>) =
    raft {
      do! setTimeoutElapsedM 0UL       // reset, so we don't start election
      do! logAppendEntries nid msg    // let the world know
      let! result = makeResponse msg  // check terms et al match, fail otherwise

      match result with
        | Right resp ->
          let! state = get
          // this is not the first AppendEntry we're reeiving
          if msg.PrevLogIdx > 0UL then
            match getEntryAt msg.PrevLogIdx state with
              | Some entry ->
                return! checkAndProcess entry nid msg resp
              | None ->
                do! log state.Node (logNodeNotFound msg)
                return resp
          else
            return! processEntry nid msg resp
        | Middle v -> return! v
        | Left err -> return err
    }

  /////////////////////////////////////////////////////////////////////////////
  //     _                               _ _____       _        _            //
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___  //
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __| //
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \ //
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/ //
  //         |_|   |_|                                                       //
  /////////////////////////////////////////////////////////////////////////////

  let inline private makeRequest nextIdx =
    raft {
      let! state = get
      let entries = getEntriesUntil nextIdx state
      let response = { Term         = state.CurrentTerm
                     ; PrevLogIdx   = 0UL
                     ; PrevLogTerm  = 0UL
                     ; LeaderCommit = state.CommitIndex
                     ; Entries      = entries }

      if nextIdx > 1UL then
        let! result = getEntryAtM (nextIdx - 1UL)
        return { response with
                   PrevLogIdx = nextIdx - 1UL
                   PrevLogTerm =
                      match result with
                        | Some(entry) -> LogEntry.term entry
                        | _           -> response.Term }
      else
        return response
    }

  let inline private logRequest (node : Node<'n>) (request: AppendEntries<'d,'n>) =
    raft {
        let! state = get
        let msg =
          sprintf "sending appendentries [node: %A] [ci: %d] [term: %d] [lc: %d] [pli: %d] [plt: %d]"
            node.Id
            (currentIndex state)
            request.Term
            request.LeaderCommit
            request.PrevLogIdx
            request.PrevLogTerm
        do! log state.Node msg
      }


  let inline private maybeFailStaleResponse (resp : AppendResponse) node =
    raft {
        if resp.CurrentIndex <> 0UL && resp.CurrentIndex <= node.MatchIndex then
          return! failM StaleResponse
      }

  //  If response contains term T > currentTerm: set currentTerm = T
  //  and convert to follower (§5.3)
  let inline private maybeFailWrongTerm (resp : AppendResponse) =
    raft {
        let! state = get
        if currentTerm state < resp.Term then
          do! setTermM resp.Term
          do! becomeFollower ()
        else if currentTerm state <> resp.Term then
          return! stopM ()
      }

  let inline private updateNodeIndices (resp : AppendResponse) (node : Node<'n>) =
    raft {
      let! state = get

      let peer =
        { node with
            NextIndex  = resp.CurrentIndex + 1UL
            MatchIndex = resp.CurrentIndex }

      let notVoting = not (Node.isVoting peer)
      let notLogs   = not (Node.hasSufficientLogs peer)
      let notCfg    = Option.isNone state.ConfigChangeEntry
      let idxOk     = currentIndex state <= resp.CurrentIndex + 1UL

      if notVoting && notCfg && idxOk && notLogs then
        let updated = Node.setHasSufficientLogs peer
        do! hasSufficientLogs updated
        do! updateNodeM updated
      else
        do! updateNodeM peer
    }

  let inline private shouldCommit peers state resp =
    let folder (votes : Long) nid (node : Node<'n>) =
      if nid = state.Node.Id || not (Node.isVoting node) then
        votes
      elif node.MatchIndex > 0UL then
        match getEntryAt node.MatchIndex state with
          | Some entry ->
            if LogEntry.term entry = state.CurrentTerm &&
              resp.CurrentIndex <= node.MatchIndex
            then votes + 1UL
            else votes
          | _ -> votes
      else votes

    let commit = commitIndex state
    let num = countNodes peers
    let votes = Map.fold folder 1UL peers

    (num / 2UL) < votes && commit < resp.CurrentIndex

  let inline private updateCommitIndex (resp : AppendResponse) =
    raft {
      let! state = get

      let commitOk =
        if inJointConsensus state then
          // handle the joint consensus case
          match state.OldPeers with
            | Some peers ->
              let older = shouldCommit peers       state resp
              let newer = shouldCommit state.Peers state resp
              older || newer
            | _ -> shouldCommit state.Peers state resp
        else
          // the base case, not in joint consensus
          shouldCommit state.Peers state resp

      if commitOk then
        do! setCommitIndexM resp.CurrentIndex
    }

  let sendAppendEntry (peer: Node<_>) =
    raft {
      let nextIdx = peer.NextIndex
      let! request = makeRequest nextIdx
      do! logRequest peer request
      do! sendAppendEntriesM peer request
    }

  let inline private sendRemainingEntries peerid =
    raft {
      let! peer = getNodeM peerid
      match peer with
        | Some node ->
          let! entry = getEntryAtM (Node.getNextIndex node)
          if Option.isSome entry then
            do! sendAppendEntry node
        | _ -> ()
    }

  let inline private maybeFailNotLeader _ =
    raft {
      let! leader = isLeaderM ()
      if not leader then
        return! failM NotLeader
    }

  let rec receiveAppendEntriesResponse (nid : NodeId) resp =
    raft {
      let! state = get

      let msg =
        sprintf "appendentries response [status: %s]"
          (if resp.Success then "SUCCESS" else "fail")
      do! log state.Node msg

      let! node = getNodeM nid
      match node with
        | None -> return! failM NoNode
        | Some peer ->
          return! maybeFailNotLeader ()
          return! maybeFailStaleResponse resp peer
          return! maybeFailWrongTerm resp
          return! handleUnsuccessful resp peer
          do! updateNodeIndices resp peer
          do! updateCommitIndex resp
          do! sendRemainingEntries peer.Id
    }

  and handleUnsuccessful (resp : AppendResponse) node =
    raft {
      if not resp.Success then

        // If AppendEntries fails because of log inconsistency:
        // decrement nextIndex and retry (§5.3)
        if resp.CurrentIndex < node.NextIndex - 1UL then
          let! idx = currentIndexM ()
          let nextIndex = min (resp.CurrentIndex + 1UL) idx
          do! setNextIndexM node.Id nextIndex
          do! sendAppendEntry { node with NextIndex = nextIndex }
        else
          let nextIndex = node.NextIndex - 1UL
          do! setNextIndexM node.Id nextIndex
          do! sendAppendEntry { node with NextIndex = nextIndex }

        return! stopM ()
    }

  let sendAllAppendEntriesM _ =
    raft {
      let! self = getSelfM ()
      let! peers = logicalPeersM ()
      for peer in peers do
        if peer.Value.Id <> self.Id then
          do! sendAppendEntry peer.Value
      do! setTimeoutElapsedM 0UL
    }

  ///////////////////////////////////////////////////
  //  ____                        _           _    //
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_  //
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __| //
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_  //
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__| //
  //                   |_|                         //
  ///////////////////////////////////////////////////

  /// utiltity to create a snapshot for the current application and raft state
  let createSnapshot (data: 'data) (state: Raft<'data,_>) =
    let peers = Map.toArray state.Peers |> Array.map snd
    Log.snapshot peers data state.Log

  let sendInstallSnapshot node =
    raft {
      let! cbs = read
      let! state = get
      match cbs.RetrieveSnapshot () with
        | Some (Snapshot(_,idx,term,_,_,_,_) as snapshot) ->
          let is =
            { Term      = state.CurrentTerm
            ; LeaderId  = state.Node.Id
            ; LastIndex = idx
            ; LastTerm  = term
            ; Data      = snapshot
            }
          cbs.SendInstallSnapshot node is
        | _ -> ()
    }

  ///////////////////////////////////////////////////////////////////
  //  ____               _             _____       _               //
  // |  _ \ ___  ___ ___(_)_   _____  | ____|_ __ | |_ _ __ _   _  //
  // | |_) / _ \/ __/ _ \ \ \ / / _ \ |  _| | '_ \| __| '__| | | | //
  // |  _ <  __/ (_|  __/ |\ V /  __/ | |___| | | | |_| |  | |_| | //
  // |_| \_\___|\___\___|_| \_/ \___| |_____|_| |_|\__|_|   \__, | //
  //                                                        |___/  //
  ///////////////////////////////////////////////////////////////////

  /// Check if an entry corresponding to a receiveEntry result has actually been
  /// committed to the state machine.
  let responseCommitted (resp : EntryResponse) =
    raft {
      let! entry = getEntryAtM resp.Index
      match entry with
        | None -> return false
        | Some entry ->
          if resp.Term <> LogEntry.term entry
          then return! failM EntryInvalidated
          else
            let! cidx = commitIndexM ()
            return resp.Index <= cidx
    }

  let inline private maybeUnexpectedConfigChange (log : LogEntry<'d,'n>) =
    raft {
        let! state = get
        if LogEntry.isConfigChange log && Option.isSome state.ConfigChangeEntry then
          return! failM UnexpectedVotingChange
      }

  let inline private updateCommitIdx (log : LogEntry<'d,'n>) (state: Raft<'d,'n>) =
    { state with
        CommitIndex =
          if state.NumNodes = 1UL
          then currentIndex state
          else state.CommitIndex
        ConfigChangeEntry =
          match log with
            | JointConsensus _ -> Some log
            | Configuration  _ -> state.ConfigChangeEntry // keep track of JointConsensus change
            |                _ -> None }

  let inline private handleLog entry resp =
    raft {
      let! result = appendEntryM entry

      match result with
        | Some appended ->
          let! state = get
          let! peers = logicalPeersM ()

          // iterate through all peers and call sendAppendEntries to each
          for peer in peers do
            let node = peer.Value
            if node.Id <> state.Node.Id then
              let nxtidx = Node.getNextIndex node
              let! cidx = currentIndexM ()

              if cidx - nxtidx <= (state.MaxLogDepth + 1UL) then
                // Only send new entries. Don't send the entry to peers who are
                // behind, to prevent them from becoming congested.
                do! sendAppendEntry node
              else
                // because this is a new node in the cluster get it up to speed
                // with a snapshot
                do! sendInstallSnapshot node

          do! updateCommitIdx appended |> modify

          return! currentTermM () >>= fun term ->
                    returnM { resp with
                                Id = LogEntry.id appended
                                Term = term
                                Index = Log.entryIndex appended }
        | _ ->
          return! failM AppendEntryFailed
      }

  ///                    _           _____       _
  ///  _ __ ___  ___ ___(_)_   _____| ____|_ __ | |_ _ __ _   _
  /// | '__/ _ \/ __/ _ \ \ \ / / _ \  _| | '_ \| __| '__| | | |
  /// | | |  __/ (_|  __/ |\ V /  __/ |___| | | | |_| |  | |_| |
  /// |_|  \___|\___\___|_| \_/ \___|_____|_| |_|\__|_|   \__, |
  ///                                                     |___/

  let receiveEntry (entry : LogEntry<'d,'n>) =
    raft {
      let! state = get
      let resp = { Id = RaftId.Create(); Term = 0UL; Index = 0UL }

      return! maybeUnexpectedConfigChange entry
      return! maybeFailNotLeader ()

      let msg =
        sprintf "received entry t:%d id: %s idx: %d"
          state.CurrentTerm
          ((LogEntry.id entry).ToString() )
          (Log.index state.Log + 1UL)

      do! log state.Node msg

      let! term = currentTermM ()

      match entry with
        | LogEntry(id,_,_,data,_) ->
          let log = LogEntry(id, 0UL, term, data, None)
          return! handleLog log resp

        | Configuration(id,_,_,nodes,_) ->
          let log = Configuration(id, 0UL, term, nodes, None)
          return! handleLog log resp

        | JointConsensus(id,_,_,changes,_) ->
          let log = JointConsensus(id, 0UL, term, changes, None)
          return! handleLog log resp

        | _ -> return! failM LogFormatError
    }

  ////////////////////////////////////////////////////////////////
  //     _                _         _____       _               //
  //    / \   _ __  _ __ | |_   _  | ____|_ __ | |_ _ __ _   _  //
  //   / _ \ | '_ \| '_ \| | | | | |  _| | '_ \| __| '__| | | | //
  //  / ___ \| |_) | |_) | | |_| | | |___| | | | |_| |  | |_| | //
  // /_/   \_\ .__/| .__/|_|\__, | |_____|_| |_|\__|_|   \__, | //
  //         |_|   |_|      |___/                        |___/  //
  ////////////////////////////////////////////////////////////////
  let selfIncluded (state: Raft<_,_>) =
    Map.containsKey state.Node.Id state.Peers

  let maybeResetFollowerM entries =
    raft {
      if Log.containsEntry Log.isConfiguration entries then
        let! included = selfIncluded |> zoomM
        if not included then
          do! becomeFollower ()
    }

  let updateLogIdx logIdx (state: Raft<'d,'n>) =
    { state with
        LastAppliedIdx = logIdx
        ConfigChangeEntry =
          match state.ConfigChangeEntry with
            | Some entry when Log.entryIndex entry = logIdx ->  None
            | _ -> state.ConfigChangeEntry  }

  let updateLogIdxM logIdx =
    updateLogIdx logIdx |> modify

  let applyEntry (cbs: IRaftCallbacks<_,_>) _ = function
    | JointConsensus(_,_,_,changes,_) ->
      let inline applyChange change =
        match change with
          | NodeAdded(node)   -> cbs.NodeAdded   node
          | NodeRemoved(node) -> cbs.NodeRemoved node
      Array.iter applyChange changes
    | Configuration(_,_,_,nodes,_) -> cbs.Configured nodes
    | LogEntry(_,_,_,data,_)       -> cbs.ApplyLog data
    | Snapshot(_,_,_,_,_,_,data)   -> cbs.ApplyLog data

  let applyEntries _ =
    raft {
      let! state = get
      let lai = state.LastAppliedIdx
      let coi = state.CommitIndex
      if lai <> coi then
        let logIdx = lai + 1UL
        let! result = getEntriesUntilM logIdx
        match result with
          | Some entries ->
            do! log state.Node "Applying Entries"
            let! cbs = read
            // Apply log chaihn in the order it arrived
            Log.foldr (applyEntry cbs) () entries
            do! maybeResetFollowerM entries
            do! updateLogIdxM (Log.entryIndex entries)
          | _ -> ()
    }

  (*
   *  ____               _
   * |  _ \ ___  ___ ___(_)_   _____
   * | |_) / _ \/ __/ _ \ \ \ / / _ \
   * |  _ <  __/ (_|  __/ |\ V /  __/
   * |_| \_\___|\___\___|_| \_/ \___|
   *
   *  ___           _        _ _ ____                        _           _
   * |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
   *  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
   *  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
   * |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
   *                                              |_|
   * +-------------------------------------------------------------------------------------------------------------------------------+
   * | 1. Reply immediately if term < currentTerm                                                                                    |
   * | 2. Create new snapshot file if first chunk (offset is 0)                                                                      |
   * | 3. Write data into snapshot file at given offset                                                                              |
   * | 4. Reply and wait for more data chunks if done is false                                                                       |
   * | 5. Save snapshot file, discard any existing or partial snapshot with a smaller index                                          |
   * | 6. If existing log entry has same index and term as snapshot’s last included entry, retain log entries following it and reply |
   * | 7. Discard the entire log                                                                                                     |
   * | 8. Reset state machine using snapshot contents (and load snapshot’s cluster configuration)                                    |
   * +-------------------------------------------------------------------------------------------------------------------------------+
   *)
  let receiveInstallSnapshot (is: InstallSnapshot<_,_>) =
    raft {
      let! cbs = read
      let! currentTerm = currentTermM ()

      if is.Term < currentTerm
      then return! failM InvalidTerm

      do! setTimeoutElapsedM 0UL

      // IMPROVEMENT: implementent chunked transmission as per paper
      cbs.PersistSnapshot is.Data

      match is.Data with
        | Snapshot(_,idx,_,_,_,nodes,_) ->
          let! state = get

          let! remaining = entriesUntilExcludingM idx

          // update the cluster configuration
          let peers =
            Array.map (fun (n: Node<_>) -> (n.Id, n)) nodes
            |> Map.ofArray
            |> Map.add state.Node.Id state.Node

          do! setPeersM peers

          // update log with snapshot and possibly merge existing entries
          match remaining with
            | Some entries ->
              do! updateLog (Log.empty
                            |> Log.append is.Data
                            |> Log.append entries)
                  |> modify
            | _ ->
              do! updateLogEntries is.Data |> modify

          // set the current leader to node which sent snapshot
          do! setLeaderM (Some is.LeaderId)

          // apply all entries in the new log
          let! state = get
          match state.Log.Data with
            | Some data ->
              Log.foldr (applyEntry cbs) () data
            | _ -> failwith "Fatal. Snapshot applied, but log is empty. Aborting."

          // reset the counters,to apply all entries in the log
          do! setLastAppliedIdxM 0UL
          do! updateLogIdxM (Log.index state.Log)
          do! setCommitIndexM (Log.index state.Log)

          // cosntruct reply
          let! term = currentTermM ()
          return { Term = term }
        | _ -> return! failM SnapshotFormatError
    }

  let maybeSnapshot _ =
    raft {
      let! state = get
      if Log.size state.Log >= state.MaxLogDepth then
        let! cbs = read
        let! state = get
        let snapshot =  cbs.PrepareSnapshot state
        do! updateLog snapshot |> modify
        cbs.PersistSnapshot (snapshot.Data |> Option.get)
    }

  ///////////////////////////////////////////////
  //  _____ _           _   _                  //
  // | ____| | ___  ___| |_(_) ___  _ __  ___  //
  // |  _| | |/ _ \/ __| __| |/ _ \| '_ \/ __| //
  // | |___| |  __/ (__| |_| | (_) | | | \__ \ //
  // |_____|_|\___|\___|\__|_|\___/|_| |_|___/ //
  ///////////////////////////////////////////////

  /// ## majority
  ///
  /// Determine the majority from a total number of eligible voters and their respective votes. This
  /// function is generic and should expect any numeric types.
  ///
  /// Turning off the warning about the cast due to sufficiently constrained requirements on the
  /// input type (op_Explicit, comparison and division).
  ///
  /// ### Signature:
  /// - total: the total number of votes cast
  /// - yays: the number of yays in this election
  ///
  /// Returns: boolean
  let majority total yays =
    if total = 0UL || yays = 0UL then
      false
    elif yays > total then
      false
    else
      yays > (total / 2UL)

  /// Determine whether a vote count constitutes a majority in the *regular*
  /// configuration (does not cover the joint consensus state).
  let regularMajorityM votes =
    votingNodesM () >>= fun num ->
      majority num votes |> returnM

  let oldConfigMajorityM votes =
    votingNodesForOldConfigM () >>= fun num ->
      majority num votes |> returnM

  let numVotesForConfig (self: Node<_>) (votedFor: NodeId option) peers =
    let counter m _ (peer : Node<'n>) =
        if (peer.Id <> self.Id) && Node.canVote peer
          then m + 1UL
          else m

    let start =
      match votedFor with
        | Some(nid) -> if nid = self.Id then 1UL else 0UL
        | _         -> 0UL

    Map.fold counter start peers

  let numVotesForMe (state: Raft<'d,'n>) =
    numVotesForConfig state.Node state.VotedFor state.Peers

  let numVotesForMeM _ = zoomM numVotesForMe

  let numVotesForMeOldConfig (state: Raft<_,_>) =
    match state.OldPeers with
      | Some peers -> numVotesForConfig state.Node state.VotedFor peers
      |      _     -> 0UL

  let numVotesForMeOldConfigM _ = zoomM numVotesForMeOldConfig

  /////////////////////////////////////////////////////////////////////////////
  //  ____                                  _                   _            //
  // | __ )  ___  ___ ___  _ __ ___   ___  | |    ___  __ _  __| | ___ _ __  //
  // |  _ \ / _ \/ __/ _ \| '_ ` _ \ / _ \ | |   / _ \/ _` |/ _` |/ _ \ '__| //
  // | |_) |  __/ (_| (_) | | | | | |  __/ | |__|  __/ (_| | (_| |  __/ |    //
  // |____/ \___|\___\___/|_| |_| |_|\___| |_____\___|\__,_|\__,_|\___|_|    //
  /////////////////////////////////////////////////////////////////////////////

  let inline private maybeSetIndex nid nextIdx matchIdx =
    let mapper peer =
      if Node.isVoting peer && peer.Id <> nid
      then { peer with NextIndex = nextIdx; MatchIndex = matchIdx }
      else peer
    updatePeersM mapper

  /// Become leader afer a successful election
  let becomeLeader _ =
    raft {
        let! state = get
        do! log state.Node "Becoming Leader"
        let nid = currentIndex state + 1UL
        do! setStateM Leader
        do! maybeSetIndex state.Node.Id nid 0UL
        do! sendAllAppendEntriesM ()
      }

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Send ->                    _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let receiveVoteResponse (nid : NodeId) (vote : VoteResponse) =
    raft {
        let msg =
          sprintf "node responded to requestvote status: %s"
            (if vote.Granted then "granted" else "not granted")

        let! state = get
        do! log state.Node msg

        /// The term must not be bigger than current raft term,
        /// otherwise set term to vote term become follower.
        if vote.Term > state.CurrentTerm then
          do! setTermM vote.Term
          do! becomeFollower ()

        /// If the vote term is smaller than current term it is considered an
        /// error and the client will be notified.
        elif vote.Term < state.CurrentTerm then
          return! failM VoteTermMismatch

        /// Process the vote if current state of our Raft must be candidate..
        elif state.State = Candidate then

          if vote.Granted then
            let! node = getNodeM nid
            match node with
              // Could not find the node in current configuration(s)
              | None -> return! failM NoNode
              // found the node
              | Some node ->
                do! setVotingM node true

                let! transitioning = inJointConsensusM ()

                // in joint consensus
                if transitioning then
                  //      _       _       _
                  //     | | ___ (_)_ __ | |_
                  //  _  | |/ _ \| | '_ \| __|
                  // | |_| | (_) | | | | | |_
                  //  \___/ \___/|_|_| |_|\__| consensus.
                  //
                  // we probe for a majority in both configurations
                  let! newConfig =
                    numVotesForMeM () >>= regularMajorityM

                  let! oldConfig =
                    numVotesForMeOldConfigM () >>= oldConfigMajorityM

                  // and finally, become leader if we have a majority in either
                  // configuration
                  if newConfig || oldConfig then
                    do! becomeLeader ()
                else
                  //  ____                  _
                  // |  _ \ ___  __ _ _   _| | __ _ _ __
                  // | |_) / _ \/ _` | | | | |/ _` | '__|
                  // |  _ <  __/ (_| | |_| | | (_| | |
                  // |_| \_\___|\__, |\__,_|_|\__,_|_| configuration.
                  //            |___/
                  // the base case: we are not in joint consensus so we just use
                  // regular configuration functions
                  let! majority =
                    numVotesForMeM () >>= regularMajorityM

                  if majority then
                    do! becomeLeader ()

        /// ...otherwise we respond with the respective RaftError.
        else
          return! failM NotCandidate
      }

  /// Request a from a given peer
  let sendVoteRequest (node : Node<'n>) =
    raft {
        if Node.isVoting node && node.State = Running then
          let! state = get
          let! cbs = read

          let vote =
            { Term         = state.CurrentTerm
            ; Candidate    = state.Node
            ; LastLogIndex = Log.index state.Log
            ; LastLogTerm  = Log.term  state.Log
            }

          do! log state.Node
            <| sprintf "sending requestvote for %s (me) to: %s [status: %A]"
                (state.Node.Id.ToString())
                (node.Id.ToString())
                (node.State)

          sendRequestVote cbs node vote
      }

  let requestAllVotes _ =
    raft {
        let! self = getSelfM ()
        let! peers = logicalPeersM ()

        do! log self "requesting all votes"

        for peer in peers do
          if self.Id <> peer.Value.Id then
            do! sendVoteRequest peer.Value
      }

  ///////////////////////////////////////////////////////
  //   ____  Should I?       _    __     __    _       //
  //  / ___|_ __ __ _ _ __ | |_  \ \   / /__ | |_ ___  //
  // | |  _| '__/ _` | '_ \| __|  \ \ / / _ \| __/ _ \ //
  // | |_| | | | (_| | | | | |_    \ V / (_) | ||  __/ //
  //  \____|_|  \__,_|_| |_|\__|    \_/ \___/ \__\___| //
  ///////////////////////////////////////////////////////

  /// if the vote's term is lower than this servers current term,
  /// decline the vote
  let inline private validateTerm (vote: VoteRequest<_>) state =
    (vote.Term < state.CurrentTerm, InvalidTerm)

  let inline private alreadyVoted (state: Raft<'d,'n>) =
    (Option.isSome state.VotedFor, AlreadyVoted)

  let inline private validateLastLog vote state =
    let result =
         vote.LastLogTerm   = lastLogTerm state
      && currentIndex state <= vote.LastLogIndex
    (result,InvalidLastLog)

  let inline private validateLastLogTerm vote state =
    (lastLogTerm state < vote.LastLogTerm, InvalidLastLogTerm)

  let inline private validateCurrentIdx state =
    (currentIndex state = 0UL, InvalidCurrentIndex)

  let inline private validateCandidate (vote: VoteRequest<_>) state =
    (getNode vote.Candidate.Id state |> Option.isNone, CandidateUnknown)

  let shouldGrantVote (vote : VoteRequest<_>) =
    raft {
      let! state = get
      let result =
        validation {       // predicate               result  input
          return! validate (validateTerm vote)        false   state
          return! validate alreadyVoted               false   state
          return! validate (validateCandidate vote)   false   state
          return! validate validateCurrentIdx         true    state
          return! validate (validateLastLogTerm vote) true    state
          return! validate (validateLastLog vote)     true    state
          return (false, LogIncomplete)
        }
        |> runValidation
      do! log state.Node (sprintf "grant vote result: %b" (fst result))
      return result
    }

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Receive->                  _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let inline private maybeResetFollower (vote : VoteRequest<_>) =
    raft {
      let! term = currentTermM ()
      if term < vote.Term then
        do! setTermM vote.Term
        do! becomeFollower ()
        do! voteFor None
    }

  let inline private processVoteRequest (vote : VoteRequest<_>) =
    raft {
      let! result = shouldGrantVote vote
      match result with
        | (true,_) ->
          let! leader = isLeaderM ()
          let! candidate = isCandidateM ()
          if not leader && not candidate then
            do! voteForId vote.Candidate.Id
            do! setLeaderM None
            do! setTimeoutElapsedM 0UL
            let! term = currentTermM ()
            return { Term    = term
                     Granted = true
                     Reason  = None }
          else
            return! failM NotVotingState
        | (false, err) ->
          let! term = currentTermM ()
          return { Term    = term
                   Granted = false
                   Reason  = Some err }
    }

  let receiveVoteRequest (nid : NodeId) (vote : VoteRequest<_>) =
    raft {
      let! node = getNodeM nid
      match node with
        | Some _ ->
          do! maybeResetFollower vote
          let! result = processVoteRequest vote
          let! state = get
          do! log state.Node (sprintf "node requested vote: %A granted: true" nid)
          return result
        | _ ->
          let! term = currentTermM ()
          return { Term    = term
                   Granted = false
                   Reason  = Some NoNode }
    }

  //  ____
  // | __ )  ___  ___ ___  _ __ ___   ___
  // |  _ \ / _ \/ __/ _ \| '_ ` _ \ / _ \
  // | |_) |  __/ (_| (_) | | | | | |  __/
  // |____/ \___|\___\___/|_| |_| |_|\___|
  //   ____                _ _     _       _
  //  / ___|__ _ _ __   __| (_) __| | __ _| |_ ___
  // | |   / _` | '_ \ / _` | |/ _` |/ _` | __/ _ \
  // | |__| (_| | | | | (_| | | (_| | (_| | ||  __/
  //  \____\__,_|_| |_|\__,_|_|\__,_|\__,_|\__\___|

  /// After timeout a Node must become Candidate
  let becomeCandidate _ =
    raft {
      let! state = get
      do! log state.Node "Becoming candidate."
      do! setTermM (state.CurrentTerm + 1UL)
      do! resetVotesM ()
      do! voteForMyself ()
      do! setLeaderM None
      do! setStateM Candidate
      do! setTimeoutElapsedM (uint64(_rand.Next()) % state.ElectionTimeout)
      do! requestAllVotes ()
    }

  //  ____  _             _     _____ _           _   _
  // / ___|| |_ __ _ _ __| |_  | ____| | ___  ___| |_(_) ___  _ __
  // \___ \| __/ _` | '__| __| |  _| | |/ _ \/ __| __| |/ _ \| '_ \
  //  ___) | || (_| | |  | |_  | |___| |  __/ (__| |_| | (_) | | | |
  // |____/ \__\__,_|_|   \__| |_____|_|\___|\___|\__|_|\___/|_| |_|

  /// start an election by becoming candidate
  let startElection _ =
    raft {
      let! state = get
      let msg =
        sprintf "election starting: %d %d, term: %d ci: %d"
          state.ElectionTimeout
          state.TimeoutElapsed
          state.CurrentTerm
          (currentIndex state)
      do! log state.Node msg
      do! becomeCandidate ()
    }

  //  ____           _           _ _
  // |  _ \ ___ _ __(_) ___   __| (_) ___
  // | |_) / _ \ '__| |/ _ \ / _` | |/ __|
  // |  __/  __/ |  | | (_) | (_| | | (__
  // |_|   \___|_|  |_|\___/ \__,_|_|\___|

  let private checkIsLeader state =
    raft {
      match state with
        | Leader ->
          let! timedout = requestTimedOutM ()
          if timedout then
            do! sendAllAppendEntriesM ()
        | _ ->
          let! num = numNodesM ()
          let! timedout = electionTimedOutM ()
          if timedout && num > 1UL then
            do! startElection ()
    }

  let periodic (elapsed : Long) =
    raft {
      let! state = get
      do! setTimeoutElapsedM (state.TimeoutElapsed + elapsed)
      do! checkIsLeader state.State
      let! coi = commitIndexM ()
      let! lai = lastAppliedIdxM ()
      if lai < coi then
        do! applyEntries ()
      do! maybeSnapshot ()
    }
