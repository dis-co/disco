namespace Iris.Core.Types

[<AutoOpen>]
module CueList =

  type CueList =
    { Id   : Id
    ; Name : Name
    ; Cues : Cue array
    }
