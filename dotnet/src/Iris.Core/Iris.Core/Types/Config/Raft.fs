namespace Iris.Core.Config

open Iris.Core.Types

/// Models 
[<AutoOpen>]
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

