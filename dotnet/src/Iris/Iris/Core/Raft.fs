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
        let request = req.ToOffset(builder)
        let rv = RequestVoteFB.CreateRequestVoteFB(builder, uint64 nid, request)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestVoteFB)
        RaftMsgFB.AddMsg(builder, rv.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      | RequestVoteResponse(nid, resp) ->
        let response = resp.ToOffset(builder)
        let rvp = RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, uint64 nid, response)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestVoteResponseFB)
        RaftMsgFB.AddMsg(builder, rvp.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      //     _                               _ _____       _        _
      //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
      //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
      //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
      // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
      //         |_|   |_|

      | AppendEntries(nid, ae) ->
        let ae = ae.ToOffset(builder)
        let rae = RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, uint64 nid, ae)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestAppendEntriesFB)
        RaftMsgFB.AddMsg(builder, rae.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      | AppendEntriesResponse(nid, ar) ->
        let resp = ar.ToOffset(builder)
        let aer = RequestAppendResponseFB.CreateRequestAppendResponseFB(builder, uint64 nid, resp)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestAppendResponseFB)
        RaftMsgFB.AddMsg(builder, aer.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      //  ___           _        _ _ ____                        _           _
      // |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      //  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                                              |_|

      | InstallSnapshot(nid, is) ->
        let req = is.ToOffset(builder)
        let ris = RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(builder, uint64 nid, req)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestInstallSnapshotFB)
        RaftMsgFB.AddMsg(builder, ris.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      | InstallSnapshotResponse(nid, ir) ->
        let id = uint64 nid
        let resp = ir.ToOffset(builder)
        let risr = RequestSnapshotResponseFB.CreateRequestSnapshotResponseFB(builder, id, resp)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.RequestSnapshotResponseFB)
        RaftMsgFB.AddMsg(builder, risr.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      //  _   _                 _ ____  _           _
      // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
      // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
      // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
      // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

      | HandShake node ->
        let node = node.ToOffset(builder)
        let shake = HandShakeFB.CreateHandShakeFB(builder, node)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.HandShakeFB)
        RaftMsgFB.AddMsg(builder, shake.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      | HandWaive node ->
        let node = node.ToOffset(builder)
        let waive = HandWaiveFB.CreateHandWaiveFB(builder, node)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.HandWaiveFB)
        RaftMsgFB.AddMsg(builder, waive.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      //  _____
      // | ____|_ __ _ __ ___  _ __
      // |  _| | '__| '__/ _ \| '__|
      // | |___| |  | | | (_) | |
      // |_____|_|  |_|  \___/|_|

      | ErrorResponse err ->
        let error = err.ToOffset(builder)
        let fb = ErrorResponseFB.CreateErrorResponseFB(builder, error)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.ErrorResponseFB)
        RaftMsgFB.AddMsg(builder, fb.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

      | EmptyResponse ->
        EmptyResponseFB.StartEmptyResponseFB(builder)
        let fb = EmptyResponseFB.EndEmptyResponseFB(builder)
        RaftMsgFB.StartRaftMsgFB(builder)
        RaftMsgFB.AddMsgType(builder, RaftMsgTypeFB.EmptyResponseFB)
        RaftMsgFB.AddMsg(builder, fb.Value)
        let msg = RaftMsgFB.EndRaftMsgFB(builder)
        builder.Finish(msg.Value)

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
