namespace Iris.Tests

open Expecto
open Iris.Core
open Iris.Raft
open Iris.Service
open Iris.Serialization
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
        ; Reason = Some (RaftError("test","error")) }

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
      either {
        let! state = mkTmpDir() |> mkState

        let mem1 = Member.create (Id.Create())
        let mem2 = Member.create (Id.Create())

        let changes = [| MemberRemoved mem2 |]
        let mems = [| mem1; mem2 |]

        let log =
          Some <| LogEntry(Id.Create(), 7u, 1u, DataSnapshot(state),
            Some <| LogEntry(Id.Create(), 6u, 1u, DataSnapshot(state),
              Some <| Configuration(Id.Create(), 5u, 1u, [| mem1 |],
                Some <| JointConsensus(Id.Create(), 4u, 1u, changes,
                  Some <| Snapshot(Id.Create(), 3u, 1u, 2u, 1u, mems, DataSnapshot(state))))))

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
      }
      |> noError

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
      either {
        let! state = mkTmpDir() |> mkState

        let mem1 = [| Member.create (Id.Create()) |]

        let is : InstallSnapshot =
          { Term = 2134u
          ; LeaderId = Id.Create()
          ; LastIndex = 242u
          ; LastTerm = 124242u
          ; Data = Snapshot(Id.Create(), 12u, 3414u, 241u, 422u, mem1, DataSnapshot(state))
          }

        let msg = InstallSnapshot(Id.Create(), is)
        let remsg = msg |> Binary.encode |> Binary.decode |> Either.get

        expect "Should be structurally the same" msg id remsg
      }
      |> noError

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
          GitError ("one","two")
          ProjectError ("one","two")
          ParseError ("one","two")
          SocketError ("one","two")
          IOError ("one","two")
          AssetError ("one","two")
          RaftError ("one","two")
          Other  ("one","two")
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
      either {
        let machine = MachineConfig.create ()

        let self =
          machine.MachineId
          |> Member.create

        let mem1 =
          Id.Create()
          |> Member.create

        let mem2 =
          Id.Create()
          |> Member.create

        let config =
          Config.create "default" machine
          |> Config.addMember self
          |> Config.addMember mem1
          |> Config.addMember mem2

        let term = 666u

        let! raft =
          createRaft config
          |> Either.map (Raft.setTerm term)

        saveRaft config raft
        |> Either.mapError Error.throw
        |> ignore

        let! loaded = loadRaft config

        expect "Member should be correct" self Raft.self loaded
        expect "Term should be correct" term Raft.currentTerm loaded
      }
      |> noError

  //  ____            _           _
  // |  _ \ _ __ ___ (_) ___  ___| |_
  // | |_) | '__/ _ \| |/ _ \/ __| __|
  // |  __/| | | (_) | |  __/ (__| |_
  // |_|   |_|  \___// |\___|\___|\__|
  //               |__/

  let test_validate_project_binary_serialization =
    testCase "Validate IrisProject Binary Serializaton" <| fun _ ->
      either {
        let! project = mkTmpDir () |>  mkProject
        let! reproject = project |> Binary.encode |> Binary.decode
        expect "Project should be the same" project id reproject
      }
      |> noError

  let test_validate_project_yaml_serialization =
    testCase "Validate IrisProject Yaml Serializaton" <| fun _ ->
      either {
        let! project = mkTmpDir () |>  mkProject
        let reproject : IrisProject = project |> Yaml.encode |> Yaml.decode |> Either.get
        let reconfig = { reproject.Config with MachineId = project.Config.MachineId }

        // not all properties can be the same (timestampts for instance, so we check basics)
        expect "Project Id should be the same" project.Id id reproject.Id
        expect "Project Name should be the same" project.Name id reproject.Name
        expect "Project Config should be the same" project.Config id reconfig
      }
      |> noError

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
      ; CompoundSlice { Index = 0u; Value = mkPins () } |]
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
      ; CompoundSlice { Index = 0u; Value = mkPins () }
      |]
      |> Array.iter
        (fun slice ->
          let reslice = slice |> Yaml.encode |> Yaml.decode |> Either.get
          expect "Should be structurally equivalent" slice id reslice)

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let test_validate_pin_binary_serialization =
    testCase "Validate Pin Binary Serialization" <| fun _ ->
      let check pin =
        pin |> Binary.encode |> Binary.decode |> Either.get
        |> expect "Should be structurally equivalent" pin id

      Array.iter check (mkPins ())

      // compound
      let compound = Pin.Compound(mk(), "compound",  mk(), mkTags (), [|{ Index = 0u; Value = mkPins () }|])
      check compound

      // nested compound :)
      Pin.Compound(Id.Create(), "compound",  Id.Create(), mkTags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  let test_validate_pin_yaml_serialization =
    testCase "Validate Pin Yaml Serialization" <| fun _ ->
      let check pin =
        pin |> Yaml.encode |> Yaml.decode |> Either.get
        |> expect "Should be structurally equivalent" pin id

      Array.iter check (mkPins ())

      // compound
      let compound = Pin.Compound(Id.Create(), "compound",  Id.Create(), mkTags (), [|{ Index = 0u; Value = mkPins () }|])
      check compound

      // nested compound :)
      Pin.Compound(Id.Create(), "compound",  Id.Create(), mkTags (), [|{ Index = 0u; Value = [| compound |] }|])
      |> check

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_validate_client_binary_serialization =
    testCase "Validate Client Binary Serialization" <| fun _ ->
      either {
        let client = mkClient ()
        let! reclient = client |> Binary.encode |> Binary.decode
        expect "Should be structurally equivalent" client id reclient
      }
      |> noError

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_binary_serialization =
    testCase "Validate State Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState
        let! restate = state |> Binary.encode |> Binary.decode
        expect "Should be structurally equivalent" state id restate
      }
      |> noError

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_binary_serialization =
    testCase "Validate StateMachine Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> mkState

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
        ; AddPin        <| mkPin ()
        ; UpdatePin     <| mkPin ()
        ; RemovePin     <| mkPin ()
        ; AddClient     <| mkClient ()
        ; UpdateClient  <| mkClient ()
        ; RemoveClient  <| mkClient ()
        ; AddMember     <| Member.create (Id.Create())
        ; UpdateMember  <| Member.create (Id.Create())
        ; RemoveMember  <| Member.create (Id.Create())
        ; DataSnapshot  <| state
        ; Command AppCommand.Undo
        ; LogMsg(Logger.create Debug (Id.Create()) "bla" "oohhhh")
        ; SetLogLevel Warn
        ]
        |> List.iter
            (fun cmd ->
              let remsg = cmd |> Binary.encode |> Binary.decode |> Either.get
              expect "Should be structurally the same" cmd id remsg)
      }
      |> noError
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
      test_validate_project_binary_serialization
      test_validate_project_yaml_serialization
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
      test_validate_client_binary_serialization
      test_validate_state_binary_serialization
      test_validate_state_machine_binary_serialization
    ]
