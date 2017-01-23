namespace Iris.Tests

open System
open System.IO
open System.Threading
open Expecto
open Iris.Core
open Iris.Raft
open Iris.Client
open Iris.Service
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
      Users    = Map.empty }

  //  _____         _
  // |_   _|__  ___| |_ ___
  //   | |/ _ \/ __| __/ __|
  //   | |  __/\__ \ |_\__ \
  //   |_|\___||___/\__|___/

  let test_should_replicate_state_snapshot =
    testCase "should replicate state snapshot on connect and SetState" <| fun _ ->
      either {
        let state = mkState ()

        let mem = Member.create (Id.Create())

        use! server = ApiServer.create mem

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

  //     _    _ _
  //    / \  | | |
  //   / _ \ | | |
  //  / ___ \| | |
  // /_/   \_\_|_|

  let apiTests =
    testList "API Tests" [
      test_should_replicate_state_snapshot
    ] |> testSequenced
