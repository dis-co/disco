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

module EnsureMappingResolver =

  let test =
    testCase "ensure mapping resolver works" <| fun _ ->
      either {
        use electionDone = new WaitEvent()
        use counter = new WaitEvent()

        let! (project, zipped) = mkCluster 1

        let serverHandler (service: IDiscoService) = function
          | DiscoEvent.StateChanged(oldst, Leader) -> electionDone.Set()
          | DiscoEvent.Append(_, UpdateSlices map) -> counter.Set()
          | other -> ()

        let group = PinGroup.create (name "My Group")

        let source =
          Pin.Source.toggle
            (DiscoId.Create())
            (name "My First Toggle")
            group.Id
            group.ClientId
            [| false |]
          |> Pin.setPersisted true

        let sink =
          Pin.Sink.toggle
            (DiscoId.Create())
            (name "My Second Toggle")
            group.Id
            group.ClientId
            [| false |]
          |> Pin.setPersisted true

        let mapping =
          { Id = DiscoId.Create()
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

        use! service = DiscoService.create {
          Machine = machine
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs = service.Subscribe (serverHandler service)

        do! service.Start()

        do! waitFor "electionDone" electionDone

        expect "Should have the group"
          true
          (PinGroupMap.containsGroup group.ClientId group.Id)
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

        let slices = BoolSlices(source.Id, None, false, [| true |])

        [ slices ]
        |> UpdateSlices.ofList
        |> service.Append

        do! waitFor "UpdateSlice" counter
        do! waitFor "UpdateSlice" counter

        expect "Sink should have true in first slice"
          (Slices.setId sink.Id slices)
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin sink.Id)
           >> Option.get
           >> Pin.slices)
          service.State.PinGroups

        expect "Source should have true in first slice"
          (Slices.setId source.Id slices)
          (PinGroupMap.tryFindGroup group.ClientId group.Id
           >> Option.map (PinGroup.findPin source.Id)
           >> Option.get
           >> Pin.slices)
          service.State.PinGroups
      }
      |> noError
