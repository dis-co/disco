namespace Iris.Core.Types

[<AutoOpen>]
[<ReflectedDefinition>]
module CueList =

  type CueList =
    { Id   : Id
    ; Name : Name
    ; Cues : Cue array
    }
