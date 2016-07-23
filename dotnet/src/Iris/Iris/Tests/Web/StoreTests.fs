namespace Test.Units

[<RequireQualifiedAccess>]
module Store =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests

  let withStore (wrap : Patch -> Store<State> -> unit) =
    let patch : Patch =
      { Id = Guid "0xb4d1d34"
      ; Name = "patch-1"
      ; IOBoxes = Array.empty
      }

    let store : Store<State> = new Store<State>(Reducer, State.Empty)
    wrap patch store

  let main () =
    (* ----------------------------------------------------------------------- *)
    suite "Test.Units.Store - Immutability"
    (* ----------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "store should be immutable" <| fun finish ->
        let state = store.State
        store.Dispatch <| PatchEvent(Create, patch)
        let newstate = store.State
        equals false (identical state newstate)
        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Patch operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add a patch to the store" <| fun finish ->
        equals 0 (Array.length store.State.Patches)
        store.Dispatch <| PatchEvent(Create, patch)
        equals 1 (Array.length store.State.Patches)
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update a patch already in the store" <| fun finish ->
        let name1 = patch.Name
        let name2 = "patch-2"

        let isPatch (p : Patch) : bool = p.Id = patch.Id

        store.Dispatch <| PatchEvent(Create, patch)

        equals true (Array.exists isPatch store.State.Patches)
        equals true (Array.find isPatch store.State.Patches |> (fun p -> p.Name = name1))

        let updated = { patch with Name = name2 }
        store.Dispatch <| PatchEvent(Update,updated)

        equals true (Array.find isPatch store.State.Patches |> (fun p -> p.Name = name2))

        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove a patch already in the store" <| fun finish ->
        let isPatch (p : Patch) : bool = p.Id = patch.Id

        store.Dispatch <| PatchEvent(Create, patch)
        equals true (Array.exists isPatch store.State.Patches)

        store.Dispatch <| PatchEvent(Delete, patch)
        equals false (Array.exists isPatch store.State.Patches)

        finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - IOBox operations"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "should add an iobox to the store if patch exists" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)

        equals 0 (Array.length store.State.Patches.[0].IOBoxes)

        let slice : StringSliceD = { Index = 0UL; Value = "Hey" }
        let iobox : IOBox = IOBox.String(Guid "0xb33f","url input", patch.Id, Array.empty, [| slice |])

        store.Dispatch <| IOBoxEvent(Create, iobox)
        equals 1 (Array.length store.State.Patches.[0].IOBoxes)
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should not add an iobox to the store if patch does not exists" <| fun finish ->
        let slice : StringSliceD = { Index = 0UL; Value =  "Hey" }
        let iobox = IOBox.String(Guid "0xb33f","url input", patch.Id, Array.empty, [| slice |])
        store.Dispatch <| IOBoxEvent(Create, iobox)
        equals 0 (Array.length store.State.Patches)
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should update an iobox in the store if it already exists" <| fun finish ->
        let name1 = "can a cat own a cat?"
        let name2 = "yes, cats are re-entrant."

        let slice : StringSliceD = { Index = 0UL; Value = "swell" }
        let iobox = IOBox.String(Guid "0xb33f", name1, patch.Id, Array.empty, [| slice |])

        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| IOBoxEvent(Create, iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> equals name1 i.Name
          | None    -> failwith "iobox is mysteriously missing"

        let updated = iobox.SetName name2
        store.Dispatch <| IOBoxEvent(Update, updated)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(i) -> equals name2 i.Name
          | None    -> failwith "iobox is mysteriously missing"

        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should remove an iobox from the store if it exists" <| fun finish ->
        let slice : StringSliceD = { Index = 0UL; Value = "swell" }
        let iobox = IOBox.String(Guid "0xb33f", "hi", Guid "0xb4d1d34", Array.empty, [| slice |])

        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| IOBoxEvent(Create, iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(_) -> ()
          | None    -> failwith "iobox is mysteriously missing"

        store.Dispatch <| IOBoxEvent(Delete, iobox)

        match Patch.FindIOBox store.State.Patches iobox.Id with
          | Some(_) -> failwith "iobox should be missing by now but isn't"
          | None    -> finish()

    (* ---------------------------------------------------------------------- *)
    suite "Test.Units.Store - Undo/Redo"
    (* ---------------------------------------------------------------------- *)

    withStore <| fun patch store ->
      test "store should trigger listeners on undo" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "patch-2" })

        // subscribe now, so as to not fire too early ;)
        store.Subscribe(fun st ev ->
          match ev with
            | PatchEvent(Create, p) -> if p.Name = patch.Name then finish ()
            | _ -> ())

        equals 3 store.History.Length
        store.Undo()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "store should dump previous states for inspection" <| fun finish ->
        equals 1 store.History.Length
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "patch-2" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "patch-3" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "patch-4" })
        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should have correct number of historic states when starting fresh" <| fun finish ->
        let patch2 : Patch = { patch with Name = "patch-2" }
        let patch3 : Patch = { patch2 with Name = "patch-3" }
        let patch4 : Patch = { patch3 with Name = "patch-4" }

        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, patch2)
        store.Dispatch <| PatchEvent(Update, patch3)
        store.Dispatch <| PatchEvent(Update, patch4)

        equals 5 store.History.Length
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo a single change" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "cats" })
        store.Undo()
        equals patch.Name store.State.Patches.[0].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo two changes" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "dogs" })
        store.Undo()
        store.Undo()
        equals patch.Name store.State.Patches.[0].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should redo an undone change" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Undo()
        equals 0 (Array.length store.State.Patches)
        store.Redo()
        equals 1 (Array.length store.State.Patches)
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should redo multiple undone changes" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "dogs" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "mice" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "men"  })
        store.Undo()
        store.Undo()

        equals "dogs" store.State.Patches.[0].Name
        store.Redo()

        equals "mice" store.State.Patches.[0].Name
        store.Redo()

        equals "men" store.State.Patches.[0].Name
        store.Redo()

        equals "men" store.State.Patches.[0].Name
        finish()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should undo/redo interleaved changes" <| fun finish ->
        store.Dispatch <| PatchEvent(Create, patch)
        store.Dispatch <| PatchEvent(Update, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(Update, { patch with Name = "dogs" })

        store.Undo()
        equals "cats" store.State.Patches.[0].Name

        store.Redo()
        equals "dogs" store.State.Patches.[0].Name

        store.Undo()
        equals "cats" store.State.Patches.[0].Name

        store.Dispatch <| PatchEvent(Update, { patch with Name = "mice" })

        store.Undo()
        equals "dogs" store.State.Patches.[0].Name

        store.Redo()
        equals "mice" store.State.Patches.[0].Name

        store.Undo()
        store.Undo()

        equals "cats" store.State.Patches.[0].Name

        store.Dispatch <| PatchEvent(Update, { patch with Name = "men"  })

        store.Undo()
        equals "mice" store.State.Patches.[0].Name

        store.Redo()
        equals "men" store.State.Patches.[0].Name

        equals 6 store.History.Length
        finish ()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should only keep specified number of undo-steps" <| fun finish ->
        store.UndoSteps <- 4
        store.Dispatch <| PatchEvent(Create, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n ->
             store.Dispatch <| PatchEvent(Update, { patch with Name = n }))
        |> List.iter (fun _ -> store.Undo())

        equals 4      store.History.Length
        equals "mice" store.State.Patches.[0].Name
        finish()


    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should keep all state in history in debug mode" <| fun finish ->
        store.UndoSteps <- 2
        store.Debug <- true

        store.Dispatch <| PatchEvent(Create, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| PatchEvent(Update, { patch with Name = n }))

        equals 8 store.History.Length
        finish ()

    (* ---------------------------------------------------------------------- *)
    withStore <| fun patch store ->
      test "should shrink history to UndoSteps after leaving debug mode" <| fun finish ->
        store.UndoSteps <- 3
        store.Debug <- true

        store.Dispatch <| PatchEvent(Create, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| PatchEvent(Update, { patch with Name = n }))

        equals 8 store.History.Length
        store.Debug <- false
        equals 3 store.History.Length
        finish ()
