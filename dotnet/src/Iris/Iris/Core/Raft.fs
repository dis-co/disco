namespace Iris.Core

open Argu
open Pallet.Core

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
module Raft =

  //  __  __
  // |  \/  |___  __ _
  // | |\/| / __|/ _` |
  // | |  | \__ \ (_| |
  // |_|  |_|___/\__, |
  //             |___/

  open System
  open FlatBuffers
  open Iris.Serialization.Raft

  let private RaftMsgEncoder : RaftMsg encoder =
    let builder = new FlatBufferBuilder(1)

    let encoder = function
      | RequestVote             _ as value -> Array.empty
      | RequestVoteResponse     _ as value -> Array.empty
      | AppendEntries           _ as value -> Array.empty
      | AppendEntriesResponse   _ as value -> Array.empty
      | InstallSnapshot         _ as value -> Array.empty
      | InstallSnapshotResponse _ as value -> Array.empty
      | HandShake               _ as value -> Array.empty
      | HandWaive               _ as value -> Array.empty
      | ErrorResponse           _ as value -> Array.empty
      | EmptyResponse           _ as value -> Array.empty

    Encoder encoder

  let private RaftMsgDecoder : RaftMsg decoder =
    let builder = new FlatBufferBuilder(1)
    let decoder (bytes: byte array) = None
    Decoder decoder

  let encode (value: RaftMsg) : byte array = withEncoder RaftMsgEncoder value

  let decode (arr: byte array) : RaftMsg option = withDecoder RaftMsgDecoder arr
