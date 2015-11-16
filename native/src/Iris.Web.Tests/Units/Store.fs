namespace Test.Units

open WebSharper
open WebSharper.Mocha
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
[<RequireQualifiedAccess>]
module Store =

  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Store
  open Iris.Web.Core.State
  open Iris.Web.Core.Events
  open Iris.Web.Core.Reducer

  [<Direct "Object.is($o1, $o2)">]
  let identical (o1 : obj) (o2 : obj) = X

  let main () =
    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Immutability"
    (*--------------------------------------------------------------------------*)

    test "store should be immutable" <| fun cb ->
      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }
      
      let store : Store<State> = new Store<State>(reducer, State.Empty)
      let state = store.State

      store.Dispatch <| PatchEvent(AddPatch, patch)

      let newstate = store.State

      check_cc (identical state newstate |> not) "should be a different object altogther" cb
      

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Patch operations"
    (*--------------------------------------------------------------------------*)

    test "should add a patch to the store" <| fun cb ->
      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      check ((List.length store.State.Patches) = 0) "patches list should be empty"
      store.Dispatch <| PatchEvent(AddPatch, patch)
      check_cc ((List.length store.State.Patches) = 1) "patches list length should be 1" cb

    (*--------------------------------------------------------------------------*)
    test "should update a patch already in the store" <| fun cb ->
      let name1 = "patch-1"
      let name2 = "patch-2"

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = name1
        ; ioboxes = Array.empty
        }

      let isPatch (p : Patch) : bool = p.id = patch.id

      let mutable store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Dispatch <| PatchEvent(AddPatch, patch)
      check (List.exists isPatch store.State.Patches) "patches list should contain patch"
      check (List.find isPatch store.State.Patches |> (fun p -> p.name = name1)) "patches list should contain patch"

      let updated = { patch with name = name2 }
      store.Dispatch <| PatchEvent(UpdatePatch,updated)
      check_cc (List.find isPatch store.State.Patches |> (fun p -> p.name = name2)) "patches list should contain patch" cb

    (*--------------------------------------------------------------------------*)
    test "should remove a patch already in the store" <| fun cb ->
      let patch : Patch =
        { id = "0xb33f"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let isPatch (p : Patch) : bool = p.id = patch.id

      let mutable store : Store<State> = new Store<State>(reducer, State.Empty)

      store.Dispatch <| PatchEvent(AddPatch, patch)
      check (List.exists isPatch store.State.Patches) "patches list should contain patch"

      store.Dispatch <| PatchEvent(RemovePatch, patch)
      check_cc (not (List.exists isPatch store.State.Patches)) "patches list should not contain patch" cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - IOBox operations"
    (*--------------------------------------------------------------------------*)

    test "should add an iobox to the store if patch exists" <| fun cb ->
      let patchid = "0xb4d1d34"

      let patch : Patch =
        { id = patchid
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let mutable store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Dispatch <| PatchEvent(AddPatch, patch)

      match store.State.Patches with
        | patch :: [] -> check ((Array.length patch.ioboxes) = 0) "iobox array length should be 0"
        | _ -> check false "patches list is empty but should contain at least one patch"

      let iobox =
        { id     = "0xb33f"
        ; name   = "url input"
        ; patch  = patchid
        ; kind   = "string"
        ; slices = [| { idx = 0; value = "Hey" } |]
        }

      store.Dispatch <| IOBoxEvent(AddIOBox, iobox)

      match store.State.Patches with
        | patch :: [] -> check_cc ((Array.length patch.ioboxes) = 1) "iobox array length should be 1" cb
        | _ -> check false "patches list is empty but should contain at least one patch"

    (*--------------------------------------------------------------------------*)
    test "should not add an iobox to the store if patch does not exists" <| fun cb ->
      let patchid = "0xb4d1d34"

      let mutable store : Store<State> = new Store<State>(reducer, State.Empty)

      let iobox =
        { id     = "0xb33f"
        ; name   = "url input"
        ; patch  = patchid
        ; kind   = "string"
        ; slices = [| { idx = 0; value = "Hey" } |]
        }

      store.Dispatch <| IOBoxEvent(AddIOBox, iobox)
      check_cc ((List.length store.State.Patches) = 0) "patches list length should be 0" cb

    (*--------------------------------------------------------------------------*)
    test "should update an iobox in the store if it already exists" <| fun cb ->
      let name1 = "can a cat own a cat?"
      let name2 = "yes, cats are re-entrant."

      let iobox =
        { id     = "0xb33f"
        ; name   = name1
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = "swell" } |]
        }

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = [| iobox |]
        }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Dispatch <| PatchEvent(AddPatch, patch)

      match findIOBox store.State.Patches iobox.id with
        | Some(i) -> check_cc (i.name = name1) "name of iobox does not match (1)" cb
        | None -> check_cc false "iobox is mysteriously missing" cb

      let updated = { iobox with name = name2 }
      store.Dispatch <| IOBoxEvent(UpdateIOBox, updated)

      match findIOBox store.State.Patches iobox.id with
        | Some(i) -> check_cc (i.name = name2) "name of iobox does not match (2)" cb
        | None -> check_cc false "iobox is mysteriously missing" cb

    (*--------------------------------------------------------------------------*)
    test "should remove an iobox from the store if it exists" <| fun cb ->
      let boxid = "0xb33f"

      let iobox =
        { id     = boxid
        ; name   = "hi"
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = "swell" } |]
        }

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = [| iobox |]
        }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Dispatch <| PatchEvent(AddPatch, patch)

      match findIOBox store.State.Patches boxid with
        | Some(_) -> check true  "iobox should be found by now"
        | None    -> check false "iobox is mysteriously missing"

      store.Dispatch <| IOBoxEvent(RemoveIOBox, iobox)

      match findIOBox store.State.Patches boxid with
        | Some(_) -> check_cc false "iobox should be missing by now but isn't" cb
        | None    -> check_cc true "iobox was found but should be missing" cb

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.Store - Debug Mode"
    (*--------------------------------------------------------------------------*)

    test "should have correct number of historic states when starting fresh" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Debug(true) 

      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)

      (store.Dump().Length ==>> 5) cb


    (*------------------------------------------------------------------------*)
    test "should have correct number of historic states when started after 1 event" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)

      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Debug(true) 
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)

      (store.Dump().Length ==>> 4) cb


    (*------------------------------------------------------------------------*)
    test "should have correct number of historic states when started after 2 events" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)

      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Debug(true) 
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)

      (store.Dump().Length ==>> 3) cb


    (*------------------------------------------------------------------------*)
    test "store should store previous states in debug mode" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Debug(true) 

      store.Dispatch <| PatchEvent(AddPatch, patch1)

      (List.head store.State.Patches).name |==| "patch-1"

      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      (List.head store.State.Patches).name |==| "patch-2"

      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      (List.head store.State.Patches).name |==| "patch-3"

      // this is HEAD
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)
      (List.head store.State.Patches).name |==| "patch-4"

      store.Previous()
      (List.head store.State.Patches).name |==| "patch-3"

      store.Previous()
      (List.head store.State.Patches).name |==| "patch-2"

      store.Previous()
      (List.head store.State.Patches).name |==| "patch-1"

      store.Previous()
      List.length store.State.Patches |==| 0
                                         
      store.Previous()                   
      List.length store.State.Patches |==| 0

      store.Next()
      (List.head store.State.Patches).name |==| "patch-1"

      store.Next()
      (List.head store.State.Patches).name |==| "patch-2"

      store.Next()
      (List.head store.State.Patches).name |==| "patch-3"

      store.Next()
      (List.head store.State.Patches).name |==| "patch-4"

      store.Next()
      (List.head store.State.Patches).name |==| "patch-4"


    (*--------------------------------------------------------------------------*)
    test "store should trigger listeners on tick" <| fun cb ->
      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Subscribe(fun st ev ->
        match ev with
          | PatchEvent(AddPatch, p) -> if p.name = "patch-1" then cb ()
          | _ -> ())

      store.Debug(true) 
      store.Dispatch <| PatchEvent(AddPatch, patch)
      store.Dispatch <| PatchEvent(UpdatePatch, { patch with name = "patch-2" })

      store.Dump().Length |==| 3
      store.Previous()

    (*--------------------------------------------------------------------------*)
    test "tick should not do anything when not in debug mode" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let store : Store<State> = new Store<State>(reducer, State.Empty)

      store.Dispatch <| PatchEvent(AddPatch, patch1)
      check ((List.head store.State.Patches).name = "patch-1") "State.Patches should have patch-1"
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      check ((List.head store.State.Patches).name = "patch-2") "State.Patches should have patch-2"
      store.Previous()
      check_cc ((List.head store.State.Patches).name = "patch-2") "State.Patches should have patch-2" cb

    (*--------------------------------------------------------------------------*)
    test "store should dump previous states for inspection" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Debug(true) 

      store.Dump().Length |==| 1

      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)

      (store.Dump().Length ==>> 5) cb

    (*--------------------------------------------------------------------------*)
    test "store should release states when turning Debug off" <| fun cb ->
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)

      store.Debug(true) 
      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)
      store.Debug(false) 

      (store.Dump().Length ==>> 0) cb


    test "should restore to last state added on leaving debug mode" <| fun cb -> 
      let patch1 : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch = { patch1 with name = "patch-2" }
      let patch3 : Patch = { patch2 with name = "patch-3" }
      let patch4 : Patch = { patch3 with name = "patch-4" }

      let store : Store<State> = new Store<State>(reducer, State.Empty)
      store.Debug(true) 
      store.Dispatch <| PatchEvent(AddPatch, patch1)
      store.Dispatch <| PatchEvent(UpdatePatch, patch2)
      store.Dispatch <| PatchEvent(UpdatePatch, patch3)
      store.Dispatch <| PatchEvent(UpdatePatch, patch4)
      store.Previous()
      store.Previous()

      (List.head store.State.Patches).name |==| "patch-2"

      store.Debug(false) 

      ((List.head store.State.Patches).name ==>> "patch-4") cb
