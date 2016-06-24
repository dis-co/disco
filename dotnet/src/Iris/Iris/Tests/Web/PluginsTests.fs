namespace Test.Units

[<RequireQualifiedAccess>]
module Plugins =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Views
  open Iris.Web.Tests

  let main () =
    (* ----------------------------------------------------------------------- *)
    suite "Test.Units.Plugins - basic operation"
    (* ----------------------------------------------------------------------- *)

    test "listing plugins should list exactly two plugins" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugins = listPlugins ()
      check_cc (Array.length plugins = 2) "should have two plugins but doesn't" cb

    (* ------------------------------------------------------------------------ *)
    test "listing plugins by kind should show exactly one" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugins = findPlugins ValuePin
      check_cc (Array.length plugins = 1) "should have one plugin but doesn't" cb

    (* ------------------------------------------------------------------------ *)
    test "rendering a plugin should return expected dom element" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin = findPlugins StringPin |> (fun plugs -> plugs.[0])
      let inst = plugin.Create(fun _ -> ())

      let elid = "0xb33f"

      let slice : StringSliceD = { Index = 0u; Value = "oh hey" }
      let iobox = IOBox.String(elid,"url input", "0xb4d1d34", Array.empty, [| slice |])

      inst.Render iobox
      |> createElement
      |> (fun elm ->
          check_cc (elm.id = elid) "element should have correct id" cb)

    (* ------------------------------------------------------------------------ *)
    test "re-rendering a plugin should return updated dom element" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin = findPlugins StringPin |> (fun plugs -> plugs.[0])
      let inst = plugin.Create (fun _ -> ())

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let slice : StringSliceD = { Index = 0u; Value = value1 }
      let iobox = IOBox.String("0xb33f","url input", "0xb4d1d34", Array.empty, [| slice |])

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          check (els.length = 1.0) "should have one slice"
          check (els.[0].textContent = value1) "should have the correct inner value")

      let update =
        StringSlices [| { Index = 0u; Value = value2 } |]
        |> iobox.SetSlices

      inst.Render update
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          check (els.length = 1.0) "should have one slice"
          check (els.[0].textContent = value2) "should have the correct inner value")

      let final =
        StringSlices [| { Index = 0u; Value = value1 }
                     ;  { Index = 0u; Value = value2 } |]
        |> iobox.SetSlices

      inst.Render final
      |> createElement
      |> childrenByClass "slice"
      |> (fun els -> check_cc (els.length = 2.0) "should have two slices" cb)


    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.Plugins - instance data structure"
    (* ------------------------------------------------------------------------ *)

    test "should add and find an instance for an iobox" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let instances = new Plugins ()

      let slice : StringSliceD = { Index = 0u; Value = "hello" }
      let iobox = IOBox.String("0xb33f","url input", "0xb4d1d34", Array.empty, [| slice |])

      instances.Add iobox (fun _ -> ())

      instances.Ids ()
      |> (fun ids -> check (ids.Length = 1) "should have one instance")

      match instances.Get iobox with
        | Some(_) -> cb ()
        | None -> bail "instance not found"

    (* ------------------------------------------------------------------------ *)
    test "should remove an instance for an iobox" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let instances = new Plugins ()

      let slice : StringSliceD = { Index = 0u; Value = "hello" }
      let iobox = IOBox.String("0xb33f","url input", "0xb4d1d34",Array.empty, [| slice |])

      instances.Add iobox (fun _ -> ())
      instances.Ids ()
      |> fun ids -> check (ids.Length = 1) "should have one instance"

      instances.Remove iobox
      instances.Ids ()
      |> fun ids -> check_cc (ids.Length = 0) "should have no instance" cb


    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.Plugins - Event Listeners"
    (* ------------------------------------------------------------------------ *)

    test "should fire an event listener when updated" <| fun cb ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin =
        findPlugins StringPin
        |> fun plugins -> plugins.[0]

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let slice : StringSliceD = { Index = 0u; Value = value1 }
      let iobox = IOBox.String("0xb33f","url input", "0xb4d1d34", Array.empty, [| slice |])

      let listener (box' : IOBox) : unit =
        match box'.Slices.[0] with
          | StringSlice data -> data.Value ==>> value2 <| cb
          |                _ -> bail "its not a string slice?"

      let inst = plugin.Create(listener)

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          els.length |==| 1.0
          let el = asHtmlInput els.[0]
          el.value <- value2
          el.textContent |==| value1
          change el
          |> ignore)
