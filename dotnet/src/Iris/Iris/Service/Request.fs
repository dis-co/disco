namespace Iris.Service

open Argu
open FlatBuffers
open Iris.Raft
open Iris.Serialization.Raft
open Iris.Core
open Iris.Raft

//  _____ ____    _   _      _
// |  ___| __ )  | | | | ___| |_ __   ___ _ __ ___
// | |_  |  _ \  | |_| |/ _ \ | '_ \ / _ \ '__/ __|
// |  _| | |_) | |  _  |  __/ | |_) |  __/ |  \__ \
// |_|   |____/  |_| |_|\___|_| .__/ \___|_|  |___/
//                            |_|
[<AutoOpen>]
module RaftMsgFB =

  let getValue (t : Offset<'a>) : int = t.Value

  let inline build< ^t when ^t : struct and ^t :> System.ValueType and ^t : (new : unit -> ^t) > builder tipe (offset: Offset< ^t >) =
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
  | RequestVote     of sender:NodeId * req:VoteRequest
  | AppendEntries   of sender:NodeId * ae:AppendEntries
  | InstallSnapshot of sender:NodeId * is:InstallSnapshot
  | HandShake       of sender:RaftNode
  | HandWaive       of sender:RaftNode

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

  member self.ToBytes () : byte array = Binary.buildBuffer self

  //  _____                    ____        _
  // |  ___| __ ___  _ __ ___ | __ ) _   _| |_ ___  ___
  // | |_ | '__/ _ \| '_ ` _ \|  _ \| | | | __/ _ \/ __|
  // |  _|| | | (_) | | | | | | |_) | |_| | ||  __/\__ \
  // |_|  |_|  \___/|_| |_| |_|____/ \__, |\__\___||___/
  //                                 |___/

  static member FromBytes (bytes: byte array) : Either<IrisError, RaftRequest> =
    let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
    match msg.MsgType with
      | RaftMsgTypeFB.RequestVoteFB -> either {
          let entry = msg.Msg<RequestVoteFB>()
          if entry.HasValue then
            let rv = entry.Value
            let request = rv.Request
            if request.HasValue then
              let! request = VoteRequest.FromFB(request.Value)
              return RequestVote(Id rv.NodeId, request)
            else
              return!
                "Could not parse empty VoteRequestFB body"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty RequestVoteFB body"
              |> ParseError
              |> Either.fail
        }

      | RaftMsgTypeFB.RequestAppendEntriesFB -> either {
          let entry = msg.Msg<RequestAppendEntriesFB>()
          if entry.HasValue then
            let ae = entry.Value
            let request = ae.Request
            if request.HasValue then
              let! request = AppendEntries.FromFB request.Value
              return AppendEntries(Id ae.NodeId, request)
            else
              return!
                "Could not parse empty AppendEntriesFB body"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty RequestAppendEntriesFB body"
              |> ParseError
              |> Either.fail
        }

      | RaftMsgTypeFB.RequestInstallSnapshotFB -> either {
          let entry = msg.Msg<RequestInstallSnapshotFB>()
          if entry.HasValue then
            let is = entry.Value
            let request = is.Request
            if request.HasValue then
              let! request = InstallSnapshot.FromFB request.Value
              return InstallSnapshot(Id is.NodeId, request)
            else
              return!
                "Could not parse empty InstallSnapshotFB body"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty RequestInstallSnapshotFB body"
              |> ParseError
              |> Either.fail
        }

      | RaftMsgTypeFB.HandShakeFB -> either {
          let entry = msg.Msg<HandShakeFB>()
          if entry.HasValue then
            let hs = entry.Value
            let node = hs.Node
            if node.HasValue then
              let! node = RaftNode.FromFB node.Value
              return HandShake(node)
            else
              return!
                "Could not parse empty RaftNodeFB body"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty HandShakeFB body"
              |> ParseError
              |> Either.fail
        }

      | RaftMsgTypeFB.HandWaiveFB -> either {
          let entry = msg.Msg<HandWaiveFB>()
          if entry.HasValue then
            let hw = entry.Value
            let node = hw.Node
            if node.HasValue then
              let! node = RaftNode.FromFB node.Value
              return HandWaive(node)
            else
              return!
                "Could not parse empty RaftNodeFB body"
                |> ParseError
                |> Either.fail
          else
            return!
              "Could not parse empty HandShakeFB body"
              |> ParseError
              |> Either.fail
        }

      | x ->
        sprintf "Could not parse unknown RaftMsgTypeFB: %A" x
        |> ParseError
        |> Either.fail

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
  | ErrorResponse           of IrisError

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

  static member FromFB(msg: RaftMsgFB) : Either<IrisError,RaftResponse> =
    match msg.MsgType with
    | RaftMsgTypeFB.RequestVoteResponseFB -> either {
        let entry = msg.Msg<RequestVoteResponseFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! parsed = VoteResponse.FromFB response.Value
            return RequestVoteResponse(Id fb.NodeId, parsed)
          else
            return!
              "Could not parse empty VoteResponseFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty RequestVoteResponseFB body"
            |> ParseError
            |> Either.fail
      }

    | RaftMsgTypeFB.RequestAppendResponseFB -> either {
        let entry = msg.Msg<RequestAppendResponseFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! response = AppendResponse.FromFB response.Value
            return AppendEntriesResponse(Id fb.NodeId, response)
          else
            return!
              "Could not parse empty AppendResponseFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty RequestAppendResponseFB body"
            |> ParseError
            |> Either.fail
      }

    | RaftMsgTypeFB.RequestSnapshotResponseFB -> either {
        let entry = msg.Msg<RequestSnapshotResponseFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! response = AppendResponse.FromFB response.Value
            return InstallSnapshotResponse(Id fb.NodeId, response)
          else
            return!
              "Could not parse empty AppendResponseFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty RequestSnapshotResponseFB body"
            |> ParseError
            |> Either.fail
      }

    | RaftMsgTypeFB.RedirectFB -> either {
        let entry = msg.Msg<RedirectFB>()
        if entry.HasValue then
          let rd = entry.Value
          let node = rd.Node
          if node.HasValue then
            let! node = RaftNode.FromFB node.Value
            return Redirect(node)
          else
            return!
              "Could not parse empty RaftNodeFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty RedirectFB body"
            |> ParseError
            |> Either.fail
      }

    | RaftMsgTypeFB.WelcomeFB -> either {
        let entry = msg.Msg<WelcomeFB>()
        if entry.HasValue then
          let wl = entry.Value
          let node = wl.Node
          if node.HasValue then
            let! node = RaftNode.FromFB node.Value
            return Welcome(node)
          else
            return!
              "Could not parse empty RaftNodeFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty WelcomeFB body"
            |> ParseError
            |> Either.fail
      }

    | RaftMsgTypeFB.ArrivederciFB ->
      Right Arrivederci

    | RaftMsgTypeFB.ErrorResponseFB -> either {
        let entry = msg.Msg<ErrorResponseFB>()
        if entry.HasValue then
          let rv = entry.Value
          let err = rv.Error
          if err.HasValue then
            let! error = IrisError.FromFB err.Value
            return ErrorResponse error
          else
            return!
              "Could not parse empty ErrorFB body"
              |> ParseError
              |> Either.fail
        else
          return!
            "Could not parse empty ErrorResponseFB body"
            |> ParseError
            |> Either.fail
      }

    | x ->
      sprintf "Could not parse unknown RaftMsgTypeFB: %A" x
      |> ParseError
      |> Either.fail

  //  _____     ____        _
  // |_   _|__ | __ ) _   _| |_ ___  ___
  //   | |/ _ \|  _ \| | | | __/ _ \/ __|
  //   | | (_) | |_) | |_| | ||  __/\__ \
  //   |_|\___/|____/ \__, |\__\___||___/
  //                  |___/

  member self.ToBytes () : byte array = Binary.buildBuffer self

  //  _____                    ____        _
  // |  ___| __ ___  _ __ ___ | __ ) _   _| |_ ___  ___
  // | |_ | '__/ _ \| '_ ` _ \|  _ \| | | | __/ _ \/ __|
  // |  _|| | | (_) | | | | | | |_) | |_| | ||  __/\__ \
  // |_|  |_|  \___/|_| |_| |_|____/ \__, |\__\___||___/
  //                                 |___/

  static member FromBytes (bytes: byte array) : Either<IrisError,RaftResponse> =
    let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
    RaftResponse.FromFB msg
