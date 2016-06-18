namespace Test.Units

open System

[<RequireQualifiedAccess>]
module PatchesView =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Tests.Util
  open Iris.Web.Views

  [<Emit
    @"
    window.IrisPlugins = [];
    (function(plugins) {
      var h = virtualDom.h;

      var sliceView = function(slice) {
        return h('li', [
          h('input', {
            className: 'slice',
            type: 'text',
            name: 'slice',
            value: slice.value
          }, [ slice.value ])
         ]);
      };

      var slices = function (iobox) {
        return h('ul', {
          className: 'slices'
        }, iobox.slices.map(sliceView))
      };

      // plugin constructor
      var myplugin = function() {

        // update view
        this.render = function (iobox) {
          return h('div', {
            id: iobox.id
          }, [
            h(""p"", { className: 'name' }, [ iobox.name ]),
            slices(iobox)
          ]);
        };

        this.dispose = function() {
        };
      }

      plugins.push({
        name: ""simple-number-plugin"",
        type: ""string"",
        create: function() {
          return new myplugin(arguments);
        }
      });
    })(window.IrisPlugins);
    "
  >]
  let stringPlugin () = failwith "OH HAY JS"

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.PatchesView - patch workflow"
    (* ------------------------------------------------------------------------ *)

    test "should render a patch added" <| fun cb ->
      stringPlugin ()
      let patchid = "patch-1"

      let patch : Patch =
        { Id = patchid
        ; Name = "cooles patch ey"
        ; IOBoxes = [||]
        }

      let store : Store<State> = new Store<State>(Reducer, State.Empty)

      let view = new Patches.Root ()
      let controller = new ViewController<State,ClientContext> (view)
      let ctx = new ClientContext()
      controller.Render store.State ctx 

      check (getById patchid |> Option.isNone) "element should be null"

      store.Dispatch <| PatchEvent(Create, patch)

      controller.Render store.State ctx

      check_cc (getById patchid |> Option.isSome) "patch element not found in dom" cb

      (controller :> IDisposable).Dispose ()


    (* ------------------------------------------------------------------------ *)
    test "should render correct list on patch removal" <| fun cb ->
      stringPlugin ()
      let pid1 = "patch-2"
      let pid2 = "patch-3"

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

      check (getById pid1 |> Option.isSome) "element 1 should not be null"
      check (getById pid2 |> Option.isSome) "element 2 should not be null"

      store.Dispatch <| PatchEvent(Delete, patch1)

      check (not <| Patch.hasPatch store.State.Patches patch1) "patch should be gone"
      check (Patch.hasPatch store.State.Patches patch2) "patch should be there"

      controller.Render store.State ctx
      
      check (getById pid1 |> Option.isNone) "element 1 should be null"

      check_cc (getById pid2 |> Option.isSome) "element 2 should not be null" cb

      (controller :> IDisposable).Dispose()

    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.PatchesView - iobox workflow"
    (* -------------------------------------------------------------------------- *)

    test "should render an added iobox" <| fun cb ->
      stringPlugin ()
      let id1 = "id1"
      let value = "hello"

      let iobox = {
        IOBox.StringBox(id1,"url input", "0xb4d1d34") with
          Slices = [| StringSlice(0,value) |]
        }

      let patch : Patch =
        { Id = "0xb4d1d34"
        ; Name = "patch-1"
        ; IOBoxes = [||]
        }

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch |] })

      let view = new Patches.Root ()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)
      controller.Render store.State ctx

      check (getById id1 |> Option.isSome) "element should not be null"

      store.Dispatch <| IOBoxEvent(Create, iobox)

      controller.Render store.State ctx

      check_cc (getById id1 |> Option.isSome) "element should not be null" cb

      (controller :> IDisposable).Dispose ()

    (* -------------------------------------------------------------------------- *)
    test "should render correct iobox list on iobox removal" <| fun cb ->
      stringPlugin ()
      let id1 = "iobox-3"
      let id2 = "iobox-4"
      let value = "hello"

      let iobox1 =
        { IOBox.StringBox(id1,"url input", "0xb4d1d34")
            with Slices = [| StringSlice(0, value) |] }

      let iobox2 =
        { IOBox.StringBox(id2,"url input", "0xb4d1d34")
            with Slices = [| StringSlice(0, value) |] }

      let patch : Patch =
        { Id = "0xb4d1d34"
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

      check (getById id1 |> Option.isSome) "element should not be null"

      // add the second iobox
      store.Dispatch <| IOBoxEvent(Create,iobox2)
      controller.Render store.State ctx

      check (getById id1 |> Option.isSome) "element 1 should not be null"
      check (getById id2 |> Option.isSome) "element 2 should not be null"

      // remove the second iobox
      store.Dispatch <| IOBoxEvent(Delete,iobox2)
      controller.Render store.State ctx

      check (getById id1 |> Option.isSome) "element 1 should not be null"
      check_cc (getById id2 |> Option.isSome) "element 2 should not be null" cb

      (controller :> IDisposable).Dispose ()


    (* -------------------------------------------------------------------------- *)
    test "should render updates on iobox to dom" <| fun cb ->
      stringPlugin ()

      let elid = "0xd34db33f"
      let value1 = "death to the confederacy!"
      let value2 = "the confederacy is dead!"
      let value3 = "death to racism!"

      let patch : Patch =
        { Id = "0xb4d1d34"
        ; Name = "cooles patch ey"
        ; IOBoxes = [||]
        }

      let iobox : IOBox =
        { IOBox.StringBox(elid, "url input", "0xb4d1d34")
            with Slices = [|  StringSlice(0, value1) |] }

      let store : Store<State> =
        new Store<State>(Reducer, { State.Empty with Patches = [| patch |] })

      // render initial state
      let view = new Patches.Root()
      let ctx = new ClientContext()
      let controller = new ViewController<State,ClientContext> (view)

      store.Dispatch <| IOBoxEvent(Create, iobox)

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById elid
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice ->
          check (slice.textContent = value1) "iobox slice value not present in dom (test 1)")

      // update the iobox slice value
      let updated1 = {
        iobox with
          Slices = [| StringSlice(0,value2) |]
        }

      store.Dispatch <| IOBoxEvent(Update, updated1)

      match Patch.findIOBox store.State.Patches elid with
        | Some(box) -> check (box.Slices.[0].StringValue = value2) "box in updated state should have right value"
        | None -> bail "IOBox was not found in store"

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById elid
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice ->
          check (slice.textContent = value2) "iobox slice value not present in dom (test 2)")

      // update the iobox slice value
      let updated2 = {
        iobox with
          Slices = [| StringSlice(0, value3) |]
        }

      store.Dispatch <| IOBoxEvent(Update, updated2)

      match Patch.findIOBox store.State.Patches elid with
        | Some(box) -> check (box.Slices.[0].StringValue = value3) "box in updated state should have right value"
        | None -> bail "IOBox was not found in store"

      controller.Render store.State ctx

      // test for the presence of the initial state
      getById elid
      |> Option.get
      |> childrenByClass "slices"
      |> nthElement 0
      |> (fun slice  ->
          check_cc (slice.textContent = value3) "iobox slice value not present in dom (test 3)" cb)

      (controller :> IDisposable).Dispose ()