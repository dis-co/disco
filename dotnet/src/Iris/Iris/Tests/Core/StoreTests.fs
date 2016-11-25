namespace Iris.Tests

open System
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module StoreTests =

  let withStore (wrap : Patch -> Store -> unit) =
    let patch : Patch =
      { Id = Id "0xb4d1d34"
      ; Name = "patch-1"
      ; IOBoxes = Map.empty
      }

    let store : Store = new Store(State.Empty)
    wrap patch store

  //  ____       _       _
  // |  _ \ __ _| |_ ___| |__
  // | |_) / _` | __/ __| '_ \
  // |  __/ (_| | || (__| | | |
  // |_|   \__,_|\__\___|_| |_|

  let test_should_add_a_patch_to_the_store =
    testCase "should add a patch to the store" <| fun _ ->
      withStore <| fun patch store ->
        expect "Should be zero" 0 id store.State.Patches.Count
        store.Dispatch <| AddPatch(patch)
        expect "Should be one" 1 id store.State.Patches.Count

  let test_should_update_a_patch_already_in_the_store =
    testCase "should update a patch already in the store" <| fun _ ->
      withStore <| fun patch store ->
        let name1 = patch.Name
        let name2 = "patch-2"

        store.Dispatch <| AddPatch(patch)

        expect "Should be true" true id (store.State.Patches.ContainsKey patch.Id)
        expect "Should be true" true id (store.State.Patches.[patch.Id].Name = name1)

        let updated = { patch with Name = name2 }
        store.Dispatch <| UpdatePatch(updated)

        expect "Should be true" true id (store.State.Patches.[patch.Id].Name = name2)

  let test_should_remove_a_patch_already_in_the_store =
    testCase "should remove a patch already in the store" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        expect "Should be true" true id (store.State.Patches.ContainsKey patch.Id)

        store.Dispatch <| RemovePatch(patch)
        expect "Should be false" false id (store.State.Patches.ContainsKey patch.Id)

  //  ___ ___  ____
  // |_ _/ _ \| __ )  _____  __
  //  | | | | |  _ \ / _ \ \/ /
  //  | | |_| | |_) | (_) >  <
  // |___\___/|____/ \___/_/\_\

  let test_should_add_an_iobox_to_the_store_if_patch_exists =
    testCase "should add an iobox to the store if patch exists" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)

        expect "Should be zero" 0 id store.State.Patches.[patch.Id].IOBoxes.Count

        let slice : StringSliceD = { Index = 0u; Value = "Hey" }
        let iobox : IOBox = IOBox.String(Id "0xb33f","url input", patch.Id, Array.empty, [| slice |])

        store.Dispatch <| AddIOBox(iobox)

        expect "Should be one" 1 id store.State.Patches.[patch.Id].IOBoxes.Count

  let test_should_not_add_an_iobox_to_the_store_if_patch_does_not_exists =
    testCase "should not add an iobox to the store if patch does not exists" <| fun _ ->
      withStore <| fun patch store ->
        let slice : StringSliceD = { Index = 0u; Value =  "Hey" }
        let iobox = IOBox.String(Id "0xb33f","url input", patch.Id, Array.empty, [| slice |])
        store.Dispatch <| AddIOBox(iobox)
        expect "Should be zero" 0 id store.State.Patches.Count

  let test_should_update_an_iobox_in_the_store_if_it_already_exists =
    testCase "should update an iobox in the store if it already exists" <| fun _ ->
      withStore <| fun patch store ->
        let name1 = "can a cat own a cat?"
        let name2 = "yes, cats are re-entrant."

        let slice : StringSliceD = { Index = 0u; Value = "swell" }
        let iobox = IOBox.String(Id "0xb33f", name1, patch.Id, Array.empty, [| slice |])

        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| AddIOBox(iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> expect "Should be correct name" name1 id i.Name
          | None    -> failwith "iobox is mysteriously missing"

        let updated = iobox.SetName name2
        store.Dispatch <| UpdateIOBox(updated)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> expect "Should be correct name" name2 id i.Name
          | None    -> failwith "iobox is mysteriously missing"

  let test_should_remove_an_iobox_from_the_store_if_it_exists =
    testCase "should remove an iobox from the store if it exists" <| fun _ ->
      withStore <| fun patch store ->
        let slice : StringSliceD = { Index = 0u; Value = "swell" }
        let iobox = IOBox.String(Id "0xb33f", "hi", Id "0xb4d1d34", Array.empty, [| slice |])

        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| AddIOBox(iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(_) -> ()
          | None    -> failwith "iobox is mysteriously missing"

        store.Dispatch <| RemoveIOBox(iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(_) -> failwith "iobox should be missing by now but isn't"
          | _       -> ()

  //   ____
  //  / ___|   _  ___
  // | |  | | | |/ _ \
  // | |__| |_| |  __/
  //  \____\__,_|\___|

  let test_should_add_a_cue_to_the_store =
    testCase "should add a cue to the store" <| fun _ ->
      withStore <| fun patch store ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        expect "Should be 0" 0 id store.State.Cues.Count

        store.Dispatch <| AddCue cue

        expect "Should be 1" 1 id store.State.Cues.Count

        store.Dispatch <| AddCue cue

        expect "Should be 0" 1 id store.State.Cues.Count

  let test_should_update_a_cue_already_in_the_store =
    testCase "should update a cue already in the store" <| fun _ ->
      withStore <| fun patch store ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        expect "Should be 0" 0 id store.State.Cues.Count

        store.Dispatch <| AddCue cue

        expect "Should be 1" 1 id store.State.Cues.Count

        let newname = "aww yeah"
        store.Dispatch <| UpdateCue { cue with Name = newname }

        expect "Should be 1" 1 id store.State.Cues.Count
        expect "Should be correct name" newname id store.State.Cues.[cue.Id].Name

  let test_should_not_add_cue_to_the_store_on_update_when_missing =
    testCase "should not add cue to the store on update when missing" <| fun _ ->
      withStore <| fun patch store ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        expect "Should be 0" 0 id store.State.Cues.Count

        store.Dispatch <| UpdateCue cue

        expect "Should be 0" 0 id store.State.Cues.Count


  let test_should_remove_cue_from_the_store =
    testCase "should remove cue from the store" <| fun _ ->
      withStore <| fun patch store ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

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
      withStore <| fun patch store ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        expect "Should be 1" 1 id store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        expect "Should be 1" 1 id store.State.CueLists.Count


  let test_should_update_a_cuelist_already_in_the_store =
    testCase "should update a cuelist already in the store" <| fun _ ->
      withStore <| fun patch store ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        expect "Should be 1" 1 id store.State.CueLists.Count

        let newname = "aww yeah"
        store.Dispatch <| UpdateCueList { cuelist with Name = newname }

        expect "Should be 1" 1 id store.State.CueLists.Count
        expect "Should be correct name" newname id store.State.CueLists.[cuelist.Id].Name


  let test_should_not_add_cuelist_to_the_store_on_update_when_missing =
    testCase "should not add cuelist to the store on update when missing" <| fun _ ->
      withStore <| fun patch store ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        expect "Should be 0" 0 id store.State.CueLists.Count

        store.Dispatch <| UpdateCueList cuelist

        expect "Should be 0" 0 id store.State.CueLists.Count

  let test_should_remove_cuelist_from_the_store =
    testCase "should remove cuelist from the store" <| fun _ ->
      withStore <| fun patch store ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

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
      withStore <| fun patch store ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Password = "1234"
          ; Joined = DateTime.Now
          ; Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count

        store.Dispatch <| AddUser user

        expect "Should be 1" 1 id store.State.Users.Count

        store.Dispatch <| AddUser user

        expect "Should be 1" 1 id store.State.Users.Count

  let test_should_update_a_user_already_in_the_store =
    testCase "should update a user already in the store" <| fun _ ->
      withStore <| fun patch store ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Password = "1234"
          ; Joined  = DateTime.Now
          ; Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count

        store.Dispatch <| AddUser user

        expect "Should be 1" 1 id store.State.Users.Count

        let newname = "kurt mix master"
        store.Dispatch <| UpdateUser { user with FirstName = newname }

        expect "Should be 1" 1 id store.State.Users.Count
        expect "Should be correct name" newname id store.State.Users.[user.Id].FirstName


  let test_should_not_add_user_to_the_store_on_update_when_missing =
    testCase "should not add user to the store on update when missing" <| fun _ ->
      withStore <| fun patch store ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Password = "1234"
          ; Joined  = DateTime.Now
          ; Created = DateTime.Now }

        expect "Should be 0" 0 id store.State.Users.Count

        store.Dispatch <| UpdateUser user

        expect "Should be 0" 0 id store.State.Users.Count

  let test_should_remove_user_from_the_store =
    testCase "should remove user from the store" <| fun _ ->
      withStore <| fun patch store ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Password = "1234"
          ; Joined  = DateTime.Now
          ; Created = DateTime.Now }

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
      withStore <| fun patch store ->

        let session : Session =
          { Id = Id.Create()
          ; Status = { StatusType = Unauthorized; Payload = "" }
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count

        store.Dispatch <| AddSession session

        expect "Should be 1" 1 id store.State.Sessions.Count

        store.Dispatch <| AddSession session

        expect "Should be 1" 1 id store.State.Sessions.Count

  let test_should_update_a_session_already_in_the_store =
    testCase "should update a session already in the store" <| fun _ ->
      withStore <| fun patch store ->

        let session : Session =
          { Id = Id.Create()
          ; Status = { StatusType = Unauthorized; Payload = "" }
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count

        store.Dispatch <| AddSession session

        expect "Should be 1" 1 id store.State.Sessions.Count

        let newStatus = "kurt mix master"
        store.Dispatch <| UpdateSession { session with Status = { StatusType = Authorized; Payload = "" } }

        expect "Should be 1" 1 id store.State.Sessions.Count
        expect "Should be correct name" Authorized id store.State.Sessions.[session.Id].Status.StatusType

  let test_should_not_add_session_to_the_store_on_update_when_missing =
    testCase "should not add session to the store on update when missing" <| fun _ ->
      withStore <| fun patch store ->

        let session : Session =
          { Id = Id.Create()
          ; Status = { StatusType = Unauthorized; Payload = "" }
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count

        store.Dispatch <| UpdateSession session

        expect "Should be 0" 0 id store.State.Sessions.Count

  let test_should_remove_session_from_the_store =
    testCase "should remove session from the store" <| fun _ ->
      withStore <| fun patch store ->

        let session : Session =
          { Id = Id.Create()
          ; Status = { StatusType = Unauthorized; Payload = "" }
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        expect "Should be 0" 0 id store.State.Sessions.Count

        store.Dispatch <| AddSession session

        expect "Should be 1" 1 id store.State.Sessions.Count

        store.Dispatch <| RemoveSession session

        expect "Should be 0" 0 id store.State.Sessions.Count

  //  _   _           _         ______          _
  // | | | |_ __   __| | ___   / /  _ \ ___  __| | ___
  // | | | | '_ \ / _` |/ _ \ / /| |_) / _ \/ _` |/ _ \
  // | |_| | | | | (_| | (_) / / |  _ <  __/ (_| | (_) |
  //  \___/|_| |_|\__,_|\___/_/  |_| \_\___|\__,_|\___/

  let test_store_should_trigger_listeners_on_undo =
    testCase "store should trigger listeners on undo" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-2" })

        // subscribe now, so as to not fire too early ;)
        store.Subscribe(fun st ev ->
          match ev with
            | AddPatch(p) -> if p.Name <> patch.Name then failwith "Oh no!"
            | _ -> ())

        expect "Should be 3" 3 id store.History.Length
        store.Undo()

  let test_store_should_dump_previous_states_for_inspection =
    testCase "store should dump previous states for inspection" <| fun _ ->
      withStore <| fun patch store ->
        expect "Should be 1" 1 id store.History.Length
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-2" })
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-3" })
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-4" })
        expect "Should be 5" 5 id store.History.Length


  let test_should_have_correct_number_of_historic_states_when_starting_fresh =
    testCase "should have correct number of historic states when starting fresh" <| fun _ ->
      withStore <| fun patch store ->
        let patch2 : Patch = { patch with Name = "patch-2" }
        let patch3 : Patch = { patch2 with Name = "patch-3" }
        let patch4 : Patch = { patch3 with Name = "patch-4" }

        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( patch2)
        store.Dispatch <| UpdatePatch( patch3)
        store.Dispatch <| UpdatePatch( patch4)

        expect "Should be 5" 5 id store.History.Length

  let test_should_undo_a_single_change =
    testCase "should undo a single change" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Undo()
        expect "Should be shoudl corrent name" patch.Name id store.State.Patches.[patch.Id].Name

  let test_should_undo_two_changes =
    testCase "should undo two changes" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })
        store.Undo()
        store.Undo()
        expect "Should be shoudl corrent name" patch.Name id store.State.Patches.[patch.Id].Name

  let test_should_redo_an_undone_change =
    testCase "should redo an undone change" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Undo()
        expect "Should be 0" 0 id store.State.Patches.Count
        store.Redo()
        expect "Should be 1" 1 id store.State.Patches.Count

  let test_should_redo_multiple_undone_changes =
    testCase "should redo multiple undone changes" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })
        store.Dispatch <| UpdatePatch( { patch with Name = "mice" })
        store.Dispatch <| UpdatePatch( { patch with Name = "men"  })
        store.Undo()
        store.Undo()

        expect "Should be dogs" "dogs" id store.State.Patches.[patch.Id].Name
        store.Redo()

        expect "Should be mice" "mice" id store.State.Patches.[patch.Id].Name
        store.Redo()

        expect "Should be men" "men" id store.State.Patches.[patch.Id].Name
        store.Redo()

        expect "Should be men" "men" id store.State.Patches.[patch.Id].Name

  let test_should_undo_redo_interleaved_changes =
    testCase "should undo/redo interleaved changes" <| fun _ ->
      withStore <| fun patch store ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })

        store.Undo()
        expect "Should be cats" "cats" id store.State.Patches.[patch.Id].Name

        store.Redo()
        expect "Should be dogs" "dogs" id store.State.Patches.[patch.Id].Name

        store.Undo()
        expect "Should be cats" "cats" id store.State.Patches.[patch.Id].Name

        store.Dispatch <| UpdatePatch( { patch with Name = "mice" })

        store.Undo()
        expect "Should be dogs" "dogs" id store.State.Patches.[patch.Id].Name

        store.Redo()
        expect "Should be mice" "mice" id store.State.Patches.[patch.Id].Name

        store.Undo()
        store.Undo()

        expect "Should be cats" "cats" id store.State.Patches.[patch.Id].Name

        store.Dispatch <| UpdatePatch( { patch with Name = "men"  })

        store.Undo()
        expect "Should be mice" "mice" id store.State.Patches.[patch.Id].Name

        store.Redo()
        expect "Should be men" "men" id store.State.Patches.[patch.Id].Name

        expect "Should be 6" 6 id store.History.Length

  let test_should_only_keep_specified_number_of_undo_steps =
    testCase "should only keep specified number of undo-steps" <| fun _ ->
      withStore <| fun patch store ->
        store.UndoSteps <- 4
        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n -> store.Dispatch <| UpdatePatch( { patch with Name = n }))
        |> List.iter (fun _ -> store.Undo())

        expect "Should be 4" 4 id store.History.Length
        expect "Should be mice" "mice" id store.State.Patches.[patch.Id].Name

  let test_should_keep_all_state_in_history_in_debug_mode =
    testCase "should keep all state in history in debug mode" <| fun _ ->
      withStore <| fun patch store ->
        store.UndoSteps <- 2
        store.Debug <- true

        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| UpdatePatch( { patch with Name = n }))

        expect "Should be 8" 8 id store.History.Length

  let test_should_shrink_history_to_UndoSteps_after_leaving_debug_mode =
    testCase "should shrink history to UndoSteps after leaving debug mode" <| fun _ ->
      withStore <| fun patch store ->
        store.UndoSteps <- 3
        store.Debug <- true

        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| UpdatePatch( { patch with Name = n }))

        expect "Should be 8" 8 id store.History.Length
        store.Debug <- false
        expect "Should be 3" 3 id store.History.Length

  let storeTests =
    testList "Store Tests" [
      test_should_add_a_patch_to_the_store
      test_should_update_a_patch_already_in_the_store
      test_should_remove_a_patch_already_in_the_store
      test_should_add_an_iobox_to_the_store_if_patch_exists
      test_should_not_add_an_iobox_to_the_store_if_patch_does_not_exists
      test_should_update_an_iobox_in_the_store_if_it_already_exists
      test_should_remove_an_iobox_from_the_store_if_it_exists
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
    ]
