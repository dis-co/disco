namespace Iris.Raft

// * Imports

open System
open Iris.Core

// * RaftMonad

[<AutoOpen>]
module RaftMonad =

  // ** warn

  let warn str = printfn "[RAFT WARNING] %s" str

  // ** get

  /// get current Raft state
  let get = MkRM (fun _ s -> Right (s, s))

  // ** put

  /// update Raft/State to supplied value
  let put s = MkRM (fun _ _ -> Right ((), s))

  // ** read

  /// get the read-only environment value
  let read : RaftM<_,_> = MkRM (fun l s -> Right (l, s))

  // ** apply

  /// unwrap the closure and apply it to the supplied state/env
  let apply (env: 'e) (state: 's) (m: RaftMonad<'e,'s,_,_>)  =
    match m with | MkRM func -> func env state

  // ** runRaft

  /// run the monadic action against state and environment values
  let runRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    apply l s m

  // ** evalRaft

  /// run monadic action against supplied state and evironment and return new state
  let evalRaft (s: 's) (l: 'e) (m: RaftMonad<'e,'s,'a,'err>) =
    match runRaft s l m with
    | Right (_,state) | Left (_,state) -> state

  // ** returnM

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Right` value. This means the computation will,
  /// if possible, continue to the next step.
  let returnM value : RaftMonad<'e,'s,'t,'err> =
    MkRM (fun _ state -> Right(value, state))

  // ** ignoreM

  let ignoreM _ : RaftMonad<'e,'s,unit,'err> =
    MkRM (fun _ state -> Right((), state))

  // ** failM

  /// Lift a regular value into a RaftMonad by wrapping it in a closure.
  /// This variant wraps it in a `Left` value. This means the computation will
  /// not continue past this step and no regular value will be returned.
  let failM l =
    MkRM (fun _ s -> Left (l, s))

  // ** returnFromM

  /// pass through the given action
  let returnFromM func : RaftMonad<'e,'s,'t,'err> =
    func

  // ** zeroM

  let zeroM () =
    MkRM (fun _ state -> Right((), state))

  // ** delayM

  let delayM (f: unit -> RaftMonad<'e,'s,'t,'err>) =
    MkRM (fun env state -> f () |> apply env state)

  // ** bindM

  /// Chain up effectful actions.
  let bindM (m: RaftMonad<'env,'state,'a,'err>)
            (f: 'a -> RaftMonad<'env,'state,'b,'err>) :
            RaftMonad<'env,'state,'b,'err> =
    MkRM (fun env state ->
          match apply env state m with
          | Right  (value,state') -> f value |> apply env state'
          | Left    err           -> Left err)

  // ** (>>=)

  let (>>=) = bindM

  // ** combineM

  let combineM (m1: RaftMonad<_,_,_,_>) (m2: RaftMonad<_,_,_,_>) =
    bindM m1 (fun _ -> m2)

  // ** tryWithM

  let tryWithM (body: RaftMonad<_,_,_,_>) (handler: exn -> RaftMonad<_,_,_,_>) =
    MkRM (fun env state ->
          try apply env state body
          with ex -> apply env state (handler ex))

  // ** tryFinallyM

  let tryFinallyM (body: RaftMonad<_,_,_,_>) handler : RaftMonad<_,_,_,_> =
    MkRM (fun env state ->
          try apply env state body
          finally handler ())

  // ** usingM

  let usingM (resource: ('a :> System.IDisposable)) (body: 'a -> RaftMonad<_,_,_,_>) =
    tryFinallyM (body resource)
      (fun _ -> if not <| isNull (box resource)
                then resource.Dispose())

  // ** whileM

  let rec whileM (guard: unit -> bool) (body: RaftMonad<_,_,_,_>) =
    match guard () with
    | true -> bindM body (fun _ -> whileM guard body)
    | _ -> zeroM ()

  // ** forM

  let rec forM (sequence: seq<_>) (body: 'a -> RaftMonad<_,_,_,_>) : RaftMonad<_,_,_,_> =
    usingM (sequence.GetEnumerator())
      (fun enum -> whileM enum.MoveNext (delayM (fun _ -> body enum.Current)))

  // ** RaftBuilder

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

  // ** raft

  let raft = new RaftBuilder()

// * Raft

[<RequireQualifiedAccess>]
module rec Raft =

  // ** tag

  let private tag (str: string) = String.Format("Raft.{0}",str)

  // ** log

  let log site level message =
    message
    |> Logger.log level (tag site)
    |> returnM

  // ** debug

  let debug site str = log site Debug str

  // ** info

  let info site str = log site Info str

  // ** warn

  let warn site str = log site Warn str

  // ** error

  let error site str = log site Err str

  // ** sendAppendEntriesM

  let sendAppendEntriesM (mem: RaftMember) (request: AppendEntries) =
    raft {
      let! state = get
      let! cbs = read

      let msg =
        sprintf "to: %s ci: %d term: %d leader-commit: %d prv-log-idx: %d prev-log-term: %d"
          (string mem.Id)
          (currentIndex state)
          request.Term
          request.LeaderCommit
          request.PrevLogIdx
          request.PrevLogTerm

      do! debug "sendAppendEntriesM" msg

      cbs.SendAppendEntries mem request
    }

  // ** persistVote

  let persistVote mem =
    read >>= fun cbs ->
      cbs.PersistVote mem
      |> returnM

  // ** persistTerm

  let persistTerm term =
    read >>= fun cbs ->
      cbs.PersistTerm term
      |> returnM

  // ** persistLog

  let persistLog log =
    read >>= fun cbs ->
      cbs.PersistLog log
      |> returnM

  // ** modify

  let modify (f: RaftState -> RaftState) =
    get >>= (f >> put)

  // ** zoomM

  let zoomM (f: RaftState -> 'a) =
    get >>= (f >> returnM)

  // ** rand

  let private rand = new System.Random()

  // ** create

  let create (self : RaftMember) : RaftState =
    { Member            = self
      State             = Follower
      CurrentTerm       = term 0
      CurrentLeader     = None
      Peers             = Map.ofList [(self.Id, self)]
      OldPeers          = None
      NumMembers        = 1
      VotedFor          = None
      Log               = Log.empty
      CommitIndex       = 0<index>
      LastAppliedIdx    = 0<index>
      TimeoutElapsed    = 0<ms>
      ElectionTimeout   = Constants.RAFT_ELECTION_TIMEOUT * 1<ms>
      RequestTimeout    = Constants.RAFT_REQUEST_TIMEOUT * 1<ms>
      MaxLogDepth       = Constants.RAFT_MAX_LOGDEPTH
      ConfigChangeEntry = None }

  // ** isFollower

  /// Is the Raft value in Follower state.
  let isFollower (state: RaftState) =
    state.State = Follower

  // ** isFollowerM

  let isFollowerM = fun _ -> zoomM isFollower

  // ** isCandidate

  /// Is the Raft value in Candate state.
  let isCandidate (state: RaftState) =
    state.State = Candidate

  // ** isCandidateM

  let isCandidateM _ = zoomM isCandidate

  // ** isLeader

  /// Is the Raft value in Leader state
  let isLeader (state: RaftState) =
    state.State = Leader

  // ** isLeaderM

  let isLeaderM _ = zoomM isLeader

  // ** inJointConsensus

  let inJointConsensus (state: RaftState) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus _) -> true
      | _                       -> false

  // ** inJointConsensusM

  let inJointConsensusM _ = zoomM inJointConsensus

  // ** hasNonVotingMembers

  let hasNonVotingMembers (state: RaftState) =
    Map.fold
      (fun b _ n ->
        if b then
          b
        else
          not (Member.hasSufficientLogs n && Member.isVoting n))
      false
      state.Peers

  // ** hasNonVotingMembersM

  let hasNonVotingMembersM _ = zoomM hasNonVotingMembers

  // ** getChanges

  let getChanges (state: RaftState) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus(_,_,_,changes,_)) -> Some changes
      | _ -> None

  // ** logicalPeers

  let logicalPeers (state: RaftState) =
    // when setting the NumMembers counter we have to include the old config
    if inJointConsensus state then
        // take the old peers as seed and apply the new peers on top
      match state.OldPeers with
        | Some peers -> Map.fold (fun m k n -> Map.add k n m) peers state.Peers
        | _ -> state.Peers
    else
      state.Peers

  // ** logicalPeersM

  let logicalPeersM _ = zoomM logicalPeers

  // ** countMembers

  let countMembers peers = Map.fold (fun m _ _ -> m + 1) 0 peers

  // ** numLogicalPeers

  let numLogicalPeers (state: RaftState) =
    logicalPeers state |> countMembers

  // ** setNumPeers

  let setNumPeers (state: RaftState) =
    { state with NumMembers = countMembers state.Peers }

  // ** recountPeers

  let recountPeers () =
    get >>= (setNumPeers >> put)

  // ** setPeers

  /// Set States Members to supplied Map of Mems. Also cache count of mems.
  let setPeers (peers : Map<MemberId,RaftMember>) (state: RaftState) =
    { state with Peers = Map.add state.Member.Id state.Member peers }
    |> setNumPeers

  // ** addMember

  /// Adds a mem to the list of known Members and increments NumMembers counter
  let addMember (mem : RaftMember) (state: RaftState) : RaftState =
    let exists = Map.containsKey mem.Id state.Peers
    { state with
        Peers = Map.add mem.Id mem state.Peers
        NumMembers =
          if exists
            then state.NumMembers
            else state.NumMembers + 1 }

  // ** addMemberM

  let addMemberM (mem: RaftMember) =
    get >>= (addMember mem >> put)

  // ** addPeer

  /// Alias for `addMember`
  let addPeer = addMember

  // ** addPeerM

  let addPeerM = addMemberM

  // ** addNonVotingMember

  /// Add a Non-voting Peer to the list of known Members
  let addNonVotingMember (mem : RaftMember) (state: RaftState) =
    addMember { mem with Voting = false; Status = Joining } state

  // ** removeMember

  /// Remove a Peer from the list of known Members and decrement NumMembers counter
  let removeMember (mem : RaftMember) (state: RaftState) =
    let numMembers =
      if Map.containsKey mem.Id state.Peers
        then state.NumMembers - 1
        else state.NumMembers

    { state with
        Peers = Map.remove mem.Id state.Peers
        NumMembers = numMembers }

  // ** applyChanges

  let applyChanges changes state =
    let folder _state = function
      | MemberAdded   mem -> addNonVotingMember mem _state
      | MemberRemoved mem -> removeMember       mem _state
    Array.fold folder state changes

  // ** updateMember

  let private updateMember (mem : RaftMember) (cbs: IRaftCallbacks) (state: RaftState) =
    // if we are in joint consensus, we must update the mem value in either the
    // new or the old configuration, or both.
    let old = Map.tryFind mem.Id state.Peers
    if inJointConsensus state then
      // if the mems has structurally changed fire the callback
      match old with
      | Some oldMember -> if oldMember <> mem then cbs.MemberUpdated mem
      | _ -> ()
      // update the state
      { state with
          Peers =
            if Option.isSome old then
              Map.add mem.Id mem state.Peers
            else state.Peers
          OldPeers =
            match state.OldPeers with
            | Some peers ->
              if Map.containsKey mem.Id peers then
                if Option.isNone old then cbs.MemberUpdated mem
                Map.add mem.Id mem peers |> Some
              else Some peers
            | None ->                    // apply all required changes again
              let folder m = function          // but this is an edge case
                | MemberAdded   peer -> Map.add peer.Id peer m
                | MemberRemoved peer -> Map.filter (fun k _ -> k <> peer.Id) m
              let changes = getChanges state |> Option.get
              let peers = Array.fold folder state.Peers changes
              if Map.containsKey mem.Id peers then
                Map.add mem.Id mem peers |> Some
              else Some peers }
      |> setNumPeers
    else // base case
      // if the mems has structurally changed fire the callback
      match old with
      | Some oldMember -> if oldMember <> mem then cbs.MemberUpdated mem
      | _ -> ()
      { state with
          Peers =
            if Map.containsKey mem.Id state.Peers
            then Map.add mem.Id mem state.Peers
            else state.Peers }

  // ** updateMemberM

  let updateMemberM (mem: RaftMember) =
    read >>= fun env ->
      get >>= (updateMember mem env >> put)

  // ** addMembers

  let addMembers (mems : Map<MemberId,RaftMember>) (state: RaftState) =
    Map.fold (fun m _ n -> addMember n m) state mems

  // ** addMembersM

  let addMembersM (mems: Map<MemberId,RaftMember>) =
    get >>= (addMembers mems >> put)

  // ** addPeers

  let addPeers = addMembers

  // ** addPeersM

  let addPeersM = addMembersM

  // ** addNonVotingMemberM

  let addNonVotingMemberM (mem: RaftMember) =
    get >>= (addNonVotingMember mem >> put)

  // ** removeMemberM

  let removeMemberM (mem: RaftMember) =
    get >>= (removeMember mem >> put)

  // ** hasMember

  let hasMember (nid : MemberId) (state: RaftState) =
    Map.containsKey nid state.Peers

  // ** hasMemberM

  let hasMemberM _ = hasMember >> zoomM

  // ** getMember

  let getMember (nid : MemberId) (state: RaftState) =
    if inJointConsensus state then
      logicalPeers state |> Map.tryFind nid
    else
      Map.tryFind nid state.Peers

  // ** getMemberM

  /// Find a peer by its Id. Return None if not found.
  let getMemberM nid = getMember nid |> zoomM

  // ** setMemberStateM

  let setMemberStateM (nid: MemberId) state =
    getMemberM nid >>= function
      | Some mem -> updateMemberM { mem with Status = state }
      | None     -> returnM ()

  // ** getMembers

  let getMembers (state: RaftState) = state.Peers

  // ** getMembersM

  let getMembersM _ = zoomM getMembers

  // ** getSelf

  let getSelf (state: RaftState) = state.Member

  // ** getSelfM

  let getSelfM _ = zoomM getSelf

  // ** setSelf

  let setSelf (mem: RaftMember) (state: RaftState) =
    { state with Member = mem }

  // ** setSelfM

  let setSelfM mem =
    setSelf mem |> modify

  // ** lastConfigChange

  let lastConfigChange (state: RaftState) =
    state.ConfigChangeEntry

  // ** lastConfigChangeM

  let lastConfigChangeM _ =
    lastConfigChange |> zoomM

  // ** setTerm

  /// Set CurrentTerm on Raft to supplied term.
  let setTerm (term : Term) (state: RaftState) =
    { state with CurrentTerm = term }

  // ** setTermM

  /// Set CurrentTerm to supplied value. Monadic action.
  let setTermM (term : Term) =
    raft {
      do! setTerm term |> modify
      do! persistTerm term
    }

  // ** setState

  /// Set current RaftState to supplied state.
  let setState (newstate: MemberState) (env: IRaftCallbacks) (state: RaftState) =
    if newstate <> state.State then
      env.StateChanged state.State newstate
      { state with State = newstate }
    else state

  // ** setStateM

  /// Set current RaftState to supplied state. Monadic action.
  let setStateM (state : MemberState) =
    read >>= (setState state >> modify)

  // ** getState

  /// Get current RaftState: Leader, Candidate or Follower
  let getState (state: RaftState) =
    state.State

  // ** getStateM

  /// Get current RaftState. Monadic action.
  let getStateM _ = zoomM getState

  // ** getMaxLogDepth

  let getMaxLogDepth (state: RaftState) =
    state.MaxLogDepth

  // ** getMaxLogDepthM

  let getMaxLogDepthM _ = zoomM getMaxLogDepth

  // ** setMaxLogDepth

  let setMaxLogDepth (depth: int) (state: RaftState) =
    { state with MaxLogDepth = depth }

  // ** setMaxLogDepthM

  let setMaxLogDepthM (depth: int) =
    setMaxLogDepth depth |> modify

  // ** self

  /// Get Member associated with supplied raft value.
  let self (state: RaftState) =
    state.Member

  // ** selfM

  /// Get Member associated with supplied raft value. Monadic action.
  let selfM _ = zoomM self

  // ** setOldPeers

  let setOldPeers (peers : Map<MemberId,RaftMember> option) (state: RaftState) =
    { state with OldPeers = peers  } |> setNumPeers

  // ** setPeersM

  /// Set States Members to supplied Map of Members. Monadic action.
  let setPeersM (peers: Map<_,_>) =
    setPeers peers |> modify

  // ** setOldPeersM

  /// Set States Members to supplied Map of Members. Monadic action.
  let setOldPeersM (peers: Map<_,_> option) =
    setOldPeers peers |> modify

  // ** updatePeers

  /// Map over States Members with supplied mapping function
  let updatePeers (f: RaftMember -> RaftMember) (state: RaftState) =
    { state with Peers = Map.map (fun _ v -> f v) state.Peers }

  // ** updatePeersM

  /// Map over States Members with supplied mapping function. Monadic action
  let updatePeersM (f: RaftMember -> RaftMember) =
    updatePeers f |> modify

  // ** setLeader

  /// Set States CurrentLeader field to supplied MemberId.
  let setLeader (leader : MemberId option) (cbs: IRaftCallbacks) (state: RaftState) =
    if leader <> state.CurrentLeader then
      let peers =
        Map.map
          (fun id peer ->
            if Some id = leader then
              let peer = Member.setState Leader peer
              cbs.MemberUpdated peer
              peer
            else
              let peer = Member.setState Follower peer
              cbs.MemberUpdated peer
              peer)
          state.Peers
      cbs.LeaderChanged leader
      { state with
          CurrentLeader = leader
          Peers = peers }
    else state

  // ** setLeaderM

  /// Set States CurrentLeader field to supplied MemberId. Monadic action.
  let setLeaderM (leader : MemberId option) =
    read >>= fun cbs -> setLeader leader cbs |> modify

  // ** setNextIndex

  /// Set the nextIndex field on Member corresponding to supplied Id (should it
  /// exist, that is).
  let setNextIndex (nid : MemberId) idx cbs (state: RaftState) =
    let mem = getMember nid state
    let nextidx = if idx < index 1 then index 1 else idx
    match mem with
    | Some mem -> updateMember { mem with NextIndex = nextidx } cbs state
    | _         -> state

  // ** setNextIndexM

  /// Set the nextIndex field on Member corresponding to supplied Id (should it
  /// exist, that is) and supplied index. Monadic action.
  let setNextIndexM (nid : MemberId) idx =
    read >>= (setNextIndex nid idx >> modify)

  // ** setAllNextIndex

  /// Set the nextIndex field on all Members to supplied index.
  let setAllNextIndex idx (state: RaftState) =
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

  // ** setAllNextIndexM

  let setAllNextIndexM idx =
    setAllNextIndex idx |> modify

  // ** setMatchIndex

  /// Set the matchIndex field on Member to supplied index.
  let setMatchIndex nid idx env (state: RaftState) =
    let mem = getMember nid state
    match mem with
    | Some peer -> updateMember { peer with MatchIndex = idx } env state
    | _         -> state

  // ** setMatchIndexM

  let setMatchIndexM nid idx =
    read >>= (setMatchIndex nid idx >> modify)

  // ** setAllMatchIndex

  /// Set the matchIndex field on all Members to supplied index.
  let setAllMatchIndex idx (state: RaftState) =
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

  // ** setAllMatchIndexM

  let setAllMatchIndexM idx =
    setAllMatchIndex idx |> modify

  // ** voteFor

  /// Remeber who we have voted for in current election.
  let voteFor (mem : RaftMember option) =
    let doVoteFor state =
      { state with VotedFor = Option.map (fun (n : RaftMember) -> n.Id) mem }

    raft {
      let! state = get
      do! persistVote mem
      do! doVoteFor state |> put
    }

  // ** voteForId

  /// Remeber who we have voted for in current election
  let voteForId (nid : MemberId)  =
    raft {
      let! mem = getMemberM nid
      do! voteFor mem
    }

  // ** resetVotes

  let resetVotes (state: RaftState) =
    let resetter _ peer = Member.setVotedForMe false peer
    { state with
        Peers = Map.map resetter state.Peers
        OldPeers =
          match state.OldPeers with
            | Some peers -> Map.map resetter peers |> Some
            | _ -> None }

  // ** resetVotesM

  let resetVotesM _ =
    resetVotes |> modify

  // ** voteForMyself

  let voteForMyself _ =
    get >>= fun state -> voteFor (Some state.Member)

  // ** votedForMyself

  let votedForMyself (state: RaftState) =
    match state.VotedFor with
    | Some(nid) -> nid = state.Member.Id
    | _ -> false

  // ** votedFor

  let votedFor (state: RaftState) =
    state.VotedFor

  // ** votedForM

  let votedForM _ = zoomM votedFor

  // ** setVoting

  let setVoting (mem : RaftMember) (vote : bool) (state: RaftState) =
    let updated = Member.setVotedForMe vote mem
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

  // ** setVotingM

  let setVotingM (mem: RaftMember) (vote: bool) =
    raft {
      let msg = sprintf "setting mem %s voting to %b" (string mem.Id) vote
      do! debug "setVotingM" msg
      do! setVoting mem vote |> modify
    }

  // ** currentIndex

  let currentIndex (state: RaftState) =
    Log.getIndex state.Log

  // ** currentIndexM

  let currentIndexM _ = zoomM currentIndex

  // ** numMembers

  let numMembers (state: RaftState) =
    state.NumMembers

  // ** numMembersM

  let numMembersM _ = zoomM numMembers

  // ** numPeers

  let numPeers = numMembers

  // ** numPeersM

  let numPeersM = numMembersM

  // ** numOldPeers

  let numOldPeers (state: RaftState) =
    match state.OldPeers with
    | Some peers -> Map.fold (fun m _ _ -> m + 1) 0 peers
    |      _     -> 0

  // ** numOldPeersM

  let numOldPeersM _ = zoomM numOldPeers

  // ** votingMembers

  let votingMembers (state: RaftState) =
    votingMembersForConfig state.Peers

  // ** votingMembersM

  let votingMembersM _ = zoomM votingMembers

  // ** votingMembersForConfig

  let votingMembersForConfig peers =
    let counter r _ n =
      if Member.isVoting n then r + 1 else r
    Map.fold counter 0 peers

  // ** votingMembersForOldConfig

  let votingMembersForOldConfig (state: RaftState) =
    match state.OldPeers with
    | Some peers -> votingMembersForConfig peers
    | _ -> 0

  // ** votingMembersForOldConfigM

  let votingMembersForOldConfigM _ = zoomM votingMembersForOldConfig

  // ** numLogs

  let numLogs (state: RaftState) =
    Log.length state.Log

  // ** numLogsM

  let numLogsM _ = zoomM numLogs

  // ** currentTerm

  let currentTerm (state: RaftState) =
    state.CurrentTerm

  // ** currentTermM

  let currentTermM _ = zoomM currentTerm

  // ** firstIndex

  let firstIndex (term: Term) (state: RaftState) =
    Log.firstIndex term state.Log

  // ** firstIndexM

  let firstIndexM (term: Term) =
    firstIndex term |> zoomM

  // ** currentLeader

  let currentLeader (state: RaftState) =
    state.CurrentLeader

  // ** currentLeaderM

  let currentLeaderM _ = zoomM currentLeader

  // ** getLeader

  let getLeader (state: RaftState) =
    currentLeader state |> Option.bind (flip getMember state)

  // ** commitIndex

  let commitIndex (state: RaftState) =
    state.CommitIndex

  // ** commitIndexM

  let commitIndexM () = zoomM commitIndex

  // ** setCommitIndex

  let setCommitIndex (idx : Index) (state: RaftState) =
    { state with CommitIndex = idx }

  // ** setCommitIndexM

  let setCommitIndexM (idx : Index) =
    setCommitIndex idx |> modify

  // ** requestTimedout

  let requestTimedOut (state: RaftState) : bool =
    state.RequestTimeout <= state.TimeoutElapsed

  // ** requestTimedoutM

  let requestTimedOutM _ = zoomM requestTimedOut

  // ** electionTimedout

  let electionTimedOut (state: RaftState) : bool =
    state.ElectionTimeout <= state.TimeoutElapsed

  // ** electionTimedoutM

  let electionTimedOutM _ = zoomM electionTimedOut

  // ** electionTimeout

  let electionTimeout (state: RaftState) =
    state.ElectionTimeout

  // ** electionTimeoutM

  let electionTimeoutM _ = zoomM electionTimeout

  // ** timeoutElapsed

  let timeoutElapsed (state: RaftState) =
    state.TimeoutElapsed

  // ** timeoutElapsedM

  let timeoutElapsedM _ = zoomM timeoutElapsed

  // ** setTimeoutElapsed

  let private setTimeoutElapsed (elapsed: Timeout) (state: RaftState) =
    { state with TimeoutElapsed = elapsed }

  // ** setTimeoutElapsedM

  let setTimeoutElapsedM (elapsed: Timeout) =
    setTimeoutElapsed elapsed |> modify

  // ** requestTimeout

  let requestTimeout (state: RaftState) =
    state.RequestTimeout

  // ** requestTimeoutM

  let requestTimeoutM _ = zoomM requestTimeout

  // ** setRequestTimeout

  let setRequestTimeout (timeout : Timeout) (state: RaftState) =
    { state with RequestTimeout = timeout }

  // ** setRequestTimeoutM

  let setRequestTimeoutM (timeout: Timeout) =
    setRequestTimeout timeout |> modify

  // ** setElectionTimeout

  let setElectionTimeout (timeout : Timeout) (state: RaftState) =
    { state with ElectionTimeout = timeout }

  // ** setElectionTimeoutM

  let setElectionTimeoutM (timeout: Timeout) =
    setElectionTimeout timeout |> modify

  // ** _lastAppliedIdx

  let private _lastAppliedIdx (state: RaftState) =
    state.LastAppliedIdx

  // ** lastAppliedIdx

  let lastAppliedIdx () = zoomM _lastAppliedIdx

  // ** setLastAppliedIdx

  let private setLastAppliedIdx (idx : Index) (state: RaftState) =
    { state with LastAppliedIdx = idx }

  // ** setLastAppliedIdxM

  let setLastAppliedIdxM (idx: Index) =
    setLastAppliedIdx idx |> modify

  // ** maxLogDepth

  let private maxLogDepth (state: RaftState) = state.MaxLogDepth

  // ** maxLogDepthM

  let maxLogDepthM _ = zoomM maxLogDepth

  // ** lastLogTerm

  let private lastLogTerm (state: RaftState) =
    Log.getTerm state.Log

  // ** lastLogTermM

  let lastLogTermM _ = zoomM lastLogTerm

  // ** getEntryAt

  let getEntryAt (idx : Index) (state: RaftState) : RaftLogEntry option =
    Log.at idx state.Log

  // ** getEntryAtM

  let getEntryAtM (idx: Index) = zoomM (getEntryAt idx)

  // ** getEntriesUntil

  let private getEntriesUntil (idx : Index) (state: RaftState) : RaftLogEntry option =
    Log.until idx state.Log

  // ** getEntriesUntilM

  let getEntriesUntilM (idx: Index) = zoomM (getEntriesUntil idx)

  // ** entriesUntilExcluding

  let private entriesUntilExcluding (idx: Index) (state: RaftState) =
    Log.untilExcluding idx state.Log

  // ** entriesUntilExcludingM

  let entriesUntilExcludingM (idx: Index) =
    entriesUntilExcluding idx |> zoomM

  // ** handleConfigChange

  let private handleConfigChange (log: RaftLogEntry) (state: RaftState) =
    match log with
    | Configuration(_,_,_,mems,_) ->
      let parting =
        mems
        |> Array.map (fun (mem: RaftMember) -> mem.Id)
        |> Array.contains state.Member.Id
        |> not

      let peers =
        if parting then // we have been kicked out of the configuration
          [| (state.Member.Id, state.Member) |]
          |> Map.ofArray
        else            // we are still part of the new cluster configuration
          Array.map toPair mems
          |> Map.ofArray

      state
      |> setPeers peers
      |> setOldPeers None
    | JointConsensus(_,_,_,changes,_) ->
      let old = state.Peers
      state
      |> applyChanges changes
      |> setOldPeers (Some old)
    | _ -> state

  // ** appendEntry

  //                                   _ _____       _
  //   __ _ _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __ _   _
  //  / _` | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| | | |
  // | (_| | |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |_| |
  //  \__,_| .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|   \__, |
  //       |_|   |_|                                          |___/

  let private appendEntry (log: RaftLogEntry) =
    raft {
      let! state = get

      // create the new log by appending
      let newlog = Log.append log state.Log
      do! put { state with Log = newlog }

      // get back the entries just added
      // (with correct monotonic idx's)
      return Log.getn (LogEntry.depth log) newlog
    }

  // ** appendEntryM

  let appendEntryM (log: RaftLogEntry) =
    raft {
      let! result = appendEntry log
      match result with
      | Some entries -> do! persistLog entries
      | _ -> ()
      return result
    }

  // ** createEntryM

  //                      _       _____       _
  //   ___ _ __ ___  __ _| |_ ___| ____|_ __ | |_ _ __ _   _
  //  / __| '__/ _ \/ _` | __/ _ \  _| | '_ \| __| '__| | | |
  // | (__| | |  __/ (_| | ||  __/ |___| | | | |_| |  | |_| |
  //  \___|_|  \___|\__,_|\__\___|_____|_| |_|\__|_|   \__, |
  //                                                   |___/

  let createEntryM (entry: StateMachine) =
    raft {
      let! state = get
      let log = LogEntry(IrisId.Create(),index 0,state.CurrentTerm,entry,None)
      return! appendEntryM log
    }

  // ** updateLog

  let updateLog (log: RaftLog) (state: RaftState) =
    { state with Log = log }

  // ** updateLogEntries

  let updateLogEntries (entries: RaftLogEntry) (state: RaftState) =
    { state with
        Log = { Index = LogEntry.getIndex entries
                Depth = LogEntry.depth entries
                Data  = Some entries } }

  // ** removeEntry

  /// Delete a log entry at the index specified. Returns the original value if
  /// the record is not found.
  let private removeEntry idx (cbs: IRaftCallbacks) state =
    match Log.at idx state.Log with
    | Some log ->
      match LogEntry.pop log with
      | Some newlog ->
        match Log.until idx state.Log with
          | Some items -> LogEntry.iter (fun _ entry -> cbs.DeleteLog entry) items
          | _ -> ()
        updateLogEntries newlog state
      | _ ->
        cbs.DeleteLog log
        updateLog Log.empty state
    | _ -> state

  // ** removeEntryM

  let removeEntryM idx =
    raft {
      let! env = read
      do! removeEntry idx env |> modify
    }

  // ** makeResponse

  /////////////////////////////////////////////////////////////////////////////
  //     _    Receive                    _ _____       _        _            //
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___  //
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __| //
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \ //
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/ //
  //         |_|   |_|                                                       //
  /////////////////////////////////////////////////////////////////////////////

  /// Preliminary Checks on the AppendEntry value
  let private makeResponse (nid: MemberId option) (msg: AppendEntries) =
    raft {
      let! state = get

      let term = currentTerm state
      let current = currentIndex state
      let first =
        match firstIndex term state with
        | Some idx -> idx
        | _        -> index 0

      let resp =
        { Term         = term
          Success      = false
          CurrentIndex = current
          FirstIndex   = first }

      // 1) If this mem is currently candidate and both its and the requests
      // term are equal, we become follower and reset VotedFor.
      let candidate = isCandidate state
      let newLeader = isLeader state && numMembers state = 1
      if (candidate || newLeader) && currentTerm state = msg.Term then
        do! voteFor None
        do! setLeaderM nid
        do! becomeFollower ()
        return Right resp
      // 2) Else, if the current mem's term value is lower than the requests
      // term, we take become follower and set our own term to higher value.
      elif currentTerm state < msg.Term then
        do! setTermM msg.Term
        do! setLeaderM nid
        do! becomeFollower ()
        return Right { resp with Term = msg.Term }
      // 3) Else, finally, if the msg's Term is lower than our own we reject the
      // the request entirely.
      elif msg.Term < currentTerm state then
        return Left { resp with CurrentIndex = currentIndex state }
      else
        return Right resp
    }

  // ** handleConflicts

  // If an existing entry conflicts with a new one (same index
  // but different terms), delete the existing entry and all that
  // follow it (§5.3)
  let private handleConflicts (request: AppendEntries) =
    raft {
      let idx = request.PrevLogIdx + index 1
      let! local = getEntryAtM idx

      match request.Entries with
      | Some entries ->
        let remote = LogEntry.last entries
        // find the entry in the local log that corresponds to position of
        // then log in the request and compare their terms
        match local with
        | Some entry ->
          if LogEntry.getTerm entry <> LogEntry.getTerm remote then
            // removes entry at idx (and all following entries)
            do! removeEntryM idx
        | _ -> ()
      | _ ->
        if Option.isSome local then
          do! removeEntryM idx
    }

  // ** applyRemainder

  let private applyRemainder (msg : AppendEntries) (resp : AppendResponse) =
    raft {
      match msg.Entries with
      | Some entries ->
        let! result = appendEntryM entries
        match result with
        | Some log ->
          let! fst = currentTermM () >>= firstIndexM
          let fidx =
            match fst with
            | Some fidx -> fidx
            | _         -> msg.PrevLogIdx + (log |> LogEntry.depth |> int |> index)
          return { resp with
                    CurrentIndex = LogEntry.getIndex log
                    FirstIndex   = fidx }
        | _ -> return resp
      | _ -> return resp
    }

  // ** maybeSetCommitIdx

  /// If leaderCommit > commitIndex, set commitIndex =
  /// min(leaderCommit, index of most recent entry)
  let private maybeSetCommitIdx (msg : AppendEntries) =
    raft {
      let! state = get
      let cmmtidx = commitIndex state
      let ldridx = msg.LeaderCommit
      if cmmtidx < ldridx then
        let lastLogIdx = max (currentIndex state) (index 1)
        let newIndex = min lastLogIdx msg.LeaderCommit
        do! setCommitIndexM newIndex
    }

  // ** processEntry

  let private processEntry nid msg resp =
    raft {
      do! handleConflicts msg
      let! response = applyRemainder msg resp
      do! maybeSetCommitIdx msg
      do! setLeaderM nid
      return { response with Success = true }
    }

  // ** checkAndProcess

  ///  2. Reply false if log doesn't contain an entry at prevLogIndex whose
  /// term matches prevLogTerm (§5.3)
  let private checkAndProcess entry nid msg resp =
    raft {
      let! current = currentIndexM ()

      if current < msg.PrevLogIdx then
        do! msg.PrevLogIdx
            |> sprintf "Failed (ci: %d) < (prev log idx: %d)" current
            |> error "receiveAppendEntries"
        return resp
      else
        let term = LogEntry.getTerm entry
        if term <> msg.PrevLogTerm then
          do! sprintf "Failed (term %d) != (prev log term %d) (ci: %d) (prev log idx: %d)"
                  term
                  msg.PrevLogTerm
                  current
                  msg.PrevLogIdx
              |> error "receiveAppendEntries"
          let response = { resp with CurrentIndex = msg.PrevLogIdx - index 1 }
          do! removeEntryM msg.PrevLogIdx
          return response
        else
          return! processEntry nid msg resp
    }

  // ** updateMemberIndices

  /////////////////////////////////////////////////////////////////////////////
  //     _                               _ _____       _        _            //
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___  //
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __| //
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \ //
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/ //
  //         |_|   |_|                                                       //
  /////////////////////////////////////////////////////////////////////////////

  let private updateMemberIndices (resp : AppendResponse) (mem : RaftMember) =
    raft {
      let peer =
        { mem with
            NextIndex  = resp.CurrentIndex + index 1
            MatchIndex = resp.CurrentIndex }

      let! current = currentIndexM ()

      let notVoting = not (Member.isVoting peer)
      let notLogs   = not (Member.hasSufficientLogs peer)
      let idxOk     = current <= resp.CurrentIndex + index 1

      if notVoting && idxOk && notLogs then
        let updated = Member.setHasSufficientLogs peer
        do! updateMemberM updated
      else
        do! updateMemberM peer
    }

  // ** shouldCommit

  let private shouldCommit peers state resp =
    let folder (votes : int) nid (mem : RaftMember) =
      if nid = state.Member.Id || not (Member.isVoting mem) then
        votes
      elif mem.MatchIndex > 0<index> then
        match getEntryAt mem.MatchIndex state with
          | Some entry ->
            if LogEntry.getTerm entry = state.CurrentTerm && resp.CurrentIndex <= mem.MatchIndex
            then votes + 1
            else votes
          | _ -> votes
      else votes

    let commit = commitIndex state
    let num = countMembers peers
    let votes = Map.fold folder 1 peers

    (num / 2) < votes && commit < resp.CurrentIndex

  // ** updateCommitIndex

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

  // ** receiveAppendEntries

  let receiveAppendEntries (nid: MemberId option) (msg: AppendEntries) =
    raft {
      do! setTimeoutElapsedM 0<ms>      // reset timer, so we don't start an election

      // log this if any entries are to be processed
      if Option.isSome msg.Entries then
        let! current = currentIndexM ()
        let str =
          sprintf "from: %A term: %d (ci: %d) (lc-idx: %d) (pli: %d) (plt: %d) (entries: %d)"
                     nid
                     msg.Term
                     current
                     msg.LeaderCommit
                     msg.PrevLogIdx
                     msg.PrevLogTerm
                     (Option.get msg.Entries |> LogEntry.depth)    // let the world know
        do! debug "receiveAppendEntries" str

      let! result = makeResponse nid msg  // check terms et al match, fail otherwise

      match result with
      | Right resp ->
        // this is not the first AppendEntry we're receiving
        if msg.PrevLogIdx > index 0 then
          let! entry = getEntryAtM msg.PrevLogIdx
          match entry with
          | Some log ->
            return! checkAndProcess log nid msg resp
          | _ ->
            do! msg.PrevLogIdx
                |> String.format "Failed. No log at (prev-log-idx: {0})"
                |> error "receiveAppendEntries"
            return resp
        else
          return! processEntry nid msg resp
      | Left err -> return err
    }

  // ** receiveAppendEntriesResponse

  let rec receiveAppendEntriesResponse (nid : MemberId) resp =
    raft {
      let! mem = getMemberM nid
      match mem with
      | None ->
        do! string nid
            |> sprintf "Failed: NoMember %s"
            |> error "receiveAppendEntriesResponse"

        return!
          string nid
          |> sprintf "Node not found: %s"
          |> Error.asRaftError (tag "receiveAppendEntriesResponse")
          |> failM
      | Some peer ->
        if resp.CurrentIndex <> index 0 && resp.CurrentIndex < peer.MatchIndex then
          let str = sprintf "Failed: peer not up to date yet (ci: %d) (match idx: %d)"
                        resp.CurrentIndex
                        peer.MatchIndex
          do! error "receiveAppendEntriesResponse" str
          // set to current index at follower and try again
          do! updateMemberM { peer with
                                NextIndex = resp.CurrentIndex + 1<index>
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
              let str = sprintf "Failed: (term: %d) < (resp.Term: %d)" term resp.Term
              do! error "receiveAppendEntriesResponse" str
              do! setTermM resp.Term
              do! setLeaderM (Some nid)
              do! becomeFollower ()
            elif term <> resp.Term then
              let str = sprintf "Failed: (term: %d) != (resp.Term: %d)" term resp.Term
              do! error "receiveAppendEntriesResponse" str
            elif not resp.Success then
              // If AppendEntries fails because of log inconsistency:
              // decrement nextIndex and retry (§5.3)
              if resp.CurrentIndex < peer.NextIndex - 1<index> then
                let! idx = currentIndexM ()
                let nextIndex = min (resp.CurrentIndex + 1<index>) idx

                do! nextIndex
                    |> sprintf "Failed: cidx < nxtidx. setting nextIndex for %O to %d" peer.Id
                    |> error "receiveAppendEntriesResponse"

                do! setNextIndexM peer.Id nextIndex
                do! setMatchIndexM peer.Id (nextIndex - 1<index>)
              else
                let nextIndex = peer.NextIndex - index 1

                do! nextIndex
                    |> sprintf "Failed: cidx >= nxtidx. setting nextIndex for %O to %d" peer.Id
                    |> error "receiveAppendEntriesResponse"

                do! setNextIndexM peer.Id nextIndex
                do! setMatchIndexM peer.Id (nextIndex - index 1)
            else
              do! updateMemberIndices resp peer
              do! updateCommitIndex resp
          else
            return!
              "Not Leader"
              |> Error.asRaftError (tag "receiveAppendEntriesResponse")
              |> failM
    }

  // ** sendAppendEntry

  let sendAppendEntry (peer: RaftMember) =
    raft {
      let! state = get
      let entries = getEntriesUntil peer.NextIndex state
      let request = { Term         = state.CurrentTerm
                    ; PrevLogIdx   = index 0
                    ; PrevLogTerm  = term 0
                    ; LeaderCommit = state.CommitIndex
                    ; Entries      = entries }

      if peer.NextIndex > index 1 then
        let! result = getEntryAtM (peer.NextIndex - 1<index>)
        let request = { request with
                          PrevLogIdx = peer.NextIndex - 1<index>
                          PrevLogTerm =
                              match result with
                                | Some(entry) -> LogEntry.getTerm entry
                                | _           -> request.Term }
        do! sendAppendEntriesM peer request
      else
        do! sendAppendEntriesM peer request
    }

  // ** sendRemainingEntries

  let private sendRemainingEntries peerid =
    raft {
      let! peer = getMemberM peerid
      match peer with
      | Some mem ->
        let! entry = getEntryAtM (Member.nextIndex mem)
        if Option.isSome entry then
          do! sendAppendEntry mem
      | _ -> return ()
    }

  // ** sendAllAppendEntriesM

  let sendAllAppendEntriesM () =
    raft {
      let! self = getSelfM ()
      let! peers = logicalPeersM ()

      for KeyValue(id,peer) in peers do
        if id <> self.Id then
          do! sendAppendEntry peer

      do! setTimeoutElapsedM 0<ms>
    }

  // ** createSnapshot

  ///////////////////////////////////////////////////
  //  ____                        _           _    //
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_  //
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __| //
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_  //
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__| //
  //                   |_|                         //
  ///////////////////////////////////////////////////

  /// utiltity to create a snapshot for the current application and raft state
  let createSnapshot (state: RaftState) (data: StateMachine) =
    let peers = Map.toArray state.Peers |> Array.map snd
    Log.snapshot peers data state.Log

  // ** sendInstallSnapshot

  let sendInstallSnapshot mem =
    raft {
      let! state = get
      let! cbs = read

      match cbs.RetrieveSnapshot () with
      | Some (Snapshot(_,idx,term,_,_,_,_) as snapshot) ->
        let is =
          { Term      = state.CurrentTerm
          ; LeaderId  = state.Member.Id
          ; LastIndex = idx
          ; LastTerm  = term
          ; Data      = snapshot
          }
        cbs.SendInstallSnapshot mem is
      | _ -> ()
    }

  // ** responseCommitted

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
          if resp.Term <> LogEntry.getTerm entry then
            return!
              "Entry Invalidated"
              |> Error.asRaftError (tag "responseCommitted")
              |> failM
          else
            let! cidx = commitIndexM ()
            return resp.Index <= cidx
    }

  // ** updateCommitIdx

  let private updateCommitIdx (state: RaftState) =
    let idx =
      if state.NumMembers = 1 then
        currentIndex state
      else
        state.CommitIndex
    { state with CommitIndex = idx }

  // ** handleLog

  let private handleLog entry resp =
    raft {
      let! result = appendEntryM entry

      match result with
      | Some appended ->
        let! state = get
        let! peers = logicalPeersM ()

        // iterate through all peers and call sendAppendEntries to each
        for peer in peers do
          let mem = peer.Value
          if mem.Id <> state.Member.Id then
            let nxtidx = Member.nextIndex mem
            let! cidx = currentIndexM ()

            // calculate whether we need to send a snapshot or not
            // uint's wrap around, so normalize to int first (might cause trouble with big numbers)
            let difference =
              let d = cidx - nxtidx
              if d < 0<index> then 0<index> else d

            if difference <= (index (int state.MaxLogDepth) + 1<index>) then
              // Only send new entries. Don't send the entry to peers who are
              // behind, to prevent them from becoming congested.
              do! sendAppendEntry mem
            else
              // because this mem is way behind in the cluster, get it up to speed
              // with a snapshot
              do! sendInstallSnapshot mem

        do! updateCommitIdx |> modify

        return! currentTermM () >>= fun term ->
                  returnM { resp with
                              Id = LogEntry.getId appended
                              Term = term
                              Index = LogEntry.getIndex appended }
      | _ ->
        return!
          "Append Entry failed"
          |> Error.asRaftError (tag "handleLog")
          |> failM
    }

  // ** receiveEntry

  ///                    _           _____       _
  ///  _ __ ___  ___ ___(_)_   _____| ____|_ __ | |_ _ __ _   _
  /// | '__/ _ \/ __/ _ \ \ \ / / _ \  _| | '_ \| __| '__| | | |
  /// | | |  __/ (_|  __/ |\ V /  __/ |___| | | | |_| |  | |_| |
  /// |_|  \___|\___\___|_| \_/ \___|_____|_| |_|\__|_|   \__, |
  ///                                                     |___/

  let receiveEntry (entry : RaftLogEntry) =
    raft {
      let! state = get
      let resp = { Id = IrisId.Create(); Term = term 0; Index = index 0 }

      if LogEntry.isConfigChange entry && Option.isSome state.ConfigChangeEntry then
        do! debug "receiveEntry" "Error: UnexpectedVotingChange"
        return!
          "Unexpected Voting Change"
          |> Error.asRaftError (tag "receiveEntry")
          |> failM
      elif isLeader state then
        do! state.CurrentTerm
            |> sprintf "(id: %A) (idx: %d) (term: %d)"
              (LogEntry.getId entry)
              (Log.getIndex state.Log + 1<index>)
            |> debug "receiveEntry"

        let! term = currentTermM ()

        match entry with
        | LogEntry(id,_,_,data,_) ->
          let log = LogEntry(id, index 0, term, data, None)
          return! handleLog log resp

        | Configuration(id,_,_,mems,_) ->
          let log = Configuration(id, index 0, term, mems, None)
          return! handleLog log resp

        | JointConsensus(id,_,_,changes,_) ->
          let log = JointConsensus(id, index 0, term, changes, None)
          return! handleLog log resp

        | _ ->
          return!
            "Log Format Error"
            |> Error.asRaftError (tag "receiveEntry")
            |> failM
      else
        return!
          "Not Leader"
          |> Error.asRaftError (tag "receiveEntry")
          |> failM
    }

  // ** calculateChanges

  ////////////////////////////////////////////////////////////////
  //     _                _         _____       _               //
  //    / \   _ __  _ __ | |_   _  | ____|_ __ | |_ _ __ _   _  //
  //   / _ \ | '_ \| '_ \| | | | | |  _| | '_ \| __| '__| | | | //
  //  / ___ \| |_) | |_) | | |_| | | |___| | | | |_| |  | |_| | //
  // /_/   \_\ .__/| .__/|_|\__, | |_____|_| |_|\__|_|   \__, | //
  //         |_|   |_|      |___/                        |___/  //
  ////////////////////////////////////////////////////////////////
  let calculateChanges (oldPeers: Map<MemberId,RaftMember>) (newPeers: Map<MemberId,RaftMember>) =
    let oldmems = Map.toArray oldPeers |> Array.map snd
    let newmems = Map.toArray newPeers |> Array.map snd

    let additions =
      Array.fold
        (fun lst (newmem: RaftMember) ->
          match Array.tryFind (Member.id >> (=) newmem.Id) oldmems with
            | Some _ -> lst
            |      _ -> MemberAdded(newmem) :: lst) [] newmems

    Array.fold
      (fun lst (oldmem: RaftMember) ->
        match Array.tryFind (Member.id >> (=) oldmem.Id) newmems with
          | Some _ -> lst
          | _ -> MemberRemoved(oldmem) :: lst) additions oldmems
    |> List.toArray

  // ** notifyChange

  let notifyChange (cbs: IRaftCallbacks) change =
    match change with
      | MemberAdded(mem)   -> cbs.MemberAdded   mem
      | MemberRemoved(mem) -> cbs.MemberRemoved mem

  // ** applyEntry

  let applyEntry (cbs: IRaftCallbacks) = function
    | JointConsensus(_,_,_,changes,_) ->
      Array.iter (notifyChange cbs) changes
      cbs.JointConsensus changes
    | Configuration(_,_,_,mems,_) -> cbs.Configured mems
    | LogEntry(_,_,_,data,_) -> cbs.ApplyLog data
    | Snapshot(_,_,_,_,_,_,data) as snapshot ->
      cbs.PersistSnapshot snapshot
      cbs.ApplyLog data

  // ** applyEntries

  let applyEntries () =
    raft {
      let! state = get
      let lai = state.LastAppliedIdx
      let coi = state.CommitIndex
      if lai <> coi then
        let logIdx = lai + 1<index>
        let! result = getEntriesUntilM logIdx
        match result with
        | Some entries ->
          let! cbs = read

          let str =
            LogEntry.depth entries
            |> sprintf "applying %d entries to state machine"

          do! info "applyEntries" str

          // Apply log chain in the order it arrived
          let state, change =
            LogEntry.foldr
              (fun (state, current) lg ->
                match lg with
                | Configuration _ as config ->
                  // set the peers map
                  let newstate = handleConfigChange config state
                  // when a new configuration is added, under certain circumstances a mem change
                  // might not have been applied yet, so calculate those dangling changes
                  let changes = calculateChanges state.Peers newstate.Peers
                  // apply dangling changes
                  Array.iter (notifyChange cbs) changes
                  // apply the entry by calling the callback
                  applyEntry cbs config
                  (newstate, None)
                | JointConsensus _ as config ->
                  let state = handleConfigChange config state
                  applyEntry cbs config
                  (state, Some (LogEntry.head config))
                | entry ->
                  applyEntry cbs entry
                  (state, current))
              (state, state.ConfigChangeEntry)
              entries

          do! match change with
              | Some _ -> "setting ConfigChangeEntry to JointConsensus"
              | None   -> "resetting ConfigChangeEntry"
              |> debug "applyEntries"

          do! put { state with ConfigChangeEntry = change }

          if LogEntry.contains LogEntry.isConfiguration entries then
            let selfIncluded (state: RaftState) =
              Map.containsKey state.Member.Id state.Peers
            let! included = selfIncluded |> zoomM
            if not included then
              let str =
                string state.Member.Id
                |> sprintf "self (%s) not included in new configuration"
              do! debug "applyEntries" str
              do! setLeaderM None
              do! becomeFollower ()
            /// snapshot now:
            ///
            /// the cluster was just re-configured, and if any of (possibly) just removed members were
            /// to be added again, the replay log they would receive when joining would cause them to
            /// be automatically being removed again. this is why, after the configuration changes are
            /// done we need to create a snapshot of the raft log, which won't contain those commands.
            do! doSnapshot()

          let! state = get
          if not (isLeader state) && LogEntry.contains LogEntry.isConfiguration entries then
            do! debug "applyEntries" "not leader and new configuration is applied. Updating mems."
            for kv in state.Peers do
              if kv.Value.Status <> Running then
                do! updateMemberM { kv.Value with Status = Running; Voting = true }

          let idx = LogEntry.getIndex entries
          do! debug "applyEntries" <| sprintf "setting LastAppliedIndex to %d" idx
          do! setLastAppliedIdxM idx
        | _ ->
          do! debug "applyEntries" (sprintf "no log entries found for index %d" logIdx)
    }

  // ** receiveInstallSnapshot

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
  let receiveInstallSnapshot (is: InstallSnapshot) =
    raft {
      let! cbs = read
      let! currentTerm = currentTermM ()

      if is.Term < currentTerm then
        return!
          "Invalid Term"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM

      do! setTimeoutElapsedM 0<ms>

      match is.Data with
      | Snapshot(_,idx,_,_,_,mems, _) as snapshot ->

        // IMPROVEMENT: implementent chunked transmission as per paper
        cbs.PersistSnapshot snapshot

        let! state = get

        let! remaining = entriesUntilExcludingM idx

        // update the cluster configuration
        let peers =
          Array.map toPair mems
          |> Map.ofArray
          |> Map.add state.Member.Id state.Member

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

        // set the current leader to mem which sent snapshot
        do! setLeaderM (Some is.LeaderId)

        // apply all entries in the new log
        let! state = get
        match state.Log.Data with
          | Some data ->
            LogEntry.foldr (fun _ entry -> applyEntry cbs entry) () data
          | _ -> failwith "Fatal. Snapshot applied, but log is empty. Aborting."

        // reset the counters,to apply all entries in the log
        do! setLastAppliedIdxM (Log.getIndex state.Log)
        do! setCommitIndexM (Log.getIndex state.Log)

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
                            | _      -> index 0 }

        return ar
      | _ ->
        return!
          "Snapshot Format Error"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM
    }

  // ** doSnapshot

  let doSnapshot () =
    raft {
      let! cbs = read
      let! state = get
      match cbs.PrepareSnapshot state with
      | Some snapshot ->
        do! updateLog snapshot |> modify
        match snapshot.Data with
        | Some snapshot -> cbs.PersistSnapshot snapshot
        | _ -> ()
      | _ -> ()
    }

  // ** maybeSnapshot

  let maybeSnapshot () =
    raft {
      let! state = get
      if Log.length state.Log >= int state.MaxLogDepth then
        do! doSnapshot ()
    }

  // ** majority

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
    if total = 0 || yays = 0 then
      false
    elif yays > total then
      false
    else
      yays > (total / 2)

  // ** regularMajorityM

  /// Determine whether a vote count constitutes a majority in the *regular*
  /// configuration (does not cover the joint consensus state).
  let regularMajorityM votes =
    votingMembersM () >>= fun num ->
      majority num votes |> returnM

  // ** oldConfigMajorityM

  let oldConfigMajorityM votes =
    votingMembersForOldConfigM () >>= fun num ->
      majority num votes |> returnM

  // ** numVotesForConfig

  let numVotesForConfig (self: RaftMember) (votedFor: MemberId option) peers =
    let counter m _ (peer : RaftMember) =
        if (peer.Id <> self.Id) && Member.canVote peer
          then m + 1
          else m

    let start =
      match votedFor with
        | Some(nid) -> if nid = self.Id then 1 else 0
        | _         -> 0

    Map.fold counter start peers

  // ** numVotesForMe

  let numVotesForMe (state: RaftState) =
    numVotesForConfig state.Member state.VotedFor state.Peers

  // ** numVotesForMeM

  let numVotesForMeM _ = zoomM numVotesForMe

  // ** numVotesForMeOldConfig

  let numVotesForMeOldConfig (state: RaftState) =
    match state.OldPeers with
      | Some peers -> numVotesForConfig state.Member state.VotedFor peers
      |      _     -> 0

  // ** numVotesForMeOldConfigM

  let numVotesForMeOldConfigM _ = zoomM numVotesForMeOldConfig

  // ** maybeSetIndex

  /////////////////////////////////////////////////////////////////////////////
  //  ____                                  _                   _            //
  // | __ )  ___  ___ ___  _ __ ___   ___  | |    ___  __ _  __| | ___ _ __  //
  // |  _ \ / _ \/ __/ _ \| '_ ` _ \ / _ \ | |   / _ \/ _` |/ _` |/ _ \ '__| //
  // | |_) |  __/ (_| (_) | | | | | |  __/ | |__|  __/ (_| | (_| |  __/ |    //
  // |____/ \___|\___\___/|_| |_| |_|\___| |_____\___|\__,_|\__,_|\___|_|    //
  /////////////////////////////////////////////////////////////////////////////

  let private maybeSetIndex nid nextIdx matchIdx =
    let mapper peer =
      if Member.isVoting peer && peer.Id <> nid
      then { peer with NextIndex = nextIdx; MatchIndex = matchIdx }
      else peer
    updatePeersM mapper

  // ** becomeLeader

  /// Become leader afer a successful election
  let becomeLeader _ =
    raft {
      let! state = get
      do! info "becomeLeader" "becoming leader"
      let nextidx = currentIndex state + 1<index>
      do! setStateM Leader
      do! setLeaderM (Some state.Member.Id)
      do! maybeSetIndex state.Member.Id nextidx (index 0)
      do! sendAllAppendEntriesM ()
    }

  // ** becomeFollower

  let becomeFollower _ =
    raft {
      do! info "becomeFollower" "becoming follower"
      do! setStateM Follower
    }

  // ** becomeCandidate

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

  /// After timeout a Member must become Candidate
  let becomeCandidate () =
    raft {
      do! info "becomeCandidate" "becoming candidate"
      let! state = get
      let term = state.CurrentTerm + 1<term>
      do! debug "becomeCandidate" <| sprintf "setting term to %d" term
      do! setTermM term
      do! resetVotesM ()
      do! voteForMyself ()
      do! setLeaderM None
      do! setStateM Candidate
      // 150–300ms see page 6 in https://raft.github.io/raft.pdf
      let elapsed = 1<ms> * rand.Next(10, int state.ElectionTimeout)
      do! debug "becomeCandidate" <| sprintf "setting timeoutElapsed to %d" elapsed
      do! setTimeoutElapsedM elapsed
      do! requestAllVotes ()
    }

  // ** receiveVoteResponse

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Send ->                    _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let receiveVoteResponse (nid : MemberId) (vote : VoteResponse) =
    raft {
        let! state = get

        do! (if vote.Granted then "granted" else "not granted")
            |> sprintf "%O responded to vote request with: %s" nid
            |> debug "receiveVoteResponse"

        /// The term must not be bigger than current raft term,
        /// otherwise set term to vote term become follower.
        if vote.Term > state.CurrentTerm then
          do! sprintf "(vote term: %d) > (current term: %d). Setting to %d."
                vote.Term
                state.CurrentTerm
                state.CurrentTerm
              |> debug "receiveVoteResponse"
          do! setTermM vote.Term
          do! setLeaderM (Some nid)
          do! becomeFollower ()

        /// If the vote term is smaller than current term it is considered an
        /// error and the client will be notified.
        elif vote.Term < state.CurrentTerm then
          do! sprintf "Failed: (vote term: %d) < (current term: %d). VoteTermMismatch."
                vote.Term
                state.CurrentTerm
              |> debug "receiveVoteResponse"
          return!
            "Vote Term Mismatch"
            |> Error.asRaftError (tag "receiveVoteResponse")
            |> failM

        /// Process the vote if current state of our Raft must be candidate..
        else
          match state.State with
          | Leader -> return ()
          | Follower ->
            /// ...otherwise we respond with the respective RaftError.
            do! debug "receiveVoteResponse" "Failed: NotCandidate"
            return!
              "Not Candidate"
              |> Error.asRaftError (tag "receiveVoteResponse")
              |> failM
          | Candidate ->
            if vote.Granted then
              let! mem = getMemberM nid
              match mem with
              // Could not find the mem in current configuration(s)
              | None ->
                do! debug "receiveVoteResponse" "Failed: vote granted but NoMember"
                return!
                  "No Node"
                  |> Error.asRaftError (tag "receiveVoteResponse")
                  |> failM
              // found the mem
              | Some mem ->
                do! setVotingM mem true

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

                  do! sprintf "In JointConsensus (majority new config: %b) (majority old config: %b)"
                        newConfig
                        oldConfig
                      |> debug "receiveVoteResponse"

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

                  do! sprintf "(majority for config: %b)" majority
                      |> debug "receiveVoteResponse"

                  if majority then
                    do! becomeLeader ()
      }

  // ** sendVoteRequest

  /// Request a from a given peer
  let sendVoteRequest (mem : RaftMember) =
    raft {
      let! state = get
      let! cbs = read

      let vote =
        { Term         = state.CurrentTerm
          Candidate    = state.Member
          LastLogIndex = Log.getIndex state.Log
          LastLogTerm  = Log.getTerm state.Log }

      do! mem.Status
          |> sprintf "(to: %s) (state: %A)" (string mem.Id)
          |> debug "sendVoteRequest"

      cbs.SendRequestVote mem vote
    }

  // ** requestAllVotes

  let requestAllVotes () =
    raft {
        let! self = getSelfM ()
        let! peers = logicalPeersM ()
        do! info "requestAllVotes" "requesting all votes"
        for peer in peers do
          if self.Id <> peer.Value.Id then
            do! sendVoteRequest peer.Value
      }

  // ** validateTerm

  ///////////////////////////////////////////////////////
  //   ____  Should I?       _    __     __    _       //
  //  / ___|_ __ __ _ _ __ | |_  \ \   / /__ | |_ ___  //
  // | |  _| '__/ _` | '_ \| __|  \ \ / / _ \| __/ _ \ //
  // | |_| | | | (_| | | | | |_    \ V / (_) | ||  __/ //
  //  \____|_|  \__,_|_| |_|\__|    \_/ \___/ \__\___| //
  ///////////////////////////////////////////////////////

  /// if the vote's term is lower than this servers current term,
  /// decline the vote
  let private validateTerm (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Invalid Term")
    (vote.Term < state.CurrentTerm, err)

  // ** alreadyVoted

  let private alreadyVoted (state: RaftState) =
    let err = RaftError (tag "shouldGrantVote","Already Voted")
    (Option.isSome state.VotedFor, err)

  // ** validateLastLog

  let private validateLastLog vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid Last Log")
    let result =
      vote.LastLogTerm = lastLogTerm state &&
      currentIndex state <= vote.LastLogIndex
    (result,err)

  // ** validateLastLogTerm

  let private validateLastLogTerm vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid LastLogTerm")
    (lastLogTerm state < vote.LastLogTerm, err)

  // ** validateCurrentIdx

  let private validateCurrentIdx state =
    let err = RaftError (tag "shouldGrantVote","Invalid Current Index")
    (currentIndex state = index 0, err)

  // ** validateCandiate

  let private validateCandidate (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Candidate Unknown")
    (getMember vote.Candidate.Id state |> Option.isNone, err)

  // ** shouldGrantVote

  let shouldGrantVote (vote: VoteRequest) =
    raft {
      let err = RaftError(tag "shouldGrantVote","Log Incomplete")
      let! state = get
      let result =
        validation {       // predicate               result  input
          return! validate (validateTerm vote)        false   state
          return! validate  alreadyVoted              false   state
          return! validate (validateCandidate vote)   false   state
          return! validate  validateCurrentIdx        true    state
          return! validate (validateLastLogTerm vote) true    state
          return! validate (validateLastLog vote)     true    state
          return (false, err)
        }
        |> runValidation

      if fst result then
        do! vote.Candidate.Id
            |> sprintf "granted vote to %O"
            |> debug "shouldGrantVote"
      else
        do! snd result
            |> sprintf "did not grant vote to %O. reason: %A" vote.Candidate.Id
            |> debug "shouldGrantVote"
      return result
    }

  // ** maybeResetFollower

  ///////////////////////////////////////////////////////////////
  // __     __    _       ____ Receive->                  _    //
  // \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_  //
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __| //
  //   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_  //
  //    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__| //
  //                                   |_|                     //
  ///////////////////////////////////////////////////////////////

  let private maybeResetFollower (nid: MemberId) (vote : VoteRequest) =
    raft {
      let! term = currentTermM ()
      if term < vote.Term then
        do! debug "maybeResetFollower" "current term < vote Term, resetting to follower state"
        do! setTermM vote.Term
        do! setLeaderM (Some nid)
        do! becomeFollower ()
        do! voteFor None
    }

  // ** processVoteRequest

  let private processVoteRequest (vote : VoteRequest) =
    raft {
      let! result = shouldGrantVote vote
      match result with
        | (true,_) ->
          let! leader = isLeaderM ()
          let! candidate = isCandidateM ()
          if not leader && not candidate then
            do! voteForId vote.Candidate.Id
            do! setTimeoutElapsedM 0<ms>
            let! term = currentTermM ()
            return { Term    = term
                     Granted = true
                     Reason  = None }
          else
            do! debug "processVoteRequest" "vote request denied: NotVotingState"
            return!
              "Not Voting State"
              |> Error.asRaftError (tag "processVoteRequest")
              |> failM
        | (false, err) ->
          let! term = currentTermM ()
          return { Term    = term
                   Granted = false
                   Reason  = Some err }
    }

  // ** receiveVoteRequest

  let receiveVoteRequest (nid : MemberId) (vote : VoteRequest) =
    raft {
      let! mem = getMemberM nid
      match mem with
      | Some _ ->
        do! maybeResetFollower nid vote
        let! result = processVoteRequest vote

        let str = sprintf "mem %s requested vote. granted: %b"
                    (string nid)
                    result.Granted
        do! info "receiveVoteRequest" str

        return result
      | _ ->
        do! info "receiveVoteRequest" <| sprintf "requested denied. NoMember %s" (string nid)

        let! trm = currentTermM ()
        let err = RaftError (tag "processVoteRequest", "Not Voting State")
        return { Term    = trm
                 Granted = false
                 Reason  = Some err }
    }

  // ** startElection

  //  ____  _             _     _____ _           _   _
  // / ___|| |_ __ _ _ __| |_  | ____| | ___  ___| |_(_) ___  _ __
  // \___ \| __/ _` | '__| __| |  _| | |/ _ \/ __| __| |/ _ \| '_ \
  //  ___) | || (_| | |  | |_  | |___| |  __/ (__| |_| | (_) | | | |
  // |____/ \__\__,_|_|   \__| |_____|_|\___|\___|\__|_|\___/|_| |_|

  /// start an election by becoming candidate
  let startElection () =
    raft {
      let! state = get
      let str = sprintf "(elapsed: %d) (elec-timeout: %d) (term: %d) (ci: %d)"
                  state.TimeoutElapsed
                  state.ElectionTimeout
                  state.CurrentTerm
                  (currentIndex state)
      do! debug "startElection" str
      do! becomeCandidate ()
    }

  // ** periodic

  //  ____           _           _ _
  // |  _ \ ___ _ __(_) ___   __| (_) ___
  // | |_) / _ \ '__| |/ _ \ / _` | |/ __|
  // |  __/  __/ |  | | (_) | (_| | | (__
  // |_|   \___|_|  |_|\___/ \__,_|_|\___|

  let periodic (elapsed : Timeout) =
    raft {
      let! state = get
      do! setTimeoutElapsedM (state.TimeoutElapsed + elapsed)

      match state.State with
      | Leader ->
        // if in JointConsensus
        let! consensus = inJointConsensusM ()
        let! timedout = requestTimedOutM ()

        if consensus then
          let! waiting = hasNonVotingMembersM () // check if any mems are still marked non-voting/Joining
          if not waiting then                    // are mems are voting and have caught up
            let! term = currentTermM ()
            let resp = { Id = IrisId.Create(); Term = term; Index = index 0 }
            let! mems = getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)
            let log = Configuration(resp.Id, index 0, term, mems, None)
            do! handleLog log resp >>= ignoreM
          else
            do! sendAllAppendEntriesM ()
        // the regular case is we need to ping our followers so as to not provoke an election
        elif timedout then
          do! sendAllAppendEntriesM ()

      | _ ->
        // have to double check the code here to ensure new elections are really only called when
        // not enough votes could be garnered
        let! num = numMembersM ()
        let! timedout = electionTimedOutM ()

        if timedout && num > 1 then
          do! startElection ()
        elif timedout && num = 1 then
          do! becomeLeader ()
        else
          do! recountPeers ()

      let! coi = commitIndexM ()
      let! lai = lastAppliedIdx ()

      if lai < coi then
        do! applyEntries ()

      do! maybeSnapshot ()
    }
