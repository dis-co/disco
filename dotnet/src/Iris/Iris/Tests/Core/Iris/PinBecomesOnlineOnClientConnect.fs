namespace Iris.Tests

open System.IO
open System.Threading
open Expecto

open Iris.Core
open Iris.Service
open Iris.Client
open Iris.Client.Interfaces
open Iris.Service.Interfaces
open Iris.Raft
open Iris.Net

open Common

module PinBecomesOnlineOnClientConnect =

  let test =
    testCase "pin becomes online on client connect" <| fun _ ->
      either {
        let started = WaitCount.Create()
        let appendDone = WaitCount.Create()
        let clientRegistered = WaitCount.Create()
        let clientAppendDone = WaitCount.Create()

        let! (project, zipped) = mkCluster 1
        let mem1, machine1 = List.head zipped

        //  _
        // / |
        // | |
        // | |
        // |_| add pin to project

        let group =
          { Id = Id "my cool group"
            Name = name "My Cool Group"
            Client = mem1.Id
            Path = None
            RefersTo = None
            Pins = Map.empty }

        let toggle =
          Pin.Sink.toggle
            (Id "/my/pin")
            (name "My Toggle")
            group.Id
            Array.empty
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

        use! service1 = IrisService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
          | IrisEvent.Started ServiceType.Raft           -> started.Increment()
          | IrisEvent.Append(Origin.Raft, AddPinGroup _) -> appendDone.Increment()
          | IrisEvent.Append(_, CallCue _)               -> appendDone.Increment()
          | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitFor "started to be 1" started 1

        expect "Should have loaded the Group" true
          (Map.containsKey toggle.PinGroup)
          service1.State.PinGroups

        expect "Should have marked pin as offline" true
          (Map.find group.Id >> flip PinGroup.findPin toggle.Id >> Pin.isOffline)
          service1.State.PinGroups

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ create an API client

        let server:IrisServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddr
        }

        use client = ApiClient.create server {
          Id = Id.Create()
          Name = name "hi"
          Role = Role.Renderer
          ServiceId = mem1.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Increment()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Increment()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)
        do! client.Start()

        do! waitFor "clientRegistered to be 1" clientRegistered 1

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| append group and check its marked 'online'

        client.AddPinGroup group

        do! waitFor "appendDone to be 1" appendDone 1
        do! waitFor "clientAppendDone to be 2" clientAppendDone 1

        expect "Should have marked pin as online" true
          (Map.find group.Id >> flip PinGroup.findPin toggle.Id >> Pin.isOnline)
          service1.State.PinGroups
      }
      |> noError
