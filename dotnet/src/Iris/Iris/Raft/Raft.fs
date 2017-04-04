namespace Iris.Raft

open System
open FSharpx.Functional
open Iris.Core
open Hopac
open Hopac.Infixes

[<AutoOpen>]
module RaftMonad =

  let warn str = printfn "[RAFT WARNING] %s" str

  /// get current Raft state
  let get = MkRM (fun _ s -> Right (s, s))

  /// update Raft/State to supplied value
  let put s = MkRM (fun _ _ -> Right ((), s))

  /// get the read-only environment value
  let read : RaftM<_,_> = MkRM (fun l s -> Right (l, s))

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

[<RequireQualifiedAccess>]
module Raft =

  let private tag (str: string) = sprintf "Raft.%s" str

  /////////////////////////////////////////////
  //  __  __                       _ _       //
  // |  \/  | ___  _ __   __ _  __| (_) ___  //
  // | |\/| |/ _ \| '_ \ / _` |/ _` | |/ __| //
  // | |  | | (_) | | | | (_| | (_| | | (__  //
  // |_|  |_|\___/|_| |_|\__,_|\__,_|_|\___| //
  /////////////////////////////////////////////
  let currentIndex (state: RaftValue) =
    Log.index state.Log

  let log site level message =
    message
    |> Logger.log level site
    |> returnM

  let debug site str = log site Debug str

  let info site str = log site Info str

  let warn site str = log site Warn str

  let error site str = log site Err str

  let sendAppendEntriesM (mem: RaftMember) (request: AppendEntries) =
    get >>= fun state ->
      read >>= fun cbs ->
        job {
          let msg =
            sprintf "SendAppendEntries: (to: %s) (ci: %d) (term: %d) (leader commit: %d) (prv log idx: %d) (prev log term: %d)"
              (string mem.Id)
              (currentIndex state)
              request.Term
              request.LeaderCommit
              request.PrevLogIdx
              request.PrevLogTerm

          Logger.debug "sendAppendEntriesM" msg

          let result = cbs.SendAppendEntries mem request
          return result
        }
        |> returnM

  let persistVote mem =
    read >>= fun cbs ->
      cbs.PersistVote mem
      |> returnM

  let persistTerm term =
    read >>= fun cbs ->
      cbs.PersistTerm term
      |> returnM

  let persistLog log =
    read >>= fun cbs ->
      cbs.PersistLog log
      |> returnM

  let modify (f: RaftValue -> RaftValue) =
    get >>= (f >> put)

  let zoomM (f: RaftValue -> 'a) =
    get >>= (f >> returnM)

  ///////////////////////////////////////
  //  ____       _            _        //
  // |  _ \ _ __(_)_   ____ _| |_ ___  //
  // | |_) | '__| \ \ / / _` | __/ _ \ //
  // |  __/| |  | |\ V / (_| | ||  __/ //
  // |_|   |_|  |_| \_/ \__,_|\__\___| //
  ///////////////////////////////////////

  let private rand = new System.Random()

  //////////////////////////////////////
  //   ____                _          //
  //  / ___|_ __ ___  __ _| |_ ___    //
  // | |   | '__/ _ \/ _` | __/ _ \   //
  // | |___| | |  __/ (_| | ||  __/   //
  //  \____|_|  \___|\__,_|\__\___|   //
  //////////////////////////////////////

  let mkRaft (self : RaftMember) : RaftValue =
    { Member            = self
      State             = Follower
      CurrentTerm       = 0u
      CurrentLeader     = None
      Peers             = Map.ofList [(self.Id, self)]
      OldPeers          = None
      NumMembers        = 1u
      VotedFor          = None
      Log               = Log.empty
      CommitIndex       = 0u
      LastAppliedIdx    = 0u
      TimeoutElapsed    = 0u
      ElectionTimeout   = 4000u         // msec
      RequestTimeout    = 500u          // msec
      MaxLogDepth       = 50u           // items
      ConfigChangeEntry = None
    }

  /// Is the Raft value in Follower state.
  let isFollower (state: RaftValue) =
    state.State = Follower

  let isFollowerM = fun _ -> zoomM isFollower

  /// Is the Raft value in Candate state.
  let isCandidate (state: RaftValue) =
    state.State = Candidate

  let isCandidateM _ = zoomM isCandidate

  /// Is the Raft value in Leader state
  let isLeader (state: RaftValue) =
    state.State = Leader

  let isLeaderM _ = zoomM isLeader

  let inJointConsensus (state: RaftValue) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus _) -> true
      | _                       -> false

  let inJointConsensusM _ = zoomM inJointConsensus

  let hasNonVotingMembers (state: RaftValue) =
    Map.fold
      (fun b _ n ->
        if b then
          b
        else
          not (Member.hasSufficientLogs n && Member.isVoting n))
      false
      state.Peers

  let hasNonVotingMembersM _ = zoomM hasNonVotingMembers

  ////////////////////////////////////////////////
  //  _   _           _         ___             //
  // | \ | | ___   __| | ___   / _ \ _ __  ___  //
  // |  \| |/ _ \ / _` |/ _ \ | | | | '_ \/ __| //
  // | |\  | (_) | (_| |  __/ | |_| | |_) \__ \ //
  // |_| \_|\___/ \__,_|\___|  \___/| .__/|___/ //
  //                                |_|         //
  ////////////////////////////////////////////////

  let getChanges (state: RaftValue) =
    match state.ConfigChangeEntry with
      | Some (JointConsensus(_,_,_,changes,_)) -> Some changes
      | _ -> None

  let logicalPeers (state: RaftValue) =
    // when setting the NumMembers counter we have to include the old config
    if inJointConsensus state then
        // take the old peers as seed and apply the new peers on top
      match state.OldPeers with
        | Some peers -> Map.fold (fun m k n -> Map.add k n m) peers state.Peers
        | _ -> state.Peers
    else
      state.Peers

  let logicalPeersM _ = zoomM logicalPeers

  let countMembers peers = Map.fold (fun m _ _ -> m + 1u) 0u peers

  let numLogicalPeers (state: RaftValue) =
    logicalPeers state |> countMembers

  let setNumPeers (state: RaftValue) =
    { state with NumMembers = countMembers state.Peers }

  let recountPeers () =
    get >>= (setNumPeers >> put)

  /// Set States Members to supplied Map of Mems. Also cache count of mems.
  let setPeers (peers : Map<MemberId,RaftMember>) (state: RaftValue) =
    { state with Peers = Map.add state.Member.Id state.Member peers }
    |> setNumPeers

  /// Adds a mem to the list of known Members and increments NumMembers counter
  let addMember (mem : RaftMember) (state: RaftValue) : RaftValue =
    let exists = Map.containsKey mem.Id state.Peers
    { state with
        Peers = Map.add mem.Id mem state.Peers
        NumMembers =
          if exists
            then state.NumMembers
            else state.NumMembers + 1u }

  let addMemberM (mem: RaftMember) =
    get >>= (addMember mem >> put)

  /// Alias for `addMember`
  let addPeer = addMember
  let addPeerM = addMemberM

  /// Add a Non-voting Peer to the list of known Members
  let addNonVotingMember (mem : RaftMember) (state: RaftValue) =
    addMember { mem with Voting = false; State = Joining } state

  /// Remove a Peer from the list of known Members and decrement NumMembers counter
  let removeMember (mem : RaftMember) (state: RaftValue) =
    let numMembers =
      if Map.containsKey mem.Id state.Peers
        then state.NumMembers - 1u
        else state.NumMembers

    { state with
        Peers = Map.remove mem.Id state.Peers
        NumMembers = numMembers }

  let updateMember (mem : RaftMember) (cbs: IRaftCallbacks) (state: RaftValue) =
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
              | None ->                 // apply all required changes again
                let folder m = function // but this is an edge case
                  | MemberAdded   peer -> Map.add peer.Id peer m
                  | MemberRemoved peer -> Map.filter (fun k _ -> k <> peer.Id) m
                let changes = getChanges state |> Option.get
                let peers = Array.fold folder state.Peers changes
                if Map.containsKey mem.Id peers
                then Map.add mem.Id mem peers |> Some
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

  let applyChanges changes state =
    let folder _state = function
      | MemberAdded   mem -> addNonVotingMember mem _state
      | MemberRemoved mem -> removeMember       mem _state
    Array.fold folder state changes

  let updateMemberM (mem: RaftMember) =
    read >>= fun env ->
      get >>= (updateMember mem env >> put)

  let addMembers (mems : Map<MemberId,RaftMember>) (state: RaftValue) =
    Map.fold (fun m _ n -> addMember n m) state mems

  let addMembersM (mems: Map<MemberId,RaftMember>) =
    get >>= (addMembers mems >> put)

  let addPeers = addMembers
  let addPeersM = addMembersM

  let addNonVotingMemberM (mem: RaftMember) =
    get >>= (addNonVotingMember mem >> put)

  let removeMemberM (mem: RaftMember) =
    get >>= (removeMember mem >> put)

  let hasMember (nid : MemberId) (state: RaftValue) =
    Map.containsKey nid state.Peers

  let hasMemberM _ = hasMember >> zoomM

  let getMember (nid : MemberId) (state: RaftValue) =
    if inJointConsensus state then
      logicalPeers state |> Map.tryFind nid
    else
      Map.tryFind nid state.Peers

  /// Find a peer by its Id. Return None if not found.
  let getMemberM nid = getMember nid |> zoomM

  let setMemberStateM (nid: MemberId) state =
    getMemberM nid >>=
      fun result ->
        match result with
        | Some mem -> updateMemberM { mem with State = state }
        | _         -> returnM ()

  let getMembers (state: RaftValue) = state.Peers
  let getMembersM _ = zoomM getMembers

  let getSelf (state: RaftValue) = state.Member
  let getSelfM _ = zoomM getSelf

  let setSelf (mem: RaftMember) (state: RaftValue) =
    { state with Member = mem }

  let setSelfM mem =
    setSelf mem |> modify

  let lastConfigChange (state: RaftValue) =
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
  let setTerm (term : Term) (state: RaftValue) =
    { state with CurrentTerm = term }

  /// Set CurrentTerm to supplied value. Monadic action.
  let setTermM (term : Term) =
    raft {
      do! setTerm term |> modify
      do! persistTerm term
    }

  /// Set current RaftState to supplied state.
  let setState (rs : RaftState) (env: IRaftCallbacks) (state: RaftValue) =
    env.StateChanged state.State rs
    { state with
        State = rs
        CurrentLeader =
          if rs = Leader
          then Some(state.Member.Id)
          else state.CurrentLeader }

  /// Set current RaftState to supplied state. Monadic action.
  let setStateM (state : RaftState) =
    read >>= (setState state >> modify)

  /// Get current RaftState: Leader, Candidate or Follower
  let getState (state: RaftValue) =
    state.State

  /// Get current RaftState. Monadic action.
  let getStateM _ = zoomM getState

  let getMaxLogDepth (state: RaftValue) =
    state.MaxLogDepth

  let getMaxLogDepthM _ = zoomM getMaxLogDepth

  let setMaxLogDepth (depth: Long) (state: RaftValue) =
    { state with MaxLogDepth = depth }

  let setMaxLogDepthM (depth: Long) =
    setMaxLogDepth depth |> modify

  /// Get Member associated with supplied raft value.
  let self (state: RaftValue) =
    state.Member

  /// Get Member associated with supplied raft value. Monadic action.
  let selfM _ = zoomM self

  let setOldPeers (peers : Map<MemberId,RaftMember> option) (state: RaftValue) =
    { state with OldPeers = peers  } |> setNumPeers

  /// Set States Members to supplied Map of Members. Monadic action.
  let setPeersM (peers: Map<_,_>) =
    setPeers peers |> modify

  /// Set States Members to supplied Map of Members. Monadic action.
  let setOldPeersM (peers: Map<_,_> option) =
    setOldPeers peers |> modify

  /// Map over States Members with supplied mapping function
  let updatePeers (f: RaftMember -> RaftMember) (state: RaftValue) =
    { state with Peers = Map.map (fun _ v -> f v) state.Peers }

  /// Map over States Members with supplied mapping function. Monadic action
  let updatePeersM (f: RaftMember -> RaftMember) =
    updatePeers f |> modify

  /// Set States CurrentLeader field to supplied MemberId.
  let setLeader (leader : MemberId option) (state: RaftValue) =
    { state with CurrentLeader = leader }

  /// Set States CurrentLeader field to supplied MemberId. Monadic action.
  let setLeaderM (leader : MemberId option) =
    setLeader leader |> modify

  /// Set the nextIndex field on Member corresponding to supplied Id (should it
  /// exist, that is).
  let setNextIndex (nid : MemberId) idx cbs (state: RaftValue) =
    let mem = getMember nid state
    let nextidx = if idx < 1u then 1u else idx
    match mem with
      | Some mem -> updateMember { mem with NextIndex = nextidx } cbs state
      | _         -> state

  /// Set the nextIndex field on Member corresponding to supplied Id (should it
  /// exist, that is) and supplied index. Monadic action.
  let setNextIndexM (nid : MemberId) idx =
    read >>= (setNextIndex nid idx >> modify)

  /// Set the nextIndex field on all Members to supplied index.
  let setAllNextIndex idx (state: RaftValue) =
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

  /// Set the matchIndex field on Member to supplied index.
  let setMatchIndex nid idx env (state: RaftValue) =
    let mem = getMember nid state
    match mem with
      | Some peer -> updateMember { peer with MatchIndex = idx } env state
      | _         -> state

  let setMatchIndexM nid idx =
    read >>= (setMatchIndex nid idx >> modify)

  /// Set the matchIndex field on all Members to supplied index.
  let setAllMatchIndex idx (state: RaftValue) =
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
  let voteFor (mem : RaftMember option) =
    let doVoteFor state =
      { state with VotedFor = Option.map (fun (n : RaftMember) -> n.Id) mem }

    raft {
      let! state = get
      do! persistVote mem
      do! doVoteFor state |> put
    }

  /// Remeber who we have voted for in current election
  let voteForId (nid : MemberId)  =
    raft {
      let! mem = getMemberM nid
      do! voteFor mem
    }

  let resetVotes (state: RaftValue) =
    let resetter _ peer = Member.voteForMe peer false
    { state with
        Peers = Map.map resetter state.Peers
        OldPeers =
          match state.OldPeers with
            | Some peers -> Map.map resetter peers |> Some
            | _ -> None }

  let resetVotesM _ =
    resetVotes |> modify

  let voteForMyself _ =
    get >>= fun state -> voteFor (Some state.Member)

  let votedForMyself (state: RaftValue) =
    match state.VotedFor with
      | Some(nid) -> nid = state.Member.Id
      | _ -> false

  let votedFor (state: RaftValue) =
    state.VotedFor

  let votedForM _ = zoomM votedFor

  let setVoting (mem : RaftMember) (vote : bool) (state: RaftValue) =
    let updated = Member.voteForMe mem vote
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

  let setVotingM (mem: RaftMember) (vote: bool) =
    raft {
      let msg = sprintf "setting mem %s voting to %b" (string mem.Id) vote
      do! debug "setVotingM" msg
      do! setVoting mem vote |> modify
    }

  let currentIndexM _ = zoomM currentIndex

  let numMembers (state: RaftValue) =
    state.NumMembers

  let numMembersM _ = zoomM numMembers

  let numPeers = numMembers
  let numPeersM = numMembersM

  let numOldPeers (state: RaftValue) =
    match state.OldPeers with
      | Some peers -> Map.fold (fun m _ _ -> m + 1u) 0u peers
      |      _     -> 0u

  let numOldPeersM _ = zoomM numOldPeers

  let votingMembersForConfig peers =
    let counter r _ n =
      if Member.isVoting n then r + 1u else r
    Map.fold counter 0u peers

  let votingMembers (state: RaftValue) =
    votingMembersForConfig state.Peers

  let votingMembersM _ = zoomM votingMembers

  let votingMembersForOldConfig (state: RaftValue) =
    match state.OldPeers with
      | Some peers -> votingMembersForConfig peers
      | _ -> 0u

  let votingMembersForOldConfigM _ = zoomM votingMembersForOldConfig

  let numLogs (state: RaftValue) =
    Log.length state.Log

  let numLogsM _ = zoomM numLogs

  let currentTerm (state: RaftValue) =
    state.CurrentTerm

  let currentTermM _ = zoomM currentTerm

  let firstIndex (term: Term) (state: RaftValue) =
    Log.firstIndex term state.Log

  let firstIndexM (term: Term) =
    firstIndex term |> zoomM

  let currentLeader (state: RaftValue) =
    state.CurrentLeader

  let currentLeaderM _ = zoomM currentLeader

  let getLeader (state: RaftValue) =
    currentLeader state |> Option.bind (flip getMember state)

  let commitIndex (state: RaftValue) =
    state.CommitIndex

  let commitIndexM _ = zoomM commitIndex

  let setCommitIndex (idx : Index) (state: RaftValue) =
    { state with CommitIndex = idx }

  let setCommitIndexM (idx : Index) =
    setCommitIndex idx |> modify

  let requestTimedOut (state: RaftValue) : bool =
    state.RequestTimeout <= state.TimeoutElapsed

  let requestTimedOutM _ = zoomM requestTimedOut

  let electionTimedOut (state: RaftValue) : bool =
    state.ElectionTimeout <= state.TimeoutElapsed

  let electionTimedOutM _ = zoomM electionTimedOut

  let electionTimeout (state: RaftValue) =
    state.ElectionTimeout

  let electionTimeoutM _ = zoomM electionTimeout

  let timeoutElapsed (state: RaftValue) =
    state.TimeoutElapsed

  let timeoutElapsedM _ = zoomM timeoutElapsed

  let setTimeoutElapsed (elapsed: Long) (state: RaftValue) =
    { state with TimeoutElapsed = elapsed }

  let setTimeoutElapsedM (elapsed: Long) =
    setTimeoutElapsed elapsed |> modify

  let requestTimeout (state: RaftValue) =
    state.RequestTimeout

  let requestTimeoutM _ = zoomM requestTimeout

  let setRequestTimeout (timeout : Long) (state: RaftValue) =
    { state with RequestTimeout = timeout }

  let setRequestTimeoutM (timeout: Long) =
    setRequestTimeout timeout |> modify

  let setElectionTimeout (timeout : Long) (state: RaftValue) =
    { state with ElectionTimeout = timeout }

  let setElectionTimeoutM (timeout: Long) =
    setElectionTimeout timeout |> modify

  let lastAppliedIdx (state: RaftValue) =
    state.LastAppliedIdx

  let lastAppliedIdxM _ = zoomM lastAppliedIdx

  let setLastAppliedIdx (idx : Index) (state: RaftValue) =
    { state with LastAppliedIdx = idx }

  let setLastAppliedIdxM (idx: Index) =
    setLastAppliedIdx idx |> modify

  let maxLogDepth (state: RaftValue) = state.MaxLogDepth

  let maxLogDepthM _ = zoomM maxLogDepth

  let becomeFollower _ =
    raft {
      do! info "becomeFollower" "becoming follower"
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

  let lastLogTerm (state: RaftValue) =
    Log.term state.Log

  let lastLogTermM _ = zoomM lastLogTerm

  let getEntryAt (idx : Index) (state: RaftValue) : RaftLogEntry option =
    Log.at idx state.Log

  let getEntryAtM (idx: Index) = zoomM (getEntryAt idx)

  let getEntriesUntil (idx : Index) (state: RaftValue) : RaftLogEntry option =
    Log.until idx state.Log

  let getEntriesUntilM (idx: Index) = zoomM (getEntriesUntil idx)

  let entriesUntilExcluding (idx: Index) (state: RaftValue) =
    Log.untilExcluding idx state.Log

  let entriesUntilExcludingM (idx: Index) =
    entriesUntilExcluding idx |> zoomM

  let handleConfigChange (log: RaftLogEntry) (state: RaftValue) =
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

        setPeers peers state
        |> setOldPeers None
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

  let appendEntry (log: RaftLogEntry) =
    raft {
      let! state = get

      // create the new log by appending
      let newlog = Log.append log state.Log
      do! put { state with Log = newlog }

      // get back the entries just added
      // (with correct monotonic idx's)
      return Log.getn (LogEntry.depth log) newlog
    }

  let appendEntryM (log: RaftLogEntry) =
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

  let createEntryM (entry: StateMachine) =
    raft {
      let! state = get
      let log = LogEntry(Id.Create(),0u,state.CurrentTerm,entry,None)
      return! appendEntryM log
    }

  let updateLog (log: RaftLog) (state: RaftValue) =
    { state with Log = log }

  let updateLogEntries (entries: RaftLogEntry) (state: RaftValue) =
    { state with
        Log = { Index = LogEntry.index entries
                Depth = LogEntry.depth entries
                Data  = Some entries } }

  /// Delete a log entry at the index specified. Returns the original value if
  /// the record is not found.
  let removeEntry idx (cbs: IRaftCallbacks) state =
    match Log.at idx state.Log with
      | Some log ->
        match LogEntry.pop log with
          | Some newlog ->
            match Log.until idx state.Log with
              | Some items ->
                LogEntry.iter (fun _ entry -> cbs.DeleteLog entry) items
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
  let private makeResponse (msg : AppendEntries) =
    raft {
      let! state = get

      let term = currentTerm state
      let current = currentIndex state
      let first =
        match firstIndex term state with
        | Some idx -> idx
        | _        -> 0u

      let resp =
        { Term         = term
        ; Success      = false
        ; CurrentIndex = current
        ; FirstIndex   = first }

      // 1) If this mem is currently candidate and both its and the requests
      // term are equal, we become follower and reset VotedFor.
      let candidate = isCandidate state
      let newLeader = isLeader state && numMembers state = 1u
      if (candidate || newLeader) && currentTerm state = msg.Term then
        do! voteFor None
        do! becomeFollower ()
        return Right resp
      // 2) Else, if the current mem's term value is lower than the requests
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
  let private handleConflicts (request: AppendEntries) =
    raft {
      let idx = request.PrevLogIdx + 1u
      let! local = getEntryAtM idx

      match request.Entries with
      | Some entries ->
        let remote = LogEntry.last entries
        // find the entry in the local log that corresponds to position of
        // then log in the request and compare their terms
        match local with
          | Some entry ->
            if LogEntry.term entry <> LogEntry.term remote then
              // removes entry at idx (and all following entries)
              do! removeEntryM idx
          | _ -> ()
      | _ ->
        if Option.isSome local then
          do! removeEntryM idx
    }

  let private applyRemainder (msg : AppendEntries) (resp : AppendResponse) =
    raft {
      match msg.Entries with
      | Some entries ->
        let! result = appendEntryM entries
        match result with
          | Some log ->
            let! fst = currentTermM () >>= firstIndexM
            return { resp with
                      CurrentIndex = LogEntry.index log
                      FirstIndex   =
                          match fst with
                            | Some fidx -> fidx
                            | _         -> msg.PrevLogIdx + LogEntry.depth log }
          | _ -> return resp
      | _ -> return resp
    }

  /// If leaderCommit > commitIndex, set commitIndex =
  /// min(leaderCommit, index of most recent entry)
  let private maybeSetCommitIdx (msg : AppendEntries) =
    raft {
      let! state = get
      let cmmtidx = commitIndex state
      let ldridx = msg.LeaderCommit
      if cmmtidx < ldridx then
        let lastLogIdx = max (currentIndex state) 1u
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
        let msg = sprintf "Failed (ci: %d) < (prev log idx: %d)" current msg.PrevLogIdx
        do! debug "receiveAppendEntries" msg
        return resp
      else
        let term = LogEntry.term entry
        if term <> msg.PrevLogTerm then
          let str = sprintf "Failed (term %d) != (prev log term %d) (ci: %d) (prev log idx: %d)"
                        term
                        msg.PrevLogTerm
                        current
                        msg.PrevLogIdx
          do! debug "receiveAppendEntries" str
          let response = { resp with CurrentIndex = msg.PrevLogIdx - 1u }
          do! removeEntryM msg.PrevLogIdx
          return response
        else
          return! processEntry nid msg resp
    }

  let receiveAppendEntries (nid: MemberId option) (msg: AppendEntries) =
    raft {
      do! setTimeoutElapsedM 0u      // reset timer, so we don't start an election

      // log this if any entries are to be processed
      if Option.isSome msg.Entries then
        let! current = currentIndexM ()
        let str =
          sprintf "(from: %s) (term: %d) (ci: %d) (lc-idx: %d) (pli: %d) (plt: %d) (entries: %d)"
                     (string nid)
                     msg.Term
                     current
                     msg.LeaderCommit
                     msg.PrevLogIdx
                     msg.PrevLogTerm
                     (Option.get msg.Entries |> LogEntry.depth)    // let the world know
        do! debug "receiveAppendEntries" str

      let! result = makeResponse msg  // check terms et al match, fail otherwise

      match result with
        | Right resp ->
          // this is not the first AppendEntry we're reeiving
          if msg.PrevLogIdx > 0u then
            let! entry = getEntryAtM msg.PrevLogIdx
            match entry with
              | Some log -> return! checkAndProcess log nid msg resp
              | _        ->
                let str = sprintf "Failed. No log at (prev log idx %d)" msg.PrevLogIdx
                do! debug "receiveAppendEntries" str
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

  let private updateMemberIndices (resp : AppendResponse) (mem : RaftMember) =
    raft {
      let peer =
        { mem with
            NextIndex  = resp.CurrentIndex + 1u
            MatchIndex = resp.CurrentIndex }

      let! current = currentIndexM ()

      let notVoting = not (Member.isVoting peer)
      let notLogs   = not (Member.hasSufficientLogs peer)
      let idxOk     = current <= resp.CurrentIndex + 1u

      if notVoting && idxOk && notLogs then
        let updated = Member.setHasSufficientLogs peer
        do! updateMemberM updated
      else
        do! updateMemberM peer
    }

  let private shouldCommit peers state resp =
    let folder (votes : Long) nid (mem : RaftMember) =
      if nid = state.Member.Id || not (Member.isVoting mem) then
        votes
      elif mem.MatchIndex > 0u then
        match getEntryAt mem.MatchIndex state with
          | Some entry ->
            if LogEntry.term entry = state.CurrentTerm && resp.CurrentIndex <= mem.MatchIndex
            then votes + 1u
            else votes
          | _ -> votes
      else votes

    let commit = commitIndex state
    let num = countMembers peers
    let votes = Map.fold folder 1u peers

    (num / 2u) < votes && commit < resp.CurrentIndex

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

  let sendAppendEntry (peer: RaftMember) =
    raft {
      let! state = get
      let entries = getEntriesUntil peer.NextIndex state
      let request = { Term         = state.CurrentTerm
                    ; PrevLogIdx   = 0u
                    ; PrevLogTerm  = 0u
                    ; LeaderCommit = state.CommitIndex
                    ; Entries      = entries }

      if peer.NextIndex > 1u then
        let! result = getEntryAtM (peer.NextIndex - 1u)
        let request = { request with
                          PrevLogIdx = peer.NextIndex - 1u
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
      let! peer = getMemberM peerid
      match peer with
        | Some mem ->
          let! entry = getEntryAtM (Member.getNextIndex mem)
          if Option.isSome entry then
            let! request = sendAppendEntry mem
            return Hopac.run request
          else
            return None
        | _ ->
          return None
    }

  let rec receiveAppendEntriesResponse (nid : MemberId) resp =
    raft {
      let! mem = getMemberM nid
      match mem with
      | None ->
        do! debug "receiveAppendEntriesResponse" (sprintf "Failed: NoMember %s" (string nid))
        return!
          sprintf "Node not found: %s" (string nid)
          |> Error.asRaftError (tag "receiveAppendEntriesResponse")
          |> failM
      | Some peer ->
        if resp.CurrentIndex <> 0u && resp.CurrentIndex < peer.MatchIndex then
          let str = sprintf "Failed: peer not up to date yet (ci: %d) (match idx: %d)"
                        resp.CurrentIndex
                        peer.MatchIndex
          do! debug "receiveAppendEntriesResponse" str
          // set to current index at follower and try again
          do! updateMemberM { peer with
                                NextIndex = resp.CurrentIndex + 1u
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
              do! debug "receiveAppendEntriesResponse" str
              do! setTermM resp.Term
              do! becomeFollower ()
              return ()
            elif term <> resp.Term then
              let str = sprintf "Failed: (term: %d) != (resp.Term: %d)" term resp.Term
              do! debug "receiveAppendEntriesResponse" str
              return ()
            elif not resp.Success then
              // If AppendEntries fails because of log inconsistency:
              // decrement nextIndex and retry (§5.3)
              if resp.CurrentIndex < peer.NextIndex - 1u then
                let! idx = currentIndexM ()
                let nextIndex = min (resp.CurrentIndex + 1u) idx

                let str = sprintf "Failed: cidx < nxtidx. setting nextIndex for %s to %d"
                              (string peer.Id)
                              nextIndex
                do! debug "receiveAppendEntriesResponse" str

                do! setNextIndexM peer.Id nextIndex
                do! setMatchIndexM peer.Id (nextIndex - 1u)
              else
                let nextIndex = peer.NextIndex - 1u

                let str = sprintf "Failed: cidx >= nxtidx. setting nextIndex for %s to %d"
                              (string peer.Id)
                              nextIndex
                do! debug "receiveAppendEntriesResponse" str

                do! setNextIndexM peer.Id nextIndex
                do! setMatchIndexM peer.Id (nextIndex - 1u)
            else
              do! updateMemberIndices resp peer
              do! updateCommitIndex resp
          else
            return!
              "Not Leader"
              |> Error.asRaftError (tag "receiveAppendEntriesResponse")
              |> failM

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

      if Array.length !requests > 0 then
        let responses =
          !requests
          |> Array.map snd
          |> Job.conCollect
          |> Hopac.run
          |> fun arr -> arr.ToArray()
          |> Array.zip (Array.map fst !requests)

        for (mem, response) in responses do
          match response with
          | Some resp ->
            do! receiveAppendEntriesResponse mem.Id resp
            let! peer = getMemberM mem.Id >>= (Option.get >> returnM)
            if peer.State = Failed then
              do! setMemberStateM mem.Id Running
          | _ ->
            do! setMemberStateM mem.Id Failed

      do! setTimeoutElapsedM 0u
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
  let createSnapshot (data: StateMachine) (state: RaftValue) =
    let peers = Map.toArray state.Peers |> Array.map snd
    Log.snapshot peers data state.Log

  let sendInstallSnapshot mem =
    get >>= fun state ->
      read >>= fun cbs ->
        job {
          match cbs.RetrieveSnapshot () with
            | Some (Snapshot(_,idx,term,_,_,_,_) as snapshot) ->
              let is =
                { Term      = state.CurrentTerm
                ; LeaderId  = state.Member.Id
                ; LastIndex = idx
                ; LastTerm  = term
                ; Data      = snapshot
                }
              return cbs.SendInstallSnapshot mem is
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
          if resp.Term <> LogEntry.term entry then
            return!
              "Entry Invalidated"
              |> Error.asRaftError (tag "responseCommitted")
              |> failM
          else
            let! cidx = commitIndexM ()
            return resp.Index <= cidx
    }

  let private updateCommitIdx (state: RaftValue) =
    let idx =
      if state.NumMembers = 1u then
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
          let mem = peer.Value
          if mem.Id <> state.Member.Id then
            let nxtidx = Member.getNextIndex mem
            let! cidx = currentIndexM ()

            // calculate whether we need to send a snapshot or not
            // uint's wrap around, so normalize to int first (might cause trouble with big numbers)
            let difference =
              let d = int cidx - int nxtidx
              if d < 0 then 0u else uint32 d

            if difference <= (state.MaxLogDepth + 1u) then
              // Only send new entries. Don't send the entry to peers who are
              // behind, to prevent them from becoming congested.
              let! request = sendAppendEntry mem
              requests := Array.append [| (mem,request) |] !requests
            else
              // because this mem is way behind in the cluster, get it up to speed
              // with a snapshot
              let! request = sendInstallSnapshot mem
              requests := Array.append [| (mem,request) |] !requests

        let results =
          Array.map snd !requests
          |> Job.conCollect
          |> Hopac.run
          |> fun arr -> arr.ToArray()
          |> Array.zip (Array.map fst !requests)

        for (mem, response) in results do
          match response with
          | Some resp ->
            do! receiveAppendEntriesResponse mem.Id resp
            let! peer = getMemberM mem.Id >>= (Option.get >> returnM)
            if peer.State = Failed then
              do! setMemberStateM mem.Id Running
          | _ ->
            do! setMemberStateM mem.Id Failed

        do! updateCommitIdx |> modify

        return! currentTermM () >>= fun term ->
                  returnM { resp with
                              Id = LogEntry.getId appended
                              Term = term
                              Index = LogEntry.index appended }
      | _ ->
        return!
          "Append Entry failed"
          |> Error.asRaftError (tag "handleLog")
          |> failM
    }

  ///                    _           _____       _
  ///  _ __ ___  ___ ___(_)_   _____| ____|_ __ | |_ _ __ _   _
  /// | '__/ _ \/ __/ _ \ \ \ / / _ \  _| | '_ \| __| '__| | | |
  /// | | |  __/ (_|  __/ |\ V /  __/ |___| | | | |_| |  | |_| |
  /// |_|  \___|\___\___|_| \_/ \___|_____|_| |_|\__|_|   \__, |
  ///                                                     |___/

  let receiveEntry (entry : RaftLogEntry) =
    raft {
      let! state = get
      let resp = { Id = Id.Create(); Term = 0u; Index = 0u }

      if LogEntry.isConfigChange entry && Option.isSome state.ConfigChangeEntry then
        do! debug "receiveEntry" "Failed: UnexpectedVotingChange"
        return!
          "Unexpected Voting Change"
          |> Error.asRaftError (tag "receiveEntry")
          |> failM
      elif isLeader state then
        let str = sprintf "(id: %s) (idx: %d) (term: %d)"
                      ((LogEntry.getId entry).ToString() )
                      (Log.index state.Log + 1u)
                      state.CurrentTerm
        do! debug "receiveEntry" str

        let! term = currentTermM ()

        match entry with
          | LogEntry(id,_,_,data,_) ->
            let log = LogEntry(id, 0u, term, data, None)
            return! handleLog log resp

          | Configuration(id,_,_,mems,_) ->
            let log = Configuration(id, 0u, term, mems, None)
            return! handleLog log resp

          | JointConsensus(id,_,_,changes,_) ->
            let log = JointConsensus(id, 0u, term, changes, None)
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
          match Array.tryFind (Member.getId >> (=) newmem.Id) oldmems with
            | Some _ -> lst
            |      _ -> MemberAdded(newmem) :: lst) [] newmems

    Array.fold
      (fun lst (oldmem: RaftMember) ->
        match Array.tryFind (Member.getId >> (=) oldmem.Id) newmems with
          | Some _ -> lst
          | _ -> MemberRemoved(oldmem) :: lst) additions oldmems
    |> List.toArray

  let notifyChange (cbs: IRaftCallbacks) change =
    match change with
      | MemberAdded(mem)   -> cbs.MemberAdded   mem
      | MemberRemoved(mem) -> cbs.MemberRemoved mem

  let applyEntry (cbs: IRaftCallbacks) = function
    | JointConsensus(_,_,_,changes,_) -> Array.iter (notifyChange cbs) changes
    | Configuration(_,_,_,mems,_) -> cbs.Configured mems
    | LogEntry(_,_,_,data,_) -> cbs.ApplyLog data
    | Snapshot(_,_,_,_,_,_,data) as snapshot ->
      cbs.PersistSnapshot snapshot
      cbs.ApplyLog data

  let applyEntries _ =
    raft {
      let! state = get
      let lai = state.LastAppliedIdx
      let coi = state.CommitIndex
      if lai <> coi then
        let logIdx = lai + 1u
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

          do! debug "applyEntries" (sprintf "setting ConfigChangeEntry to %A" change)
          do! put { state with ConfigChangeEntry = change }

          if LogEntry.contains LogEntry.isConfiguration entries then
            let selfIncluded (state: RaftValue) =
              Map.containsKey state.Member.Id state.Peers
            let! included = selfIncluded |> zoomM
            if not included then
              let str =
                string state.Member.Id
                |> sprintf "self (%s) not included in new configuration"
              do! debug "applyEntries" str
              do! becomeFollower ()

          let! state = get
          if not (isLeader state) && LogEntry.contains LogEntry.isConfiguration entries then
            do! debug "applyEntries" "not leader and new configuration is applied. Updating mems."
            for kv in state.Peers do
              if kv.Value.State <> Running then
                do! updateMemberM { kv.Value with State = Running; Voting = true }

          let idx = LogEntry.index entries
          do! debug "applyEntries" <| sprintf "setting LastAppliedIndex to %d" idx
          do! setLastAppliedIdxM idx
        | _ ->
          do! debug "applyEntries" (sprintf "no log entries found for index %d" logIdx)
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
  let receiveInstallSnapshot (is: InstallSnapshot) =
    raft {
      let! cbs = read
      let! currentTerm = currentTermM ()

      if is.Term < currentTerm then
        return!
          "Invalid Term"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM

      do! setTimeoutElapsedM 0u

      match is.Data with
      | Snapshot(_,idx,_,_,_,mems,data) as snapshot ->

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
                            | _      -> 0u }

        return ar
      | _ ->
        return!
          "Snapshot Format Error"
          |> Error.asRaftError (tag "receiveInstallSnapshot")
          |> failM
    }

  let maybeSnapshot _ =
    raft {
      let! state = get
      if Log.length state.Log >= state.MaxLogDepth then
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
    if total = 0u || yays = 0u then
      false
    elif yays > total then
      false
    else
      yays > (total / 2u)

  /// Determine whether a vote count constitutes a majority in the *regular*
  /// configuration (does not cover the joint consensus state).
  let regularMajorityM votes =
    votingMembersM () >>= fun num ->
      majority num votes |> returnM

  let oldConfigMajorityM votes =
    votingMembersForOldConfigM () >>= fun num ->
      majority num votes |> returnM

  let numVotesForConfig (self: RaftMember) (votedFor: MemberId option) peers =
    let counter m _ (peer : RaftMember) =
        if (peer.Id <> self.Id) && Member.canVote peer
          then m + 1u
          else m

    let start =
      match votedFor with
        | Some(nid) -> if nid = self.Id then 1u else 0u
        | _         -> 0u

    Map.fold counter start peers

  let numVotesForMe (state: RaftValue) =
    numVotesForConfig state.Member state.VotedFor state.Peers

  let numVotesForMeM _ = zoomM numVotesForMe

  let numVotesForMeOldConfig (state: RaftValue) =
    match state.OldPeers with
      | Some peers -> numVotesForConfig state.Member state.VotedFor peers
      |      _     -> 0u

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
      if Member.isVoting peer && peer.Id <> nid
      then { peer with NextIndex = nextIdx; MatchIndex = matchIdx }
      else peer
    updatePeersM mapper

  /// Become leader afer a successful election
  let becomeLeader _ =
    raft {
      let! state = get
      do! info "becomeLeader" "becoming leader"
      let nextidx = currentIndex state + 1u
      do! setStateM Leader
      do! maybeSetIndex state.Member.Id nextidx 0u
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

  let receiveVoteResponse (nid : MemberId) (vote : VoteResponse) =
    raft {
        let! state = get

        let str = sprintf "%s responded to vote request with: %s"
                    (string nid)
                    (if vote.Granted then "granted" else "not granted")
        do! debug "receiveVoteResponse" str

        /// The term must not be bigger than current raft term,
        /// otherwise set term to vote term become follower.
        if vote.Term > state.CurrentTerm then
          let str = sprintf "(vote term: %d) > (current term: %d). Setting to %d."
                      vote.Term
                      state.CurrentTerm
                      state.CurrentTerm
          do! debug "receiveVoteResponse" str
          do! setTermM vote.Term
          do! becomeFollower ()

        /// If the vote term is smaller than current term it is considered an
        /// error and the client will be notified.
        elif vote.Term < state.CurrentTerm then
          let str = sprintf "Failed: (vote term: %d) < (current term: %d). VoteTermMismatch."
                      vote.Term
                      state.CurrentTerm
          do! debug "receiveVoteResponse" str
          return!
            "Vote Term Mismatch"
            |> Error.asRaftError (tag "receiveVoteResponse")
            |> failM

        /// Process the vote if current state of our Raft must be candidate..
        elif state.State = Candidate then

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

                  let str =
                    sprintf "In JointConsensus (majority new config: %b) (majority old config: %b)"
                      newConfig
                      oldConfig
                  do! debug "receiveVoteResponse" str

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

                  do! debug "receiveVoteResponse" <| sprintf "(majority for config: %b)" majority

                  if majority then
                    do! becomeLeader ()

        /// ...otherwise we respond with the respective RaftError.
        else
          do! debug "receiveVoteResponse" "Failed: NotCandidate"
          return!
            "Not Candidate"
            |> Error.asRaftError (tag "receiveVoteResponse")
            |> failM
      }

  /// Request a from a given peer
  let sendVoteRequest (mem : RaftMember) =
    get >>= fun state ->
      read >>= fun cbs ->
        job {
          if mem.State = Running || mem.State = Failed then
            let vote =
              { Term         = state.CurrentTerm
                Candidate    = state.Member
                LastLogIndex = Log.index state.Log
                LastLogTerm  = Log.term state.Log }

            mem.State
            |> sprintf "(to: %s) (state: %A)" (string mem.Id)
            |> Logger.debug "sendVoteRequest"

            let result = cbs.SendRequestVote mem vote
            return result
          else
            mem.State
            |> sprintf "not requesting vote from %s: (voting: %b) (state: %A)"
                (string mem.Id)
                (Member.isVoting mem)
            |> Logger.debug "sendVoteRequest"

            return None
        }
        |> returnM

  let requestAllVotes _ =
    raft {
        let! self = getSelfM ()
        let! peers = logicalPeersM ()

        do! info "requestAllVotes" "requesting all votes"

        let requests = ref [| |]

        for peer in peers do
          if self.Id <> peer.Value.Id then
            let! request = sendVoteRequest peer.Value
            requests := Array.append [| (peer.Value, request) |] !requests

        if Array.length !requests > 0 then
          let responses =
            !requests
            |> Array.map snd
            |> Job.conCollect
            |> Hopac.run
            |> fun arr -> arr.ToArray()
            |> Array.zip (Array.map fst !requests)

          for (mem, response) in responses do
            let! leader = isLeaderM ()
            if not leader then                          // check if raft is already leader
              match response with                     // otherwise process the vote
              | Some resp ->
                do! receiveVoteResponse mem.Id resp
                do! setMemberStateM mem.Id Running
              | _ ->
                do! setMemberStateM mem.Id Failed      // mark mem as failed (calls the callback)
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
  let private validateTerm (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Invalid Term")
    (vote.Term < state.CurrentTerm, err)

  let private alreadyVoted (state: RaftValue) =
    let err = RaftError (tag "shouldGrantVote","Already Voted")
    (Option.isSome state.VotedFor, err)

  let private validateLastLog vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid Last Log")
    let result =
         vote.LastLogTerm = lastLogTerm state &&
         currentIndex state <= vote.LastLogIndex
    (result,err)

  let private validateLastLogTerm vote state =
    let err = RaftError (tag "shouldGrantVote","Invalid LastLogTerm")
    (lastLogTerm state < vote.LastLogTerm, err)

  let private validateCurrentIdx state =
    let err = RaftError (tag "shouldGrantVote","Invalid Current Index")
    (currentIndex state = 0u, err)

  let private validateCandidate (vote: VoteRequest) state =
    let err = RaftError (tag "shouldGrantVote","Candidate Unknown")
    (getMember vote.Candidate.Id state |> Option.isNone, err)

  let shouldGrantVote (vote : VoteRequest) =
    raft {
      let err = RaftError(tag "shouldGrantVote","Log Incomplete")
      let! state = get
      let result =
        validation {       // predicate               result  input
          return! validate (validateTerm vote)        false   state
          return! validate alreadyVoted               false   state
          return! validate (validateCandidate vote)   false   state
          return! validate validateCurrentIdx         true    state
          return! validate (validateLastLogTerm vote) true    state
          return! validate (validateLastLog vote)     true    state
          return (false, err)
        }
        |> runValidation

      if fst result then
        let str = sprintf "granted vote to %s" (string vote.Candidate.Id)
        do! debug "shouldGrantVote" str
      else
        let str = sprintf "did not grant vote to %s. reason: %A"
                    (string vote.Candidate.Id)
                    (snd result)
        do! debug "shouldGrantVote" str
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

  let private maybeResetFollower (vote : VoteRequest) =
    raft {
      let! term = currentTermM ()
      if term < vote.Term then
        do! debug "maybeResetFollower" "term < vote.Term resetting"
        do! setTermM vote.Term
        do! becomeFollower ()
        do! voteFor None
    }

  let private processVoteRequest (vote : VoteRequest) =
    raft {
      let! result = shouldGrantVote vote
      match result with
        | (true,_) ->
          let! leader = isLeaderM ()
          let! candidate = isCandidateM ()
          if not leader && not candidate then
            do! voteForId vote.Candidate.Id
            do! setLeaderM None
            do! setTimeoutElapsedM 0u
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

  let receiveVoteRequest (nid : MemberId) (vote : VoteRequest) =
    raft {
      let! mem = getMemberM nid
      match mem with
        | Some _ ->
          do! maybeResetFollower vote
          let! result = processVoteRequest vote

          let str = sprintf "mem %s requested vote. granted: %b"
                      (string nid)
                      result.Granted
          do! info "receiveVoteRequest" str

          return result
        | _ ->
          do! info "receiveVoteRequest" <| sprintf "requested denied. NoMember %s" (string nid)

          let! term = currentTermM ()
          let err = RaftError (tag "processVoteRequest", "Not Voting State")
          return { Term    = term
                   Granted = false
                   Reason  = Some err }
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

  /// After timeout a Member must become Candidate
  let becomeCandidate () =
    raft {
      do! info "becomeCandidate" "becoming candidate"

      let! state = get

      let term = state.CurrentTerm + 1u
      do! debug "becomeCandidate" <| sprintf "setting term to %d" term
      do! setTermM term

      do! resetVotesM ()
      do! voteForMyself ()
      do! setLeaderM None
      do! setStateM Candidate

      let elapsed = uint32(rand.Next(15, int state.ElectionTimeout))
      do! debug "becomeCandidate" <| sprintf "setting timeoutElapsed to %d" elapsed
      do! setTimeoutElapsedM elapsed

      do! requestAllVotes ()
    }

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
          let! waiting = hasNonVotingMembersM () // check if any mems are still marked non-voting/Joining
          if not waiting then                    // are mems are voting and have caught up
            let! term = currentTermM ()
            let resp = { Id = Id.Create(); Term = term; Index = 0u }
            let! mems = getMembersM () >>= (Map.toArray >> Array.map snd >> returnM)
            let log = Configuration(resp.Id, 0u, term, mems, None)
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

        if timedout && num > 1u then
          do! startElection ()
        elif timedout && num = 1u then
          do! becomeLeader ()
        else
          do! recountPeers ()

      let! coi = commitIndexM ()
      let! lai = lastAppliedIdxM ()

      if lai < coi then
        do! applyEntries ()

      do! maybeSnapshot ()
    }
