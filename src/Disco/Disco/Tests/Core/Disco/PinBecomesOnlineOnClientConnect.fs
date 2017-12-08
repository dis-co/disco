namespace Disco.Tests

open System.IO
open System.Threading
open Expecto

open Disco.Core
open Disco.Service
open Disco.Client
open Disco.Client.Interfaces
open Disco.Service.Interfaces
open Disco.Raft
open Disco.Net

open Common

module PinBecomesOnlineOnClientConnect =

  let test =
    testCase "pin becomes online on client connect" <| fun _ ->
      either {
        use started = new WaitEvent()
        use appendDone = new WaitEvent()
        use clientRegistered = new WaitEvent()
        use clientAppendDone = new WaitEvent()

        let! (project, zipped) = mkCluster 1

        let clientId = DiscoId.Create()

        let mem1, machine1 = List.head zipped

        //  _
        // / |
        // | |
        // | |
        // |_| add pin to project

        let group =
          { Id = DiscoId.Create()
            Name = name "My Cool Group"
            ClientId = clientId
            Path = None
            RefersTo = None
            Pins = Map.empty }

        let toggle =
          Pin.Sink.toggle
            (DiscoId.Create())
            (name "My Toggle")
            group.Id
            group.ClientId
            [| true |]

        let group =
          { group with
              Pins = Map.add toggle.Id (Pin.setPersisted true toggle) group.Pins }

        do! Asset.save project.Path group

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| load and start

        use! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
          | DiscoEvent.Started ServiceType.Raft           -> started.Set()
          | DiscoEvent.Append(Origin.Raft, AddPinGroup _) -> appendDone.Set()
          | DiscoEvent.Append(_, CallCue _)               -> appendDone.Set()
          | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitFor "started" started

        expect "Should have loaded the Group" true
          (PinGroupMap.containsGroup toggle.ClientId toggle.PinGroupId)
          service1.State.PinGroups

        expect "Should have marked pin as offline" true
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isOffline)
          service1.State.PinGroups

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ create an API client

        let server:DiscoServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddress
        }

        use client = ApiClient.create server {
          Id = clientId
          Name = name "hi"
          Role = Role.Renderer
          ServiceId = mem1.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Set()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)
        do! client.Start()

        do! waitFor "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| append group and check its marked 'online'

        client.AddPinGroup group

        do! waitFor "appendDone" appendDone
        do! waitFor "clientAppendDone" clientAppendDone

        expect "Should have marked pin as online" true
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isOnline)
          service1.State.PinGroups
      }
      |> noError
