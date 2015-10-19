[<ReflectedDefinition>]
module Test.Units.Plugins

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
  window.IrisPlugins = window.IrisPlugins || [];

  (function(plugins) {
    var h = virtualDom.h;

    // plugin constructor
    var myplugin = function() {

      // update view
      this.render = function (iobox) {
        return h('div', { id: iobox.id }, iobox.slices.map(function(slice) {
          return h('p', { className: 'slice' }, [ slice.value ]);
        }));
      };

      this.dispose = function() {};
    }

    plugins.push({
      name: "simple-string-plugin",
      type: "string",
      create: function() {
        return new myplugin(arguments);
      }
    });
  })(window.IrisPlugins);


  (function(plugins) {
    var h = virtualDom.h;

    // plugin constructor
    var myplugin = function() {

      // update view
      this.render = function (iobox) {
        var view = h('div', { id: iobox.id }, [
          h('p', { className: 'slice' }, [ iobox.slices[0].value ])
        ]);
        return view;
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

let document = Globals.document

let main () =
  simpleString () // register the plugin

  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Plugins - basic operation"
  (*--------------------------------------------------------------------------*)

  test "listing plugins should list exactly one" <| fun cb ->
    let plugins = listPlugins ()
    check_cc (Array.length plugins = 2) "should have two plugins but doesn't" cb

  (*--------------------------------------------------------------------------*)
  test "listing plugins by kind should show exactly one" <| fun cb ->
    let plugins = findPlugins "number"
    check_cc (Array.length plugins = 1) "should have one plugin but doesn't" cb

  (*--------------------------------------------------------------------------*)
  test "rendering a plugin should return expected dom element" <| fun cb ->
    let plugin = findPlugins "string" |> (fun plugs -> Array.get plugs 0)
    let inst = plugin.Create ()

    let elid = "0xb33f"

    let iobox =
      { id     = elid
      ; name   = "url input"
      ; patch  = "0xb4d1d34"
      ; kind   = "string"
      ; slices = [| { idx = 0; value = "oh hey" } |]
      }

    inst.render iobox
    |> createElement
    |> (fun elm ->
        check_cc (elm.id = elid) "element should have correct id" cb)

  (*--------------------------------------------------------------------------*)
  test "re-rendering a plugin should return updated dom element" <| fun cb ->
    let plugin = findPlugins "string" |> (fun plugs -> Array.get plugs 0)
    let inst = plugin.Create ()

    let value1 = "r4nd0m"
    let value2 = "pr1m0p"

    let iobox =
      { id     = "0xb33f"
      ; name   = "url input"
      ; patch  = "0xb4d1d34"
      ; kind   = "string"
      ; slices = [| { idx = 0; value = value1 } |]
      }

    inst.render iobox
    |> createElement
    |> (fun elm -> elm.getElementsByClassName "slice")
    |> (fun els ->
        check (els.length = 1.0) "should have one slice"
        check (els.[0].textContent = value1) "should have the correct inner value")

    let update =
      { iobox with slices = [| { idx = 0; value = value2 } |] }
    
    inst.render update
    |> createElement
    |> (fun elm -> elm.getElementsByClassName "slice")
    |> (fun els ->
        check (els.length = 1.0) "should have one slice"
        check (els.[0].textContent = value2) "should have the correct inner value")

    let final =
      { iobox with slices = [| { idx = 0; value = value1 }
                            ;  { idx = 0; value = value2 }
                            |] }
    
    inst.render final
    |> createElement
    |> (fun elm -> elm.getElementsByClassName "slice")
    |> (fun els -> check_cc (els.length = 2.0) "should have two slices" cb)


  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Plugins - instance data structure"
  (*--------------------------------------------------------------------------*)

  test "should add and find an instance for an iobox" <| fun cb ->
    let instances = new Plugins ()
    let iobox =
      { id     = "0xb33f"
      ; name   = "url input"
      ; patch  = "0xb4d1d34"
      ; kind   = "string"
      ; slices = [| { idx = 0; value = "hello" } |]
      }
    
    instances.add iobox

    let ids = instances.ids ()
    check (ids.length = 1.0) "should have one instance" 

    match instances.get iobox with
      | Some(_) -> cb ()
      | None -> fail "instance not found"
  
  (*--------------------------------------------------------------------------*)
  test "should remove an instance for an iobox" <| fun cb ->
    let instances = new Plugins ()
    let iobox =
      { id     = "0xb33f"
      ; name   = "url input"
      ; patch  = "0xb4d1d34"
      ; kind   = "string"
      ; slices = [| { idx = 0; value = "hello" } |]
      }
    
    instances.add iobox
    instances.ids ()
    |> fun ids -> check (ids.length = 1.0) "should have one instance" 

    instances.remove iobox
    instances.ids ()
    |> fun ids -> check_cc (ids.length = 0.0) "should have no instance" cb


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
