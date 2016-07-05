namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open Pallet.Core
open System
open System.Net
open FSharpx.Functional

[<AutoOpen>]
module SerializationTests =

  //  ____                            _ __     __    _
  // |  _ \ ___  __ _ _   _  ___  ___| |\ \   / /__ | |_ ___
  // | |_) / _ \/ _` | | | |/ _ \/ __| __\ \ / / _ \| __/ _ \
  // |  _ <  __/ (_| | |_| |  __/\__ \ |_ \ V / (_) | ||  __/
  // |_| \_\___|\__, |\__,_|\___||___/\__| \_/ \___/ \__\___|
  //               |_|

  let test_validate_requestvote_serialization =
    testCase "Validate RequestVote Serialization" <| fun _ ->
      let info =
        { MemberId = Guid.Create ()
        ; HostName = "test-host"
        ; IpAddr = IpAddress.Parse "192.168.2.10"
        ; Port = 8080 }

      let node = Node.create (RaftId.Create()) info

      let vr : VoteRequest =
        { Term = 8UL
        ; LastLogIndex = 128UL
        ; LastLogTerm = 7UL
        ; Candidate = node }

      let msg   = RequestVote(RaftId.Create(), vr)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  // __     __    _       ____
  // \ \   / /__ | |_ ___|  _ \ ___  ___ _ __   ___  _ __  ___  ___
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  //   \ V / (_) | ||  __/  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  //    \_/ \___/ \__\___|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                                    |_|

  let test_validate_requestvote_response_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let vr : VoteResponse =
        { Term = 8UL
        ; Granted = false
        ; Reason = Some VoteTermMismatch }

      let msg   = RequestVoteResponse(RaftId.Create(), vr)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let info1 =
        { MemberId = Guid.Create()
        ; HostName = "Hans"
        ; IpAddr = IpAddress.Parse "192.168.1.20"
        ; Port = 8080 }

      let info2 =
        { MemberId = Guid.Create()
        ; HostName = "Klaus"
        ; IpAddr = IpAddress.Parse "192.168.1.22"
        ; Port = 8080 }

      let node1 = Node.create (RaftId.Create()) info1
      let node2 = Node.create (RaftId.Create()) info2

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        Some <| LogEntry(RaftId.Create(), 7UL, 1UL, Close "cccc",
          Some <| LogEntry(RaftId.Create(), 6UL, 1UL, AddClient "bbbb",
            Some <| Configuration(RaftId.Create(), 5UL, 1UL, [| node1 |],
              Some <| JointConsensus(RaftId.Create(), 4UL, 1UL, changes, nodes,
                Some <| Snapshot(RaftId.Create(), 3UL, 1UL, 2UL, 1UL, nodes, DataSnapshot "aaaa")))))

      let ae : AppendEntries =
        { Term = 8UL
        ; PrevLogIdx = 192UL
        ; PrevLogTerm = 87UL
        ; LeaderCommit = 182UL
        ; Entries = log }

      let msg   = AppendEntries(RaftId.Create(), ae)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

      let msg   = AppendEntries(RaftId.Create(), { ae with Entries = None })
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //     _                               _ ____
  //    / \   _ __  _ __   ___ _ __   __| |  _ \ ___  ___ _ __   ___  _ __  ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` | |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  //  / ___ \| |_) | |_) |  __/ | | | (_| |  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //         |_|   |_|                                   |_|

  let test_validate_appendentries_response_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let response : AppendResponse =
        { Term         = 38UL
        ; Success      = true
        ; CurrentIndex = 1234UL
        ; FirstIndex   = 8942UL
        }

      let msg = AppendEntriesResponse(RaftId.Create(), response)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      let info =
        { MemberId = Guid.Create()
        ; HostName = "Hans"
        ; IpAddr = IpAddress.Parse "123.23.21.1"
        ; Port = 124 }

      let node1 = [| Node.create (RaftId.Create()) info |]

      let is : InstallSnapshot =
        { Term = 2134UL
        ; LeaderId = RaftId.Create()
        ; LastIndex = 242UL
        ; LastTerm = 124242UL
        ; Data = Snapshot(RaftId.Create(), 12UL, 3414UL, 241UL, 422UL, node1, DataSnapshot "hahahah")
        }

      let msg = InstallSnapshot(RaftId.Create(), is)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _   ____
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_|  _ \ ___  ___ _ __   ___  _ __   ___  ___
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __| |_) / _ \/ __| '_ \ / _ \| '_ \ / __|/ _ \
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_|  _ <  __/\__ \ |_) | (_) | | | |\__ \  __/
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|_| \_\___||___/ .__/ \___/|_| |_||___/\___|
  //                   |_|                                      |_|

  let test_validate_installsnapshot_response_serialization =
    testCase "Validate InstallSnapshot Response Serialization" <| fun _ ->
      let response : SnapshotResponse = { Term = 92381UL }

      let msg = InstallSnapshotResponse(RaftId.Create(), response)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    testCase "Validate HandShake Serialization" <| fun _ ->
      let info =
        { MemberId = Guid.Create()
        ; HostName = "horst"
        ; IpAddr = IpAddress.Parse "127.0.0.1"
        ; Port = 8080 }

      let msg = HandShake(Node.create (RaftId.Create()) info)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    testCase "Validate HandWaive Serialization" <| fun _ ->
      let info =
        { MemberId = Guid.Create()
        ; HostName = "horst"
        ; IpAddr = IpAddress.Parse "127.0.0.1"
        ; Port = 8080 }

      let msg = HandWaive(Node.create (RaftId.Create()) info)
      let remsg = msg |> encode |> decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _____
  // | ____|_ __ _ __ ___  _ __
  // |  _| | '__| '__/ _ \| '__|
  // | |___| |  | | | (_) | |
  // |_____|_|  |_|  \___/|_|

  let test_validate_errorresponse_serialization =
    testCase "Validate ErrorResponse Serialization" <| fun _ ->

      let errors = [
          AlreadyVoted
          AppendEntryFailed
          CandidateUnknown
          EntryInvalidated
          InvalidCurrentIndex
          InvalidLastLog
          InvalidLastLogTerm
          InvalidTerm
          LogFormatError
          LogIncomplete
          NoError
          NoNode
          NotCandidate
          NotLeader
          NotVotingState
          ResponseTimeout
          SnapshotFormatError
          StaleResponse
          UnexpectedVotingChange
          VoteTermMismatch
          OtherError "whatever"
        ]
      List.iter (fun err ->
                  let msg = ErrorResponse(err)
                  let remsg = msg |> encode |> decode |> Option.get
                  expect "Should be structurally the same" msg id remsg)
                errors

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let serializationTests =
    testList "Serialization Tests" [
        test_validate_requestvote_serialization
        test_validate_requestvote_response_serialization
        test_validate_appendentries_serialization
        test_validate_appendentries_response_serialization
        test_validate_installsnapshot_serialization
        test_validate_installsnapshot_response_serialization
        test_validate_handshake_serialization
        test_validate_handwaive_serialization
        test_validate_errorresponse_serialization
      ]
