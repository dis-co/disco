namespace Pallet.Core

open System
open System.Net

///  _____ _ _   _
/// | ____(_) |_| |__   ___ _ __
/// |  _| | | __| '_ \ / _ \ '__|
/// | |___| | |_| | | |  __/ |
/// |_____|_|\__|_| |_|\___|_|
///
type Either<'l,'m,'r> =
  | Left   of 'l                        // Encodes errors
  | Middle of 'm                        // Return result immediately
  | Right  of 'r                        // Return result and keep computation running

////////////////////////////////////////
//  _____                             //
// | ____|_ __ _ __ ___  _ __         //
// |  _| | '__| '__/ _ \| '__|        //
// | |___| |  | | | (_) | |           //
// |_____|_|  |_|  \___/|_|           //
////////////////////////////////////////

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
  ;  Index : Index
  }

[<RequireQualifiedAccess>]
module Entry =
  let inline id    (er : EntryResponse) = er.Id
  let inline term  (er : EntryResponse) = er.Term
  let inline index (er : EntryResponse) = er.Index

/// Request to Vote for a new Leader
///
/// ## Vote:
///  - `Term`         -  the current term, to force any other leader/candidate to step down
///  - `Candidate`    -  the unique node id of candidate for leadership
///  - `LastLogIndex` -  the index of the candidates last log entry
///  - `LastLogTerm`  -  the index of the candidates last log entry
type VoteRequest<'n> =
  { Term         : Term
  ; Candidate    : Node<'n>
  ; LastLogIndex : Index
  ; LastLogTerm  : Term
  }

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

[<RequireQualifiedAccess>]
module Vote =
  // requests
  let inline term         (vote : VoteRequest<_>) = vote.Term
  let inline candidate    (vote : VoteRequest<_>) = vote.Candidate
  let inline lastLogIndex (vote : VoteRequest<_>) = vote.LastLogIndex
  let inline lastLogTerm  (vote : VoteRequest<_>) = vote.LastLogTerm

  // responses
  let inline granted  (vote : VoteResponse) = vote.Granted
  let inline declined (vote : VoteResponse) = not vote.Granted

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
type AppendEntries<'a,'n> =
  { Term         : Term
  ; PrevLogIdx   : Index
  ; PrevLogTerm  : Term
  ; LeaderCommit : Index
  ; Entries      : LogEntry<'a,'n> option
  }

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

[<RequireQualifiedAccess>]
module AppendRequest =
  let inline term ar = ar.Term
  let inline succeeded ar = ar.Success
  let inline failed ar = not ar.Success
  let inline firstIndex ar = ar.FirstIndex
  let inline currentIndex ar = ar.CurrentIndex

  let inline numEntries ar =
    match ar.Entries with
      | Some entries -> Log.depth entries
      | _ -> 0UL

  let inline prevLogIndex ae = ae.PrevLogIdx
  let inline prevLogTerm ae = ae.PrevLogTerm

//////////////////////////////////////////////////////////////////////////////
//  ___           _        _ _ ____                        _           _    //
// |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_  //
//  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __| //
//  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_  //
// |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__| //
//                                              |_|                         //
//////////////////////////////////////////////////////////////////////////////

type InstallSnapshot<'node,'data> =
  { Term      : Term
  ; LeaderId  : NodeId
  ; LastIndex : Index
  ; LastTerm  : Term
  ; Data      : LogEntry<'node,'data>
  }

type SnapshotResponse = { Term : Term }

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

type IRaftCallbacks<'a,'b> =

  /// Request a vote from given Raft server
  abstract member SendRequestVote:     Node<'b>        -> VoteRequest<'b>        -> unit

  /// Send AppendEntries message to given server
  abstract member SendAppendEntries:   Node<'b>        -> AppendEntries<'a,'b>   -> unit

  /// Send InstallSnapshot command to given server
  abstract member SendInstallSnapshot: Node<'b>        -> InstallSnapshot<'a,'b> -> unit

  /// given the current state of Raft, prepare and return a snapshot value of
  /// current application state
  abstract member PrepareSnapshot:     Raft<'a,'b>     -> Log<'a,'b>

  /// perist the given Snapshot value to disk. For safety reasons this MUST
  /// flush all changes to disk.
  abstract member PersistSnapshot:     LogEntry<'a,'b> -> unit

  /// attempt to load a snapshot from disk. return None if no snapshot was found
  abstract member RetrieveSnapshot:    unit            -> LogEntry<'a,'b> option

  /// apply the given command to state machine
  abstract member ApplyLog:            'a              -> unit

  /// a new server was added to the configuration
  abstract member NodeAdded:           Node<'b>        -> unit

  /// a new server was added to the configuration
  abstract member NodeUpdated:         Node<'b>        -> unit

  /// a server was removed from the configuration
  abstract member NodeRemoved:         Node<'b>        -> unit

  /// a cluster configuration transition was successfully applied
  abstract member Configured:          Node<'b> array  -> unit

  /// the state of Raft itself has changed from old state to new given state
  abstract member StateChanged:        RaftState       -> RaftState              -> unit

  /// persist vote data to disk. For safety reasons this callback MUST flush
  /// the change to disk.
  abstract member PersistVote:         Node<'b> option -> unit

  /// persist term data to disk. For safety reasons this callback MUST flush
  /// the change to disk>
  abstract member PersistTerm:         Node<'b>        -> unit

  /// persist an entry added to the log to disk. For safety reasons this
  /// callback MUST flush the change to disk.
  abstract member PersistLog:          LogEntry<'a,'b> -> unit

  /// persist the removal of the passed entry from the log to disk. For safety
  /// reasons this callback MUST flush the change to disk.
  abstract member DeleteLog:           LogEntry<'a,'b> -> unit

  /// Callback for detecting when a non-voting node has sufficient logs
  abstract member HasSufficientLogs:   Node<'b>        -> unit

  /// Callback for catching debug messsages
  abstract member LogMsg:              Node<'b>        -> String                 -> unit

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

and Raft<'d,'n> =
  { Node              : Node<'n>
  ; State             : RaftState
  ; CurrentTerm       : Term
  ; CurrentLeader     : NodeId option
  ; Peers             : Map<NodeId,Node<'n>>
  ; OldPeers          : Map<NodeId,Node<'n>> option
  ; NumNodes          : Long
  ; VotedFor          : NodeId option
  ; Log               : Log<'d,'n>
  ; CommitIndex       : Index
  ; LastAppliedIdx    : Index
  ; TimeoutElapsed    : Long
  ; ElectionTimeout   : Long
  ; RequestTimeout    : Long
  ; MaxLogDepth       : Long
  ; ConfigChangeEntry : LogEntry<'d,'n> option
  }
  override self.ToString() =
    sprintf "
Node              = %s (%s)
State             = %A
CurrentTerm       = %A
CurrentLeader     = %A
Peers             = %s
NumNodes          = %s
VotedFor          = %A
Log               = %s
MaxLogDepth       = %A
CommitIndex       = %A
LastAppliedIdx    = %A
TimeoutElapsed    = %A
ElectionTimeout   = %A
RequestTimeout    = %A
ConfigChangeEntry = %A
"
      (self.Node.ToString()) (self.Node.Data.ToString())
      self.State
      self.CurrentTerm
      self.CurrentLeader
      (Map.fold (fun m _ peer -> sprintf "%s\n    %s" m (peer.ToString())) "" self.Peers)
      (self.NumNodes.ToString())
      self.VotedFor
      (Log.foldLogL (fun m log -> sprintf "%s\n    %s" m (log.ToString())) "" self.Log)
      self.MaxLogDepth
      self.CommitIndex
      self.LastAppliedIdx
      self.TimeoutElapsed
      self.ElectionTimeout
      self.RequestTimeout
      self.ConfigChangeEntry

////////////////////////////////////////////////
//   ____      _ _ _                _         //
//  / ___|__ _| | | |__   __ _  ___| | _____  //
// | |   / _` | | | '_ \ / _` |/ __| |/ / __| //
// | |__| (_| | | | |_) | (_| | (__|   <\__ \ //
//  \____\__,_|_|_|_.__/ \__,_|\___|_|\_\___/ //
////////////////////////////////////////////////

type Logger<'n> = Node<'n> -> string -> unit

type ApplyLog<'d,'n> = LogEntry<'d,'n> -> unit

type LogOffer<'d,'n> = LogEntry<'d,'n> -> Index -> unit

type PersistVote<'n> = Node<'n> option -> unit

////////////////////////////////////////
//  __  __                       _    //
// |  \/  | ___  _ __   __ _  __| |   //
// | |\/| |/ _ \| '_ \ / _` |/ _` |   //
// | |  | | (_) | | | | (_| | (_| |   //
// |_|  |_|\___/|_| |_|\__,_|\__,_|   //
////////////////////////////////////////

[<NoComparison;NoEquality>]
type RaftMonad<'Env,'State,'T,'M,'Error> =
  MkRM of ('Env -> 'State -> Either<'Error * 'State,'M * 'State,'T * 'State>)

type RaftM<'d,'n,'t,'m,'e> =
  RaftMonad<IRaftCallbacks<'d,'n>,Raft<'d,'n>,'t,'m,'e>
