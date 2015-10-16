[<ReflectedDefinition>]
module Test.Units.ViewController

open FunScript
open FunScript.Mocha
open FunScript.TypeScript

open Iris.Web.Test.Util
open Iris.Web.Core.Store
open Iris.Web.Core.View


type PatchView () =

  interface IWidget with
    member self.render (store : Store) = failwith "never"
      // let patches = store.state.Patches

      // let content =
      //   if List.length patches = 0
      //   then p <|> text "Empty" |> Pure
      //   else patchList patches

      // mainView content |> compToVTree
      

let main () =
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.ViewController - basics"
  (*--------------------------------------------------------------------------*)

  withContent <| fun content ->
    test "should render a patch view" <| fun cb ->

      check_cc false "this was always meant to fail dear" cb
  
