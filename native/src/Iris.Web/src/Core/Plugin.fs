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
  abstract render  : IOBox       -> VTree
  abstract dispose : unit        -> unit
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


[<JSEmit("""return window.IrisPlugins;""")>]
let listPlugins () : IPluginSpec array = failwith "never"

[<JSEmit(
  """
  var pred = function (plugin) {
    return plugin.type === {0};
  };
  return window.IrisPlugins.filter(pred);
  """
)>]
let findPlugins (kind : string) : IPluginSpec array = failwith "never"

[<JSEmit(""" return ({0} === null) || ({0} === undefined); """)>]
let isNull (o : obj) : bool = failwith "never"

type Plugins () =
  (* ------------------------ internal -------------------------- *)
  [<JSEmit("""{0}[{1}] = {2};""")>]
  let addImpl (id : string) (inst : IPlugin) = failwith "never"

  [<JSEmit("""return {0}[{1}] != null;""")>]
  let hasImpl (id : string) : bool = failwith "never"

  [<JSEmit("""
           if({0}[{1}] != null) {
             {0}[{1}].dispose();
             {0}[{1}] = null;
             delete {0}[{1}];
           }
           """)>]
  let rmImpl (id : string) = failwith "never"

  [<JSEmit(""" return {0}[{1}]; """)>]
  let getImpl (id : string) = failwith "never"

  [<JSEmit(""" return Object.keys({0}); """)>]
  let idsImpl () : string array = failwith "never"

  (* ------------------------ public interface -------------------------- *)
  
  (* instantiate a new view plugin *)
  member self.add (iobox : IOBox) =
    let candidates = findPlugins iobox.kind
    in if candidates.length > 0.0
       then addImpl iobox.id (candidates.[0].Create ())
       else Globals.console.log("Could not instantiate view for IOBox. Type not found:  " + iobox.kind)

  member self.has (iobox : IOBox) : bool = hasImpl iobox.id

  member self.get (iobox : IOBox) : IPlugin option =
    let inst = getImpl iobox.id
    if isNull inst
    then None
    else Some(inst)

  (* remove an instance of a view plugin *)
  member self.remove (iobox : IOBox) = rmImpl iobox.id

  member self.ids () : string array = idsImpl ()
