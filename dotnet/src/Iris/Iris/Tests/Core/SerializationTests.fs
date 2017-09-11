namespace Iris.Tests

open Expecto
open FsCheck
open Iris.Core
open Iris.Net
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Persistence
open System
open System.IO
open System.Threading

[<AutoOpen>]
module SerializationTests =
  ///  ____  _        ____                       __  __
  /// |  _ \(_)_ __  / ___|_ __ ___  _   _ _ __ |  \/  | __ _ _ __
  /// | |_) | | '_ \| |  _| '__/ _ \| | | | '_ \| |\/| |/ _` | '_ \
  /// |  __/| | | | | |_| | | | (_) | |_| | |_) | |  | | (_| | |_) |
  /// |_|   |_|_| |_|\____|_|  \___/ \__,_| .__/|_|  |_|\__,_| .__/
  ///                                     |_|                |_|

  let test_binary_pingroup_map =
    testCase "PinGroupMap binary serialization should work" <| fun _ ->
      binaryEncDec<PinGroupMap>
      |> Prop.forAll Generators.pinGroupMapArb
      |> Check.QuickThrowOnFailure

  ///  ____       __                                  ___     __    _
  /// |  _ \ ___ / _| ___ _ __ ___ _ __   ___ ___  __| \ \   / /_ _| |_   _  ___
  /// | |_) / _ \ |_ / _ \ '__/ _ \ '_ \ / __/ _ \/ _` |\ \ / / _` | | | | |/ _ \
  /// |  _ <  __/  _|  __/ | |  __/ | | | (_|  __/ (_| | \ V / (_| | | |_| |  __/
  /// |_| \_\___|_|  \___|_|  \___|_| |_|\___\___|\__,_|  \_/ \__,_|_|\__,_|\___|

  let test_binary_referenced_value =
    testCase "ReferencedValue binary serialization should work" <| fun _ ->
      binaryEncDec<ReferencedValue>
      |> Prop.forAll Generators.referencedValueArb
      |> Check.QuickThrowOnFailure

  let test_yaml_referenced_value =
    testCase "ReferencedValue yaml serialization should work" <| fun _ ->
      yamlEncDec
      |> Prop.forAll Generators.referencedValueArb
      |> Check.QuickThrowOnFailure

  //  ____  _    __        ___     _            _
  // |  _ \(_)_ _\ \      / (_) __| | __ _  ___| |_
  // | |_) | | '_ \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
  // |  __/| | | | \ V  V / | | (_| | (_| |  __/ |_
  // |_|   |_|_| |_|\_/\_/  |_|\__,_|\__, |\___|\__|
  //                                 |___/

  let test_binary_pin_widget =
    testCase "PinWidget binary serialization should work" <| fun _ ->
      binaryEncDec<PinWidget>
      |> Prop.forAll Generators.pinWidgetArb
      |> Check.QuickThrowOnFailure

  let test_yaml_pin_widget =
    testCase "PinWidget yaml serialization should work" <| fun _ ->
      yamlEncDec
      |> Prop.forAll Generators.pinWidgetArb
      |> Check.QuickThrowOnFailure

  //  ____  _       __  __                   _
  // |  _ \(_)_ __ |  \/  | __ _ _ __  _ __ (_)_ __   __ _
  // | |_) | | '_ \| |\/| |/ _` | '_ \| '_ \| | '_ \ / _` |
  // |  __/| | | | | |  | | (_| | |_) | |_) | | | | | (_| |
  // |_|   |_|_| |_|_|  |_|\__,_| .__/| .__/|_|_| |_|\__, |
  //                            |_|   |_|            |___/

  let test_binary_pin_mapping =
    testCase "PinMapping binary serialization should work" <| fun _ ->
      binaryEncDec<PinMapping>
      |> Prop.forAll Generators.pinMappingArb
      |> Check.QuickThrowOnFailure

  let test_yaml_pin_mapping =
    testCase "PinMapping yaml serialization should work" <| fun _ ->
      yamlEncDec
      |> Prop.forAll Generators.pinMappingArb
      |> Check.QuickThrowOnFailure

  //   ____                                          _ ____        _       _
  //  / ___|___  _ __ ___  _ __ ___   __ _ _ __   __| | __ )  __ _| |_ ___| |__
  // | |   / _ \| '_ ` _ \| '_ ` _ \ / _` | '_ \ / _` |  _ \ / _` | __/ __| '_ \
  // | |__| (_) | | | | | | | | | | | (_| | | | | (_| | |_) | (_| | || (__| | | |
  //  \____\___/|_| |_| |_|_| |_| |_|\__,_|_| |_|\__,_|____/ \__,_|\__\___|_| |_|

  let test_command_batch =
    testCase "StateMachineBatch serialization should work" <| fun _ ->
      binaryEncDec<StateMachineBatch>
      |> Prop.forAll Generators.commandBatchArb
      |> Check.QuickThrowOnFailure

  //  ____                            _
  // |  _ \ ___  __ _ _   _  ___  ___| |_
  // | |_) / _ \/ _` | | | |/ _ \/ __| __|
  // |  _ <  __/ (_| | |_| |  __/\__ \ |_
  // |_| \_\___|\__, |\__,_|\___||___/\__|
  //               |_|

  let test_correct_request_serialization =
    testCase "RequestResposse serialization should work" <| fun _ ->
      use reset = new WaitEvent()
      let rand = System.Random()

      let mutable count = 0

      let data = ResizeArray()
      let codata = ResizeArray()

      Prop.forAll Generators.requestArb (fun request ->
        data.Add request
        request
        |> Request.serialize
        |> codata.Add)
      |> Check.QuickThrowOnFailure

      let expected = data.Count

      let max =
        codata.ToArray()
        |> Array.map Array.length
        |> Array.max

      let check request (rerequest: Request) =
        Expect.equal rerequest request "Should be structurally equal"

        Interlocked.Increment &count |> ignore
        if count = expected then
          reset.Set() |> ignore

      let manager = BufferManager.create 1 max
      use builder = RequestBuilder.create <| fun requestId clientId body ->
        let request = data.Find (fun (request: Request) -> request.RequestId = requestId)
        body
        |> Request.make requestId clientId
        |> check request

      for binary in codata do
        for bte in binary do
          builder.Write bte

      waitFor "reset" reset |> noError

  //  ____                     ____  _        _
  // |  _ \ __ _ _ __ ___  ___/ ___|| |_ __ _| |_ ___
  // | |_) / _` | '__/ __|/ _ \___ \| __/ _` | __/ _ \
  // |  __/ (_| | |  \__ \  __/___) | || (_| | ||  __/
  // |_|   \__,_|_|  |___/\___|____/ \__\__,_|\__\___|

  let tests_parse_state_deserialization =
    testCase "ParseState deserialization should work" <| fun _ ->
      let rand = System.Random()
      let bufsize = 128
      let requests = ResizeArray()
      let rerequests = ResizeArray()
      let edges = ResizeArray()
      let blob = ResizeArray()
      use stopper = new WaitEvent()

      let collect (request: Request) =
        requests.Add request
        let data = Request.serialize request
        // adding some random padding in between to ensure robustness
        let padding = [| for n in 0 .. (rand.Next(5,20)) -> byte (rand.Next(0,255)) |]
        edges.Add blob.Count
        blob.AddRange data
        blob.AddRange padding

      // generate some test data
      collect
      |> Prop.forAll Generators.requestArb
      |> Check.Quick

      use parser = RequestBuilder.create <| fun request client body ->
        body
        |> Request.make request client
        |> rerequests.Add
        if rerequests.Count = requests.Count then
          stopper.Set() |> ignore

      let payload = blob.ToArray()
      let payloadSize = payload.Length
      let chunked = Array.chunkBySize bufsize payload
      let manager = BufferManager.create 10 bufsize

      let mutable read = 0
      for chunk in chunked do
        for bte in chunk do
          parser.Write bte
        Interlocked.Increment &read |> ignore

      waitFor "stopper" stopper |> noError

      Expect.equal rerequests.Count requests.Count "Should have the same count of requests"

      let rerequests = rerequests.ToArray()
      for request in requests do
        let rerequest = Array.tryFind ((=) request) rerequests
        Expect.equal rerequest (Some request) "Request and Rerequest should be equal"

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

  let test_validate_raftresponse_serialization =
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
          machine
          |> Config.create
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
      yamlEncDec
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
      yamlEncDec
      |> Prop.forAll Generators.cueArb
      |> Check.QuickThrowOnFailure

  let test_validate_cueref_binary_serialization =
    testCase "Validate CueReference Binary Serialization" <| fun _ ->
      mkCueRef () |> binaryEncDec

  let test_validate_cueref_yaml_serialization =
    testCase "Validate CueReference Yaml Serialization" <| fun _ ->
      mkCueRef () |> yamlEncDec

  let test_validate_cuegroup_binary_serialization =
    testCase "Validate CueGroup Binary Serialization" <| fun _ ->
      mkCueGroup () |> binaryEncDec

  let test_validate_cuegroup_yaml_serialization =
    testCase "Validate CueGroup Yaml Serialization" <| fun _ ->
      mkCueGroup () |> yamlEncDec

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
      yamlEncDec
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
      yamlEncDec
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
      yamlEncDec
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
      yamlEncDec
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
      yamlEncDec
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
      yamlEncDec
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
      yamlEncDec
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

  //     _          _ ____                            _
  //    / \   _ __ (_)  _ \ ___  __ _ _   _  ___  ___| |_
  //   / _ \ | '_ \| | |_) / _ \/ _` | | | |/ _ \/ __| __|
  //  / ___ \| |_) | |  _ <  __/ (_| | |_| |  __/\__ \ |_
  // /_/   \_\ .__/|_|_| \_\___|\__, |\__,_|\___||___/\__|
  //         |_|                   |_|

  let test_validate_api_request_binary_serialization =
    testCase "Validate ApiRequest Binary Serialization" <| fun _ ->
      binaryEncDec<ApiRequest>
      |> Prop.forAll Generators.apiRequestArb
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
      yamlEncDec
      |> Prop.forAll Generators.cuePlayerArb
      |> Check.QuickThrowOnFailure

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/

  let serializationTests =
    testList "Serialization Tests" [
      test_binary_pingroup_map
      test_binary_referenced_value
      test_yaml_referenced_value
      test_binary_pin_widget
      test_yaml_pin_widget
      test_binary_pin_mapping
      test_yaml_pin_mapping
      test_command_batch
      test_correct_request_serialization
      tests_parse_state_deserialization
      test_save_restore_raft_value_correctly
      test_validate_config_change
      test_validate_user_yaml_serialization
      test_validate_user_binary_serialization
      test_validate_slice_binary_serialization
      test_validate_slice_yaml_serialization
      test_validate_slices_binary_serialization
      test_validate_slices_yaml_serialization
      test_validate_client_binary_serialization
      test_validate_cue_binary_serialization
      test_validate_cue_yaml_serialization
      test_validate_cuelist_binary_serialization
      test_validate_cuelist_yaml_serialization
      test_validate_session_binary_serialization
      test_validate_session_yaml_serialization
      test_validate_pin_binary_serialization
      test_validate_pin_yaml_serialization
      test_validate_cueplayer_binary_serialization
      test_validate_cueplayer_yaml_serialization
      test_validate_group_binary_serialization
      test_validate_group_yaml_serialization
      test_validate_discovered_service_binary_serialization
      test_validate_project_binary_serialization
      test_validate_state_binary_serialization
      test_validate_raftrequest_serialization
      test_validate_raftresponse_serialization
      test_validate_state_machine_binary_serialization
      test_validate_api_request_binary_serialization
      // test_validate_project_yaml_serialization // FIXME: project yamls are different :/
    ]
