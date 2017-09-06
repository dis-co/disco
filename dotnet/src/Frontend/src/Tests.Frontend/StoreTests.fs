namespace Test.Units

[<RequireQualifiedAccess>]
module Store =

  open Fable.Core
  open Fable.Import

  open System
  open System.Collections.Generic

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests

  let withStore (wrap : PinGroup -> Store -> unit) =
    let group : PinGroup =
      { Id = Id.Create()
        Name = name "group-1"
        Client = Id.Create()
        Path = None
        RefersTo = None
        Pins = Map.empty }

    let machine =
      { MachineId    = Id.Create ()
        HostName     = name "La la Land"
        WorkSpace    = filepath "C:\Program Files\Yo Mama"
        LogDirectory = filepath "C:\Program Files\Yo Mama\logs"
        BindAddress  = IPv4Address "127.0.0.1"
        WebPort      = port 80us
        RaftPort     = port 70us
        WsPort       = port 60us
        GitPort      = port 50us
        ApiPort      = port 40us
        Version      = version "1.0.0" }

    let project = IrisProject.Empty

    let state =
      { Project            = project
        PinGroups          = PinGroupMap.empty
        PinMappings        = Map.empty
        PinWidgets         = Map.empty
        Cues               = Map.empty
        CueLists           = Map.empty
        Users              = Map.empty
        Sessions           = Map.empty
        Clients            = Map.empty
        CuePlayers         = Map.empty
        DiscoveredServices = Map.empty }

    let store : Store = Store(state)
    wrap group store

  let main () =
    (* ----------------------------------------------------------------------- *)
    suite "Test.Units.Store - Immutability:"
    (* ----------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "store should be immutable" <| fun finish ->
        let state = store.State
        store.Dispatch <| AddPinGroup(group)
        let newstate = store.State
        equals false (Object.ReferenceEquals(state, newstate))
        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - PinGroup operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a group to the store" <| fun finish ->
        equals 0 store.State.PinGroups.Count
        store.Dispatch <| AddPinGroup(group)
        equals 1 store.State.PinGroups.Count
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a group already in the store" <| fun finish ->
        let name1 = group.Name
        let name2 = name "group-2"

        store.Dispatch <| AddPinGroup(group)

        equals true (store.State.PinGroups.ContainsGroup group.Client group.Id)
        equals true (store.State.PinGroups.[group.Client,group.Id].Name = name1)

        let updated = { group with Name = name2 }
        store.Dispatch <| UpdatePinGroup(updated)

        equals true (store.State.PinGroups.[group.Client,group.Id].Name = name2)

        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove a group already in the store" <| fun finish ->
        let pin =
          Pin.Sink.string
            (Id.Create())
            (name "url input")
            group.Id
            group.Client
            [| "hey" |]
          |> Pin.setPersisted true

        group
        |> PinGroup.addPin pin
        |> AddPinGroup
        |> store.Dispatch

        equals true  (store.State.PinGroups.ContainsGroup group.Client group.Id)
        store.Dispatch <| RemovePinGroup(group)
        equals false (store.State.PinGroups.ContainsGroup group.Client group.Id)

        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Pin operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a pin to the store if group exists" <| fun finish ->
        Pin.Sink.string
          (Id.Create())
          (name "url input")
          group.Id
          group.Client
          [| "hey" |]
        |> Pin.setPersisted true
        |> flip PinGroup.addPin group
        |> AddPinGroup
        |> store.Dispatch

        equals 1 store.State.PinGroups.[group.Client,group.Id].Pins.Count

        let pin =
          Pin.Sink.string
            (Id "0xb33f")
            (name "url input")
            group.Id
            group.Client
            [| "hey" |]

        store.Dispatch <| AddPin(pin)
        equals 2 store.State.PinGroups.[group.Client,group.Id].Pins.Count
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should not add a pin to the store if group does not exists" <| fun finish ->
        let pin =
          Pin.Sink.string
            (Id "0xb33f")
            (name "url input")
            group.Id
            group.Client
            [| "Ho" |]
        store.Dispatch <| AddPin(pin)
        equals 0 store.State.PinGroups.Count
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a pin in the store if it already exists" <| fun finish ->
        let name1 = name "can a cat own a cat?"
        let name2 = name "yes, cats are re-entrant."
        let pin =
          Pin.Sink.string
            (Id.Create())
            name1
            group.Id
            group.Client
            [| "swell" |]

        group
        |> PinGroup.addPin pin
        |> AddPinGroup
        |> store.Dispatch

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> fun m -> let i = Map.find pin.Client m in equals name1 i.Name

        let updated = Pin.setName name2 pin
        store.Dispatch <| UpdatePin(updated)

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> fun m -> let i = Map.find pin.Client m in equals name2 i.Name

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove a pin from the store if it exists" <| fun finish ->
        let pin =
          Pin.Sink.string
            (Id.Create())
            (name "hi")
            group.Id
            group.Client
            [| "oh my" |]

        group
        |> PinGroup.addPin pin
        |> AddPinGroup
        |> store.Dispatch

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> Map.find pin.Client
        |> ignore                        /// will fail if not found

        store.Dispatch <| RemovePin(pin)

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> Map.isEmpty
        |> equals true

        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Cue operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a cue to the store" <| fun finish ->
        let cue = {
          Id = Id.Create()
          Name = name "My Cue"
          Slices = [| |] }

        equals 0 store.State.Cues.Count
        store.Dispatch <| AddCue cue
        equals 1 store.State.Cues.Count
        store.Dispatch <| AddCue cue
        equals 1 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a cue already in the store" <| fun finish ->
        let cue =
          { Id = Id.Create()
            Name = name "My Cue"
            Slices = [| |] }

        equals 0 store.State.Cues.Count
        store.Dispatch <| AddCue cue
        equals 1 store.State.Cues.Count

        let newname = name "aww yeah"
        store.Dispatch <| UpdateCue { cue with Name = newname }
        equals 1 store.State.Cues.Count
        equals newname store.State.Cues.[cue.Id].Name

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should not add cue to the store on update when missing" <| fun finish ->
        let cue =
          { Id = Id.Create()
            Name = name "My Cue"
            Slices = [| |] }

        equals 0 store.State.Cues.Count
        store.Dispatch <| UpdateCue cue
        equals 0 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove cue from the store" <| fun finish ->
        let cue =
          { Id = Id.Create()
            Name = name "My Cue"
            Slices = [| |] }

        equals 0 store.State.Cues.Count
        store.Dispatch <| AddCue cue
        equals 1 store.State.Cues.Count
        store.Dispatch <| RemoveCue cue
        equals 0 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - CueList operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a cuelist to the store" <| fun finish ->
        let cuelist =
          { Id = Id.Create()
            Name = name "My CueList"
            Groups = [| |] }

        equals 0 store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        equals 1 store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        equals 1 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a cuelist already in the store" <| fun finish ->
        let cuelist =
          { Id = Id.Create()
            Name = name "My CueList"
            Groups = [| |] }

        equals 0 store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        equals 1 store.State.CueLists.Count

        let newname = name "aww yeah"
        store.Dispatch <| UpdateCueList { cuelist with Name = newname }

        equals 1 store.State.CueLists.Count
        equals newname store.State.CueLists.[cuelist.Id].Name

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should not add cuelist to the store on update when missing" <| fun finish ->
        let cuelist =
          { Id = Id.Create()
            Name = name "My CueList"
            Groups = [| |] }

        equals 0 store.State.CueLists.Count
        store.Dispatch <| UpdateCueList cuelist
        equals 0 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove cuelist from the store" <| fun finish ->
        let cuelist =
          { Id = Id.Create()
            Name = name "My CueList"
            Groups = [| |] }

        equals 0 store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        equals 1 store.State.CueLists.Count
        store.Dispatch <| RemoveCueList cuelist
        equals 0 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - User operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a user to the store" <| fun finish ->
        let user =
          { Id = Id.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "090asd902"
            Joined = DateTime.UtcNow.Date
            Created = DateTime.UtcNow.Date.AddDays(-1.) }

        equals 0 store.State.Users.Count
        store.Dispatch <| AddUser user
        equals 1 store.State.Users.Count
        store.Dispatch <| AddUser user
        equals 1 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a user already in the store" <| fun finish ->
        let user =
          { Id = Id.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "090asd902"
            Joined = DateTime.UtcNow.Date
            Created = DateTime.UtcNow.Date.AddDays(-1.) }

        equals 0 store.State.Users.Count
        store.Dispatch <| AddUser user
        equals 1 store.State.Users.Count

        let newname = name "kurt mix master"
        store.Dispatch <| UpdateUser { user with FirstName = newname }

        equals 1 store.State.Users.Count
        equals newname store.State.Users.[user.Id].FirstName

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should not add user to the store on update when missing" <| fun finish ->
        let user =
          { Id = Id.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "090asd902"
            Joined = DateTime.UtcNow.Date
            Created = DateTime.UtcNow.Date.AddDays(-1.) }

        equals 0 store.State.Users.Count
        store.Dispatch <| UpdateUser user
        equals 0 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove user from the store" <| fun finish ->
        let user =
          { Id = Id.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "090asd902"
            Joined = DateTime.UtcNow.Date
            Created = DateTime.UtcNow.Date.AddDays(-1.) }

        equals 0 store.State.Users.Count
        store.Dispatch <| AddUser user
        equals 1 store.State.Users.Count
        store.Dispatch <| RemoveUser user
        equals 0 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Session operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "should add a session to the store" <| fun finish ->
        let session =
          { Id = Id.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count
        store.Dispatch <| AddSession session
        equals 1 store.State.Sessions.Count
        store.Dispatch <| AddSession session
        equals 1 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should update a Session already in the store" <| fun finish ->
        let session =
          { Id = Id.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count
        store.Dispatch <| AddSession session
        equals 1 store.State.Sessions.Count

        let newUserAgent = "Hoogle Magenta"
        store.Dispatch <| UpdateSession { session with UserAgent = newUserAgent }

        equals 1 store.State.Sessions.Count
        equals newUserAgent store.State.Sessions.[session.Id].UserAgent

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should not add Session to the store on update when missing" <| fun finish ->
        let session =
          { Id = Id.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count
        store.Dispatch <| UpdateSession session
        equals 0 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should remove Session from the store" <| fun finish ->
        let session =
          { Id = Id.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count
        store.Dispatch <| AddSession session
        equals 1 store.State.Sessions.Count
        store.Dispatch <| RemoveSession session
        equals 0 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Undo/Redo"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun group store ->
      test "store should trigger listeners on undo" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-2" })

        // subscribe now, so as to not fire too early ;)
        store.Subscribe(fun st ev ->
          match ev with
          | AddPinGroup(p) -> if p.Name = group.Name then finish ()
          | _ -> ())

        equals 3 store.History.Length
        store.Undo()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "store should dump previous states for inspection" <| fun finish ->
        equals 1 store.History.Length
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-2" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-3" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-4" })
        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should have correct number of historic states when starting fresh" <| fun finish ->
        let group2 : PinGroup = { group with Name = name "group-2" }
        let group3 : PinGroup = { group2 with Name = name "group-3" }
        let group4 : PinGroup = { group3 with Name = name "group-4" }

        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( group2)
        store.Dispatch <| UpdatePinGroup( group3)
        store.Dispatch <| UpdatePinGroup( group4)

        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should undo a single change" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Undo()
        equals group.Name store.State.PinGroups.[group.Client,group.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should undo two changes" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })
        store.Undo()
        store.Undo()
        equals group.Name store.State.PinGroups.[group.Client,group.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should redo an undone change" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Undo()
        equals 0 store.State.PinGroups.Count
        store.Redo()
        equals 1 store.State.PinGroups.Count
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should redo multiple undone changes" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "mice" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "men"  })
        store.Undo()
        store.Undo()

        equals (name "dogs") store.State.PinGroups.[group.Client,group.Id].Name
        store.Redo()

        equals (name "mice") store.State.PinGroups.[group.Client,group.Id].Name
        store.Redo()

        equals (name "men") store.State.PinGroups.[group.Client,group.Id].Name
        store.Redo()

        equals (name "men") store.State.PinGroups.[group.Client,group.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should undo/redo interleaved changes" <| fun finish ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })

        store.Undo()
        equals (name "cats") store.State.PinGroups.[group.Client,group.Id].Name

        store.Redo()
        equals (name "dogs") store.State.PinGroups.[group.Client,group.Id].Name

        store.Undo()
        equals (name "cats") store.State.PinGroups.[group.Client,group.Id].Name

        store.Dispatch <| UpdatePinGroup( { group with Name = name "mice" })

        store.Undo()
        equals (name "dogs") store.State.PinGroups.[group.Client,group.Id].Name

        store.Redo()
        equals (name "mice") store.State.PinGroups.[group.Client,group.Id].Name

        store.Undo()
        store.Undo()

        equals (name "cats") store.State.PinGroups.[group.Client,group.Id].Name

        store.Dispatch <| UpdatePinGroup( { group with Name = name "men"  })

        store.Undo()
        equals (name "mice") store.State.PinGroups.[group.Client,group.Id].Name

        store.Redo()
        equals (name "men") store.State.PinGroups.[group.Client,group.Id].Name

        equals 6 store.History.Length
        finish ()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should only keep specified number of undo-steps" <| fun finish ->
        store.UndoSteps <- 4
        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n -> store.Dispatch <| UpdatePinGroup( { group with Name = name n }))
        |> List.iter (fun _ -> store.Undo())

        equals 4             store.History.Length
        equals (name "mice") store.State.PinGroups.[group.Client,group.Id].Name
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should keep all state in history in debug mode" <| fun finish ->
        store.UndoSteps <- 2
        store.Debug <- true

        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n -> store.Dispatch <| UpdatePinGroup( { group with Name = name n }))

        equals 8 store.History.Length
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun group store ->
      test "should shrink history to UndoSteps after leaving debug mode" <| fun finish ->
        store.UndoSteps <- 3
        store.Debug <- true

        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n -> store.Dispatch <| UpdatePinGroup( { group with Name = name n }))

        equals 8 store.History.Length
        store.Debug <- false
        equals 3 store.History.Length
        finish ()
