namespace Test.Units

[<RequireQualifiedAccess>]
module TypeTests =

  open Fable.Core.JsInterop
  open Iris.Core
  open Iris.Web.Tests

  let main () =
    (* -------------------------------------------------------------------------- *)
    suite "Test.Units.TypeTests -- Id"
    (* -------------------------------------------------------------------------- *)

    test "Validate Id Equality" <| fun finish ->
      let id1 = Id "yeah"
      let id2 = Id "yeah"

      equals id1 id2

      finish ()

    test "Validate Id as Key in Map" <| fun finish ->
      let num = 10
      let map =
        [| for n in 1 .. 10 do
            yield (Id.Create(), n) |]
        |> Map.ofArray

      equals num (Map.fold (fun m _ _ -> m + 1) 0 map)

      let id1 = Id.Create()
      finish ()

    test "Validate Id toString is valid json" <| fun finish ->
      let id = Id.Create()
      equals id (id.toString() |> ofJson<Id>)
      finish ()
