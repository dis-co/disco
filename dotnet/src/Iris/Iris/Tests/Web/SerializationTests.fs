namespace Test.Units

[<RequireQualifiedAccess>]
module SerializationTests =

  open Fable.Core
  open Fable.Import

  open Iris.Core
  open Iris.Web.Core
  open Iris.Web.Tests
  open Iris.Web.Views

  let main () =
    (* ------------------------------------------------------------------------ *)
    suite "Test.Units.SerializationTests"
    (* ------------------------------------------------------------------------ *)

    test "should serialize/deserialize cue correctly" <| fun finish ->
      let cue : Cue = { Id = Id.Create(); Name = "My funky Cue"; IOBoxes = [| |] }
      let recue : Cue = cue |> Binary.encode |> Binary.decode |> Option.get

      equals true (cue = recue)

      finish()
