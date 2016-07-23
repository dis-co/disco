namespace Test.Units

[<RequireQualifiedAccess>]
module PatchesView =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Views

  [<Emit("btoa((new Date().getTime() * Math.random()).toString())")>]
  let hash _ : string = failwith "onlyjs"

  [<Emit("console.log($0, $1)")>]
  let show str a = failwith "ONLY IN JS"

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.PatchesView - patch workflow"
    (* ------------------------------------------------------------------------ *)

    test "should render a patch added" <| fun finished ->
      resetPlugins()
      addString2Plug ()

      let patchid = hash () |> Guid

      let patch : Patch =
        { Id = patchid
        ; Name = "cooles patch ey"
        ; IOBoxes = [||]
        }

      let store : Store<State> = new Store<State>(Reducer, State.Empty)
      let view = new Patches.Root ()
      let controller = new ViewController<State,ClientContext> (view)
      let ctx = new ClientContext()

      equals None (getById (string patchid))
      controller.Render store.State ctx
      equals None (getById (string patchid))
      store.Dispatch <| PatchEvent(Create, patch)
      controller.Render store.State ctx
      equals true (getById (string patchid) |> Option.isSome)
      dispose controller
      finished()

    (* ------------------------------------------------------------------------ *)
    test "should render correct list on patch removal" <| fun finished ->
      resetPlugins()
      addString2Plug ()

      let pid1 = Guid "patch-2"
      let pid2 = Guid "patch-3"

      let patch1 : Patch =
        { Id = pid1
        ; Name = "patch-1"
        ; IOBoxes = [||]
        }

      let patch2 : Patch =
        { Id = pid2
        ; Name = "patch-2"
        ; IOBoxes = [||]
        }

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch1; patch2 |] })

      let view = new Patches.Root ()
      let controller = new ViewController<State,ClientContext> (view)
      let ctx = new ClientContext()
      controller.Render store.State ctx

      equals true (getById (string pid1) |> Option.isSome)
      equals true (getById (string pid2) |> Option.isSome)

      store.Dispatch <| PatchEvent(Delete, patch1)

      equals false (Patch.HasPatch store.State.Patches patch1)
      equals true  (Patch.HasPatch store.State.Patches patch2)

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

      let id1 = Guid "id1"
      let value = "hello"

      let slice : StringSliceD = { Index = 0UL; Value = value }
      let iobox = IOBox.String(id1,"url input", Guid "0xb4d1d34", Array.empty, [| slice |])

      let patch : Patch =
        { Id = Guid "0xb4d1d34"
        ; Name = "patch-1"
        ; IOBoxes = [||]
        }

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch |] })

      let view = new Patches.Root ()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)
      controller.Render store.State ctx

      equals None (getById (string id1))
      store.Dispatch <| IOBoxEvent(Create, iobox)
      controller.Render store.State ctx
      equals true (getById (string id1) |> Option.isSome)

      dispose controller
      finished ()

    (* -------------------------------------------------------------------------- *)
    test "should render correct iobox list on iobox removal" <| fun finished ->
      resetPlugins ()
      addString2Plug ()

      let id1 = Guid "iobox-3"
      let id2 = Guid "iobox-4"
      let value = "hello"

      let slice1 : StringSliceD = { Index = 0UL; Value = value }
      let iobox1 = IOBox.String(id1,"url input", Guid "0xb4d1d34", Array.empty, [| slice1 |])

      let slice2 : StringSliceD = { Index = 0UL; Value = value }
      let iobox2 = IOBox.String(id2,"url input", Guid "0xb4d1d34", Array.empty, [| slice2 |])

      let patch : Patch =
        { Id = Guid "0xb4d1d34"
        ; Name = "patch-1"
        ; IOBoxes = [||]
        }

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch |] })

      let view = new Patches.Root ()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)

      // add the first iobox
      store.Dispatch <| IOBoxEvent(Create,iobox1)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)

      // add the second iobox
      store.Dispatch <| IOBoxEvent(Create,iobox2)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)
      equals true (getById (string id2) |> Option.isSome)

      // remove the second iobox
      store.Dispatch <| IOBoxEvent(Delete,iobox2)
      controller.Render store.State ctx

      equals true (getById (string id1) |> Option.isSome)
      equals true (getById (string id2) |> Option.isNone)

      dispose controller
      finished ()

    (* -------------------------------------------------------------------------- *)
    test "should render updates on iobox to dom" <| fun finished ->
      resetPlugins ()
      addString2Plug ()

      let elid = Guid "0xd34db33f"
      let value1 = "death to the confederacy!"
      let value2 = "the confederacy is dead!"
      let value3 = "death to racism!"

      let patch : Patch =
        { Id = Guid "0xb4d1d34"
        ; Name = "cooles patch ey"
        ; IOBoxes = [||]
        }

      let slice : StringSliceD = { Index = 0UL; Value = value1 }
      let iobox = IOBox.String(elid, "url input", Guid "0xb4d1d34", Array.empty, [| slice |])

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch |] })

      // render initial state
      let view = new Patches.Root()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)

      store.Dispatch <| IOBoxEvent(Create, iobox)

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

      store.Dispatch <| IOBoxEvent(Update, updated1)

      match Patch.FindIOBox store.State.Patches elid with
        | Some(box) -> equals value2 box.Slices.[0].StringValue
        | None      -> failwith "IOBox was not found in store"

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

      store.Dispatch <| IOBoxEvent(Update, updated2)

      match Patch.FindIOBox store.State.Patches elid with
        | Some(box) -> equals value3 box.Slices.[0].StringValue
        | None      -> failwith "IOBox was not found in store"

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
