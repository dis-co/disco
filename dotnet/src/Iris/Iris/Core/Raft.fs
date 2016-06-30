namespace Iris.Core

open Argu
open System
open FlatBuffers
open Pallet.Core
open Iris.Serialization.Raft

//  ____        __ _      ___        _   _
// |  _ \ __ _ / _| |_   / _ \ _ __ | |_(_) ___  _ __  ___
// | |_) / _` | |_| __| | | | | '_ \| __| |/ _ \| '_ \/ __|
// |  _ < (_| |  _| |_  | |_| | |_) | |_| | (_) | | | \__ \
// |_| \_\__,_|_|  \__|  \___/| .__/ \__|_|\___/|_| |_|___/
//                            |_|

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


//   ____ _     ___      _
//  / ___| |   |_ _|    / \   _ __ __ _ ___
// | |   | |    | |    / _ \ | '__/ _` / __|
// | |___| |___ | |   / ___ \| | | (_| \__ \
//  \____|_____|___| /_/   \_\_|  \__, |___/
//                                |___/

type GeneralArgs =
  | [<Mandatory>][<EqualsAssignment>] Bind        of string
  | [<Mandatory>][<EqualsAssignment>] RaftId     of uint32
  | [<Mandatory>][<EqualsAssignment>] RaftPort   of uint32
  | [<Mandatory>][<EqualsAssignment>] WebPort    of uint32
  |                                   Debug
  |                                   Start
  |                                   Join
  |              [<EqualsAssignment>] LeaderId   of uint32
  |              [<EqualsAssignment>] LeaderIp   of string
  |              [<EqualsAssignment>] LeaderPort of uint32

  interface IArgParserTemplate with

    member self.Usage =
      match self with
        | Bind       _ -> "Specify a valid IP address."
        | WebPort    _ -> "Http server port."
        | RaftPort   _ -> "Raft server port (internal)."
        | RaftId     _ -> "Raft server ID (internal)."
        | Debug        -> "Log output to console."
        | Start        -> "Start a new cluster"
        | Join         -> "Join an existing cluster"
        | LeaderId   _ -> "Leader id when joining an existing cluster"
        | LeaderIp   _ -> "Ip address of leader when joining a cluster"
        | LeaderPort _ -> "Port of leader when joining a cluster"


//  ____        __ _     __  __
// |  _ \ __ _ / _| |_  |  \/  |___  __ _
// | |_) / _` | |_| __| | |\/| / __|/ _` |
// |  _ < (_| |  _| |_  | |  | \__ \ (_| |
// |_| \_\__,_|_|  \__| |_|  |_|___/\__, |
//                                  |___/
[<AutoOpen>]
module RaftMsgFB = 

  let getValue (t : Offset<'a>) : int = t.Value

  let build builder tipe value =
    RaftMsgFB.StartRaftMsgFB(builder)
    RaftMsgFB.AddMsgType(builder, tipe)
    RaftMsgFB.AddMsg(builder, value)
    RaftMsgFB.EndRaftMsgFB(builder)
    |> getValue

  let createAppendEntriesFB builder nid (ar: AppendEntries) =
    RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, uint64 nid, ar.ToOffset builder)
    |> getValue

  let createAppendResponseFB builder nid (ar: AppendResponse) =
    RequestAppendResponseFB.CreateRequestAppendResponseFB(builder, uint64 nid, ar.ToOffset builder)
    |> getValue

  let createRequestVoteFB builder nid (vr: VoteRequest) =
    RequestVoteFB.CreateRequestVoteFB(builder, uint64 nid, vr.ToOffset builder)
    |> getValue

  let createRequestVoteResponseFB builder nid (vr: VoteResponse) =
    RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, uint64 nid, vr.ToOffset builder)
    |> getValue

  let createInstallSnapshotFB bldr id (is: InstallSnapshot) =
    RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(bldr, uint64 id, is.ToOffset bldr)
    |> getValue

  let createSnapshotResponseFB bldr id (sr: SnapshotResponse) =
    RequestSnapshotResponseFB.CreateRequestSnapshotResponseFB(bldr, uint64 id, sr.ToOffset bldr)
    |> getValue

type RaftMsg =
  | RequestVote             of sender:NodeId * req:VoteRequest<IrisNode>
  | RequestVoteResponse     of sender:NodeId * vote:VoteResponse
  | AppendEntries           of sender:NodeId * ae:AppendEntries<StateMachine,IrisNode>
  | AppendEntriesResponse   of sender:NodeId * ar:AppendResponse
  | InstallSnapshot         of sender:NodeId * is:InstallSnapshot<StateMachine,IrisNode>
  | InstallSnapshotResponse of sender:NodeId * ir:SnapshotResponse
  | HandShake               of sender:Node<IrisNode>
  | HandWaive               of sender:Node<IrisNode>
  | ErrorResponse           of RaftError
  | EmptyResponse

  with

    member self.ToBytes () : byte array =
      let builder = new FlatBufferBuilder(1)

      match self with
      //  ____                            _ __     __    _
      // |  _ \ ___  __ _ _   _  ___  ___| |\ \   / /__ | |_ ___
      // | |_) / _ \/ _` | | | |/ _ \/ __| __\ \ / / _ \| __/ _ \
      // |  _ <  __/ (_| | |_| |  __/\__ \ |_ \ V / (_) | ||  __/
      // |_| \_\___|\__, |\__,_|\___||___/\__| \_/ \___/ \__\___|
      //               |_|
      
      | RequestVote(nid, req) ->
        createRequestVoteFB builder nid req
        |> build builder RaftMsgTypeFB.RequestVoteFB
        |> builder.Finish

      | RequestVoteResponse(nid, resp) ->
        createRequestVoteResponseFB builder nid resp
        |> build builder RaftMsgTypeFB.RequestVoteResponseFB
        |> builder.Finish

      //     _                               _ _____       _        _
      //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
      //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
      //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
      // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
      //         |_|   |_|

      | AppendEntries(nid, ae) ->
        createAppendEntriesFB builder nid ae
        |> build builder RaftMsgTypeFB.RequestAppendEntriesFB
        |> builder.Finish

      | AppendEntriesResponse(nid, ar) ->
        createAppendResponseFB builder nid ar
        |> build builder RaftMsgTypeFB.RequestAppendResponseFB
        |> builder.Finish

      //  ___           _        _ _ ____                        _           _
      // |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      //  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                                              |_|

      | InstallSnapshot(nid, is) ->
        createInstallSnapshotFB builder nid is
        |> build builder RaftMsgTypeFB.RequestInstallSnapshotFB
        |> builder.Finish

      | InstallSnapshotResponse(nid, ir) ->
        createSnapshotResponseFB builder nid ir
        |> build builder RaftMsgTypeFB.RequestSnapshotResponseFB
        |> builder.Finish

      //  _   _                 _ ____  _           _
      // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
      // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
      // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
      // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

      | HandShake node ->
        HandShakeFB.CreateHandShakeFB(builder, node.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.HandShakeFB
        |> builder.Finish

      | HandWaive node ->
        HandWaiveFB.CreateHandWaiveFB(builder, node.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.HandWaiveFB
        |> builder.Finish

      //  _____
      // | ____|_ __ _ __ ___  _ __
      // |  _| | '__| '__/ _ \| '__|
      // | |___| |  | | | (_) | |
      // |_____|_|  |_|  \___/|_|

      | ErrorResponse err ->
        ErrorResponseFB.CreateErrorResponseFB(builder, err.ToOffset builder)
        |> getValue
        |> build builder RaftMsgTypeFB.ErrorResponseFB
        |> builder.Finish

      | EmptyResponse ->
        EmptyResponseFB.StartEmptyResponseFB(builder)
        EmptyResponseFB.EndEmptyResponseFB(builder)
        |> getValue
        |> build builder RaftMsgTypeFB.EmptyResponseFB
        |> builder.Finish

      //  ____                 _ _
      // |  _ \ ___  ___ _   _| | |_
      // | |_) / _ \/ __| | | | | __|
      // |  _ <  __/\__ \ |_| | | |_
      // |_| \_\___||___/\__,_|_|\__|

      builder.SizedByteArray()

    static member FromBytes (bytes: byte array) : RaftMsg option =
      let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
      match msg.MsgType with
        | RaftMsgTypeFB.RequestVoteFB ->
          let entry = msg.GetMsg(new RequestVoteFB())
          let request = VoteRequest<IrisNode>.FromFB(entry.Request)

          RequestVote(uint32 entry.NodeId, request)
          |> Some

        | RaftMsgTypeFB.RequestVoteResponseFB ->
          let entry = msg.GetMsg(new RequestVoteResponseFB())
          let response = VoteResponse.FromFB entry.Response

          RequestVoteResponse(uint32 entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.RequestAppendEntriesFB ->
          let entry = msg.GetMsg(new RequestAppendEntriesFB())
          let request = AppendEntries.FromFB entry.Request

          AppendEntries(uint32 entry.NodeId, request)
          |> Some

        | RaftMsgTypeFB.RequestAppendResponseFB ->
          let entry = msg.GetMsg(new RequestAppendResponseFB())
          let response = AppendResponse.FromFB entry.Response

          AppendEntriesResponse(uint32 entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.RequestInstallSnapshotFB ->
          let entry = msg.GetMsg(new RequestInstallSnapshotFB())
          let request = InstallSnapshot.FromFB entry.Request

          InstallSnapshot(uint32 entry.NodeId, request)
          |> Some
          
        | RaftMsgTypeFB.RequestSnapshotResponseFB ->
          let entry = msg.GetMsg(new RequestSnapshotResponseFB())
          let response = SnapshotResponse.FromFB entry.Response

          InstallSnapshotResponse(uint32 entry.NodeId, response)
          |> Some

        | RaftMsgTypeFB.HandShakeFB ->
          let entry = msg.GetMsg(new HandShakeFB())
          let node = Node.FromFB entry.Node

          HandShake(node) |> Some

        | RaftMsgTypeFB.HandWaiveFB ->
          let entry = msg.GetMsg(new HandWaiveFB())
          let node = Node.FromFB entry.Node

          HandWaive(node) |> Some

        | RaftMsgTypeFB.ErrorResponseFB ->
          let entry = msg.GetMsg(new ErrorResponseFB())

          ErrorResponse(RaftError.FromFB entry.Error)
          |> Some

        | RaftMsgTypeFB.EmptyResponseFB -> Some EmptyResponse

        | _ ->
          failwith "unable to de-serialize unknown garbage RaftMsgTypeFB"
