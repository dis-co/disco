[<ReflectedDefinition>]
module Test.Units.VirtualDom

#nowarn "1182"

open FunScript
open FunScript.TypeScript
open FunScript.Mocha

let main () =
  suite "Test.Units.VirtualDom" 
  test "it is ok!" (fun cb -> check true "never to be seen" cb)
  test "is it ok?" (fun cb -> check false "OMG it happened!" cb)
  

