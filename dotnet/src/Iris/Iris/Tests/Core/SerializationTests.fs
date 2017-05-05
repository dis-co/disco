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
        { Term = term 8
        ; LastLogIndex = index 128
        ; LastLogTerm = term 7
        ; Candidate = mem }

      RequestVote(Id.Create(), vr)
      |> binaryEncDec

  // __     __    _       ____
  // \ \   / /__ | |_ ___|  _ \ ___  ___ _ __   ___  _ __  ___  ___
  //  \ \ / / _ \| __/ _ \ |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  //   \ V / (_) | ||  __/  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  //    \_/ \___/ \__\___|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                                    |_|

  let test_validate_requestvote_response_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      let vr : VoteResponse =
        { Term = term 8
        ; Granted = false
        ; Reason = Some (RaftError("test","error")) }

      RequestVoteResponse(Id.Create(), vr)
      |> binaryEncDec

  //     _                               _ _____       _        _
  //    / \   _ __  _ __   ___ _ __   __| | ____|_ __ | |_ _ __(_) ___  ___
  //   / _ \ | '_ \| '_ \ / _ \ '_ \ / _` |  _| | '_ \| __| '__| |/ _ \/ __|
  //  / ___ \| |_) | |_) |  __/ | | | (_| | |___| | | | |_| |  | |  __/\__ \
  // /_/   \_\ .__/| .__/ \___|_| |_|\__,_|_____|_| |_|\__|_|  |_|\___||___/
  //         |_|   |_|

  let test_validate_appendentries_serialization =
    testCase "Validate RequestVote Response Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> Project.ofFilePath |> mkState

        let mem1 = Member.create (Id.Create())
        let mem2 = Member.create (Id.Create())

        let changes = [| MemberRemoved mem2 |]
        let mems = [| mem1; mem2 |]

        let log =
          Some <| LogEntry(Id.Create(), index 7, term 1, DataSnapshot            (state),
            Some <| LogEntry(Id.Create(), index 6, term 1, DataSnapshot            (state),
              Some <| Configuration(Id.Create(), index 5, term 1, [| mem1 |],
                Some <| JointConsensus(Id.Create(), index 4, term 1, changes,
                  Some <| Snapshot(Id.Create(), index 3, term 1, index 2, term 1, mems, DataSnapshot            (state))))))

        let ae : AppendEntries =
          { Term = term 8
          ; PrevLogIdx = index 192
          ; PrevLogTerm = term 87
          ; LeaderCommit = index 182
          ; Entries = log }

        AppendEntries(Id.Create(), ae)
        |> binaryEncDec

        AppendEntries(Id.Create(), { ae with Entries = None })
        |> binaryEncDec
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
        { Term         = term 38
        ; Success      = true
        ; CurrentIndex = index 1234
        ; FirstIndex   = index 8942
        }

      AppendEntriesResponse(Id.Create(), response)
      |> binaryEncDec

  //  ____                        _           _
  // / ___| _ __   __ _ _ __  ___| |__   ___ | |_
  // \___ \| '_ \ / _` | '_ \/ __| '_ \ / _ \| __|
  //  ___) | | | | (_| | |_) \__ \ | | | (_) | |_
  // |____/|_| |_|\__,_| .__/|___/_| |_|\___/ \__|
  //                   |_|

  let test_validate_installsnapshot_serialization =
    testCase "Validate InstallSnapshot Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> Project.ofFilePath |> mkState

        let mem1 = [| Member.create (Id.Create()) |]

        let is : InstallSnapshot =
          { Term = term 2134
          ; LeaderId = Id.Create()
          ; LastIndex = index 242
          ; LastTerm = term 124242
          ; Data = Snapshot(Id.Create(), index 12, term 3414, index 241, term 422, mem1, DataSnapshot            (state))
          }

        InstallSnapshot(Id.Create(), is)
        |> binaryEncDec
      }
      |> noError

  //  _   _                 _ ____  _           _
  // | | | | __ _ _ __   __| / ___|| |__   __ _| | _____
  // | |_| |/ _` | '_ \ / _` \___ \| '_ \ / _` | |/ / _ \
  // |  _  | (_| | | | | (_| |___) | | | | (_| |   <  __/
  // |_| |_|\__,_|_| |_|\__,_|____/|_| |_|\__,_|_|\_\___|

  let test_validate_handshake_serialization =
    testCase "Validate HandShake Serialization" <| fun _ ->
      HandShake(Member.create (Id.Create()))
      |> binaryEncDec

  //  _   _                 ___        __    _
  // | | | | __ _ _ __   __| \ \      / /_ _(_)_   _____
  // | |_| |/ _` | '_ \ / _` |\ \ /\ / / _` | \ \ / / _ \
  // |  _  | (_| | | | | (_| | \ V  V / (_| | |\ V /  __/
  // |_| |_|\__,_|_| |_|\__,_|  \_/\_/ \__,_|_| \_/ \___|

  let test_validate_handwaive_serialization =
    testCase "Validate HandWaive Serialization" <| fun _ ->
      HandWaive(Member.create (Id.Create()))
      |> binaryEncDec

  //  ____          _ _               _
  // |  _ \ ___  __| (_)_ __ ___  ___| |_
  // | |_) / _ \/ _` | | '__/ _ \/ __| __|
  // |  _ <  __/ (_| | | | |  __/ (__| |_
  // |_| \_\___|\__,_|_|_|  \___|\___|\__|

  let test_validate_redirect_serialization =
    testCase "Validate Redirect Serialization" <| fun _ ->
      Redirect(Member.create (Id.Create()))
      |> binaryEncDec

  // __        __   _
  // \ \      / /__| | ___ ___  _ __ ___   ___
  //  \ \ /\ / / _ \ |/ __/ _ \| '_ ` _ \ / _ \
  //   \ V  V /  __/ | (_| (_) | | | | | |  __/
  //    \_/\_/ \___|_|\___\___/|_| |_| |_|\___|

  let test_validate_welcome_serialization =
    testCase "Validate Welcome Serialization" <| fun _ ->
      Welcome(Member.create (Id.Create()))
      |> binaryEncDec

  //     _              _               _               _
  //    / \   _ __ _ __(_)_   _____  __| | ___ _ __ ___(_)
  //   / _ \ | '__| '__| \ \ / / _ \/ _` |/ _ \ '__/ __| |
  //  / ___ \| |  | |  | |\ V /  __/ (_| |  __/ | | (__| |
  // /_/   \_\_|  |_|  |_| \_/ \___|\__,_|\___|_|  \___|_|

  let test_validate_arrivederci_serialization =
    testCase "Validate Arrivederci Serialization" <| fun _ ->
      Arrivederci |> binaryEncDec

  //  _____
  // | ____|_ __ _ __ ___  _ __
  // |  _| | '__| '__/ _ \| '__|
  // | |___| |  | | | (_) | |
  // |_____|_|  |_|  \___/|_|

  let test_validate_errorresponse_serialization =
    testCase "Validate ErrorResponse Serialization" <| fun _ ->
      List.iter (ErrorResponse >> binaryEncDec) [
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

  //  ____        __ _
  // |  _ \ __ _ / _| |_
  // | |_) / _` | |_| __|
  // |  _ < (_| |  _| |_
  // |_| \_\__,_|_|  \__|

  let test_save_restore_raft_value_correctly =
    testCase "save/restore raft value correctly" <| fun _ ->
      either {
        let machine = MachineConfig.create "127.0.0.1" None

        let self =
          machine.MachineId
          |> Member.create

        let mem1 =
          Id.Create()
          |> Member.create

        let mem2 =
          Id.Create()
          |> Member.create

        let site =
          { ClusterConfig.Default with
              Name = name "Cool Cluster Yo"
              Members = Map.ofArray [| (self.Id,self)
                                       (mem1.Id, mem1)
                                       (mem2.Id, mem2) |] }

        let config =
          Config.create "default" machine
          |> Config.addSiteAndSetActive site

        let trm = term 666

        let! raft =
          createRaft config
          |> Either.map (Raft.setTerm trm)

        saveRaft config raft
        |> Either.mapError Error.throw
        |> ignore

        let! loaded = loadRaft config

        expect "Member should be correct" self Raft.self loaded
        expect "Term should be correct" trm Raft.currentTerm loaded
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
        let! project = mkTmpDir () |> Project.ofFilePath |>  mkProject
        let! reproject = project |> Binary.encode |> Binary.decode
        expect "Project should be the same" project id reproject
      }
      |> noError

  let test_validate_project_yaml_serialization =
    testCase "Validate IrisProject Yaml Serializaton" <| fun _ ->
      either {
        let! project = mkTmpDir () |> Project.ofFilePath |> mkProject
        let reproject : IrisProject = project |> Yaml.encode |> Yaml.decode |> Either.get
        let reconfig = { reproject.Config with Machine = project.Config.Machine }

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
      mkCue () |> binaryEncDec

  let test_validate_cue_yaml_serialization =
    testCase "Validate Cue Yaml Serialization" <| fun _ ->
      mkCue () |> yamlEncDec

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_validate_cuelist_binary_serialization =
    testCase "Validate CueList Binary Serialization" <| fun _ ->
      mkCueList () |> binaryEncDec

  let test_validate_cuelist_yaml_serialization =
    testCase "Validate CueList Yaml Serialization" <| fun _ ->
      mkCueList () |> yamlEncDec

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_validate_group_binary_serialization =
    testCase "Validate PinGroup Binary Serialization" <| fun _ ->
      mkPinGroup () |> binaryEncDec

  let test_validate_group_yaml_serialization =
    testCase "Validate PinGroup Yaml Serialization" <| fun _ ->
      mkPinGroup () |> yamlEncDec

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_validate_session_binary_serialization =
    testCase "Validate Session Binary Serialization" <| fun _ ->
      mkSession () |> binaryEncDec

  let test_validate_session_yaml_serialization =
    testCase "Validate Session Yaml Serialization" <| fun _ ->
      mkSession () |> yamlEncDec

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_validate_user_binary_serialization =
    testCase "Validate User Binary Serialization" <| fun _ ->
      mkUser () |> binaryEncDec

  let test_validate_user_yaml_serialization =
    testCase "Validate User Yaml Serialization" <| fun _ ->
      mkUser () |> yamlEncDec

  //  ____  _ _
  // / ___|| (_) ___ ___
  // \___ \| | |/ __/ _ \
  //  ___) | | | (_|  __/
  // |____/|_|_|\___\___|

  let test_validate_slice_binary_serialization =
    testCase "Validate Slice Binary Serialization" <| fun _ ->
      [| BoolSlice   (index 0, true)
      ;  StringSlice (index 0, "hello")
      ;  NumberSlice (index 0, 1234.0)
      ;  ByteSlice   (index 0, [| 0uy; 4uy; 9uy; 233uy |])
      ;  EnumSlice   (index 0, { Key = "one"; Value = "two" })
      ;  ColorSlice  (index 0, RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy })
      ;  ColorSlice  (index 0, HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy })
      |]
      |> Array.iter binaryEncDec

  let test_validate_slice_yaml_serialization =
    testCase "Validate Slice Yaml Serialization" <| fun _ ->
      [| BoolSlice    (index 0, true    )
      ;  StringSlice  (index 0, "hello" )
      ;  NumberSlice  (index 0, 1234.0  )
      ;  ByteSlice    (index 0, [| 0uy; 4uy; 9uy; 233uy |] )
      ;  EnumSlice    (index 0, { Key = "one"; Value = "two" })
      ;  ColorSlice   (index 0, RGBA { Red = 255uy; Blue = 2uy; Green = 255uy; Alpha = 33uy } )
      ;  ColorSlice   (index 0, HSLA { Hue = 255uy; Saturation = 25uy; Lightness = 255uy; Alpha = 55uy } )
      |]
      |> Array.iter yamlEncDec

  //  ____  _ _
  // / ___|| (_) ___ ___  ___
  // \___ \| | |/ __/ _ \/ __|
  //  ___) | | | (_|  __/\__ \
  // |____/|_|_|\___\___||___/

  let test_validate_slices_binary_serialization =
    testCase "Validate Slices Binary Serialization" <| fun _ ->
      mkSlices() |> Array.iter binaryEncDec

  let test_validate_slices_yaml_serialization =
    testCase "Validate Slices Yaml Serialization" <| fun _ ->
      mkSlices() |> Array.iter yamlEncDec

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let test_validate_pin_binary_serialization =
    testCase "Validate Pin Binary Serialization" <| fun _ ->
      mkPins () |> Array.iter binaryEncDec

  let test_validate_pin_yaml_serialization =
    testCase "Validate Pin Yaml Serialization" <| fun _ ->
      mkPins () |> Array.iter yamlEncDec

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_validate_client_binary_serialization =
    testCase "Validate Client Binary Serialization" <| fun _ ->
      mkClient () |> binaryEncDec

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_binary_serialization =
    testCase "Validate State Binary Serialization" <| fun _ ->
      mkTmpDir() |> Project.ofFilePath |> mkState |> Either.map binaryEncDec |> noError

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_binary_serialization =
    testCase "Validate StateMachine Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> Project.ofFilePath |> mkState

        [ AddCue                  <| mkCue ()
          UpdateCue               <| mkCue ()
          RemoveCue               <| mkCue ()
          AddCueList              <| mkCueList ()
          UpdateCueList           <| mkCueList ()
          RemoveCueList           <| mkCueList ()
          AddSession              <| mkSession ()
          UpdateSession           <| mkSession ()
          RemoveSession           <| mkSession ()
          AddUser                 <| mkUser ()
          UpdateUser              <| mkUser ()
          RemoveUser              <| mkUser ()
          AddPinGroup             <| mkPinGroup ()
          UpdatePinGroup          <| mkPinGroup ()
          RemovePinGroup          <| mkPinGroup ()
          AddPin                  <| mkPin ()
          UpdatePin               <| mkPin ()
          RemovePin               <| mkPin ()
          UpdateSlices            <| mkSlice ()
          AddClient               <| mkClient ()
          UpdateClient            <| mkClient ()
          RemoveClient            <| mkClient ()
          AddMember               <| Member.create (Id.Create())
          UpdateMember            <| Member.create (Id.Create())
          RemoveMember            <| Member.create (Id.Create())
          AddDiscoveredService    <| mkDiscoveredService ()
          UpdateDiscoveredService <| mkDiscoveredService ()
          RemoveDiscoveredService <| mkDiscoveredService ()
          DataSnapshot            <| state
          UpdateClock 1234u
          Command AppCommand.Undo
          LogMsg(Logger.create Debug "bla" "oohhhh")
          SetLogLevel Warn
        ] |> List.iter binaryEncDec
      }
      |> noError

  let test_validate_discovered_service_binary_serialization =
    testCase "Validate DiscoveredService Binary Serialization" <| fun _ ->
      mkDiscoveredService()
      |> binaryEncDec

  let test_validate_client_api_request_binary_serialization =
    testCase "Validate ClientApiRequest Binary Serialization" <| fun _ ->
      either {
        let! state = mkTmpDir() |> Project.ofFilePath |> mkState
        [ AddCue                  <| mkCue ()
          UpdateCue               <| mkCue ()
          RemoveCue               <| mkCue ()
          AddCueList              <| mkCueList ()
          UpdateCueList           <| mkCueList ()
          RemoveCueList           <| mkCueList ()
          AddSession              <| mkSession ()
          UpdateSession           <| mkSession ()
          RemoveSession           <| mkSession ()
          AddUser                 <| mkUser ()
          UpdateUser              <| mkUser ()
          RemoveUser              <| mkUser ()
          AddPinGroup             <| mkPinGroup ()
          UpdatePinGroup          <| mkPinGroup ()
          RemovePinGroup          <| mkPinGroup ()
          AddPin                  <| mkPin ()
          UpdatePin               <| mkPin ()
          RemovePin               <| mkPin ()
          UpdateSlices            <| mkSlice ()
          AddClient               <| mkClient ()
          UpdateClient            <| mkClient ()
          RemoveClient            <| mkClient ()
          AddDiscoveredService    <| mkDiscoveredService ()
          UpdateDiscoveredService <| mkDiscoveredService ()
          RemoveDiscoveredService <| mkDiscoveredService ()
          AddMember               <| Member.create (Id.Create())
          UpdateMember            <| Member.create (Id.Create())
          RemoveMember            <| Member.create (Id.Create())
          DataSnapshot            <| state
        ] |> List.iter binaryEncDec
      }
      |> noError

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let serializationTests =
    testList "Serialization Tests" [
      test_validate_discovered_service_binary_serialization
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
      test_validate_group_binary_serialization
      test_validate_group_yaml_serialization
      test_validate_session_binary_serialization
      test_validate_session_yaml_serialization
      test_validate_user_binary_serialization
      test_validate_user_yaml_serialization
      test_validate_slice_binary_serialization
      test_validate_slices_binary_serialization
      test_validate_pin_binary_serialization
      test_validate_pin_yaml_serialization
      test_validate_client_binary_serialization
      test_validate_state_binary_serialization
      test_validate_state_machine_binary_serialization
      test_validate_client_api_request_binary_serialization
    ]
