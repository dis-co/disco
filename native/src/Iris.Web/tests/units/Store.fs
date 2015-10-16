[<ReflectedDefinition>]
module Test.Units.Store

open FunScript
open FunScript.Mocha
open FunScript.TypeScript

open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch
open Iris.Web.Core.Store
open Iris.Web.Core.Events
open Iris.Web.Core.Reducer

let main () =
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Store - Patch operations"
  (*--------------------------------------------------------------------------*)

  test "should add a patch to the store" <| fun cb ->
    let patch : Patch =
      { id = "0xb4d1d34"
      ; name = "patch-1"
      ; ioboxes = Array.empty
      }

    let mutable store : Store = mkStore reducer

    check ((List.length store.state.Patches) = 0) "patches list should be empty"

    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    check_cc ((List.length store.state.Patches) = 1) "patches list length should be 1" cb

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

    let mutable store : Store = mkStore reducer
    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    check (List.exists isPatch store.state.Patches) "patches list should contain patch"
    check (List.find isPatch store.state.Patches |> (fun p -> p.name = name1)) "patches list should contain patch"

    let updated = { patch with name = name2 }
    store <- dispatch store { Kind = UpdatePatch; Payload = PatchD(updated) }

    check_cc (List.find isPatch store.state.Patches |> (fun p -> p.name = name2)) "patches list should contain patch" cb

  (*--------------------------------------------------------------------------*)
  test "should remove a patch already in the store" <| fun cb ->
    let patch : Patch =
      { id = "0xb33f"
      ; name = "patch-1"
      ; ioboxes = Array.empty
      }

    let isPatch (p : Patch) : bool = p.id = patch.id

    let mutable store : Store = mkStore reducer
    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    check (List.exists isPatch store.state.Patches) "patches list should contain patch"

    store <- dispatch store { Kind = RemovePatch; Payload = PatchD(patch) }

    check_cc (not (List.exists isPatch store.state.Patches)) "patches list should not contain patch" cb

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

    let mutable store : Store = mkStore reducer
    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    match store.state.Patches with
      | patch :: [] -> check ((Array.length patch.ioboxes) = 0) "iobox array length should be 0"
      | _ -> check false "patches list is empty but should contain at least one patch"

    let iobox =
      { id     = "0xb33f"
      ; name   = "url input"
      ; patch  = patchid
      ; kind   = "string"
      ; slices = [| { idx = 0; value = "Hey" } |]
      }

    store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox) }

    match store.state.Patches with
      | patch :: [] -> check_cc ((Array.length patch.ioboxes) = 1) "iobox array length should be 1" cb
      | _ -> check false "patches list is empty but should contain at least one patch"

  (*--------------------------------------------------------------------------*)
  test "should not add an iobox to the store if patch does not exists" <| fun cb ->
    let patchid = "0xb4d1d34"

    let mutable store : Store = mkStore reducer

    let iobox =
      { id     = "0xb33f"
      ; name   = "url input"
      ; patch  = patchid
      ; kind   = "string"
      ; slices = [| { idx = 0; value = "Hey" } |]
      }

    store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox) }
    check_cc ((List.length store.state.Patches) = 0) "patches list length should be 0" cb

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

    let mutable store : Store = mkStore reducer
    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    match findIOBox store.state.Patches iobox.id with
      | Some(i) -> check_cc (i.name = name1) "name of iobox does not match (1)" cb
      | None -> check_cc false "iobox is mysteriously missing" cb

    let updated = { iobox with name = name2 }
    store <- dispatch store { Kind = UpdateIOBox; Payload = IOBoxD(updated) }

    match findIOBox store.state.Patches iobox.id with
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

    let mutable store : Store = mkStore reducer
    store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

    match findIOBox store.state.Patches boxid with
      | Some(_) -> check true  "iobox should be found by now"
      | None    -> check false "iobox is mysteriously missing"

    store <- dispatch store { Kind = RemoveIOBox; Payload = IOBoxD(iobox) }

    match findIOBox store.state.Patches boxid with
      | Some(_) -> check_cc false "iobox should be missing by now but isn't" cb
      | None    -> check_cc true "iobox was found but should be missing" cb

