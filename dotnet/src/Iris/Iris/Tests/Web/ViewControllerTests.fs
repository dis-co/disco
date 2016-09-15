namespace Test.Units

open System

[<RequireQualifiedAccess>]
module ViewController =

  open Fable.Core
  open Fable.Import
  open Iris.Web.Tests
  open Iris.Web.Core
  open Iris.Web.Core.Html
  open Iris.Core

  (* ____       _       _  __     ___
    |  _ \ __ _| |_ ___| |_\ \   / (_) _____      __
    | |_) / _` | __/ __| '_ \ \ / /| |/ _ \ \ /\ / /
    |  __/ (_| | || (__| | | \ V / | |  __/\ V  V /
    |_|   \__,_|\__\___|_| |_|\_/  |_|\___| \_/\_/ replacement
  *)

  type PatchView () =
    let patchView (patch : Patch) =
      H1 [ Class "patch" ] [| Text patch.Name |]

    let patchList (patches : Patch array) =
      Div [] <| Array.map patchView patches

    let mainView content =
      Div [ ElmId "patches" ] [| content |]

    interface IWidget<State,ClientContext> with
      member self.Render (state : State) (context : ClientContext) =
        let content =
          if state.Patches.Count = 0 then
            P [] [| Text "Empty" |]
          else
            state.Patches
            |> Seq.toArray
            |> Array.map (fun kv -> kv.Value)
            |> patchList

        mainView content |> renderHtml

      member self.Dispose () = ()


  let main () =
    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.ViewController - basics"
    (* -------------------------------------------------------------------------- *)

    test "should render successive updates of a patch view" <| fun finished ->
      let patch1 : Patch =
        { Id = Id "0xb33f"
        ; Name = "patch-1"
        ; IOBoxes = Map.empty
        }

      let patch2 : Patch =
        { Id = Id "0xd001"
        ; Name = "patch-2"
        ; IOBoxes = Map.empty
        }

      let patch3 : Patch =
        { Id = Id "0x400f"
        ; Name = "patch-3"
        ; IOBoxes = Map.empty
        }

      let store = new Store(State.Empty)

      let view = new PatchView()
      let ctx = new ClientContext()
      let ctrl = new ViewController<State,ClientContext>(view)

      store.Dispatch <| AddPatch(patch1)

      ctrl.Render store.State ctx

      equals 1.0 (getByClass "patch" |> fun els -> els.length)

      store.Dispatch <| AddPatch(patch2)

      ctrl.Render store.State ctx

      equals 2.0 (getByClass "patch" |> fun els -> els.length)

      store.Dispatch <| AddPatch(patch3)

      ctrl.Render store.State ctx

      equals 3.0 (getByClass "patch"|> fun els -> els.length)

      dispose ctrl
      finished()

    (* ------------------------------------------------------------------------ *)
    test "should take care of removing its root element on Dispose" <| fun finished ->
      let patch1 : Patch =
        { Id = Id "0xb33f"
        ; Name = "patch-1"
        ; IOBoxes = Map.empty
        }

      let mutable store = new Store(State.Empty)

      let view = new PatchView()
      let ctx = new ClientContext()
      let ctrl = new ViewController<State,ClientContext>(view)

      store.Dispatch <| AddPatch(patch1)

      ctrl.Render store.State ctx

      equals 1.0 (getByClass "patch" |> fun els -> els.length)
      dispose ctrl
      equals 0.0 (getByClass "patch" |> fun els -> els.length)
      finished ()
