namespace Iris.Core.Types

[<AutoOpen>]
[<ReflectedDefinition>]
module Project =

  (* ---------- Project ---------- *)
  type Project =
    { Id       : Id
    ; Name     : string
    ; Path     : FilePath
    ; Cues     : Cue array
    ; CueLists : CueList array
    }

