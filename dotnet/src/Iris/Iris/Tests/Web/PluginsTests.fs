namespace Test.Units

[<RequireQualifiedAccess>]
module Plugins =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Views
  open Iris.Web.Tests

  [<Emit @"
    window.IrisPlugins = [];
      (function(plugins) {
          var h = virtualDom.h;
      
          var stringplugin = function(cb) {
            this.render = function (iobox) {
                return h(""div"", { id: iobox.id }, iobox.slices.map(function(slice) {
                  return h(""input"", {
                    value: slice.value,
                    className: 'slice',
                    onchange: function(ev) {
                      // mutate slices array
                      iobox.slices[slice.idx] = { idx: slice.idx, value: $(ev.target).val() };
                      cb(iobox);
                    }
                  }, [ slice.value ]);
                }));
            };
        
            this.dispose = function() {};
          }
      
          plugins.push({
            name: ""simple-string-plugin"",
            type: ""string"",
            create: function(cb) {
              return new stringplugin(cb);
            }
          });

          var numberplugin = function(cb) {
            this.render = function (iobox) {
                var view = h(""div"", { id: iobox.id }, [
                  h(""p"", { className: ""slice"" }, [ iobox.slices[0].value ])
                ]);
                return view;
            };
            this.dispose = function() {};
          }
      
          plugins.push({
            name: ""simple-number-plugin"",
            type: ""value"",
            create: function(cb) {
              return new numberplugin(cb);
            }
          });
      })(window.IrisPlugins);">]
  let setupPlugins () = failwith "OHNLY JSSS"

  let main () =
    (****************************************************************************)
    suite "Test.Units.Plugins - basic operation"
    (****************************************************************************)

    test "listing plugins should list exactly two plugins" <| fun cb ->
      setupPlugins ()
      let plugins = listPlugins ()
      check_cc (Array.length plugins = 2) "should have two plugins but doesn't" cb

    (*--------------------------------------------------------------------------*)
    test "listing plugins by kind should show exactly one" <| fun cb ->
      setupPlugins ()
      let plugins = findPlugins PinType.Value
      check_cc (Array.length plugins = 1) "should have one plugin but doesn't" cb

    (*--------------------------------------------------------------------------*)
    test "rendering a plugin should return expected dom element" <| fun cb ->
      setupPlugins ()
      
      let plugin = findPlugins PinType.String |> (fun plugs -> Array.get plugs 0)
      let inst = plugin.create(fun _ -> ())

      let elid = "0xb33f"

      let iobox =
        { IOBox.StringBox(elid,"url input", "0xb4d1d34")
            with Slices = [| StringSlice(0,"oh hey") |] }

      inst.Render iobox
      |> createElement
      |> (fun elm ->
          check_cc (elm.id = elid) "element should have correct id" cb)

    (*--------------------------------------------------------------------------*)
    test "re-rendering a plugin should return updated dom element" <| fun cb ->
      setupPlugins () // register the plugin
      
      let plugin = findPlugins PinType.String |> (fun plugs -> Array.get plugs 0)
      let inst = plugin.create (fun _ -> ())

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let iobox =
        { IOBox.StringBox("0xb33f","url input", "0xb4d1d34")
            with Slices = [| StringSlice(0,value1) |] }

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          check (els.length = 1.0) "should have one slice"
          check (els.[0].textContent = value1) "should have the correct inner value")

      let update =
        { iobox with Slices = [| StringSlice(0, value2) |] }

      inst.Render update
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          check (els.length = 1.0) "should have one slice"
          check (els.[0].textContent = value2) "should have the correct inner value")

      let final =
        { iobox with Slices = [| StringSlice(0,value1)
                              ;  StringSlice(0,value2) |] }

      inst.Render final
      |> createElement
      |> childrenByClass "slice"
      |> (fun els -> check_cc (els.length = 2.0) "should have two slices" cb)


    (****************************************************************************)
    suite "Test.Units.Plugins - instance data structure"
    (****************************************************************************)

    test "should add and find an instance for an iobox" <| fun cb ->
      setupPlugins ()
      
      let instances = new Plugins ()
      let iobox =
        { IOBox.StringBox("0xb33f","url input", "0xb4d1d34")
            with Slices = [| StringSlice(0,"hello")  |] }

      instances.Add iobox (fun _ -> ())

      instances.Ids ()
      |> (fun ids -> check (ids.Length = 1) "should have one instance")

      match instances.Get iobox with
        | Some(_) -> cb ()
        | None -> bail "instance not found"

    (*--------------------------------------------------------------------------*)
    test "should remove an instance for an iobox" <| fun cb ->
      setupPlugins ()
      
      let instances = new Plugins ()
      let iobox =
        { IOBox.StringBox("0xb33f","url input", "0xb4d1d34")
            with Slices = [| StringSlice(0,"hello") |] }

      instances.Add iobox (fun _ -> ())
      instances.Ids ()
      |> fun ids -> check (ids.Length = 1) "should have one instance"

      instances.Remove iobox
      instances.Ids ()
      |> fun ids -> check_cc (ids.Length = 0) "should have no instance" cb


    (****************************************************************************)
    suite "Test.Units.Plugins - Event Listeners"
    (****************************************************************************)
    
    test "should fire an event listener when updated" <| fun cb ->
      setupPlugins ()
      
      let plugin =
        findPlugins PinType.String
        |> (fun plugs -> Array.get plugs 0)

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let iobox =
        { IOBox.StringBox("0xb33f","url input", "0xb4d1d34")
            with Slices = [| StringSlice(0,value1) |] }

      let listener (box' : IOBox) : unit =
        box'.Slices.[0].StringValue ==>> value2 <| cb

      let inst = plugin.create(listener)

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          els.length |==| 1.0
          els.[0].textContent |==| value1)
          // els.First().Val(value2).Trigger("change") |> ignore)
