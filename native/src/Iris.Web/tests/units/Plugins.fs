[<ReflectedDefinition>]
module Test.Units.Plugins

open FunScript
open FunScript.TypeScript
open FunScript.Mocha

open Iris.Web.Test.Util

open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch
open Iris.Web.Core.Store
open Iris.Web.Core.Events
open Iris.Web.Core.Reducer
open Iris.Web.Core.View

open Iris.Web.Views.Patches

[<JSEmit("""
         window.IrisPlugins = window.IrisPlugins || [];

         (function(plugins) {
           var h = virtualDom.h;

           // plugin constructor
           var myplugin = function() {

             // update view
             this.render = function (iobox) {
               return h('div', { id: iobox.id }, [
                 h('p', { className: 'slice' }, [ iobox.slices[0].value ])
               ]);
             };

             this.dispose = function() {

             };
           }
         
           plugins.push({
             name: "simple-string-plugin",
             type: "string",
             create: function() {
               return new myplugin(arguments);
             }
           });
         })(window.IrisPlugins);
         """)>]
let simpleString () = failwith "never"

let document = Globals.document

let main () =
  simpleString () // register the plugin
  
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Plugins - basic operation"
  (*--------------------------------------------------------------------------*)

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

    controller.render store

    let el = document.getElementById elid

    check (el.id = elid) "element not found in dom"

    let slices = el.getElementsByClassName "slice"
    let slice = slices.item 0.0

    check_cc (slice.textContent = value) "iobox slice value not present in dom" cb
