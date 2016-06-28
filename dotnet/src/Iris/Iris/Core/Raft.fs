namespace Iris.Core

open Argu
open System
open FlatBuffers
open Pallet.Core
open Iris.Serialization.Raft


///////////////////////////////////////////////
//   ____ _     ___      _                   //
//  / ___| |   |_ _|    / \   _ __ __ _ ___  //
// | |   | |    | |    / _ \ | '__/ _` / __| //
// | |___| |___ | |   / ___ \| | | (_| \__ \ //
//  \____|_____|___| /_/   \_\_|  \__, |___/ //
//                                |___/      //
///////////////////////////////////////////////

type RaftOptions =
  { RaftId           : uint32
  ; Debug            : bool
  ; IpAddr           : string
  ; WebPort          : int
  ; RaftPort         : int
  ; Start            : bool
  ; LeaderId         : uint32 option
  ; LeaderIp         : string option
  ; LeaderPort       : uint32 option
  }

type GeneralArgs =
  | [<Mandatory>][<EqualsAssignment>] Bind        of string
  | [<Mandatory>][<EqualsAssignment>] Raft_Id     of uint32
  | [<Mandatory>][<EqualsAssignment>] Raft_Port   of uint32
  | [<Mandatory>][<EqualsAssignment>] Web_Port    of uint32
  |                                   Debug
  |                                   Start
  |                                   Join
  |              [<EqualsAssignment>] Leader_Id   of uint32
  |              [<EqualsAssignment>] Leader_Ip   of string
  |              [<EqualsAssignment>] Leader_Port of uint32

  interface IArgParserTemplate with
    member self.Usage =
      match self with
        | Bind        _ -> "Specify a valid IP address."
        | Web_Port    _ -> "Http server port."
        | Raft_Port   _ -> "Raft server port (internal)."
        | Raft_Id     _ -> "Raft server ID (internal)."
        | Debug         -> "Log output to console."
        | Start         -> "Start a new cluster"
        | Join          -> "Join an existing cluster"
        | Leader_Id   _ -> "Leader id when joining an existing cluster"
        | Leader_Ip   _ -> "Ip address of leader when joining a cluster"
        | Leader_Port _ -> "Port of leader when joining a cluster"


//  ____        __ _     __  __
// |  _ \ __ _ / _| |_  |  \/  |___  __ _
// | |_) / _` | |_| __| | |\/| / __|/ _` |
// |  _ < (_| |  _| |_  | |  | \__ \ (_| |
// |_| \_\__,_|_|  \__| |_|  |_|___/\__, |
//                                  |___/

type RaftMsg =
  | RequestVote             of sender:NodeId * req:VoteRequest<IrisNode>
  | RequestVoteResponse     of sender:NodeId * vote:VoteResponse
  | AppendEntries           of sender:NodeId * ae:AppendEntries<StateMachine,IrisNode>
  | AppendEntriesResponse   of sender:NodeId * ar:AppendResponse
  | InstallSnapshot         of sender:NodeId * is:InstallSnapshot<StateMachine,IrisNode>
  | InstallSnapshotResponse of sender:NodeId * bool
  | HandShake               of sender:Node<IrisNode>
  | HandWaive               of sender:Node<IrisNode>
  | ErrorResponse           of RaftError
  | EmptyResponse


[<RequireQualifiedAccess>]
module Log =

  let private encoder : LogEntry<'a,'n> encoder =
    let encode (log: LogEntry<'a,'n>) : byte array =
      failwith "TODO: LOG ENCODER"
    Encoder encode

  let private decoder : LogEntry<'a,'n> decoder =
    let decode (bytes: byte array) : LogEntry<'a,'n> option =
      failwith "TODO: LOG DECODER"
    Decoder decode

  let encode = withEncoder encoder
  let decode = withDecoder decoder

[<RequireQualifiedAccess>]
module AppendEntries =

  let private encoder : AppendEntries<'a,'n> encoder =
    let encode (log: AppendEntries<'a,'n>) : byte array =
      failwith "TODO: AE ENCODER"
    Encoder encode


[<RequireQualifiedAccess>]
module IrisNode =

  let toOffset (builder: FlatBufferBuilder) node =
    IrisNodeFB.StartIrisNodeFB(builder)
    IrisNodeFB.AddHostName(builder, node.HostName |> builder.CreateString)
    IrisNodeFB.AddIpAddr(builder, node.IpAddr.ToString() |> builder.CreateString)
    IrisNodeFB.AddPort(builder, uint32 node.Port)
    IrisNodeFB.EndIrisNodeFB(builder)

[<RequireQualifiedAccess>]
module NodeState =

  let toOffset (state: NodeState) =
    match state with
      | Running -> NodeStateFB.RunningFB
      | Joining -> NodeStateFB.JoiningFB
      | Failed  -> NodeStateFB.FailedFB


[<RequireQualifiedAccess>]
module Node =

  let toOffset (builder: FlatBufferBuilder) (node: Node<IrisNode>) =
    let info = IrisNode.toOffset builder node.Data
    NodeFB.StartNodeFB(builder)
    NodeFB.AddId(builder, string node.Id |> builder.CreateString)
    NodeFB.AddVoting(builder, node.Voting)
    NodeFB.AddVotedForMe(builder, node.VotedForMe)
    NodeFB.AddState(builder, NodeState.toOffset node.State)
    NodeFB.AddNextIndex(builder, uint64 node.nextIndex)
    NodeFB.AddMatchIndex(builder, uint64 node.matchIndex)
    NodeFB.AddData(builder, info)
    NodeFB.EndNodeFB(builder)


[<RequireQualifiedAccess>]
module RaftError =

  let toOffset (builder: FlatBufferBuilder) (err: RaftError) =
    let tipe =
      match err with
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
    
    match err with
      | OtherError msg ->
        let message = builder.CreateString msg
        RaftErrorFB.CreateRaftErrorFB(builder, tipe, message)
      | _ -> 
        RaftErrorFB.CreateRaftErrorFB(builder, tipe)

[<RequireQualifiedAccess>]
module VoteRequest =

  let toOffset (builder: FlatBufferBuilder) (request: VoteRequest<IrisNode>) =
    let node = Node.toOffset builder request.Candidate
    VoteRequestFB.StartVoteRequestFB(builder)
    VoteRequestFB.AddTerm(builder, uint64 request.Term)
    VoteRequestFB.AddLastLogTerm(builder, uint64 request.LastLogTerm)
    VoteRequestFB.AddLastLogIndex(builder, uint64 request.LastLogIndex)
    VoteRequestFB.AddCandidate(builder, node)
    VoteRequestFB.EndVoteRequestFB(builder)


module VoteResponse =

  let toOffset (builder: FlatBufferBuilder) (response: VoteResponse) =
    let err = Option.map (RaftError.toOffset builder) response.Reason
    VoteResponseFB.StartVoteResponseFB(builder)
    VoteResponseFB.AddTerm(builder, uint64 response.Term)
    match err with
      | Some offset -> VoteResponseFB.AddReason(builder, offset)
      | _ -> ()
    VoteResponseFB.AddGranted(builder, response.Granted)
    VoteResponseFB.EndVoteResponseFB(builder)


[<RequireQualifiedAccess>]
module Raft =

  //  __  __
  // |  \/  |___  __ _
  // | |\/| / __|/ _` |
  // | |  | \__ \ (_| |
  // |_|  |_|___/\__, |
  //             |___/

  let private encoder : RaftMsg encoder =
    let encoder msg =
      let builder = new FlatBufferBuilder(1)
      match msg with
      | RequestVote(nid, req) ->
        let nodeid = string nid |> builder.CreateString
        let request = VoteRequest.toOffset builder req
        let fb = RequestVoteFB.CreateRequestVoteFB(builder, nodeid, request)

        builder.Finish(fb.Value)
        builder.DataBuffer.Data

      | RequestVoteResponse(nid, resp) ->
        let nodeid = string nid |> builder.CreateString
        let response = VoteResponse.toOffset builder resp
        let fb = RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, nodeid, response)

        builder.Finish(fb.Value)
        builder.DataBuffer.Data

      | AppendEntries           _ as value -> Array.empty
      | AppendEntriesResponse   _ as value -> Array.empty
      | InstallSnapshot         _ as value -> Array.empty
      | InstallSnapshotResponse _ as value -> Array.empty
      | HandShake               _ as value -> Array.empty
      | HandWaive               _ as value -> Array.empty
      | ErrorResponse           _ as value -> Array.empty
      | EmptyResponse           _ as value -> Array.empty

    Encoder encoder

  let private decoder : RaftMsg decoder =
    let builder = new FlatBufferBuilder(1)
    let decoder (bytes: byte array) = None
    Decoder decoder

  let encode (value: RaftMsg) : byte array = withEncoder encoder value

  let decode (arr: byte array) : RaftMsg option = withDecoder decoder arr
