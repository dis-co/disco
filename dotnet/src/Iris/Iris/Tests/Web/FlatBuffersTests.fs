namespace Test.Units

[<RequireQualifiedAccess>]
module FlatBuffersTests =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Views

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.FlatBuffersTests"
    (* ------------------------------------------------------------------------ *)

    test "should serialize/deserialize cue correctly" <| fun finish ->
      let cue : Cue = { Id = Id.Create(); Name = "My funky Cue"; IOBoxes = [| |] }
      let recue : Cue = cue |> Binary.encode |> Binary.decode |> Option.get

      printfn "original: %A " cue
      printfn "new: %A " recue

      equals true (cue = recue)

      finish()
