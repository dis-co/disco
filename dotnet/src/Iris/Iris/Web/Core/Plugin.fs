namespace Iris.Web.Core

open Fable.Core
open Fable.Import

[<AutoOpen>]
module Plugin =

  open System
  open Iris.Core

  (*   ____  _             _
      |  _ \| |_   _  __ _(_)_ __
      | |_) | | | | |/ _` | | '_ \
      |  __/| | |_| | (_| | | | | |
      |_|   |_|\__,_|\__, |_|_| |_| + spec
                     |___/
  *)

  type EventCallback = IOBox -> unit
                     
  type Plugin () =
    member this.Render (_: IOBox) : VTree = failwith "JS Only"
    member this.Dispose() : unit = failwith "JS Only"

  type PluginSpec ()  =
    [<DefaultValue>] val mutable name : string
    [<DefaultValue>] val mutable ``type`` : string
    [<DefaultValue>] val mutable  create : EventCallback -> Plugin


  [<Emit "return window.IrisPlugins">]
  let listPlugins () : PluginSpec array = failwith "JS Only"

  [<Emit "return window.IrisPlugins.filter(function (plugin) { return plugin.type === $0; })" >]
  let findPlugins (_: PinType) : PluginSpec array = failwith "JS Only"

  [<Emit "return ($0 === null) || ($0 === undefined)">]
  let isNull (_: obj) : bool = failwith "JS Only"

  //  ____  _             _
  // |  _ \| |_   _  __ _(_)_ __  ___
  // | |_) | | | | |/ _` | | '_ \/ __|
  // |  __/| | |_| | (_| | | | | \__ \
  // |_|   |_|\__,_|\__, |_|_| |_|___/ instances map
  //                |___/

  type Plugins () =
    (* ------------------------ internal -------------------------- *)
    [<Emit "$0[$1]">]
    member private __.GetImpl (_: string) : Plugin = failwith "JS Only"

    [<Emit "Object.keys($0)">]
    member private __.IdsImpl () : string array = failwith "ONLY IN JS"

    [<Emit("$0[$1] = $2")>]
    member private __.AddImpl(_: string, _: Plugin) = failwith "ONLY IN JS"

    [<Emit "delete $0[$1] ">]
    member private __.RmImpl(_: string) = failwith "ONLY IN JS"

    [<Emit " $0[$1] != null ">]
    member private __.HasImpl (_: string) : bool = failwith "ONLY IN JS"

    (* ------------------------ public interface -------------------------- *)

    (* instantiate a new view plugin *)
    member self.Add (iobox : IOBox) (onupdate : EventCallback) =
      let candidates = findPlugins iobox.Type
      in if candidates.Length > 0
         then self.AddImpl(iobox.Id, candidates.[0].create(onupdate))
         else printfn "Could not instantiate view for IOBox. Type not found: %A" iobox.Type

    member self.Has (iobox : IOBox) : bool = self.HasImpl(iobox.Id)

    member self.Get (iobox : IOBox) : Plugin option =
      let inst = self.GetImpl(iobox.Id)
      if isNull inst
      then None
      else Some(inst)

    (* remove an instance of a view plugin *)
    member self.Remove (iobox : IOBox) = self.RmImpl(iobox.Id)

    member self.Ids () : string array = self.IdsImpl()

    interface IDisposable with
      member self.Dispose () =
        // FIXME should also call Dispose on the Plugin instance
        Array.map self.RmImpl (self.Ids ()) |> ignore
