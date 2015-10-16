[<ReflectedDefinition>]
module Iris.Web.Core.Plugin

#nowarn "1182"

open FunScript
open FunScript.TypeScript
open FunScript.VirtualDom

open Iris.Web.Core.IOBox
open Iris.Web.Core.Patch

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


type Plugins () =
  (* ------------------------ internal -------------------------- *)

  [<JSEmit("""return window.IrisPlugins;""")>]
  let available () : IPluginSpec array = failwith "never"

  [<JSEmit("""{0}[{1}] = {2};""")>]
  let addImpl (id : string) (inst : IPlugin) = failwith "never"

  [<JSEmit("""return {0}[{1}] != null;""")>]
  let hasImpl (id : string) : bool = failwith "never"

  [<JSEmit("""
           if({0}[{1}] != null) {
             {0}[{1}].dispose();
             {0}[{1}] = null;
           }
           """)>]
  let rmImpl (id : string) = failwith "never"

  [<JSEmit(""" return {0}[{1}].render({2}); """)>]
  let renderImpl id iobox = failwith "never"

  [<JSEmit(""" {0}[{1}].set({2}) """)>]
  let updateImpl (id : string) (slices : Slice array) = failwith "never"

  (* ------------------------ public interface -------------------------- *)
  
  (* instantiate a new view plugin *)
  member self.add (iobox : IOBox) =
    let instance =
      available ()
      |> Array.tryFind (fun spec -> spec.GetType = iobox.kind)
    in match instance with
        | Some(spec) -> addImpl iobox.id (spec.Create ())
        | _          -> Globals.console.log("Could not instantiate view for IOBox. Type not found:  " + iobox.kind)

  member self.has  (iobox : IOBox) : bool  = hasImpl iobox.id

  member self.render (iobox : IOBox) : VTree = renderImpl iobox.id iobox

  member self.update (iobox : IOBox) : unit =
    updateImpl iobox.id iobox.slices

  member self.updateAll (patches : Patch list) = 
    for patch in patches do
      for iobox in patch.ioboxes do
        if hasImpl iobox.id
        then updateImpl iobox.id iobox.slices
        else self.add iobox

  (* remove an instance of a view plugin *)
  member self.remove (iobox : IOBox) = rmImpl iobox.id


