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

module PinBecomesDirty =

  let test =
    testCase "pin becomes dirty" <| fun _ ->
      either {
        use started = new WaitEvent()
        use updateDone = new WaitEvent()
        use saveDone = new WaitEvent()
        use clientRegistered = new WaitEvent()

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
          | DiscoEvent.Started ServiceType.Raft  -> started.Set()
          | DiscoEvent.Append(_, UpdateSlices _) -> updateDone.Set()
          | DiscoEvent.Append(_, Command AppCommand.Save) -> saveDone.Set()
          | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitFor "started" started

        expect "Should have loaded the Group" true
          (PinGroupMap.containsGroup toggle.ClientId toggle.PinGroupId)
          service1.State.PinGroups

        expect "Should have marked pin as clean" true
          (PinGroupMap.tryFindGroup group.ClientId group.Id
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
          BoolSlices(toggle.Id, None, false, [| false |])
        ]

        do! waitFor "updateDone" updateDone

        expect "Should have marked pin as dirty in service state" true
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty)
          service1.State.PinGroups

        ///  ____
        /// | ___|
        /// |___ \
        ///  ___) |
        /// |____/ saving should reset to clean

        AppCommand.Save
        |> Command
        |> service1.Append

        do! waitFor "saveDone" saveDone

        expect "Should have marked pin as clean in service state" true
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin toggle.Id)
           >> Option.get
           >> Pin.isDirty
           >> not)
          service1.State.PinGroups
      }
      |> noError
