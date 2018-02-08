(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

namespace Disco.Tests

open System
open System.Threading
open Expecto
open Disco.Net
open Disco.Core
open Disco.Raft
open Disco.Client
open Disco.Service
open Disco.Service.Interfaces

[<AutoOpen>]
module ApiTests =

  let mkState () =
    let machine = MachineConfig.create "127.0.0.1" None

    let project =
      { Id        = DiscoId.Create()
        Name      = name "Hello"
        Path      = Path.getTempPath()
        CreatedOn = Time.createTimestamp()
        LastSaved = Some (Time.createTimestamp ())
        Copyright = None
        Author    = None
        Config    = Config.create machine  }

    { Project            = project
      PinGroups          = PinGroupMap.empty
      PinMappings        = Map.empty
      PinWidgets         = Map.empty
      Cues               = Map.empty
      CueLists           = Map.empty
      Sessions           = Map.empty
      Users              = Map.empty
      Clients            = Map.empty
      CuePlayers         = Map.empty
      FsTrees            = Map.empty
      DiscoveredServices = Map.empty }

  //  ____
  // / ___|  ___ _ ____   _____ _ __
  // \___ \ / _ \ '__\ \ / / _ \ '__|
  //  ___) |  __/ |   \ V /  __/ |
  // |____/ \___|_|    \_/ \___|_|

  let test_server_should_not_start_when_bind_fails =
    testCase "server should not start when bind fails" <| fun _ ->
      either {
        let mutable store = Store(mkState ())

        let mem = ClusterMember.create (DiscoId.Create())

        use! server1 = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot() = store.State
        }

        do! server1.Start()

        use! server2 = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot() = store.State
        }

        do! match server2.Start() with
            | Right () -> Left (Other("test","should have failed"))
            | Left _ -> Right()
      }
      |> noError

  let test_server_should_replicate_state_snapshot_to_client =
    testCase "server should replicate state snapshot to client" <| fun _ ->
      either {
        let mutable store = Store(mkState ())

        let mem = ClusterMember.create (DiscoId.Create())

        use! server = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot() = store.State
        }

        let serverHandler = function
          | DiscoEvent.Append(origin, cmd) -> store.Dispatch cmd
          | other -> ignore other

        use sobs = server.Subscribe(serverHandler)

        do! server.Start()

        let srvr : DiscoServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddress }

        let clnt : DiscoClient =
          { Id = DiscoId.Create()
            Name = name "client cool"
            Role = Role.Renderer
            ServiceId = mem.Id
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddress
            Port = port (unwrap mem.ApiPort + 1us) }

        let client = ApiClient.create srvr clnt

        use registered = new WaitEvent()
        use unregistered = new WaitEvent()
        use snapshot = new WaitEvent()

        let handler = function
          | ClientEvent.Registered   -> registered.Set() |> ignore
          | ClientEvent.UnRegistered -> unregistered.Set() |> ignore
          | ClientEvent.Snapshot     -> snapshot.Set() |> ignore
          | _ -> ()

        use cobs = client.Subscribe(handler)

        do! client.Start()

        do! waitFor "registered" registered
        do! waitFor "snapshot (1)" snapshot

        expect "Should be equal" store.State id client.State

        store <- Store(mkState ())
        // snapshot.Reset() |> ignore
        server.SendSnapshot()

        do! waitFor "snapshot (2)" snapshot

        expect "Should be equal" store.State id client.State

        dispose client

        do! waitFor "unregistered" unregistered
      }
      |> noError

  let test_server_should_replicate_state_machine_commands_to_client =
    testCase "should replicate state machine commands to client" <| fun _ ->
      either {
        let store = Store(mkState ())

        let mem = ClusterMember.create (DiscoId.Create())

        use! server = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot () = store.State
        }

        let serverHandler = function
          | DiscoEvent.Append(origin, cmd)  -> store.Dispatch cmd
          | other -> ignore other

        use sobs = server.Subscribe(serverHandler)

        do! server.Start()

        let srvr : DiscoServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddress }

        let clnt : DiscoClient =
          { Id = DiscoId.Create()
            Name = name "client cool"
            Role = Role.Renderer
            ServiceId = mem.Id
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddress
            Port = port (unwrap mem.ApiPort + 1us) }

        use client = ApiClient.create srvr clnt

        use snapshot = new WaitEvent()
        use doneCheck = new WaitEvent()

        let events = [
          AddCue     (mkCue ())
          AddUser    (mkUser ())
          AddSession (mkSession ())
          AddCue     (mkCue ())
          AddPin     (mkPin ())
          AddCueList (mkCueList ())
          AddUser    (mkUser ())
        ]

        let mutable check = 0L

        let clientHandler = function
          | ClientEvent.Snapshot -> snapshot.Set() |> ignore
          | ClientEvent.Update _ ->
            Threading.Interlocked.Increment &check |> ignore
            if int check = List.length events then
              doneCheck.Set() |> ignore
          | _ -> ()

        use cobs = client.Subscribe(clientHandler)

        do! client.Start()

        do! waitFor "snapshot" snapshot

        List.iter
          (fun cmd ->
            server.Update Origin.Raft cmd
            store.Dispatch cmd)
          events

        do! waitFor "doneCheck" doneCheck

        expect "Should have emitted correct number of events" (List.length events |> int64) id check
        expect "Should be equal" store.State id client.State
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
        use clientRegistered = new WaitEvent()
        use clientSnapshot = new WaitEvent()
        use clientUpdate = new WaitEvent()

        let store = Store(mkState ())

        let mem = ClusterMember.create (DiscoId.Create())

        let srvr : DiscoServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddress }

        let clnt : DiscoClient =
          { Id = DiscoId.Create()
            Name = name "client cool"
            Role = Role.Renderer
            ServiceId = mem.Id
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddress
            Port = port (unwrap mem.ApiPort + 1us) }

        use! server = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot () = store.State
        }

        let check = ref 0

        let apiHandler = function
          | DiscoEvent.Append(_, (AddPin _ as sm))
          | DiscoEvent.Append(_, (AddCue _ as sm))
          | DiscoEvent.Append(_, (AddCueList _ as sm))
          | DiscoEvent.Append(_, (UpdatePin _ as sm))
          | DiscoEvent.Append(_, (UpdateCue _ as sm))
          | DiscoEvent.Append(_, (UpdateCueList _ as sm))
          | DiscoEvent.Append(_, (RemovePin _ as sm))
          | DiscoEvent.Append(_, (RemoveCue _ as sm))
          | DiscoEvent.Append(_, (RemoveCueList _ as sm)) ->
            check := !check + 1
            store.Dispatch sm
            server.Update Origin.Raft sm
          | _ -> ()

        use obs2 = server.Subscribe(apiHandler) //

        use client = ApiClient.create srvr clnt

        let clientHandler = function
          | ClientEvent.Registered -> clientRegistered.Set() |> ignore
          | ClientEvent.Snapshot -> clientSnapshot.Set() |> ignore
          | ClientEvent.Update _ -> clientUpdate.Set() |> ignore
          | _ -> ()

        use obs3 = client.Subscribe(clientHandler)

        do! server.Start()
        do! client.Start()

        do! waitFor "clientRegisterd" clientRegistered
        do! waitFor "clientSnaphot" clientSnapshot

        let pin = mkPin() // Toggle
        let cue = mkCue()
        let cuelist = mkCueList()

        client.AddPin pin

        do! waitFor "clientUpdate" clientUpdate

        client.AddCue cue

        do! waitFor "clientUpdate" clientUpdate

        client.AddCueList cuelist

        do! waitFor "clientUpdate" clientUpdate

        let len m = m |> Map.toArray |> Array.length

        expect "Should be equal" store.State id client.State
        expect "Server should have one cue" 1 len store.State.Cues
        expect "Client should have one cue" 1 len client.State.Cues
        expect "Server should have one cuelist" 1 len store.State.CueLists
        expect "Client should have one cuelist" 1 len client.State.CueLists

        client.UpdatePin (Pin.setSlice (BoolSlice(0<index>, false, false)) pin)

        do! waitFor "clientUpdate" clientUpdate

        client.UpdateCue { cue with Slices = mkSlices() }

        do! waitFor "clientUpdate" clientUpdate

        client.UpdateCueList { cuelist with Items = mkCueListItems() }

        do! waitFor "clientUpdate" clientUpdate

        expect "Should be equal" store.State id client.State //

        client.RemovePin pin

        do! waitFor "clientUpdate" clientUpdate

        client.RemoveCue cue

        do! waitFor "clientUpdate" clientUpdate

        client.RemoveCueList cuelist

        do! waitFor "clientUpdate" clientUpdate

        expect "Server should have zero cues" 0 len store.State.Cues
        expect "Client should have zero cues" 0 len client.State.Cues
        expect "Server should have zero cuelists" 0 len store.State.CueLists
        expect "Client should have zero cuelists" 0 len client.State.CueLists

        expect "Should be equal" store.State id client.State
      }
      |> noError

  let test_server_should_dispose_properly =
    testCase "server should dispose properly" <| fun _ ->
      either {
        let store = Store(mkState ())
        let mem = ClusterMember.create (DiscoId.Create())

        use! server = ApiServer.create mem {
          new IApiServerCallbacks with
            member self.PrepareSnapshot() = store.State
        }
        do! server.Start()
      }
      |> noError

  let test_client_should_dispose_properly =
    testCase "client should dispose properly" <| fun _ ->
      either {
        let machine = MachineConfig.create "127.0.0.1" None
        let mem = Machine.toClusterMember machine

        let srvr : DiscoServer =
          { Port = mem.ApiPort
            IpAddress = mem.IpAddress }

        let clnt : DiscoClient =
          { Id = DiscoId.Create()
            Name = name "client cool"
            Role = Role.Renderer
            ServiceId = mem.Id
            Status = ServiceStatus.Starting
            IpAddress = mem.IpAddress
            Port = port (unwrap mem.ApiPort + 1us) }

        use client = ApiClient.create srvr clnt
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
      test_server_should_not_start_when_bind_fails
      test_server_should_dispose_properly
      test_client_should_dispose_properly
    ] |> testSequenced
