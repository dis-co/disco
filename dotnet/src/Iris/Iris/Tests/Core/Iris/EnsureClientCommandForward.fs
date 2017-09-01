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

module EnsureClientCommandForward =

  let test =
    testCase "ensure client commands are forwarded to leader" <| fun _ ->
      either {
        use electionDone = new AutoResetEvent(false)
        use clientReady = new AutoResetEvent(false)
        use clientAppendDone = new AutoResetEvent(false)
        use cueAppendDone = new AutoResetEvent(false)
        use updateDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 2

        let serverHandler (service: IIrisService) = function
          | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
          | IrisEvent.Append(_, AddCue _) ->
            if not service.RaftServer.IsLeader then
              cueAppendDone.Set() |> ignore
          | other -> ()

        //  ____                  _            _
        // / ___|  ___ _ ____   _(_) ___ ___  / |
        // \___ \ / _ \ '__\ \ / / |/ __/ _ \ | |
        //  ___) |  __/ |   \ V /| | (_|  __/ | |
        // |____/ \___|_|    \_/ |_|\___\___| |_|

        let mem1, machine1 = List.head zipped

        use! service1 = IrisService.create {
          Machine = machine1
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs1 = service1.Subscribe (serverHandler service1)

        do! service1.Start()

        //  ____                  _            ____
        // / ___|  ___ _ ____   _(_) ___ ___  |___ \
        // \___ \ / _ \ '__\ \ / / |/ __/ _ \   __) |
        //  ___) |  __/ |   \ V /| | (_|  __/  / __/
        // |____/ \___|_|    \_/ |_|\___\___| |_____|

        let mem2, machine2 = List.last zipped

        use! service2 = IrisService.create {
          Machine = machine2
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs2 = service2.Subscribe (serverHandler service2)

        do! service2.Start()
        do! waitOrDie "electionDone" electionDone

        //   ____ _ _            _
        //  / ___| (_) ___ _ __ | |_ ___
        // | |   | | |/ _ \ '_ \| __/ __|
        // | |___| | |  __/ | | | |_\__ \
        //  \____|_|_|\___|_| |_|\__|___/

        let handleClient (service: IIrisService) = function
          | ClientEvent.Snapshot -> clientReady.Set() |> ignore
          | ClientEvent.Update (AddCue _) ->
            if not service.RaftServer.IsLeader then
              clientAppendDone.Set() |> ignore
          | _ -> ()

        //   ____ _ _            _     _
        //  / ___| (_) ___ _ __ | |_  / |
        // | |   | | |/ _ \ '_ \| __| | |
        // | |___| | |  __/ | | | |_  | |
        //  \____|_|_|\___|_| |_|\__| |_|

        let serverAddress1:IrisServer = {
          Port = mem1.ApiPort
          IpAddress = mem1.IpAddr
        }

        use client1 = ApiClient.create serverAddress1 {
          Id = Id.Create()
          Name = name "Client 1"
          Role = Role.Renderer
          ServiceId = mem1.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        use clobs1 = client1.Subscribe (handleClient service1)
        do! client1.Start()

        //   ____ _ _            _     ____
        //  / ___| (_) ___ _ __ | |_  |___ \
        // | |   | | |/ _ \ '_ \| __|   __) |
        // | |___| | |  __/ | | | |_   / __/
        //  \____|_|_|\___|_| |_|\__| |_____|


        let serverAddress2:IrisServer = {
          Port = mem2.ApiPort
          IpAddress = mem2.IpAddr
        }

        use client2 = ApiClient.create serverAddress2 {
          Id = Id.Create()
          Name = name "Client 2"
          Role = Role.Renderer
          ServiceId = mem2.Id
          Status = ServiceStatus.Starting
          IpAddress = IpAddress.Localhost
          Port = port 12345us
        }

        use clobs2 = client2.Subscribe (handleClient service2)
        do! client2.Start()

        //   ____      _   _   _               ____                _
        //  / ___| ___| |_| |_(_)_ __   __ _  |  _ \ ___  __ _  __| |_   _
        // | |  _ / _ \ __| __| | '_ \ / _` | | |_) / _ \/ _` |/ _` | | | |
        // | |_| |  __/ |_| |_| | | | | (_| | |  _ <  __/ (_| | (_| | |_| |
        //  \____|\___|\__|\__|_|_| |_|\__, | |_| \_\___|\__,_|\__,_|\__, |
        //                             |___/                         |___/

        do! waitOrDie "clientReady" clientReady
        do! waitOrDie "clientReady" clientReady

        //  ____            _ _           _
        // |  _ \ ___ _ __ | (_) ___ __ _| |_ ___
        // | |_) / _ \ '_ \| | |/ __/ _` | __/ _ \
        // |  _ <  __/ |_) | | | (_| (_| | ||  __/
        // |_| \_\___| .__/|_|_|\___\__,_|\__\___|
        //           |_|

        let cues = [
          { Id = Id.Create(); Name = name "Cue 1"; Slices = [||] }
          { Id = Id.Create(); Name = name "Cue 2"; Slices = [||] }
          { Id = Id.Create(); Name = name "Cue 3"; Slices = [||] }
        ]

        do! either {
          if service1.RaftServer.IsLeader then
            for cue in cues do
              client2.AddCue cue
              do! waitOrDie "addCueFollowerDone" cueAppendDone
              do! waitOrDie "addCueClientDone" clientAppendDone
          else
            for cue in cues do
              client1.AddCue cue
              do! waitOrDie "addCueFollowerDone" cueAppendDone
              do! waitOrDie "addCueClientDone" clientAppendDone
        }

        expect "Services should have same state "
          service1.State.Cues
          id
          service2.State.Cues

        expect "Clients should have same state"
          client1.State.Cues
          id
          client2.State.Cues
      }
      |> noError
