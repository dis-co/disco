namespace Iris.Core.Config

open Iris.Core.Types

/// Models 
[<AutoOpen>]
[<ReflectedDefinition>]
module Raft =

  type RaftConfig =
    { RequestTimeout : uint32
    ; TempDir : string
    }
    with
      static member Default =
        { RequestTimeout = 1000u
        ; TempDir        = "ohai"
        }

