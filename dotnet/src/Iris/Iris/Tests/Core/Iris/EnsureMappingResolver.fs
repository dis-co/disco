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

  let waitFor (count: int ref) (expected: int) =
    while !count < expected do
      Thread.Sleep(1)

  let test =
    testCase "ensure mapping resolver works" <| fun _ ->
      either {
        use electionDone = new AutoResetEvent(false)

        let! (project, zipped) = mkCluster 1

        let count = ref 0

        let serverHandler (service: IIrisService) = function
          | IrisEvent.StateChanged(oldst, Leader) -> electionDone.Set() |> ignore
          | IrisEvent.Append(_, UpdateSlices map) -> count := !count + 1
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

        let mapping =
          { Id = Id.Create()
            Source = source.Id
            Sinks = Set [ sink.Id ] }

        do! Asset.save project.Path mapping
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

        expect "Should have the mapping"
          true
          (Map.containsKey mapping.Id)
          service.State.PinMappings

        ///  _____         _
        /// |_   _|__  ___| |_
        ///   | |/ _ \/ __| __|
        ///   | |  __/\__ \ |_
        ///   |_|\___||___/\__|

        let slices = BoolSlices(source.Id, [| true |])

        [ slices ]
        |> UpdateSlices.ofList
        |> service.Append

        do waitFor count 2

        expect "Sink should have true in first slice"
          (Slices.setId sink.Id slices)
          (Map.find group.Id >> flip PinGroup.findPin sink.Id >> Pin.slices)
          service.State.PinGroups

        expect "Source should have true in first slice"
          (Slices.setId source.Id slices)
          (Map.find group.Id >> flip PinGroup.findPin source.Id >> Pin.slices)
          service.State.PinGroups
      }
      |> noError
