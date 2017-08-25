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

  let logger = Logger.subscribe Logger.stdout

  let inline fileOnDisk basePath (asset: ^t when ^t : (member AssetPath: FilePath)) =
    basePath </> (^t : (member AssetPath: FilePath) asset)
    |> File.exists

  let test =
    testCase "ensure pins are correctly persisted" <| fun _ ->
      either {
        use started = new AutoResetEvent(false)
        use appendDone = new AutoResetEvent(false)
        use createDone = new AutoResetEvent(false)
        use changedDone = new AutoResetEvent(false)
        use deletedDone = new AutoResetEvent(false)

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
          | IrisEvent.Started ServiceType.Raft ->
            started.Set() |> ignore

          | IrisEvent.Append(Origin.Raft, AddPinGroup _)
          | IrisEvent.Append(Origin.Raft, AddPin      _)
          | IrisEvent.Append(Origin.Raft, UpdatePin   _)
          | IrisEvent.Append(Origin.Raft, RemovePin   _) ->
            appendDone.Set() |> ignore

          | IrisEvent.Append(_, LogMsg _) -> () // ignore log messages

          | IrisEvent.FileSystem(FileSystemEvent.Created _ as data) -> createDone.Set() |> ignore
          | IrisEvent.FileSystem(FileSystemEvent.Changed _ as data) -> changedDone.Set() |> ignore
          | IrisEvent.FileSystem(FileSystemEvent.Deleted _ as data) -> deletedDone.Set() |> ignore

          | ev -> ()

        do! service.Start()

        do! waitOrDie "started" started

        //  ____
        // |___ \
        //   __) |
        //  / __/
        // |_____| append a volatile PinGroup + Pin

        let group =
          { Id     = Id.Create()
            Name   = name "Group 1"
            Client = Id.Create()
            Pins   = Map.empty }

        let toggle =
          Pin.toggle
            (Id.Create())
            "My Toggle"
            group.Id
            Array.empty
            Array.empty

        group
        |> AddPinGroup
        |> service.Append

        do! waitOrDie "append PinGroup" appendDone

        toggle
        |> AddPin
        |> service.Append

        do! waitOrDie "append Pin" appendDone

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

        do! waitOrDie "created pingroup file" createDone
        do! waitOrDie "pingroup file changed" changedDone

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

        do! waitOrDie "pingroup file deleted" deletedDone

        //  _____
        // |___  |
        //    / /
        //   / /
        //  /_/ test that pin and group were deleted

        expect "[3] serialized group should not exist on disk" false (fileOnDisk project.Path) group
      }
      |> noError
