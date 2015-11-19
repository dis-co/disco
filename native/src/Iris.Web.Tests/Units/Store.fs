namespace Test.Units

open WebSharper
open WebSharper.Mocha
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
[<RequireQualifiedAccess>]
module Store =

  open Iris.Core.Types
  open Iris.Web.Core

  [<Direct "Object.is($o1, $o2)">]
  let identical (o1 : obj) (o2 : obj) = X

  let withStore (wrap : Patch -> Store<State> -> unit) =
    let patch : Patch =
      { Id = "0xb4d1d34"
      ; Name = "patch-1"
      ; IOBoxes = Array.empty
      }

    let store : Store<State> = new Store<State>(reducer, State.Empty)
    wrap patch store

  let main () =
    (****************************************************************************)
    suite "Test.Units.Store - Immutability"
    (****************************************************************************)

    withStore <| fun patch store ->
      test "store should be immutable" <| fun cb ->
        let state = store.State
        store.Dispatch <| PatchEvent(AddPatch, patch)
        let newstate = store.State
        (identical state newstate ==>> false) cb


    (****************************************************************************)
    suite "Test.Units.Store - Patch operations"
    (****************************************************************************)

    withStore <| fun patch store ->
      test "should add a patch to the store" <| fun cb ->
        (List.length store.State.Patches) |==| 0
        store.Dispatch <| PatchEvent(AddPatch, patch)
        ((List.length store.State.Patches) ==>> 1) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should update a patch already in the store" <| fun cb ->
        let name1 = patch.Name
        let name2 = "patch-2"

        let isPatch (p : Patch) : bool = p.Id = patch.Id

        store.Dispatch <| PatchEvent(AddPatch, patch)
        (List.exists isPatch store.State.Patches) |==| true
        (List.find isPatch store.State.Patches |> (fun p -> p.Name = name1)) |==| true

        let updated = { patch with Name = name2 }
        store.Dispatch <| PatchEvent(UpdatePatch,updated)
        ((List.find isPatch store.State.Patches |> (fun p -> p.Name = name2)) ==>> true) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should remove a patch already in the store" <| fun cb ->
        let isPatch (p : Patch) : bool = p.Id = patch.Id

        store.Dispatch <| PatchEvent(AddPatch, patch)
        (List.exists isPatch store.State.Patches) |==| true

        store.Dispatch <| PatchEvent(RemovePatch, patch)
        ((List.exists isPatch store.State.Patches) ==>> false) cb

    (****************************************************************************)
    suite "Test.Units.Store - IOBox operations"
    (****************************************************************************)

    withStore <| fun patch store ->
      test "should add an iobox to the store if patch exists" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)

        match store.State.Patches with
          | patch :: [] -> check ((Array.length patch.IOBoxes) = 0) "iobox array length should be 0"
          | _ -> check false "patches list is empty but should contain at least one patch"

        let iobox =
          { IOBox.StringBox("0xb33f","url input", patch.Id)
              with Slices = [| { Idx = 0; Value = "Hey" } |] }

        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match store.State.Patches with
          | patch :: [] -> ((Array.length patch.IOBoxes) ==>> 1) cb
          | _ -> fail "patches list is empty but should contain at least one patch"

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should not add an iobox to the store if patch does not exists" <| fun cb ->
        let iobox =
          { IOBox.StringBox("0xb33f","url input", patch.Id)
              with Slices = [| { Idx = 0; Value = "Hey" } |] }

        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)
        ((List.length store.State.Patches) ==>> 0) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should update an iobox in the store if it already exists" <| fun cb ->
        let name1 = "can a cat own a cat?"
        let name2 = "yes, cats are re-entrant."

        let iobox =
          { IOBox.StringBox("0xb33f", name1, patch.Id)
              with Slices = [| { Idx = 0; Value = "swell" } |] }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match findIOBox store.State.Patches iobox.Id with
          | Some(i) -> i.Name |==| name1
          | None -> fail "iobox is mysteriously missing"

        let updated = { iobox with Name = name2 }
        store.Dispatch <| IOBoxEvent(UpdateIOBox, updated)

        match findIOBox store.State.Patches iobox.Id with
          | Some(i) -> (i.Name ==>> name2) cb
          | None -> fail "iobox is mysteriously missing"

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should remove an iobox from the store if it exists" <| fun cb ->
        let iobox =
          { IOBox.StringBox("0xb33f", "hi", "0xb4d1d34")
              with Slices = [| { Idx = 0; Value = "swell" } |] }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

        match findIOBox store.State.Patches iobox.Id with
          | Some(_) -> ()
          | None    -> fail "iobox is mysteriously missing"

        store.Dispatch <| IOBoxEvent(RemoveIOBox, iobox)

        match findIOBox store.State.Patches iobox.Id with
          | Some(_) -> fail "iobox should be missing by now but isn't"
          | None    -> success cb

    (****************************************************************************)
    suite "Test.Units.Store - Undo/Redo"
    (****************************************************************************)


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "store should trigger listeners on undo" <| fun cb ->
        store.Subscribe(fun st ev ->
          match ev with
            | PatchEvent(AddPatch, p) -> if p.Name = patch.Name then cb ()
            | _ -> ())

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "patch-2" })

        store.History.Length |==| 3
        store.Undo()


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "store should dump previous states for inspection" <| fun cb ->
        store.History.Length |==| 1
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "patch-2" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "patch-3" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "patch-4" })
        (store.History.Length ==>> 5) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should have correct number of historic states when starting fresh" <| fun cb ->
        let patch2 : Patch = { patch with Name = "patch-2" }
        let patch3 : Patch = { patch2 with Name = "patch-3" }
        let patch4 : Patch = { patch3 with Name = "patch-4" }

        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, patch2)
        store.Dispatch <| PatchEvent(UpdatePatch, patch3)
        store.Dispatch <| PatchEvent(UpdatePatch, patch4)

        (store.History.Length ==>> 5) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should undo a single change" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "cats" })
        store.Undo()
        ((List.head store.State.Patches).Name ==>> patch.Name) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should undo two changes" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "dogs" })
        store.Undo()
        store.Undo()
        ((List.head store.State.Patches).Name ==>> patch.Name) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should redo an undone change" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Undo()
        List.length store.State.Patches |==| 0
        store.Redo()
        (List.length store.State.Patches ==>> 1) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should redo multiple undone changes" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "dogs" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "mice" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "men"  })
        store.Undo()
        store.Undo()
        (List.head store.State.Patches).Name |==| "dogs"
        store.Redo()
        (List.head store.State.Patches).Name |==| "mice"
        store.Redo()
        (List.head store.State.Patches).Name |==| "men"
        store.Redo()
        ((List.head store.State.Patches).Name ==>> "men") cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should undo/redo interleaved changes" <| fun cb ->
        store.Dispatch <| PatchEvent(AddPatch, patch)
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "cats" })
        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "dogs" })

        store.Undo()
        (List.head store.State.Patches).Name |==| "cats"

        store.Redo()
        (List.head store.State.Patches).Name |==| "dogs"

        store.Undo()
        (List.head store.State.Patches).Name |==| "cats"

        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "mice" })

        store.Undo()
        (List.head store.State.Patches).Name |==| "dogs"

        store.Redo()
        (List.head store.State.Patches).Name |==| "mice"

        store.Undo()
        store.Undo()
        (List.head store.State.Patches).Name |==| "cats"

        store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = "men"  })

        store.Undo()
        (List.head store.State.Patches).Name |==| "mice"

        store.Redo()
        (List.head store.State.Patches).Name |==| "men"

        (store.History.Length ==>> 6) cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should only keep specified number of undo-steps" <| fun cb ->
        store.UndoSteps <- 4
        store.Dispatch <| PatchEvent(AddPatch, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.map (fun n ->
             store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = n }))
        |> List.iter (fun _ -> store.Undo())

        store.History.Length |==| 4
        ((List.head store.State.Patches).Name ==>> "mice") cb


    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should keep all state in history in debug mode" <| fun cb ->
        store.UndoSteps <- 2
        store.Debug(true)

        store.Dispatch <| PatchEvent(AddPatch, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = n }))

        (store.History.Length ==>> 8) cb

    (*--------------------------------------------------------------------------*)
    withStore <| fun patch store ->
      test "should shrink history to UndoSteps after leaving debug mode" <| fun cb ->
        store.UndoSteps <- 3
        store.Debug(true)

        store.Dispatch <| PatchEvent(AddPatch, patch)

        ["dogs"; "cats"; "mice"; "men"; "worms"; "hens"]
        |> List.iter (fun n ->
            store.Dispatch <| PatchEvent(UpdatePatch, { patch with Name = n }))

        store.History.Length |==| 8
        store.Debug(false)
        store.History.Length ==>> 3 <| cb
