namespace Iris.Web.Core

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript

[<JavaScript>]
module Plugin =

  open System

  open Iris.Web.Core.Html
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

  type IPluginSpec [<Inline "{}">] ()  =
    [<DefaultValue>]
    val mutable name : string

    [<DefaultValue>]
    val mutable ``type`` : string

    [<DefaultValue>]
    val mutable  create : unit -> IPlugin


  [<Direct "return window.IrisPlugins">]
  let listPlugins () : IPluginSpec array = X<IPluginSpec array>

  [<Direct "return window.IrisPlugins.filter(function (plugin) { return plugin.type === $kind; })" >]
  let findPlugins (kind : string) : IPluginSpec array = X<IPluginSpec array>

  [<Direct "return ($o === null) || ($o === undefined)">]
  let isNull (o : obj) : bool = X<bool>

  (*
     ____  _             _
    |  _ \| |_   _  __ _(_)_ __  ___
    | |_) | | | | |/ _` | | '_ \/ __|
    |  __/| | |_| | (_| | | | | \__ \
    |_|   |_|\__,_|\__, |_|_| |_|___/ instances map
                   |___/
  *)

  type Plugins () =
    (* ------------------------ internal -------------------------- *)
    [<Inline " $0[$id] = $inst ">]
    let addImpl (id : string) (inst : IPlugin) : unit = X<unit>

    [<Inline " $0[$id] != null ">]
    let hasImpl (id : string) : bool = X<bool>

    [<Inline " $0[$id] = null ">]
    let rmImpl (id : string) : unit = X<unit>

    [<Inline " $0[$id] ">]
    let getImpl (id : string) : IPlugin = X<IPlugin>

    [<Inline " Object.keys($0) ">]
    let idsImpl () : string array = X<string array>

    (* ------------------------ public interface -------------------------- *)

    (* instantiate a new view plugin *)
    member self.add (iobox : IOBox) =
      let candidates = findPlugins iobox.kind
      in if candidates.Length > 0
         then addImpl iobox.id (candidates.[0].create ())
         else Console.Log("Could not instantiate view for IOBox. Type not found:  " + iobox.kind)

    member self.has (iobox : IOBox) : bool = hasImpl iobox.id

    member self.get (iobox : IOBox) : IPlugin option =
      let inst = getImpl iobox.id
      if isNull inst
      then None
      else Some(inst)

    (* remove an instance of a view plugin *)
    member self.remove (iobox : IOBox) = rmImpl iobox.id

    member self.ids () : string array = idsImpl ()

    interface IDisposable with
      member self.Dispose () = Array.map rmImpl (self.ids ()) |> ignore
