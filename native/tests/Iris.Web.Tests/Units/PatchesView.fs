namespace Test.Units

open System
open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.Mocha

[<JavaScript>]
[<RequireQualifiedAccess>]
module PatchesView =

  open Iris.Web.Test.Util

  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Store
  open Iris.Web.Core.Events
  open Iris.Web.Core.Reducer
  open Iris.Web.Core.ViewController
  open Iris.Web.Core.Plugin

  open Iris.Web.Views.PatchesView

  [<Direct
    "
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
            h('p', { className: 'name' }, [ iobox.name ]),
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
  let simpleString () = X<unit>

  let document = JS.Document

  let main () =
    simpleString ()

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.PatchesView - patch workflow"
    (*--------------------------------------------------------------------------*)

    test "should render a patch added" <| fun cb ->
      let pid = "1"

      let patch : Patch =
        { id = pid
        ; name = "cooles patch ey"
        ; ioboxes = Array.empty
        }

      let mutable store : Store = mkStore reducer

      let view = new PatchesView ()
      let controller = new ViewController (view)
      controller.render store

      document.getElementById pid
      |> (fun el -> check (isNull el) "element should be null")

      store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch) }

      controller.render store

      document.getElementById pid
      |> (fun el -> check_cc (el.id = pid) "patch element not found in dom" cb)

      (controller :> IDisposable).Dispose ()


    test "should render correct list on patch removal" <| fun cb ->
      let pid1 = "patch-2"
      let pid2 = "patch-3"

      let patch1 : Patch =
        { id = pid1
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 : Patch =
        { id = pid2
        ; name = "patch-2"
        ; ioboxes = Array.empty
        }

      let mutable store : Store =
        { state     = { Patches = [ patch1; patch2 ] }
        ; reducer   = reducer
        ; listeners = []}

      let view = new PatchesView ()
      let controller = new ViewController (view)
      controller.render store

      document.getElementById pid1
      |> (fun el -> check (not (isNull el)) "element 1 should not be null")

      document.getElementById pid2
      |> (fun el -> check (not (isNull el)) "element 2 should not be null")

      store <- dispatch store { Kind = RemovePatch; Payload = PatchD(patch1) }

      check (not <| hasPatch store.state.Patches patch1) "patch should be gone"
      check (hasPatch store.state.Patches patch2) "patch should be there"

      controller.render store

      document.getElementById pid1
      |> (fun el -> check (isNull el) "element 1 should be null")

      document.getElementById pid2
      |> (fun el -> check_cc (not (isNull el)) "element 2 should not be null" cb)

      (controller :> IDisposable).Dispose()

    (*--------------------------------------------------------------------------*)
    suite "Test.Units.PatchesView - iobox workflow"
    (*--------------------------------------------------------------------------*)

    test "should render an added iobox" <| fun cb ->
      let id1 = "3"
      let value = "hello"

      let iobox =
        { id     = id1
        ; name   = "url input"
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = value } |]
        }

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let mutable store : Store =
        { state     = { Patches = [ patch ] }
        ; reducer   = reducer
        ; listeners = []}

      let view = new PatchesView ()
      let controller = new ViewController (view)
      controller.render store

      document.getElementById id1
      |> (fun el -> check (isNull el) "element should be null")

      store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox) }

      controller.render store

      document.getElementById id1
      |> (fun el -> check_cc (not (isNull el)) "element should not be null" cb)

      (controller :> IDisposable).Dispose ()

    (*--------------------------------------------------------------------------*)
    test "should render correct iobox list on iobox removal" <| fun cb ->
      let id1 = "iobox-3"
      let id2 = "iobox-4"
      let value = "hello"

      let iobox1 =
        { id     = id1
        ; name   = "url input"
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = value } |]
        }

      let iobox2 =
        { id     = id2
        ; name   = "url input"
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = value } |]
        }

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let mutable store : Store =
        { state     = { Patches = [ patch ] }
        ; reducer   = reducer
        ; listeners = []}

      let view = new PatchesView ()
      let controller = new ViewController (view)

      // add the first iobox
      store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox1) }
      controller.render store

      document.getElementById id1
      |> (fun el -> check_cc (not (isNull el)) "element should not be null" cb)

      // add the second iobox
      store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox2) }
      controller.render store

      document.getElementById id1
      |> (fun el -> check (not (isNull el)) "element 1 should not be null")

      document.getElementById id2
      |> (fun el -> check (not (isNull el)) "element 2 should not be null")

      // remove the second iobox
      store <- dispatch store { Kind = RemoveIOBox; Payload = IOBoxD(iobox2) }
      controller.render store

      document.getElementById id1
      |> (fun el -> check (not (isNull el)) "element 1 should not be null")

      document.getElementById id2
      |> (fun el -> check_cc (isNull el) "element 2 should be null" cb)

      (controller :> IDisposable).Dispose ()


    (*--------------------------------------------------------------------------*)
    test "should render updates on iobox to dom" <| fun cb ->
      let elid = "0xd34db33f"
      let value1 = "death to the confederacy!"
      let value2 = "the confederacy is dead!"
      let value3 = "death to racism!"

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "cooles patch ey"
        ; ioboxes = Array.empty
        }

      let iobox : IOBox =
        { id     = elid
        ; name   = "url input"
        ; patch  = "0xb4d1d34"
        ; kind   = "string"
        ; slices = [| { idx = 0; value = value1 } |]
        }

      let mutable store : Store =
        { state     = { Patches = [ patch ] }
        ; reducer   = reducer
        ; listeners = []}

      // render initial state
      let view = new PatchesView ()
      let controller = new ViewController (view)

      store <- dispatch store { Kind = AddIOBox; Payload = IOBoxD(iobox) }

      controller.render store

      // test for the presence of the initial state
      document.getElementById elid
      |> (fun el -> el.getElementsByClassName "slice")
      |> (fun slices -> slices.item(0.0))
      |> (fun slice -> check (slice.textContent = value1) "iobox slice value not present in dom (test 1)")

      // update the iobox slice value
      let updated1 = {
        iobox with
          slices = [| { idx = 0; value = value2 }|]
        }

      store <- dispatch store { Kind = UpdateIOBox; Payload = IOBoxD(updated1) }

      match findIOBox store.state.Patches elid with
        | Some(box) -> check (box.slices.[0].value = value2) "box in updated state should have right value"
        | None -> fail "IOBox was not found in store"

      controller.render store

      // test for the presence of the initial state
      document.getElementById elid
      |> (fun el -> el.getElementsByClassName "slice")
      |> (fun slices -> slices.item(0.0))
      |> (fun slice ->
          check (slice.textContent = value2) "iobox slice value not present in dom (test 2)")

      // update the iobox slice value
      let updated2 = {
        iobox with
          slices = [| { idx = 0; value = value3 }|]
        }

      store <- dispatch store { Kind = UpdateIOBox; Payload = IOBoxD(updated2) }

      match findIOBox store.state.Patches elid with
        | Some(box) -> check (box.slices.[0].value = value3) "box in updated state should have right value"
        | None -> fail "IOBox was not found in store"

      controller.render store

      // test for the presence of the initial state
      document.getElementById elid
      |> (fun el     -> el.getElementsByClassName "slice")
      |> (fun slices -> slices.item(0.0))
      |> (fun slice  ->
          check_cc (slice.textContent = value3) "iobox slice value not present in dom (test 3)" cb)

      (controller :> IDisposable).Dispose ()
