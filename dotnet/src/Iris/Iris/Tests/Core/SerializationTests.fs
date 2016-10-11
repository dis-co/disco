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
        { Term = 8u
        ; LastLogIndex = 128u
        ; LastLogTerm = 7u
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
        { Term = 8u
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
        Some <| LogEntry(Id.Create(), 7u, 1u, DataSnapshot State.Empty,
          Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot State.Empty,
            Some <| Configuration(Id.Create(), 5u, 1u, [| node1 |],
              Some <| JointConsensus(Id.Create(), 4u, 1u, changes,
                Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, nodes, DataSnapshot State.Empty)))))

      let ae : AppendEntries =
        { Term = 8u
        ; PrevLogIdx = 192u
        ; PrevLogTerm = 87u
        ; LeaderCommit = 182u
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
        { Term         = 38u
        ; Success      = true
        ; CurrentIndex = 1234u
        ; FirstIndex   = 8942u
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
        { Term = 2134u
        ; LeaderId = Id.Create()
        ; LastIndex = 242u
        ; LastTerm = 124242u
        ; Data = Snapshot(Id.Create(), 12u, 3414u, 241u, 422u, node1, DataSnapshot State.Empty)
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
          OK
          BranchNotFound        "bla"
          BranchDetailsNotFound "haha"
          RepositoryNotFound    "haha"
          RepositoryInitFailed  "haha"
          CommitError           "haha"
          GitError              "haha"
          ProjectNotFound       "aklsdfl"
          ProjectPathError
          ProjectSaveError      "lskdfj"
          ProjectParseError     "lskdfj"
          MissingNodeId
          MissingNode           "lak"
          DatabaseCreateError   "oiwe"
          DatabaseNotFound      "lksjfolsk"
          MetaDataNotFound
          MissingStartupDir
          CliParseError
          AssetSaveError        "lskd"
          AssetDeleteError      "lskd"
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
          Other "whatever"
        ]
      List.iter (fun err ->
                  let msg = ErrorResponse(err)
                  let remsg = msg |> Binary.encode |> Binary.decode |> Option.get
                  expect "Should be structurally the same" msg id remsg)
                errors

  //   ____                 ____        _       _____
  //  / ___|___  _ __ ___  |  _ \  __ _| |_ __ |_   _|   _ _ __   ___  ___
  // | |   / _ \| '__/ _ \ | | | |/ _` | __/ _` || || | | | '_ \ / _ \/ __|
  // | |__| (_) | | |  __/ | |_| | (_| | || (_| || || |_| | |_) |  __/\__ \
  //  \____\___/|_|  \___| |____/ \__,_|\__\__,_||_| \__, | .__/ \___||___/
  //                                                 |___/|_|

  let rand = new System.Random()

  let mktags _ =
    [| for n in 0 .. rand.Next(2,8) do
        yield Id.Create() |> string |]

  let ioboxes _ =
    [| IOBox.Bang       (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
    ; IOBox.Toggle     (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
    ; IOBox.String     (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 0u; Value = "one"   }|])
    ; IOBox.MultiLine  (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 0u; Value = "two"   }|])
    ; IOBox.FileName   (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 0u; Value = "three" }|])
    ; IOBox.Directory  (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 0u; Value = "four"  }|])
    ; IOBox.Url        (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 0u; Value = "five"  }|])
    ; IOBox.IP         (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 0u; Value = "six"   }|])
    ; IOBox.Float      (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0u; Value = 3.0    }|])
    ; IOBox.Double     (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0u; Value = double 3.0 }|])
    ; IOBox.Bytes      (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0u; Value = [| 2uy; 9uy |] }|])
    ; IOBox.Color      (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
    ; IOBox.Color      (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
    ; IOBox.Enum       (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
    |]

  let mkIOBox _ =
    let slice : StringSliceD = { Index = 0u; Value = "hello" }
    IOBox.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = "Cue 1"; IOBoxes = ioboxes () }

  let mkPatch _ : Patch =
    let ioboxes = ioboxes () |> Array.map (fun b -> (b.Id,b)) |> Map.ofArray
    { Id = Id.Create(); Name = "Patch 3"; IOBoxes = ioboxes }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = "Patch 3"; Cues = [| mkCue (); mkCue () |] }

  let mkUser _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; FirstName = "Karsten"
    ; LastName = "Gebbert"
    ; Email = "k@ioctl.it"
    ; Joined = System.DateTime.Now
    ; Created = System.DateTime.Now
    }

  let mkNode _ = Id.Create() |> Node.create

  let mkSession _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; IpAddress = IPv4Address "127.0.0.1"
    ; UserAgent = "Oh my goodness"
    }

  let mkState _ =
    { Patches  = mkPatch   () |> fun (patch: Patch) -> Map.ofList [ (patch.Id, patch) ]
    ; IOBoxes  = ioboxes   () |> (fun (boxes: IOBox array) -> Array.map (fun (box: IOBox) -> (box.Id,box)) boxes) |> Map.ofArray
    ; Cues     = mkCue     () |> fun (cue: Cue) -> Map.ofList [ (cue.Id, cue) ]
    ; CueLists = mkCueList () |> fun (cuelist: CueList) -> Map.ofList [ (cuelist.Id, cuelist) ]
    ; Nodes    = mkNode    () |> fun (node: RaftNode) -> Map.ofList [ (node.Id, node) ]
    ; Sessions = mkSession () |> fun (session: Session) -> Map.ofList [ (session.Id, session) ]
    ; Users    = mkUser    () |> fun (user: User) -> Map.ofList [ (user.Id, user) ]
    }

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_validate_cue_binary_serialization =
    testCase "Validate Cue Binary Serialization" <| fun _ ->
      let cue : Cue = mkCue ()

      let recue = cue |> Binary.encode |> Binary.decode |> Option.get
      expect "should be same" cue id recue

  let test_validate_cue_yaml_serialization =
    testCase "Validate Cue Yaml Serialization" <| fun _ ->
      let cue : Cue = mkCue ()

      let recue = cue |> Yaml.encode |> Yaml.decode |> Option.get
      expect "should be same" cue id recue

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_validate_cuelist_binary_serialization =
    testCase "Validate CueList Binary Serialization" <| fun _ ->
      let cuelist : CueList = mkCueList ()

      let recuelist = cuelist |> Binary.encode |> Binary.decode |> Option.get
      expect "should be same" cuelist id recuelist

  let test_validate_cuelist_yaml_serialization =
    testCase "Validate CueList Yaml Serialization" <| fun _ ->
      let cuelist : CueList = mkCueList ()

      let recuelist = cuelist |> Yaml.encode |> Yaml.decode |> Option.get
      expect "should be same" cuelist id recuelist

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_validate_patch_serialization =
    testCase "Validate Patch Serialization" <| fun _ ->
      let patch : Patch = mkPatch ()

      let repatch = patch |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally equivalent" patch id repatch

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_validate_session_serialization =
    testCase "Validate Session Serialization" <| fun _ ->
      let session : Session = mkSession ()

      let resession = session |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally equivalent" session id resession

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_validate_user_binary_serialization =
    testCase "Validate User Binary Serialization" <| fun _ ->
      let user : User = mkUser ()

      let reuser = user |> Binary.encode |> Binary.decode |> Option.get
      expect "Should be structurally equivalent" user id reuser

  let test_validate_user_yaml_serialization =
    testCase "Validate User Yaml Serialization" <| fun _ ->
      let user : User = mkUser ()

      let reuser = user |> Yaml.encode |> Yaml.decode |> Option.get
      expect "Should be structurally equivalent" user id reuser

  //  ____  _ _
  // / ___|| (_) ___ ___
  // \___ \| | |/ __/ _ \
  //  ___) | | | (_|  __/
  // |____/|_|_|\___\___|

  let test_validate_slice_binary_serialization =
    testCase "Validate Slice Binary Serialization" <| fun _ ->

      [| BoolSlice     { Index = 0u; Value = true    }
      ; StringSlice   { Index = 0u; Value = "hello" }
      ; IntSlice      { Index = 0u; Value = 1234    }
      ; FloatSlice    { Index = 0u; Value = 1234.0  }
      ; DoubleSlice   { Index = 0u; Value = 1234.0  }
      ; ByteSlice     { Index = 0u; Value = [| 0uy |] }
      ; EnumSlice     { Index = 0u; Value = { Key = "one"; Value = "two" }}
      ; ColorSlice    { Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }
      ; ColorSlice    { Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }
      ; CompoundSlice { Index = 0u; Value = ioboxes () } |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Option.get
          expect "Should be structurally equivalent" slice id reslice)

  let test_validate_slice_yaml_serialization =
    testCase "Validate Slice Yaml Serialization" <| fun _ ->

      [| BoolSlice     { Index = 0u; Value = true    }
      ; StringSlice   { Index = 0u; Value = "hello" }
      ; IntSlice      { Index = 0u; Value = 1234    }
      ; FloatSlice    { Index = 0u; Value = 1234.0  }
      ; DoubleSlice   { Index = 0u; Value = 1234.0  }
      ; ByteSlice     { Index = 0u; Value = [| 0uy; 4uy; 9uy; 233uy |] }
      ; EnumSlice     { Index = 0u; Value = { Key = "one"; Value = "two" }}
      ; ColorSlice    { Index = 0u; Value = RGBA { Red = 255uy; Blue = 2uy; Green = 255uy; Alpha = 33uy } }
      ; ColorSlice    { Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 25uy; Lightness = 255uy; Alpha = 55uy } }
      ; CompoundSlice { Index = 0u; Value = ioboxes () }
      |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Yaml.encode |> Yaml.decode |> Option.get
          expect "Should be structurally equivalent" slice id reslice)

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  let test_validate_iobox_binary_serialization =
    testCase "Validate IOBox Binary Serialization" <| fun _ ->
      let check iobox =
        iobox |> Binary.encode |> Binary.decode |> Option.get
        |> expect "Should be structurally equivalent" iobox id

      Array.iter check (ioboxes ())

      // compound
      let compound = IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = ioboxes () }|])
      check compound

      // nested compound :)
      IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  let test_validate_iobox_yaml_serialization =
    testCase "Validate IOBox Yaml Serialization" <| fun _ ->
      let check iobox =
        iobox |> Yaml.encode |> Yaml.decode |> Option.get
        |> expect "Should be structurally equivalent" iobox id

      Array.iter check (ioboxes ())

      // compound
      let compound = IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = ioboxes () }|])
      check compound

      // nested compound :)
      IOBox.CompoundBox(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_serialization =
    testCase "Validate State Serialization" <| fun _ ->
      let state : State = mkState ()

      state |> Binary.encode |> Binary.decode |> Option.get
      |> expect "Should be structurally equivalent" state id

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_serialization =
    testCase "Validate StateMachine Serialization" <| fun _ ->
      [ AddCue        <| mkCue ()
      ; UpdateCue     <| mkCue ()
      ; RemoveCue     <| mkCue ()
      ; AddCueList    <| mkCueList ()
      ; UpdateCueList <| mkCueList ()
      ; RemoveCueList <| mkCueList ()
      ; AddSession    <| mkSession ()
      ; UpdateSession <| mkSession ()
      ; RemoveSession <| mkSession ()
      ; AddUser       <| mkUser ()
      ; UpdateUser    <| mkUser ()
      ; RemoveUser    <| mkUser ()
      ; AddPatch      <| mkPatch ()
      ; UpdatePatch   <| mkPatch ()
      ; RemovePatch   <| mkPatch ()
      ; AddIOBox      <| mkIOBox ()
      ; UpdateIOBox   <| mkIOBox ()
      ; RemoveIOBox   <| mkIOBox ()
      ; AddNode       <| Node.create (Id.Create())
      ; UpdateNode    <| Node.create (Id.Create())
      ; RemoveNode    <| Node.create (Id.Create())
      ; DataSnapshot  <| mkState ()
      ; Command AppCommand.Undo
      ; LogMsg(Debug, "ohai")
      ]
      |> List.iter (fun cmd ->
                     let remsg = cmd |> Binary.encode |> Binary.decode |> Option.get
                     expect "Should be structurally the same" cmd id remsg)

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
        test_validate_cue_binary_serialization
        test_validate_cue_yaml_serialization
        test_validate_cuelist_binary_serialization
        test_validate_cuelist_yaml_serialization
        test_validate_patch_serialization
        test_validate_session_serialization
        test_validate_user_binary_serialization
        test_validate_user_yaml_serialization
        test_validate_slice_binary_serialization
        test_validate_slice_yaml_serialization
        test_validate_iobox_binary_serialization
        test_validate_iobox_yaml_serialization
        test_validate_state_serialization
        test_validate_state_machine_serialization
      ]
