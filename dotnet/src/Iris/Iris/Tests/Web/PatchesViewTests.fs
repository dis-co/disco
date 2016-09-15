namespace Test.Units

[<RequireQualifiedAccess>]
module PatchesView =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Views

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.PatchesView - patch workflow"
    (* ------------------------------------------------------------------------ *)

    test "should render a patch added" <| fun finished ->
      resetPlugins()
      addString2Plug ()

      let patchid = Id.Create()

      let patch : Patch =
        { Id = patchid
        ; Name = "cooles patch ey"
        ; IOBoxes = Map.empty
        }

      let store : Store = new Store(State.Empty)
      let view = new Patches.Root ()
      let controller = new ViewController<State,ClientContext> (view)
      let ctx = new ClientContext()

      equals None (getById (string patchid))
      controller.Render store.State ctx
      equals None (getById (string patchid))
      store.Dispatch <| AddPatch(patch)
      controller.Render store.State ctx
      equals true (getById (string patchid) |> Option.isSome)
      dispose controller
      finished()

    (* ------------------------------------------------------------------------ *)
    test "should render correct list on patch removal" <| fun finished ->
      resetPlugins()
      addString2Plug ()

      let pid1 = Id "patch-2"
      let pid2 = Id "patch-3"

      let patch1 : Patch =
        { Id = pid1
        ; Name = "patch-1"
        ; IOBoxes = Map.empty
        }

      let patch2 : Patch =
        { Id = pid2
        ; Name = "patch-2"
        ; IOBoxes = Map.empty
        }

      let patches =
        Map.empty
        |> Map.add patch1.Id patch1
        |> Map.add patch2.Id patch2

      let store : Store =
        new Store({ State.Empty with Patches = patches })

      let view = new Patches.Root ()
      let controller = new ViewController<State,ClientContext> (view)
      let ctx = new ClientContext()
      controller.Render store.State ctx

      equals true (getById (string pid1) |> Option.isSome)
      equals true (getById (string pid2) |> Option.isSome)

      store.Dispatch <| RemovePatch(patch1)

      equals false (store.State.Patches.ContainsKey patch1.Id)
      equals true  (store.State.Patches.ContainsKey patch2.Id)

      controller.Render store.State ctx

      equals None (getById (string pid1))
      equals true (getById (string pid2) |> Option.isSome)

      dispose controller
      finished ()

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.PatchesView - iobox workflow"
    (* -------------------------------------------------------------------------- *)

    test "should render an added iobox" <| fun finished ->
      resetPlugins ()
      addString2Plug ()

      let id1 = Id "id1"
      let value = "hello"

      let slice : StringSliceD = { Index = 0UL; Value = value }
      let iobox = IOBox.String(id1,"url input", Id "0xb4d1d34", Array.empty, [| slice |])

      let patch : Patch =
        { Id = Id "0xb4d1d34"
        ; Name = "patch-1"
        ; IOBoxes = Map.empty
        }

      let patches =
        Map.empty
        |> Map.add patch.Id patch

      let store : Store =
        new Store({ State.Empty with Patches = patches })

      let view = new Patches.Root ()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)
      controller.Render store.State ctx

      equals None (getById (string id1))
      store.Dispatch <| AddIOBox(iobox)
      controller.Render store.State ctx
      equals true (getById (string id1) |> Option.isSome)

      dispose controller
      finished ()

    (* -------------------------------------------------------------------------- *)
    test "should render correct iobox list on iobox removal" <| fun finished ->
      resetPlugins ()
      addString2Plug ()

      let id1 = Id "iobox-3"
      let id2 = Id "iobox-4"
      let value = "hello"

      let slice1 : StringSliceD = { Index = 0UL; Value = value }
      let iobox1 = IOBox.String(id1,"url input", Id "0xb4d1d34", Array.empty, [| slice1 |])

      let slice2 : StringSliceD = { Index = 0UL; Value = value }
      let iobox2 = IOBox.String(id2,"url input", Id "0xb4d1d34", Array.empty, [| slice2 |])

      let patch : Patch =
        { Id = Id "0xb4d1d34"
        ; Name = "patch-1"
        ; IOBoxes = Map.empty
        }

      let patches =
        Map.empty
        |> Map.add patch.Id patch

      let store : Store =
        new Store({ State.Empty with Patches = patches })

      let view = new Patches.Root ()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)

      // add the first iobox
      store.Dispatch <| AddIOBox(iobox1)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)

      // add the second iobox
      store.Dispatch <| AddIOBox(iobox2)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)
      equals true (getById (string id2) |> Option.isSome)

      // remove the second iobox
      store.Dispatch <| RemoveIOBox(iobox2)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)
      equals true (getById (string id2) |> Option.isNone)

      dispose controller
      finished ()

    (* -------------------------------------------------------------------------- *)
    test "should render updates on iobox to dom" <| fun finished ->
      resetPlugins ()
      addString2Plug ()

      let elid = Id "0xd34db33f"
      let value1 = "death to the confederacy!"
      let value2 = "the confederacy is dead!"
      let value3 = "death to racism!"

      let patch : Patch =
        { Id = Id "0xb4d1d34"
        ; Name = "cooles patch ey"
        ; IOBoxes = Map.empty
        }

      let patches =
        Map.empty
        |> Map.add patch.Id patch

      let slice : StringSliceD = { Index = 0UL; Value = value1 }
      let iobox = IOBox.String(elid, "url input", Id "0xb4d1d34", Array.empty, [| slice |])

      let store : Store =
        new Store({ State.Empty with Patches = patches })

      // render initial state
      let view = new Patches.Root()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)

      store.Dispatch <| AddIOBox(iobox)

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById (string elid)
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice -> equals value1 slice.textContent)

      // update the iobox slice value
      let updated1 =
        StringSlices [| { Index = 0UL; Value = value2 } |]
        |> iobox.SetSlices

      store.Dispatch <| UpdateIOBox(updated1)

      match Patch.FindIOBox store.State.Patches elid with
        | Some(box) ->
          match box.Slices.[0].StringValue with
          | Some value -> equals value2 value
          | _          -> failwith "IOBox should have correct value"
        | None         -> failwith "IOBox was not found in store"

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById (string elid)
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice -> equals value2 slice.textContent)

      // update the iobox slice value
      let updated2 =
        StringSlices [| { Index = 0UL; Value = value3 } |]
        |> iobox.SetSlices

      store.Dispatch <| UpdateIOBox(updated2)

      match Patch.FindIOBox store.State.Patches elid with
        | Some(box) ->
          match box.Slices.[0].StringValue with
          | Some value -> equals value3 value
          | _          -> failwith "IOBox has no value"
        | None         -> failwith "IOBox was not found in store"

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById (string elid)
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice  ->
          equals value3 slice.textContent
          dispose controller
          finished ())
