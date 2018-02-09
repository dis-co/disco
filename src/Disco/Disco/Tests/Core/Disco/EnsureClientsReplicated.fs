(*
 *  This file is part of Distributed Show Control
 *
 *  Copyright 2015, 2018 by it's authors.
 *  Some rights reserved. See COPYING, AUTHORS.
 *)

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

module EnsureClientsReplicated =

  let test =
    testCase "ensure connected clients are forwarded to leader" <| fun _ ->
      result {
        use electionDone = new WaitEvent()
        use addClientDone = new WaitEvent()
        use appendDone = new WaitEvent()
        use clientRegistered = new WaitEvent()
        use clientAppendDone = new WaitEvent()
        use updateDone = new WaitEvent()
        use pushDone = new WaitEvent()

        let! (project, zipped) = mkCluster 2

        let serverHandler id = function
          | DiscoEvent.GitPush _                        -> pushDone.Set()
          | DiscoEvent.StateChanged(oldst, Leader)      -> electionDone.Set()
          | DiscoEvent.Append(Origin.Raft, AddClient _) -> addClientDone.Set()
          | DiscoEvent.Append(Origin.Raft, msg)         -> appendDone.Set()
          | _ -> ()

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Set()
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Set()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set()
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Set()
          | _ -> ()

        //  ____                  _            _
        // / ___|  ___ _ ____   _(_) ___ ___  / |
        // \___ \ / _ \ '__\ \ / / |/ __/ _ \ | |
        //  ___) |  __/ |   \ V /| | (_|  __/ | |
        // |____/ \___|_|    \_/ |_|\___\___| |_|

        let mem1, machine1 = List.head zipped

        use! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe (serverHandler machine1.MachineId)

        do! service1.Start()

        //  ____                  _            ____
        // / ___|  ___ _ ____   _(_) ___ ___  |___ \
        // \___ \ / _ \ '__\ \ / / |/ __/ _ \   __) |
        //  ___) |  __/ |   \ V /| | (_|  __/  / __/
        // |____/ \___|_|    \_/ |_|\___\___| |_____|

        let mem2, machine2 = List.last zipped

        use! service2 = DiscoService.create {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe (serverHandler machine2.MachineId)

        do! service2.Start()
        do! waitFor "electionDone" electionDone

        //   ____ _ _            _     _
        //  / ___| (_) ___ _ __ | |_  / |
        // | |   | | |/ _ \ '_ \| __| | |
        // | |___| | |  __/ | | | |_  | |
        //  \____|_|_|\___|_| |_|\__| |_|

        let serverAddress1:DiscoServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddress
        }

        use client1 = ApiClient.create serverAddress1 {
          Id = DiscoId.Create()
          Name = name "Client 1"
          Role = Role.Renderer
          ServiceId = mem1.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        use clobs1 = client1.Subscribe (handleClient)
        do! client1.Start()

        do! waitFor "clientRegistered" clientRegistered
        do! waitFor "addClientDone" addClientDone

        //   ____ _ _            _     ____
        //  / ___| (_) ___ _ __ | |_  |___ \
        // | |   | | |/ _ \ '_ \| __|   __) |
        // | |___| | |  __/ | | | |_   / __/
        //  \____|_|_|\___|_| |_|\__| |_____|

        let serverAddress2:DiscoServer = {
          Port = mem2.ApiPort
          IpAddress = mem2.IpAddress
        }

        use client2 = ApiClient.create serverAddress2 {
          Id = DiscoId.Create()
          Name = name "Client 2"
          Role = Role.Renderer
          ServiceId = mem2.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        use clobs2 = client2.Subscribe (handleClient)
        do! client2.Start()

        do! waitFor "clientRegistered" clientRegistered
        do! waitFor "addClientDone" addClientDone

        //  _____         _
        // |_   _|__  ___| |_ ___
        //   | |/ _ \/ __| __/ __|
        //   | |  __/\__ \ |_\__ \
        //   |_|\___||___/\__|___/

        // we need to wait twice more for the append commands that have been forwardecd issued
        do! waitFor "addClientDone" addClientDone
        do! waitFor "addClientDone" addClientDone

        expect "Service 1 and Service 2 should have both 2 Clients"
          service1.State.Clients
          id
          service2.State.Clients
      }
      |> noError
