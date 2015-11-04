namespace Test.Units

open WebSharper
open WebSharper.JavaScript
open WebSharper.JQuery
open WebSharper.Mocha

module Plugins =

  open Iris.Web.Tests.Util

  open Iris.Web.Core.Html
  open Iris.Web.Core.IOBox
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Store
  open Iris.Web.Core.Events
  open Iris.Web.Core.Reducer
  open Iris.Web.Core.ViewController
  open Iris.Web.Core.Plugin

  open Iris.Web.Views.PatchView

  [<Direct
    @"window.IrisPlugins = [];

(function(plugins) {
    var h = virtualDom.h;

    // plugin constructor
    var myplugin = function() {

    // update view
    this.render = function (iobox) {
        return h(""div"", { id: iobox.id }, iobox.slices.map(function(slice) {
        return h(""p"", { className: 'slice' }, [ slice.value ]);
        }));
    };

    this.dispose = function() {};
    }

    plugins.push({
    name: ""simple-string-plugin"",
    type: ""string"",
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
        var view = h(""div"", { id: iobox.id }, [
        h(""p"", { className: ""slice"" }, [ iobox.slices[0].value ])
        ]);
        return view;
    };

    this.dispose = function() {

    };
    }

    plugins.push({
    name: ""simple-number-plugin"",
    type: ""number"",
    create: function() {
        return new myplugin(arguments);
    }
    });
})(window.IrisPlugins);"
  >]
  let simpleString () = X<unit>

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
      let inst = plugin.create ()

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
      |> JQuery.Of
      |> (fun elm ->
          check_cc (elm.Attr("id") = elid) "element should have correct id" cb)

    (*--------------------------------------------------------------------------*)
    test "re-rendering a plugin should return updated dom element" <| fun cb ->
      let plugin = findPlugins "string" |> (fun plugs -> Array.get plugs 0)
      let inst = plugin.create ()

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
      |> JQuery.Of
      |> (fun el -> el.Children("slice"))
      |> (fun els ->
          check (els.Length = 1) "should have one slice"
          check (els.Get(0).TextContent = value1) "should have the correct inner value")

      let update =
        { iobox with slices = [| { idx = 0; value = value2 } |] }

      inst.render update
      |> createElement
      |> JQuery.Of
      |> (fun el -> el.Children("slice"))
      |> (fun els ->
          check (els.Length = 1) "should have one slice"
          check (els.Get(0).TextContent = value2) "should have the correct inner value")

      let final =
        { iobox with slices = [| { idx = 0; value = value1 }
                              ;  { idx = 0; value = value2 }
                              |] }

      inst.render final
      |> createElement
      |> JQuery.Of
      |> (fun elm -> elm.Children("slice"))
      |> (fun els -> check_cc (els.Length = 2) "should have two slices" cb)


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

      instances.ids ()
      |> (fun ids -> check (ids.Length = 1) "should have one instance")

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
      |> fun ids -> check (ids.Length = 1) "should have one instance"

      instances.remove iobox
      instances.ids ()
      |> fun ids -> check_cc (ids.Length = 0) "should have no instance" cb
