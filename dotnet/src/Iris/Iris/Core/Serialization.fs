namespace Iris.Core

//  ____            _       _ _          _   _
// / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
// \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
//  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
// |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

[<AutoOpen>]
module Serialization =

  let inline encode (value : ^t when ^t : (member ToBytes : unit -> byte array)) =
    (^t : (member ToBytes : unit -> byte array) value)


//  _____      _                 _
// | ____|_  _| |_ ___ _ __  ___(_) ___  _ __  ___
// |  _| \ \/ / __/ _ \ '_ \/ __| |/ _ \| '_ \/ __|
// | |___ >  <| ||  __/ | | \__ \ | (_) | | | \__ \
// |_____/_/\_\\__\___|_| |_|___/_|\___/|_| |_|___/

open System.Runtime.CompilerServices
open Iris.Serialization.Raft
open Pallet.Core
open FlatBuffers

//  _   _           _      ____  _        _
// | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
// |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
// | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
// |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

[<AutoOpen>]
module NodeStateExtensions =

  type NodeState with
    member self.ToOffset () =
      match self with
        | Running -> NodeStateFB.RunningFB
        | Joining -> NodeStateFB.JoiningFB
        | Failed  -> NodeStateFB.FailedFB

    static member FromFB (fb: NodeStateFB) =
      match fb with
        | NodeStateFB.JoiningFB -> Joining
        | NodeStateFB.RunningFB -> Running
        | NodeStateFB.FailedFB  -> Failed
        | _                     ->
          failwith "unable to de-serialize garbage NodeState case"

//  _   _           _
// | \ | | ___   __| | ___
// |  \| |/ _ \ / _` |/ _ \
// | |\  | (_) | (_| |  __/
// |_| \_|\___/ \__,_|\___|

[<Extension>]
type NodeExtensions() =

  [<Extension>]
  static member inline ToOffset (node: Node, builder: FlatBufferBuilder) =
    let id = string node.Id |> builder.CreateString
    let info = node.Data.ToOffset(builder)
    let state = node.State.ToOffset()
    NodeFB.StartNodeFB(builder)
    NodeFB.AddId(builder, id)
    NodeFB.AddVoting(builder, node.Voting)
    NodeFB.AddVotedForMe(builder, node.VotedForMe)
    NodeFB.AddState(builder, state)
    NodeFB.AddNextIndex(builder, node.NextIndex)
    NodeFB.AddMatchIndex(builder, node.MatchIndex)
    NodeFB.AddData(builder, info)
    NodeFB.EndNodeFB(builder)

[<AutoOpen>]
module StaticNodeExtensions =

  type Node<'t> with
    static member FromFB (fb: NodeFB) : Node =
      let info = fb.Data |> IrisNode.FromFB
      { Id = RaftId fb.Id
      ; State = fb.State |> NodeState.FromFB
      ; Data = info
      ; Voting = fb.Voting
      ; VotedForMe = fb.VotedForMe
      ; NextIndex = fb.NextIndex
      ; MatchIndex = fb.MatchIndex
      }



//   ____             __ _        ____ _
//  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
// | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
// | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
//  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
//                         |___/                         |___/

[<Extension>]
type ConfigChangeExtensions() =

  [<Extension>]
  static member inline ToOffset (change: ConfigChange, builder: FlatBufferBuilder) =
    match change with
      | NodeAdded node ->
        let node = node.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.NodeAdded)
        ConfigChangeFB.AddNode(builder, node)
        ConfigChangeFB.EndConfigChangeFB(builder)
      | NodeRemoved node ->
        let node = node.ToOffset(builder)
        ConfigChangeFB.StartConfigChangeFB(builder)
        ConfigChangeFB.AddType(builder, ConfigChangeTypeFB.NodeRemoved)
        ConfigChangeFB.AddNode(builder, node)
        ConfigChangeFB.EndConfigChangeFB(builder)

[<AutoOpen>]
module StaticConfigChangeExtensions =

  type ConfigChange<'t> with
    static member FromFB (fb: ConfigChangeFB) : ConfigChange =
      let node = fb.Node |> Node.FromFB
      match fb.Type with
        | ConfigChangeTypeFB.NodeAdded   -> NodeAdded   node
        | ConfigChangeTypeFB.NodeRemoved -> NodeRemoved node
        | _                              ->
          failwith "unable to de-serialie garbage ConfigChange"

//  _
// | |    ___   __ _
// | |   / _ \ / _` |
// | |__| (_) | (_| |
// |_____\___/ \__, |
//             |___/

[<Extension>]
type LogExentions() =

  [<Extension>]
  static member inline ToOffset (entries: LogEntry, builder: FlatBufferBuilder) =
    let buildLogFB tipe value =
      LogFB.StartLogFB(builder)
      LogFB.AddEntryType(builder, tipe)
      LogFB.AddEntry(builder, value)
      LogFB.EndLogFB(builder)

    let toOffset (log: LogEntry) =
      match log with
      //   ____             __ _                       _   _
      //  / ___|___  _ __  / _(_) __ _ _   _ _ __ __ _| |_(_) ___  _ __
      // | |   / _ \| '_ \| |_| |/ _` | | | | '__/ _` | __| |/ _ \| '_ \
      // | |__| (_) | | | |  _| | (_| | |_| | | | (_| | |_| | (_) | | | |
      //  \____\___/|_| |_|_| |_|\__, |\__,_|_|  \__,_|\__|_|\___/|_| |_|
      //                         |___/
      | Configuration(id,index,term,nodes,_)          ->
        let id = string id |> builder.CreateString
        let nodes = Array.map (fun (node: Node) -> node.ToOffset(builder)) nodes
        let nvec = ConfigurationFB.CreateNodesVector(builder, nodes)

        ConfigurationFB.StartConfigurationFB(builder)
        ConfigurationFB.AddId(builder, id)
        ConfigurationFB.AddIndex(builder, uint64 index)
        ConfigurationFB.AddTerm(builder, uint64 term)
        ConfigurationFB.AddNodes(builder, nvec)

        let entry = ConfigurationFB.EndConfigurationFB(builder)

        buildLogFB LogTypeFB.ConfigurationFB entry.Value

      //      _       _       _    ____
      //     | | ___ (_)_ __ | |_ / ___|___  _ __  ___  ___ _ __  ___ _   _ ___
      //  _  | |/ _ \| | '_ \| __| |   / _ \| '_ \/ __|/ _ \ '_ \/ __| | | / __|
      // | |_| | (_) | | | | | |_| |__| (_) | | | \__ \  __/ | | \__ \ |_| \__ \
      //  \___/ \___/|_|_| |_|\__|\____\___/|_| |_|___/\___|_| |_|___/\__,_|___/
      | JointConsensus(id,index,term,changes,nodes,_) ->
        let id = string id |> builder.CreateString
        let changes = Array.map (fun (change: ConfigChange) -> change.ToOffset(builder)) changes
        let chvec = JointConsensusFB.CreateChangesVector(builder, changes)
        let nodes = Array.map (fun (node: Node) -> node.ToOffset(builder)) nodes
        let nvec = JointConsensusFB.CreateNodesVector(builder, nodes)

        JointConsensusFB.StartJointConsensusFB(builder)
        JointConsensusFB.AddId(builder, id)
        JointConsensusFB.AddIndex(builder, uint64 index)
        JointConsensusFB.AddTerm(builder, uint64 term)
        JointConsensusFB.AddChanges(builder, chvec)
        JointConsensusFB.AddNodes(builder, nvec)

        let entry = JointConsensusFB.EndJointConsensusFB(builder)

        buildLogFB LogTypeFB.JointConsensusFB entry.Value

      //  _                _____       _
      // | |    ___   __ _| ____|_ __ | |_ _ __ _   _
      // | |   / _ \ / _` |  _| | '_ \| __| '__| | | |
      // | |__| (_) | (_| | |___| | | | |_| |  | |_| |
      // |_____\___/ \__, |_____|_| |_|\__|_|   \__, |
      //             |___/                      |___/
      | LogEntry(id,index,term,data,_) ->
        let id = string id |> builder.CreateString
        let data = data.ToOffset(builder)

        LogEntryFB.StartLogEntryFB(builder)
        LogEntryFB.AddId(builder, id)
        LogEntryFB.AddIndex(builder, uint64 index)
        LogEntryFB.AddTerm(builder, uint64 term)
        LogEntryFB.AddData(builder, data)

        let entry = LogEntryFB.EndLogEntryFB(builder)

        buildLogFB LogTypeFB.LogEntryFB entry.Value

      //  ____                        _           _
      // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                   |_|
      | Snapshot(id,index,term,lidx,lterm,nodes,data) ->
        let id = string id |> builder.CreateString
        let nodes = Array.map (fun (node: Node) -> node.ToOffset(builder)) nodes
        let nvec = SnapshotFB.CreateNodesVector(builder, nodes)
        let data = data.ToOffset(builder)

        SnapshotFB.StartSnapshotFB(builder)
        SnapshotFB.AddId(builder, id)
        SnapshotFB.AddIndex(builder, uint64 index)
        SnapshotFB.AddTerm(builder, uint64 term)
        SnapshotFB.AddLastIndex(builder, uint64 lidx)
        SnapshotFB.AddLastTerm(builder, uint64 lterm)
        SnapshotFB.AddNodes(builder, nvec)
        SnapshotFB.AddData(builder, data)

        let entry = SnapshotFB.EndSnapshotFB(builder)

        buildLogFB LogTypeFB.SnapshotFB entry.Value

    let arr = Array.zeroCreate (Log.depth entries |> int)
    Log.iter (fun i (log: LogEntry) -> arr.[int i] <- toOffset log) entries
    arr

[<AutoOpen>]
module StaticLogExtensions =

  /// ## Decode a FlatBuffer into a Log structure
  ///
  /// Decodes a single FlatBuffer encoded log entry into its corresponding Raft LogEntry type and
  /// adds passed-in `LogEntry option` as previous field value. Indicates failure by returning None.
  ///
  /// ### Signature:
  /// - fb: LogFB FlatBuffer object to parse
  /// - log: previous LogEntry value to reconstruct the chain of events
  ///
  /// Returns: LogEntry option
  let private fb2Log (fb: LogFB) (log: LogEntry option) : LogEntry option =
    match fb.EntryType with
      | LogTypeFB.ConfigurationFB ->
        let entry = fb.GetEntry(new ConfigurationFB())
        let nodes = Array.zeroCreate entry.NodesLength

        for i in 0 .. (entry.NodesLength - 1) do
          nodes.[i] <- entry.GetNodes(i) |> Node.FromFB

        Configuration(RaftId entry.Id, entry.Index, entry.Term, nodes, log)
        |> Some

      | LogTypeFB.JointConsensusFB ->
        let entry = fb.GetEntry(new JointConsensusFB())
        let changes = Array.zeroCreate entry.ChangesLength
        let nodes = Array.zeroCreate entry.NodesLength

        for i in 0 .. (entry.NodesLength - 1) do
          nodes.[i] <- entry.GetNodes(i) |> Node.FromFB

        for i in 0 .. (entry.ChangesLength - 1) do
          changes.[i] <- entry.GetChanges(i) |> ConfigChange.FromFB

        JointConsensus(RaftId entry.Id, entry.Index, entry.Term, changes, nodes, log)
        |> Some

      | LogTypeFB.LogEntryFB ->
        let entry = fb.GetEntry(new LogEntryFB())
        let data = StateMachine.FromFB entry.Data

        LogEntry(RaftId entry.Id, entry.Index, entry.Term, data, log)
        |> Some

      | LogTypeFB.SnapshotFB ->
        let entry = fb.GetEntry(new SnapshotFB())
        let data = StateMachine.FromFB entry.Data
        let nodes = Array.zeroCreate entry.NodesLength

        let id = RaftId entry.Id

        for i in 0..(entry.NodesLength - 1) do
          nodes.[i] <- entry.GetNodes(i) |> Node.FromFB

        Snapshot(id, entry.Index, entry.Term, entry.LastIndex, entry.LastTerm, nodes, data)
        |> Some

      | _ ->
        failwith "unable to de-serialize garbage LogTypeFB value"


  type Log<'a,'n> with
    static member FromFB (logs: LogFB array) : LogEntry option =
      Array.foldBack fb2Log logs None


//  ____        __ _   _____
// |  _ \ __ _ / _| |_| ____|_ __ _ __ ___  _ __
// | |_) / _` | |_| __|  _| | '__| '__/ _ \| '__|
// |  _ < (_| |  _| |_| |___| |  | | | (_) | |
// |_| \_\__,_|_|  \__|_____|_|  |_|  \___/|_|

[<AutoOpen>]
module StaticRaftErrorExtensions =

  type RaftError with
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
      | RaftErrorTypeFB.AlreadyVotedFB           -> AlreadyVoted
      | RaftErrorTypeFB.AppendEntryFailedFB      -> AppendEntryFailed
      | RaftErrorTypeFB.CandidateUnknownFB       -> CandidateUnknown
      | RaftErrorTypeFB.EntryInvalidatedFB       -> EntryInvalidated
      | RaftErrorTypeFB.InvalidCurrentIndexFB    -> InvalidCurrentIndex
      | RaftErrorTypeFB.InvalidLastLogFB         -> InvalidLastLog
      | RaftErrorTypeFB.InvalidLastLogTermFB     -> InvalidLastLogTerm
      | RaftErrorTypeFB.InvalidTermFB            -> InvalidTerm
      | RaftErrorTypeFB.LogFormatErrorFB         -> LogFormatError
      | RaftErrorTypeFB.LogIncompleteFB          -> LogIncomplete
      | RaftErrorTypeFB.NoErrorFB                -> NoError
      | RaftErrorTypeFB.NoNodeFB                 -> NoNode
      | RaftErrorTypeFB.NotCandidateFB           -> NotCandidate
      | RaftErrorTypeFB.NotLeaderFB              -> NotLeader
      | RaftErrorTypeFB.NotVotingStateFB         -> NotVotingState
      | RaftErrorTypeFB.ResponseTimeoutFB        -> ResponseTimeout
      | RaftErrorTypeFB.SnapshotFormatErrorFB    -> SnapshotFormatError
      | RaftErrorTypeFB.StaleResponseFB          -> StaleResponse
      | RaftErrorTypeFB.UnexpectedVotingChangeFB -> UnexpectedVotingChange
      | RaftErrorTypeFB.VoteTermMismatchFB       -> VoteTermMismatch
      | RaftErrorTypeFB.OtherErrorFB             -> OtherError(fb.Message)
      | _ -> failwith "could not de-serialize garbage RaftErrorTypeFB"

//----------------------------------------------------------------------------//
// __     __    _   _                                                         //
// \ \   / /__ | |_(_)_ __   __ _                                             //
//  \ \ / / _ \| __| | '_ \ / _` |                                            //
//   \ V / (_) | |_| | | | | (_| |                                            //
//    \_/ \___/ \__|_|_| |_|\__, |                                            //
//                          |___/                                             //
//----------------------------------------------------------------------------//

[<Extension>]
type VotingExentions() =

  //  ____                            _
  // |  _ \ ___  __ _ _   _  ___  ___| |_
  // | |_) / _ \/ _` | | | |/ _ \/ __| __|
  // |  _ <  __/ (_| | |_| |  __/\__ \ |_
  // |_| \_\___|\__, |\__,_|\___||___/\__|
  //               |_|

  [<Extension>]
  static member inline ToOffset (request: VoteRequest<IrisNode>, builder: FlatBufferBuilder) =
    let node = request.Candidate.ToOffset(builder)
    VoteRequestFB.StartVoteRequestFB(builder)
    VoteRequestFB.AddTerm(builder, uint64 request.Term)
    VoteRequestFB.AddLastLogTerm(builder, uint64 request.LastLogTerm)
    VoteRequestFB.AddLastLogIndex(builder, uint64 request.LastLogIndex)
    VoteRequestFB.AddCandidate(builder, node)
    VoteRequestFB.EndVoteRequestFB(builder)

  //  ____
  // |  _ \ ___  ___ _ __   ___  _ __  ___  ___
  // | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  // |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  // |_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                |_|

  [<Extension>]
  static member inline ToOffset (response: VoteResponse, builder: FlatBufferBuilder) =
    let err = Option.map (fun (r: RaftError) -> r.ToOffset(builder)) response.Reason
    VoteResponseFB.StartVoteResponseFB(builder)
    VoteResponseFB.AddTerm(builder, uint64 response.Term)
    match err with
      | Some offset -> VoteResponseFB.AddReason(builder, offset)
      | _ -> ()
    VoteResponseFB.AddGranted(builder, response.Granted)
    VoteResponseFB.EndVoteResponseFB(builder)

[<AutoOpen>]
module StaticVotingExtensions =

  type VoteRequest<'t> with
    static member FromFB (fb: VoteRequestFB) : VoteRequest =
      { Term         = fb.Term
      ; Candidate    = fb.Candidate |> Node.FromFB
      ; LastLogIndex = fb.LastLogIndex
      ; LastLogTerm  = fb.LastLogTerm
      }

  type VoteResponse with
    static member FromFB (fb: VoteResponseFB) : VoteResponse =
      let reason =
        if isNull fb.Reason |> not then
          Some(RaftError.FromFB fb.Reason)
        else None

      { Term    = fb.Term
      ; Granted = fb.Granted
      ; Reason  = reason }

//-----------------------------------------------------------------------------//
//     _                               _ _____       _        _                //
//    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___      //
//   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|     //
//  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \     //
// /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/     //
//         |_|   |_|                                                           //
//-----------------------------------------------------------------------------//

[<Extension>]
type AppendEntriesExentions() =

  //  ____                            _
  // |  _ \ ___  __ _ _   _  ___  ___| |_
  // | |_) / _ \/ _` | | | |/ _ \/ __| __|
  // |  _ <  __/ (_| | |_| |  __/\__ \ |_
  // |_| \_\___|\__, |\__,_|\___||___/\__|
  //               |_|

  [<Extension>]
  static member inline ToOffset (ae: AppendEntries, builder: FlatBufferBuilder) =
    let entries =
      Option.map
        (fun (entries: LogEntry) ->
           let offsets = entries.ToOffset(builder)
           AppendEntriesFB.CreateEntriesVector(builder, offsets))
        ae.Entries

    AppendEntriesFB.StartAppendEntriesFB(builder)
    AppendEntriesFB.AddTerm(builder, uint64 ae.Term)
    AppendEntriesFB.AddPrevLogTerm(builder, uint64 ae.PrevLogTerm)
    AppendEntriesFB.AddPrevLogIdx(builder, uint64 ae.PrevLogIdx)
    AppendEntriesFB.AddLeaderCommit(builder, uint64 ae.LeaderCommit)

    Option.map (fun offset -> AppendEntriesFB.AddEntries(builder, offset)) entries
    |> ignore

    AppendEntriesFB.EndAppendEntriesFB(builder)

  //  ____
  // |  _ \ ___  ___ _ __   ___  _ __  ___  ___
  // | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  // |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  // |_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                |_|

  [<Extension>]
  static member inline ToOffset (ar: AppendResponse, builder: FlatBufferBuilder) =
    AppendResponseFB.StartAppendResponseFB(builder)
    AppendResponseFB.AddTerm(builder, uint64 ar.Term)
    AppendResponseFB.AddSuccess(builder, ar.Success)
    AppendResponseFB.AddFirstIndex(builder, uint64 ar.FirstIndex)
    AppendResponseFB.AddCurrentIndex(builder, uint64 ar.CurrentIndex)
    AppendResponseFB.EndAppendResponseFB(builder)

[<AutoOpen>]
module StaticAppendEntriesExtensions =

  type AppendEntries<'a,'n> with
    static member FromFB (fb: AppendEntriesFB) : AppendEntries =
      let entries =
        if fb.EntriesLength = 0
        then None
        else
          let raw = Array.zeroCreate fb.EntriesLength
          for i in 0 .. (fb.EntriesLength - 1) do
            raw.[i] <- fb.GetEntries(i)
          Log.FromFB raw

      { Term         = fb.Term
      ; PrevLogIdx   = fb.PrevLogIdx
      ; PrevLogTerm  = fb.PrevLogTerm
      ; LeaderCommit = fb.LeaderCommit
      ; Entries      = entries
      }

  type AppendResponse with
    static member FromFB (fb: AppendResponseFB) : AppendResponse =
      { Term         = fb.Term
      ; Success      = fb.Success
      ; CurrentIndex = fb.CurrentIndex
      ; FirstIndex   = fb.FirstIndex
      }

//  ____                        _           _
// / ___| _ __   __ _ _ __  ___| |__   ___ | |_
// \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
//  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
// |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
//                   |_|

[<Extension>]
type InstallSnapshotExtensions() =

  [<Extension>]
  static member inline ToOffset (is: InstallSnapshot, builder: FlatBufferBuilder) =
    let data = InstallSnapshotFB.CreateDataVector(builder, is.Data.ToOffset(builder))
    let leaderid = string is.LeaderId |> builder.CreateString

    InstallSnapshotFB.StartInstallSnapshotFB(builder)
    InstallSnapshotFB.AddTerm(builder, is.Term)
    InstallSnapshotFB.AddLeaderId(builder, leaderid)
    InstallSnapshotFB.AddLastTerm(builder, is.LastTerm)
    InstallSnapshotFB.AddLastIndex(builder, is.LastIndex)
    InstallSnapshotFB.AddData(builder, data)
    InstallSnapshotFB.EndInstallSnapshotFB(builder)

  [<Extension>]
  static member inline ToOffset (ir: SnapshotResponse, builder: FlatBufferBuilder) =
    SnapshotResponseFB.CreateSnapshotResponseFB(builder, uint64 ir.Term)


[<AutoOpen>]
module StaticIntsallSnapshotExtensions =

  type InstallSnapshot<'a,'n> with
    static member FromFB (fb: InstallSnapshotFB) =
      let entries =
        if fb.DataLength > 0 then
          let raw = Array.zeroCreate fb.DataLength
          for i in 0 .. (fb.DataLength - 1) do
            raw.[i] <- fb.GetData(i)
          Log.FromFB raw
        else None

      { Term      = fb.Term
      ; LeaderId  = RaftId fb.LeaderId
      ; LastIndex = fb.LastIndex
      ; LastTerm  = fb.LastTerm
      ; Data      = Option.get entries
      }

  type SnapshotResponse with
    static member FromFB (fb: SnapshotResponseFB) : SnapshotResponse =
      { Term = fb.Term }
