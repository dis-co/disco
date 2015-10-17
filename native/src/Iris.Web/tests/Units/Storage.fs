[<ReflectedDefinition>]
module Test.Units.Storage

open FunScript
open FunScript.Mocha
open FunScript.TypeScript


let tests = async {
  (*--------------------------------------------------------------------------*)
  suite "Test.Units.Storage"
  (*--------------------------------------------------------------------------*)
  do! withTest "what a nice test"
       (fun () -> ())
  do! withTest "and another nice tests"
       (fun () -> ())
  }


let main () =
  Async.StartImmediate(tests)
