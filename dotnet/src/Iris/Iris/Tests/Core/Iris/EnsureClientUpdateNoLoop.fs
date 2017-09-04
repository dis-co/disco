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

module EnsureClientUpdateNoLoop =

  let test =
    testCase "ensure client slice update does not loop" <| fun _ ->
      either {
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

        use! service1 = IrisService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 =
          (function
            | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set()
            | IrisEvent.Append(Origin.Raft, _)      -> appendDone.Set()
            | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitFor "electionDone" electionDone

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
          | ClientEvent.Registered              -> clientRegistered.Set()
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Set()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set()
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Set()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)

        do! client.Start()

        do! waitFor "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| do some work

        let pinId = Id.Create()
        let groupId = Id.Create()

        let pin = BoolPin {
          Id        = pinId
          Name      = name "hi"
          PinGroup  = groupId
          Tags      = Array.empty
          Direction = ConnectionDirection.Sink
          Online    = true
          Persisted = false
          IsTrigger = false
          VecSize   = VecSize.Dynamic
          Labels    = Array.empty
          Values    = [| true |]
        }

        let group = {
          Id = groupId
          Name = name "whatevva"
          Client = Id.Create()
          Path = None
          RefersTo = None
          Pins = Map.ofList [(pin.Id, pin)]
        }

        client.AddPinGroup group

        do! waitFor "appendDone" appendDone
        do! waitFor "clientAppendDone" clientAppendDone

        let update = BoolSlices(pin.Id, [| false |])

        client.UpdateSlices [
          update
        ]

        do! waitFor "updateDone" updateDone

        let actual: Slices =
          client.State.PinGroups
          |> Map.find groupId
          |> fun group -> Map.find pinId group.Pins
          |> fun pin -> pin.Values

       expect "should be equal" update id actual
      }
      |> noError
