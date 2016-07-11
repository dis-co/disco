namespace Iris.Web.Core

open Fable.Core
open Fable.Import

[<AutoOpen>]
module Plugin =

  open System
  open Iris.Core

  type EventCallback = IOBox -> unit

  //  ____  _             _
  // |  _ \| |_   _  __ _(_)_ __
  // | |_) | | | | |/ _` | | '_ \
  // |  __/| | |_| | (_| | | | | |
  // |_|   |_|\__,_|\__, |_|_| |_| + spec
  //                |___/

  type Plugin () =
    [<Emit("$0.render($1)")>]
    member this.Render (_: IOBox) : VTree = failwith "JS Only"

    [<Emit("$0.dispose()")>]
    member this.Dispose() : unit = failwith "ONLY IN JS"

  //  ____  _             _       ____
  // |  _ \| |_   _  __ _(_)_ __ / ___| _ __   ___  ___
  // | |_) | | | | |/ _` | | '_ \\___ \| '_ \ / _ \/ __|
  // |  __/| | |_| | (_| | | | | |___) | |_) |  __/ (__
  // |_|   |_|\__,_|\__, |_|_| |_|____/| .__/ \___|\___|
  //                |___/              |_|

  type PluginSpec ()  =
    [<Emit("$0.name")>]
    member __.Name () : string = failwith "ONLY IN JS"

    [<Emit("$0.type")>]
    member __.PinType () : PinType = failwith "ONLY IN JS"

    [<Emit("$0.create($1)")>]
    member __.Create (_: EventCallback) : Plugin = failwith "ONLY IN JS"

  //  _   _ _   _ _ _ _   _
  // | | | | |_(_) (_) |_(_) ___  ___
  // | | | | __| | | | __| |/ _ \/ __|
  // | |_| | |_| | | | |_| |  __/\__ \
  //  \___/ \__|_|_|_|\__|_|\___||___/

  [<Emit "return window.IrisPlugins">]
  let listPlugins () : PluginSpec array = failwith "JS Only"

  [<Emit("return window.IrisPlugins.filter(function (plugin) { return plugin.type === $0; })")>]
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
    member private __.GetImpl (_: Id) : Plugin = failwith "JS Only"

    [<Emit "Object.keys($0)">]
    member private __.IdsImpl () : Id array = failwith "ONLY IN JS"

    [<Emit("$0[$1] = $2")>]
    member private __.AddImpl(_: Id, _: Plugin) = failwith "ONLY IN JS"

    [<Emit "delete $0[$1] ">]
    member private __.RmImpl(_: Id) = failwith "ONLY IN JS"

    [<Emit " $0[$1] != null ">]
    member private __.HasImpl (_: Id) : bool = failwith "ONLY IN JS"

    (* ------------------------ public interface -------------------------- *)

    (* instantiate a new view plugin *)
    member self.Add (iobox : IOBox) (onupdate : EventCallback) =
      let t = getType iobox
      let candidates = findPlugins t
      in if candidates.Length > 0
         then self.AddImpl(iobox.Id, candidates.[0].Create(onupdate))
         else printfn "Could not instantiate view for IOBox. Type not found: %A" t

    member self.Has (iobox : IOBox) : bool = self.HasImpl(iobox.Id)

    member self.Get (iobox : IOBox) : Plugin option =
      let inst = self.GetImpl(iobox.Id)
      if isNull inst
      then None
      else Some(inst)

    (* remove an instance of a view plugin *)
    member self.Remove (iobox : IOBox) = self.RmImpl(iobox.Id)

    member self.Ids () : Id array = self.IdsImpl()

    interface IDisposable with
      member self.Dispose () =
        // FIXME should also call Dispose on the Plugin instance
        Array.map self.RmImpl (self.Ids ()) |> ignore
