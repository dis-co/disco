namespace Iris.Raft

open System
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module RaftMonad =
  let warn str = printfn "[RAFT WARNING] %s" str

  /// get current Raft state
  let get = MkRM (fun _ s -> Right (s, s))

  /// update Raft/State to supplied value
  let put s = MkRM (fun _ _ -> Right ((), s))

  /// get the read-only environment value
  let read : RaftM<_,_,_,_> = MkRM (fun l s -> Right (l, s))

  /// unwrap the closure and apply it to the supplied state/env
  let apply (env: 'e) (state: 's) (m: RaftMonad<'e,'s,_,_>)  =
    match m with | MkRM func -> func env state

  /// run the monadic action against state and environment values
  let runRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    apply l s m

  /// run monadic action against supplied state and evironment and return new state
  let evalRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    match runRaft s l m with
      | Right (_,state) | Left (_,state) -> state

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Right` value. This means the computation will,
  /// if possible, continue to the next step.
  let returnM value : RaftMonad<'e,'s,'t,'err> =
    MkRM (fun _ state -> Right(value, state))

  let ignoreM _ : RaftMonad<'e,'s,unit,'err> =
    MkRM (fun _ state -> Right((), state))

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Left` value. This means the computation will
  /// not continue past this step and no regular value will be returned.
  let failM l =
    MkRM (fun _ s -> Left (l, s))

  /// pass through the given action
  let returnFromM func : RaftMonad<'e,'s,'t,'err> =
    func

  let zeroM () =
    MkRM (fun _ state -> Right((), state))

  let delayM (f: unit -> RaftMonad<'e,'s,'t,'err>) =
    MkRM (fun env state -> f () |> apply env state)

  /// Chain up effectful actions.
  let bindM (m: RaftMonad<'env,'state,'a,'err>)
            (f: 'a -> RaftMonad<'env,'state,'b,'err>) :
            RaftMonad<'env,'state,'b,'err> =
    MkRM (fun env state ->
          match apply env state m with
            | Right  (value,state') -> f value |> apply env state'
            | Left    err           -> Left err)

  let (>>=) = bindM

  let combineM (m1: RaftMonad<_,_,_,_>) (m2: RaftMonad<_,_,_,_>) =
    bindM m1 (fun _ -> m2)

  let tryWithM (body: RaftMonad<_,_,_,_>) (handler: exn -> RaftMonad<_,_,_,_>) =
    MkRM (fun env state ->
          try apply env state body
          with ex -> apply env state (handler ex))

  let tryFinallyM (body: RaftMonad<_,_,_,_>) handler : RaftMonad<_,_,_,_> =
    MkRM (fun env state ->
          try apply env state body
          finally handler ())

  let usingM (resource: ('a :> System.IDisposable)) (body: 'a -> RaftMonad<_,_,_,_>) =
    tryFinallyM (body resource)
      (fun _ -> if not <| isNull (box resource)
                then resource.Dispose())

  let rec whileM (guard: unit -> bool) (body: RaftMonad<_,_,_,_>) =
    match guard () with
      | true -> bindM body (fun _ -> whileM guard body)
      | _ -> zeroM ()

  let rec forM (sequence: seq<_>) (body: 'a -> RaftMonad<_,_,_,_>) : RaftMonad<_,_,_,_> =
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
  let currentIndex (state: Raft<'d,'n>) =
    Log.index state.Log

  let log level str =
    read >>= fun cbs ->
      get >>= fun state ->
        cbs.LogMsg level state.Node str |> returnM

  let debug str = log Debug str

  let info str = log Info str

  let warn str = log Warn str

  let error str = log Err str

  let sendAppendEntriesM (node: Node<_>) (request: AppendEntries<_,_>) =
    get >>= fun state ->
      read >>= fun cbs ->
        async {
          let msg =
            sprintf "SendAppendEntries: (to: %s) (ci: %d) (term: %d) (leader commit: %d) (prv log idx: %d) (prev log term: %d)"
              (string node.Id)
              (currentIndex state)
              request.Term
              request.LeaderCommit
              request.PrevLogIdx
              request.PrevLogTerm

          cbs.LogMsg Debug state.Node msg

          let result = cbs.SendAppendEntries node request
          return result
        }
        |> returnM

  let persistVote node =
    read >>= fun cbs ->
      cbs.PersistVote node
      |> returnM

  let persistTerm term =
    read >>= fun cbs ->
      cbs.PersistTerm term
      |> returnM

  let persistLog log =
    read >>= fun cbs ->
      cbs.PersistLog log
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
    ; ElectionTimeout   = 4000UL         // msec
    ; RequestTimeout    = 500UL          // msec
    ; MaxLogDepth       = 50UL           // items
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

  let hasNonVotingNodes (state: Raft<_,_>) =
    Map.fold
      (fun b _ n -> if b then b else not (Node.hasSufficientLogs n && Node.isVoting n))
      false
      state.Peers

  let hasNonVotingNodesM _ = zoomM hasNonVotingNodes

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

  let setNodeStateM (nid: NodeId) state =
    getNodeM nid >>=
      fun result ->
        match result with
        | Some node -> updateNodeM { node with State = state }
        | _         -> returnM ()

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
    raft {
      do! setTerm term |> modify
      do! persistTerm term
    }

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
  let getState (state: Raft<'d,'n>) =
    state.State

  /// Get current RaftState. Monadic action.
  let getStateM _ = zoomM getState

  let getMaxLogDepth (state: Raft<'d,'n>) =
    state.MaxLogDepth

  let getMaxLogDepthM _ = zoomM getMaxLogDepth

  let setMaxLogDepth (depth: Long) (state: Raft<'d,'n>) =
    { state with MaxLogDepth = depth }

  let setMaxLogDepthM (depth: Long) =
    setMaxLogDepth depth |> modify

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
    let nextidx = if idx < 1UL then 1UL else idx
    match node with
      | Some node -> updateNode { node with NextIndex = nextidx } cbs state
      | _         -> state

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
      | Some peer -> updateNode { peer with MatchIndex = idx } env state
      | _         -> state

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

  let setVotingM (node: Node<_>) (vote: bool) =
    raft {
      do! debug <| sprintf "setVotingM: setting node %s voting to %b" (string node.Id) vote
      do! setVoting node vote |> modify
    }

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

  let setTimeoutElapsed (elapsed: Long) (state: Raft<'d,'n>) =
    { state with TimeoutElapsed = elapsed }

  let setTimeoutElapsedM (elapsed: Long) =
    setTimeoutElapsed elapsed |> modify

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
      do! info "becoming follower"
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

      // create the new log by appending
      let newlog = Log.append log state.Log
      do! put { state with Log = newlog }

      // get back the entries just added
      // (with correct monotonic idx's)
      return Log.getn (Log.depth log) newlog
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
      let log = LogEntry(Id.Create(),0UL,state.CurrentTerm,d,None)
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
  let private makeResponse (msg : AppendEntries<'d,'n>) =
    raft {
      let! state = get

      let term = currentTerm state
      let current = currentIndex state
      let first =
        match firstIndex term state with
        | Some idx -> idx
        | _        -> 0UL

      let resp =
        { Term         = term
        ; Success      = false
        ; CurrentIndex = current
        ; FirstIndex   = first }

      // 1) If this node is currently candidate and both its and the requests
      // term are equal, we become follower and reset VotedFor.
      if isCandidate state && currentTerm state = msg.Term then
        do! voteFor None
        do! becomeFollower ()
        return Right resp
      // 2) Else, if the current node's term value is lower than the requests
      // term, we take become follower and set our own term to higher value.
      elif currentTerm state < msg.Term then
        do! setTermM msg.Term
        do! becomeFollower ()
        return Right { resp with Term = msg.Term }
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
  let private handleConflicts (request: AppendEntries<'d,'n>) =
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

  let private applyRemainder (msg : AppendEntries<'d,'n>) (resp : AppendResponse) =
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
  let private maybeSetCommitIdx (msg : AppendEntries<'d,'n>) =
    raft {
      let! state = get
      let cmmtidx = commitIndex state
      let ldridx = msg.LeaderCommit
      if cmmtidx < ldridx then
        let lastLogIdx = max (currentIndex state) 1UL
        let newIndex = min lastLogIdx msg.LeaderCommit
        do! setCommitIndexM newIndex
    }

  let private processEntry nid msg resp =
    raft {
      do! handleConflicts msg
      let! response = applyRemainder msg resp
      do! maybeSetCommitIdx msg
      do! setLeaderM nid

      return { response with Success = true }
    }

  ///  2. Reply false if log doesn't contain an entry at prevLogIndex whose
  /// term matches prevLogTerm (§5.3)
  let private checkAndProcess entry nid msg resp =
    raft {
      let! current = currentIndexM ()

      if current < msg.PrevLogIdx then

        do! debug <| sprintf "receiveAppendEntries: Failed (ci: %d) < (prev log idx: %d)"
                      current
                      msg.PrevLogIdx
        return resp
      else
        let term = LogEntry.term entry
        if term <> msg.PrevLogTerm then
          do! debug <| sprintf "receiveAppendEntries: Failed (term %d) != (prev log term %d) (ci: %d) (prev log idx: %d)"
                        term
                        msg.PrevLogTerm
                        current
                        msg.PrevLogIdx
          let response = { resp with CurrentIndex = msg.PrevLogIdx - 1UL }
          do! removeEntryM msg.PrevLogIdx
          return response
        else
          return! processEntry nid msg resp
    }

  let receiveAppendEntries (nid: NodeId option) (msg: AppendEntries<'d,'n>) =
    raft {
      do! setTimeoutElapsedM 0UL      // reset timer, so we don't start an election

      // log this if any entries are to be processed
      if Option.isSome msg.Entries then
        let! current = currentIndexM ()
        do! debug <| sprintf "receiveAppendEntries: (from: %s) (term: %d) (ci: %d) (lc-idx: %d) (pli: %d) (plt: %d) (entries: %d)"
                     (string nid)
                     msg.Term
                     current
                     msg.LeaderCommit
                     msg.PrevLogIdx
                     msg.PrevLogTerm
                     (Option.get msg.Entries |> LogEntry.depth)    // let the world know

      let! result = makeResponse msg  // check terms et al match, fail otherwise

      match result with
        | Right resp ->
          // this is not the first AppendEntry we're reeiving
          if msg.PrevLogIdx > 0UL then
            let! entry = getEntryAtM msg.PrevLogIdx
            match entry with
              | Some log -> return! checkAndProcess log nid msg resp
              | _        ->
                do! debug <| sprintf "receiveAppendEntries: Failed. No log at (prev log idx %d)"
                              msg.PrevLogIdx
                return resp
          else
            return! processEntry nid msg resp
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

  let private updateNodeIndices (resp : AppendResponse) (node : Node<'n>) =
    raft {
      let peer =
        { node with
            NextIndex  = resp.CurrentIndex + 1UL
            MatchIndex = resp.CurrentIndex }

      let! current = currentIndexM ()

      let notVoting = not (Node.isVoting peer)
      let notLogs   = not (Node.hasSufficientLogs peer)
      let idxOk     = current <= resp.CurrentIndex + 1UL

      if notVoting && idxOk && notLogs then
        let updated = Node.setHasSufficientLogs peer
        do! updateNodeM updated
      else
        do! updateNodeM peer
    }

  let private shouldCommit peers state resp =
    let folder (votes : Long) nid (node : Node<'n>) =
      if nid = state.Node.Id || not (Node.isVoting node) then
        votes
      elif node.MatchIndex > 0UL then
        match getEntryAt node.MatchIndex state with
          | Some entry ->
            if LogEntry.term entry = state.CurrentTerm && resp.CurrentIndex <= node.MatchIndex
            then votes + 1UL
            else votes
          | _ -> votes
      else votes

    let commit = commitIndex state
    let num = countNodes peers
    let votes = Map.fold folder 1UL peers

    (num / 2UL) < votes && commit < resp.CurrentIndex

  let private updateCommitIndex (resp : AppendResponse) =
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
            | _ ->
              shouldCommit state.Peers state resp
        else
          // the base case, not in joint consensus
          shouldCommit state.Peers state resp

      if commitOk then
        do! setCommitIndexM resp.CurrentIndex
    }

  let sendAppendEntry (peer: Node<_>) =
    raft {
      let! state = get
      let entries = getEntriesUntil peer.NextIndex state
      let request = { Term         = state.CurrentTerm
                    ; PrevLogIdx   = 0UL
                    ; PrevLogTerm  = 0UL
                    ; LeaderCommit = state.CommitIndex
                    ; Entries      = entries }

      if peer.NextIndex > 1UL then
        let! result = getEntryAtM (peer.NextIndex - 1UL)
        let request = { request with
                          PrevLogIdx = peer.NextIndex - 1UL
                          PrevLogTerm =
                              match result with
                                | Some(entry) -> LogEntry.term entry
                                | _           -> request.Term }
        return! sendAppendEntriesM peer request
      else
        return! sendAppendEntriesM peer request
    }

  let private sendRemainingEntries peerid =
    raft {
      let! peer = getNodeM peerid
      match peer with
        | Some node ->
          let! entry = getEntryAtM (Node.getNextIndex node)
          if Option.isSome entry then
            let! request = sendAppendEntry node
            return Async.RunSynchronously(request)
          else
            return None
        | _ ->
          return None
    }

  let rec receiveAppendEntriesResponse (nid : NodeId) resp =
    raft {
      let! node = getNodeM nid
      match node with
        | None ->
          do! debug <| sprintf "receiveAppendEntriesResponse: Failed: NoNode %s" (string nid)
          return! failM NoNode
        | Some peer ->
          if resp.CurrentIndex <> 0UL && resp.CurrentIndex < peer.MatchIndex then
            do! debug <| sprintf "receiveAppendEntriesResponse: Failed: peer not up to date yet (ci: %d) (match idx: %d)"
                          resp.CurrentIndex
                          peer.MatchIndex
            // set to current index at follower and try again
            do! updateNodeM { peer with
                                NextIndex = resp.CurrentIndex + 1UL
                                MatchIndex = resp.CurrentIndex }
            return ()
          else
            let! state = get

            // we only process this if we are indeed the leader of the pack
            if isLeader state then
              let term = currentTerm state
              //  If response contains term T > currentTerm: set currentTerm = T
              //  and convert to follower (§5.3)
              if term < resp.Term then
                do! debug <| sprintf "receiveAppendEntriesResponse: Failed: (term: %d) < (resp.Term: %d)"
                              term
                              resp.Term
                do! setTermM resp.Term
                do! becomeFollower ()
                return ()
              elif term <> resp.Term then
                do! debug <| sprintf "receiveAppendEntriesResponse: Failed: (term: %d) != (resp.Term: %d)"
                              term
                              resp.Term
                return ()
              elif not resp.Success then
                // If AppendEntries fails because of log inconsistency:
                // decrement nextIndex and retry (§5.3)
                if resp.CurrentIndex < peer.NextIndex - 1UL then
                  let! idx = currentIndexM ()
                  let nextIndex = min (resp.CurrentIndex + 1UL) idx

                  do! debug <| sprintf "receiveAppendEntriesResponse: Failed: cidx < nxtidx. setting nextIndex for %s to %d"
                                (string peer.Id)
                                nextIndex

                  do! setNextIndexM peer.Id nextIndex
                  do! setMatchIndexM peer.Id (nextIndex - 1UL)
                else
                  let nextIndex = peer.NextIndex - 1UL

                  do! debug <| sprintf "receiveAppendEntriesResponse: Failed: cidx >= nxtidx. setting nextIndex for %s to %d"
                                (string peer.Id)
                                nextIndex

                  do! setNextIndexM peer.Id nextIndex
                  do! setMatchIndexM peer.Id (nextIndex - 1UL)
              else
                do! updateNodeIndices resp peer
                do! updateCommitIndex resp
            else
              return! failM NotLeader

    }

  let sendAllAppendEntriesM _ =
    raft {
      let! self = getSelfM ()
      let! peers = logicalPeersM ()

      let requests = ref [| |]

      for peer in peers do
        if peer.Value.Id <> self.Id then
          let! request = sendAppendEntry peer.Value
          requests := Array.append [| (peer.Value, request) |] !requests

      let responses =
        !requests
        |> Array.map snd
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Array.zip (Array.map fst !requests)

      for (node, response) in responses do
        match response with
        | Some resp ->
          do! receiveAppendEntriesResponse node.Id resp
          let! peer = getNodeM node.Id >>= (Option.get >> returnM)
          if peer.State = Failed then
            do! setNodeStateM node.Id Running
        | _ ->
          do! setNodeStateM node.Id Failed

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
    get >>= fun state ->
      read >>= fun cbs ->
        async {
          match cbs.RetrieveSnapshot () with
            | Some (Snapshot(_,idx,term,_,_,_,_) as snapshot) ->
              let is =
                { Term      = state.CurrentTerm
                ; LeaderId  = state.Node.Id
                ; LastIndex = idx
                ; LastTerm  = term
                ; Data      = snapshot
                }
              return cbs.SendInstallSnapshot node is
            | _ -> return None
        }
        |> returnM


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
          if resp.Term <> LogEntry.term entry then return! failM EntryInvalidated
          else
            let! cidx = commitIndexM ()
            return resp.Index <= cidx
    }

  let private updateCommitIdx (state: Raft<'d,'n>) =
    let idx =
      if state.NumNodes = 1UL then
        currentIndex state
      else
        state.CommitIndex
    { state with CommitIndex = idx }

  let private handleLog entry resp =
    raft {
      let! result = appendEntryM entry

      match result with
        | Some appended ->
          let! state = get
          let! peers = logicalPeersM ()

          let requests = ref [| |]

          // iterate through all peers and call sendAppendEntries to each
          for peer in peers do
            let node = peer.Value
            if node.Id <> state.Node.Id then
              let nxtidx = Node.getNextIndex node
              let! cidx = currentIndexM ()

              // calculate whether we need to send a snapshot or not
              // uint's wrap around, so normalize to int first (might cause trouble with big numbers)
              let difference =
                let d = int cidx - int nxtidx
                if d < 0 then 0UL else uint64 d

              if difference <= (state.MaxLogDepth + 1UL) then
                // Only send new entries. Don't send the entry to peers who are
                // behind, to prevent them from becoming congested.
                let! request = sendAppendEntry node
                requests := Array.append [| (node,request) |] !requests
              else
                // because this node is way behind in the cluster, get it up to speed
                // with a snapshot
                let! request = sendInstallSnapshot node
                requests := Array.append [| (node,request) |] !requests

          let results =
            Array.map snd !requests
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.zip (Array.map fst !requests)

          for (node, response) in results do
            match response with
            | Some resp ->
              do! receiveAppendEntriesResponse node.Id resp
              let! peer = getNodeM node.Id >>= (Option.get >> returnM)
              if peer.State = Failed then
                do! setNodeStateM node.Id Running
            | _ ->
              do! setNodeStateM node.Id Failed

          do! updateCommitIdx |> modify

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
      let resp = { Id = Id.Create(); Term = 0UL; Index = 0UL }

      if LogEntry.isConfigChange entry && Option.isSome state.ConfigChangeEntry then
        do! debug "receiveEntry: Failed: UnexpectedVotingChange"
        return! failM UnexpectedVotingChange
      elif isLeader state then
        do! debug <| sprintf "receiveEntry: (id: %s) (idx: %d) (term: %d)"
                      ((LogEntry.id entry).ToString() )
                      (Log.index state.Log + 1UL)
                      state.CurrentTerm

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

          | _ ->
            return! failM LogFormatError
      else
        return! failM NotLeader
    }

  ////////////////////////////////////////////////////////////////
  //     _                _         _____       _               //
  //    / \   _ __  _ __ | |_   _  | ____|_ __ | |_ _ __ _   _  //
  //   / _ \ | '_ \| '_ \| | | | | |  _| | '_ \| __| '__| | | | //
  //  / ___ \| |_) | |_) | | |_| | | |___| | | | |_| |  | |_| | //
  // /_/   \_\ .__/| .__/|_|\__, | |_____|_| |_|\__|_|   \__, | //
  //         |_|   |_|      |___/                        |___/  //
  ////////////////////////////////////////////////////////////////

  let applyEntry (cbs: IRaftCallbacks<_,_>) = function
    | JointConsensus(_,_,_,changes,_) ->
      let applyChange change =
        match change with
          | NodeAdded(node)   -> cbs.NodeAdded   node
          | NodeRemoved(node) -> cbs.NodeRemoved node
      Array.iter applyChange changes
    | Configuration(_,_,_,nodes,_) -> cbs.Configured nodes
    | LogEntry(_,_,_,data,_)       -> cbs.ApplyLog data
    | Snapshot(_,_,_,_,_,_,data) as snapshot  ->
      cbs.PersistSnapshot snapshot
      cbs.ApplyLog data

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
          let! cbs = read

          do! info <| sprintf "applyEntries: applying %d entries to state machine"
                        (Log.depth entries)

          // Apply log chain in the order it arrived
          let state, change =
            LogEntry.foldr
              (fun (state, current) lg ->
                match lg with
                  | Configuration _ as config ->
                    let state = handleConfigChange config state
                    applyEntry cbs config
                    (state, None)
                  | JointConsensus _ as config ->
                    let state = handleConfigChange config state
                    applyEntry cbs config
                    (state, Some (Log.first config))
                  | entry ->
                    applyEntry cbs entry
                    (state, current))
              (state, state.ConfigChangeEntry)
              entries

          do! debug <| sprintf "applyEntries: setting ConfigChangeEntry to %A" change
          do! put { state with ConfigChangeEntry = change }

          if Log.containsEntry Log.isConfiguration entries then
            let selfIncluded (state: Raft<_,_>) =
              Map.containsKey state.Node.Id state.Peers
            let! included = selfIncluded |> zoomM
            if not included then
              do! debug "applyEntries: self (%s) not included in new configuration"
              do! becomeFollower ()

          let! state = get
          if not (isLeader state) && Log.containsEntry Log.isConfiguration entries then
            do! debug "applyEntries: not leader and new configuration is applied. Updating nodes."
            for kv in state.Peers do
              if kv.Value.State <> Running then
                do! updateNodeM { kv.Value with State = Running; Voting = true }

          let idx = Log.entryIndex entries
          do! debug <| sprintf "applyEntries: setting LastAppliedIndex to %d" idx
          do! setLastAppliedIdxM idx
        | _ ->
          do! debug <| sprintf "applyEntries: no log entries found for index %d" logIdx
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
              Log.foldr (fun _ entry -> applyEntry cbs entry) () data
            | _ -> failwith "Fatal. Snapshot applied, but log is empty. Aborting."

          // reset the counters,to apply all entries in the log
          do! setLastAppliedIdxM (Log.index state.Log)
          do! setCommitIndexM (Log.index state.Log)

          // cosntruct reply
          let! term = currentTermM ()
          let! ci = currentIndexM ()
          let! fi = firstIndexM term

          let ar : AppendResponse =
            { Term         = term
            ; Success      = true
            ; CurrentIndex = ci
            ; FirstIndex   = match fi with
                             | Some i -> i
                             | _      -> 0UL }

          return ar
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

  let private maybeSetIndex nid nextIdx matchIdx =
    let mapper peer =
      if Node.isVoting peer && peer.Id <> nid
      then { peer with NextIndex = nextIdx; MatchIndex = matchIdx }
      else peer
    updatePeersM mapper

  /// Become leader afer a successful election
  let becomeLeader _ =
    raft {
      let! state = get
      do! info "becoming leader"
      let nextidx = currentIndex state + 1UL
      do! setStateM Leader
      do! maybeSetIndex state.Node.Id nextidx 0UL
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
        let! state = get

        do! debug <| sprintf "receiveVoteResponse: %s responded to vote request with: %s"
                      (string nid)
                      (if vote.Granted then "granted" else "not granted")

        /// The term must not be bigger than current raft term,
        /// otherwise set term to vote term become follower.
        if vote.Term > state.CurrentTerm then
          do! debug <| sprintf "receiveVoteResponse: (vote term: %d) > (current term: %d). Setting to %d."
                        vote.Term
                        state.CurrentTerm
                        state.CurrentTerm
          do! setTermM vote.Term
          do! becomeFollower ()

        /// If the vote term is smaller than current term it is considered an
        /// error and the client will be notified.
        elif vote.Term < state.CurrentTerm then
          do! debug <| sprintf "receiveVoteResponse: Failed: (vote term: %d) < (current term: %d). VoteTermMismatch."
                        vote.Term
                        state.CurrentTerm
          return! failM VoteTermMismatch

        /// Process the vote if current state of our Raft must be candidate..
        elif state.State = Candidate then

          if vote.Granted then
            let! node = getNodeM nid
            match node with
              // Could not find the node in current configuration(s)
              | None ->
                do! debug "receiveVoteResponse: Failed: vote granted but NoNode"
                return! failM NoNode
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

                  do! debug <| sprintf "receiveVoteResponse: In JointConsensus (majority for new config: %b) (majority for old config: %b)"
                                newConfig
                                oldConfig

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

                  do! debug <| sprintf "receiveVoteResponse: (majority for config: %b)" majority

                  if majority then
                    do! becomeLeader ()

        /// ...otherwise we respond with the respective RaftError.
        else
          do! debug "receiveVoteResponse: Failed: NotCandidate"
          return! failM NotCandidate
      }

  /// Request a from a given peer
  let sendVoteRequest (node : Node<'n>) =
    get >>= fun state ->
      read >>= fun cbs ->
        async {
          if Node.isVoting node (*&& node.State = Running *) then
            let vote =
              { Term         = state.CurrentTerm
              ; Candidate    = state.Node
              ; LastLogIndex = Log.index state.Log
              ; LastLogTerm  = Log.term  state.Log }

            sprintf "sendVoteRequest: (to: %s) (state: %A)"
              (string node.Id)
              (node.State)
            |> cbs.LogMsg Debug state.Node

            let result = cbs.SendRequestVote node vote
            return result
          else
            sprintf "sendVoteRequest: not requesting vote from %s: (voting: %b) (state: %A)"
              (string node.Id)
              (Node.isVoting node)
              (node.State)
            |> cbs.LogMsg Debug state.Node

            return None
        }
        |> returnM

  let requestAllVotes _ =
    raft {
        let! self = getSelfM ()
        let! peers = logicalPeersM ()

        do! info "requesting all votes"

        let requests = ref [| |]

        for peer in peers do
          if self.Id <> peer.Value.Id then
            let! request = sendVoteRequest peer.Value
            requests := Array.append [| (peer.Value, request) |] !requests

        let responses =
          !requests
          |> Array.map snd
          |> Async.Parallel
          |> Async.RunSynchronously
          |> Array.zip (Array.map fst !requests)

        for (node, response) in responses do
          let! leader = isLeaderM ()
          if not leader then                          // check if raft is already leader
            match response with                     // otherwise process the vote
            | Some resp ->
              do! receiveVoteResponse node.Id resp
              do! setNodeStateM node.Id Running
            | _ ->
              do! setNodeStateM node.Id Failed      // mark node as failed (calls the callback)
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
  let private validateTerm (vote: VoteRequest<_>) state =
    (vote.Term < state.CurrentTerm, InvalidTerm)

  let private alreadyVoted (state: Raft<'d,'n>) =
    (Option.isSome state.VotedFor, AlreadyVoted)

  let private validateLastLog vote state =
    let result =
         vote.LastLogTerm   = lastLogTerm state
      && currentIndex state <= vote.LastLogIndex
    (result,InvalidLastLog)

  let private validateLastLogTerm vote state =
    (lastLogTerm state < vote.LastLogTerm, InvalidLastLogTerm)

  let private validateCurrentIdx state =
    (currentIndex state = 0UL, InvalidCurrentIndex)

  let private validateCandidate (vote: VoteRequest<_>) state =
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

      if fst result then
        do! debug <| sprintf "shouldGrantVote: granted vote to %s"
                      (string vote.Candidate.Id)
      else
        do! debug <| sprintf "shouldGrantVote: did not grant vote to %s. reason: %A"
                      (string vote.Candidate.Id)
                      (snd result)

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

  let private maybeResetFollower (vote : VoteRequest<_>) =
    raft {
      let! term = currentTermM ()
      if term < vote.Term then
        do! debug "maybeResetFollower: term < vote.Term resetting"
        do! setTermM vote.Term
        do! becomeFollower ()
        do! voteFor None
    }

  let private processVoteRequest (vote : VoteRequest<_>) =
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
            do! debug "processVoteRequest: vote request denied: NotVotingState"
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

          do! info <| sprintf "receiveVoteRequest: node %s requested vote. granted: %b"
                        (string nid)
                        result.Granted

          return result
        | _ ->
          do! info <| sprintf "receiveVoteRequest: requested denied. NoNode %s" (string nid)

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
      do! info "becoming candidate"

      let! state = get

      let term = state.CurrentTerm + 1UL
      do! debug <| sprintf "becomeCandidate: setting term to %d" term
      do! setTermM term

      do! resetVotesM ()
      do! voteForMyself ()
      do! setLeaderM None
      do! setStateM Candidate

      let elapsed = uint64(_rand.Next(15, int state.ElectionTimeout))
      do! debug <| sprintf "becomeCandidate: setting timeoutElapsed to %d" elapsed
      do! setTimeoutElapsedM elapsed

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
      do! debug <| sprintf "startElection: (elapsed: %d) (elec-timeout: %d) (term: %d) (ci: %d)"
                    state.TimeoutElapsed
                    state.ElectionTimeout
                    state.CurrentTerm
                    (currentIndex state)

      do! becomeCandidate ()
    }


  //  ____           _           _ _
  // |  _ \ ___ _ __(_) ___   __| (_) ___
  // | |_) / _ \ '__| |/ _ \ / _` | |/ __|
  // |  __/  __/ |  | | (_) | (_| | | (__
  // |_|   \___|_|  |_|\___/ \__,_|_|\___|

  let periodic (elapsed : Long) =
    raft {
      let! state = get
      do! setTimeoutElapsedM (state.TimeoutElapsed + elapsed)

      match state.State with
      | Leader ->
        // if in JointConsensus
        let! consensus = inJointConsensusM ()
        let! timedout = requestTimedOutM ()

        if consensus then
          let! waiting = hasNonVotingNodesM () // check if any nodes are still marked non-voting/Joining
          if not waiting then                    // are nodes are voting and have caught up
            let! term = currentTermM ()
            let resp = { Id = Id.Create(); Term = term; Index = 0UL }
            let! nodes = getNodesM () >>= (Map.toArray >> Array.map snd >> returnM)
            let log = Configuration(resp.Id, 0UL, term, nodes, None)
            do! handleLog log resp >>= ignoreM
          else
            do! sendAllAppendEntriesM ()
        // the regular case is we need to ping our followers so as to not provoke an election
        elif timedout then
          do! sendAllAppendEntriesM ()

      | _ ->
        // have to double check the code here to ensure new elections are really only called when
        // not enough votes could be garnered
        let! num = numNodesM ()
        let! timedout = electionTimedOutM ()

        if timedout && num > 1UL then
          do! startElection ()

      let! coi = commitIndexM ()
      let! lai = lastAppliedIdxM ()

      if lai < coi then
        do! applyEntries ()
      do! maybeSnapshot ()
    }
