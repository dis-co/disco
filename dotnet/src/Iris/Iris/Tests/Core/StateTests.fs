namespace Iris.Tests

open System
open Expecto
open Iris.Core
open Iris.Raft
open System.Net
open FSharpx.Functional
open Iris.Core

[<AutoOpen>]
module StateTests =

  let mkState () =
    let pins =
      [| Pin.Bang      (Id.Create(), "Bang",      Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
      ; Pin.Toggle    (Id.Create(), "Toggle",    Id.Create(), mktags (), [|{ Index = 0u; Value = true    }|])
      ; Pin.String    (Id.Create(), "string",    Id.Create(), mktags (), [|{ Index = 0u; Value = "one"   }|])
      ; Pin.MultiLine (Id.Create(), "multiline", Id.Create(), mktags (), [|{ Index = 0u; Value = "two"   }|])
      ; Pin.FileName  (Id.Create(), "filename",  Id.Create(), mktags (), "haha", [|{ Index = 0u; Value = "three" }|])
      ; Pin.Directory (Id.Create(), "directory", Id.Create(), mktags (), "hmmm", [|{ Index = 0u; Value = "four"  }|])
      ; Pin.Url       (Id.Create(), "url",       Id.Create(), mktags (), [|{ Index = 0u; Value = "five"  }|])
      ; Pin.IP        (Id.Create(), "ip",        Id.Create(), mktags (), [|{ Index = 0u; Value = "six"   }|])
      ; Pin.Float     (Id.Create(), "float",     Id.Create(), mktags (), [|{ Index = 0u; Value = 3.0    }|])
      ; Pin.Double    (Id.Create(), "double",    Id.Create(), mktags (), [|{ Index = 0u; Value = double 3.0 }|])
      ; Pin.Bytes     (Id.Create(), "bytes",     Id.Create(), mktags (), [|{ Index = 0u; Value = [| 2uy; 9uy |] }|])
      ; Pin.Color     (Id.Create(), "rgba",      Id.Create(), mktags (), [|{ Index = 0u; Value = RGBA { Red = 255uy; Blue = 255uy; Green = 255uy; Alpha = 255uy } }|])
      ; Pin.Color     (Id.Create(), "hsla",      Id.Create(), mktags (), [|{ Index = 0u; Value = HSLA { Hue = 255uy; Saturation = 255uy; Lightness = 255uy; Alpha = 255uy } }|])
      ; Pin.Enum      (Id.Create(), "enum",      Id.Create(), mktags (), [|{ Key = "one"; Value = "two" }; { Key = "three"; Value = "four"}|] , [|{ Index = 0u; Value = { Key = "one"; Value = "two" }}|])
      |]
      |> Array.map (fun p -> (p.Id,p))
      |> Map.ofArray

    let patch : Patch =
      { Id   = Id "0xb4d1d34"
        Name = "patch-1"
        Pins = pins }

    let user =
      { Id = Id.Create()
      ; UserName = "krgn"
      ; FirstName = "Karsten"
      ; LastName = "Gebbert"
      ; Email = "k@ioctl.it"
      ; Password = "1234"
      ; Salt = "909090"
      ; Joined = System.DateTime.Now
      ; Created = System.DateTime.Now
      }

    let cue =
      { Id = Id.Create(); Name = "Cue 1"; Pins = pins () }

    let machine = MachineConfig.create ()

    let project = Project.create "test-project" machine

    let state =
      { Project  = project
        Patches  = Map.empty
        Cues     = Map.empty
        CueLists = Map.empty
        Users    = Map.empty
        Sessions = Map.empty }



  let test_load_state_correctly =
    testCase "should load state correctly" <| fun _ ->


  let stateTests =
    testList "State Tests" [
      test_load_state_correctly
    ]
