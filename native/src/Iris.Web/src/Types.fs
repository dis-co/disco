[<ReflectedDefinition>]
module Iris.Web.Types

open FunScript.VirtualDom

// open Iris.Core.Types.IOBox
// open Iris.Core.Types.Patch

(*   ___ ___  ____            
    |_ _/ _ \| __ )  _____  __
     | | | | |  _ \ / _ \ \/ /
     | | |_| | |_) | (_) >  < 
    |___\___/|____/ \___/_/\_\
*)

type Slice (name : string, value: string) =
  let mutable name  = name
  let mutable value = value

type Slices = Slice array

type IOBox () =
  let   id     = ""
  let   name   = ""
  let ``type`` = ""
  let mutable slices = Array.empty
  
  member x.Name
    with get () = name

  member x.Type
    with get () = ``type``
    
  member x.Slices
    with get ()  = slices
    and  set arr = slices <- arr


(*   ____       _       _     
    |  _ \ __ _| |_ ___| |__  
    | |_) / _` | __/ __| '_ \ 
    |  __/ (_| | || (__| | | |
    |_|   \__,_|\__\___|_| |_|
*)

type Patch () =
  let   id : string = ""
  let name : string = ""
  let mutable ioboxes : IOBox array = Array.empty

  member x.Name
    with get () = name

  member x.IOBoxes
    with get () = ioboxes
    and  set bx = ioboxes <- bx

(*   __  __                                
    |  \/  | ___  ___ ___  __ _  __ _  ___ 
    | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
    | |  | |  __/\__ \__ \ (_| | (_| |  __/
    |_|  |_|\___||___/___/\__,_|\__, |\___|
                                |___/      
*)
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

(*      _                _____                 _   
       / \   _ __  _ __ | ____|_   _____ _ __ | |_ 
      / _ \ | '_ \| '_ \|  _| \ \ / / _ \ '_ \| __|
     / ___ \| |_) | |_) | |___ \ V /  __/ | | | |_ 
    /_/   \_\ .__/| .__/|_____| \_/ \___|_| |_|\__|
            |_|   |_|                              
*)
type EventType =
  |      AddPin
  |   RemovePin
  |   UpdatePin
  |    AddPatch
  | UpdatePatch
  | RemovePatch

type EventData =
  | IOBoxD of IOBox
  | PatchD of Patch
  | EmptyD

type AppEvent =
  { Kind : EventType
  ; Data : EventData
  }

type Listener = (AppEvent -> unit)

(*   ____  _        _       
    / ___|| |_ __ _| |_ ___ 
    \___ \| __/ _` | __/ _ \
     ___) | || (_| | ||  __/
    |____/ \__\__,_|\__\___|
*)
type State =
  { Patches  : Patch list
  ; ViewTree : VTree option
  }
  static member empty =
    { Patches  = []
    ; ViewTree = None
    }

(*   ____  _             _       
    |  _ \| |_   _  __ _(_)_ __  
    | |_) | | | | |/ _` | | '_ \ 
    |  __/| | |_| | (_| | | | | |
    |_|   |_|\__,_|\__, |_|_| |_| + spec
                   |___/         
*)
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

(*  __        __   _    ____             _        _   
    \ \      / /__| |__/ ___|  ___   ___| | _____| |_ 
     \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
      \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_ 
       \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|
*)
type IWebSocket =
  abstract send : string -> unit
  abstract close : unit -> unit
