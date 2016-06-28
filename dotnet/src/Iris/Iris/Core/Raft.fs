namespace Iris.Core

open Argu
open System
open FlatBuffers
open Pallet.Core
open Iris.Serialization.Raft

//     _    _ _
//    / \  | (_) __ _ ___  ___  ___
//   / _ \ | | |/ _` / __|/ _ \/ __|
//  / ___ \| | | (_| \__ \  __/\__ \
// /_/   \_\_|_|\__,_|___/\___||___/

type ConfigChange = ConfigChange<IrisNode>
type Log = Log<StateMachine,IrisNode>
type LogEntry = LogEntry<StateMachine,IrisNode>
type Raft = Raft<StateMachine,IrisNode>
type AppendEntries = AppendEntries<StateMachine,IrisNode>
type VoteRequest = VoteRequest<IrisNode>
type Node = Node<IrisNode>

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
  | InstallSnapshotResponse of sender:NodeId * bool
  | HandShake               of sender:Node<IrisNode>
  | HandWaive               of sender:Node<IrisNode>
  | ErrorResponse           of RaftError
  | EmptyResponse



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
      //  ____                            _ __     __    _
      // |  _ \ ___  __ _ _   _  ___  ___| |\ \   / /__ | |_ ___
      // | |_) / _ \/ _` | | | |/ _ \/ __| __\ \ / / _ \| __/ _ \
      // |  _ <  __/ (_| | |_| |  __/\__ \ |_ \ V / (_) | ||  __/
      // |_| \_\___|\__, |\__,_|\___||___/\__| \_/ \___/ \__\___|
      //               |_|

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

      //     _                               _ _____       _        _
      //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
      //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
      //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
      // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
      //         |_|   |_|

      | AppendEntries(nid, ae) ->
        let nodeid = string nid |> builder.CreateString
        let appendentries = ae.ToOffset builder
        let fb = RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, nodeid, appendentries)

        builder.Finish(fb.Value)
        builder.DataBuffer.Data

      | AppendEntriesResponse   _ as value -> Array.empty

      //  ___           _        _ _ ____                        _           _
      // |_ _|_ __  ___| |_ __ _| | / ___| _ __   __ _ _ __  ___| |__   ___ | |_
      //  | || '_ \/ __| __/ _` | | \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
      //  | || | | \__ \ || (_| | | |___) | | | | (_| | |_) \__ \ | | | (_) | |_
      // |___|_| |_|___/\__\__,_|_|_|____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
      //                                              |_|

      | InstallSnapshot         _ as value -> Array.empty
      | InstallSnapshotResponse _ as value -> Array.empty

      //  _   _                 _ ____  _           _
      // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
      // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
      // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
      // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

      | HandShake               _ as value -> Array.empty
      | HandWaive               _ as value -> Array.empty

      //  _____
      // | ____|_ __ _ __ ___  _ __
      // |  _| | '__| '__/ _ \| '__|
      // | |___| |  | | | (_) | |
      // |_____|_|  |_|  \___/|_|

      | ErrorResponse           _ as value -> Array.empty
      | EmptyResponse           _ as value -> Array.empty

    Encoder encoder

  let private decoder : RaftMsg decoder =
    let builder = new FlatBufferBuilder(1)
    let decoder (bytes: byte array) = None
    Decoder decoder

  let encode (value: RaftMsg) : byte array = withEncoder encoder value

  let decode (arr: byte array) : RaftMsg option = withDecoder decoder arr
