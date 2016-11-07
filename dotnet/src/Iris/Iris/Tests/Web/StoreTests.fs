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

  let withStore (wrap : Patch -> Store -> unit) =
    let patch : Patch =
      { Id = Id "0xb4d1d34"
      ; Name = "patch-1"
      ; IOBoxes = Map.empty
      }

    let store : Store = new Store(State.Empty)
    wrap patch store

  let main () =
    (* ----------------------------------------------------------------------- *)
    suite "Test.Units.Store - Immutability:"
    (* ----------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "store should be immutable" <| fun finish ->
        let state = store.State
        store.Dispatch <| AddPatch(patch)
        let newstate = store.State
        equals false (Object.ReferenceEquals(state, newstate))
        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Patch operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a patch to the store" <| fun finish ->
        equals 0 store.State.Patches.Count
        store.Dispatch <| AddPatch(patch)
        equals 1 store.State.Patches.Count
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a patch already in the store" <| fun finish ->
        let name1 = patch.Name
        let name2 = "patch-2"

        store.Dispatch <| AddPatch(patch)

        equals true (store.State.Patches.ContainsKey patch.Id)
        equals true (store.State.Patches.[patch.Id].Name = name1)

        let updated = { patch with Name = name2 }
        store.Dispatch <| UpdatePatch(updated)

        equals true (store.State.Patches.[patch.Id].Name = name2)

        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove a patch already in the store" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        equals true (store.State.Patches.ContainsKey patch.Id)

        store.Dispatch <| RemovePatch(patch)
        equals false (store.State.Patches.ContainsKey patch.Id)

        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - IOBox operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add an iobox to the store if patch exists" <| fun finish ->
        store.Dispatch <| AddPatch(patch)

        equals 0 store.State.Patches.[patch.Id].IOBoxes.Count

        let slice : StringSliceD = { Index = 0u; Value = "Hey" }
        let iobox : IOBox = IOBox.String(Id "0xb33f","url input", patch.Id, Array.empty, [| slice |])

        store.Dispatch <| AddIOBox(iobox)

        equals 1 store.State.Patches.[patch.Id].IOBoxes.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add an iobox to the store if patch does not exists" <| fun finish ->
        let slice : StringSliceD = { Index = 0u; Value =  "Hey" }
        let iobox = IOBox.String(Id "0xb33f","url input", patch.Id, Array.empty, [| slice |])
        store.Dispatch <| AddIOBox(iobox)
        equals 0 store.State.Patches.Count
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update an iobox in the store if it already exists" <| fun finish ->
        let name1 = "can a cat own a cat?"
        let name2 = "yes, cats are re-entrant."

        let slice : StringSliceD = { Index = 0u; Value = "swell" }
        let iobox = IOBox.String(Id "0xb33f", name1, patch.Id, Array.empty, [| slice |])

        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| AddIOBox(iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> equals name1 i.Name
          | None    -> failwith "iobox is mysteriously missing"

        let updated = iobox.SetName name2
        store.Dispatch <| UpdateIOBox(updated)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> equals name2 i.Name
          | None    -> failwith "iobox is mysteriously missing"

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove an iobox from the store if it exists" <| fun finish ->
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
          | None    -> finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Cue operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a cue to the store" <| fun finish ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        equals 0 store.State.Cues.Count

        store.Dispatch <| AddCue cue

        equals 1 store.State.Cues.Count

        store.Dispatch <| AddCue cue

        equals 1 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a cue already in the store" <| fun finish ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        equals 0 store.State.Cues.Count

        store.Dispatch <| AddCue cue

        equals 1 store.State.Cues.Count

        let newname = "aww yeah"
        store.Dispatch <| UpdateCue { cue with Name = newname }

        equals 1 store.State.Cues.Count
        equals newname store.State.Cues.[cue.Id].Name

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add cue to the store on update when missing" <| fun finish ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        equals 0 store.State.Cues.Count

        store.Dispatch <| UpdateCue cue

        equals 0 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove cue from the store" <| fun finish ->

        let cue : Cue = { Id = Id.Create(); Name = "My Cue"; IOBoxes = [| |] }

        equals 0 store.State.Cues.Count

        store.Dispatch <| AddCue cue

        equals 1 store.State.Cues.Count

        store.Dispatch <| RemoveCue cue

        equals 0 store.State.Cues.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - CueList operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a cuelist to the store" <| fun finish ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        equals 0 store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        equals 1 store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        equals 1 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a cuelist already in the store" <| fun finish ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        equals 0 store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        equals 1 store.State.CueLists.Count

        let newname = "aww yeah"
        store.Dispatch <| UpdateCueList { cuelist with Name = newname }

        equals 1 store.State.CueLists.Count
        equals newname store.State.CueLists.[cuelist.Id].Name

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add cuelist to the store on update when missing" <| fun finish ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        equals 0 store.State.CueLists.Count

        store.Dispatch <| UpdateCueList cuelist

        equals 0 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove cuelist from the store" <| fun finish ->

        let cuelist : CueList = { Id = Id.Create(); Name = "My CueList"; Cues = [| |] }

        equals 0 store.State.CueLists.Count

        store.Dispatch <| AddCueList cuelist

        equals 1 store.State.CueLists.Count

        store.Dispatch <| RemoveCueList cuelist

        equals 0 store.State.CueLists.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - User operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a user to the store" <| fun finish ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Joined = "today"
          ; Created = "yesterday" }

        equals 0 store.State.Users.Count

        store.Dispatch <| AddUser user

        equals 1 store.State.Users.Count

        store.Dispatch <| AddUser user

        equals 1 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a user already in the store" <| fun finish ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Joined = "today"
          ; Created = "yesterday" }

        equals 0 store.State.Users.Count

        store.Dispatch <| AddUser user

        equals 1 store.State.Users.Count

        let newname = "kurt mix master"
        store.Dispatch <| UpdateUser { user with FirstName = newname }

        equals 1 store.State.Users.Count
        equals newname store.State.Users.[user.Id].FirstName

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add user to the store on update when missing" <| fun finish ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Joined = "today"
          ; Created = "yesterday" }

        equals 0 store.State.Users.Count

        store.Dispatch <| UpdateUser user

        equals 0 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove user from the store" <| fun finish ->

        let user : User =
          { Id = Id.Create()
          ; UserName = "krgn"
          ; FirstName = "Karsten"
          ; LastName = "Gebbert"
          ; Email = "k@ioctl.it"
          ; Joined = "today"
          ; Created = "yesterday" }

        equals 0 store.State.Users.Count

        store.Dispatch <| AddUser user

        equals 1 store.State.Users.Count

        store.Dispatch <| RemoveUser user

        equals 0 store.State.Users.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Session operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a session to the store" <| fun finish ->

        let session : Session =
          { Id = Id.Create()
          ; UserName = "Karsten"
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count

        store.Dispatch <| AddSession session

        equals 1 store.State.Sessions.Count

        store.Dispatch <| AddSession session

        equals 1 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a Session already in the store" <| fun finish ->

        let session : Session =
          { Id = Id.Create()
          ; UserName = "Karsten"
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count

        store.Dispatch <| AddSession session

        equals 1 store.State.Sessions.Count

        let newname = "kurt mix master"
        store.Dispatch <| UpdateSession { session with UserName = newname }

        equals 1 store.State.Sessions.Count
        equals newname store.State.Sessions.[session.Id].UserName

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add Session to the store on update when missing" <| fun finish ->

        let session : Session =
          { Id = Id.Create()
          ; UserName = "Karsten"
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count

        store.Dispatch <| UpdateSession session

        equals 0 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove Session from the store" <| fun finish ->

        let session : Session =
          { Id = Id.Create()
          ; UserName = "Karsten"
          ; IpAddress = IPv4Address "126.0.0.1"
          ; UserAgent = "Firefuckingfox" }

        equals 0 store.State.Sessions.Count

        store.Dispatch <| AddSession session

        equals 1 store.State.Sessions.Count

        store.Dispatch <| RemoveSession session

        equals 0 store.State.Sessions.Count

        finish ()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Undo/Redo"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "store should trigger listeners on undo" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-2" })

        // subscribe now, so as to not fire too early ;)
        store.Subscribe(fun st ev ->
          match ev with
            | AddPatch(p) -> if p.Name = patch.Name then finish ()
            | _ -> ())

        equals 3 store.History.Length
        store.Undo()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "store should dump previous states for inspection" <| fun finish ->
        equals 1 store.History.Length
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-2" })
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-3" })
        store.Dispatch <| UpdatePatch( { patch with Name = "patch-4" })
        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should have correct number of historic states when starting fresh" <| fun finish ->
        let patch2 : Patch = { patch with Name = "patch-2" }
        let patch3 : Patch = { patch2 with Name = "patch-3" }
        let patch4 : Patch = { patch3 with Name = "patch-4" }

        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( patch2)
        store.Dispatch <| UpdatePatch( patch3)
        store.Dispatch <| UpdatePatch( patch4)

        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo a single change" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Undo()
        equals patch.Name store.State.Patches.[patch.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo two changes" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })
        store.Undo()
        store.Undo()
        equals patch.Name store.State.Patches.[patch.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should redo an undone change" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Undo()
        equals 0 store.State.Patches.Count
        store.Redo()
        equals 1 store.State.Patches.Count
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should redo multiple undone changes" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })
        store.Dispatch <| UpdatePatch( { patch with Name = "mice" })
        store.Dispatch <| UpdatePatch( { patch with Name = "men"  })
        store.Undo()
        store.Undo()

        equals "dogs" store.State.Patches.[patch.Id].Name
        store.Redo()

        equals "mice" store.State.Patches.[patch.Id].Name
        store.Redo()

        equals "men" store.State.Patches.[patch.Id].Name
        store.Redo()

        equals "men" store.State.Patches.[patch.Id].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo/redo interleaved changes" <| fun finish ->
        store.Dispatch <| AddPatch(patch)
        store.Dispatch <| UpdatePatch( { patch with Name = "cats" })
        store.Dispatch <| UpdatePatch( { patch with Name = "dogs" })

        store.Undo()
        equals "cats" store.State.Patches.[patch.Id].Name

        store.Redo()
        equals "dogs" store.State.Patches.[patch.Id].Name

        store.Undo()
        equals "cats" store.State.Patches.[patch.Id].Name

        store.Dispatch <| UpdatePatch( { patch with Name = "mice" })

        store.Undo()
        equals "dogs" store.State.Patches.[patch.Id].Name

        store.Redo()
        equals "mice" store.State.Patches.[patch.Id].Name

        store.Undo()
        store.Undo()

        equals "cats" store.State.Patches.[patch.Id].Name

        store.Dispatch <| UpdatePatch( { patch with Name = "men"  })

        store.Undo()
        equals "mice" store.State.Patches.[patch.Id].Name

        store.Redo()
        equals "men" store.State.Patches.[patch.Id].Name

        equals 6 store.History.Length
        finish ()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should only keep specified number of undo-steps" <| fun finish ->
        store.UndoSteps <- 4
        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n ->
             store.Dispatch <| UpdatePatch( { patch with Name = n }))
        |> List.iter (fun _ -> store.Undo())

        equals 4      store.History.Length
        equals "mice" store.State.Patches.[patch.Id].Name
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should keep all state in history in debug mode" <| fun finish ->
        store.UndoSteps <- 2
        store.Debug <- true

        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| UpdatePatch( { patch with Name = n }))

        equals 8 store.History.Length
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should shrink history to UndoSteps after leaving debug mode" <| fun finish ->
        store.UndoSteps <- 3
        store.Debug <- true

        store.Dispatch <| AddPatch(patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| UpdatePatch( { patch with Name = n }))

        equals 8 store.History.Length
        store.Debug <- false
        equals 3 store.History.Length
        finish ()
