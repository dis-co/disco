[<ReflectedDefinition>]
module Iris.Web.Types

open FunScript.VirtualDom

// open Iris.Core.Types.IOBox
// open Iris.Core.Types.Patch

type Slice (name : string, value: string) =
  let mutable name  = name
  let mutable value = value

type Slices = Slice array

type IOBox () =
  let mutable slices = Array.empty
  let   name   = ""
  let ``type`` = ""
  
  member x.Name
    with get () = name

  member x.Type
    with get () = ``type``
    
  member x.Slices
    with get ()  = slices
    and  set arr = slices <- arr


type Patch () =
  let name : string = ""
  let mutable ioboxes : IOBox array = Array.empty

  member x.Name
    with get () = name

  member x.IOBoxes
    with get () = ioboxes
    and  set bx = ioboxes <- bx


type MsgType = string

type MsgPayload =
  | IOBoxP of IOBox
  | PatchP of Patch
  | EmptyP
  
type Message (t : MsgType, p : MsgPayload) =
  let msgtype = t
  let payload = p

  member x.Type    with get () = msgtype
  member x.Payload with get () = payload

type EventType =
  | AddPin
  | RemovePin
  | UpdatePin

type EventData =
  | IOBoxD of IOBox
  | PatchD of Patch
  | EmptyD

type AppEvent =
  { Kind : EventType
  ; Data : EventData
  }

type Listener = (AppEvent -> unit)

type State =
  { Patches  : Patch list
  ; ViewTree : VTree option
  }
  static member empty =
    { Patches  = []
    ; ViewTree = None
    }

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

type IWebSocket =
  abstract send : string -> unit
  abstract close : unit -> unit
