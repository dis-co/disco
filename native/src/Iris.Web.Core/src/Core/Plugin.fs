namespace Iris.Web.Core

#nowarn "1182"

open WebSharper
open WebSharper.JavaScript

[<AutoOpen>]
[<JavaScript>]
module Plugin =

  open System
  open Iris.Core.Types

  (*   ____  _             _
      |  _ \| |_   _  __ _(_)_ __
      | |_) | | | | |/ _` | | '_ \
      |  __/| | |_| | (_| | | | | |
      |_|   |_|\__,_|\__, |_|_| |_| + spec
                     |___/
  *)
  [<Stub>]
  type Plugin () = class
    [<Name "render">]
    [<Stub>]
    member this.Render (iobox : IOBox) : VTree = X<_>

    [<Name "dispose">]
    [<Stub>]
    member this.Dispose() : unit = X<_>

    [<Name "get">]
    [<Stub>]
    member this.Get() : Slice array = X<_>

    [<Name "set">]
    [<Stub>]
    member this.Set(slices : Slice array) : unit = X<_>
  end

  type PluginSpec [<Inline "{}">] ()  =
    [<DefaultValue>]
    val mutable name : string

    [<DefaultValue>]
    val mutable ``type`` : string

    [<DefaultValue>]
    val mutable  create : unit -> Plugin


  [<Direct "return window.IrisPlugins">]
  let listPlugins () : PluginSpec array = X<PluginSpec array>

  [<Direct "return window.IrisPlugins.filter(function (plugin) { return plugin.type === $kind; })" >]
  let findPlugins (kind : PinType) : PluginSpec array = X<PluginSpec array>

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
    [<Direct " delete $ctx[$id] ">]
    let rmImpl (ctx : obj) (id : string) : unit = X<unit>

    [<Inline " $0[$id] = $inst ">]
    let addImpl (id : string) (inst : Plugin) : unit = X<unit>

    [<Inline " $0[$id] != null ">]
    let hasImpl (id : string) : bool = X<bool>

    [<Inline " $0[$id] ">]
    let getImpl (id : string) : Plugin = X<Plugin>

    [<Inline " Object.keys($0) ">]
    let idsImpl () : string array = X<string array>

    (* ------------------------ public interface -------------------------- *)

    (* instantiate a new view plugin *)
    member self.add (iobox : IOBox) =
      let candidates = findPlugins iobox.Type
      in if candidates.Length > 0
         then addImpl iobox.Id (candidates.[0].create ())
         else Console.Log("Could not instantiate view for IOBox. Type not found:  ", iobox.Type)

    member self.has (iobox : IOBox) : bool = hasImpl iobox.Id

    member self.get (iobox : IOBox) : Plugin option =
      let inst = getImpl iobox.Id
      if isNull inst
      then None
      else Some(inst)

    (* remove an instance of a view plugin *)
    member self.remove (iobox : IOBox) = rmImpl self iobox.Id

    member self.ids () : string array = idsImpl ()

    interface IDisposable with
      member self.Dispose () = Array.map rmImpl (self.ids ()) |> ignore
