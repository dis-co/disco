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

module EnsureResolver =

  let test =
    testCase "ensure cue resolver works" <| fun _ ->
      either {
        use checkGitStarted = new AutoResetEvent(false)
        use electionDone = new AutoResetEvent(false)
        use appendDone = new AutoResetEvent(false)
        use clientRegistered = new AutoResetEvent(false)
        use clientAppendDone = new AutoResetEvent(false)
        use updateDone = new AutoResetEvent(false)

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
            | IrisEvent.Started ServiceType.Git            -> checkGitStarted.Set() |> ignore
            | IrisEvent.StateChanged(oldst, Leader)        -> electionDone.Set() |> ignore
            | IrisEvent.Append(Origin.Raft, AddPinGroup _) -> appendDone.Set() |> ignore
            | IrisEvent.Append(_, CallCue _)               -> appendDone.Set() |> ignore
            | _ -> ())
          |> service1.Subscribe

        do! service1.Start()
        do! waitOrDie "checkGitStarted" checkGitStarted
        do! waitOrDie "electionDone" electionDone

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
          Name = "hi"
          Role = Role.Renderer
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        let handleClient = function
          | ClientEvent.Registered              -> clientRegistered.Set() |> ignore
          | ClientEvent.Update (AddCue _)       -> clientAppendDone.Set() |> ignore
          | ClientEvent.Update (AddPinGroup _)  -> clientAppendDone.Set() |> ignore
          | ClientEvent.Update (UpdateSlices _) -> updateDone.Set() |> ignore
          | _ -> ()

        use clobs = client.Subscribe (handleClient)

        do! client.Start()

        do! waitOrDie "clientRegistered" clientRegistered

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| do some work

        let pinId = Id.Create()
        let groupId = Id.Create()

        let pin = BoolPin {
          Id        = pinId
          Name      = "hi"
          PinGroup  = groupId
          Tags      = Array.empty
          Direction = ConnectionDirection.Output
          IsTrigger = false
          VecSize   = VecSize.Dynamic
          Labels    = Array.empty
          Values    = [| true |]
        }

        let group = {
          Id = groupId
          Name = name "whatevva"
          Client = Id.Create()
          Pins = Map.ofList [(pin.Id, pin)]
        }

        client.AddPinGroup group

        do! waitOrDie "appendDone" appendDone
        do! waitOrDie "clientAppendDone" clientAppendDone

        let cue = {
          Id = Id.Create()
          Name = name "hi"
          Slices = [| BoolSlices(pin.Id, [| false |]) |]
        }

        cue
        |> CallCue
        |> service1.Append

        do! waitOrDie "appendDone" appendDone
        do! waitOrDie "updateDone" updateDone

        let actual: Slices =
          client.State.PinGroups
          |> Map.find groupId
          |> fun group -> Map.find pinId group.Pins
          |> fun pin -> pin.Values

       expect "should be equal" cue.Slices.[0] id actual
      }
      |> noError
