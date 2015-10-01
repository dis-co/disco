[<ReflectedDefinition>]
module Iris.Web.Types

open Iris.Core.Types.IOBox
open Iris.Core.Types.Patch


type EventType =
  | AddPin
  | RemovePin
  | UpdatePin

type EventData =
  | IOBoxD of IOBox
  | PatchD of Patch


type AppEvent =
  { Kind : EventType
  ; Data : EventData
  }

type Listener = (AppEvent -> unit)

type State =
  { Patches : Patch list
  ; Pins    : IOBox list
  ; View    : VTree option
  }
  static member empty =
    { Patches = []
    ; Pins    = []
    ; View    = None
    }

type ViewPlugin (name : string ) =
  let mutable name = name
  member this.Name with get () = name
