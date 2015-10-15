[<ReflectedDefinition>]
module Test.Units.Plugins

open FunScript
open FunScript.TypeScript
open FunScript.Mocha

open Iris.Web.Test.Util
open Iris.Web.Types.IOBox
open Iris.Web.Types.Patch

[<JSEmit("""
         window.IrisPlugins = window.IrisPlugins || [];

         (function(plugins) {
           var h = virtualDom.h;

           // plugin constructor
           var myplugin = function() {

             // update view
             this.render = function (iobox) {
               return h('div', { id: iobox.id }, [
                 h('h1', ["hello"]);
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

let main () =
  simpleString () // register the plugin
  
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Plugins"
  (*--------------------------------------------------------------------------*)

  // withContent <| fun content ->
  //   test "plugin should render iobox changes" <| fun cb ->
  //     let patch : Patch =
  //       { id = "0xb4d1d34"
  //       ; name = "cooles patch ey"
  //       ; ioboxes =
  //         [| { id     = "0xd34db33f"
  //            ; name   = "url input"
  //            ; patch  = "0xb4d1d34"
  //            ; kind   = "string"
  //            ; slices = [| { idx = 0; value = "death to the confederacy" } |]
  //            } |]
  //       }
  //       
  //     failwith "oops"
