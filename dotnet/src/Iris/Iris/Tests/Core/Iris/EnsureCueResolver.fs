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

module EnsureCueResolver =

  let test =
    testCase "ensure cue resolver works" <| fun _ ->
      either {
        let checkGitStarted = WaitCount.Create()
        let electionDone = WaitCount.Create()
        let appendDone = WaitCount.Create()
        let clientRegistered = WaitCount.Create()
        let clientAppendDone = WaitCount.Create()
        let updateDone = WaitCount.Create()

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
            | IrisEvent.Started ServiceType.Git            -> checkGitStarted.Increment()
            | IrisEvent.StateChanged(oldst, Leader)        -> electionDone.Increment()
            | IrisEvent.Append(Origin.Raft, AddPinGroup _) -> appendDone.Increment()
            | IrisEvent.Append(_, CallCue _)               -> appendDone.Increment()
            | _ -> ())
          |> service1.Subscribe

        do! service1.Start()

        do! waitFor "checkGitStarted to be 1" checkGitStarted 1
        do! waitFor "electionDone to be 1" electionDone 1

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| create an API client

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
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Increment()
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Increment()
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Increment()
          | _ -> ()

        use clobs = client.Subscribe (handleClient)

        do! client.Start()

        do! waitFor "clientRegistered to be 1" clientRegistered 1

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ do some work

        let pinId = Id.Create()
        let groupId = Id.Create()

        let pin = BoolPin {
          Id        = pinId
          Name      = name "hi"
          PinGroup  = groupId
          Tags      = Array.empty
          Direction = ConnectionDirection.Sink
          IsTrigger = false
          Persisted = false
          Online    = true
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

        do! waitFor "appendDone to be 1" appendDone 1
        do! waitFor "clientAppendDone to be 1" clientAppendDone 1

        let cue = {
          Id = Id.Create()
          Name = name "hi"
          Slices = [| BoolSlices(pin.Id, [| false |]) |]
        }

        cue
        |> CallCue
        |> service1.Append

        do! waitFor "appendDone to be 2" appendDone 2
        do! waitFor "updateDone to be 1" updateDone 1

        let actual: Slices =
          client.State.PinGroups
          |> Map.find groupId
          |> fun group -> Map.find pinId group.Pins
          |> fun pin -> pin.Values

       expect "should be equal" cue.Slices.[0] id actual
      }
      |> noError
