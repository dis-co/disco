namespace Iris.Core

//  ____            _       _ _          _   _
// / ___|  ___ _ __(_) __ _| (_)______ _| |_(_) ___  _ __
// \___ \ / _ \ '__| |/ _` | | |_  / _` | __| |/ _ \| '_ \
//  ___) |  __/ |  | | (_| | | |/ / (_| | |_| | (_) | | | |
// |____/ \___|_|  |_|\__,_|_|_/___\__,_|\__|_|\___/|_| |_|

[<AutoOpen>]
module Serialization =

  type 't encoder = Encoder of ('t -> byte array)

  type 't decoder = Decoder of (byte array -> 't option)

  let withEncoder (coder: 't encoder) (value: 't) : byte array =
    match coder with | Encoder f -> f value

  let withDecoder (coder: 't decoder) (value: byte array) : 't option =
    match coder with | Decoder f -> f value




//  _____      _                 _
// | ____|_  _| |_ ___ _ __  ___(_) ___  _ __  ___
// |  _| \ \/ / __/ _ \ '_ \/ __| |/ _ \| '_ \/ __|
// | |___ >  <| ||  __/ | | \__ \ | (_) | | | \__ \
// |_____/_/\_\\__\___|_| |_|___/_|\___/|_| |_|___/

open System.Runtime.CompilerServices
open Iris.Serialization.Raft
open Pallet.Core
open FlatBuffers

[<Extension>]
type NodeStateExtensions() =

  //  _   _           _      ____  _        _
  // | \ | | ___   __| | ___/ ___|| |_ __ _| |_ ___
  // |  \| |/ _ \ / _` |/ _ \___ \| __/ _` | __/ _ \
  // | |\  | (_) | (_| |  __/___) | || (_| | ||  __/
  // |_| \_|\___/ \__,_|\___|____/ \__\__,_|\__\___|

  [<Extension>]
  static member inline ToOffset (state: NodeState, _: unit) =
    match state with
      | Running -> NodeStateFB.RunningFB
      | Joining -> NodeStateFB.JoiningFB
      | Failed  -> NodeStateFB.FailedFB


[<Extension>]
type NodeExtensions() =
  //  _   _           _
  // | \ | | ___   __| | ___
  // |  \| |/ _ \ / _` |/ _ \
  // | |\  | (_) | (_| |  __/
  // |_| \_|\___/ \__,_|\___|

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
    NodeFB.AddNextIndex(builder, uint64 node.nextIndex)
    NodeFB.AddMatchIndex(builder, uint64 node.matchIndex)
    NodeFB.AddData(builder, info)
    NodeFB.EndNodeFB(builder)


[<Extension>]
type ConfigChangeExtensions() =
  //   ____             __ _        ____ _
  //  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  //  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                         |___/                         |___/
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

[<Extension>]
type LogExentions() =
  //  _
  // | |    ___   __ _
  // | |   / _ \ / _` |
  // | |__| (_) | (_| |
  // |_____\___/ \__, |
  //             |___/

  [<Extension>]
  static member inline ToOffset (entries: LogEntry, builder: FlatBufferBuilder) =
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

        let config = ConfigurationFB.EndConfigurationFB(builder)

        LogFB.StartLogFB(builder)
        LogFB.AddEntryType(builder, LogTypeFB.ConfigChangeFB)
        LogFB.AddEntry(builder,config.Value)
        LogFB.EndLogFB(builder)

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

        let config = JointConsensusFB.EndJointConsensusFB(builder)

        LogFB.StartLogFB(builder)
        LogFB.AddEntryType(builder, LogTypeFB.JointConsensusFB)
        LogFB.AddEntry(builder,config.Value)
        LogFB.EndLogFB(builder)

      //  _                _____       _
      // | |    ___   __ _| ____|_ __ | |_ _ __ _   _
      // | |   / _ \ / _` |  _| | '_ \| __| '__| | | |
      // | |__| (_) | (_| | |___| | | | |_| |  | |_| |
      // |_____\___/ \__, |_____|_| |_|\__|_|   \__, |
      //             |___/                      |___/
      | LogEntry(id,index,term,data,_) ->
        let id = string id |> builder.CreateString
        let data = data.ToOffset(builder)

        EntryFB.StartEntryFB(builder)
        EntryFB.AddId(builder, id)
        EntryFB.AddIndex(builder, uint64 index)
        EntryFB.AddTerm(builder, uint64 term)
        EntryFB.AddData(builder, data)

        let config = EntryFB.EndEntryFB(builder)

        LogFB.StartLogFB(builder)
        LogFB.AddEntryType(builder, LogTypeFB.EntryFB)
        LogFB.AddEntry(builder,config.Value)
        LogFB.EndLogFB(builder)

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

        let config = SnapshotFB.EndSnapshotFB(builder)

        LogFB.StartLogFB(builder)
        LogFB.AddEntryType(builder, LogTypeFB.SnapshotFB)
        LogFB.AddEntry(builder,config.Value)
        LogFB.EndLogFB(builder)

    let arr = Array.zeroCreate (Log.depth entries |> int)
    Log.iter (fun i (log: LogEntry) -> arr.[int i] <- toOffset log) entries
    arr


[<Extension>]
type RaftErrorExentions() =
  //  ____        __ _   _____
  // |  _ \ __ _ / _| |_| ____|_ __ _ __ ___  _ __
  // | |_) / _` | |_| __|  _| | '__| '__/ _ \| '__|
  // |  _ < (_| |  _| |_| |___| |  | | | (_) | |
  // |_| \_\__,_|_|  \__|_____|_|  |_|  \___/|_|

  [<Extension>]
  static member inline ToOffset (error: RaftError, builder: FlatBufferBuilder) =
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
        (fun (entries: LogEntry) -> entries.ToOffset(builder))
        ae.Entries

    AppendEntriesFB.StartAppendEntriesFB(builder)
    AppendEntriesFB.AddTerm(builder, uint64 ae.Term)
    AppendEntriesFB.AddPrevLogTerm(builder, uint64 ae.PrevLogTerm)
    AppendEntriesFB.AddPrevLogIdx(builder, uint64 ae.PrevLogIdx)
    AppendEntriesFB.AddLeaderCommit(builder, uint64 ae.LeaderCommit)

    match entries with
      | Some etr ->
        let etrvec = AppendEntriesFB.CreateEntriesVector(builder, etr)
        AppendEntriesFB.AddEntries(builder, etrvec)
      | _ -> ()

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
    let leader = string is.LeaderId |> builder.CreateString
    let data = InstallSnapshotFB.CreateDataVector(builder, is.Data.ToOffset(builder))

    InstallSnapshotFB.StartInstallSnapshotFB(builder)
    InstallSnapshotFB.AddTerm(builder, uint64 is.Term)
    InstallSnapshotFB.AddLeaderId(builder, leader)
    InstallSnapshotFB.AddLastTerm(builder, uint64 is.LastTerm)
    InstallSnapshotFB.AddLastIndex(builder, uint64 is.LastIndex)
    InstallSnapshotFB.AddData(builder, data)
    InstallSnapshotFB.EndInstallSnapshotFB(builder)

  [<Extension>]
  static member inline ToOffset (ir: SnapshotResponse, builder: FlatBufferBuilder) =
    SnapshotResponseFB.CreateSnapshotResponseFB(builder, uint64 ir.Term)
