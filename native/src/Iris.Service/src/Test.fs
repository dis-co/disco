namespace Iris.Service.Types

open Iris.Core.Types

[<AutoOpen>]
module TestData =

  let pid = "patch-1"

  let iob1 =
    { IOBox.ValueBox("value-box-id", "Value Box Example", pid)
        with Slices = [| { Idx = 0; Value = 666 } |] }

  let iob2 =
    { IOBox.StringBox("string-box-id", "String Box Example", pid)
        with Slices = [| { Idx = 0; Value = "my example string value" } |] }

  let iob3 =
    { IOBox.ColorBox("color-box-id", "Color Box Example", pid)
        with Slices = [| { Idx = 0; Value = "#0f23ea" } |] }

  let iob4 =
    { IOBox.EnumBox("enum-box-id", "Enum Box Example", pid)
        with
          Properties =
            [| ("0", "zero")
             ; ("1", "one")
             ; ("2", "two")
             ; ("3", "three")
             |];
          Slices = [| { Idx = 0; Value = "2";  } |]
    }
  let patch = { Id       = pid
              ; Name     = "asdfas"
              ; IOBoxes  =  [| iob1; iob2; iob3; iob4; |]
              }

  let msg : ApiMessage = { Type = AddPatch; Payload = patch }
