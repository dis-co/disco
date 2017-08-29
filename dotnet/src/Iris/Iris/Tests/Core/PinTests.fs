namespace Iris.Tests

open Expecto
open Iris.Core
open Iris.Service

[<AutoOpen>]
module PinTests =

  let mkStringPin (value: string array) =
    Pin.string (Id.Create()) (name "test") (Id.Create()) [| |] value

  let mkNumberPin (value: double array) =
    Pin.number (Id.Create()) (name "test") (Id.Create()) [| |] value

  let mkBooleanPin (value: bool array) =
    Pin.toggle (Id.Create()) (name "test") (Id.Create()) [| |] value

  let mkEnumPin (value: Property array) =
    Pin.enum (Id.Create()) (name "test") (Id.Create()) [| |] value value

  let mkColorPin (value: ColorSpace array) =
    Pin.color (Id.Create()) (name "test") (Id.Create()) [| |] value

  let test_simple_string_pin_to_spread =
    testCase "validate correct string pin ToSpread with non-whitespace" <| fun _ ->
      let pin = mkStringPin [| "single" |]
      let spread = pin.ToSpread()
      Expect.equal spread "single" "Should be unescaped value"

  let test_whitespace_string_pin_to_spread =
    testCase "validate correct string pin ToSpread with whitespace" <| fun _ ->
      let pin = mkStringPin [| "single spread with whitespace" |]
      let spread = pin.ToSpread()
      Expect.equal spread "|single spread with whitespace|" "Should be an escaped value"

  let test_whitespace_string_pin_with_pipes_to_spread =
    testCase "validate correct string with pipes pin ToSpread with whitespace" <| fun _ ->
      let pin = mkStringPin [| "single spread with | and another |" |]
      let spread = pin.ToSpread()
      Expect.equal spread "|single spread with || and another |||" "Should be an escaped value"

  let test_mixed_string_pin_to_spread =
    testCase "validate correct string pin ToSpread with whitespace" <| fun _ ->
      let pin = mkStringPin [| "single spread with whitespace"; "none"; "oh and again" |]
      let spread = pin.ToSpread()
      Expect.equal spread "|single spread with whitespace|,none,|oh and again|" "Should be an escaped value"

  let test_simple_number_pin_to_spread =
    testCase "validate correct number pin ToSpread" <| fun _ ->
      let pin = mkNumberPin [| 3.14 |]
      let spread = pin.ToSpread()
      Expect.equal spread "3.14" "Should be the same value"

  let test_multiple_number_pin_to_spread =
    testCase "validate correct number pin ToSpread" <| fun _ ->
      let pin = mkNumberPin [| 3.14; 0.18 |]
      let spread = pin.ToSpread()
      Expect.equal spread "3.14,0.18" "Should be the same value"

  let test_simple_boolean_pin_to_spread =
    testCase "validate correct boolean pin ToSpread" <| fun _ ->
      let pin = mkBooleanPin [| true |]
      let spread = pin.ToSpread()
      Expect.equal spread "1" "Should be the same value"

  let test_multiple_boolean_pin_to_spread =
    testCase "validate correct boolean pin ToSpread" <| fun _ ->
      let pin = mkBooleanPin [| true; false; true |]
      let spread = pin.ToSpread()
      Expect.equal spread "1,0,1" "Should be the same value"

  let test_simple_enum_pin_to_spread =
    testCase "validate correct enum pin ToSpread with non-whitespace" <| fun _ ->
      let pin = mkEnumPin [| { Key = "0"; Value = "single" } |]
      let spread = pin.ToSpread()
      Expect.equal spread "single" "Should be unescaped value"

  let test_whitespace_enum_pin_to_spread =
    testCase "validate correct enum pin ToSpread with whitespace" <| fun _ ->
      let pin = mkEnumPin [| { Key = "0"; Value = "single spread with whitespace" }|]
      let spread = pin.ToSpread()
      Expect.equal spread "|single spread with whitespace|" "Should be an escaped value"

  let test_mixed_enum_pin_to_spread =
    testCase "validate correct enum pin ToSpread with whitespace" <| fun _ ->
      let pin = mkEnumPin [| { Key = "0"; Value = "single spread with whitespace" }
                           ; { Key = "1"; Value = "none" } |]
      let spread = pin.ToSpread()
      Expect.equal spread "|single spread with whitespace|,none" "Should be an escaped value"

  let test_simple_color_pin_to_spread =
    testCase "validate correct color pin ToSpread" <| fun _ ->
      let pin = mkColorPin [| RGBA { Red = 255uy; Green = 0uy; Blue = 124uy; Alpha = 0uy } |]
      let expected = "|1,0,0.486274509803922,0|"
      let spread = pin.ToSpread()
      Expect.equal spread expected "Should be the same value"

  let test_multiple_color_pin_to_spread =
    testCase "validate correct color pin ToSpread" <| fun _ ->
      let pin = mkColorPin [| RGBA { Red = 255uy; Green = 0uy; Blue = 124uy; Alpha = 0uy }
                            ; RGBA { Red = 24uy; Green = 243uy; Blue = 2uy; Alpha = 23uy } |]
      let expected = "|1,0,0.486274509803922,0|,|0.0941176470588235,0.952941176470588,0.00784313725490196,0.0901960784313725|"
      let spread = pin.ToSpread()
      Expect.equal spread expected "Should be the same value"

  //     _    _ _   _____         _
  //    / \  | | | |_   _|__  ___| |_ ___
  //   / _ \ | | |   | |/ _ \/ __| __/ __|
  //  / ___ \| | |   | |  __/\__ \ |_\__ \
  // /_/   \_\_|_|   |_|\___||___/\__|___/ grouped.

  let pinTests =
    testList "Pin Tests" [
      test_simple_string_pin_to_spread
      test_whitespace_string_pin_to_spread
      test_whitespace_string_pin_with_pipes_to_spread
      test_mixed_string_pin_to_spread
      test_simple_number_pin_to_spread
      test_multiple_number_pin_to_spread
      test_simple_boolean_pin_to_spread
      test_multiple_boolean_pin_to_spread
      test_simple_enum_pin_to_spread
      test_whitespace_enum_pin_to_spread
      test_mixed_enum_pin_to_spread
      test_simple_color_pin_to_spread
      test_multiple_color_pin_to_spread
    ]
