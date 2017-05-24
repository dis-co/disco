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
open ZeroMQ
open FSharpx.Control
open FSharpx.Functional

[<AutoOpen>]
module ApiTests =

  let mkState () =
    let machine = MachineConfig.create "127.0.0.1" None

    let project =
      { Id        = Id.Create()
        Name      = name "Hello"
        Path      = Path.getTempPath() |> Project.ofFilePath
        CreatedOn = Time.createTimestamp()
        LastSaved = Some (Time.createTimestamp ())
        Copyright = None
        Author    = None
        Config    = Config.create "Hello" machine  }

    { Project            = project
      PinGroups          = Map.empty
      Cues               = Map.empty
      CueLists           = Map.empty
      Sessions           = Map.empty
      Users              = Map.empty
      Clients            = Map.empty
      CuePlayers         = Map.empty
      DiscoveredServices = Map.empty }

  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  // |____/ \___|_|    \_/ \___|_|

  let test_server_should_replicate_state_snapshot_to_client =
    testCase "should replicate state snapshot on connect and SetState" <| fun _ ->
      either {
        use ctx = new ZContext()
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create ctx mem state.Project.Id

        do! server.Start()

        server.State <- state

        let srvr : IrisServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = port (unwrap mem.ApiPort + 1us) }

        let client = ApiClient.create ctx srvr clnt

        use registered = new AutoResetEvent(false)
        use unregistered = new AutoResetEvent(false)
        use snapshot = new AutoResetEvent(false)

        let handler = function
          | ClientEvent.Registered   -> registered.Set() |> ignore
          | ClientEvent.UnRegistered -> unregistered.Set() |> ignore
          | ClientEvent.Snapshot     -> snapshot.Set() |> ignore
          | _ -> ()

        use obs = client.Subscribe(handler)

        do! client.Start()

        do! waitOrDie "registered" registered
        do! waitOrDie "snapshot" snapshot

        expect "Should be equal" state id client.State

        let newstate = mkState ()

        server.State <- newstate

        do! waitOrDie "snapshot" snapshot

        expect "Should be equal" newstate id client.State

        dispose client

        do! waitOrDie "unregistered" unregistered
      }
      |> noError

  let test_server_should_replicate_state_machine_commands_to_client =
    testCase "should replicate state machine commands" <| fun _ ->
      either {
        use ctx = new ZContext()
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create ctx mem state.Project.Id

        do! server.Start()
        server.State <- state

        let srvr : IrisServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = port (unwrap mem.ApiPort + 1us) }

        use client = ApiClient.create ctx srvr clnt

        use snapshot = new AutoResetEvent(false)
        use doneCheck = new AutoResetEvent(false)

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

        let mutable check = 0L

        let handler (ev: ClientEvent) =
          match ev with
          | ClientEvent.Snapshot -> snapshot.Set() |> ignore
          | ClientEvent.Update _ ->
            Threading.Interlocked.Increment &check |> ignore
            if int check = List.length events then
              doneCheck.Set() |> ignore
          | _ -> ()

        use obs = client.Subscribe(handler)

        do! client.Start()

        do! waitOrDie "shaptwhot" snapshot

        List.iter (server.Update >> ignore) events

        do! waitOrDie "doneCheck" doneCheck

        expect "Should have emitted correct number of events" (List.length events |> int64) id check
        expect "Should be equal" server.State id client.State
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
        use ctx = new ZContext()
        let state = mkState ()

        let mem = Member.create (Id.Create())

        let srvr : IrisServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = port (unwrap mem.ApiPort + 1us) }

        use! server = ApiServer.create ctx mem state.Project.Id

        let check = ref 0

        let apiHandler (ev: ApiEvent) =
          match ev with
          | ApiEvent.Update sm ->
            check := !check + 1
            server.Update sm
          | _ -> ()

        use obs2 = server.Subscribe(apiHandler)
        use clientRegistered = new AutoResetEvent(false)
        use clientSnapshot = new AutoResetEvent(false)
        use clientUpdate = new AutoResetEvent(false)

        use client = ApiClient.create ctx srvr clnt

        let clientHandler = function
          | ClientEvent.Registered -> clientRegistered.Set() |> ignore
          | ClientEvent.Snapshot -> clientSnapshot.Set() |> ignore
          | ClientEvent.Update _ -> clientUpdate.Set() |> ignore
          | _ -> ()

        use obs3 = client.Subscribe(clientHandler)

        do! server.Start()
        server.State <- state

        do! client.Start()

        do! waitOrDie "clientRegisterd" clientRegistered
        do! waitOrDie "clientSnaphot" clientSnapshot

        let pin = mkPin() // Toggle
        let cue = mkCue()
        let cuelist = mkCueList()

        client.AddPin pin

        do! waitOrDie "clientUpdate" clientUpdate

        client.AddCue cue

        do! waitOrDie "clientUpdate" clientUpdate

        client.AddCueList cuelist

        do! waitOrDie "clientUpdate" clientUpdate

        let len m = m |> Map.toArray |> Array.length

        expect "Should be equal" server.State id client.State
        expect "Server should have one cue" 1 len server.State.Cues
        expect "Client should have one cue" 1 len client.State.Cues
        expect "Server should have one cuelist" 1 len server.State.CueLists
        expect "Client should have one cuelist" 1 len client.State.CueLists

        client.UpdatePin (Pin.setSlice (BoolSlice(index 0, false)) pin)

        do! waitOrDie "clientUpdate" clientUpdate

        client.UpdateCue { cue with Slices = mkSlices() }

        do! waitOrDie "clientUpdate" clientUpdate

        client.UpdateCueList { cuelist with Groups = [| mkCueGroup() |] }

        do! waitOrDie "clientUpdate" clientUpdate

        expect "Should be equal" server.State id client.State

        client.RemovePin pin

        do! waitOrDie "clientUpdate" clientUpdate

        client.RemoveCue cue

        do! waitOrDie "clientUpdate" clientUpdate

        client.RemoveCueList cuelist

        do! waitOrDie "clientUpdate" clientUpdate

        expect "Server should have zero cues" 0 len server.State.Cues
        expect "Client should have zero cues" 0 len client.State.Cues
        expect "Server should have zero cuelists" 0 len server.State.CueLists
        expect "Client should have zero cuelists" 0 len client.State.CueLists

        expect "Should be equal" server.State id client.State
      }
      |> noError

  let test_server_should_dispose_properly =
    testCase "server should dispose properly" <| fun _ ->
      either {
        use ctx = new ZContext()
        let state = mkState ()
        let mem = Member.create (Id.Create())

        use! server = ApiServer.create ctx mem state.Project.Id
        do! server.Start()
      }
      |> noError

  let test_client_should_dispose_properly =
    testCase "client should dispose properly" <| fun _ ->
      either {
        use ctx = new ZContext()

        let mem = Member.create (Id.Create())

        let srvr : IrisServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddr }

        let clnt : IrisClient =
          { Id = Id.Create()
            Name = "client cool"
            Role = Role.Renderer
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddr
            Port = port (unwrap mem.ApiPort + 1us) }

        use client = ApiClient.create ctx srvr clnt
        do! client.Start()
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
      test_server_should_dispose_properly
      test_client_should_dispose_properly
    ] |> testSequenced
