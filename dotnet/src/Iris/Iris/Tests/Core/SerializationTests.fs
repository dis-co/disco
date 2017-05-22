namespace Iris.Tests

open Expecto
open Expecto.Helpers
open FsCheck
open FsCheck.GenBuilder
open Iris.Core
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Serialization
open Iris.Service.Utilities
open Iris.Service.Persistence
open System
open System.Net
open FlatBuffers
open FSharpx.Functional



[<AutoOpen>]
module SerializationTests =
  //   ____             __ _        ____ _
  //  / ___|___  _ __  / _(_) __ _ / ___| |__   __ _ _ __   __ _  ___
  // | |   / _ \| '_ \| |_| |/ _` | |   | '_ \ / _` | '_ \ / _` |/ _ \
  // | |__| (_) | | | |  _| | (_| | |___| | | | (_| | | | | (_| |  __/
  //  \____\___/|_| |_|_| |_|\__, |\____|_| |_|\__,_|_| |_|\__, |\___|
  //                         |___/                         |___/

  let test_validate_config_change =
    testCase "ConfigChange serialization should work" <| fun _ ->
      binaryEncDec<ConfigChange>
      |> Prop.forAll Generators.changeArb
      |> Check.QuickThrowOnFailure

  //  ____        __ _   ____                            _
  // |  _ \ __ _ / _| |_|  _ \ ___  __ _ _   _  ___  ___| |_
  // | |_) / _` | |_| __| |_) / _ \/ _` | | | |/ _ \/ __| __|
  // |  _ < (_| |  _| |_|  _ <  __/ (_| | |_| |  __/\__ \ |_
  // |_| \_\__,_|_|  \__|_| \_\___|\__, |\__,_|\___||___/\__|
  //                                  |_|

  let test_validate_raftrequest_serialization =
    testCase "Validate RaftRequest Serialization" <| fun _ ->
      binaryEncDec<RaftRequest>
      |> Prop.forAll Generators.raftRequestArb
      |> Check.QuickThrowOnFailure

  //  ____        __ _   ____
  // |  _ \ __ _ / _| |_|  _ \ ___  ___ _ __   ___  _ __  ___  ___
  // | |_) / _` | |_| __| |_) / _ \/ __| '_ \ / _ \| '_ \/ __|/ _ \
  // |  _ < (_| |  _| |_|  _ <  __/\__ \ |_) | (_) | | | \__ \  __/
  // |_| \_\__,_|_|  \__|_| \_\___||___/ .__/ \___/|_| |_|___/\___|
  //                                   |_|

  let test_validate_requestvote_response_serialization =
    testCase "Validate RaftResponse Serialization" <| fun _ ->
      binaryEncDec<RaftResponse>
      |> Prop.forAll Generators.raftResponseArb
      |> Check.QuickThrowOnFailure

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
      binaryEncDec<IrisProject>
      |> Prop.forAll Generators.projectArb
      |> Check.QuickThrowOnFailure


  let test_validate_project_yaml_serialization =
    testCase "Validate IrisProject Yaml Serializaton" <| fun _ ->
      yamlEncDec<IrisProject>
      |> Prop.forAll Generators.projectArb
      |> Check.QuickThrowOnFailure

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_validate_cue_binary_serialization =
    testCase "Validate Cue Binary Serialization" <| fun _ ->
      binaryEncDec<Cue>
      |> Prop.forAll Generators.cueArb
      |> Check.QuickThrowOnFailure


  let test_validate_cue_yaml_serialization =
    testCase "Validate Cue Yaml Serialization" <| fun _ ->
      yamlEncDec<Cue>
      |> Prop.forAll Generators.cueArb
      |> Check.QuickThrowOnFailure

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_validate_cuelist_binary_serialization =
    testCase "Validate CueList Binary Serialization" <| fun _ ->
      binaryEncDec<CueList>
      |> Prop.forAll Generators.cuelistArb
      |> Check.QuickThrowOnFailure

  let test_validate_cuelist_yaml_serialization =
    testCase "Validate CueList Yaml Serialization" <| fun _ ->
      yamlEncDec<CueList>
      |> Prop.forAll Generators.cuelistArb
      |> Check.QuickThrowOnFailure

  //  ____  _        ____
  // |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __
  // | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \
  // |  __/| | | | | |_| | | | (_) | |_| | |_) |
  // |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/
  //                                     |_|

  let test_validate_group_binary_serialization =
    testCase "Validate PinGroup Binary Serialization" <| fun _ ->
      binaryEncDec<PinGroup>
      |> Prop.forAll Generators.pingroupArb
      |> Check.QuickThrowOnFailure

  let test_validate_group_yaml_serialization =
    testCase "Validate PinGroup Yaml Serialization" <| fun _ ->
      yamlEncDec<PinGroup>
      |> Prop.forAll Generators.pingroupArb
      |> Check.QuickThrowOnFailure

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_validate_session_binary_serialization =
    testCase "Validate Session Binary Serialization" <| fun _ ->
      binaryEncDec<Session>
      |> Prop.forAll Generators.sessionArb
      |> Check.QuickThrowOnFailure

  let test_validate_session_yaml_serialization =
    testCase "Validate Session Yaml Serialization" <| fun _ ->
      yamlEncDec<Session>
      |> Prop.forAll Generators.sessionArb
      |> Check.QuickThrowOnFailure


  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_validate_user_binary_serialization =
    testCase "Validate User Binary Serialization" <| fun _ ->
      binaryEncDec<User>
      |> Prop.forAll Generators.userArb
      |> Check.QuickThrowOnFailure

  let test_validate_user_yaml_serialization =
    testCase "Validate User Yaml Serialization" <| fun _ ->
      yamlEncDec<User>
      |> Prop.forAll Generators.userArb
      |> Check.QuickThrowOnFailure

  //  ____  _ _
  // / ___|| (_) ___ ___
  // \___ \| | |/ __/ _ \
  //  ___) | | | (_|  __/
  // |____/|_|_|\___\___|

  let test_validate_slice_binary_serialization =
    testCase "Validate Slice Binary Serialization" <| fun _ ->
      binaryEncDec<Slice>
      |> Prop.forAll Generators.sliceArb
      |> Check.QuickThrowOnFailure

  let test_validate_slice_yaml_serialization =
    testCase "Validate Slice Yaml Serialization" <| fun _ ->
      yamlEncDec<Slice>
      |> Prop.forAll Generators.sliceArb
      |> Check.QuickThrowOnFailure


  //  ____  _ _
  // / ___|| (_) ___ ___  ___
  // \___ \| | |/ __/ _ \/ __|
  //  ___) | | | (_|  __/\__ \
  // |____/|_|_|\___\___||___/

  let test_validate_slices_binary_serialization =
    testCase "Validate Slices Binary Serialization" <| fun _ ->
      binaryEncDec<Slices>
      |> Prop.forAll Generators.slicesArb
      |> Check.QuickThrowOnFailure

  let test_validate_slices_yaml_serialization =
    testCase "Validate Slices Yaml Serialization" <| fun _ ->
      yamlEncDec<Slices>
      |> Prop.forAll Generators.slicesArb
      |> Check.QuickThrowOnFailure

  //  ____  _
  // |  _ \(_)_ __
  // | |_) | | '_ \
  // |  __/| | | | |
  // |_|   |_|_| |_|

  let test_validate_pin_binary_serialization =
    testCase "Validate Pin Binary Serialization" <| fun _ ->
      binaryEncDec<Pin>
      |> Prop.forAll Generators.pinArb
      |> Check.QuickThrowOnFailure

  let test_validate_pin_yaml_serialization =
    testCase "Validate Pin Yaml Serialization" <| fun _ ->
      yamlEncDec<Pin>
      |> Prop.forAll Generators.pinArb
      |> Check.QuickThrowOnFailure

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_validate_client_binary_serialization =
    testCase "Validate Client Binary Serialization" <| fun _ ->
      binaryEncDec<IrisClient>
      |> Prop.forAll Generators.clientArb
      |> Check.QuickThrowOnFailure

  //  ____  _        _
  // / ___|| |_ __ _| |_ ___
  // \___ \| __/ _` | __/ _ \
  //  ___) | || (_| | ||  __/
  // |____/ \__\__,_|\__\___|

  let test_validate_state_binary_serialization =
    testCase "Validate State Binary Serialization" <| fun _ ->
      binaryEncDec<State>
      |> Prop.forAll Generators.stateArb
      |> Check.QuickThrowOnFailure

  //  ____  _        _       __  __            _     _
  // / ___|| |_ __ _| |_ ___|  \/  | __ _  ___| |__ (_)_ __   ___
  // \___ \| __/ _` | __/ _ \ |\/| |/ _` |/ __| '_ \| | '_ \ / _ \
  //  ___) | || (_| | ||  __/ |  | | (_| | (__| | | | | | | |  __/
  // |____/ \__\__,_|\__\___|_|  |_|\__,_|\___|_| |_|_|_| |_|\___|

  let test_validate_state_machine_binary_serialization =
    testCase "Validate StateMachine Binary Serialization" <| fun _ ->
      binaryEncDec<StateMachine>
      |> Prop.forAll Generators.stateMachineArb
      |> Check.QuickThrowOnFailure

  //  ____  _                                     _
  // |  _ \(_)___  ___ _____   _____ _ __ ___  __| |
  // | | | | / __|/ __/ _ \ \ / / _ \ '__/ _ \/ _` |
  // | |_| | \__ \ (_| (_) \ V /  __/ | |  __/ (_| |
  // |____/|_|___/\___\___/ \_/ \___|_|  \___|\__,_|

  let test_validate_discovered_service_binary_serialization =
    testCase "Validate DiscoveredService Binary Serialization" <| fun _ ->
      binaryEncDec<DiscoveredService>
      |> Prop.forAll Generators.discoveredArb
      |> Check.QuickThrowOnFailure

  let test_validate_client_api_request_binary_serialization =
    testCase "Validate ClientApiRequest Binary Serialization" <| fun _ ->
      binaryEncDec<ClientApiRequest>
      |> Prop.forAll Generators.clientApiRequestArb
      |> Check.QuickThrowOnFailure

  let test_validate_server_api_request_binary_serialization =
    testCase "Validate ServerApiRequest Binary Serialization" <| fun _ ->
      binaryEncDec<ServerApiRequest>
      |> Prop.forAll Generators.serverApiRequestArb
      |> Check.QuickThrowOnFailure

  let test_validate_api_response_binary_serialization =
    testCase "Validate ApiResponse Binary Serialization" <| fun _ ->
      binaryEncDec<ApiResponse>
      |> Prop.forAll Generators.apiResponseArb
      |> Check.QuickThrowOnFailure

  let test_validate_cueplayer_binary_serialization =
    testCase "Validate CuePlayer Binary Serialization" <| fun _ ->
      binaryEncDec<CuePlayer>
      |> Prop.forAll Generators.cuePlayerArb
      |> Check.QuickThrowOnFailure

  let test_validate_cueplayer_yaml_serialization =
    testCase "Validate CuePlayer Yaml Serialization" <| fun _ ->
      yamlEncDec<CuePlayer>
      |> Prop.forAll Generators.cuePlayerArb
      |> Check.QuickThrowOnFailure

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let serializationTests =
    ftestList "Serialization Tests" [
      test_validate_config_change
      test_validate_user_yaml_serialization
      test_validate_user_binary_serialization

      // test_validate_raftrequest_serialization
      // test_validate_requestvote_response_serialization
      // test_validate_discovered_service_binary_serialization
      // test_save_restore_raft_value_correctly
      // test_validate_project_binary_serialization
      // test_validate_project_yaml_serialization
      // test_validate_cue_binary_serialization
      // test_validate_cue_yaml_serialization
      // test_validate_cuelist_binary_serialization
      // test_validate_cuelist_yaml_serialization
      // test_validate_group_binary_serialization
      // test_validate_group_yaml_serialization
      // test_validate_session_binary_serialization
      // test_validate_session_yaml_serialization
      // test_validate_slice_binary_serialization
      // test_validate_slices_binary_serialization
      // test_validate_pin_binary_serialization
      // test_validate_pin_yaml_serialization
      // test_validate_client_binary_serialization
      // test_validate_state_binary_serialization
      // test_validate_state_machine_binary_serialization
      // test_validate_client_api_request_binary_serialization
      // test_validate_cueplayer_binary_serialization
      // test_validate_cueplayer_yaml_serialization
    ]
