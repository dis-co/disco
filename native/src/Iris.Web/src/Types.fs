[<ReflectedDefinition>]
module Iris.Web.Types

open FunScript.VirtualDom
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

type Slice (name : string, value: string) =
  let mutable name  = name
  let mutable value = value

type Slices = Slice array

type IViewPlugin =
  abstract render   : unit -> VTree
  abstract metadata : unit -> string
