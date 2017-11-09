namespace Iris.Tests

open System
open Expecto
open Iris.Core

[<AutoOpen>]
module StoreTests =

  let withStore (wrap : PinGroup -> Store -> unit) =
    let group : PinGroup =
      { Id   = IrisId.Create()
        Name = name "group-1"
        Path = None
        ClientId = IrisId.Create()
        RefersTo = None
        Pins = Map.empty }

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

    let store : Store = new Store(state)
    wrap group store

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_should_add_a_group_to_the_store =
    testCase "should add a group to the store" <| fun _ ->
      withStore <| fun group store ->
        expect "Should be zero" 0 id (PinGroupMap.count store.State.PinGroups)
        store.Dispatch <| AddPinGroup(group)
        expect "Should be one" 1 id (PinGroupMap.count store.State.PinGroups)

  let test_should_update_a_group_already_in_the_store =
    testCase "should update a group already in the store" <| fun _ ->
      withStore <| fun group store ->
        let name1 = group.Name
        let name2 = "group-2"

        store.Dispatch <| AddPinGroup(group)

        expect "Should be true" true id
          (PinGroupMap.containsGroup group.ClientId group.Id store.State.PinGroups)

        expect "Should be true" true id
          (store.State.PinGroups.[group.ClientId,group.Id].Name = name1)

        { group with Name = name name2 }
        |> UpdatePinGroup
        |> store.Dispatch

        expect "Should be true" true id
          (store.State.PinGroups.[group.ClientId,group.Id].Name = name name2)

  let test_should_remove_a_group_already_in_the_store =
    testCase "should remove a group already in the store" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)

        expect "Should be true" true id
          (PinGroupMap.containsGroup group.ClientId group.Id store.State.PinGroups)

        store.Dispatch <| RemovePinGroup(group)

        expect "Should be false" false id
          (PinGroupMap.containsGroup group.ClientId group.Id store.State.PinGroups)

  ///  ____  _
  /// |  _ \(_)_ __
  /// | |_) | | '_ \
  /// |  __/| | | | |
  /// |_|   |_|_| |_|

  let test_should_add_a_pin_to_the_store_if_group_exists =
    testCase "should add a pin to the store if group exists" <| fun _ ->
      withStore <| fun group store ->
        group
        |> PinGroup.addPin
          (Pin.Source.string
            (IrisId.Create())
            (name "url input")
            group.Id
            group.ClientId
            [| "hey" |])
        |> AddPinGroup
        |> store.Dispatch

        expect "Should be one" 1 id
          store.State.PinGroups.[group.ClientId,group.Id].Pins.Count

        Pin.Source.string
          (IrisId.Create())
          (name "another url input")
          group.Id
          group.ClientId
          [| "ho" |]
        |> AddPin
        |> store.Dispatch

        expect "Should be two" 2 id
          store.State.PinGroups.[group.ClientId,group.Id].Pins.Count

  let test_should_not_add_a_pin_to_the_store_if_group_does_not_exists =
    testCase "should not add a pin to the store if group does not exists" <| fun _ ->
      withStore <| fun group store ->
        let pin =
          Pin.Source.string
            (IrisId.Create())
            (name "url input")
            group.Id
            group.ClientId
            [| "ho" |]

        store.Dispatch <| AddPin(pin)
        expect "Should be zero" 0 id (PinGroupMap.count store.State.PinGroups)

  let test_should_update_a_pin_in_the_store_if_it_already_exists =
    testCase "should update a pin in the store if it already exists" <| fun _ ->
      withStore <| fun group store ->
        let name1 = name "can a cat own a cat?"
        let name2 = name "yes, cats are re-entrant."

        let pin =
          Pin.Sink.string
            (IrisId.Create())
            name1
            group.Id
            group.ClientId
            [| "swell" |]

        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| AddPin(pin)

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> flip Map.iter <| fun _ pin ->
          expect "Should be correct name" name1 id pin.Name

        pin
        |> Pin.setName name2
        |> UpdatePin
        |> store.Dispatch

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> flip Map.iter <| fun _ pin ->
          expect "Should be correct name" name2 id pin.Name

  let test_should_remove_a_pin_from_the_store_if_it_exists =
    testCase "should remove a pin from the store if it exists" <| fun _ ->
      withStore <| fun group store ->
        let pin =
          Pin.Sink.string
            (IrisId.Create())
            (name "hi")
            group.Id
            group.ClientId
            [| "swell" |]

        group
        |> PinGroup.addPin pin
        |> AddPinGroup
        |> store.Dispatch

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> expect "should not be empty" false Map.isEmpty

        store.Dispatch <| RemovePin(pin)

        store.State.PinGroups
        |> PinGroupMap.findPin pin.Id
        |> expect "should be empty" true Map.isEmpty

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_should_add_a_cue_to_the_store =
    testCase "should add a cue to the store" <| fun _ ->
      withStore <| fun group store ->

        let cue : Cue = { Id = IrisId.Create(); Name = name "My Cue"; Slices = mkSlices() }

        expect "Should be 0" 0 id store.State.Cues.Count

        store.Dispatch <| AddCue cue

        expect "Should be 1" 1 id store.State.Cues.Count

        store.Dispatch <| AddCue cue

        expect "Should be 0" 1 id store.State.Cues.Count

  let test_should_update_a_cue_already_in_the_store =
    testCase "should update a cue already in the store" <| fun _ ->
      withStore <| fun group store ->
        let cue =
          { Id = IrisId.Create()
            Name = name "My Cue"
            Slices = mkSlices() }

        expect "Should be 0" 0 id store.State.Cues.Count
        store.Dispatch <| AddCue cue
        expect "Should be 1" 1 id store.State.Cues.Count

        let newname = name "aww yeah"
        store.Dispatch <| UpdateCue { cue with Name = newname }

        expect "Should be 1" 1 id store.State.Cues.Count
        expect "Should be correct name" newname id store.State.Cues.[cue.Id].Name

  let test_should_not_add_cue_to_the_store_on_update_when_missing =
    testCase "should not add cue to the store on update when missing" <| fun _ ->
      withStore <| fun group store ->
        let cue =
          { Id = IrisId.Create()
            Name = name "My Cue"
            Slices = mkSlices() }

        expect "Should be 0" 0 id store.State.Cues.Count
        store.Dispatch <| UpdateCue cue
        expect "Should be 0" 0 id store.State.Cues.Count


  let test_should_remove_cue_from_the_store =
    testCase "should remove cue from the store" <| fun _ ->
      withStore <| fun group store ->
        let cue =
          { Id = IrisId.Create()
            Name = name "My Cue"
            Slices = mkSlices() }

        expect "Should be 0" 0 id store.State.Cues.Count
        store.Dispatch <| AddCue cue
        expect "Should be 1" 1 id store.State.Cues.Count
        store.Dispatch <| RemoveCue cue
        expect "Should be 0" 0 id store.State.Cues.Count

  //   ____           _     _     _
  //  / ___|   _  ___| |   (_)___| |_
  // | |  | | | |/ _ \ |   | / __| __|
  // | |__| |_| |  __/ |___| \__ \ |_
  //  \____\__,_|\___|_____|_|___/\__|

  let test_should_add_a_cuelist_to_the_store =
    testCase "should add a cuelist to the store" <| fun _ ->
      withStore <| fun group store ->
        let cuelist =
          { Id = IrisId.Create()
            Name = name "My CueList"
            Items = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        expect "Should be 1" 1 id store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        expect "Should be 1" 1 id store.State.CueLists.Count


  let test_should_update_a_cuelist_already_in_the_store =
    testCase "should update a cuelist already in the store" <| fun _ ->
      withStore <| fun group store ->
        let cuelist =
          { Id = IrisId.Create()
            Name = name "My CueList"
            Items = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        expect "Should be 1" 1 id store.State.CueLists.Count

        let newname = name "aww yeah"
        store.Dispatch <| UpdateCueList { cuelist with Name = newname }

        expect "Should be 1" 1 id store.State.CueLists.Count
        expect "Should be correct name" newname id store.State.CueLists.[cuelist.Id].Name


  let test_should_not_add_cuelist_to_the_store_on_update_when_missing =
    testCase "should not add cuelist to the store on update when missing" <| fun _ ->
      withStore <| fun group store ->
        let cuelist =
          { Id = IrisId.Create()
            Name = name "My CueList"
            Items = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count
        store.Dispatch <| UpdateCueList cuelist
        expect "Should be 0" 0 id store.State.CueLists.Count

  let test_should_remove_cuelist_from_the_store =
    testCase "should remove cuelist from the store" <| fun _ ->
      withStore <| fun group store ->
        let cuelist =
          { Id = IrisId.Create()
            Name = name "My CueList"
            Items = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count
        store.Dispatch <| AddCueList cuelist
        expect "Should be 1" 1 id store.State.CueLists.Count
        store.Dispatch <| RemoveCueList cuelist
        expect "Should be 0" 0 id store.State.CueLists.Count

  //  _   _
  // | | | |___  ___ _ __
  // | | | / __|/ _ \ '__|
  // | |_| \__ \  __/ |
  //  \___/|___/\___|_|

  let test_should_add_a_user_to_the_store =
    testCase "should add a user to the store" <| fun _ ->
      withStore <| fun group store ->
        let user =
          { Id = IrisId.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "901f121"
            Joined = DateTime.Now
            Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count
        store.Dispatch <| AddUser user
        expect "Should be 1" 1 id store.State.Users.Count
        store.Dispatch <| AddUser user
        expect "Should be 1" 1 id store.State.Users.Count

  let test_should_update_a_user_already_in_the_store =
    testCase "should update a user already in the store" <| fun _ ->
      withStore <| fun group store ->
        let user =
          { Id = IrisId.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "lsfa0s9df0"
            Joined  = DateTime.Now
            Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count
        store.Dispatch <| AddUser user
        expect "Should be 1" 1 id store.State.Users.Count

        let newname = "kurt mix master"
        store.Dispatch <| UpdateUser { user with FirstName = name newname }

        expect "Should be 1" 1 id store.State.Users.Count
        expect "Should be correct name" newname id (unwrap store.State.Users.[user.Id].FirstName)


  let test_should_not_add_user_to_the_store_on_update_when_missing =
    testCase "should not add user to the store on update when missing" <| fun _ ->
      withStore <| fun group store ->
        let user =
          { Id = IrisId.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "90av213"
            Joined  = DateTime.Now
            Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count
        store.Dispatch <| UpdateUser user
        expect "Should be 0" 0 id store.State.Users.Count

  let test_should_remove_user_from_the_store =
    testCase "should remove user from the store" <| fun _ ->
      withStore <| fun group store ->
        let user =
          { Id = IrisId.Create()
            UserName = name "krgn"
            FirstName = name "Karsten"
            LastName = name "Gebbert"
            Email = email "k@ioctl.it"
            Password = checksum "1234"
            Salt = checksum "f0sad9fa2"
            Joined  = DateTime.Now
            Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count
        store.Dispatch <| AddUser user
        expect "Should be 1" 1 id store.State.Users.Count
        store.Dispatch <| RemoveUser user
        expect "Should be 0" 0 id store.State.Users.Count

  //  ____                _
  // / ___|  ___  ___ ___(_) ___  _ __
  // \___ \ / _ \/ __/ __| |/ _ \| '_ \
  //  ___) |  __/\__ \__ \ | (_) | | | |
  // |____/ \___||___/___/_|\___/|_| |_|

  let test_should_add_a_session_to_the_store =
    testCase "should add a session to the store" <| fun _ ->
      withStore <| fun group store ->
        let session =
          { Id = IrisId.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count
        store.Dispatch <| AddSession session
        expect "Should be 1" 1 id store.State.Sessions.Count
        store.Dispatch <| AddSession session
        expect "Should be 1" 1 id store.State.Sessions.Count

  let test_should_update_a_session_already_in_the_store =
    testCase "should update a session already in the store" <| fun _ ->
      withStore <| fun group store ->
        let session =
          { Id = IrisId.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count
        store.Dispatch <| AddSession session
        expect "Should be 1" 1 id store.State.Sessions.Count

        let newStatus = "kurt mix master"
        store.Dispatch <| UpdateSession { session with UserAgent = "Hoogle Magenta" }

        expect "Should be 1" 1 id store.State.Sessions.Count
        expect "Should be correct name" "Hoogle Magenta" id
          store.State.Sessions.[session.Id].UserAgent

  let test_should_not_add_session_to_the_store_on_update_when_missing =
    testCase "should not add session to the store on update when missing" <| fun _ ->
      withStore <| fun group store ->
        let session =
          { Id = IrisId.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count
        store.Dispatch <| UpdateSession session
        expect "Should be 0" 0 id store.State.Sessions.Count

  let test_should_remove_session_from_the_store =
    testCase "should remove session from the store" <| fun _ ->
      withStore <| fun group store ->
        let session =
          { Id = IrisId.Create()
            IpAddress = IPv4Address "126.0.0.1"
            UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count
        store.Dispatch <| AddSession session
        expect "Should be 1" 1 id store.State.Sessions.Count
        store.Dispatch <| RemoveSession session
        expect "Should be 0" 0 id store.State.Sessions.Count

  //   ____ _ _            _
  //  / ___| (_) ___ _ __ | |_
  // | |   | | |/ _ \ '_ \| __|
  // | |___| | |  __/ | | | |_
  //  \____|_|_|\___|_| |_|\__|

  let test_should_add_a_client_to_the_store =
    testCase "should add a client to the store" <| fun _ ->
      withStore <| fun group store ->
        let client = mkClient ()

        expect "Should be 0" 0 id store.State.Clients.Count
        store.Dispatch <| AddClient client
        expect "Should be 1" 1 id store.State.Clients.Count
        store.Dispatch <| AddClient client
        expect "Should be 1" 1 id store.State.Clients.Count

  let test_should_update_a_client_already_in_the_store =
    testCase "should update a client already in the store" <| fun _ ->
      withStore <| fun group store ->
        let client = mkClient ()

        expect "Should be 0" 0 id store.State.Clients.Count
        store.Dispatch <| AddClient client
        expect "Should be 1" 1 id store.State.Clients.Count
        store.Dispatch <| UpdateClient { client with Status = ServiceStatus.Stopped }
        expect "Should be 1" 1 id store.State.Clients.Count
        expect "Should be correct status" ServiceStatus.Stopped id
          store.State.Clients.[client.Id].Status

  let test_should_not_add_client_to_the_store_on_update_when_missing =
    testCase "should not add client to the store on update when missing" <| fun _ ->
      withStore <| fun group store ->
        let client = mkClient ()

        expect "Should be 0" 0 id store.State.Clients.Count
        store.Dispatch <| UpdateClient client
        expect "Should be 0" 0 id store.State.Clients.Count

  let test_should_remove_client_from_the_store =
    testCase "should remove client from the store" <| fun _ ->
      withStore <| fun group store ->
        let client = mkClient ()

        expect "Should be 0" 0 id store.State.Clients.Count
        store.Dispatch <| AddClient client
        expect "Should be 1" 1 id store.State.Clients.Count
        store.Dispatch <| RemoveClient client
        expect "Should be 0" 0 id store.State.Clients.Count


  //  _   _           _         ______          _
  // | | | |_ __   __| | ___   / /  _ \ ___  __| | ___
  // | | | | '_ \ / _` |/ _ \ / /| |_) / _ \/ _` |/ _ \
  // | |_| | | | | (_| | (_) / / |  _ <  __/ (_| | (_) |
  //  \___/|_| |_|\__,_|\___/_/  |_| \_\___|\__,_|\___/

  let test_store_should_trigger_listeners_on_undo =
    testCase "store should trigger listeners on undo" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-2" })

        // subscribe now, so as to not fire too early ;)
        store.Subscribe(fun st ev ->
          match ev with
            | AddPinGroup(p) -> if p.Name <> group.Name then failwith "Oh no!"
            | _ -> ())

        expect "Should be 3" 3 id store.History.Length
        store.Undo()

  let test_store_should_dump_previous_states_for_inspection =
    testCase "store should dump previous states for inspection" <| fun _ ->
      withStore <| fun group store ->
        expect "Should be 1" 1 id store.History.Length
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-2" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-3" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "group-4" })
        expect "Should be 5" 5 id store.History.Length


  let test_should_have_correct_number_of_historic_states_when_starting_fresh =
    testCase "should have correct number of historic states when starting fresh" <| fun _ ->
      withStore <| fun group store ->
        let group2 : PinGroup = { group with Name = name "group-2" }
        let group3 : PinGroup = { group2 with Name = name "group-3" }
        let group4 : PinGroup = { group3 with Name = name "group-4" }

        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( group2)
        store.Dispatch <| UpdatePinGroup( group3)
        store.Dispatch <| UpdatePinGroup( group4)

        expect "Should be 5" 5 id store.History.Length

  let test_should_undo_a_single_change =
    testCase "should undo a single change" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Undo()
        expect "Should be shoudl corrent name" group.Name id store.State.PinGroups.[group.ClientId,group.Id].Name

  let test_should_undo_two_changes =
    testCase "should undo two changes" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })
        store.Undo()
        store.Undo()
        expect "Should be shoudl corrent name" group.Name id store.State.PinGroups.[group.ClientId,group.Id].Name

  let test_should_redo_an_undone_change =
    testCase "should redo an undone change" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Undo()
        expect "Should be 0" 0 id (PinGroupMap.count store.State.PinGroups)
        store.Redo()
        expect "Should be 1" 1 id (PinGroupMap.count store.State.PinGroups)

  let test_should_redo_multiple_undone_changes =
    testCase "should redo multiple undone changes" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "mice" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "men"  })
        store.Undo()
        store.Undo()

        expect "Should be dogs" "dogs" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name
        store.Redo()

        expect "Should be mice" "mice" unwrap  store.State.PinGroups.[group.ClientId,group.Id].Name
        store.Redo()

        expect "Should be men" "men" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name
        store.Redo()

        expect "Should be men" "men" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

  let test_should_undo_redo_interleaved_changes =
    testCase "should undo/redo interleaved changes" <| fun _ ->
      withStore <| fun group store ->
        store.Dispatch <| AddPinGroup(group)
        store.Dispatch <| UpdatePinGroup( { group with Name = name "cats" })
        store.Dispatch <| UpdatePinGroup( { group with Name = name "dogs" })

        store.Undo()
        expect "Should be cats" "cats" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Redo()
        expect "Should be dogs" "dogs" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Undo()
        expect "Should be cats" "cats" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Dispatch <| UpdatePinGroup( { group with Name = name "mice" })

        store.Undo()
        expect "Should be dogs" "dogs" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Redo()
        expect "Should be mice" "mice" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Undo()
        store.Undo()

        expect "Should be cats" "cats" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Dispatch <| UpdatePinGroup( { group with Name = name "men"  })

        store.Undo()
        expect "Should be mice" "mice" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        store.Redo()
        expect "Should be men" "men" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

        expect "Should be 6" 6 id store.History.Length

  let test_should_only_keep_specified_number_of_undo_steps =
    testCase "should only keep specified number of undo-steps" <| fun _ ->
      withStore <| fun group store ->
        store.UndoSteps <- 4
        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n -> store.Dispatch <| UpdatePinGroup( { group with Name = name n }))
        |> List.iter (fun _ -> store.Undo())

        expect "Should be 4" 4 id store.History.Length
        expect "Should be mice" "mice" unwrap store.State.PinGroups.[group.ClientId,group.Id].Name

  let test_should_keep_all_state_in_history_in_debug_mode =
    testCase "should keep all state in history in debug mode" <| fun _ ->
      withStore <| fun group store ->
        store.UndoSteps <- 2
        store.Debug <- true

        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| UpdatePinGroup( { group with Name = name n }))

        expect "Should be 8" 8 id store.History.Length

  let test_should_shrink_history_to_UndoSteps_after_leaving_debug_mode =
    testCase "should shrink history to UndoSteps after leaving debug mode" <| fun _ ->
      withStore <| fun group store ->
        store.UndoSteps <- 3
        store.Debug <- true

        store.Dispatch <| AddPinGroup(group)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
          store.Dispatch <| UpdatePinGroup( { group with Name = name n }))

        expect "Should be 8" 8 id store.History.Length
        store.Debug <- false
        expect "Should be 3" 3 id store.History.Length

  ///  ____  _
  /// |  _ \| | __ _ _   _  ___ _ __
  /// | |_) | |/ _` | | | |/ _ \ '__|
  /// |  __/| | (_| | |_| |  __/ |
  /// |_|   |_|\__,_|\__, |\___|_|
  ///                |___/

  let test_should_add_player_and_corresponding_group =
    testCase "should add player and corresponding group" <| fun _ ->
      withStore <| fun _ store ->
        let player = CuePlayer.create "My Player" None

        player
        |> AddCuePlayer
        |> store.Dispatch

        expect "Should have one player" 1 Map.count (State.cuePlayers store.State)
        expect "Should have one group" 1 PinGroupMap.count (State.pinGroupMap store.State)

  let test_should_remove_player_and_corresponding_group =
    testCase "should remove player and corresponding group" <| fun _ ->
      withStore <| fun _ store ->
        let player = CuePlayer.create "My Player" None

        player
        |> AddCuePlayer
        |> store.Dispatch

        expect "Should have one player" 1 Map.count (State.cuePlayers store.State)
        expect "Should have one group" 1 PinGroupMap.count (State.pinGroupMap store.State)

        player
        |> RemoveCuePlayer
        |> store.Dispatch

        expect "Should have no player" 0 Map.count (State.cuePlayers store.State)
        expect "Should have no group" 0 PinGroupMap.count (State.pinGroupMap store.State)

  ///  ____  _             _   _                _
  /// |  _ \| | __ _ _   _| | | | ___  __ _  __| |
  /// | |_) | |/ _` | | | | |_| |/ _ \/ _` |/ _` |
  /// |  __/| | (_| | |_| |  _  |  __/ (_| | (_| |
  /// |_|   |_|\__,_|\__, |_| |_|\___|\__,_|\__,_|
  ///                |___/

  let test_should_update_player_position =
    testCase "should update player position" <| fun _ ->
      withStore <| fun _ store ->
        let cueList =
          CueList.create "My CueList" [|
            for i in 0 .. 4 do
              yield CueGroup.create [|
                  for n in 0 .. 4 do
                    let cue = Cue.create ("Cue-" + string (i + n)) Array.empty
                    cue |> AddCue |> store.Dispatch
                    yield CueReference.ofCue cue
                |]
          |]
        cueList |> AddCueList |> store.Dispatch

        let player = CuePlayer.create "My Player" (Some cueList.Id)
        player |> AddCuePlayer |> store.Dispatch

        [ BoolSlices(player.NextId, None, [| true |]) ]
        |> UpdateSlices.ofList
        |> store.Dispatch

        [ BoolSlices(player.NextId, None, [| true |]) ]
        |> UpdateSlices.ofList
        |> store.Dispatch

        let updated = State.cuePlayer player.Id store.State |> Option.get
        expect "Should have advanced one step" (player.Selected + 2<index>) id updated.Selected

        [ BoolSlices(player.PreviousId, None, [| true |]) ]
        |> UpdateSlices.ofList
        |> store.Dispatch

        let updated = State.cuePlayer player.Id store.State |> Option.get
        expect "Should have reversed one step" (player.Selected + 1<index>) id updated.Selected

  /// __        ___     _            _
  /// \ \      / (_) __| | __ _  ___| |_
  ///  \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
  ///   \ V  V / | | (_| | (_| |  __/ |_
  ///    \_/\_/  |_|\__,_|\__, |\___|\__|
  ///                     |___/

  let test_should_add_widget_and_corresponding_group =
    testCase "should add widget and corresponding group" <| fun _ ->
      withStore <| fun _ store ->
        let widget = PinWidget.create "My Widget" (IrisId.Create())

        widget
        |> AddPinWidget
        |> store.Dispatch

        expect "Should have one widget" 1 Map.count (State.pinWidgets store.State)
        expect "Should have one group" 1 PinGroupMap.count (State.pinGroupMap store.State)

  let test_should_remove_widget_and_corresponding_group =
    testCase "should remove widget and corresponding group" <| fun _ ->
      withStore <| fun _ store ->
        let widget = PinWidget.create "My Widget" (IrisId.Create())

        widget
        |> AddPinWidget
        |> store.Dispatch

        expect "Should have one widget" 1 Map.count (State.pinWidgets store.State)
        expect "Should have one group" 1 PinGroupMap.count (State.pinGroupMap store.State)

        widget
        |> RemovePinWidget
        |> store.Dispatch

        expect "Should have no widget" 0 Map.count (State.pinWidgets store.State)
        expect "Should have no group" 0 PinGroupMap.count (State.pinGroupMap store.State)

  let storeTests =
    testList "Store Tests" [
      test_should_add_a_group_to_the_store
      test_should_update_a_group_already_in_the_store
      test_should_remove_a_group_already_in_the_store
      test_should_add_a_pin_to_the_store_if_group_exists
      test_should_not_add_a_pin_to_the_store_if_group_does_not_exists
      test_should_update_a_pin_in_the_store_if_it_already_exists
      test_should_remove_a_pin_from_the_store_if_it_exists
      test_should_add_a_cue_to_the_store
      test_should_update_a_cue_already_in_the_store
      test_should_not_add_cue_to_the_store_on_update_when_missing
      test_should_remove_cue_from_the_store
      test_should_add_a_cuelist_to_the_store
      test_should_update_a_cuelist_already_in_the_store
      test_should_not_add_cuelist_to_the_store_on_update_when_missing
      test_should_remove_cuelist_from_the_store
      test_should_add_a_user_to_the_store
      test_should_update_a_user_already_in_the_store
      test_should_not_add_user_to_the_store_on_update_when_missing
      test_should_remove_user_from_the_store
      test_should_add_a_session_to_the_store
      test_should_update_a_session_already_in_the_store
      test_should_not_add_session_to_the_store_on_update_when_missing
      test_should_remove_session_from_the_store
      test_should_add_a_client_to_the_store
      test_should_update_a_client_already_in_the_store
      test_should_not_add_client_to_the_store_on_update_when_missing
      test_should_remove_client_from_the_store
      test_store_should_trigger_listeners_on_undo
      test_store_should_dump_previous_states_for_inspection
      test_should_have_correct_number_of_historic_states_when_starting_fresh
      test_should_undo_a_single_change
      test_should_undo_two_changes
      test_should_redo_an_undone_change
      test_should_redo_multiple_undone_changes
      test_should_undo_redo_interleaved_changes
      test_should_only_keep_specified_number_of_undo_steps
      test_should_keep_all_state_in_history_in_debug_mode
      test_should_shrink_history_to_UndoSteps_after_leaving_debug_mode
      test_should_add_player_and_corresponding_group
      test_should_remove_player_and_corresponding_group
      test_should_add_widget_and_corresponding_group
      test_should_remove_widget_and_corresponding_group
      test_should_update_player_position
    ]
