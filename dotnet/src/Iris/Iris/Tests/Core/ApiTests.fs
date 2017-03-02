namespace Iris.Tests

open System
open System.IO
open System.Threading
open Expecto
open Iris.Core
open Iris.Raft
open Iris.Client
open Iris.Service
open Iris.Service.Interfaces
open System.Net
open FSharpx.Control
open FSharpx.Functional

[<AutoOpen>]
module ApiTests =

  let mkState () =
    let machine = MachineConfig.create ()

    let project =
      { Id        = Id.Create()
        Name      = "Hello"
        Path      = Path.GetTempPath()
        CreatedOn = Time.createTimestamp()
        LastSaved = Some (Time.createTimestamp ())
        Copyright = None
        Author    = None
        Config    = Config.create "Hello" machine  }

    { Project  = project
      Patches  = Map.empty
      Cues     = Map.empty
      CueLists = Map.empty
      Sessions = Map.empty
      Users    = Map.empty
      Clients  = Map.empty
      DiscoveredServices = Map.empty }

  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  // |____/ \___|_|    \_/ \___|_|

  let test_server_should_replicate_state_snapshot_to_client =
    testCase "should replicate state snapshot on connect and SetState" <| fun _ ->
      either {
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create mem state.Project.Id

        do! server.Start()
        do! server.SetState state

        let srvr : IrisServer =
          { Id = Id.Create()
            Name = "cool"
            Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = mem.ApiPort + 1us }

        use! client = ApiClient.create srvr clnt

        let check = ref false

        let handler (ev: ClientEvent) =
          match ev with
          | ClientEvent.Snapshot -> check := true
          | _ -> ()

        use obs = client.Subscribe(handler)

        do! client.Start()

        Thread.Sleep 100

        expect "Should have received snapshot" true id !check

        check := false

        let! clientState = client.State

        expect "Should be equal" state id clientState

        let newstate = mkState ()

        do! server.SetState newstate

        Thread.Sleep 100

        expect "Should have received another snapshot" true id !check

        let! clientState = client.State

        expect "Should be equal" newstate id clientState
      }
      |> noError

  let test_server_should_replicate_state_machine_commands_to_client =
    testCase "should replicate state machine commands" <| fun _ ->
      either {
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create mem state.Project.Id

        do! server.Start()
        do! server.SetState state

        let srvr : IrisServer =
          { Id = Id.Create()
            Name = "cool"
            Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = mem.ApiPort + 1us }

        use! client = ApiClient.create srvr clnt

        let check = ref 0

        let handler (ev: ClientEvent) =
          match ev with
          | ClientEvent.Update _ -> check := !check + 1
          | _ -> ()

        use obs = client.Subscribe(handler)

        do! client.Start()

        Thread.Sleep 100

        let events = [
          AddCue     (mkCue ())
          AddUser    (mkUser ())
          AddSession (mkSession ())
          AddCue     (mkCue ())
          AddPin     (mkPin ())
          AddCueList (mkCueList ())
          AddMember  (mkMember ())
          AddUser    (mkUser ())
        ]

        List.iter (server.Update >> ignore) events

        Thread.Sleep 100

        expect "Should have emitted correct number of events" (List.length events) id !check

        let! serverState = server.State
        let! clientState = client.State

        expect "Should be equal" serverState id clientState
      }
      |> noError

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_client_should_replicate_state_machine_commands_to_server =
    testCase "client should replicate state machine commands to server" <| fun _ ->
      either {
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create mem state.Project.Id

        let check = ref 0

        let apiHandler (ev: ApiEvent) =
          match ev with
          | ApiEvent.Update sm ->
            check := !check + 1
            server.Update sm
          | _ -> ()

        use obs2 = server.Subscribe(apiHandler)

        do! server.Start()
        do! server.SetState state

        let srvr : IrisServer =
          { Id = Id.Create()
            Name = "cool"
            Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = mem.ApiPort + 1us }

        use! client = ApiClient.create srvr clnt
        do! client.Start()

        Thread.Sleep 100

        let pin = mkPin() // Toggle
        let cue = mkCue()
        let cuelist = mkCueList()

        do! client.AddPin pin
        do! client.AddCue cue
        do! client.AddCueList cuelist

        Thread.Sleep 100

        let! serverState = server.State
        let! clientState = client.State

        let len m = m |> Map.toArray |> Array.length

        expect "Should be equal" serverState id clientState
        expect "Server should have one cue" 1 len serverState.Cues
        expect "Client should have one cue" 1 len clientState.Cues
        expect "Server should have one cuelist" 1 len serverState.CueLists
        expect "Client should have one cuelist" 1 len clientState.CueLists

        do! client.UpdatePin (pin.SetSlice (BoolSlice { Index = 0u; Value = false }))
        do! client.UpdateCue { cue with Pins = [| mkPin() |] }
        do! client.UpdateCueList { cuelist with Cues = [| mkCue() |] }

        Thread.Sleep 100

        let! serverState = server.State
        let! clientState = client.State

        expect "Should be equal" serverState id clientState

        do! client.RemovePin pin
        do! client.RemoveCue cue
        do! client.RemoveCueList cuelist

        Thread.Sleep 100

        let! serverState = server.State
        let! clientState = client.State

        expect "Server should have zero cues" 0 len serverState.Cues
        expect "Client should have zero cues" 0 len clientState.Cues
        expect "Server should have zero cuelists" 0 len serverState.CueLists
        expect "Client should have zero cuelists" 0 len clientState.CueLists

        expect "Should be equal" serverState id clientState
      }
      |> noError

  //     _    _ _
  //    / \  | | |
  //   / _ \ | | |
  //  / ___ \| | |
  // /_/   \_\_|_|

  let apiTests =
    testList "API Tests" [
      test_server_should_replicate_state_snapshot_to_client
      test_server_should_replicate_state_machine_commands_to_client
      test_client_should_replicate_state_machine_commands_to_server
    ] |> testSequenced
