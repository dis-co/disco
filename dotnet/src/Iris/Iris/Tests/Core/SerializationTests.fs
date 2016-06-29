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
        { HostName = "test-host"
        ; IpAddr = IPAddress.Parse "192.168.2.10"
        ; Port = 8080 }

      let node = Node.create 18u info

      let vr : VoteRequest =
        { Term = 8u
        ; LastLogIndex = 128u
        ; LastLogTerm = 7u
        ; Candidate = node }

      let msg   = RequestVote(18u, vr)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

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
        { Term = 8u
        ; Granted = false
        ; Reason = Some VoteTermMismatch }

      let msg   = RequestVoteResponse(18u, vr)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let node1 = Node.create 1u { HostName = "Hans";  IpAddr = IPAddress.Parse "192.168.1.20"; Port = 8080 }
      let node2 = Node.create 2u { HostName = "Klaus"; IpAddr = IPAddress.Parse "192.168.1.22"; Port = 8080 }

      let log =
        LogEntry(Guid.NewGuid(), 8u, 1u, Open "latest",
                 Some <| LogEntry(Guid.NewGuid(), 7u, 1u, Close "cccc",
                                  Some <| LogEntry(Guid.NewGuid(), 6u, 1u, AddClient "bbbb",
                                                   Some <| Configuration(Guid.NewGuid(), 5u, 1u, [| node1 |],
                                                                         Some <| JointConsensus(Guid.NewGuid(), 4u, 1u, [| NodeRemoved node2 |], [| node1; node2 |],
                                                                                                Some <| Snapshot(Guid.NewGuid(), 3u, 1u, 2u, 1u, [| node1; node2 |],  DataSnapshot "aaaa"))))))
      let ae : AppendEntries =
        { Term = 8u
        ; PrevLogIdx = 192u
        ; PrevLogTerm = 87u
        ; LeaderCommit = 182u
        ; Entries = Some log }

      let msg   = AppendEntries(18u, ae)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

      let msg   = AppendEntries(18u, { ae with Entries = None })
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

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
        { Term         = 38u
        ; Success      = true
        ; CurrentIndex = 1234u
        ; FirstIndex   = 8942u
        }

      let msg = AppendEntriesResponse(12341u, response)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      let node1 = Node.create 1u { HostName = "Hans"; IpAddr = IPAddress.Parse "123.23.21.1"; Port = 124 }

      let is : InstallSnapshot =
        { Term = 2134u
        ; LeaderId = 1230u
        ; LastIndex = 242u
        ; LastTerm = 124242u
        ; Data = Snapshot(Guid.NewGuid(), 12u, 3414u, 241u, 422u, [| node1 |], DataSnapshot "hahahah")
        }

      let msg = InstallSnapshot(2134u, is)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _   ____
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_|  _ \ ___  ___ _ __   ___  _ __   ___  ___
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __| |_) / _ \/ __| '_ \ / _ \| '_ \ / __|/ _ \
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_|  _ <  __/\__ \ |_) | (_) | | | |\__ \  __/
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|_| \_\___||___/ .__/ \___/|_| |_||___/\___|
  //                   |_|                                      |_|

  let test_validate_installsnapshot_response_serialization =
    testCase "Validate InstallSnapshot Response Serialization" <| fun _ ->
      let response : SnapshotResponse = { Term = 92381u }
      
      let msg = InstallSnapshotResponse(32423u, response)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    testCase "Validate HandShake Serialization" <| fun _ ->
      let info = { HostName = "horst"
                 ; IpAddr = IPAddress.Parse "127.0.0.1"
                 ; Port = 8080 }

      let msg = HandShake(Node.create 1u info)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    testCase "Validate HandWaive Serialization" <| fun _ ->
      let info = { HostName = "horst"
                 ; IpAddr = IPAddress.Parse "127.0.0.1"
                 ; Port = 8080 }

      let msg = HandWaive(Node.create 1u info)
      let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get

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
                  let remsg = msg.ToBytes() |> RaftMsg.FromBytes |> Option.get
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
