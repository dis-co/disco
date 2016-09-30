namespace Test.Units

[<RequireQualifiedAccess>]
module Plugins =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Core.Html
  open Iris.Web.Views
  open Iris.Web.Tests

  let main () =
    (* ----------------------------------------------------------------------- *)
    suite "Test.Units.Plugins - basic operation"
    (* ----------------------------------------------------------------------- *)

    test "listing plugins should list exactly two plugins" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugins = listPlugins ()
      equals 2 (Array.length plugins)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "listing plugins by kind should show exactly one" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugins = findPlugins ValuePin
      equals 1 (Array.length plugins)
      finish()

    (* ------------------------------------------------------------------------ *)
    test "rendering a plugin should return expected dom element" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin = findPlugins StringPin |> (fun plugs -> plugs.[0])
      let inst = plugin.Create(fun _ -> ())

      let elid = Id "0xb33f"

      let slice : StringSliceD = { Index = 0u; Value = "oh hey" }
      let iobox = IOBox.String(elid,"url input", Id "0xb4d1d34", Array.empty, [| slice |])

      inst.Render iobox
      |> createElement
      |> (fun elm -> equals (string elid) elm.id)

      finish ()

    (* ------------------------------------------------------------------------ *)
    test "re-rendering a plugin should return updated dom element" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin = findPlugins StringPin |> (fun plugs -> plugs.[0])
      let inst = plugin.Create (fun _ -> ())

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let slice : StringSliceD = { Index = 0u; Value = value1 }
      let iobox = IOBox.String(Id "0xb33f","url input", Id "0xb4d1d34", Array.empty, [| slice |])

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          equals 1.0    els.length
          equals value1 els.[0].textContent)

      let update =
        StringSlices [| { Index = 0u; Value = value2 } |]
        |> iobox.SetSlices

      inst.Render update
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          equals 1.0    els.length
          equals value2 els.[0].textContent)

      let final =
        StringSlices [| { Index = 0u; Value = value1 }
                     ; { Index = 0u; Value = value2 } |]
        |> iobox.SetSlices

      inst.Render final
      |> createElement
      |> childrenByClass "slice"
      |> (fun els -> equals 2.0 els.length)

      finish()


    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.Plugins - instance data structure"
    (* ------------------------------------------------------------------------ *)

    test "should add and find an instance for an iobox" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let instances = new Plugins ()

      let slice : StringSliceD = { Index = 0u; Value = "hello" }
      let iobox = IOBox.String(Id "0xb33f","url input", Id "0xb4d1d34", Array.empty, [| slice |])

      instances.Add iobox (fun _ -> ())
      equals 1 <| instances.Ids().Length

      match instances.Get iobox with
        | Some(_) -> finish ()
        | None    -> failwith "instance not found"

    (* ------------------------------------------------------------------------ *)
    test "should remove an instance for an iobox" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let instances = new Plugins ()

      let slice : StringSliceD = { Index = 0u; Value = "hello" }
      let iobox = IOBox.String(Id "0xb33f","url input", Id "0xb4d1d34",Array.empty, [| slice |])

      instances.Add iobox (fun _ -> ())
      equals 1 <| instances.Ids().Length

      instances.Remove iobox
      equals 0 <| instances.Ids().Length

      finish()

    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.Plugins - Event Listeners"
    (* ------------------------------------------------------------------------ *)

    test "should fire an event listener when updated" <| fun finish ->
      resetPlugins ()
      addString1Plug ()
      addNumberPlug ()

      let plugin =
        findPlugins StringPin
        |> fun plugins -> plugins.[0]

      let value1 = "r4nd0m"
      let value2 = "pr1m0p"

      let slice : StringSliceD = { Index = 0u; Value = value1 }
      let iobox = IOBox.String(Id "0xb33f","url input", Id "0xb4d1d34", Array.empty, [| slice |])

      let listener (box' : IOBox) : unit =
        match box'.Slices.[0] with
          | StringSlice data ->
            equals value2 data.Value
            finish()
          | _ ->
            failwith "its not a string slice?"

      let inst = plugin.Create(listener)

      inst.Render iobox
      |> createElement
      |> childrenByClass "slice"
      |> (fun els ->
          equals 1.0 els.length
          let el = asHtmlInput els.[0]
          el.value <- value2
          equals value1 el.textContent
          change el
          |> ignore)
