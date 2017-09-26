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

module StateShouldBeCleanedOnClientRemove =

  let test =
    testCase "state should be clean on client remove" <| fun _ ->
      either {
        use started = new WaitEvent()
        use batchDone = new WaitEvent()
        use clientRegistered = new WaitEvent()
        use unRegistered = new WaitEvent()
        use clientBatchDone = new WaitEvent()

        let! (project, zipped) = mkCluster 1

        let clientId = IrisId.Create()

        let mem1, machine1 = List.head zipped

        //  _
        // / |
        // | |
        // | |
        // |_| add pin to project

        let group1 =
          { Id = IrisId.Create()
            Name = name "My Cool Group 2"
            ClientId = clientId
            Path = None
            RefersTo = None
            Pins = Map.empty }

        let toggle1 =
          Pin.Sink.toggle
            (IrisId.Create())
            (name "My Toggle 1")
            group1.Id
            group1.ClientId
            [| true |]

        let toggle2 =
          Pin.Sink.toggle
            (IrisId.Create())
            (name "My Toggle 2")
            group1.Id
            group1.ClientId
            [| true |]

        let group1 =
          { group1 with
              Pins =
                Map.ofList [
                  (toggle1.Id, toggle1)
                  (toggle2.Id, Pin.setPersisted true toggle2)
                ] }

        let group2 =
          { Id = IrisId.Create()
            Name = name "My Cool Group 2"
            ClientId = clientId
            Path = None
            RefersTo = None
            Pins = Map.empty }

        let toggle3 =
          Pin.Sink.toggle
            (IrisId.Create())
            (name "My Toggle 3")
            group2.Id
            group2.ClientId
            [| true |]

        let group2 = { group2 with Pins = Map.ofList [(toggle3.Id, toggle3)] }

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| load and start

        use! service = IrisService.create {
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
          | IrisEvent.Append(Origin.Raft, RemoveClient _) -> unRegistered.Set()
          | _ -> ())
          |> service.Subscribe

        do! service.Start()
        do! waitFor "started" started

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ create an API client

        let serverOptions:IrisServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddr
        }

        let client = ApiClient.create serverOptions {
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
          | ClientEvent.Update (AddPinGroup  _) -> clientBatchDone.Set()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)
        do! client.Start()

        do! waitFor "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| add groups

        client.AddPinGroup group1
        do! waitFor "add PinGroup done" clientBatchDone

        client.AddPinGroup group2
        do! waitFor "add PinGroup done" clientBatchDone

        expect "Should have volatile pin in service state" true
          (PinGroupMap.findPin toggle1.Id >> Map.isEmpty >> not)
          service.State.PinGroups

        expect "Should have persisted pin from service state" true
          (PinGroupMap.findPin toggle2.Id >> Map.isEmpty >> not)
          service.State.PinGroups

        expect "Should have other volatile pin from service state" true
          (PinGroupMap.findPin toggle3.Id >> Map.isEmpty >> not)
          service.State.PinGroups

        ///  ____
        /// | ___|
        /// |___ \
        ///  ___) |
        /// |____/ dispose client

        dispose client
        do! waitFor "unRegistered" unRegistered

        expect "Should not have volatile pin in service state" true
          (PinGroupMap.findPin toggle1.Id >> Map.isEmpty)
          service.State.PinGroups

        expect "Should still have persisted pin in service state" true
          (PinGroupMap.findPin toggle2.Id >> Map.isEmpty >> not)
          service.State.PinGroups

        expect "persisted pin should be marked offline" true
          (PinGroupMap.findPin toggle2.Id
           >> Map.fold
              (fun result _ pin -> if result then result else Pin.isOffline pin)
              false)
          service.State.PinGroups

        expect "Should not have other volatile pin in service state" true
          (PinGroupMap.findPin toggle3.Id >> Map.isEmpty)
          service.State.PinGroups

      }
      |> noError
