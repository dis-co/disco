namespace Test.Units

open System
open WebSharper
open WebSharper.Mocha
open WebSharper.JavaScript
open WebSharper.JQuery

[<JavaScript>]
[<RequireQualifiedAccess>]
module ViewController =

  open Iris.Web.Core.Html
  open Iris.Web.Tests.Util
  open Iris.Web.Core.Store
  open Iris.Web.Core.ViewController
  open Iris.Web.Core.Patch
  open Iris.Web.Core.Store
  open Iris.Web.Core.State
  open Iris.Web.Core.Reducer
  open Iris.Web.Core.Events

  (* ____       _       _  __     ___
    |  _ \ __ _| |_ ___| |_\ \   / (_) _____      __
    | |_) / _` | __/ __| '_ \ \ / /| |/ _ \ \ /\ / /
    |  __/ (_| | || (__| | | \ V / | |  __/\ V  V /
    |_|   \__,_|\__\___|_| |_|\_/  |_|\___| \_/\_/ replacement
  *)

  type PatchView () =
    let patchView (patch : Patch) =
      h1 <@> class' "patch" <|> text patch.name

    let patchList (patches : Patch array) =
      div <||> Array.map patchView patches

    let mainView content = div <@> id' "patches" <|> content

    interface IWidget<State> with
      member self.Render (store : Store<State>) =
        let patches = store.State.Patches |> List.toArray

        let content =
          if Array.length patches = 0
          then p <|> text "Empty"
          else patchList patches

        mainView content |> renderHtml

      member self.Dispose () = ()


  let main () =
    (*--------------------------------------------------------------------------*)
    suite "Test.Units.ViewController - basics"
    (*--------------------------------------------------------------------------*)

    test "should render successive updates of a patch view" <| fun cb ->
      let patch1 =
        { id = "0xb33f"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let patch2 =
        { id = "0xd001"
        ; name = "patch-2"
        ; ioboxes = Array.empty
        }

      let patch3 =
        { id = "0x400f"
        ; name = "patch-3"
        ; ioboxes = Array.empty
        }

      let store = new Store<State>(reducer, State.Empty)

      let view = new PatchView()
      let ctrl = new ViewController<State>(view)

      store.Dispatch <| PatchEvent(AddPatch, patch1)

      ctrl.Render store

      JQuery.Of(".patch")
      |> (fun els -> check (els.Length = 1) "should be one rendered patch template in dom")

      store.Dispatch <| PatchEvent(AddPatch, patch2)

      ctrl.Render store

      JQuery.Of(".patch")
      |> (fun els -> check (els.Length = 2) "should be two rendered patch templates in dom")

      store.Dispatch <| PatchEvent(AddPatch, patch3)

      ctrl.Render store

      JQuery.Of(".patch")
      |> (fun els -> check_cc (els.Length = 3) "should be three rendered patch templates in dom" cb)

      (ctrl :> IDisposable).Dispose ()

    (*------------------------------------------------------------------------*)
    test "should take care of removing its root element on Dispose" <| fun cb ->
      let patch1 =
        { id = "0xb33f"
        ; name = "patch-1"
        ; ioboxes = Array.empty
        }

      let mutable store = new Store<State>(reducer, State.Empty)

      let view = new PatchView()
      let ctrl = new ViewController<State>(view)

      store.Dispatch <| PatchEvent(AddPatch, patch1)

      ctrl.Render store

      JQuery.Of(".patch")
      |> (fun els -> check (els.Length = 1) "should be one patch in dom")

      (ctrl :> IDisposable).Dispose ()

      JQuery.Of(".patch")
      |> (fun els -> check_cc (els.Length = 0) "should be no patch in dom" cb)

      
