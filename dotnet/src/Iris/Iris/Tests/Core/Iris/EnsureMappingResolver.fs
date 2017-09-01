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

module EnsureMappingResolver =

  let logger = Logger.subscribe Logger.stdout

  let test =
    ftestCase "ensure mapping resolver works" <| fun _ ->
      either {
        use electionDone = new AutoResetEvent(false)
        use clientReady = new AutoResetEvent(false)
        use clientAppendDone = new AutoResetEvent(false)
        use cueAppendDone = new AutoResetEvent(false)
        use updateDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 1

        let serverHandler (service: IIrisService) = function
          | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
          | IrisEvent.Append(_, AddCue _) ->
            if not service.RaftServer.IsLeader then
              cueAppendDone.Set() |> ignore
          | other -> ()

        let group = PinGroup.create (name "My Group")

        let source =
          Pin.Source.toggle
            (Id "My First Toggle")
            (name "My First Toggle")
            group.Id
            Array.empty
            [| false |]
          |> Pin.setPersisted true

        let sink =
          Pin.Sink.toggle
            (Id "My Second Toggle")
            (name "My Second Toggle")
            group.Id
            Array.empty
            [| false |]
          |> Pin.setPersisted true

        do! { group with Pins = Map.ofList [ (source.Id,source); (sink.Id, sink) ] }
            |> Asset.save project.Path

        ///  ____                  _
        /// / ___|  ___ _ ____   _(_) ___ ___
        /// \___ \ / _ \ '__\ \ / / |/ __/ _ \
        ///  ___) |  __/ |   \ V /| | (_|  __/
        /// |____/ \___|_|    \_/ |_|\___\___|

        let mem, machine = List.head zipped

        use! service = IrisService.create {
          Machine = machine
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs = service.Subscribe (serverHandler service)

        do! service.Start()

        do! waitOrDie "electionDone" electionDone

        expect "Should have the group"
          true
          (Map.containsKey group.Id)
          service.State.PinGroups

        ///  _____         _
        /// |_   _|__  ___| |_
        ///   | |/ _ \/ __| __|
        ///   | |  __/\__ \ |_
        ///   |_|\___||___/\__|



      }
      |> noError
