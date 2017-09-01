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

module CorrectPinPersistance =

  let inline fileOnDisk basePath (asset: ^t when ^t : (member AssetPath: FilePath)) =
    basePath </> (^t : (member AssetPath: FilePath) asset)
    |> File.exists

  let test =
    ftestCase "ensure pins are correctly persisted" <| fun _ ->
      either {
        let started = WaitCount.Create()
        let appendDone = WaitCount.Create()
        let createDone = WaitCount.Create()
        let changedDone = WaitCount.Create()
        let deletedDone = WaitCount.Create()

        let! (project, zipped) = mkCluster 1

        //  _
        // / |
        // | |
        // | |
        // |_| start

        let mem, machine = List.head zipped

        use! service = IrisService.create {
          Machine = machine
          ProjectName = project.Name
          UserName = User.Admin.UserName
          Password = password Constants.ADMIN_DEFAULT_PASSWORD
          SiteId = None
        }

        use oobs = service.Subscribe <| function
          | IrisEvent.Started ServiceType.Raft -> started.Increment()

          | IrisEvent.Append(Origin.Raft, AddPinGroup _)
          | IrisEvent.Append(Origin.Raft, AddPin      _)
          | IrisEvent.Append(Origin.Raft, UpdatePin   _)
          | IrisEvent.Append(Origin.Raft, RemovePin   _) -> appendDone.Increment()

          | IrisEvent.Append(_, LogMsg _) -> () // ignore log messages

          | IrisEvent.FileSystem(FileSystemEvent.Created _ as data) -> createDone.Increment()
          | IrisEvent.FileSystem(FileSystemEvent.Changed _ as data) -> changedDone.Increment()
          | IrisEvent.FileSystem(FileSystemEvent.Deleted _ as data) -> deletedDone.Increment()

          | ev -> ()

        do! service.Start()

        do! waitFor "started to be 1" started 1

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| append a volatile PinGroup + Pin

        let group =
          { Id     = Id.Create()
            Name   = name "Group 1"
            Client = Id.Create()
            Path   = None
            RefersTo = None
            Pins   = Map.empty }

        let toggle =
          Pin.Sink.toggle
            (Id.Create())
            (name "My Toggle")
            group.Id
            Array.empty
            Array.empty

        group
        |> AddPinGroup
        |> service.Append

        do! waitFor "append PinGroup to be 1" appendDone 1

        toggle
        |> AddPin
        |> service.Append

        do! waitFor "append Pin to be 2" appendDone 2

        //  _____
        // |___ /
        //   |_ \
        //  ___) |
        // |____/ test that nothing was actually put on disk

        expect "[1] serialized group should not exist on disk" false (fileOnDisk project.Path) group

        //  _  _
        // | || |
        // | || |_
        // |__   _|
        //    |_| update the pin to be persisted

        toggle
        |> Pin.setPersisted true
        |> UpdatePin
        |> service.Append

        do! waitFor "created pingroup file to be 1" createDone 1
        do! waitFor "pingroup file changed to be 1" changedDone 1

        //  ____
        // | ___|
        // |___ \
        //  ___) |
        // |____/ test that it was indeed persisted successfully

        expect "[2] serialized group should exist on disk" true (fileOnDisk project.Path) group

        //   __
        //  / /_
        // | '_ \
        // | (_) |
        //  \___/ update the pin to be volatile again

        toggle
        |> Pin.setPersisted false
        |> UpdatePin
        |> service.Append

        do! waitFor "pingroup file deleted to be 1" deletedDone 1

        //  _____
        // |___  |
        //    / /
        //   / /
        //  /_/ test that pin and group were deleted

        expect "[3] serialized group should not exist on disk" false (fileOnDisk project.Path) group
      }
      |> noError
