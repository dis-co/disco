namespace Iris.Core

open Argu
open FlatBuffers
open Iris.Raft
open Iris.Serialization.Raft

//  _____ ____    _   _      _
// |  ___| __ )  | | | | ___| |_ __   ___ _ __ ___
// | |_  |  _ \  | |_| |/ _ \ | '_ \ / _ \ '__/ __|
// |  _| | |_) | |  _  |  __/ | |_) |  __/ |  \__ \
// |_|   |____/  |_| |_|\___|_| .__/ \___|_|  |___/
//                            |_|
[<AutoOpen>]
module RaftMsgFB =

  let getValue (t : Offset<'a>) : int = t.Value

  let inline build< ^t when ^t : not struct > builder tipe (offset: Offset< ^t >) =
    RaftMsgFB.StartRaftMsgFB(builder)
    RaftMsgFB.AddMsgType(builder, tipe)
    RaftMsgFB.AddMsg(builder, offset.Value)
    RaftMsgFB.EndRaftMsgFB(builder)

  let createAppendEntriesFB (builder: FlatBufferBuilder) (nid: NodeId) (ar: AppendEntries) =
    let sid = string nid |> builder.CreateString
    RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, sid, ar.ToOffset builder)

  let createAppendResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (ar: AppendResponse) =
    let id = string nid |> builder.CreateString
    RequestAppendResponseFB.CreateRequestAppendResponseFB(builder, id, ar.ToOffset builder)

  let createRequestVoteFB (builder: FlatBufferBuilder) (nid: NodeId) (vr: VoteRequest) =
    let id = string nid |> builder.CreateString
    RequestVoteFB.CreateRequestVoteFB(builder, id, vr.ToOffset builder)

  let createRequestVoteResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (vr: VoteResponse) =
    let id = string nid |> builder.CreateString
    RequestVoteResponseFB.CreateRequestVoteResponseFB(builder, id, vr.ToOffset builder)

  let createInstallSnapshotFB (builder: FlatBufferBuilder) (nid: NodeId) (is: InstallSnapshot) =
    let id = string nid |> builder.CreateString
    RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(builder, id, is.ToOffset builder)

  let createSnapshotResponseFB (builder: FlatBufferBuilder) (nid: NodeId) (ar: AppendResponse) =
    let id = string nid |> builder.CreateString
    RequestSnapshotResponseFB.CreateRequestSnapshotResponseFB(builder, id, ar.ToOffset builder)

//  ____        __ _     ____                            _
// |  _ \ __ _ / _| |_  |  _ \ ___  __ _ _   _  ___  ___| |_
// | |_) / _` | |_| __| | |_) / _ \/ _` | | | |/ _ \/ __| __|
// |  _ < (_| |  _| |_  |  _ <  __/ (_| | |_| |  __/\__ \ |_
// |_| \_\__,_|_|  \__| |_| \_\___|\__, |\__,_|\___||___/\__|
//                                    |_|

type RaftRequest =
  | RequestVote             of sender:NodeId * req:VoteRequest
  | AppendEntries           of sender:NodeId * ae:AppendEntries
  | InstallSnapshot         of sender:NodeId * is:InstallSnapshot
  | HandShake               of sender:RaftNode
  | HandWaive               of sender:RaftNode
  with

    member self.ToOffset(builder: FlatBufferBuilder) : Offset<RaftMsgFB> =
      match self with
      | RequestVote(nid, req) ->
        createRequestVoteFB builder nid req
        |> build builder RaftMsgTypeFB.RequestVoteFB

      | AppendEntries(nid, ae) ->
        createAppendEntriesFB builder nid ae
        |> build builder RaftMsgTypeFB.RequestAppendEntriesFB

      | InstallSnapshot(nid, is) ->
        createInstallSnapshotFB builder nid is
        |> build builder RaftMsgTypeFB.RequestInstallSnapshotFB

      | HandShake node ->
        HandShakeFB.CreateHandShakeFB(builder, node.ToOffset builder)
        |> build builder RaftMsgTypeFB.HandShakeFB

      | HandWaive node ->
        HandWaiveFB.CreateHandWaiveFB(builder, node.ToOffset builder)
        |> build builder RaftMsgTypeFB.HandWaiveFB

    //  _____     ____        _
    // |_   _|__ | __ ) _   _| |_ ___  ___
    //   | |/ _ \|  _ \| | | | __/ _ \/ __|
    //   | | (_) | |_) | |_| | ||  __/\__ \
    //   |_|\___/|____/ \__, |\__\___||___/
    //                  |___/

    member self.ToBytes () : byte array = buildBuffer self

    //  _____                    ____        _
    // |  ___| __ ___  _ __ ___ | __ ) _   _| |_ ___  ___
    // | |_ | '__/ _ \| '_ ` _ \|  _ \| | | | __/ _ \/ __|
    // |  _|| | | (_) | | | | | | |_) | |_| | ||  __/\__ \
    // |_|  |_|  \___/|_| |_| |_|____/ \__, |\__\___||___/
    //                                 |___/

    static member FromBytes (bytes: byte array) : RaftRequest option =
      let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
      match msg.MsgType with
        | RaftMsgTypeFB.RequestVoteFB ->
          let entry = msg.GetMsg(new RequestVoteFB())
          VoteRequest.FromFB(entry.Request)
          |> Option.map (fun request -> RequestVote(Id entry.NodeId, request))

        | RaftMsgTypeFB.RequestAppendEntriesFB ->
          let entry = msg.GetMsg(new RequestAppendEntriesFB())
          AppendEntries.FromFB entry.Request
          |> Option.map (fun request -> AppendEntries(Id entry.NodeId, request))

        | RaftMsgTypeFB.RequestInstallSnapshotFB ->
          let entry = msg.GetMsg(new RequestInstallSnapshotFB())
          InstallSnapshot.FromFB entry.Request
          |> Option.map (fun request -> InstallSnapshot(Id entry.NodeId, request))

        | RaftMsgTypeFB.HandShakeFB ->
          let entry = msg.GetMsg(new HandShakeFB())
          RaftNode.FromFB entry.Node
          |> Option.map (fun node -> HandShake(node))

        | RaftMsgTypeFB.HandWaiveFB ->
          let entry = msg.GetMsg(new HandWaiveFB())
          RaftNode.FromFB entry.Node
          |> Option.map (fun node -> HandWaive(node))

        | _ -> None

//  ____        __ _     ____
// |  _ \ __ _ / _| |_  |  _ \ ___  ___ _ __   ___  _ __  ___  ___
// | |_) / _` | |_| __| | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
// |  _ < (_| |  _| |_  |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// |_| \_\__,_|_|  \__| |_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//                                     |_|

type RaftResponse =
  | RequestVoteResponse     of sender:NodeId * vote:VoteResponse
  | AppendEntriesResponse   of sender:NodeId * ar:AppendResponse
  | InstallSnapshotResponse of sender:NodeId * ar:AppendResponse
  | Redirect                of leader:RaftNode
  | Welcome                 of leader:RaftNode
  | Arrivederci
  | ErrorResponse           of RaftError

  with

    //  _____      ___   __  __          _
    // |_   _|__  / _ \ / _|/ _|___  ___| |_
    //   | |/ _ \| | | | |_| |_/ __|/ _ \ __|
    //   | | (_) | |_| |  _|  _\__ \  __/ |_
    //   |_|\___/ \___/|_| |_| |___/\___|\__|

    member self.ToOffset(builder: FlatBufferBuilder) =
      match self with
      | RequestVoteResponse(nid, resp) ->
        createRequestVoteResponseFB builder nid resp
        |> build builder RaftMsgTypeFB.RequestVoteResponseFB

      | AppendEntriesResponse(nid, ar) ->
        createAppendResponseFB builder nid ar
        |> build builder RaftMsgTypeFB.RequestAppendResponseFB

      | InstallSnapshotResponse(nid, ir) ->
        createSnapshotResponseFB builder nid ir
        |> build builder RaftMsgTypeFB.RequestSnapshotResponseFB

      | Redirect node ->
        RedirectFB.CreateRedirectFB(builder, node.ToOffset builder)
        |> build builder RaftMsgTypeFB.RedirectFB

      | Welcome node ->
        WelcomeFB.CreateWelcomeFB(builder, node.ToOffset builder)
        |> build builder RaftMsgTypeFB.WelcomeFB

      | Arrivederci ->
        ArrivederciFB.StartArrivederciFB(builder)
        ArrivederciFB.EndArrivederciFB(builder)
        |> build builder RaftMsgTypeFB.ArrivederciFB

      | ErrorResponse err ->
        ErrorResponseFB.CreateErrorResponseFB(builder, err.ToOffset builder)
        |> build builder RaftMsgTypeFB.ErrorResponseFB

    //  _____                    _____ ____
    // |  ___| __ ___  _ __ ___ |  ___| __ )
    // | |_ | '__/ _ \| '_ ` _ \| |_  |  _ \
    // |  _|| | | (_) | | | | | |  _| | |_) |
    // |_|  |_|  \___/|_| |_| |_|_|   |____/

    static member FromFB(msg: RaftMsgFB) : RaftResponse option =
      match msg.MsgType with
      | RaftMsgTypeFB.RequestVoteResponseFB ->
        let entry = msg.GetMsg(new RequestVoteResponseFB())
        let response = VoteResponse.FromFB entry.Response

        RequestVoteResponse(Id entry.NodeId, response)
        |> Some

      | RaftMsgTypeFB.RequestAppendResponseFB ->
        let entry = msg.GetMsg(new RequestAppendResponseFB())
        AppendResponse.FromFB entry.Response
        |> Option.map
          (fun response ->
            AppendEntriesResponse(Id entry.NodeId, response))

      | RaftMsgTypeFB.RequestSnapshotResponseFB ->
        let entry = msg.GetMsg(new RequestSnapshotResponseFB())
        AppendResponse.FromFB entry.Response
        |> Option.map
          (fun response ->
            InstallSnapshotResponse(Id entry.NodeId, response))

      | RaftMsgTypeFB.RedirectFB ->
        let entry = msg.GetMsg(new RedirectFB())
        RaftNode.FromFB entry.Node
        |> Option.map (fun node -> Redirect(node))

      | RaftMsgTypeFB.WelcomeFB ->
        let entry = msg.GetMsg(new WelcomeFB())
        RaftNode.FromFB entry.Node
        |> Option.map (fun node -> Welcome(node))

      | RaftMsgTypeFB.ArrivederciFB ->
        Some Arrivederci

      | RaftMsgTypeFB.ErrorResponseFB ->
        let entry = msg.GetMsg(new ErrorResponseFB())
        RaftError.FromFB entry.Error
        |> Option.map ErrorResponse

      | _ -> None

    //  _____     ____        _
    // |_   _|__ | __ ) _   _| |_ ___  ___
    //   | |/ _ \|  _ \| | | | __/ _ \/ __|
    //   | | (_) | |_) | |_| | ||  __/\__ \
    //   |_|\___/|____/ \__, |\__\___||___/
    //                  |___/

    member self.ToBytes () : byte array = buildBuffer self

    //  _____                    ____        _
    // |  ___| __ ___  _ __ ___ | __ ) _   _| |_ ___  ___
    // | |_ | '__/ _ \| '_ ` _ \|  _ \| | | | __/ _ \/ __|
    // |  _|| | | (_) | | | | | | |_) | |_| | ||  __/\__ \
    // |_|  |_|  \___/|_| |_| |_|____/ \__, |\__\___||___/
    //                                 |___/

    static member FromBytes (bytes: byte array) : RaftResponse option =
      let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
      RaftResponse.FromFB msg
