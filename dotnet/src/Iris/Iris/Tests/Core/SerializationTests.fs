namespace Iris.Tests

open Expecto
open Iris.Core
open Iris.Raft
open Iris.Service
open Iris.Serialization.Raft
open Iris.Service.Utilities
open Iris.Service.Persistence
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
      let mem =
        { Member.create (Id.Create()) with
            HostName = "test-host"
            IpAddr   = IpAddress.Parse "192.168.2.10"
            Port     = 8080us }

      let vr : VoteRequest =
        { Term = 8u
        ; LastLogIndex = 128u
        ; LastLogTerm = 7u
        ; Candidate = mem }

      let msg   = RequestVote(Id.Create(), vr)
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

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
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let mem1 = Member.create (Id.Create())
      let mem2 = Member.create (Id.Create())

      let changes = [| MemberRemoved mem2 |]
      let mems = [| mem1; mem2 |]

      let log =
        Some <| LogEntry(Id.Create(), 7u, 1u, DataSnapshot State.Empty,
          Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot State.Empty,
            Some <| Configuration(Id.Create(), 5u, 1u, [| mem1 |],
              Some <| JointConsensus(Id.Create(), 4u, 1u, changes,
                Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, mems, DataSnapshot State.Empty)))))

      let ae : AppendEntries =
        { Term = 8u
        ; PrevLogIdx = 192u
        ; PrevLogTerm = 87u
        ; LeaderCommit = 182u
        ; Entries = log }

      let msg   = AppendEntries(Id.Create(), ae)
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

      let msg   = AppendEntries(Id.Create(), { ae with Entries = None })
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

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
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      let mem1 = [| Member.create (Id.Create()) |]

      let is : InstallSnapshot =
        { Term = 2134u
        ; LeaderId = Id.Create()
        ; LastIndex = 242u
        ; LastTerm = 124242u
        ; Data = Snapshot(Id.Create(), 12u, 3414u, 241u, 422u, mem1, DataSnapshot State.Empty)
        }

      let msg = InstallSnapshot(Id.Create(), is)
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    testCase "Validate HandShake Serialization" <| fun _ ->
      let msg = HandShake(Member.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    testCase "Validate HandWaive Serialization" <| fun _ ->
      let msg = HandWaive(Member.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  //  ____          _ _               _
  // |  _ \ ___  __| (_)_ __ ___  ___| |_
  // | |_) / _ \/ _` | | '__/ _ \/ __| __|
  // |  _ <  __/ (_| | | | |  __/ (__| |_
  // |_| \_\___|\__,_|_|_|  \___|\___|\__|

  let test_validate_redirect_serialization =
    testCase "Validate Redirect Serialization" <| fun _ ->
      let msg = Redirect(Member.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

      expect "Should be structurally the same" msg id remsg

  // __        __   _
  // \ \      / /__| | ___ ___  _ __ ___   ___
  //  \ \ /\ / / _ \ |/ __/ _ \| '_ ` _ \ / _ \
  //   \ V  V /  __/ | (_| (_) | | | | | |  __/
  //    \_/\_/ \___|_|\___\___/|_| |_| |_|\___|

  let test_validate_welcome_serialization =
    testCase "Validate Welcome Serialization" <| fun _ ->
      let msg = Welcome(Member.create (Id.Create()))
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get
      expect "Should be structurally the same" msg id remsg

  //     _              _               _               _
  //    / \   _ __ _ __(_)_   _____  __| | ___ _ __ ___(_)
  //   / _ \ | '__| '__| \ \ / / _ \/ _` |/ _ \ '__/ __| |
  //  / ___ \| |  | |  | |\ V /  __/ (_| |  __/ | | (__| |
  // /_/   \_\_|  |_|  |_| \_/ \___|\__,_|\___|_|  \___|_|

  let test_validate_arrivederci_serialization =
    testCase "Validate Arrivederci Serialization" <| fun _ ->
      let msg = Arrivederci
      let remsg = msg |> Binary.encode |> Binary.decode |> Either.get
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
          ProjectInitError      "oiwe"
          MetaDataNotFound
          MissingStartupDir
          ParseError            "lah"
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
                  let remsg = msg |> Binary.encode |> Binary.decode |> Either.get
                  expect "Should be structurally the same" msg id remsg)
                errors

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  let test_save_restore_raft_value_correctly =
    testCase "save/restore raft value correctly" <| fun _ ->
      let machine = MachineConfig.create ()

      let self =
        machine.MachineId
        |> Member.create

      let mem1 =
        { Member.create (Id.Create()) with
            HostName = "Hans"
            IpAddr = IpAddress.Parse "192.168.1.20"
            Port   = 8080us }

      let mem2 =
        { Member.create (Id.Create()) with
            HostName = "Klaus"
            IpAddr = IpAddress.Parse "192.168.1.22"
            Port   = 8080us }

      let changes = [| MemberRemoved mem2 |]
      let mems = [| mem1; mem2 |]

      let log =
        LogEntry(Id.Create(), 7u, 1u, DataSnapshot State.Empty,
          Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot State.Empty,
            Some <| Configuration(Id.Create(), 5u, 1u, [| mem1 |],
              Some <| JointConsensus(Id.Create(), 4u, 1u, changes,
                Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, mems, DataSnapshot State.Empty)))))
        |> Log.fromEntries

      let config =
        Config.create "default" machine
        |> Config.addMember self
        |> Config.addMember mem1
        |> Config.addMember mem2

      let raft =
        createRaft config
        |> Either.map
            (fun raft ->
              { raft with
                  Log = log
                  CurrentTerm = 666u })
        |> Either.get

      saveRaft config raft
      |> Either.mapError Error.throw
      |> ignore

      let loaded = loadRaft config

      expect "Values should be equal" (Right raft) id loaded

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

  let pins _ =
    [| Pin.Bang       (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
    ; Pin.Toggle     (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
    ; Pin.String     (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 0u; Value = "one"   }|])
    ; Pin.MultiLine  (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 0u; Value = "two"   }|])
    ; Pin.FileName   (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 0u; Value = "three" }|])
    ; Pin.Directory  (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 0u; Value = "four"  }|])
    ; Pin.Url        (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 0u; Value = "five"  }|])
    ; Pin.IP         (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 0u; Value = "six"   }|])
    ; Pin.Float      (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0u; Value = 3.0    }|])
    ; Pin.Double     (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0u; Value = double 3.0 }|])
    ; Pin.Bytes      (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0u; Value = [| 2uy; 9uy |] }|])
    ; Pin.Color      (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
    ; Pin.Color      (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
    ; Pin.Enum       (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
    |]

  let mkPin _ =
    let slice : StringSliceD = { Index = 0u; Value = "hello" }
    Pin.String(Id.Create(), "url input", Id.Create(), [| |], [| slice |])

  let mkCue _ : Cue =
    { Id = Id.Create(); Name = "Cue 1"; Pins = pins () }

  let mkPatch _ : Patch =
    let pins = pins () |> Array.map toPair |> Map.ofArray
    { Id = Id.Create(); Name = "Patch 3"; Pins = pins }

  let mkCueList _ : CueList =
    { Id = Id.Create(); Name = "Patch 3"; Cues = [| mkCue (); mkCue () |] }

  let mkUser _ =
    { Id = Id.Create()
    ; UserName = "krgn"
    ; FirstName = "Karsten"
    ; LastName = "Gebbert"
    ; Email = "k@ioctl.it"
    ; Password = "1234"
    ; Salt = "909090"
    ; Joined = System.DateTime.Now
    ; Created = System.DateTime.Now
    }

  let mkMember _ = Id.Create() |> Member.create

  let mkMembers _ =
    let n = rand.Next(1, 6)
    [| for _ in 0 .. n do
        yield mkMember () |]

  let mkSession _ =
    { Id = Id.Create()
    ; Status = { StatusType = Unauthorized; Payload = "" }
    ; IpAddress = IPv4Address "127.0.0.1"
    ; UserAgent = "Oh my goodness"
    }

  let mkState _ =
    { Patches  = mkPatch   () |> fun (patch: Patch) -> Map.ofList [ (patch.Id, patch) ]
    ; Cues     = mkCue     () |> fun (cue: Cue) -> Map.ofList [ (cue.Id, cue) ]
    ; CueLists = mkCueList () |> fun (cuelist: CueList) -> Map.ofList [ (cuelist.Id, cuelist) ]
    ; Members  = mkMember  () |> fun (mem: RaftMember) -> Map.ofList [ (mem.Id, mem) ]
    ; Sessions = mkSession () |> fun (session: Session) -> Map.ofList [ (session.Id, session) ]
    ; Users    = mkUser    () |> fun (user: User) -> Map.ofList [ (user.Id, user) ]
    }

  let mkChange _ =
    match rand.Next(0,2) with
    | n when n > 0 -> MemberAdded(mkMember ())
    |          _   -> MemberRemoved(mkMember ())

  let mkChanges _ =
    let n = rand.Next(1, 6)
    [| for _ in 0 .. n do
        yield mkChange () |]

  let mkLog _ =
    LogEntry(Id.Create(), 7u, 1u, DataSnapshot State.Empty,
      Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot State.Empty,
        Some <| Configuration(Id.Create(), 5u, 1u, [| mkMember () |],
          Some <| JointConsensus(Id.Create(), 4u, 1u, mkChanges (),
            Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, mkMembers (), DataSnapshot State.Empty)))))
    |> Log.fromEntries

  //  ____        __ _   _
  // |  _ \ __ _ / _| |_| |    ___   __ _
  // | |_) / _` | |_| __| |   / _ \ / _` |
  // |  _ < (_| |  _| |_| |__| (_) | (_| |
  // |_| \_\__,_|_|  \__|_____\___/ \__, |
  //                                |___/

  let test_validate_log_yaml_serialization =
    testCase "Validate Log Yaml Serialization" <| fun _ ->
      let log : RaftLog = mkLog ()

      let relog = log |> Yaml.encode |> Yaml.decode |> Either.get
      expect "should be same" log id relog

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_validate_cue_binary_serialization =
    testCase "Validate Cue Binary Serialization" <| fun _ ->
      let cue : Cue = mkCue ()

      let recue = cue |> Binary.encode |> Binary.decode |> Either.get
      expect "should be same" cue id recue

  let test_validate_cue_yaml_serialization =
    testCase "Validate Cue Yaml Serialization" <| fun _ ->
      let cue : Cue = mkCue ()

      let recue = cue |> Yaml.encode |> Yaml.decode |> Either.get
      expect "should be same" cue id recue

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_validate_cuelist_binary_serialization =
    testCase "Validate CueList Binary Serialization" <| fun _ ->
      let cuelist : CueList = mkCueList ()

      let recuelist = cuelist |> Binary.encode |> Binary.decode |> Either.get
      expect "should be same" cuelist id recuelist

  let test_validate_cuelist_yaml_serialization =
    testCase "Validate CueList Yaml Serialization" <| fun _ ->
      let cuelist : CueList = mkCueList ()

      let recuelist = cuelist |> Yaml.encode |> Yaml.decode |> Either.get
      expect "should be same" cuelist id recuelist

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_validate_patch_binary_serialization =
    testCase "Validate Patch Binary Serialization" <| fun _ ->
      let patch : Patch = mkPatch ()

      let repatch = patch |> Binary.encode |> Binary.decode |> Either.get
      expect "Should be structurally equivalent" patch id repatch

  let test_validate_patch_yaml_serialization =
    testCase "Validate Patch Yaml Serialization" <| fun _ ->
      let patch : Patch = mkPatch ()

      let repatch = patch |> Yaml.encode |> Yaml.decode |> Either.get
      expect "Should be structurally equivalent" patch id repatch

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_validate_session_binary_serialization =
    testCase "Validate Session Binary Serialization" <| fun _ ->
      let session : Session = mkSession ()

      let resession = session |> Binary.encode |> Binary.decode |> Either.get
      expect "Should be structurally equivalent" session id resession

  let test_validate_session_yaml_serialization =
    testCase "Validate Session Yaml Serialization" <| fun _ ->
      let session : Session = mkSession ()

      let resession = session |> Yaml.encode |> Yaml.decode |> Either.get
      expect "Should be structurally equivalent" session id resession

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_validate_user_binary_serialization =
    testCase "Validate User Binary Serialization" <| fun _ ->
      let user : User = mkUser ()

      let reuser = user |> Binary.encode |> Binary.decode |> Either.get
      expect "Should be structurally equivalent" user id reuser

  let test_validate_user_yaml_serialization =
    testCase "Validate User Yaml Serialization" <| fun _ ->
      let user : User = mkUser ()

      let reuser = user |> Yaml.encode |> Yaml.decode |> Either.get
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
      ; CompoundSlice { Index = 0u; Value = pins () } |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Binary.encode |> Binary.decode |> Either.get
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
      ; CompoundSlice { Index = 0u; Value = pins () }
      |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Yaml.encode |> Yaml.decode |> Either.get
          expect "Should be structurally equivalent" slice id reslice)

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  let test_validate_pin_binary_serialization =
    testCase "Validate Pin Binary Serialization" <| fun _ ->
      let check pin =
        pin |> Binary.encode |> Binary.decode |> Either.get
        |> expect "Should be structurally equivalent" pin id

      Array.iter check (pins ())

      // compound
      let compound = Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = pins () }|])
      check compound

      // nested compound :)
      Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  let test_validate_pin_yaml_serialization =
    testCase "Validate Pin Yaml Serialization" <| fun _ ->
      let check pin =
        pin |> Yaml.encode |> Yaml.decode |> Either.get
        |> expect "Should be structurally equivalent" pin id

      Array.iter check (pins ())

      // compound
      let compound = Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = pins () }|])
      check compound

      // nested compound :)
      Pin.Compound(Id.Create(), "compound",  Id.Create(), mktags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_binary_serialization =
    testCase "Validate State Binary Serialization" <| fun _ ->
      let state : State = mkState ()

      state |> Binary.encode |> Binary.decode |> Either.get
      |> expect "Should be structurally equivalent" state id

  let test_validate_state_yaml_serialization =
    testCase "Validate State Yaml Serialization" <| fun _ ->
      let state : State = mkState ()

      state |> Yaml.encode |> Yaml.decode |> Either.get
      |> expect "Should be structurally equivalent" state id

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_binary_serialization =
    testCase "Validate StateMachine Binary Serialization" <| fun _ ->
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
      ; AddPin      <| mkPin ()
      ; UpdatePin   <| mkPin ()
      ; RemovePin   <| mkPin ()
      ; AddMember       <| Member.create (Id.Create())
      ; UpdateMember    <| Member.create (Id.Create())
      ; RemoveMember    <| Member.create (Id.Create())
      ; DataSnapshot  <| mkState ()
      ; Command AppCommand.Undo
      ; LogMsg(Logger.create Debug (Id.Create()) "bla" "oohhhh")
      ; SetLogLevel Warn
      ]
      |> List.iter
          (fun cmd ->
            let remsg = cmd |> Binary.encode |> Binary.decode |> Either.get
            expect "Should be structurally the same" cmd id remsg)

  let test_validate_state_machine_yaml_serialization =
    testCase "Validate StateMachine Yaml Serialization" <| fun _ ->
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
      ; AddPin      <| mkPin ()
      ; UpdatePin   <| mkPin ()
      ; RemovePin   <| mkPin ()
      ; AddMember     <| Member.create (Id.Create())
      ; UpdateMember  <| Member.create (Id.Create())
      ; RemoveMember  <| Member.create (Id.Create())
      ; DataSnapshot  <| mkState ()
      ; Command AppCommand.Undo
      ; LogMsg(Logger.create Debug (Id.Create()) "bla" "oohhhh")
      ; SetLogLevel Err
      ]
      |> List.iter
          (fun cmd ->
            let remsg = cmd |> Yaml.encode |> Yaml.decode |> Either.get
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
      test_save_restore_raft_value_correctly
      test_validate_log_yaml_serialization
      test_validate_cue_binary_serialization
      test_validate_cue_yaml_serialization
      test_validate_cuelist_binary_serialization
      test_validate_cuelist_yaml_serialization
      test_validate_patch_binary_serialization
      test_validate_patch_yaml_serialization
      test_validate_session_binary_serialization
      test_validate_session_yaml_serialization
      test_validate_user_binary_serialization
      test_validate_user_yaml_serialization
      test_validate_slice_binary_serialization
      test_validate_slice_yaml_serialization
      test_validate_pin_binary_serialization
      test_validate_pin_yaml_serialization
      test_validate_state_binary_serialization
      test_validate_state_yaml_serialization
      test_validate_state_machine_binary_serialization
      test_validate_state_machine_yaml_serialization
    ]
