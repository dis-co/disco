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

type IPlugin =
  abstract render  : unit   -> VTree
  abstract dispose : unit   -> unit
  abstract update  : IOBox  -> unit
  abstract get     : unit   -> Slices
  abstract set     : Slices -> unit
  abstract on      : string -> (unit -> unit) -> unit
  abstract off     : string -> unit

type IPluginSpec () =
  let mutable   name   = ""
  let mutable ``type`` = ""
  let mutable  create : (unit -> IPlugin) =
    (fun _ -> Unchecked.defaultof<_>)

  member x.Name    with get () = name
  member x.GetType with get () = ``type``
  member x.Create  with get () = create
