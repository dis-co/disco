[<ReflectedDefinition>]
module Iris.Web.Types.Plugin

open Iris.Web.Types.IOBox
open FunScript.VirtualDom

(*   ____  _             _       
    |  _ \| |_   _  __ _(_)_ __  
    | |_) | | | | |/ _` | | '_ \ 
    |  __/| | |_| | (_| | | | | |
    |_|   |_|\__,_|\__, |_|_| |_| + spec
                   |___/         
*)
type IPlugin =
  abstract render  : unit        -> VTree
  abstract dispose : unit        -> unit
  abstract update  : IOBox       -> unit
  abstract get     : unit        -> Slice array
  abstract set     : Slice array -> unit
  abstract on      : string      -> (unit -> unit) -> unit
  abstract off     : string      -> unit

type IPluginSpec () =
  let mutable   name   = ""
  let mutable ``type`` = ""
  let mutable  create : (unit -> IPlugin) =
    (fun _ -> Unchecked.defaultof<_>)

  member self.Name    with get () = name
  member self.GetType with get () = ``type``
  member self.Create  with get () = create

