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

module EnsureCueResolver =

  let test =
    testCase "ensure cue resolver works" <| fun _ ->
      either {
        use checkGitStarted = new WaitEvent()
        use electionDone = new WaitEvent()
        use appendDone = new WaitEvent()
        use clientRegistered = new WaitEvent()
        use clientAppendDone = new WaitEvent()
        use updateDone = new WaitEvent()

        let! (project, zipped) = mkCluster 1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem1, machine1 = List.head zipped

        use! service1 = DiscoService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
            | DiscoEvent.Started ServiceType.Git            -> checkGitStarted.Set()
            | DiscoEvent.StateChanged(oldst, Leader)        -> electionDone.Set()
            | DiscoEvent.Append(Origin.Raft, AddPinGroup _) -> appendDone.Set()
            | DiscoEvent.Append(_, CallCue _)               -> appendDone.Set()
            | _ -> ())
          |> service1.Subscribe

        do! service1.Start()

        do! waitFor "checkGitStarted" checkGitStarted
        do! waitFor "electionDone" electionDone

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| create an API client

        let server:DiscoServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddress
        }

        use client = ApiClient.create server {
          Id = DiscoId.Create()
          Name = name "hi"
          Role = Role.Renderer
          ServiceId = mem1.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Set()
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Set()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set()
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Set()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)

        do! client.Start()

        do! waitFor "clientRegistered" clientRegistered

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ do some work

        let pinId = DiscoId.Create()
        let groupId = DiscoId.Create()

        let pin = BoolPin {
          Id               = pinId
          Name             = name "hi"
          PinGroupId       = groupId
          ClientId         = client.Id
          Tags             = Array.empty
          PinConfiguration = PinConfiguration.Sink
          IsTrigger        = false
          Persisted        = false
          Online           = true
          Dirty            = false
          VecSize          = VecSize.Dynamic
          Labels           = Array.empty
          Values           = [| true |]
        }

        let group = {
          Id = groupId
          Name = name "whatevva"
          ClientId = client.Id
          Path = None
          RefersTo = None
          Pins = Map.ofList [(pin.Id, pin)]
        }

        client.AddPinGroup group

        do! waitFor "appendDone" appendDone
        do! waitFor "clientAppendDone" clientAppendDone

        let cue = {
          Id = DiscoId.Create()
          Name = name "hi"
          Slices = [| BoolSlices(pin.Id, None, false, [| false |]) |]
        }

        cue
        |> CallCue
        |> service1.Append

        do! waitFor "appendDone" appendDone
        do! waitFor "updateDone" updateDone

        let actual: Slices =
          client.State.PinGroups
          |> PinGroupMap.tryFindGroup client.Id groupId
          |> Option.bind (PinGroup.tryFindPin pin.Id)
          |> Option.get
          |> fun (pin:Pin) -> pin.Slices

       expect "should be equal" cue.Slices.[0] id actual
      }
      |> noError
