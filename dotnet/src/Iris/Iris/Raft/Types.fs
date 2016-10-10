namespace Iris.Raft

open System
open System.Net
open Iris.Core
open Iris.Serialization.Raft
open FlatBuffers

//  _____
// | ____|_ __ _ __ ___  _ __
// |  _| | '__| '__/ _ \| '__|
// | |___| |  | | | (_) | |
// |_____|_|  |_|  \___/|_|

type RaftError =
  | AlreadyVoted
  | AppendEntryFailed
  | CandidateUnknown
  | EntryInvalidated
  | InvalidCurrentIndex
  | InvalidLastLog
  | InvalidLastLogTerm
  | InvalidTerm
  | LogFormatError
  | LogIncomplete
  | NoError
  | NoNode
  | NotCandidate
  | NotLeader
  | NotVotingState
  | ResponseTimeout
  | SnapshotFormatError
  | StaleResponse
  | UnexpectedVotingChange
  | VoteTermMismatch
  | OtherError of string

  with
    member error.ToOffset (builder: FlatBufferBuilder) =
      let tipe =
        match error with
        | AlreadyVoted           -> RaftErrorTypeFB.AlreadyVotedFB
        | AppendEntryFailed      -> RaftErrorTypeFB.AppendEntryFailedFB
        | CandidateUnknown       -> RaftErrorTypeFB.CandidateUnknownFB
        | EntryInvalidated       -> RaftErrorTypeFB.EntryInvalidatedFB
        | InvalidCurrentIndex    -> RaftErrorTypeFB.InvalidCurrentIndexFB
        | InvalidLastLog         -> RaftErrorTypeFB.InvalidLastLogFB
        | InvalidLastLogTerm     -> RaftErrorTypeFB.InvalidLastLogTermFB
        | InvalidTerm            -> RaftErrorTypeFB.InvalidTermFB
        | LogFormatError         -> RaftErrorTypeFB.LogFormatErrorFB
        | LogIncomplete          -> RaftErrorTypeFB.LogIncompleteFB
        | NoError                -> RaftErrorTypeFB.NoErrorFB
        | NoNode                 -> RaftErrorTypeFB.NoNodeFB
        | NotCandidate           -> RaftErrorTypeFB.NotCandidateFB
        | NotLeader              -> RaftErrorTypeFB.NotLeaderFB
        | NotVotingState         -> RaftErrorTypeFB.NotVotingStateFB
        | ResponseTimeout        -> RaftErrorTypeFB.ResponseTimeoutFB
        | SnapshotFormatError    -> RaftErrorTypeFB.SnapshotFormatErrorFB
        | StaleResponse          -> RaftErrorTypeFB.StaleResponseFB
        | UnexpectedVotingChange -> RaftErrorTypeFB.UnexpectedVotingChangeFB
        | VoteTermMismatch       -> RaftErrorTypeFB.VoteTermMismatchFB
        | OtherError           _ -> RaftErrorTypeFB.OtherErrorFB

      match error with
      | OtherError msg ->
        let message = builder.CreateString msg
        RaftErrorFB.CreateRaftErrorFB(builder, tipe, message)
      | _ ->
        RaftErrorFB.CreateRaftErrorFB(builder, tipe)

    static member FromFB (fb: RaftErrorFB) =
      match fb.Type with
      | RaftErrorTypeFB.AlreadyVotedFB           -> Some AlreadyVoted
      | RaftErrorTypeFB.AppendEntryFailedFB      -> Some AppendEntryFailed
      | RaftErrorTypeFB.CandidateUnknownFB       -> Some CandidateUnknown
      | RaftErrorTypeFB.EntryInvalidatedFB       -> Some EntryInvalidated
      | RaftErrorTypeFB.InvalidCurrentIndexFB    -> Some InvalidCurrentIndex
      | RaftErrorTypeFB.InvalidLastLogFB         -> Some InvalidLastLog
      | RaftErrorTypeFB.InvalidLastLogTermFB     -> Some InvalidLastLogTerm
      | RaftErrorTypeFB.InvalidTermFB            -> Some InvalidTerm
      | RaftErrorTypeFB.LogFormatErrorFB         -> Some LogFormatError
      | RaftErrorTypeFB.LogIncompleteFB          -> Some LogIncomplete
      | RaftErrorTypeFB.NoErrorFB                -> Some NoError
      | RaftErrorTypeFB.NoNodeFB                 -> Some NoNode
      | RaftErrorTypeFB.NotCandidateFB           -> Some NotCandidate
      | RaftErrorTypeFB.NotLeaderFB              -> Some NotLeader
      | RaftErrorTypeFB.NotVotingStateFB         -> Some NotVotingState
      | RaftErrorTypeFB.ResponseTimeoutFB        -> Some ResponseTimeout
      | RaftErrorTypeFB.SnapshotFormatErrorFB    -> Some SnapshotFormatError
      | RaftErrorTypeFB.StaleResponseFB          -> Some StaleResponse
      | RaftErrorTypeFB.UnexpectedVotingChangeFB -> Some UnexpectedVotingChange
      | RaftErrorTypeFB.VoteTermMismatchFB       -> Some VoteTermMismatch
      | RaftErrorTypeFB.OtherErrorFB             -> Some (OtherError fb.Message)
      | _                                        -> None

/// The Raft state machine
///
/// ## States
///  - `None`     - hm
///  - `Follower` - this Node is currently following a different Leader
///  - `Candiate` - this Node currently seeks to become Leader
///  - `Leader`   - this Node currently is Leader of the cluster
type RaftState =
  | Follower
  | Candidate
  | Leader

  override self.ToString() =
    sprintf "%A" self

  static member Parse str =
    match str with
      | "Follower"  -> Follower
      | "Candidate" -> Candidate
      | "Leader"    -> Leader
      | _           -> failwithf "unable to parse %A as RaftState" str

/// Response to an AppendEntry request
///
/// ## Constructor:
///  - `Id`    - the generated unique identified for the entry
///  - `Term`  - the entry's term
///  - `Index` - the entry's index in the log
type EntryResponse =
  {  Id    : Id
  ;  Term  : Term
  ;  Index : Index }

  with
    override self.ToString() =
      sprintf "Entry added with Id: %A in term: %d at log index: %d"
        (string self.Id)
        self.Term
        self.Index

[<RequireQualifiedAccess>]
module Entry =
  let inline id    (er : EntryResponse) = er.Id
  let inline term  (er : EntryResponse) = er.Term
  let inline index (er : EntryResponse) = er.Index

// __     __    _       ____                            _
// \ \   / /__ | |_ ___|  _ \ ___  __ _ _   _  ___  ___| |_
//  \ \ / / _ \| __/ _ \ |_) / _ \/ _` | | | |/ _ \/ __| __|
//   \ V / (_) | ||  __/  _ <  __/ (_| | |_| |  __/\__ \ |_
//    \_/ \___/ \__\___|_| \_\___|\__, |\__,_|\___||___/\__|
//                                   |_|

/// Request to Vote for a new Leader
///
/// ## Vote:
///  - `Term`         -  the current term, to force any other leader/candidate to step down
///  - `Candidate`    -  the unique node id of candidate for leadership
///  - `LastLogIndex` -  the index of the candidates last log entry
///  - `LastLogTerm`  -  the index of the candidates last log entry
type VoteRequest =
  { Term         : Term
  ; Candidate    : RaftNode
  ; LastLogIndex : Index
  ; LastLogTerm  : Term
  }

  with
    member self.ToOffset(builder: FlatBufferBuilder) =
      let node = self.Candidate.ToOffset(builder)
      VoteRequestFB.StartVoteRequestFB(builder)
      VoteRequestFB.AddTerm(builder, self.Term)
      VoteRequestFB.AddLastLogTerm(builder, self.LastLogTerm)
      VoteRequestFB.AddLastLogIndex(builder, self.LastLogIndex)
      VoteRequestFB.AddCandidate(builder, node)
      VoteRequestFB.EndVoteRequestFB(builder)

    static member FromFB (fb: VoteRequestFB) : VoteRequest option =
      let candidate = fb.Candidate
      if candidate.HasValue then
        RaftNode.FromFB candidate.Value
        |> Option.map
          (fun node ->
            { Term         = fb.Term
            ; Candidate    = node
            ; LastLogIndex = fb.LastLogIndex
            ; LastLogTerm  = fb.LastLogTerm })
      else None

// __     __    _       ____
// \ \   / /__ | |_ ___|  _ \ ___  ___ _ __   ___  _ __  ___  ___
//  \ \ / / _ \| __/ _ \ |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//   \ V / (_) | ||  __/  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
//    \_/ \___/ \__\___|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//                                    |_|

/// Result of a vote
///
/// ## Result:
///  - `Term`    - current term for candidate to apply
///  - `Granted` - result of vote
type VoteResponse =
  { Term    : Term
  ; Granted : bool
  ; Reason  : RaftError option
  }

  with
    static member FromFB (fb: VoteResponseFB) : VoteResponse =
      let reason =
        let reason = fb.Reason
        if reason.HasValue then
          RaftError.FromFB reason.Value
        else None

      { Term    = fb.Term
      ; Granted = fb.Granted
      ; Reason  = reason }

    member self.ToOffset(builder: FlatBufferBuilder) =
      let err = Option.map (fun (r: RaftError) -> r.ToOffset(builder)) self.Reason
      VoteResponseFB.StartVoteResponseFB(builder)
      VoteResponseFB.AddTerm(builder, self.Term)
      match err with
        | Some offset -> VoteResponseFB.AddReason(builder, offset)
        | _ -> ()
      VoteResponseFB.AddGranted(builder, self.Granted)
      VoteResponseFB.EndVoteResponseFB(builder)


[<RequireQualifiedAccess>]
module Vote =
  // requests
  let inline term         (vote : VoteRequest) = vote.Term
  let inline candidate    (vote : VoteRequest) = vote.Candidate
  let inline lastLogIndex (vote : VoteRequest) = vote.LastLogIndex
  let inline lastLogTerm  (vote : VoteRequest) = vote.LastLogTerm

  // responses
  let inline granted  (vote : VoteResponse) = vote.Granted
  let inline declined (vote : VoteResponse) = not vote.Granted



//     _                               _ _____       _        _
//    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
//   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
//  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
// /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
//         |_|   |_|

/// AppendEntries message.
///
/// This message is used to tell nodes if it's safe to apply entries to the
/// FSM. Can be sent without any entries as a keep alive message.  This
/// message could force a leader/candidate to become a follower.
///
/// ## Message:
///  - `Term`        - currentTerm, to force other leader/candidate to step down
///  - `PrevLogIdx`  - the index of the log just before the newest entry for the node who receive this message
///  - `PrevLogTerm` - the term of the log just before the newest entry for the node who receives this message
///  - `LeaderCommit`- the index of the entry that has been appended to the majority of the cluster. Entries up to this index will be applied to the FSM
type AppendEntries =
  { Term         : Term
  ; PrevLogIdx   : Index
  ; PrevLogTerm  : Term
  ; LeaderCommit : Index
  ; Entries      : LogEntry option
  }

  with
    static member FromFB (fb: AppendEntriesFB) : AppendEntries option =
      let entries =
        if fb.EntriesLength = 0
        then None
        else
          let raw = Array.zeroCreate fb.EntriesLength
          for i in 0 .. (fb.EntriesLength - 1) do
            let entry = fb.Entries(i)
            if entry.HasValue then
              raw.[i] <- entry.Value
          LogEntry.FromFB raw

      try
        { Term         = fb.Term
        ; PrevLogIdx   = fb.PrevLogIdx
        ; PrevLogTerm  = fb.PrevLogTerm
        ; LeaderCommit = fb.LeaderCommit
        ; Entries      = entries
        }
        |> Some
      with
        | _ -> None

    member self.ToOffset(builder: FlatBufferBuilder) =
      let entries =
        Option.map
          (fun (entries: LogEntry) ->
            let offsets = entries.ToOffset(builder)
            AppendEntriesFB.CreateEntriesVector(builder, offsets))
          self.Entries

      AppendEntriesFB.StartAppendEntriesFB(builder)
      AppendEntriesFB.AddTerm(builder, self.Term)
      AppendEntriesFB.AddPrevLogTerm(builder, self.PrevLogTerm)
      AppendEntriesFB.AddPrevLogIdx(builder, self.PrevLogIdx)
      AppendEntriesFB.AddLeaderCommit(builder, self.LeaderCommit)

      Option.map (fun offset -> AppendEntriesFB.AddEntries(builder, offset)) entries
      |> ignore

      AppendEntriesFB.EndAppendEntriesFB(builder)

//     _                               _ ____
//    / \   _ __  _ __   ___ _ __   __| |  _ \ ___  ___ _ __   ___  _ __  ___  ___
//   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
//  / ___ \| |_) | |_) |  __/ | | | (_| |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//         |_|   |_|                                   |_|

/// Appendentries response message.
///
/// an be sent without any entries as a keep alive message.
/// his message could force a leader/candidate to become a follower.
///
/// ## Response Message:
///  - `Term`       - currentTerm, to force other leader/candidate to step down
///  - `Success`    - true if follower contained entry matching prevLogidx and prevLogTerm
///  - `CurrentIdx` - This is the highest log IDX we've received and appended to our log
///  - `FirstIdx`   - The first idx that we received within the appendentries message
type AppendResponse =
  { Term         : Term
  ; Success      : bool
  ; CurrentIndex : Index
  ; FirstIndex   : Index
  }

  with
    static member FromFB (fb: AppendResponseFB) : AppendResponse option =
      try
        { Term         = fb.Term
        ; Success      = fb.Success
        ; CurrentIndex = fb.CurrentIndex
        ; FirstIndex   = fb.FirstIndex
        }
        |> Some
      with
        | _ -> None

    member self.ToOffset(builder: FlatBufferBuilder) =
      AppendResponseFB.StartAppendResponseFB(builder)
      AppendResponseFB.AddTerm(builder, self.Term)
      AppendResponseFB.AddSuccess(builder, self.Success)
      AppendResponseFB.AddFirstIndex(builder, self.FirstIndex)
      AppendResponseFB.AddCurrentIndex(builder, self.CurrentIndex)
      AppendResponseFB.EndAppendResponseFB(builder)

[<RequireQualifiedAccess>]
module AppendRequest =
  let inline term ar = ar.Term
  let inline succeeded ar = ar.Success
  let inline failed ar = not ar.Success
  let inline firstIndex ar = ar.FirstIndex
  let inline currentIndex ar = ar.CurrentIndex

  let inline numEntries ar =
    match ar.Entries with
      | Some entries -> LogEntry.depth entries
      | _            -> 0u

  let inline prevLogIndex ae = ae.PrevLogIdx
  let inline prevLogTerm ae = ae.PrevLogTerm

//  ___           _        _ _ ____                        _           _
// |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
//  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
//  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
// |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
//                                              |_|

type InstallSnapshot =
  { Term      : Term
  ; LeaderId  : NodeId
  ; LastIndex : Index
  ; LastTerm  : Term
  ; Data      : LogEntry }

  with
    member self.ToOffset (builder: FlatBufferBuilder) =
      let data = InstallSnapshotFB.CreateDataVector(builder, self.Data.ToOffset(builder))
      let leaderid = string self.LeaderId |> builder.CreateString

      InstallSnapshotFB.StartInstallSnapshotFB(builder)
      InstallSnapshotFB.AddTerm(builder, self.Term)
      InstallSnapshotFB.AddLeaderId(builder, leaderid)
      InstallSnapshotFB.AddLastTerm(builder, self.LastTerm)
      InstallSnapshotFB.AddLastIndex(builder, self.LastIndex)
      InstallSnapshotFB.AddData(builder, data)
      InstallSnapshotFB.EndInstallSnapshotFB(builder)

    static member FromFB (fb: InstallSnapshotFB) =
      let decoded =
        if fb.DataLength > 0 then
          let raw = Array.zeroCreate fb.DataLength
          for i in 0 .. (fb.DataLength - 1) do
            let data = fb.Data(i)
            if data.HasValue then
              raw.[i] <- data.Value
          LogEntry.FromFB raw
        else None

      decoded
      |> Option.map
        (fun entries ->
          { Term      = fb.Term
          ; LeaderId  = Id fb.LeaderId
          ; LastIndex = fb.LastIndex
          ; LastTerm  = fb.LastTerm
          ; Data      = entries })

/////////////////////////////////////////////////
//   ____      _ _ _                _          //
//  / ___|__ _| | | |__   __ _  ___| | __      //
// | |   / _` | | | '_ \ / _` |/ __| |/ /      //
// | |__| (_| | | | |_) | (_| | (__|   <       //
//  \____\__,_|_|_|_.__/ \__,_|\___|_|\_\      //
//                                             //
//  ___       _             __                 //
// |_ _|_ __ | |_ ___ _ __ / _| __ _  ___ ___  //
//  | || '_ \| __/ _ \ '__| |_ / _` |/ __/ _ \ //
//  | || | | | ||  __/ |  |  _| (_| | (_|  __/ //
// |___|_| |_|\__\___|_|  |_|  \__,_|\___\___| //
/////////////////////////////////////////////////

type IRaftCallbacks =

  /// Request a vote from given Raft server
  abstract member SendRequestVote:     RaftNode  -> VoteRequest            -> VoteResponse option

  /// Send AppendEntries message to given server
  abstract member SendAppendEntries:   RaftNode  -> AppendEntries          -> AppendResponse option

  /// Send InstallSnapshot command to given serve
  abstract member SendInstallSnapshot: RaftNode  -> InstallSnapshot        -> AppendResponse option

  /// given the current state of Raft, prepare and return a snapshot value of
  /// current application state
  abstract member PrepareSnapshot:     Raft            -> Log option

  /// perist the given Snapshot value to disk. For safety reasons this MUST
  /// flush all changes to disk.
  abstract member PersistSnapshot:     LogEntry        -> unit

  /// attempt to load a snapshot from disk. return None if no snapshot was found
  abstract member RetrieveSnapshot:    unit            -> LogEntry option

  /// apply the given command to state machine
  abstract member ApplyLog:            StateMachine    -> unit

  /// a new server was added to the configuration
  abstract member NodeAdded:           RaftNode        -> unit

  /// a new server was added to the configuration
  abstract member NodeUpdated:         RaftNode        -> unit

  /// a server was removed from the configuration
  abstract member NodeRemoved:         RaftNode        -> unit

  /// a cluster configuration transition was successfully applied
  abstract member Configured:          RaftNode array  -> unit

  /// the state of Raft itself has changed from old state to new given state
  abstract member StateChanged:        RaftState       -> RaftState              -> unit

  /// persist vote data to disk. For safety reasons this callback MUST flush
  /// the change to disk.
  abstract member PersistVote:         RaftNode option -> unit

  /// persist term data to disk. For safety reasons this callback MUST flush
  /// the change to disk>
  abstract member PersistTerm:         Term            -> unit

  /// persist an entry added to the log to disk. For safety reasons this
  /// callback MUST flush the change to disk.
  abstract member PersistLog:          LogEntry        -> unit

  /// persist the removal of the passed entry from the log to disk. For safety
  /// reasons this callback MUST flush the change to disk.
  abstract member DeleteLog:           LogEntry        -> unit

  /// Callback for catching debug messsages
  abstract member LogMsg:  LogLevel ->  RaftNode        -> String                 -> unit

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//  ____        __ _                                                                                                                     //
// |  _ \ __ _ / _| |_                                                                                                                   //
// | |_) / _` | |_| __|                                                                                                                  //
// |  _ < (_| |  _| |_                                                                                                                   //
// |_| \_\__,_|_|  \__|                                                                                                                  //
//                                                                                                                                       //
// ## Raft Server:                                                                                                                       //
//  - `Node`                  - the server's own node information                                                                        //
//  - `RaftState`             - follower/leader/candidate indicator                                                                  //
//  - `CurrentTerm`           - the server's best guess of what the current term is starts at zero                                       //
//  - `CurrentLeader`         - what this node thinks is the node ID of the current leader, or -1 if there isn't a known current leader. //
//  - `Peers`                 - list of all known nodes                                                                                  //
//  - `NumNodes`              - number of currently known peers                                                                          //
//  - `VotedFor`              - the candidate the server voted for in its current term or None if it hasn't voted for any yet            //
//  - `Log`                   - the log which is replicated                                                                              //
//  - `CommitIdx`             - idx of highest log entry known to be committed                                                           //
//  - `LastAppliedIdx`        - idx of highest log entry applied to state machine                                                        //
//  - `TimoutElapsed`         - amount of time left till timeout                                                                         //
//  - `ElectionTimeout`       - amount of time left till we start a new election                                                         //
//  - `RequestTimeout`        - amount of time left till we consider request to be failed                                                //
//  - `Callbacks`             - all callbacks to be invoked                                                                              //
//  - `MaxLogDepth`           - maximum log depth to reach before snapshotting triggers
//  - `VotingCfgChangeLogIdx` - the log which has a voting cfg change, otherwise None                                                    //
//                                                                                                                                       //
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

and Raft =
  { Node              : RaftNode
  ; State             : RaftState
  ; CurrentTerm       : Term
  ; CurrentLeader     : NodeId option
  ; Peers             : Map<NodeId,RaftNode>
  ; OldPeers          : Map<NodeId,RaftNode> option
  ; NumNodes          : Long
  ; VotedFor          : NodeId option
  ; Log               : Log
  ; CommitIndex       : Index
  ; LastAppliedIdx    : Index
  ; TimeoutElapsed    : Long
  ; ElectionTimeout   : Long
  ; RequestTimeout    : Long
  ; MaxLogDepth       : Long
  ; ConfigChangeEntry : LogEntry option
  }

  override self.ToString() =
    sprintf "Node              = %s
State             = %A
CurrentTerm       = %A
CurrentLeader     = %A
NumNodes          = %A
VotedFor          = %A
MaxLogDepth       = %A
CommitIndex       = %A
LastAppliedIdx    = %A
TimeoutElapsed    = %A
ElectionTimeout   = %A
RequestTimeout    = %A
ConfigChangeEntry = %s
"
      (self.Node.ToString())
      self.State
      self.CurrentTerm
      self.CurrentLeader
      self.NumNodes
      self.VotedFor
      self.MaxLogDepth
      self.CommitIndex
      self.LastAppliedIdx
      self.TimeoutElapsed
      self.ElectionTimeout
      self.RequestTimeout
      (if Option.isSome self.ConfigChangeEntry then
        Option.get self.ConfigChangeEntry |> string
       else "<empty>")

  member self.IsLeader
    with get () =
      match self.CurrentLeader with
      | Some lid -> self.Node.Id = lid
      | _ -> false

////////////////////////////////////////
//  __  __                       _    //
// |  \/  | ___  _ __   __ _  __| |   //
// | |\/| |/ _ \| '_ \ / _` |/ _` |   //
// | |  | | (_) | | | | (_| | (_| |   //
// |_|  |_|\___/|_| |_|\__,_|\__,_|   //
////////////////////////////////////////

[<NoComparison;NoEquality>]
type RaftMonad<'Env,'State,'T,'Error> =
  MkRM of ('Env -> 'State -> Either<'Error * 'State,'T * 'State>)

type RaftM<'t,'err> =
  RaftMonad<IRaftCallbacks,Raft,'t,'err>
