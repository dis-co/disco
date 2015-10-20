[<ReflectedDefinition>]
module Test.Units.PatchesView

open FunScript
open FunScript.TypeScript
open FunScript.Mocha
open FunScript.VirtualDom

open Iris.Web.Test.Util

open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch
open Iris.Web.Core.Store
open Iris.Web.Core.Events
open Iris.Web.Core.Reducer
open Iris.Web.Core.View
open Iris.Web.Core.Plugin

open Iris.Web.Views.Patches

[<JSEmit(
  """
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
      name: "simple-number-plugin",
      type: "number",
      create: function() {
        return new myplugin(arguments);
      }
    });
  })(window.IrisPlugins);
  """
)>]
let simpleString () = failwith "never"


let main () =
  simpleString ()

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.PatchesView - patch workflow"
  (*--------------------------------------------------------------------------*)

  pending "should render a patch added"
  pending "should render correct list on patch removal"

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.PatchesView - iobox workflow"
  (*--------------------------------------------------------------------------*)

  pending "should render an added iobox"
  pending "should render correct iobox list on iobox removal"
  pending "should render correct iobox on iobox update"
  

  (*
  withContent <| fun content ->
    test "should render plugin for iobox" <| fun cb ->
      let elid = "0xd34db33f"
      let value = "death to the confederacy"

      let patch : Patch =
        { id = "0xb4d1d34"
        ; name = "cooles patch ey"
        ; ioboxes =
          [| { id     = elid
             ; name   = "url input"
             ; patch  = "0xb4d1d34"
             ; kind   = "string"
             ; slices = [| { idx = 0; value = value } |]
             } |]
        }

      let store : Store =
        { state     = { Patches = [ patch ] }
        ; reducer   = reducer
        ; listeners = []}

      let view = new PatchView ()
      let controller = new ViewController (view)
      controller.Container <- content
      controller.render store

      let el = document.getElementById elid

      check (el.id = elid) "element not found in dom"

      let slices = el.getElementsByClassName "slice"
      let slice = slices.item 0.0

      check_cc (slice.textContent = value) "iobox slice value not present in dom" cb
      cleanup content


  (*--------------------------------------------------------------------------*)
  withContent <| fun content ->
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
      let view = new PatchView ()
      let controller = new ViewController (view)
      controller.Container <- content

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

       cleanup content
   *)
