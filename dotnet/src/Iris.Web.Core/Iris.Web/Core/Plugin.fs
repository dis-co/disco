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
                     
  type Plugin () = class
    [<Emit "render">]
    member this.Render (iobox : IOBox) : VTree = failwith "JS Only"

    [<Emit "dispose">]
    member this.Dispose() : unit = failwith "JS Only"
  end

  type PluginSpec [<Emit "{}">] ()  =
    [<DefaultValue>] val mutable name : string
    [<DefaultValue>] val mutable ``type`` : string
    [<DefaultValue>] val mutable  create : EventCallback -> Plugin


  [<Emit "return window.IrisPlugins">]
  let listPlugins () : PluginSpec array = failwith "JS Only"

  [<Emit "return window.IrisPlugins.filter(function (plugin) { return plugin.type === $kind; })" >]
  let findPlugins (kind : PinType) : PluginSpec array = failwith "JS Only"

  [<Emit "return ($o === null) || ($o === undefined)">]
  let isNull (o : obj) : bool = failwith "JS Only"

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
    [<Emit " delete $ctx[$id] ">]
    let rmImpl (ctx : obj) (id : string) : unit = failwith "JS Only"

    [<Emit " $0[$id] = $inst ">]
    let addImpl (id : string) (inst : Plugin) : unit = failwith "JS Only"

    [<Emit " $0[$id] != null ">]
    let hasImpl (id : string) : bool = failwith "JS Only"

    [<Emit " $0[$id] ">]
    let getImpl (id : string) : Plugin = failwith "JS Only"

    [<Emit " Object.keys($0) ">]
    let idsImpl () : string array = failwith "JS Only"

    (* ------------------------ public interface -------------------------- *)

    (* instantiate a new view plugin *)
    member self.Add (iobox : IOBox) (onupdate : EventCallback) =
      let candidates = findPlugins iobox.Type
      in if candidates.Length > 0
         then addImpl iobox.Id (candidates.[0].create(onupdate))
         else printfn "Could not instantiate view for IOBox. Type not found: %A" iobox.Type

    member self.Has (iobox : IOBox) : bool = hasImpl iobox.Id

    member self.Get (iobox : IOBox) : Plugin option =
      let inst = getImpl iobox.Id
      if isNull inst
      then None
      else Some(inst)

    (* remove an instance of a view plugin *)
    member self.Remove (iobox : IOBox) = rmImpl self iobox.Id

    member self.Ids () : string array = idsImpl ()

    interface IDisposable with
      member self.Dispose () = Array.map rmImpl (self.Ids ()) |> ignore
