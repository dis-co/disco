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
        let nodeid = string nid |> builder.CreateString
        let request = req.ToOffset(builder)
        let fb = RequestVoteFB.CreateRequestVoteFB(builder, nodeid, request)
        builder.Finish(fb.Value)

      | RequestVoteResponse(nid, resp) ->
        let nodeid = string nid |> builder.CreateString
        let response = resp.ToOffset(builder)
        let fb = RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, nodeid, response)
        builder.Finish(fb.Value)

      //     _                               _ _____       _        _
      //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
      //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
      //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
      // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
      //         |_|   |_|

      | AppendEntries(nid, ae) ->
        let nodeid = string nid |> builder.CreateString
        let appendentries = ae.ToOffset(builder)
        let fb = RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, nodeid, appendentries)
        builder.Finish(fb.Value)

      | AppendEntriesResponse(nid, ar) ->
        let nodeid = string nid |> builder.CreateString
        let response = ar.ToOffset(builder)
        let fb = RequestAppendResponseFB.CreateRequestAppendResponseFB(builder, nodeid, response)
        builder.Finish(fb.Value)

      //  ___           _        _ _ ____                        _           _
      // |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      //  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                                              |_|

      | InstallSnapshot(nid, is) ->
        let nodeid = string id |> builder.CreateString
        let request = is.ToOffset(builder)
        let fb = RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(builder, nodeid, request)
        builder.Finish(fb.Value)

      | InstallSnapshotResponse(nid, ir) ->
        let nodeid = string id |> builder.CreateString
        let response = ir.ToOffset(builder)
        let fb = RequestSnapshotResponseFB.CreateRequestSnapshotResponseFB(builder, nodeid, response)
        builder.Finish(fb.Value)

      //  _   _                 _ ____  _           _
      // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
      // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
      // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
      // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

      | HandShake node ->
        let node = node.ToOffset(builder)
        let shake = HandShakeFB.CreateHandShakeFB(builder, node)
        builder.Finish(shake.Value)
        
      | HandWaive node ->
        let node = node.ToOffset(builder)
        let waive = HandWaiveFB.CreateHandWaiveFB(builder, node)
        builder.Finish(waive.Value)

      //  _____
      // | ____|_ __ _ __ ___  _ __
      // |  _| | '__| '__/ _ \| '__|
      // | |___| |  | | | (_) | |
      // |_____|_|  |_|  \___/|_|

      | ErrorResponse err ->
        let error = err.ToOffset(builder)
        let fb = ErrorResponseFB.CreateErrorResponseFB(builder, error)
        builder.Finish(fb.Value)
       
      | EmptyResponse ->
        EmptyResponseFB.StartEmptyResponseFB(builder)
        let fb = EmptyResponseFB.EndEmptyResponseFB(builder)
        builder.Finish(fb.Value)

      //  ____                 _ _
      // |  _ \ ___  ___ _   _| | |_
      // | |_) / _ \/ __| | | | | __|
      // |  _ <  __/\__ \ |_| | | |_
      // |_| \_\___||___/\__,_|_|\__|
 
      builder.DataBuffer.Data

    static member FromBytes (bytes: byte array) : RaftMsg option =
      failwith "nope"
