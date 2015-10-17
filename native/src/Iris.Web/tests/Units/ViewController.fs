[<ReflectedDefinition>]
module Test.Units.ViewController

open FSharp.Html
open FunScript
open FunScript.Mocha
open FunScript.TypeScript

open Iris.Web.Dom
open Iris.Web.Test.Util
open Iris.Web.Core.Store
open Iris.Web.Core.View
open Iris.Web.Core.Patch
open Iris.Web.Core.Store
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

  let patchList (patches : Patch list) =
    div <||> List.map patchView patches

  let mainView content = div <@> id' "patches" <|> content

  interface IWidget with
    member self.render (store : Store) =
      let patches = store.state.Patches

      let content =
        if List.length patches = 0
        then p <|> text "Empty"
        else patchList patches

      mainView content |> htmlToVTree


let main () =
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.ViewController - basics"
  (*--------------------------------------------------------------------------*)

  withContent <| fun content ->
    test "should render successive updates of a patch view" <| fun cb ->
      let patch1 = { id = "0xb33f"
                   ; name = "patch-1"
                   ; ioboxes = Array.empty
                   }

      let patch2 = { id = "0xd001"
                   ; name = "patch-2"
                   ; ioboxes = Array.empty
                   }

      let patch3 = { id = "0x400f"
                   ; name = "patch-3"
                   ; ioboxes = Array.empty
                   }

      let mutable store = mkStore reducer

      let view = new PatchView()
      let ctrl = new ViewController(view)
      ctrl.Container <- content

      store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch1) }

      ctrl.render store

      content.getElementsByClassName "patch"
      |> (fun els ->
          check (els.length = 1.0) "should be one rendered patch template in dom")

      store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch2) }

      ctrl.render store

      content.getElementsByClassName "patch"
      |> (fun els ->
          check (els.length = 2.0) "should be two rendered patch templates in dom")

      store <- dispatch store { Kind = AddPatch; Payload = PatchD(patch3) }

      ctrl.render store

      content.getElementsByClassName "patch"
      |> (fun els ->
          check_cc (els.length = 3.0) "should be three rendered patch templates in dom" cb)

      cleanup content
