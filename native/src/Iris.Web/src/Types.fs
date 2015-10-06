[<ReflectedDefinition>]
module Iris.Web.Types

open FSharp.Html

open FunScript.TypeScript
open FunScript.VirtualDom

// open Iris.Core.Types.IOBox
// open Iris.Core.Types.Patch

(******************************************************************************)

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

(******************************************************************************)

(*   _____                 _   ____        _        
    | ____|_   _____ _ __ | |_|  _ \  __ _| |_ __ _ 
    |  _| \ \ / / _ \ '_ \| __| | | |/ _` | __/ _` |
    | |___ \ V /  __/ | | | |_| |_| | (_| | || (_| |
    |_____| \_/ \___|_| |_|\__|____/ \__,_|\__\__,_|

    Wrapper type around diffent payloads for events.
*)

type EventData =
  | IOBoxD of IOBox
  | PatchD of Patch
  | EmptyD


(*
        _                _____                 _   
       / \   _ __  _ __ | ____|_   _____ _ __ | |_ ™
      / _ \ | '_ \| '_ \|  _| \ \ / / _ \ '_ \| __|
     / ___ \| |_) | |_) | |___ \ V /  __/ | | | |_ 
    /_/   \_\ .__/| .__/|_____| \_/ \___|_| |_|\__|
            |_|   |_|                              

    The AppEventT type models all possible state-changes the app can legally
    undergo. Using this design, we have a clean understanding of how data flows
    through the system, and have the compiler assist us in handling all possible
    states with total functions.

*)

type AppEventT =
  | AddIOBox
  | RemoveIOBox
  | UpdateIOBox
  | AddPatch
  | UpdatePatch
  | RemovePatch
  | UnknownEvent

type AppEvent =
  { Kind    : AppEventT
  ; Payload : EventData
  }

type Listener = (AppEvent -> unit)

(******************************************************************************)

(*   ____  _        _       
    / ___|| |_ __ _| |_ ___ 
    \___ \| __/ _` | __/ _ \
     ___) | || (_| | ||  __/
    |____/ \__\__,_|\__\___|

    Record type containing all the actual data that gets passed around in our
    application.
*)

type State =
  { Patches  : Patch list }
  static member empty = { Patches  = [] }

(*
     ____  _                 
    / ___|| |_ ___  _ __ ___ 
    \___ \| __/ _ \| '__/ _ \
     ___) | || (_) | | |  __/
    |____/ \__\___/|_|  \___|

    The store centrally manages all state changes and notifies interested
    parties of changes to the carried state (e.g. views, socket transport).

*)

type Store () =
  let mutable state     : State = State.empty
  let mutable listeners : Listener list = []

  let notify ev =
    List.map (fun l -> l ev) listeners

  // let updatePins pins =
  //   state <- { state with IOBoxes = pins }

  let addPatch (patch : Patch) = 
    state <- { state with Patches = patch :: state.Patches }

  let updatePatch (patch : Patch) = ()
  let removePatch (patch : Patch) = ()

  let addIOBox    (iobox : IOBox) = ()
  let updateIOBox (iobox : IOBox) = ()
  let removeIOBox (iobox : IOBox) = ()

  member self.Dispatch (ev : AppEvent) =
    match ev with
      | { Kind = AddPatch;    Payload = PatchD(patch) } -> addPatch    patch
      | { Kind = UpdatePatch; Payload = PatchD(patch) } -> updatePatch patch
      | { Kind = RemovePatch; Payload = PatchD(patch) } -> removePatch patch
      | { Kind = AddIOBox;    Payload = IOBoxD(patch) } -> addIOBox    patch
      | { Kind = UpdateIOBox; Payload = IOBoxD(patch) } -> updateIOBox patch
      | { Kind = RemoveIOBox; Payload = IOBoxD(patch) } -> removeIOBox patch
      | _ -> Globals.console.log("unhandled event detected")

    notify ev |> ignore

  member self.Subscribe (listener : AppEvent -> unit) =
    listeners <- listener :: listeners

  member self.GetState
    with get () = state

(******************************************************************************)

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


(*  
    __        ___     _            _   
    \ \      / (_) __| | __ _  ___| |_ 
     \ \ /\ / /| |/ _` |/ _` |/ _ \ __|
      \ V  V / | | (_| | (_| |  __/ |_ 
       \_/\_/  |_|\__,_|\__, |\___|\__|
                        |___/          
*)

type IWidget =
  abstract render : Store -> VTree

(*
    __     ___                ____ _        _ 
    \ \   / (_) _____      __/ ___| |_ _ __| |
     \ \ / /| |/ _ \ \ /\ / / |   | __| '__| |
      \ V / | |  __/\ V  V /| |___| |_| |  | |
       \_/  |_|\___| \_/\_/  \____|\__|_|  |_|RRrrr..

    ViewController orchestrates the rendering of state changes and wraps up both
    the widget tree and the rendering context needed for virtual-dom.
*)

type ViewController (widget : IWidget) =
  let mutable view : IWidget      = widget 
  let mutable tree : VTree option = None
  let mutable root : Node  option = None 
  
  member self.init tree = 
    let rootNode = createElement tree
    Globals.document.body.appendChild(rootNode) |> ignore
    root <- Some(rootNode)

  (* render and patch the DOM *)
  member self.render (store : Store) : unit =  
    let newtree = view.render store

    match tree with
      | Some(oldtree) -> 
        match root with
          | Some(oldroot) -> 
            let update = diff oldtree newtree
            root <- Some(patch oldroot update)
          | _ -> self.init newtree
      | _ -> self.init newtree

    tree <- Some(newtree)

(******************************************************************************)

(*   __  __                                
    |  \/  | ___  ___ ___  __ _  __ _  ___ 
    | |\/| |/ _ \/ __/ __|/ _` |/ _` |/ _ \
    | |  | |  __/\__ \__ \ (_| | (_| |  __/
    |_|  |_|\___||___/___/\__,_|\__, |\___|
                                |___/      
*)
type MsgType = string

type Message (t : MsgType, p : EventData) =
  let msgtype = t
  let payload = p

  member x.Type    with get () = msgtype
  member x.Payload with get () = payload

(*  __        __   _    ____             _        _   
    \ \      / /__| |__/ ___|  ___   ___| | _____| |_ 
     \ \ /\ / / _ \ '_ \___ \ / _ \ / __| |/ / _ \ __|
      \ V  V /  __/ |_) |__) | (_) | (__|   <  __/ |_ 
       \_/\_/ \___|_.__/____/ \___/ \___|_|\_\___|\__|
*)

type IWebSocket =
  abstract send : string -> unit
  abstract close : unit -> unit
