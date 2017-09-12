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
      let id1 = IrisId.Create()
      let id2 = IrisId.Parse (string id1)

      equals id1 id2

      finish ()

    test "Validate Id as Key in Map" <| fun finish ->
      let num = 10
      let map =
        [| for n in 1 .. 10 do
            yield (IrisId.Create(), n) |]
        |> Map.ofArray

      equals num (Map.fold (fun m _ _ -> m + 1) 0 map)

      // test querying by IrisId
      equals true <|
        Map.fold
          (fun m id value -> if m then map.[id] = value else m)
          true
          map

      finish ()

    test "Validate Id toString is valid json" <| fun finish ->
      let id = IrisId.Create()
      equals id (id.toString() |> ofJson<IrisId>)
      finish ()
