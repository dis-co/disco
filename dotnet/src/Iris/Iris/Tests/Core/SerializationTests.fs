namespace Iris.Tests

open Fuchu
open Fuchu.Test
open Iris.Core
open Iris.Raft
open Iris.Serialization.Raft
open System.Net
open FlatBuffers
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
      let node =
        { Node.create (Id.Create()) with
            HostName = "test-host"
            IpAddr   = IpAddress.Parse "192.168.2.10"
            Port     = 8080us }

      let vr : VoteRequest =
        { Term = 8UL
        ; LastLogIndex = 128UL
        ; LastLogTerm = 7UL
        ; Candidate = node }

      let msg   = RequestVote(Id.Create(), vr)
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

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

      let msg   = RequestVoteResponse(Id.Create(), vr)
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let node1 = Node.create (Id.Create())
      let node2 = Node.create (Id.Create())

      let changes = [| NodeRemoved node2 |]
      let nodes = [| node1; node2 |]

      let log =
        Some <| LogEntry(Id.Create(), 7UL, 1UL, DataSnapshot "cccc",
          Some <| LogEntry(Id.Create(), 6UL, 1UL, DataSnapshot "bbbb",
            Some <| Configuration(Id.Create(), 5UL, 1UL, [| node1 |],
              Some <| JointConsensus(Id.Create(), 4UL, 1UL, changes,
                Some <| Snapshot(Id.Create(), 3UL, 1UL, 2UL, 1UL, nodes, DataSnapshot "aaaa")))))

      let ae : AppendEntries =
        { Term = 8UL
        ; PrevLogIdx = 192UL
        ; PrevLogTerm = 87UL
        ; LeaderCommit = 182UL
        ; Entries = log }

      let msg   = AppendEntries(Id.Create(), ae)
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

      let msg   = AppendEntries(Id.Create(), { ae with Entries = None })
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

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

      let msg = AppendEntriesResponse(Id.Create(), response)
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      let node1 = [| Node.create (Id.Create()) |]

      let is : InstallSnapshot =
        { Term = 2134UL
        ; LeaderId = Id.Create()
        ; LastIndex = 242UL
        ; LastTerm = 124242UL
        ; Data = Snapshot(Id.Create(), 12UL, 3414UL, 241UL, 422UL, node1, DataSnapshot "hahahah")
        }

      let msg = InstallSnapshot(Id.Create(), is)
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    testCase "Validate HandShake Serialization" <| fun _ ->
      let msg = HandShake(Node.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    testCase "Validate HandWaive Serialization" <| fun _ ->
      let msg = HandWaive(Node.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  //  ____          _ _               _
  // |  _ \ ___  __| (_)_ __ ___  ___| |_
  // | |_) / _ \/ _` | | '__/ _ \/ __| __|
  // |  _ <  __/ (_| | | | |  __/ (__| |_
  // |_| \_\___|\__,_|_|_|  \___|\___|\__|

  let test_validate_redirect_serialization =
    testCase "Validate Redirect Serialization" <| fun _ ->
      let msg = Redirect(Node.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get

      expect "Should be structurally the same" msg id remsg

  // __        __   _
  // \ \      / /__| | ___ ___  _ __ ___   ___
  //  \ \ /\ / / _ \ |/ __/ _ \| '_ ` _ \ / _ \
  //   \ V  V /  __/ | (_| (_) | | | | | |  __/
  //    \_/\_/ \___|_|\___\___/|_| |_| |_|\___|

  let test_validate_welcome_serialization =
    testCase "Validate Welcome Serialization" <| fun _ ->
      let msg = Welcome(Node.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally the same" msg id remsg

  //     _              _               _               _
  //    / \   _ __ _ __(_)_   _____  __| | ___ _ __ ___(_)
  //   / _ \ | '__| '__| \ \ / / _ \/ _` |/ _ \ '__/ __| |
  //  / ___ \| |  | |  | |\ V /  __/ (_| |  __/ | | (__| |
  // /_/   \_\_|  |_|  |_| \_/ \___|\__,_|\___|_|  \___|_|

  let test_validate_arrivederci_serialization =
    testCase "Validate Arrivederci Serialization" <| fun _ ->
      let msg = Arrivederci
      let remsg = msg |> Binary.encode |> Binary.decode |> Option.get
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
                  let remsg = msg |> Binary.encode |> Binary.decode |> Option.get
                  expect "Should be structurally the same" msg id remsg)
                errors

  let mktags _ =
    let rand = new System.Random()
    [| for n in 0 .. rand.Next(2,8) do
        yield Id.Create() |> string |]

  let ioboxes _ =
    [| IOBox.Bang       (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 0UL; Value = true    }|])
    ; IOBox.Toggle     (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 0UL; Value = true    }|])
    ; IOBox.String     (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 0UL; Value = "one"   }|])
    ; IOBox.MultiLine  (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 0UL; Value = "two"   }|])
    ; IOBox.FileName   (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 0UL; Value = "three" }|])
    ; IOBox.Directory  (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 0UL; Value = "four"  }|])
    ; IOBox.Url        (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 0UL; Value = "five"  }|])
    ; IOBox.IP         (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 0UL; Value = "six"   }|])
    ; IOBox.Float      (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0UL; Value = 3.0    }|])
    ; IOBox.Double     (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0UL; Value = double 3.0 }|])
    ; IOBox.Bytes      (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0UL; Value = [| 2uy; 9uy |] }|])
    ; IOBox.Color      (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0UL; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
    ; IOBox.Color      (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0UL; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
    ; IOBox.Enum       (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0UL; Value = { Key = "one"; Value = "two" }}|])
    |]

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_validate_cue_serialization =
    testCase "Validate Cue Serialization" <| fun _ ->

      let cue : Cue = { Id = Id.Create(); Name = "Cue 1"; IOBoxes = ioboxes () }

      let recue = cue |> Binary.encode |> Binary.decode |> Option.get
      expect "should be same" cue id recue

      let recue = cue |> Json.encode |> Json.decode |> Option.get
      expect "should be same" cue id recue

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_validate_patch_serialization =
    testCase "Validate Patch Serialization" <| fun _ ->

      let patch : Patch = { Id = Id.Create(); Name = "Patch 1"; IOBoxes = ioboxes () }

      let repatch = patch |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally equivalent" patch id repatch

      let repatch = patch |> Json.encode |> Json.decode |> Option.get
      expect "Should be structurally equivalent" patch id repatch

  //  ____  _ _
  // / ___|| (_) ___ ___
  // \___ \| | |/ __/ _ \
  //  ___) | | | (_|  __/
  // |____/|_|_|\___\___|

  let test_validate_slice_serialization =
    testCase "Validate Slice Serialization" <| fun _ ->

      [| BoolSlice     { Index = 0UL; Value = true    }
      ; StringSlice   { Index = 0UL; Value = "hello" }
      ; IntSlice      { Index = 0UL; Value = 1234    }
      ; FloatSlice    { Index = 0UL; Value = 1234.0  }
      ; DoubleSlice   { Index = 0UL; Value = 1234.0  }
      ; ByteSlice     { Index = 0UL; Value = [| 0uy |] }
      ; EnumSlice     { Index = 0UL; Value = { Key = "one"; Value = "two" }}
      ; ColorSlice    { Index = 0UL; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }
      ; ColorSlice    { Index = 0UL; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }
      ; CompoundSlice { Index = 0UL; Value = ioboxes () } |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Option.get
          expect "Should be structurally equivalent" slice id reslice

          let reslice = slice |> Json.encode |> Json.decode |> Option.get
          expect "Should be structurally equivalent" slice id reslice)

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  let test_validate_iobox_serialization =
    testCase "Validate IOBox Serialization" <| fun _ ->
      let rand = new System.Random()

      let check iobox =
        iobox |> Binary.encode |> Binary.decode |> Option.get
        |> expect "Should be structurally equivalent" iobox id

        iobox |> Json.encode |> Json.decode |> Option.get
        |> expect "Should be structurally equivalent" iobox id

      Array.iter check (ioboxes ())

      // compound
      let compound = IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0UL; Value = ioboxes () }|])
      check compound

      // nested compound :)
      IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0UL; Value = [| compound |] }|])
      |> check

  //     _                _ _           _   _             _____                 _
  //    / \   _ __  _ __ | (_) ___ __ _| |_(_) ___  _ __ | ____|_   _____ _ __ | |_
  //   / _ \ | '_ \| '_ \| | |/ __/ _` | __| |/ _ \| '_ \|  _| \ \ / / _ \ '_ \| __|
  //  / ___ \| |_) | |_) | | | (_| (_| | |_| | (_) | | | | |___ \ V /  __/ | | | |_
  // /_/   \_\ .__/| .__/|_|_|\___\__,_|\__|_|\___/|_| |_|_____| \_/ \___|_| |_|\__|
  //         |_|   |_|

  let test_validate_application_event_serialization =
    testCase "Validate Cue Serialization" <| fun _ ->
      let rand = new System.Random()

      let mktags _ =
        [| for n in 0 .. rand.Next(2,8) do
            yield Id.Create() |> string |]

      let mkIOBox _ =
        let slice : StringSliceD = { Index = 0UL; Value = "hello" }
        IOBox.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

      [ AddCue    { Id = Id.Create(); Name = "Cue 1"; IOBoxes = ioboxes () }
      ; UpdateCue { Id = Id.Create(); Name = "Cue 2"; IOBoxes = ioboxes () }
      ; RemoveCue { Id = Id.Create(); Name = "Cue 2"; IOBoxes = ioboxes () }
      ; AddPatch    { Id = Id.Create(); Name = "Patch 1"; IOBoxes = ioboxes () }
      ; UpdatePatch { Id = Id.Create(); Name = "Patch 2"; IOBoxes = ioboxes () }
      ; RemovePatch { Id = Id.Create(); Name = "Patch 3"; IOBoxes = ioboxes () }
      ; AddIOBox    <| mkIOBox ()
      ; UpdateIOBox <| mkIOBox ()
      ; RemoveIOBox <| mkIOBox ()
      ; AddNode    <| Node.create (Id.Create())
      ; UpdateNode <| Node.create (Id.Create())
      ; RemoveNode <| Node.create (Id.Create())
      ; Command AppCommand.Undo
      ; LogMsg(Debug, "ohai")
      ]
      |> List.iter (fun cmd ->
                     let remsg = cmd |> Binary.encode |> Binary.decode |> Option.get
                     expect "Should be structurally the same" cmd id remsg

                     let remsg = cmd |> Json.encode |> Json.decode |> Option.get
                     expect "Should be structurally the same" cmd id remsg)

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_serialization =
    testCase "Validate correct StateMachine serialization" <| fun _ ->
      let snapshot = DataSnapshot "hello"
      let remsg = snapshot |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally the same" snapshot id remsg

      let rand = new System.Random()

      let mktags _ =
        [| for n in 0 .. rand.Next(2,8) do
            yield Id.Create() |> string |]

      let mkIOBox _ =
        let slice : StringSliceD = { Index = 0UL; Value = "hello" }
        IOBox.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

      [ AddCue    { Id = Id.Create(); Name = "Cue 1"; IOBoxes = ioboxes () }
      ; UpdateCue { Id = Id.Create(); Name = "Cue 2"; IOBoxes = ioboxes () }
      ; RemoveCue { Id = Id.Create(); Name = "Cue 3"; IOBoxes = ioboxes () }
      ; AddPatch    { Id = Id.Create(); Name = "Patch 1"; IOBoxes = ioboxes () }
      ; UpdatePatch { Id = Id.Create(); Name = "Patch 2"; IOBoxes = ioboxes () }
      ; RemovePatch { Id = Id.Create(); Name = "Patch 3"; IOBoxes = ioboxes () }
      ; AddIOBox    <| mkIOBox ()
      ; UpdateIOBox <| mkIOBox ()
      ; RemoveIOBox <| mkIOBox ()
      ; AddNode    <| Node.create (Id.Create())
      ; UpdateNode <| Node.create (Id.Create())
      ; RemoveNode <| Node.create (Id.Create())
      ; Command AppCommand.Redo
      ; LogMsg(Debug, "ohai")
      ]
      |> List.iter
        (fun cmd ->
          let command = AppEvent cmd
          let remsg = command |> Binary.encode |> Binary.decode
          expect "Should be structurally the same" command id (Option.get remsg))


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
        test_validate_handshake_serialization
        test_validate_handwaive_serialization
        test_validate_redirect_serialization
        test_validate_welcome_serialization
        test_validate_arrivederci_serialization
        test_validate_errorresponse_serialization
        test_validate_cue_serialization
        test_validate_patch_serialization
        test_validate_slice_serialization
        test_validate_iobox_serialization
        test_validate_application_event_serialization
        test_validate_state_machine_serialization
      ]
