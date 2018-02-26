(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Service

// * Imports

open Argu
open FlatBuffers
open Disco.Raft
open Disco.Serialization
open Disco.Core
open Disco.Raft

// * RaftMsg module

[<RequireQualifiedAccess>]
module RaftMsg =

  // ** getValue

  let getValue (t : Offset<'a>) : int = t.Value

  // ** build

  let inline build< ^t when ^t : struct and ^t :> System.ValueType and ^t : (new : unit -> ^t) >
                builder
                tipe
                (offset: Offset< ^t >) =
    RaftMsgFB.StartRaftMsgFB(builder)
    RaftMsgFB.AddMsgType(builder, tipe)
    RaftMsgFB.AddMsg(builder, offset.Value)
    RaftMsgFB.EndRaftMsgFB(builder)

  // ** createAppendEntriesFB

  let createAppendEntriesFB (builder: FlatBufferBuilder) (nid: MemberId) (ar: AppendEntries) =
    let sid = RequestAppendEntriesFB.CreateMemberIdVector(builder,nid.ToByteArray())
    RequestAppendEntriesFB.CreateRequestAppendEntriesFB(builder, sid, ar.ToOffset builder)

  // ** createAppendResponseFB

  let createAppendResponseFB (builder: FlatBufferBuilder) (nid: MemberId) (ar: AppendResponse) =
    let sid = RespondAppendEntriesFB.CreateMemberIdVector(builder,nid.ToByteArray())
    RespondAppendEntriesFB.CreateRespondAppendEntriesFB(builder, sid, ar.ToOffset builder)

  // ** createRequestVoteFB

  let createRequestVoteFB (builder: FlatBufferBuilder) (nid: MemberId) (vr: VoteRequest) =
    let sid = RequestVoteFB.CreateMemberIdVector(builder,nid.ToByteArray())
    RequestVoteFB.CreateRequestVoteFB(builder, sid, vr.ToOffset builder)

  // ** createRequestVoteResponseFB

  let createRequestVoteResponseFB (builder: FlatBufferBuilder) (nid: MemberId) (vr: VoteResponse) =
    let sid = RespondVoteFB.CreateMemberIdVector(builder, nid.ToByteArray())
    RespondVoteFB.CreateRespondVoteFB(builder, sid, vr.ToOffset builder)

  // ** createInstallSnapshotFB

  let createInstallSnapshotFB (builder: FlatBufferBuilder) (nid: MemberId) (is: InstallSnapshot) =
    let sid = RequestInstallSnapshotFB.CreateMemberIdVector(builder,nid.ToByteArray())
    RequestInstallSnapshotFB.CreateRequestInstallSnapshotFB(builder, sid, is.ToOffset builder)

  // ** createSnapshotResponseFB

  let createSnapshotResponseFB (builder: FlatBufferBuilder) (nid: MemberId) (ar: AppendResponse) =
    let sid = RespondInstallSnapshotFB.CreateMemberIdVector(builder,nid.ToByteArray())
    RespondInstallSnapshotFB.CreateRespondInstallSnapshotFB(builder, sid, ar.ToOffset builder)

  // ** createAppendEntryFB

  let createAppendEntryFB (builder: FlatBufferBuilder) (sm: StateMachine) =
    let offset = sm.ToOffset(builder)
    RequestAppendEntryFB.CreateRequestAppendEntryFB(builder, offset)

  // ** createAppendEntryResponseFB

  let createAppendEntryResponseFB (builder: FlatBufferBuilder) (entry: EntryResponse) =
    let offset = entry.ToOffset(builder)
    RespondAppendEntryFB.CreateRespondAppendEntryFB(builder, offset)

// * RaftRequest

//  ____        __ _     ____                            _
// |  _ \ __ _ / _| |_  |  _ \ ___  __ _ _   _  ___  ___| |_
// | |_) / _` | |_| __| | |_) / _ \/ _` | | | |/ _ \/ __| __|
// |  _ < (_| |  _| |_  |  _ <  __/ (_| | |_| |  __/\__ \ |_
// |_| \_\__,_|_|  \__| |_| \_\___|\__, |\__,_|\___||___/\__|
//                                    |_|

type RaftRequest =
  | RequestVote     of sender:MemberId * req:VoteRequest
  | AppendEntries   of sender:MemberId * ae:AppendEntries
  | InstallSnapshot of sender:MemberId * is:InstallSnapshot
  | AppendEntry     of entry:StateMachine
  // | HandShake       of sender:RaftMember
  // | HandWaive       of sender:RaftMember

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) : Offset<RaftMsgFB> =
    match self with
    | RequestVote(nid, req) ->
      RaftMsg.createRequestVoteFB builder nid req
      |> RaftMsg.build builder RaftMsgTypeFB.RequestVoteFB

    | AppendEntries(nid, ae) ->
      RaftMsg.createAppendEntriesFB builder nid ae
      |> RaftMsg.build builder RaftMsgTypeFB.RequestAppendEntriesFB

    | InstallSnapshot(nid, is) ->
      RaftMsg.createInstallSnapshotFB builder nid is
      |> RaftMsg.build builder RaftMsgTypeFB.RequestInstallSnapshotFB

    | AppendEntry sm ->
      RaftMsg.createAppendEntryFB builder sm
      |> RaftMsg.build builder RaftMsgTypeFB.RequestAppendEntryFB

    // | HandShake mem ->
    //   HandShakeFB.CreateHandShakeFB(builder, mem.ToOffset builder)
    //   |> RaftMsg.build builder RaftMsgTypeFB.HandShakeFB

    // | HandWaive mem ->
    //   HandWaiveFB.CreateHandWaiveFB(builder, mem.ToOffset builder)
    //   |> RaftMsg.build builder RaftMsgTypeFB.HandWaiveFB


  // ** ToBytes

  member self.ToBytes () : byte array = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte array) : DiscoResult<RaftRequest> =
    let msg = RaftMsgFB.GetRootAsRaftMsgFB(new ByteBuffer(bytes))
    match msg.MsgType with
    | RaftMsgTypeFB.RequestVoteFB -> result {
        let entry = msg.Msg<RequestVoteFB>()
        if entry.HasValue then
          let rv = entry.Value
          let request = rv.Request
          if request.HasValue then
            let! id = Id.decodeMemberId rv
            let! request = VoteRequest.FromFB(request.Value)
            return RequestVote(id, request)
          else
            return!
              "Could not parse empty VoteRequestFB body"
              |> Error.asParseError "RaftRequest.FromBytes"
              |> Result.fail
        else
          return!
            "Could not parse empty RequestVoteFB body"
            |> Error.asParseError "RaftRequest.FromBytes"
            |> Result.fail
      }

    | RaftMsgTypeFB.RequestAppendEntriesFB -> result {
        let entry = msg.Msg<RequestAppendEntriesFB>()
        if entry.HasValue then
          let ae = entry.Value
          let request = ae.Request
          if request.HasValue then
            let! id = Id.decodeMemberId ae
            let! request = AppendEntries.FromFB request.Value
            return AppendEntries(id, request)
          else
            return!
              "Could not parse empty AppendEntriesFB body"
              |> Error.asParseError "RaftRequest.FromBytes"
              |> Result.fail
        else
          return!
            "Could not parse empty RequestAppendEntriesFB body"
            |> Error.asParseError "RaftRequest.FromBytes"
            |> Result.fail
      }

    | RaftMsgTypeFB.RequestInstallSnapshotFB -> result {
        let entry = msg.Msg<RequestInstallSnapshotFB>()
        if entry.HasValue then
          let is = entry.Value
          let request = is.Request
          if request.HasValue then
            let! id = Id.decodeMemberId is
            let! request = InstallSnapshot.FromFB request.Value
            return InstallSnapshot(id, request)
          else
            return!
              "Could not parse empty InstallSnapshotFB body"
              |> Error.asParseError "RaftRequest.FromBytes"
              |> Result.fail
        else
          return!
            "Could not parse empty RequestInstallSnapshotFB body"
            |> Error.asParseError "RaftRequest.FromBytes"
            |> Result.fail
      }

    | RaftMsgTypeFB.RequestAppendEntryFB -> result {
        let entry = msg.Msg<RequestAppendEntryFB>()
        if entry.HasValue then
          let is = entry.Value
          let request = is.Request
          if request.HasValue then
            let! sm = StateMachine.FromFB request.Value
            return AppendEntry(sm)
          else
            return!
              "Could not parse empty AppendEntryFB body"
              |> Error.asParseError "RaftRequest.FromBytes"
              |> Result.fail
        else
          return!
            "Could not parse empty RequestAppendEntryFB body"
            |> Error.asParseError "RaftRequest.FromBytes"
            |> Result.fail
      }

    // | RaftMsgTypeFB.HandShakeFB -> result {
    //     let entry = msg.Msg<HandShakeFB>()
    //     if entry.HasValue then
    //       let hs = entry.Value
    //       let mem = hs.Member
    //       if mem.HasValue then
    //         let! mem = RaftMember.FromFB mem.Value
    //         return HandShake(mem)
    //       else
    //         return!
    //           "Could not parse empty RaftMemberFB body"
    //           |> Error.asParseError "RaftRequest.FromBytes"
    //           |> Result.fail
    //     else
    //       return!
    //         "Could not parse empty HandShakeFB body"
    //         |> Error.asParseError "RaftRequest.FromBytes"
    //         |> Result.fail
    //   }

    // | RaftMsgTypeFB.HandWaiveFB -> result {
    //     let entry = msg.Msg<HandWaiveFB>()
    //     if entry.HasValue then
    //       let hw = entry.Value
    //       let mem = hw.Member
    //       if mem.HasValue then
    //         let! mem = RaftMember.FromFB mem.Value
    //         return HandWaive(mem)
    //       else
    //         return!
    //           "Could not parse empty RaftMemberFB body"
    //           |> Error.asParseError "RaftRequest.FromBytes"
    //           |> Result.fail
    //     else
    //       return!
    //         "Could not parse empty HandShakeFB body"
    //         |> Error.asParseError "RaftRequest.FromBytes"
    //         |> Result.fail
    //   }

    | x ->
      sprintf "Could not parse unknown RaftMsgTypeFB: %A" x
      |> Error.asParseError "RaftRequest.FromBytes"
      |> Result.fail

// * RaftResponse

//  ____        __ _     ____
// |  _ \ __ _ / _| |_  |  _ \ ___  ___ _ __   ___  _ __  ___  ___
// | |_) / _` | |_| __| | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
// |  _ < (_| |  _| |_  |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
// |_| \_\__,_|_|  \__| |_| \_\___||___/ .__/ \___/|_| |_|___/\___|
//                                     |_|

type RaftResponse =
  | RequestVoteResponse     of sender:MemberId * vote:VoteResponse
  | AppendEntriesResponse   of sender:MemberId * ar:AppendResponse
  | InstallSnapshotResponse of sender:MemberId * ar:AppendResponse
  | ErrorResponse           of sender:MemberId * error:DiscoError
  | AppendEntryResponse     of response:EntryResponse
  | Redirect                of leader:RaftMember
  // | Welcome                 of leader:RaftMember
  // | Arrivederci

  // ** ToOffset

  member self.ToOffset(builder: FlatBufferBuilder) =
    match self with
    | RequestVoteResponse(nid, resp) ->
      RaftMsg.createRequestVoteResponseFB builder nid resp
      |> RaftMsg.build builder RaftMsgTypeFB.RespondVoteFB

    | AppendEntriesResponse(nid, ar) ->
      RaftMsg.createAppendResponseFB builder nid ar
      |> RaftMsg.build builder RaftMsgTypeFB.RespondAppendEntriesFB

    | InstallSnapshotResponse(nid, ir) ->
      RaftMsg.createSnapshotResponseFB builder nid ir
      |> RaftMsg.build builder RaftMsgTypeFB.RespondInstallSnapshotFB

    | AppendEntryResponse(entry) ->
      RaftMsg.createAppendEntryResponseFB builder entry
      |> RaftMsg.build builder RaftMsgTypeFB.RespondAppendEntryFB

    | ErrorResponse (id, err) ->
      let nid = ErrorResponseFB.CreateMemberIdVector(builder,id.ToByteArray())
      ErrorResponseFB.CreateErrorResponseFB(builder, nid, err.ToOffset builder)
      |> RaftMsg.build builder RaftMsgTypeFB.ErrorResponseFB

    | Redirect mem ->
      RedirectFB.CreateRedirectFB(builder, mem.ToOffset builder)
      |> RaftMsg.build builder RaftMsgTypeFB.RedirectFB

    // | Welcome mem ->
    //   WelcomeFB.CreateWelcomeFB(builder, mem.ToOffset builder)
    //   |> RaftMsg.build builder RaftMsgTypeFB.WelcomeFB

    // | Arrivederci ->
    //   ArrivederciFB.StartArrivederciFB(builder)
    //   ArrivederciFB.EndArrivederciFB(builder)
    //   |> RaftMsg.build builder RaftMsgTypeFB.ArrivederciFB

  // ** FromFB

  static member FromFB(msg: RaftMsgFB) : DiscoResult<RaftResponse> =
    match msg.MsgType with
    | RaftMsgTypeFB.RespondVoteFB -> result {
        let entry = msg.Msg<RespondVoteFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! id = Id.decodeMemberId fb
            let! parsed = VoteResponse.FromFB response.Value
            return RequestVoteResponse(id, parsed)
          else
            return!
              "Could not parse empty VoteResponseFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty RespondVoteFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    | RaftMsgTypeFB.RespondAppendEntriesFB -> result {
        let entry = msg.Msg<RespondAppendEntriesFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! id = Id.decodeMemberId fb
            let! response = AppendResponse.FromFB response.Value
            return AppendEntriesResponse(id, response)
          else
            return!
              "Could not parse empty AppendResponseFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty RespodnAppendEntriesFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    | RaftMsgTypeFB.RespondInstallSnapshotFB -> result {
        let entry = msg.Msg<RespondInstallSnapshotFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! id = Id.decodeMemberId fb
            let! response = AppendResponse.FromFB response.Value
            return InstallSnapshotResponse(id, response)
          else
            return!
              "Could not parse empty AppendResponseFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty RespondInstallSnapshotFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    | RaftMsgTypeFB.RespondAppendEntryFB -> result {
        let entry = msg.Msg<RespondAppendEntryFB>()
        if entry.HasValue then
          let fb = entry.Value
          let response = fb.Response
          if response.HasValue then
            let! response = EntryResponse.FromFB response.Value
            return AppendEntryResponse(response)
          else
            return!
              "Could not parse empty AppendResponseFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty RespondInstallSnapshotFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    | RaftMsgTypeFB.ErrorResponseFB -> result {
        let entry = msg.Msg<ErrorResponseFB>()
        if entry.HasValue then
          let rv = entry.Value
          let err = rv.Error
          if err.HasValue then
            let! id = Id.decodeMemberId rv
            let! error = DiscoError.FromFB err.Value
            return ErrorResponse (id, error)
          else
            return!
              "Could not parse empty ErrorFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty ErrorResponseFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    | RaftMsgTypeFB.RedirectFB -> result {
        let entry = msg.Msg<RedirectFB>()
        if entry.HasValue then
          let rd = entry.Value
          let mem = rd.Member
          if mem.HasValue then
            let! mem = RaftMember.FromFB mem.Value
            return Redirect(mem)
          else
            return!
              "Could not parse empty RaftMemberFB body"
              |> Error.asParseError "RaftResponse.FromFB"
              |> Result.fail
        else
          return!
            "Could not parse empty RedirectFB body"
            |> Error.asParseError "RaftResponse.FromFB"
            |> Result.fail
      }

    // | RaftMsgTypeFB.WelcomeFB -> result {
    //     let entry = msg.Msg<WelcomeFB>()
    //     if entry.HasValue then
    //       let wl = entry.Value
    //       let mem = wl.Member
    //       if mem.HasValue then
    //         let! mem = RaftMember.FromFB mem.Value
    //         return Welcome(mem)
    //       else
    //         return!
    //           "Could not parse empty RaftMemberFB body"
    //           |> Error.asParseError "RaftResponse.FromFB"
    //           |> Result.fail
    //     else
    //       return!
    //         "Could not parse empty WelcomeFB body"
    //         |> Error.asParseError "RaftResponse.FromFB"
    //         |> Result.fail
    //   }

    // | RaftMsgTypeFB.ArrivederciFB ->
    //   Ok Arrivederci

    | x ->
      sprintf "Could not parse unknown RaftMsgTypeFB: %A" x
      |> Error.asParseError "RaftResponse.FromFB"
      |> Result.fail

  // ** ToBytes

  member self.ToBytes () : byte array = Binary.buildBuffer self

  // ** FromBytes

  static member FromBytes (bytes: byte array) : DiscoResult<RaftResponse> =
    let msg = RaftMsgFB.GetRootAsRaftMsgFB(ByteBuffer(bytes))
    RaftResponse.FromFB msg
