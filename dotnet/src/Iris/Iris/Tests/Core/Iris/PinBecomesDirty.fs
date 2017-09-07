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

module PinBecomesDirty =

  let test =
    testCase "pin becomes dirty" <| fun _ ->
      either {
        use started = new WaitEvent()
        use batchDone = new WaitEvent()
        use clientRegistered = new WaitEvent()
        use clientBatchDone = new WaitEvent()

        let! (project, zipped) = mkCluster 1

        let clientId = Id.Create()

        let mem1, machine1 = List.head zipped

        //  _
        // / |
        // | |
        // | |
        // |_| add pin to project

        let group =
          { Id = Id "my cool group"
            Name = name "My Cool Group"
            Client = clientId
            Path = None
            RefersTo = None
            Pins = Map.empty }

        let toggle =
          Pin.Sink.toggle
            (Id "/my/pin")
            (name "My Toggle")
            group.Id
            group.Client
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
          | IrisEvent.Started ServiceType.Raft  -> started.Set()
          | IrisEvent.Append(Origin.Raft, CommandBatch _) -> batchDone.Set()
          | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitFor "started" started

        expect "Should have loaded the Group" true
          (PinGroupMap.containsGroup toggle.Client toggle.PinGroup)
          service1.State.PinGroups

        expect "Should have marked pin as clean" true
          (PinGroupMap.tryFindGroup group.Client group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty
           >> not)
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
          | ClientEvent.Update (CommandBatch _) -> clientBatchDone.Set()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)
        do! client.Start()

        do! waitFor "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| append group and check its marked 'online'

        client.UpdateSlices [
          BoolSlices(toggle.Id, None, [| false |])
        ]

        do! waitFor "batchDone" batchDone
        do! waitFor "clientBatchDone" clientBatchDone

        expect "Should have marked pin as dirty in service state" true
          (PinGroupMap.tryFindGroup group.Client group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty)
          service1.State.PinGroups

        expect "Should have marked pin as dirty in client state" true
          (PinGroupMap.tryFindGroup group.Client group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty)
          client.State.PinGroups

        ///  ____
        /// | ___|
        /// |___ \
        ///  ___) |
        /// |____/ saving should reset to clean

        AppCommand.Save
        |> Command
        |> service1.Append

        do! waitFor "batchDone" batchDone
        do! waitFor "clientBatchDone" clientBatchDone

        expect "Should have marked pin as clean in service state" true
          (PinGroupMap.tryFindGroup group.Client group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty
           >> not)
          service1.State.PinGroups

        expect "Should have marked pin as clean in client state" true
          (PinGroupMap.tryFindGroup group.Client group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty
           >> not)
          client.State.PinGroups

      }
      |> noError
